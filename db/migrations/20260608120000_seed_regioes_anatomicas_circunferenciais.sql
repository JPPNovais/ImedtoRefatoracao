-- ─────────────────────────────────────────────────────────────────────────────
-- Seed — 9 nós agregadores de vista circunferencial (briefing 2026-06-08_005 B1)
--
-- Adiciona os nós nível-1 de vista "circunferencial" ao catálogo global de
-- regiões anatômicas. Cada nó é um agregador sem filhos próprios: a modal
-- resolve os filhos em tempo de execução via getFilhos("{base}-anterior") +
-- getFilhos("{base}-posterior"). Exceção clínica: abdome-circunferencial →
-- ramo posterior = lombossacra-posterior (não abdome-posterior, inexistente).
--
-- Decisão 2026-06-23: códigos de membros expandidos por extenso (membro-*).
-- Sub-regiões (nível 2/3) NÃO são seedadas — gerenciadas manualmente.
--
-- Campo vista: character varying(20) — texto livre sem CHECK constraint.
-- Aceita 'circunferencial' sem alteração de schema (confirmado via snapshot EF
-- e RegiaoAnatomicaCatalogoConfiguration.cs — HasMaxLength(20)).
--
-- Idempotente: ON CONFLICT (codigo) DO NOTHING — seguro para re-runs.
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- ─────────────────────────────────────────────────────────────────────────────

-- ── NÍVEL 1 — Vista CIRCUNFERENCIAL (9 nós agregadores) ──────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, svg_coords, ordem, lateralidade, ativo)
VALUES
    -- Cabeça circunferencial: une cabeca-anterior + cabeca-posterior
    ('cabeca-circunferencial',                        'Cabeça (circunferencial)',                         NULL, 1, 'circunferencial', NULL, NULL, 19, false, true),
    -- Pescoço circunferencial: une pescoco-anterior + pescoco-posterior
    ('pescoco-circunferencial',                       'Pescoço (circunferencial)',                        NULL, 1, 'circunferencial', NULL, NULL, 20, false, true),
    -- Tórax circunferencial: une torax-anterior + torax-posterior
    ('torax-circunferencial',                         'Tórax (circunferencial)',                          NULL, 1, 'circunferencial', NULL, NULL, 21, false, true),
    -- Abdome circunferencial: une abdome-anterior + lombossacra-posterior (exceção clínica — não existe abdome-posterior)
    ('abdome-circunferencial',                        'Abdome (circunferencial)',                         NULL, 1, 'circunferencial', NULL, NULL, 22, false, true),
    -- Pelve circunferencial: une pelve-anterior + pelve-posterior
    ('pelve-circunferencial',                         'Pelve (circunferencial)',                          NULL, 1, 'circunferencial', NULL, NULL, 23, false, true),
    -- Membros superiores
    ('membro-superior-direito-circunferencial',       'Membro superior direito (circunferencial)',        NULL, 1, 'circunferencial', NULL, NULL, 24, false, true),
    ('membro-superior-esquerdo-circunferencial',      'Membro superior esquerdo (circunferencial)',       NULL, 1, 'circunferencial', NULL, NULL, 25, false, true),
    -- Membros inferiores
    ('membro-inferior-direito-circunferencial',       'Membro inferior direito (circunferencial)',        NULL, 1, 'circunferencial', NULL, NULL, 26, false, true),
    ('membro-inferior-esquerdo-circunferencial',      'Membro inferior esquerdo (circunferencial)',       NULL, 1, 'circunferencial', NULL, NULL, 27, false, true)
ON CONFLICT (codigo) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Verificação pós-seed (apenas informativa — não bloqueia deploy):
-- SELECT codigo, nome, vista, nivel, pai_codigo, lateralidade, ordem, ativo
--   FROM public.regioes_anatomicas_catalogo
--  WHERE vista = 'circunferencial'
--  ORDER BY ordem;
-- Esperado: 9 linhas, nivel=1, pai_codigo=NULL, lateralidade=false, ativo=true.
--
-- Idempotência: re-executar não insere duplicatas nem falha (ON CONFLICT DO NOTHING).
-- exame_fisico_regioes: não alterada — nenhuma coluna, índice ou constraint nova.
-- ─────────────────────────────────────────────────────────────────────────────
