import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"
import { usePermissoesStore } from "@/stores/permissoes"
import { useUiStore } from "@/stores/ui"

const routes: RouteRecordRaw[] = [
  { path: "/login", name: "login", component: () => import("@/views/LoginView.vue"), meta: { layout: "full", public: true } },
  { path: "/seletor", name: "seletor", component: () => import("@/views/SeletorEstabelecimentoView.vue"), meta: { layout: "full", requiresAuth: true } },
  { path: "/assinatura-expirada", name: "assinatura", component: () => import("@/views/AssinaturaExpiradaView.vue"), meta: { layout: "full", requiresAuth: true } },

  // Abas (chrome persistente: top bar + bottom tab bar)
  {
    path: "/",
    component: () => import("@/components/layout/TabsLayout.vue"),
    meta: { requiresAuth: true, requiresTenant: true },
    children: [
      { path: "", redirect: "/agenda" },
      { path: "agenda", name: "agenda", component: () => import("@/views/AgendaView.vue"), meta: { tab: "agenda", perm: "agenda.ver" } },
      { path: "pacientes", name: "pacientes", component: () => import("@/views/PacientesView.vue"), meta: { tab: "pacientes", perm: "pacientes.ver" } },
      { path: "avisos", name: "avisos", component: () => import("@/views/AvisosView.vue"), meta: { tab: "avisos" } },
      { path: "mais", name: "mais", component: () => import("@/views/MaisView.vue"), meta: { tab: "mais" } },
    ],
  },

  // Drill-ins (push lateral, tela cheia) — cada um com a permissão exigida (RBAC)
  { path: "/agenda/:id", name: "agendamento", component: () => import("@/views/AgendamentoDetalheView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "agenda.ver" } },
  { path: "/novo-agendamento", name: "novo-agendamento", component: () => import("@/views/NovoAgendamentoView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "agenda.criar" } },
  { path: "/paciente/:id", name: "ficha", component: () => import("@/views/PacienteFichaView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "pacientes.ver" } },
  { path: "/paciente/:id/prontuario", name: "prontuario", component: () => import("@/views/ProntuarioView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "prontuario.ver" } },
  { path: "/receita", name: "receita", component: () => import("@/views/ReceitaView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "prescricao" } },
  { path: "/atestado", name: "atestado", component: () => import("@/views/AtestadoView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "prescricao" } },
  { path: "/exame", name: "exame", component: () => import("@/views/ExameView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "prescricao" } },
  { path: "/orcamento/:id", name: "orcamento", component: () => import("@/views/OrcamentoView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true, perm: "orcamento.ver" } },

  { path: "/:pathMatch(.*)*", redirect: "/agenda" },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  const tenant = useTenantStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: "login" }
  }
  if (to.meta.public && auth.isAuthenticated) {
    return tenant.temTenantSelecionado ? { name: "agenda" } : { name: "seletor" }
  }
  if (to.meta.requiresTenant && !tenant.temTenantSelecionado) {
    // Sem vínculo nenhum → o seletor mostra o estado vazio (G2/onboarding).
    return { name: "seletor" }
  }
  // RBAC (G2): bloqueia acesso direto a rota sem permissão → cai numa aba acessível.
  const perm = to.meta.perm as string | undefined
  if (perm && tenant.temTenantSelecionado) {
    const permissoes = usePermissoesStore()
    if (!permissoes.pode(perm)) {
      useUiStore().toast("Você não tem permissão para acessar isso", "error")
      const fallback = permissoes.pode("agenda.ver")
        ? "agenda"
        : permissoes.pode("pacientes.ver")
          ? "pacientes"
          : "avisos"
      // evita loop se a própria rota destino já é o fallback
      return to.name === fallback ? false : { name: fallback }
    }
  }
  return true
})

export default router
