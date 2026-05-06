-- =============================================================================
-- Migration: modelo_permissao_estabelecimento — colunas visuais + ações granulares
-- =============================================================================
-- Adiciona campos icone/cor/descricao ao modelo de permissão (usados pelo front
-- para decorar o papel — ícone FontAwesome, cor HSL/hex, descrição curta) e
-- migra as permissões dos modelos padrão (Admin/Médico/Recepção) para o formato
-- granular `area.acao` alinhado ao novo design.
--
-- Modelos personalizados (eh_padrao = false) NÃO têm suas permissões alteradas
-- — o domínio aceita ambos os formatos (legacy `agenda` e novo `agenda.ver`),
-- e o frontend faz a leitura compatível durante a transição.
-- =============================================================================

-- ── 1. Colunas visuais ─────────────────────────────────────────────────────
ALTER TABLE public.modelo_permissao_estabelecimento
    ADD COLUMN IF NOT EXISTS icone     TEXT,
    ADD COLUMN IF NOT EXISTS cor       TEXT,
    ADD COLUMN IF NOT EXISTS descricao TEXT;

-- Constraints de tamanho (espelham as do domain).
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_modelo_permissao_icone_len'
    ) THEN
        ALTER TABLE public.modelo_permissao_estabelecimento
            ADD CONSTRAINT ck_modelo_permissao_icone_len     CHECK (icone     IS NULL OR length(icone)     <= 50);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_modelo_permissao_cor_len'
    ) THEN
        ALTER TABLE public.modelo_permissao_estabelecimento
            ADD CONSTRAINT ck_modelo_permissao_cor_len       CHECK (cor       IS NULL OR length(cor)       <= 40);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_modelo_permissao_descricao_len'
    ) THEN
        ALTER TABLE public.modelo_permissao_estabelecimento
            ADD CONSTRAINT ck_modelo_permissao_descricao_len CHECK (descricao IS NULL OR length(descricao) <= 200);
    END IF;
END $$;

-- ── 2. Backfill visual dos modelos padrão ─────────────────────────────────
UPDATE public.modelo_permissao_estabelecimento
SET icone     = 'fa-crown',
    cor       = 'hsl(280 60% 50%)',
    descricao = 'Acesso total — recomendado para o dono da clínica'
WHERE eh_padrao = TRUE AND nome = 'Admin' AND icone IS NULL;

UPDATE public.modelo_permissao_estabelecimento
SET icone     = 'fa-user-doctor',
    cor       = 'hsl(254 56% 38%)',
    descricao = 'Profissional de saúde com agenda e prontuário'
WHERE eh_padrao = TRUE AND nome = 'Médico' AND icone IS NULL;

UPDATE public.modelo_permissao_estabelecimento
SET icone     = 'fa-headset',
    cor       = 'hsl(40 80% 50%)',
    descricao = 'Atendimento, agenda e cadastro de pacientes'
WHERE eh_padrao = TRUE AND nome = 'Recepção' AND icone IS NULL;

-- ── 3. Migrar permissões dos modelos padrão para o formato `area.acao` ────
-- Admin: acesso total ao catálogo novo.
UPDATE public.modelo_permissao_estabelecimento
SET permissoes = jsonb_build_array(
    'agenda.ver','agenda.criar','agenda.editar','agenda.excluir',
    'prontuario.ver','prontuario.editar','prontuario.assinar',
    'prescricao.criar','prescricao.assinar',
    'pacientes.ver','pacientes.criar','pacientes.editar','pacientes.excluir',
    'financeiro.ver','financeiro.lancar','financeiro.fechar',
    'orcamento.ver','orcamento.criar','orcamento.editar',
    'convenios.ver','convenios.gerenciar',
    'estoque.ver','estoque.gerenciar',
    'relatorios.ver','relatorios.exportar',
    'configuracoes.gerenciar',
    'equipe.ver','equipe.convidar','equipe.permissoes','equipe.remover'
)
WHERE eh_padrao = TRUE AND nome = 'Admin';

-- Médico: agenda + prontuário + receitas + leitura/edição de pacientes + orçamentos básicos.
UPDATE public.modelo_permissao_estabelecimento
SET permissoes = jsonb_build_array(
    'agenda.ver','agenda.criar','agenda.editar','agenda.excluir',
    'prontuario.ver','prontuario.editar','prontuario.assinar',
    'prescricao.criar','prescricao.assinar',
    'pacientes.ver','pacientes.criar','pacientes.editar',
    'orcamento.ver','orcamento.criar',
    'relatorios.ver'
)
WHERE eh_padrao = TRUE AND nome = 'Médico';

-- Recepção: agenda + cadastro de pacientes + leitura de prontuário/financeiro/convênios.
UPDATE public.modelo_permissao_estabelecimento
SET permissoes = jsonb_build_array(
    'agenda.ver','agenda.criar','agenda.editar','agenda.excluir',
    'pacientes.ver','pacientes.criar','pacientes.editar',
    'prontuario.ver',
    'convenios.ver',
    'financeiro.ver'
)
WHERE eh_padrao = TRUE AND nome = 'Recepção';
