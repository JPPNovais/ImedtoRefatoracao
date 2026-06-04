-- Seed idempotente: 3 registros globais de modelo de permissão padrão do sistema.
-- estabelecimento_id = NULL → escopo global (fonte/template para propagação).
-- eh_padrao = true → bloqueado para edição pelo tenant (regra R7 do domínio).
--
-- Valores extraídos de:
--   ModeloPermissaoEstabelecimento.CriarPadroes() em Domain/ModelosPermissao/
--   CatalogoPermissoes.AdminPadrao / MedicoPadrao / RecepcaoPadrao
--   PermissoesExtras (constantes)
--
-- Idempotência: WHERE NOT EXISTS filtrando por nome no escopo global.
-- Reexecutar não duplica registros.
--
-- Multi-tenant: registros com estabelecimento_id IS NULL nunca são retornados por
-- queries de tenant que filtram estabelecimento_id = @X (NULL nunca casa = qualquer valor).

-- Admin: acesso total (CatalogoPermissoes.Todas — todas as combinações area.acao)
INSERT INTO public.modelo_permissao_estabelecimento
    (estabelecimento_id, nome, tipo_acesso, permissoes, permissoes_extras,
     icone, cor, descricao, eh_padrao, criado_em, atualizado_em)
SELECT
    NULL,
    'Admin',
    'Profissional',
    '["agenda.ver","agenda.criar","agenda.editar","agenda.excluir","prontuario.ver","prontuario.editar","prontuario.assinar","prescricao.criar","prescricao.assinar","pacientes.ver","pacientes.criar","pacientes.editar","pacientes.excluir","financeiro.ver","financeiro.lancar","financeiro.fechar","orcamento.ver","orcamento.criar","orcamento.editar","orcamento.aprovar","orcamento.configurar","convenios.ver","convenios.gerenciar","estoque.ver","estoque.gerenciar","relatorios.ver","relatorios.exportar","configuracoes.gerenciar","equipe.ver","equipe.convidar","equipe.permissoes","equipe.remover","termos.emitir","termos.gerenciar_modelos"]'::jsonb,
    '["ia_assistente_clinico","gerir_permissoes","config_estabelecimento","gerir_profissionais","modelos_prontuario","automacao_config"]'::jsonb,
    'fa-crown',
    'hsl(280 60% 50%)',
    'Acesso total — recomendado para o dono da clínica',
    true,
    NOW(),
    NULL
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_permissao_estabelecimento
    WHERE nome = 'Admin' AND estabelecimento_id IS NULL
);

-- Médico: agenda + prontuário + receitas + leitura/edição de pacientes
INSERT INTO public.modelo_permissao_estabelecimento
    (estabelecimento_id, nome, tipo_acesso, permissoes, permissoes_extras,
     icone, cor, descricao, eh_padrao, criado_em, atualizado_em)
SELECT
    NULL,
    'Médico',
    'Profissional',
    '["agenda.ver","agenda.criar","agenda.editar","agenda.excluir","prontuario.ver","prontuario.editar","prontuario.assinar","prescricao.criar","prescricao.assinar","pacientes.ver","pacientes.criar","pacientes.editar","orcamento.ver","orcamento.criar","orcamento.editar","relatorios.ver","termos.emitir"]'::jsonb,
    '["ia_assistente_clinico","modelos_prontuario"]'::jsonb,
    'fa-user-doctor',
    'hsl(254 56% 38%)',
    'Profissional de saúde com agenda e prontuário',
    true,
    NOW(),
    NULL
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_permissao_estabelecimento
    WHERE nome = 'Médico' AND estabelecimento_id IS NULL
);

-- Recepção: atendimento, agenda e cadastro de pacientes
INSERT INTO public.modelo_permissao_estabelecimento
    (estabelecimento_id, nome, tipo_acesso, permissoes, permissoes_extras,
     icone, cor, descricao, eh_padrao, criado_em, atualizado_em)
SELECT
    NULL,
    'Recepção',
    'Recepcionista',
    '["agenda.ver","agenda.criar","agenda.editar","agenda.excluir","pacientes.ver","pacientes.criar","pacientes.editar","prontuario.ver","convenios.ver","financeiro.ver","termos.emitir"]'::jsonb,
    '[]'::jsonb,
    'fa-headset',
    'hsl(40 80% 50%)',
    'Atendimento, agenda e cadastro de pacientes',
    true,
    NOW(),
    NULL
WHERE NOT EXISTS (
    SELECT 1 FROM public.modelo_permissao_estabelecimento
    WHERE nome = 'Recepção' AND estabelecimento_id IS NULL
);
