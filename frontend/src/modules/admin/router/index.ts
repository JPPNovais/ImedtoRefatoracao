import type { RouteRecordRaw } from "vue-router"

/**
 * Rotas do módulo admin global.
 *
 * Carregadas com lazy import no router principal (main.ts ou router/index.ts).
 * Guard de rota verifica adminAuthStore.isAuthenticated antes de liberar rotas protegidas.
 * Se must_reset_password = true, força /admin/change-password.
 */
export const adminRoutes: RouteRecordRaw[] = [
    {
        path: "/admin/login",
        name: "AdminLogin",
        component: () => import("../views/AdminLogin.vue"),
        meta: { adminPublica: true },
    },
    {
        path: "/admin",
        component: () => import("../views/AdminLayout.vue"),
        meta: { requiresAdminAuth: true },
        children: [
            {
                path: "",
                redirect: { name: "AdminDashboard" },
            },
            {
                path: "dashboard",
                name: "AdminDashboard",
                component: () => import("../views/AdminDashboard.vue"),
            },
            {
                path: "change-password",
                name: "AdminChangePassword",
                component: () => import("../views/AdminChangePassword.vue"),
                // Esta rota é acessível mesmo com must_reset_password = true.
                meta: { allowMustReset: true },
            },
            {
                path: "admins",
                name: "AdminAdminsList",
                component: () => import("../views/AdminsListView.vue"),
            },
            {
                path: "admins/novo",
                name: "AdminAdminsNew",
                component: () => import("../views/AdminsFormView.vue"),
            },
            {
                path: "estabelecimentos",
                name: "AdminEstabelecimentos",
                component: () => import("../views/EstabelecimentosListView.vue"),
            },
            {
                path: "estabelecimentos/:id",
                name: "AdminEstabelecimentoDetalhe",
                component: () => import("../views/EstabelecimentoDetalheView.vue"),
                props: true,
            },
            {
                path: "planos",
                name: "AdminPlanos",
                component: () => import("../views/PlanosListView.vue"),
            },
            {
                path: "planos/novo",
                name: "AdminPlanosNovo",
                component: () => import("../views/PlanosFormView.vue"),
            },
            {
                path: "planos/:id",
                name: "AdminPlanosEditar",
                component: () => import("../views/PlanosFormView.vue"),
                props: true,
            },
            // Configurações globais
            {
                path: "configuracoes",
                name: "AdminConfiguracoes",
                component: () => import("../views/ConfigsView.vue"),
            },
            {
                path: "config-trial",
                name: "AdminConfigTrial",
                component: () => import("../views/ConfigTrialView.vue"),
            },
            // Catálogos — Modelos de prontuário
            {
                path: "catalogos/modelos",
                name: "AdminModelosGlobais",
                component: () => import("../views/ModelosGlobaisListView.vue"),
            },
            {
                path: "catalogos/modelos/novo",
                name: "AdminModelosGlobaisNovo",
                component: () => import("../views/ModelosGlobaisFormView.vue"),
            },
            {
                path: "catalogos/modelos/:id",
                name: "AdminModelosGlobaisEditar",
                component: () => import("../views/ModelosGlobaisFormView.vue"),
                props: true,
            },
            // Catálogos — Variáveis pool
            {
                path: "catalogos/variaveis",
                name: "AdminVariaveisGlobais",
                component: () => import("../views/VariaveisGlobaisListView.vue"),
            },
            {
                path: "catalogos/variaveis/novo",
                name: "AdminVariaveisGlobaisNovo",
                component: () => import("../views/VariaveisGlobaisFormView.vue"),
            },
            {
                path: "catalogos/variaveis/:id",
                name: "AdminVariaveisGlobaisEditar",
                component: () => import("../views/VariaveisGlobaisFormView.vue"),
                props: true,
            },
            // Catálogos — Modelos de permissão padrão sistema
            {
                path: "catalogos/permissoes",
                name: "AdminPermissoesGlobais",
                component: () => import("../views/PermissoesGlobaisListView.vue"),
            },
            // Catálogos — Regiões anatômicas
            {
                path: "catalogos/regioes",
                name: "AdminRegioesGlobais",
                component: () => import("../views/RegioesGlobaisListView.vue"),
            },
            {
                path: "catalogos/regioes/novo",
                name: "AdminRegioesGlobaisNovo",
                component: () => import("../views/RegioesGlobaisFormView.vue"),
            },
            {
                path: "catalogos/regioes/:id",
                name: "AdminRegioesGlobaisEditar",
                component: () => import("../views/RegioesGlobaisFormView.vue"),
                props: true,
            },
            // Central de Migração (briefing 2026-06-15_001 — Marco 2)
            {
                path: "migracao",
                name: "AdminMigracoesLista",
                component: () => import("../views/MigracoesListView.vue"),
            },
            {
                path: "migracao/:jobId",
                name: "AdminMigracaoRevisao",
                component: () => import("../views/MigracaoRevisaoView.vue"),
                props: true,
            },
        ],
    },
    // Rota catch-all para /admin/* não encontrado.
    {
        path: "/admin/:pathMatch(.*)*",
        redirect: { name: "AdminDashboard" },
    },
]

/**
 * Guard de rota para o módulo admin.
 * Aplicado pelo router principal via beforeEach.
 */
export async function adminRouteGuard(
    to: { path: string; meta: Record<string, unknown>; name?: string | symbol | null | undefined },
    _from: unknown,
    next: (arg?: unknown) => void,
): Promise<void> {
    const isAdminPath = to.path.startsWith("/admin")
    if (!isAdminPath) {
        next()
        return
    }

    // Rotas públicas do admin (ex: /admin/login) passam livremente.
    if (to.meta.adminPublica) {
        next()
        return
    }

    // Importação lazy do store para evitar circular.
    const { useAdminAuthStore } = await import("../stores/adminAuthStore")
    const store = useAdminAuthStore()

    if (!store.isAuthenticated) {
        next({ name: "AdminLogin" })
        return
    }

    // Se must_reset_password e não está na rota de change-password → redireciona.
    if (store.mustResetPassword && !to.meta.allowMustReset) {
        next({ name: "AdminChangePassword" })
        return
    }

    next()
}
