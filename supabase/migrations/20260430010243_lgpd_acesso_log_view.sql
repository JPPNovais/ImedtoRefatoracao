-- LGPD — VIEW lgpd_acesso_log apontando para prontuario_acesso_log.
--
-- Justificativa: Art. 46 LGPD exige audit trail de acesso a dados pessoais.
-- A tabela prontuario_acesso_log já registra cada leitura/escrita sensível
-- (impl. via IProntuarioAcessoLogService desde a Fase 1). Esta VIEW expõe os
-- mesmos dados sob o nome canonico "lgpd_acesso_log" para alinhar com a
-- nomenclatura do legado e facilitar consultas LGPD/compliance.
--
-- Sem par EF — SQL puro. service_role acessa para auditoria; RLS bloqueia
-- authenticated/anon (audit interno).

CREATE OR REPLACE VIEW public.lgpd_acesso_log AS
SELECT
    id,
    usuario_id,
    estabelecimento_id,
    'prontuarios'::text     AS tabela,
    prontuario_id           AS registro_id,
    tipo_acesso,
    ocorrido_em             AS acessado_em
FROM public.prontuario_acesso_log;

COMMENT ON VIEW public.lgpd_acesso_log IS
    'Audit trail de acesso a dados pessoais (Art. 46 LGPD). View sobre prontuario_acesso_log com nomenclatura canônica.';

-- Permissions: a VIEW herda RLS da tabela base. Negar acesso direto para
-- authenticated/anon — apenas service_role (backend) consulta para auditoria.
REVOKE ALL ON public.lgpd_acesso_log FROM authenticated, anon;
