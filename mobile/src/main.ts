import { createApp, watch } from "vue"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import { configureHttp } from "@/lib/http"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore } from "@/stores/ui"
import { initDb } from "@/lib/db"
import { configurarStatusBar } from "@/native/useStatusBar"

import "./styles/tokens.css"
import "./styles/app.css"

async function bootstrap() {
  const app = createApp(App)
  const pinia = createPinia()
  app.use(pinia)

  const ui = useUiStore()
  const auth = useAuthStore()
  const tenant = useTenantStore()

  // HTTP: injeta o tenant ativo e reage a sessão/assinatura.
  configureHttp({
    tenantIdProvider: () => tenant.estabelecimentoAtivoId,
    onAuthExpired: () => {
      void auth.limparSessao().then(() => router.replace({ name: "login" }))
    },
    onAssinaturaBloqueada: (tipo) => {
      if (tipo === "AssinaturaInativa" || tipo === "RequiresAssinaturaAtiva") {
        router.replace({ name: "assinatura" })
      } else {
        ui.toast("Recurso disponível no plano superior", "error")
      }
    },
  })

  // initTheme e initDb são independentes do bootstrap — rodam em paralelo para reduzir TTI.
  await Promise.all([ui.initTheme(), initDb()])

  // Barra de status nativa com espaço próprio + estilo conforme o tema (evita
  // conteúdo do app vazando na faixa do topo). Atualiza ao trocar claro/escuro.
  void configurarStatusBar(ui.isDark)
  watch(() => ui.isDark, (dark) => void configurarStatusBar(dark))

  await auth.bootstrap() // rehidrata sessão via cookie (BFF)

  app.use(router)
  await router.isReady()
  app.mount("#app")
}

void bootstrap()
