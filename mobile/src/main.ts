import { createApp } from "vue"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import { configureHttp } from "@/lib/http"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore } from "@/stores/ui"
import { initDb } from "@/lib/db"

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

  await ui.initTheme()
  await initDb()
  await auth.bootstrap() // rehidrata sessão via cookie (BFF)

  app.use(router)
  await router.isReady()
  app.mount("#app")
}

void bootstrap()
