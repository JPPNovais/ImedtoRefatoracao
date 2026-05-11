import { createApp } from "vue"
import "@/assets/main.css"
import { createPinia } from "pinia"
import App from "./App.vue"
import router from "./router"
import { useAuthStore } from "./stores/authStore"
import { useProfissionalStore } from "./stores/profissionalStore"
import { useTenantStore } from "./stores/tenantStore"
import { usePermissoesStore } from "./stores/permissoesStore"
import { bootstrapService } from "./services/bootstrapService"
import realtimeService from "./services/realtimeService"

async function bootstrap() {
    const app = createApp(App)
    const pinia = createPinia()

    app.use(pinia)

    const auth = useAuthStore()
    const profissional = useProfissionalStore()
    const tenant = useTenantStore()

    // Único round-trip de hidratação. Substitui /auth/me + /profissional/me +
    // /estabelecimento serializados. Quando NÃO há sessão (carga da landing/login),
    // o backend responde 401 — caso esperado, suprimimos auto-refresh e log de warning
    // para não poluir o console em prod (monitoramento de erro fica limpo).
    const data = await bootstrapService.obterInicial().catch((err) => {
        const status = err?.response?.status
        if (status === 401) {
            // Sem sessão na carga inicial — comportamento esperado, não loga.
            return null
        }
        // Erro de rede/servidor real: registra pra diagnóstico.
        // eslint-disable-next-line no-console
        console.warn("[bootstrap] falhou — usuário cairá em /login pelo guard.", {
            status,
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
        registrarListenersPermissoes()
    }

    app.use(router)
    await router.isReady()
    app.mount("#app")
}

/**
 * Hooks que mantêm `permissoesStore` em dia quando o Dono altera o modelo do vínculo:
 *
 *  - SignalR `permissoes-alteradas` (push imediato pelo backend ao salvar).
 *  - `visibilitychange` (rede de segurança: se o WS perdeu, ao voltar pra aba revalida).
 *
 * Idempotente: registrado uma única vez no boot.
 */
function registrarListenersPermissoes() {
    const tenant = useTenantStore()
    const permissoes = usePermissoesStore()

    realtimeService.on<{ estabelecimentoId: number }>("permissoes-alteradas", (payload) => {
        // Só revalida se o evento é do tenant que está ativo agora — outros tenants
        // são revalidados quando o usuário trocar pra eles (popularEstabelecimentos
        // recarrega tudo via /auth/bootstrap).
        if (tenant.ativo && payload?.estabelecimentoId === tenant.ativo.id) {
            void permissoes.revalidar()
        }
    })

    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "visible" && tenant.ativo) {
            void permissoes.revalidar()
        }
    })
}

bootstrap()
