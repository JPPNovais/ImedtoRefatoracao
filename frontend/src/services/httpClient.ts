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
 *
 * Base URL:
 * - Dev (`vite dev`): "/api" — o proxy do Vite redireciona para o backend local.
 * - Prod: `VITE_API_BASE_URL` + "/api" (ex: https://imedtorefatoracao.onrender.com/api).
 *   A variável é setada no painel da Vercel — em dev fica indefinida e o
 *   fallback "/api" continua usando o proxy.
 */
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL
    ? `${import.meta.env.VITE_API_BASE_URL.replace(/\/+$/, "")}/api`
    : "/api"

const httpClient = axios.create({
    baseURL: apiBaseUrl,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
    // Sem timeout, requests em rede ruim ficam pendurados indefinidamente.
    // 30s cobre uploads grandes; ajustar via second-arg em chamadas pontuais se preciso.
    timeout: 30_000,
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
                // Renova apenas o cookie — não atualiza o store com o usuário da credencial de auth,
                // pois esse objeto não tem os campos de domínio (ex: onboardingCompleto).
                await httpClient.post("/auth/refresh")

                return httpClient(originalRequest)
            } catch (refreshError: any) {
                const auth = useAuthStore()
                const tenant = useTenantStore()
                // Refresh com 401 é o caso "sem sessão" (cookie ausente/expirado) —
                // esperado quando o usuário ainda não logou ou ficou inativo demais.
                // Usar console.debug evita falso-positivo no monitoramento.
                const refreshStatus = refreshError?.response?.status
                const log = refreshStatus === 401 ? console.debug : console.warn
                log("[auth] Sessão expirada — redirecionando para login.", {
                    originalUrl: originalRequest?.url,
                    refreshStatus,
                })
                auth.setUsuario(null)
                tenant.limpar()
                router.push({ name: "Login" })
                return Promise.reject(error)
            } finally {
                isRefreshing = false
            }
        }

        // 402 Payment Required → distingue assinatura inativa (redirect) vs feature bloqueada (modal upsell)
        if (error.response?.status === 402) {
            const tipo = error.response?.data?.tipo as string | undefined
            const mensagem = error.response?.data?.mensagem ?? "Seu plano não inclui este recurso."

            if (tipo === "AssinaturaInativa") {
                // Assinatura inativa (trial expirado / suspensa / cancelada / expirada).
                // O router guard global já redireciona, mas aqui é segunda barreira para
                // requests fora de navegação (ex: AJAX em página já carregada).
                const { useAssinaturaStore } = await import("@/stores/assinaturaStore")
                const assinatura = useAssinaturaStore()
                await assinatura.recarregar()
                if (router.currentRoute.value.name !== "AssinaturaExpirada") {
                    router.push({ name: "AssinaturaExpirada" })
                }
            } else {
                // Feature não incluída no plano → modal de upsell.
                useUpsellStore().abrir(mensagem)
            }
            return Promise.reject(error)
        }

        return Promise.reject(error)
    },
)

export default httpClient
