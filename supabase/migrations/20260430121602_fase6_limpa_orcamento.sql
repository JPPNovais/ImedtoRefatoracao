-- ============================================================
-- Fase 6.0 — Limpeza de divergências do módulo de orçamentos.
--
-- Mudanças:
--   1. DROP COLUMN tipo (era 'Simples'|'Cirurgico'). Aggregate é único agora.
--   2. DROP COLUMN config_pagamento_json. Descontos/acréscimos vivem por forma de
--      pagamento (orcamento_formas_pagamento.acrescimo_percentual / entrada_percentual).
--   3. UPDATE de status: 'Pendente' → 'Enviado' (semântica equivalente — era o estado
--      em que o orçamento aguardava decisão). 'Rascunho' é novo estado inicial pré-envio.
--   4. CHECK constraint atualizado para os 6 status do legado:
--      Rascunho / Enviado / Aprovado / Recusado / Cancelado / Expirado.
--
-- Idempotente: usa DO $EF$ blocks gerados pelo `dotnet ef migrations script --idempotent`
-- e pula se a migration já foi aplicada.
-- ============================================================

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260430121602_Fase6LimpaOrcamento') THEN
    ALTER TABLE public.orcamentos DROP COLUMN IF EXISTS config_pagamento_json;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260430121602_Fase6LimpaOrcamento') THEN
    ALTER TABLE public.orcamentos DROP COLUMN IF EXISTS tipo;
    END IF;
END $EF$;

-- Mapeia status antigos para o novo conjunto (idempotente — só aplica se ainda houver
-- linhas com 'Pendente').
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260430121602_Fase6LimpaOrcamento') THEN
    UPDATE public.orcamentos SET status = 'Enviado' WHERE status = 'Pendente';
    END IF;
END $EF$;

-- CHECK constraint do conjunto de status novo. Drop seguro do antigo se existir.
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260430121602_Fase6LimpaOrcamento') THEN
    ALTER TABLE public.orcamentos DROP CONSTRAINT IF EXISTS chk_orcamento_status;
    ALTER TABLE public.orcamentos ADD CONSTRAINT chk_orcamento_status
        CHECK (status IN ('Rascunho','Enviado','Aprovado','Recusado','Cancelado','Expirado'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260430121602_Fase6LimpaOrcamento') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260430121602_Fase6LimpaOrcamento', '10.0.0');
    END IF;
END $EF$;
