import { createRouter, createWebHistory } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { useAssinaturaStore } from "@/stores/assinaturaStore"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { podeAcessarRota, rotaRestrita } from "./routePermissions"
import { adminRoutes } from "@/modules/admin/router"

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
        // Aceite de termo via link público (Fase 4). Anônima, sem AppLayout —
        // autenticação é o próprio token na URL. NÃO incluir em ROTAS_RESTRITAS
        // nem marcar como requiresAuth; o backend já trata segurança e LGPD.
        {
            path: "/termos/aceite/:token",
            name: "AceiteTermoPublico",
            component: () => import("@/views/publico/AceiteTermoPublicoView.vue"),
        },
        // Confirmação de presença em agendamento via link público (Fase 2). Anônima, sem AppLayout.
        // Token 256 bits é a credencial. Backend retorna apenas dados mínimos (sem PII).
        {
            path: "/agendamentos/confirmar/:token",
            name: "ConfirmarPresencaPublico",
            component: () => import("@/views/publico/ConfirmarPresencaPublicaView.vue"),
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

        // Termos de consentimento (modelos)
        {
            path: "/configuracoes/termos",
            name: "TermosModelos",
            component: () => import("@/views/configuracoes/TermosListaView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/configuracoes/termos/novo",
            name: "TermosNovo",
            component: () => import("@/views/configuracoes/TermoFormView.vue"),
            meta: { requiresAuth: true, requiresTenant: true, ...APP },
        },
        {
            path: "/configuracoes/termos/:id(\\d+)/editar",
            name: "TermosEditar",
            component: () => import("@/views/configuracoes/TermoFormView.vue"),
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
            path: "/orcamentos/novo",
            name: "OrcamentoNovo",
            component: () => import("@/views/orcamentos/OrcamentoFormView.vue"),
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
        {
            path: "/inventario/cadastros",
            name: "InventarioCadastros",
            component: () => import("@/views/inventario/EstoqueCadastrosView.vue"),
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

        // Módulo admin global (CA47/CA48) — lazy load completo.
        // O guard adminRouteGuard protege as rotas filhas.
        ...adminRoutes,
    ],
})

router.beforeEach(async (to) => {
    // Guard do módulo admin — rotas /admin/* são completamente independentes.
    // Retorna antes de entrar na lógica de auth do app principal.
    if (to.path.startsWith("/admin")) {
        // Rotas públicas do admin (ex: /admin/login) passam sem verificação.
        if (to.meta.adminPublica) return true

        const { useAdminAuthStore } = await import("@/modules/admin/stores/adminAuthStore")
        const adminStore = useAdminAuthStore()

        if (!adminStore.isAuthenticated) return { name: "AdminLogin" }
        if (adminStore.mustResetPassword && !to.meta.allowMustReset) return { name: "AdminChangePassword" }
        return true
    }

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

    // Defense-in-depth UX: bloqueia rotas restritas para quem não tem a
    // permissão correspondente, em vez de deixar a view renderizar e cair
    // num 422 pequeno no meio da tela. O backend continua sendo a fonte da
    // verdade — este guard é apenas espelho do `[RequiresAcao]` dos controllers.
    // Skip quando o tenant ainda não resolveu (popularEstabelecimentos roda no boot).
    if (
        tenant.temTenantSelecionado
        && rotaRestrita(to.name as string | null)
    ) {
        const permissoes = usePermissoesStore()
        const acessivel = podeAcessarRota(to.name as string, {
            ehDono: permissoes.ehDono,
            pode: (k) => permissoes.pode(k),
            podeExtra: (k) => permissoes.podeExtra(k),
        })
        if (!acessivel) {
            // Sinaliza para a Home exibir um toast discreto explicando o
            // redirecionamento. Sem isso, o usuário só vê a URL trocar e
            // pensa que clicou errado. O nome da rota bloqueada acompanha o
            // querystring para permitir uma mensagem contextual futuramente —
            // hoje a Home usa o mesmo texto para qualquer rota.
            return { name: "Home", query: { bloqueado: String(to.name ?? "") } }
        }
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
