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
    const data = await bootstrapService.obter().catch((err) => {
        // Diagnóstico explícito: sem isso o usuário fica numa SPA "morta" (sem tenant,
        // sem usuário, sem feedback de erro). Logamos status + URL para facilitar suporte.
        // eslint-disable-next-line no-console
        console.warn("[bootstrap] falhou — usuário cairá em /login pelo guard.", {
            status: err?.response?.status,
            data: err?.response?.data,
            message: err?.message,
        })
        return null
    })
    if (data) {
        auth.setUsuario(data.usuario)
        profissional.setProfissional(data.profissional)
        if (!auth.onboardingPendente) {
            tenant.popularEstabelecimentos(data.estabelecimentos)
            // Sanidade: se o backend devolveu estabelecimentos mas nada foi selecionado,
            // o sessionStorage tem um tenant órfão (id que não existe mais nesta lista).
            // Limpar e re-selecionar evita SPA chamando endpoints com header inválido.
            const ids = new Set(data.estabelecimentos.map(e => e.id))
            if (tenant.estabelecimentoAtivoId && !ids.has(tenant.estabelecimentoAtivoId)) {
                // eslint-disable-next-line no-console
                console.warn("[bootstrap] sessionStorage tinha estabelecimento órfão — re-selecionando.")
                tenant.limpar()
                tenant.popularEstabelecimentos(data.estabelecimentos)
            }
        }
        auth.ativarRealtime()
    }

    app.use(router)
    await router.isReady()
    app.mount("#app")
}

bootstrap()
