-- ─────────────────────────────────────────────────────────────────────────────
-- Fusão estrutural do tronco — remoção das 9 regiões antigas (briefing 2026-06-25_002)
--
-- Contexto: Tórax/Abdome/Pelve em anterior, posterior e circunferencial foram
-- fundidos em 3 regiões reais: tronco-anterior, tronco-posterior,
-- tronco-circunferencial. Este arquivo remove as 9 regiões antigas do catálogo.
--
-- SEGURANÇA DE DADO CLÍNICO:
--   Inspeção de 2026-06-25 confirmou ZERO registros em exame_fisico_regioes
--   referenciando esses 9 códigos. Banco em fase pré-uso clínico. Nenhum dado
--   de paciente é perdido — operação é só de catálogo.
--
-- IDEMPOTÊNCIA:
--   DELETE WHERE codigo IN (...) é idempotente por construção:
--   - Se as linhas existem → remove (1ª aplicação).
--   - Se já foram removidas → DELETE afeta 0 linhas, sem erro (2ª+ aplicação).
--   - NÃO usa IF EXISTS (não se aplica a DML) — não é necessário.
--   - NÃO falha se as linhas não existirem — Postgres DELETE é silencioso.
--
-- SUB-REGIÕES: zero filhos dos 9 códigos removidos (confirmado na inspeção).
--   O único nó nível 2 existente é cabeca-anterior-olho (intocado).
--
-- MULTI-TENANT: catálogo global (sem estabelecimento_id). A fusão vale para
--   todos os estabelecimentos simultaneamente por construção.
--
-- ORDEM DE EXECUÇÃO: este arquivo tem timestamp 20260625000001, posterior aos
--   seeds (20260526 e 20260608). A pipeline (migrate.sh) aplica em ordem
--   alfabética/numérica — os seeds rodam primeiro (inserem as 3 novas regiões
--   via ON CONFLICT DO NOTHING), depois este arquivo remove as 9 antigas.
--   Resultado final após qualquer número de re-aplicações:
--     - tronco-anterior, tronco-posterior, tronco-circunferencial PRESENTES
--     - as 9 antigas AUSENTES
--
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- ─────────────────────────────────────────────────────────────────────────────

-- Remove as 9 regiões de tronco antigas (idempotente — DELETE silencioso se ausentes)
DELETE FROM public.regioes_anatomicas_catalogo
WHERE codigo IN (
    -- Vista anterior (3 antigas)
    'torax-anterior',
    'abdome-anterior',
    'pelve-anterior',
    -- Vista posterior (3 antigas, incluindo lombossacra absorvida no tronco-posterior)
    'torax-posterior',
    'lombossacra-posterior',
    'pelve-posterior',
    -- Vista circunferencial (3 antigas)
    'torax-circunferencial',
    'abdome-circunferencial',
    'pelve-circunferencial'
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Verificação pós-fusão (apenas informativa — não bloqueia deploy):
--
-- CA1 — 3 regiões de tronco presentes:
-- SELECT codigo, nome, vista, nivel, pai_codigo, lateralidade, ativo
--   FROM public.regioes_anatomicas_catalogo
--  WHERE codigo IN ('tronco-anterior','tronco-posterior','tronco-circunferencial');
-- Esperado: 3 linhas, nivel=1, pai_codigo=NULL, lateralidade=false, ativo=true.
--
-- CA2 — 9 antigas ausentes:
-- SELECT codigo FROM public.regioes_anatomicas_catalogo
--  WHERE codigo IN (
--    'torax-anterior','abdome-anterior','pelve-anterior',
--    'torax-posterior','lombossacra-posterior','pelve-posterior',
--    'torax-circunferencial','abdome-circunferencial','pelve-circunferencial'
--  );
-- Esperado: 0 linhas.
--
-- CA3 — contagem total após fusão:
-- SELECT nivel, COUNT(*) FROM public.regioes_anatomicas_catalogo GROUP BY nivel ORDER BY nivel;
-- Esperado: nivel 1 = 21 (7 anterior + 7 posterior + 7 circunferencial),
--           nivel 2 = 1  (cabeca-anterior-olho, intocada).
-- ─────────────────────────────────────────────────────────────────────────────
