import { createRouter, createWebHistory } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { useAssinaturaStore } from "@/stores/assinaturaStore"

/**
 * Rotas isentas do bloqueio por assinatura — necessárias para o fluxo de
 * upgrade/contato/logout. Espelha a lógica do legado em router/index.ts.
 */
const ROTAS_ISENTAS_ASSINATURA = new Set([
    "Login",
    "Landing",
    "Onboarding",
    "AssinaturaExpirada",
    "Planos",
    "MinhaAssinatura",
])

const APP = { layout: "app" } as const

const router = createRouter({
    history: createWebHistory(),
    routes: [
        // Públicas (sem layout)
        {
            path: "/",
            name: "Landing",
            component: () => import("@/views/LandingView.vue"),
        },
        {
            path: "/login",
            name: "Login",
            component: () => import("@/views/auth/LoginView.vue"),
        },
        {
            path: "/auth/confirmar-email",
            name: "ConfirmarEmail",
            component: () => import("@/views/auth/ConfirmarEmailView.vue"),
        },
        {
            path: "/auth/redefinir-senha",
            name: "RedefinirSenha",
            component: () => import("@/views/auth/RedefinirSenhaView.vue"),
        },
        {
            path: "/auth/aceitar-convite",
            name: "AceitarConvite",
            component: () => import("@/views/auth/AceitarConviteView.vue"),
        },
        {
            path: "/privacidade",
            name: "Privacidade",
            component: () => import("@/views/legal/PrivacidadeView.vue"),
        },
        {
            path: "/termos",
            name: "Termos",
            component: () => import("@/views/legal/TermosView.vue"),
        },

        // Pós-login sem tenant (sem layout)
        {
            path: "/onboarding",
            name: "Onboarding",
            component: () => import("@/views/auth/OnboardingView.vue"),
            meta: { requiresAuth: true },
        },
        // Com tenant (com sidebar)
        {
            path: "/home",
            name: "Home",
            component: () => import("@/views/HomeView.vue"),
            // Sem requiresTenant: HomeView renderiza modo "sem vínculo" quando
            // tenant.semEstabelecimento, com CTA para convites e perfil pessoal.
            meta: { requiresAuth: true, ...APP },
        },
        {
            path: "/minha-conta",
            name: "MinhaConta",
            component: () => import("@/views/minhaConta/MinhaContaView.vue"),
            meta: { requiresAuth: true, ...APP },
        },
        {
            path: "/minha-conta/lgpd",
            name: "MinhaContaLgpd",
            component: () => import("@/views/minhaConta/MinhaContaLgpdView.vue"),
            meta: { requiresAuth: true, ...APP },
        },

        // Configurações (apenas Dono)
        // Rota legada — redireciona para a tela unificada Equipe na aba "papeis".
        {
            path: "/configuracoes/modelos-permissao",
            name: "ModelosPermissao",
            redirect: { name: "Equipe", query: { aba: "papeis" } },
        },
        {
            path: "/configuracoes/modelos-prontuario",
            name: "ModelosProntuario",
            component: () => import("@/views/configuracoes/ModelosProntuarioView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Equipe e permissões — tela unificada (apenas Dono).
        {
            path: "/equipe",
            name: "Equipe",
            component: () => import("@/views/equipe/EquipeView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        // Rota legada — preserva deep-links para `/profissionais`.
        {
            path: "/profissionais",
            name: "Profissionais",
            redirect: { name: "Equipe" },
        },

        // Estabelecimento (edição)
        {
            path: "/estabelecimento",
            name: "Estabelecimento",
            component: () => import("@/views/estabelecimento/EstabelecimentoView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Meus convites (para qualquer usuário)
        {
            path: "/meus-convites",
            name: "MeusConvites",
            component: () => import("@/views/profissionais/MeusConvitesView.vue"),
            meta: { requiresAuth: true, ...APP },
        },

        // Automações
        {
            path: "/automacoes",
            name: "Automacoes",
            component: () => import("@/views/automacoes/AutomacoesView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Assinatura
        {
            path: "/minha-assinatura",
            name: "MinhaAssinatura",
            component: () => import("@/views/assinatura/MinhaAssinaturaView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/planos",
            name: "Planos",
            component: () => import("@/views/assinatura/PlanosView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/assinatura-expirada",
            name: "AssinaturaExpirada",
            component: () => import("@/views/assinatura/AssinaturaExpiradaView.vue"),
            meta: { requiresAuth: true, requiresTenant: true },
        },

        // Configurações IA (apenas Dono)
        {
            path: "/configuracoes/ia",
            name: "IaSettings",
            component: () => import("@/views/configuracoes/MinhaIaSettingsView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Notificações full-page
        {
            path: "/notificacoes",
            name: "Notificacoes",
            component: () => import("@/views/notificacoes/NotificacoesView.vue"),
            meta: { requiresAuth: true, ...APP },
        },

        // Relatórios — view única com tabs, deep-link via ?aba=
        {
            path: "/relatorios",
            name: "Relatorios",
            component: () => import("@/views/relatorios/RelatoriosView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        // Aliases das rotas antigas → redirecionam para /relatorios?aba=...
        {
            path: "/relatorios/financeiro",
            name: "RelatorioFinanceiro",
            redirect: { path: "/relatorios", query: { aba: "financeiro" } },
        },
        {
            path: "/relatorios/operacional",
            name: "RelatorioOperacional",
            redirect: { path: "/relatorios", query: { aba: "agenda" } },
        },
        {
            path: "/relatorios/pessoas",
            name: "RelatorioPessoas",
            redirect: { path: "/relatorios", query: { aba: "pessoas" } },
        },
        {
            path: "/relatorios/orcamentos",
            name: "RelatorioOrcamentos",
            redirect: { path: "/relatorios", query: { aba: "orcamentos" } },
        },

        // Financeiro
        {
            path: "/financeiro",
            name: "Financeiro",
            component: () => import("@/views/financeiro/FinanceiroView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/financeiro/categorias",
            name: "CategoriasFinanceiras",
            component: () => import("@/views/financeiro/CategoriasFinanceirasView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/financeiro/formas-pagamento",
            name: "FormasPagamento",
            component: () => import("@/views/financeiro/FormasPagamentoView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Orçamentos — Lista / Detalhe / Form / Settings (Fase 6.2)
        {
            path: "/orcamentos",
            name: "Orcamentos",
            component: () => import("@/views/orcamentos/OrcamentoListaView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/orcamentos/:id(\\d+)",
            name: "OrcamentoDetalhe",
            component: () => import("@/views/orcamentos/OrcamentoDetalheView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/orcamentos/:id(\\d+)/editar",
            name: "OrcamentoForm",
            component: () => import("@/views/orcamentos/OrcamentoFormView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/configuracoes/orcamento",
            name: "OrcamentoSettings",
            component: () => import("@/views/orcamentos/OrcamentoSettingsView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Cirurgias
        {
            path: "/pacientes/:pacienteId(\\d+)/cirurgias/:id(\\d+)",
            name: "CirurgiaDetalhe",
            component: () => import("@/views/cirurgias/CirurgiaView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Inventário
        {
            path: "/inventario",
            name: "Inventario",
            component: () => import("@/views/inventario/InventarioView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Agenda
        {
            path: "/agenda",
            name: "Agenda",
            component: () => import("@/views/agenda/AgendaView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Minhas consultas (worklist do profissional)
        {
            path: "/minhas-consultas",
            name: "MinhasConsultas",
            component: () => import("@/views/atendimentos/MeusAtendimentosView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Pacientes
        {
            path: "/pacientes",
            name: "Pacientes",
            component: () => import("@/views/pacientes/PacientesView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/pacientes/:id(\\d+)",
            name: "PacienteDetalhe",
            component: () => import("@/views/pacientes/PacienteDetalheView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/pacientes/:id(\\d+)/prontuario",
            name: "Prontuario",
            component: () => import("@/views/pacientes/ProntuarioView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // 404
        { path: "/:pathMatch(.*)*", redirect: "/" },
    ],
})

router.beforeEach(async (to) => {
    const auth = useAuthStore()
    const tenant = useTenantStore()

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
        return { name: "Login" }
    }

    // /meus-convites é acessível ao convidado mesmo com onboarding pendente —
    // ele precisa ver e aceitar convites antes de cadastrar dados pessoais.
    const rotasIsentasOnboarding = new Set(["Onboarding", "MeusConvites"])
    if (auth.isAuthenticated && auth.onboardingPendente && !rotasIsentasOnboarding.has((to.name as string) ?? "")) {
        return { name: "Onboarding" }
    }

    // Bloqueia acesso direto ao /onboarding após já ter completado
    if (to.name === "Onboarding" && auth.isAuthenticated && !auth.onboardingPendente) {
        return { name: "Home" }
    }

    if (to.meta.requiresTenant && !tenant.temTenantSelecionado) {
        // Profissional sem nenhum vínculo aprovado: redireciona para seus convites.
        if (tenant.semEstabelecimento) {
            return { name: "MeusConvites" }
        }
        // Tenant ainda não resolvido (ex: refresh em rota protegida antes do boot terminar).
        // Deixa passar — a view vai lidar com o estado vazio ou o boot vai resolver.
        return undefined
    }

    if (to.name === "Login" && auth.isAuthenticated) {
        return { name: "Home" }
    }

    if (to.name === "Landing" && auth.isAuthenticated && tenant.temTenantSelecionado) {
        return { name: "Home" }
    }

    // Bloqueio por assinatura inativa (trial expirado / suspensa / cancelada / expirada).
    // Só roda quando há tenant ativo E o usuário é Dono — o endpoint /minha-assinatura
    // exige papel Dono (retorna 403 SemAcesso para Profissional). Profissionais não
    // ficam bloqueados por estado de assinatura aqui; quando a assinatura está
    // inativa, o backend já gateia as ações de domínio com 422/403.
    if (auth.isAuthenticated && tenant.temTenantSelecionado && tenant.papel === "Dono") {
        const assinatura = useAssinaturaStore()
        await assinatura.ensureLoaded()

        const routeName = (to.name as string | undefined) ?? ""
        if (assinatura.isBlocked && !ROTAS_ISENTAS_ASSINATURA.has(routeName)) {
            return { name: "AssinaturaExpirada" }
        }
    }
})

export default router
