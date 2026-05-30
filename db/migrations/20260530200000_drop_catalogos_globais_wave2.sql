-- Migration: Drop Catálogos Globais Wave 2
-- Timestamp: 20260530200000
-- Wave 4 (2026-05-30) — tabelas paralelas da Wave 2 eliminadas.
-- Admin passa a operar diretamente nas tabelas legado com EhPadraoSistema=true
-- (live-link nativo — sem cópia, sem importação, sem duplicação de schema).
--
-- Inspeção prévia em dev (2026-05-30):
--   imedto_modelo_prontuario_global  → 2 registros (seeds Wave 2)
--   imedto_variavel_pool_global      → 3 registros (seeds Wave 2)
--   imedto_regiao_anatomica_global   → 15 registros (seeds Wave 2)
--   FKs apontando para essas tabelas → 0 (nenhuma)
--   Cópias em tenants após 2026-05-30 (eh_padrao_sistema=false) → 0
--
-- Política: contagem > 0 são seeds globais (não tenant-owned) → drop puro.
-- Tabelas legado permanecem intactas:
--   modelo_de_prontuario (4 padrão-sistema, 1 do tenant) — OK
--   prontuario_variaveis_pool (0 padrão-sistema, 80 do tenant) — OK
--   regioes_anatomicas_catalogo (144 ativas, 0 inativas) — OK
--
-- Sem BEGIN/COMMIT — a pipeline (deploy/scripts/migrate.sh) gerencia a transação.
-- Rerun é idempotente: DROP IF EXISTS não falha; INSERT usa ON CONFLICT DO NOTHING.
-- Nota: migration registrada manualmente em __ef_migrations_history (SQL pura,
--   sem snapshot EF). O developer deve criar migration EF de sincronização ao
--   remover os domain types C# das 3 entities (W4-CA24).

-- ── Drop: imedto_modelo_prontuario_global ─────────────────────────────────────
-- CASCADE cobre os índices e constraints internas (nenhuma FK externa confirmada).

DROP TABLE IF EXISTS public.imedto_modelo_prontuario_global CASCADE;

-- ── Drop: imedto_variavel_pool_global ─────────────────────────────────────────

DROP TABLE IF EXISTS public.imedto_variavel_pool_global CASCADE;

-- ── Drop: imedto_regiao_anatomica_global ─────────────────────────────────────

DROP TABLE IF EXISTS public.imedto_regiao_anatomica_global CASCADE;

-- ── Registrar migration em __ef_migrations_history ───────────────────────────
-- Registra como migration autônoma (não gerada pelo EF, mas rastreada para
-- que o histórico de aplicações seja auditável e a pipeline não reaplique).

INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260530200000_DropCatalogosGlobaisWave2', '10.0.0')
ON CONFLICT DO NOTHING;
