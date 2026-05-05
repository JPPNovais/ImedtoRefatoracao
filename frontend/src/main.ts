import { createApp } from "vue"
import "@/assets/main.css"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import { useAuthStore } from "./stores/authStore"
import { useProfissionalStore } from "./stores/profissionalStore"
import { useTenantStore } from "./stores/tenantStore"
import { bootstrapService } from "./services/bootstrapService"

async function bootstrap() {
    const app = createApp(App)
    const pinia = createPinia()

    app.use(pinia)

    const auth = useAuthStore()
    const profissional = useProfissionalStore()
    const tenant = useTenantStore()

    // Único round-trip de hidratação. Substitui /auth/me + /profissional/me +
    // /estabelecimento serializados. Em caso de 401, o interceptor já tenta o
    // refresh; se falhar, cai aqui como erro e o usuário cai no /login pelo guard.
    const data = await bootstrapService.obter().catch(() => null)
    if (data) {
        auth.setUsuario(data.usuario)
        // Profissional e tenant só fazem sentido pós-onboarding — antes disso o
        // front sequer roteia para áreas que dependem deles.
        profissional.setProfissional(data.profissional)
        if (!auth.onboardingPendente) {
            tenant.popularEstabelecimentos(data.estabelecimentos)
        }
        auth.ativarRealtime()
    }

    app.use(router)
    await router.isReady()
    app.mount("#app")
}

bootstrap()
