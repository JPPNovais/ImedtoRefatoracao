import axios from "axios"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { useUpsellStore } from "@/stores/upsellStore"
import router from "@/router"

/**
 * Cliente HTTP global — BFF pattern.
 *
 * - withCredentials: true → envia cookies HttpOnly em todo request.
 * - Interceptor de request: injeta o header X-Estabelecimento-Id quando há tenant ativo.
 * - Interceptor de 401: tenta refresh automático e repete o request original.
 * - Interceptor de 402: abre modal de upsell global.
 */
const httpClient = axios.create({
    baseURL: "/api",
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
})

httpClient.interceptors.request.use((config) => {
    const tenant = useTenantStore()
    if (tenant.estabelecimentoAtivoId) {
        config.headers.set(
            "X-Estabelecimento-Id",
            String(tenant.estabelecimentoAtivoId),
        )
    }
    return config
})

let isRefreshing = false

httpClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config

        const is401 = error.response?.status === 401
        const isRefreshEndpoint = originalRequest?.url?.includes("/auth/refresh")
        const alreadyRetried = originalRequest?._retry

        const skipAutoRefresh = (originalRequest as any)?._noAutoRefresh === true

        if (is401 && !isRefreshEndpoint && !alreadyRetried && !isRefreshing && !skipAutoRefresh) {
            originalRequest._retry = true
            isRefreshing = true

            try {
                // Renova apenas o cookie — não atualiza o store com o usuário do Supabase Auth,
                // pois esse objeto não tem os campos de domínio (ex: onboardingCompleto).
                await httpClient.post("/auth/refresh")

                return httpClient(originalRequest)
            } catch (refreshError) {
                const auth = useAuthStore()
                const tenant = useTenantStore()
                console.warn("[auth] Sessão expirada — redirecionando para login.", {
                    originalUrl: originalRequest?.url,
                    refreshError,
                })
                auth.setUsuario(null)
                tenant.limpar()
                router.push({ name: "Login" })
                return Promise.reject(error)
            } finally {
                isRefreshing = false
            }
        }

        // 402 Payment Required → abre modal de upsell
        if (error.response?.status === 402) {
            const mensagem = error.response?.data?.mensagem ?? "Seu plano não inclui este recurso."
            useUpsellStore().abrir(mensagem)
            return Promise.reject(error)
        }

        return Promise.reject(error)
    },
)

export default httpClient
