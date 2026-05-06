-- =============================================================================
-- Migration: pacientes — tags clínicas e alertas
-- =============================================================================
-- Adiciona dois arrays de texto à tabela `pacientes`:
--
--  - `tags`     → tags clínicas/operacionais curtas (ex: "vip", "gestante",
--                 "cronico"). Usadas para filtros e badges na lista.
--  - `alertas`  → alertas clínicos críticos (ex: "Alergia grave a penicilina")
--                 exibidos em destaque vermelho no detalhe do paciente.
--
-- LGPD: alertas armazenam dado de saúde (Art. 5º II). O acesso já é controlado
-- pelo isolamento multi-tenant (estabelecimento_id) + RLS + permissão por
-- prontuário. Não há vazamento adicional.
--
-- Índice GIN em `tags` para suportar filtros eficientes na lista
-- (ex: WHERE tags && ARRAY['gestante']).
-- =============================================================================

ALTER TABLE public.pacientes
    ADD COLUMN IF NOT EXISTS tags     TEXT[] NOT NULL DEFAULT ARRAY[]::text[],
    ADD COLUMN IF NOT EXISTS alertas  TEXT[] NOT NULL DEFAULT ARRAY[]::text[];

-- Constraints de tamanho — espelham os limites do domain (Paciente.cs).
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_pacientes_tags_quantidade'
    ) THEN
        ALTER TABLE public.pacientes
            ADD CONSTRAINT ck_pacientes_tags_quantidade
            CHECK (cardinality(tags) <= 10);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_pacientes_alertas_quantidade'
    ) THEN
        ALTER TABLE public.pacientes
            ADD CONSTRAINT ck_pacientes_alertas_quantidade
            CHECK (cardinality(alertas) <= 10);
    END IF;
END $$;

-- Índice GIN em tags para suportar filtros (tags && ARRAY['x']) eficientemente.
CREATE INDEX IF NOT EXISTS ix_pacientes_tags_gin
    ON public.pacientes USING gin (tags);
