import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"

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
      { path: "agenda", name: "agenda", component: () => import("@/views/AgendaView.vue"), meta: { tab: "agenda" } },
      { path: "pacientes", name: "pacientes", component: () => import("@/views/PacientesView.vue"), meta: { tab: "pacientes" } },
      { path: "avisos", name: "avisos", component: () => import("@/views/AvisosView.vue"), meta: { tab: "avisos" } },
      { path: "mais", name: "mais", component: () => import("@/views/MaisView.vue"), meta: { tab: "mais" } },
    ],
  },

  // Drill-ins (push lateral, tela cheia)
  { path: "/agenda/:id", name: "agendamento", component: () => import("@/views/AgendamentoDetalheView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/novo-agendamento", name: "novo-agendamento", component: () => import("@/views/NovoAgendamentoView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/paciente/:id", name: "ficha", component: () => import("@/views/PacienteFichaView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/paciente/:id/prontuario", name: "prontuario", component: () => import("@/views/ProntuarioView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/receita", name: "receita", component: () => import("@/views/ReceitaView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/atestado", name: "atestado", component: () => import("@/views/AtestadoView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/exame", name: "exame", component: () => import("@/views/ExameView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },
  { path: "/orcamento/:id", name: "orcamento", component: () => import("@/views/OrcamentoView.vue"), meta: { layout: "push", requiresAuth: true, requiresTenant: true } },

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
  return true
})

export default router
