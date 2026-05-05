-- =========================================================================
-- Índices compostos multi-tenant + cobertura de FKs órfãs.
-- Trade-offs documentados no header de cada índice.
-- Idempotente (CREATE INDEX IF NOT EXISTS) — Supabase CLI gerencia transação.
-- NÃO usa CREATE INDEX CONCURRENTLY: incompatível com migration transacional
--   do supabase db push. Em produção com tabela > 1M linhas, aplicar em
--   janela de manutenção via psql direto + CONCURRENTLY (fora desta migration).
-- =========================================================================

-- 1) Agenda por estabelecimento + profissional + janela ----------------------
-- Cobre: agenda do profissional por dia/semana num estabelecimento.
-- Partial WHERE excluindo Cancelado para reduzir tamanho.
CREATE INDEX IF NOT EXISTS ix_agendamentos_estab_prof_inicio
    ON public.agendamentos (estabelecimento_id, profissional_usuario_id, inicio_previsto)
    WHERE status <> 'Cancelado';

-- 2) Prontuário: lookup por tenant + paciente --------------------------------
-- O UQ existente (paciente_id, estabelecimento_id) não cobre WHERE estabelecimento_id=?.
-- Este índice cobre tanto a busca multi-tenant quanto o GET por paciente.
CREATE INDEX IF NOT EXISTS ix_prontuarios_estab_paciente
    ON public.prontuarios (estabelecimento_id, paciente_id)
    WHERE deletado_em IS NULL;

-- 3) Anexos do prontuário por tenant -----------------------------------------
-- Cobre query LGPD/listagem de anexos por estabelecimento e fecha o FK
-- fk_anexo_estabelecimento sem índice (advisor).
CREATE INDEX IF NOT EXISTS ix_prontuario_anexos_estab_criado
    ON public.prontuario_anexos (estabelecimento_id, criado_em DESC)
    WHERE arquivado_em IS NULL AND deletado_em IS NULL;

-- 4) Auditoria de acesso a prontuário por tenant -----------------------------
-- Cobre relatório LGPD "quem acessou prontuários neste estabelecimento entre
-- A e B". Sem este índice, auditoria multi-tenant faz seq scan.
CREATE INDEX IF NOT EXISTS ix_prontuario_acesso_log_estab_data
    ON public.prontuario_acesso_log (estabelecimento_id, ocorrido_em DESC);

-- 5) FKs sem índice — risco de lock cascade no DELETE da tabela pai ----------

-- ai_audit_logs (3 FKs)
CREATE INDEX IF NOT EXISTS ix_ai_audit_logs_evolucao
    ON public.ai_audit_logs (evolucao_id) WHERE evolucao_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_ai_audit_logs_paciente
    ON public.ai_audit_logs (paciente_id) WHERE paciente_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_ai_audit_logs_prontuario
    ON public.ai_audit_logs (prontuario_id) WHERE prontuario_id IS NOT NULL;

-- prontuario_anexos (1 FK adicional além do já coberto pelo ix_prontuario_anexos_estab_criado)
CREATE INDEX IF NOT EXISTS ix_prontuario_anexos_autor
    ON public.prontuario_anexos (criado_por_usuario_id);

-- prontuario_evolucoes (1 FK)
CREATE INDEX IF NOT EXISTS ix_prontuario_evolucoes_autor
    ON public.prontuario_evolucoes (autor_usuario_id);

-- prontuarios (1 FK adicional além do já coberto pelo ix_prontuarios_estab_paciente)
CREATE INDEX IF NOT EXISTS ix_prontuarios_modelo
    ON public.prontuarios (modelo_de_prontuario_id) WHERE modelo_de_prontuario_id IS NOT NULL;

-- lista_espera_agendamento (2 FKs)
CREATE INDEX IF NOT EXISTS ix_lista_espera_paciente
    ON public.lista_espera_agendamento (paciente_id);
CREATE INDEX IF NOT EXISTS ix_lista_espera_agendamento_atendido
    ON public.lista_espera_agendamento (atendido_por_agendamento_id)
    WHERE atendido_por_agendamento_id IS NOT NULL;

-- vinculo_profissional_estabelecimento (2 FKs)
CREATE INDEX IF NOT EXISTS ix_vinculo_convidado_por
    ON public.vinculo_profissional_estabelecimento (convidado_por_usuario_id)
    WHERE convidado_por_usuario_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_vinculo_modelo_permissao
    ON public.vinculo_profissional_estabelecimento (modelo_permissao_id)
    WHERE modelo_permissao_id IS NOT NULL;

-- =========================================================================
-- Pendências NÃO endereçadas aqui (tickets separados):
--  * 36 RLS policies com auth.uid()/auth.role() não-cacheado (auth_rls_initplan).
--  * Drop de índices "unused" só após coleta com dados reais (>30 dias prod).
--  * RLS desabilitado em: itens_inventario, orcamentos, paciente_acesso_log
--    (alinhar com time antes de produção).
-- =========================================================================
