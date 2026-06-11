-- Migration: 20260611145045_criar_financeiro_export_log_audit_ca10
-- CA10 (briefing 2026-06-11_002): tabela de audit LGPD append-only para exports do extrato financeiro.
-- Grava: quem exportou (usuario_id), qual tenant (estabelecimento_id), período e contagem de linhas.
-- Sem PII: nenhum dado de paciente. Best-effort via Dapper em ConsolidacaoFinanceiraQueryRepository.
-- BEGIN/COMMIT removidos — a pipeline (migrate.sh) gerencia a transação externamente.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611145045_CriarFinanceiroExportLogAuditCA10') THEN
    CREATE TABLE public.financeiro_export_log (
        id                  bigint GENERATED ALWAYS AS IDENTITY,
        usuario_id          uuid                        NOT NULL,
        estabelecimento_id  bigint                      NOT NULL,
        acao                text                        NOT NULL,
        periodo_inicio      timestamp with time zone    NOT NULL,
        periodo_fim         timestamp with time zone    NOT NULL,
        total_linhas        integer                     NOT NULL,
        ocorrido_em         timestamp with time zone    NOT NULL,
        CONSTRAINT "PK_financeiro_export_log" PRIMARY KEY (id),
        CONSTRAINT fk_financeiro_export_log_estabelecimento
            FOREIGN KEY (estabelecimento_id)
            REFERENCES public.estabelecimentos (id)
            ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611145045_CriarFinanceiroExportLogAuditCA10') THEN
    CREATE INDEX ix_financeiro_export_log_estabelecimento_data
        ON public.financeiro_export_log (estabelecimento_id, ocorrido_em);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260611145045_CriarFinanceiroExportLogAuditCA10') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260611145045_CriarFinanceiroExportLogAuditCA10', '10.0.0');
    END IF;
END $EF$;
