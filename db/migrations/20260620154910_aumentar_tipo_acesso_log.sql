-- Aumenta varchar(20) → varchar(50) em paciente_acesso_log.tipo_acesso.
-- Necessário porque o valor 'RevelacaoDadosSensiveis' tem 23 caracteres e causava 500
-- ao tentar gravar o audit de acesso a dados sensíveis do paciente.
-- Idempotente: ALTER TYPE é seguro de re-executar (Postgres ignora se já é varchar(50) ou maior).
ALTER TABLE public.paciente_acesso_log
    ALTER COLUMN tipo_acesso TYPE varchar(50);
