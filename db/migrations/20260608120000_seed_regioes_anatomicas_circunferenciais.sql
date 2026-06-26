-- ─────────────────────────────────────────────────────────────────────────────
-- Seed — 7 nós de vista circunferencial (briefing 2026-06-08_005 B1)
--
-- Decisão 2026-06-25 (briefing 002, fusão estrutural do tronco): Tórax, Abdome
-- e Pelve circunferenciais foram FUNDIDOS em "tronco-circunferencial". A exceção
-- clínica abdome-circunferencial→lombossacra-posterior deixa de existir: o tronco
-- circunferencial resolve simétricamente para tronco-anterior + tronco-posterior.
-- As 3 antigas (torax/abdome/pelve-circunferencial) são removidas pelo arquivo
-- 20260625000001_fusao_tronco_remover_regioes_antigas.sql (que roda depois).
-- Este seed NÃO as insere — numa instalação limpa elas nunca surgem.
--
-- Sub-regiões (nível 2/3) NÃO são seedadas — gerenciadas manualmente.
-- Campo vista: character varying(20) — texto livre sem CHECK constraint.
-- Idempotente: ON CONFLICT (codigo) DO NOTHING — seguro para re-runs.
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- ─────────────────────────────────────────────────────────────────────────────

-- ── NÍVEL 1 — Vista CIRCUNFERENCIAL (7 nós) ──────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, svg_coords, ordem, lateralidade, ativo)
VALUES
    -- Cabeça circunferencial: une cabeca-anterior + cabeca-posterior
    ('cabeca-circunferencial',                        'Cabeça (circunferencial)',                         NULL, 1, 'circunferencial', NULL, NULL, 15, false, true),
    -- Pescoço circunferencial: une pescoco-anterior + pescoco-posterior
    ('pescoco-circunferencial',                       'Pescoço (circunferencial)',                        NULL, 1, 'circunferencial', NULL, NULL, 16, false, true),
    -- Tronco circunferencial: une tronco-anterior + tronco-posterior (simétrico — sem exceção clínica)
    ('tronco-circunferencial',                        'Tronco (circunferencial)',                         NULL, 1, 'circunferencial', NULL, NULL, 17, false, true),
    -- Membros superiores
    ('membro-superior-direito-circunferencial',       'Membro superior direito (circunferencial)',        NULL, 1, 'circunferencial', NULL, NULL, 18, false, true),
    ('membro-superior-esquerdo-circunferencial',      'Membro superior esquerdo (circunferencial)',       NULL, 1, 'circunferencial', NULL, NULL, 19, false, true),
    -- Membros inferiores
    ('membro-inferior-direito-circunferencial',       'Membro inferior direito (circunferencial)',        NULL, 1, 'circunferencial', NULL, NULL, 20, false, true),
    ('membro-inferior-esquerdo-circunferencial',      'Membro inferior esquerdo (circunferencial)',       NULL, 1, 'circunferencial', NULL, NULL, 21, false, true)
ON CONFLICT (codigo) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Verificação pós-seed (apenas informativa — não bloqueia deploy):
-- SELECT codigo, nome, vista, nivel, pai_codigo, lateralidade, ordem, ativo
--   FROM public.regioes_anatomicas_catalogo
--  WHERE vista = 'circunferencial'
--  ORDER BY ordem;
-- Esperado: 7 linhas, nivel=1, pai_codigo=NULL, lateralidade=false, ativo=true.
-- (cabeca, pescoco, tronco, 2 membros superiores, 2 membros inferiores)
--
-- Idempotência: re-executar não insere duplicatas nem falha (ON CONFLICT DO NOTHING).
-- As 3 antigas (torax/abdome/pelve-circunferencial) são removidas pelo arquivo
-- 20260625000001_fusao_tronco_remover_regioes_antigas.sql.
-- ─────────────────────────────────────────────────────────────────────────────
