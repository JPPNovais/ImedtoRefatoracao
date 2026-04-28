-- RLS policies para todas as tabelas de domínio.
-- Princípio: service_role (backend) bypassa RLS; authenticated (JWT direto) só acessa
-- dados do(s) estabelecimento(s) ao qual o usuário pertence (defense-in-depth / LGPD).

-- Habilita RLS em tabelas que não tinham nas migrations originais
ALTER TABLE public.itens_orcamento         ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.movimentacoes_estoque   ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.lancamentos             ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.configuracoes_automacao ENABLE ROW LEVEL SECURITY;

-- ============================================================
-- Função helper: IDs dos estabelecimentos acessíveis ao usuário
-- ============================================================
CREATE OR REPLACE FUNCTION public.meus_estabelecimentos()
RETURNS SETOF bigint
LANGUAGE sql
SECURITY DEFINER   -- roda como owner; evita recursão nas policies
STABLE
SET search_path = public
AS $$
    SELECT id
    FROM public.estabelecimentos
    WHERE dono_usuario_id = auth.uid()
    UNION
    SELECT estabelecimento_id
    FROM public.vinculo_profissional_estabelecimento
    WHERE profissional_usuario_id = auth.uid()
      AND status = 'Ativo'
$$;

REVOKE EXECUTE ON FUNCTION public.meus_estabelecimentos() FROM public;
GRANT  EXECUTE ON FUNCTION public.meus_estabelecimentos() TO authenticated;

-- ============================================================
-- usuarios — somente o próprio registro
-- ============================================================
DROP POLICY IF EXISTS "usuarios_select_proprio" ON public.usuarios;
CREATE POLICY "usuarios_select_proprio" ON public.usuarios
    FOR SELECT TO authenticated
    USING (id = auth.uid());

-- ============================================================
-- profissionais — somente o próprio registro
-- ============================================================
DROP POLICY IF EXISTS "profissionais_select_proprio" ON public.profissionais;
CREATE POLICY "profissionais_select_proprio" ON public.profissionais
    FOR SELECT TO authenticated
    USING (usuario_id = auth.uid());

-- ============================================================
-- estabelecimentos — dono ou membro ativo
-- ============================================================
DROP POLICY IF EXISTS "estabelecimentos_select" ON public.estabelecimentos;
CREATE POLICY "estabelecimentos_select" ON public.estabelecimentos
    FOR SELECT TO authenticated
    USING (id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- modelo_permissao_estabelecimento
-- ============================================================
DROP POLICY IF EXISTS "modelo_permissao_select" ON public.modelo_permissao_estabelecimento;
CREATE POLICY "modelo_permissao_select" ON public.modelo_permissao_estabelecimento
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- vinculo_profissional_estabelecimento
-- ============================================================
DROP POLICY IF EXISTS "vinculo_select" ON public.vinculo_profissional_estabelecimento;
CREATE POLICY "vinculo_select" ON public.vinculo_profissional_estabelecimento
    FOR SELECT TO authenticated
    USING (
        profissional_usuario_id = auth.uid()
        OR estabelecimento_id IN (SELECT public.meus_estabelecimentos())
    );

-- ============================================================
-- pacientes (dados sensíveis — LGPD Art. 5º II)
-- ============================================================
DROP POLICY IF EXISTS "pacientes_select" ON public.pacientes;
CREATE POLICY "pacientes_select" ON public.pacientes
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- modelo_de_prontuario (padrão-sistema visível a todos autenticados)
-- ============================================================
DROP POLICY IF EXISTS "modelo_prontuario_select" ON public.modelo_de_prontuario;
CREATE POLICY "modelo_prontuario_select" ON public.modelo_de_prontuario
    FOR SELECT TO authenticated
    USING (
        eh_padrao_sistema = true
        OR estabelecimento_id IN (SELECT public.meus_estabelecimentos())
    );

-- ============================================================
-- prontuario_variaveis_pool
-- ============================================================
DROP POLICY IF EXISTS "pool_select" ON public.prontuario_variaveis_pool;
CREATE POLICY "pool_select" ON public.prontuario_variaveis_pool
    FOR SELECT TO authenticated
    USING (
        eh_padrao_sistema = true
        OR estabelecimento_id IN (SELECT public.meus_estabelecimentos())
    );

-- ============================================================
-- prontuarios (dados sensíveis de saúde)
-- ============================================================
DROP POLICY IF EXISTS "prontuarios_select" ON public.prontuarios;
CREATE POLICY "prontuarios_select" ON public.prontuarios
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- prontuario_evolucoes (imutáveis)
-- ============================================================
DROP POLICY IF EXISTS "evolucoes_select" ON public.prontuario_evolucoes;
CREATE POLICY "evolucoes_select" ON public.prontuario_evolucoes
    FOR SELECT TO authenticated
    USING (
        prontuario_id IN (
            SELECT id FROM public.prontuarios
            WHERE estabelecimento_id IN (SELECT public.meus_estabelecimentos())
        )
    );

-- ============================================================
-- prontuario_acesso_log (LGPD audit log)
-- ============================================================
DROP POLICY IF EXISTS "acesso_log_select" ON public.prontuario_acesso_log;
CREATE POLICY "acesso_log_select" ON public.prontuario_acesso_log
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- prontuario_anexos
-- ============================================================
DROP POLICY IF EXISTS "anexos_select" ON public.prontuario_anexos;
CREATE POLICY "anexos_select" ON public.prontuario_anexos
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- agendamentos
-- ============================================================
DROP POLICY IF EXISTS "agendamentos_select" ON public.agendamentos;
CREATE POLICY "agendamentos_select" ON public.agendamentos
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- itens_inventario
-- ============================================================
DROP POLICY IF EXISTS "inventario_select" ON public.itens_inventario;
CREATE POLICY "inventario_select" ON public.itens_inventario
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- movimentacoes_estoque
-- ============================================================
DROP POLICY IF EXISTS "movimentacoes_select" ON public.movimentacoes_estoque;
CREATE POLICY "movimentacoes_select" ON public.movimentacoes_estoque
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- orcamentos
-- ============================================================
DROP POLICY IF EXISTS "orcamentos_select" ON public.orcamentos;
CREATE POLICY "orcamentos_select" ON public.orcamentos
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- itens_orcamento (via orcamento)
-- ============================================================
DROP POLICY IF EXISTS "itens_orcamento_select" ON public.itens_orcamento;
CREATE POLICY "itens_orcamento_select" ON public.itens_orcamento
    FOR SELECT TO authenticated
    USING (
        orcamento_id IN (
            SELECT id FROM public.orcamentos
            WHERE estabelecimento_id IN (SELECT public.meus_estabelecimentos())
        )
    );

-- ============================================================
-- lancamentos
-- ============================================================
DROP POLICY IF EXISTS "lancamentos_select" ON public.lancamentos;
CREATE POLICY "lancamentos_select" ON public.lancamentos
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- ============================================================
-- configuracoes_automacao
-- ============================================================
DROP POLICY IF EXISTS "automacao_select" ON public.configuracoes_automacao;
CREATE POLICY "automacao_select" ON public.configuracoes_automacao
    FOR SELECT TO authenticated
    USING (estabelecimento_id IN (SELECT public.meus_estabelecimentos()));

-- Nota: nenhuma policy de escrita é adicionada para authenticated —
-- toda escrita é feita pelo backend via service_role (bypassa RLS).
-- Tentativas de INSERT/UPDATE/DELETE direto via JWT são negadas pelo default deny.
