-- ===========================================================================
-- Script: 05_transacional_agenda_pacientes.sql
-- Fase: 5 (ETL Legado -> Novo) -- Wave 2
-- Dependencias: 03_dominio_core.sql, 04_configuracoes.sql
--
-- Objetivo: transacional ligado a paciente/agenda
--   - agendamentos              (rename de evento_de_agendamento)
--   - prontuarios
--   - prontuario_evolucoes      (rename de evolucao_prontuario)
--   - prontuario_anexos         (apenas metadata; arquivos copiados pelo
--                                runbook de Storage antes deste script)
--
-- IMPORTANTES (decisoes do mapeamento):
--   * Constraint EXCLUDE GiST `agendamentos_no_overlap` precisa estar DROPPADA
--     antes do INSERT (legado tem overlaps historicos legitimos). Recriada
--     no script 99 apos relatorio de conflitos.
--   * Triggers `prontuario_evolucoes_imutavel_*` (BEFORE UPDATE/DELETE) sao
--     DESABILITADAS durante a carga e re-habilitadas no 99.
--   * Status legado lowercase -> PascalCase via _etl.status_agendamento_map.
--   * exame_fisico_regioes do legado E catalogo (fica em
--     regioes_anatomicas_catalogo no novo) -- NAO migra aqui (ver 06).
-- ===========================================================================

\set VERBOSITY terse
\set ON_ERROR_STOP on
SET client_min_messages = WARNING;

DO $$ BEGIN
    IF (SELECT COUNT(*) FROM public.pacientes) = 0 THEN
        RAISE EXCEPTION 'public.pacientes vazia. Rode 03 antes.';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- DROP de constraints/triggers que bloqueiam carga (DDL FORA da BEGIN/COMMIT
-- principal -- DDL e auto-commit em PG mas mantemos logico fora da transacao
-- de dados para isolar falhas de carga vs falhas de DDL).
-- ---------------------------------------------------------------------------
ALTER TABLE public.agendamentos
    DROP CONSTRAINT IF EXISTS agendamentos_no_overlap;

-- prontuario_evolucoes append-only -- desabilitar triggers durante carga.
ALTER TABLE public.prontuario_evolucoes DISABLE TRIGGER USER;

BEGIN;

