import { createApp } from "vue"
import "@/assets/main.css"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import { useAuthStore } from "./stores/authStore"
import { useProfissionalStore } from "./stores/profissionalStore"
import { useTenantStore } from "./stores/tenantStore"
import { estabelecimentoService } from "./services/estabelecimentoService"

async function bootstrap() {
    const app = createApp(App)
    const pinia = createPinia()

    app.use(pinia)

    // init() ANTES de app.use(router): o beforeEach dispara no install() do router
    // e precisa ver isAuthenticated correto; se rodar depois, redireciona para /login.
    const auth = useAuthStore()
    await auth.init()

    if (auth.isAuthenticated && !auth.onboardingPendente) {
        // Carrega perfil profissional e resolve tenant em paralelo.
        await Promise.all([
            useProfissionalStore().init(),
            useTenantStore().resolverTenant(() => estabelecimentoService.listarMeus()),
        ])
    } else if (auth.isAuthenticated) {
        await useProfissionalStore().init()
    }

    app.use(router)
    await router.isReady()
    app.mount("#app")
}

bootstrap()
