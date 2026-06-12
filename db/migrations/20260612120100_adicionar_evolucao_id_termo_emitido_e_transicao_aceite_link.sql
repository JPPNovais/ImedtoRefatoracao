-- =============================================================================
-- Migration: 20260612120100_adicionar_evolucao_id_termo_emitido_e_transicao_aceite_link
-- Briefing:  2026-06-12_002 — Termo de consentimento físico-primeiro
-- Aplicado:  pela pipeline CI/CD via deploy/scripts/migrate.sh (psql)
-- Transação: gerenciada externamente — NÃO adicionar BEGIN/COMMIT aqui
-- =============================================================================

-- ── Guard de idempotência EF ──────────────────────────────────────────────────
-- A pipeline aplica este arquivo após a migration EF já ter sido registrada
-- em __ef_migrations_history. Verificações individuais abaixo são suficientes.

-- ── Necessidade 1: coluna evolucao_id em termo_emitido ───────────────────────

ALTER TABLE public.termo_emitido
    ADD COLUMN IF NOT EXISTS evolucao_id bigint NULL;

-- FK para prontuario_evolucoes (ON DELETE SET NULL — termo não perde dados se
-- a evolução for deletada; o vínculo é perdido, o termo permanece).
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_termo_emitido_evolucao'
          AND conrelid = 'public.termo_emitido'::regclass
    ) THEN
        ALTER TABLE public.termo_emitido
            ADD CONSTRAINT fk_termo_emitido_evolucao
            FOREIGN KEY (evolucao_id)
            REFERENCES public.prontuario_evolucoes (id)
            ON DELETE SET NULL;
    END IF;
END $$;

-- Índice simples em evolucao_id — separado em arquivo _indices.sql para
-- aplicação com CONCURRENTLY em produção (não pode estar em transação).
-- Em dev/stage o índice abaixo (sem CONCURRENTLY) é suficiente:
--   CREATE INDEX IF NOT EXISTS ix_termo_emitido_evolucao_id
--       ON public.termo_emitido (evolucao_id);
-- Ver: 20260612120100_adicionar_evolucao_id_termo_emitido_e_transicao_aceite_link_indices.sql

-- ── Necessidade 2: transição AceiteLink → Expirado ────────────────────────────
-- Idempotente: WHERE duplo garante que rodar 2x não altera mais nada.

UPDATE public.termo_emitido
SET    status        = 'Expirado',
       atualizado_em = now()
WHERE  assinatura_tipo = 'AceiteLink'
  AND  status          = 'Pendente';

-- Limpeza de tokens mortos em registros já finalizados (LGPD — minimização de dados).
-- Idempotente: apenas registros com token_aceite IS NOT NULL são afetados.

UPDATE public.termo_emitido
SET    token_aceite    = NULL,
       token_expira_em = NULL,
       atualizado_em   = now()
WHERE  assinatura_tipo = 'AceiteLink'
  AND  token_aceite    IS NOT NULL
  AND  status          IN ('Expirado', 'Assinado', 'Recusado', 'Revogado');
