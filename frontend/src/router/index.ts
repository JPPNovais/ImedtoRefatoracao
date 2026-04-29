import { createRouter, createWebHistory } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"

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
        {
            path: "/selecionar-estabelecimento",
            name: "SelecionarEstabelecimento",
            component: () => import("@/views/tenant/SelecionarEstabelecimentoView.vue"),
            meta: { requiresAuth: true },
        },
        {
            path: "/criar-estabelecimento",
            name: "CriarPrimeiroEstabelecimento",
            component: () => import("@/views/tenant/CriarPrimeiroEstabelecimentoView.vue"),
            meta: { requiresAuth: true },
        },

        // Com tenant (com sidebar)
        {
            path: "/home",
            name: "Home",
            component: () => import("@/views/HomeView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/minha-conta",
            name: "MinhaConta",
            component: () => import("@/views/minhaConta/MinhaContaView.vue"),
            meta: { requiresAuth: true, ...APP },
        },

        // Configurações (apenas Dono)
        {
            path: "/configuracoes/modelos-permissao",
            name: "ModelosPermissao",
            component: () => import("@/views/configuracoes/ModelosPermissaoView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/configuracoes/modelos-prontuario",
            name: "ModelosProntuario",
            component: () => import("@/views/configuracoes/ModelosProntuarioView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Profissionais (apenas Dono)
        {
            path: "/profissionais",
            name: "Profissionais",
            component: () => import("@/views/profissionais/ProfissionaisView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
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
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },

        // Relatórios
        {
            path: "/relatorios",
            name: "Relatorios",
            component: () => import("@/views/relatorios/RelatoriosView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
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

        // Orçamentos
        {
            path: "/orcamentos",
            name: "Orcamentos",
            component: () => import("@/views/orcamentos/OrcamentosView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/orcamentos/:id(\\d+)/completo",
            name: "OrcamentoCompleto",
            component: () => import("@/views/orcamentos/OrcamentoCompletoView.vue"),
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

router.beforeEach((to) => {
    const auth = useAuthStore()
    const tenant = useTenantStore()

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
        return { name: "Login" }
    }

    if (auth.isAuthenticated && auth.onboardingPendente && to.name !== "Onboarding") {
        return { name: "Onboarding" }
    }

    // Bloqueia acesso direto ao /onboarding após já ter completado
    if (to.name === "Onboarding" && auth.isAuthenticated && !auth.onboardingPendente) {
        return { name: "Home" }
    }

    if (to.meta.requiresTenant && !tenant.temTenantSelecionado) {
        return { name: "SelecionarEstabelecimento" }
    }

    if (to.name === "Login" && auth.isAuthenticated) {
        return { name: "Home" }
    }

    if (to.name === "Landing" && auth.isAuthenticated && tenant.temTenantSelecionado) {
        return { name: "Home" }
    }
})

export default router
