-- =============================================================================
-- Migration: normalizar_especialidade_convidada_legado
-- Timestamp: 20260604131549
-- Briefing: 2026-06-04_003 (CA8 / CA9 / CA10)
-- Responsável: imedto-database
-- =============================================================================
--
-- OBJETIVO
-- --------
-- Normalizar o campo texto-livre `especialidade_convidada` da tabela
-- `vinculo_profissional_estabelecimento` contra o catálogo global de
-- especialidades, usando a profissão do próprio vínculo como âncora.
--
-- REGRA DE NORMALIZAÇÃO
-- ---------------------
-- Para cada vínculo com `especialidade_convidada IS NOT NULL`:
--   - Normaliza via: lower(unaccent(trim(valor)))
--   - Busca especialidade ATIVA na tabela `especialidades` cuja profissao_id
--     coincide com `profissao_convidada_id` do vínculo e cujo nome normalizado
--     bate (lower(unaccent(trim(nome)))).
--   - MATCH    → grava o nome CANÔNICO do catálogo (ex: "dermatologia" → "Dermatologia").
--   - SEM MATCH → grava NULL (cai no COALESCE(v.especialidade_convidada, p.especialidade)
--     para o cadastro global do profissional).
--
-- VÍNCULOS SEM profissao_convidada_id
-- ------------------------------------
-- Se profissao_convidada_id IS NULL, não há catálogo de referência para casar;
-- especialidade_convidada vira NULL. Decisão documentada: sem profissão de
-- âncora, qualquer string livre é irrecuperável — o COALESCE usa o cadastro
-- global, que é a fonte autoritativa nesses casos.
--
-- INVARIANTES
-- -----------
-- - `profissao_convidada_id` NUNCA é alterada por esta migration.
-- - Vínculos com `especialidade_convidada IS NULL` não são tocados (WHERE filtra).
-- - Após a 1ª execução, os valores são canônicos ou NULL: reaplicar não altera
--   nenhum registro (idempotência garantida pelo WHERE de normalização).
-- - Catálogos `profissoes` e `especialidades` são GLOBAIS (sem estabelecimento_id).
--   O casamento usa a profissão do próprio vínculo — sem cruzamento entre tenants.
--
-- EXTENSÃO unaccent
-- -----------------
-- Habilitada na migration `20260509032304_InitialCreate`. Função
-- `public.imutable_unaccent(text)` disponível (wrapper IMMUTABLE sobre unaccent).
-- Nenhuma nova extensão necessária.
--
-- BACKFILL: fora do output do EF Core (CREATE INDEX CONCURRENTLY / UPDATE em
-- massa não podem rodar dentro de transação de migration EF). Aplicado pela
-- pipeline via psql direto.
-- =============================================================================

DO $$
DECLARE
    v_total_com_especialidade  integer := 0;
    v_casaram                  integer := 0;
    v_viraram_null             integer := 0;
    v_sem_profissao_virou_null integer := 0;
