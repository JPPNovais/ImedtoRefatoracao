-- Migration: adicionar_responsavel_paciente
-- Briefing: 2026-06-23_002 (Tags etárias e responsável do paciente)
-- Tabela: public.pacientes — 3 colunas novas, todas nullable.
-- LGPD: PII de terceiro (responsável). Expor apenas em PacienteDto (detalhe),
--       nunca em PacienteListaItemDto / PacienteBuscaRapidaDto (R7).
--       Anonimização zera os três campos junto com o titular (R8).
-- Multi-tenant: herdado da linha do paciente (estabelecimento_id já na tabela).
-- Sem índice novo: campos não são chave de busca.
-- Idempotente via IF NOT EXISTS no DDL e guard __ef_migrations_history.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260623170756_AdicionarResponsavelPaciente') THEN
    ALTER TABLE public.pacientes
        ADD COLUMN IF NOT EXISTS responsavel_nome        character varying(200) NULL,
        ADD COLUMN IF NOT EXISTS responsavel_parentesco  character varying(40)  NULL,
        ADD COLUMN IF NOT EXISTS responsavel_telefone    character varying(20)  NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "MigrationId" = '20260623170756_AdicionarResponsavelPaciente') THEN
    INSERT INTO public.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260623170756_AdicionarResponsavelPaciente', '10.0.0');
    END IF;
END $EF$;