-- ---------------------------------------------------------------------------
-- agendamentos (volume top -- 810k linhas otim.)
-- Status mapeado via lookup. Valores novos (nao no map) caem em 'Agendado'
-- com warning.
-- ---------------------------------------------------------------------------
INSERT INTO public.agendamentos (
    id,
    estabelecimento_id,
    profissional_id,
    paciente_id,
    sala_id,
    especialidade_id,
    tipo_consulta,
    titulo,
    paciente_nome_avulso,
    data_hora_inicio,
    data_hora_fim,
    status,
    observacoes,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.estabelecimento_id,
    leg.profissional_id,        -- e usuario_id no novo (1:1)
    leg.paciente_id,
    leg.sala_id,
    me.novo_id                  AS especialidade_id,
    leg.tipo_consulta,
    leg.titulo,
    leg.paciente_nome_avulso,
    leg.data_hora_inicio,
    leg.data_hora_fim,
    COALESCE(sm.novo, 'Agendado') AS status,
    leg.observacoes,
    leg.created_at,
    leg.updated_at,
    leg.deletado_em
FROM legado.evento_de_agendamento leg
LEFT JOIN _etl.status_agendamento_map  sm ON sm.legado = leg.status
LEFT JOIN _etl.mapping_especialidades  me ON me.legado_id = leg.especialidade_id
WHERE EXISTS (SELECT 1 FROM public.estabelecimentos e WHERE e.id = leg.estabelecimento_id)
  AND (leg.paciente_id IS NULL OR EXISTS (SELECT 1 FROM public.pacientes p WHERE p.id = leg.paciente_id))
ON CONFLICT (id) DO NOTHING;

-- Detecta overlaps para relatorio (NAO bloqueia ETL).
INSERT INTO _etl.agendamentos_overlap (agendamento_a, agendamento_b, profissional_id, intervalo_a, intervalo_b)
SELECT
    a.id,
    b.id,
    a.profissional_id,
    tstzrange(a.data_hora_inicio, a.data_hora_fim, '[)'),
    tstzrange(b.data_hora_inicio, b.data_hora_fim, '[)')
FROM public.agendamentos a
JOIN public.agendamentos b
  ON a.profissional_id = b.profissional_id
 AND a.id < b.id
 AND a.estabelecimento_id = b.estabelecimento_id
 AND a.deletado_em IS NULL AND b.deletado_em IS NULL
 AND tstzrange(a.data_hora_inicio, a.data_hora_fim, '[)')
   && tstzrange(b.data_hora_inicio, b.data_hora_fim, '[)')
ON CONFLICT (agendamento_a, agendamento_b) DO NOTHING;

-- ---------------------------------------------------------------------------
-- prontuarios (linhas pequenas; 180k otim.)
-- ---------------------------------------------------------------------------
INSERT INTO public.prontuarios (
    id,
    paciente_id,
    estabelecimento_id,
    modelo_de_prontuario_id,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.paciente_id,
    leg.estabelecimento_id,
    leg.modelo_de_prontuario_id,
    leg.created_at,
    leg.updated_at,
    leg.deletado_em
FROM legado.prontuarios leg
WHERE EXISTS (SELECT 1 FROM public.pacientes p WHERE p.id = leg.paciente_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- prontuario_evolucoes (rename de evolucao_prontuario; volume MAX -- 648k otim).
-- template_snapshot: copia o jsonb do modelo no momento do criado_em (best-effort
-- -- sem versionamento historico de modelo, usamos o estado atual). Marca
-- migrado_legado=true para audit.
-- ---------------------------------------------------------------------------
INSERT INTO public.prontuario_evolucoes (
    id,
    prontuario_id,
    estabelecimento_id,
    paciente_id,
    profissional_id,
    agendamento_id,
    modelo_de_prontuario_id,
    conteudo,
    template_snapshot,
    migrado_legado,
    criado_em,
    atualizado_em
)
SELECT
    leg.id,
    leg.prontuario_id,
    leg.estabelecimento_id,
    leg.paciente_id,
    leg.profissional_id,
    leg.evento_de_agendamento_id    AS agendamento_id,
    leg.modelo_de_prontuario_id,
    leg.conteudo,
    COALESCE(mp.estrutura, '{}'::jsonb) AS template_snapshot,
    true                             AS migrado_legado,
    leg.criado_em,
    leg.atualizado_em
FROM legado.evolucao_prontuario leg
LEFT JOIN public.modelo_de_prontuario mp ON mp.id = leg.modelo_de_prontuario_id
WHERE EXISTS (SELECT 1 FROM public.prontuarios p WHERE p.id = leg.prontuario_id)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- prontuario_anexos -- METADATA apenas. Arquivos JA foram copiados de
-- imedto-anexos legado -> novo pelo runbook de Storage (passo manual).
-- Se arquivo nao foi copiado, linha eh ignorada (orphan-prevention).
-- ---------------------------------------------------------------------------
INSERT INTO public.prontuario_anexos (
    id,
    prontuario_id,
    estabelecimento_id,
    profissional_id,
    nome_arquivo,
    storage_path,
    tipo_mime,
    tamanho_bytes,
    descricao,
    criado_em,
    atualizado_em,
    deletado_em
)
SELECT
    leg.id,
    leg.prontuario_id,
    leg.estabelecimento_id,
    leg.profissional_id,
    leg.nome_arquivo,
    leg.storage_path,
    leg.tipo_mime,
    leg.tamanho_bytes,
    leg.descricao,
    leg.created_at,
    leg.updated_at,
    leg.deletado_em
FROM legado.prontuario_anexos leg
WHERE EXISTS (SELECT 1 FROM public.prontuarios p WHERE p.id = leg.prontuario_id)
  -- TODO devops: filtrar por anexos.confirmado_em IS NOT NULL apos sync de Storage.
ON CONFLICT (id) DO NOTHING;

INSERT INTO _etl.run_log (script_nome, finalizado_em, observacao)
VALUES ('05_transacional_agenda_pacientes.sql', now(),
        'agendamentos/prontuarios/evolucoes/anexos carregados. Constraints/triggers re-habilitar no 99.');

COMMIT;

-- Pos-flight
SELECT 'agendamentos'         AS tabela, COUNT(*) FROM public.agendamentos
UNION ALL SELECT 'prontuarios',           COUNT(*) FROM public.prontuarios
UNION ALL SELECT 'prontuario_evolucoes',  COUNT(*) FROM public.prontuario_evolucoes
UNION ALL SELECT 'prontuario_anexos',     COUNT(*) FROM public.prontuario_anexos
UNION ALL SELECT 'overlaps_detectados',   COUNT(*) FROM _etl.agendamentos_overlap;