BEGIN
    -- -------------------------------------------------------------------------
    -- Passo 1: contar estado ANTES (apenas para relatório — não altera dados)
    -- -------------------------------------------------------------------------
    SELECT COUNT(*)
    INTO v_total_com_especialidade
    FROM public.vinculo_profissional_estabelecimento
    WHERE especialidade_convidada IS NOT NULL;

    RAISE NOTICE '[normalizar_especialidade_convidada_legado] Total de vínculos com especialidade_convidada não-nula: %',
        v_total_com_especialidade;

    -- -------------------------------------------------------------------------
    -- Passo 2: MATCH → atualizar para o nome canônico do catálogo
    --
    -- Condição de idempotência: o valor atual já deve ser diferente do nome
    -- canônico para ser atualizado. Após a 1ª execução, especialidade_convidada
    -- já é o nome canônico — lower(unaccent(trim(e.nome))) = lower(unaccent(trim(e.nome))),
    -- mas e.nome != e.nome é false, então nenhum UPDATE dispara de novo.
    --
    -- Multi-tenant: o JOIN usa profissao_convidada_id do próprio vínculo —
    -- cada vínculo só casa contra a profissão que ele mesmo declara, sem
    -- vazamento entre estabelecimentos.
    -- -------------------------------------------------------------------------
    WITH casamentos AS (
        UPDATE public.vinculo_profissional_estabelecimento v
        SET especialidade_convidada = e.nome
        FROM public.especialidades e
        WHERE v.especialidade_convidada IS NOT NULL
          AND v.profissao_convidada_id IS NOT NULL
          AND e.profissao_id = v.profissao_convidada_id
          AND e.ativo = true
          AND lower(public.imutable_unaccent(trim(e.nome)))
              = lower(public.imutable_unaccent(trim(v.especialidade_convidada)))
          -- Idempotência: só atualiza se o valor ainda não é canônico
          AND v.especialidade_convidada IS DISTINCT FROM e.nome
        RETURNING 1
    )
    SELECT COUNT(*) INTO v_casaram FROM casamentos;

    RAISE NOTICE '[normalizar_especialidade_convidada_legado] Vínculos casados com catálogo (→ nome canônico): %',
        v_casaram;

    -- -------------------------------------------------------------------------
    -- Passo 3a: SEM MATCH com profissao_convidada_id → vira NULL
    --
    -- Vínculos com profissao_convidada_id preenchida, mas cujo valor de
    -- especialidade_convidada não casa com nenhuma especialidade ativa da
    -- profissão (após passo 2, o que sobra é o não-casado).
    --
    -- Idempotência: após 1ª execução, esses registros já são NULL (o WHERE
    -- especialidade_convidada IS NOT NULL elimina-os na reexecução).
    -- -------------------------------------------------------------------------
    WITH sem_match AS (
        UPDATE public.vinculo_profissional_estabelecimento v
        SET especialidade_convidada = NULL
        WHERE v.especialidade_convidada IS NOT NULL
          AND v.profissao_convidada_id IS NOT NULL
          -- Não existe especialidade ativa que case normalizado
          AND NOT EXISTS (
              SELECT 1
              FROM public.especialidades e
              WHERE e.profissao_id = v.profissao_convidada_id
                AND e.ativo = true
                AND lower(public.imutable_unaccent(trim(e.nome)))
                    = lower(public.imutable_unaccent(trim(v.especialidade_convidada)))
          )
        RETURNING 1
    )
    SELECT COUNT(*) INTO v_viraram_null FROM sem_match;

    RAISE NOTICE '[normalizar_especialidade_convidada_legado] Vínculos sem match (→ NULL, profissão preenchida): %',
        v_viraram_null;

    -- -------------------------------------------------------------------------
    -- Passo 3b: SEM profissao_convidada_id → especialidade também vira NULL
    --
    -- Sem profissão de âncora, não há catálogo para casar. O COALESCE usa o
    -- cadastro global do profissional. Decisão: tornar NULL explicitamente.
    -- -------------------------------------------------------------------------
    WITH sem_profissao AS (
        UPDATE public.vinculo_profissional_estabelecimento v
        SET especialidade_convidada = NULL
        WHERE v.especialidade_convidada IS NOT NULL
          AND v.profissao_convidada_id IS NULL
        RETURNING 1
    )
    SELECT COUNT(*) INTO v_sem_profissao_virou_null FROM sem_profissao;

    RAISE NOTICE '[normalizar_especialidade_convidada_legado] Vínculos sem profissão (→ NULL): %',
        v_sem_profissao_virou_null;

    -- -------------------------------------------------------------------------
    -- Relatório final
    -- -------------------------------------------------------------------------
    RAISE NOTICE '=================================================================';
    RAISE NOTICE '[normalizar_especialidade_convidada_legado] RELATÓRIO FINAL';
    RAISE NOTICE '  Total com especialidade (antes):  %', v_total_com_especialidade;
    RAISE NOTICE '  Casaram → nome canônico:          %', v_casaram;
    RAISE NOTICE '  Sem match → NULL:                 %', v_viraram_null;
    RAISE NOTICE '  Sem profissão → NULL:             %', v_sem_profissao_virou_null;
    RAISE NOTICE '  Verificação (casaram + nulls):    %', v_casaram + v_viraram_null + v_sem_profissao_virou_null;
    RAISE NOTICE '=================================================================';

END $$;
