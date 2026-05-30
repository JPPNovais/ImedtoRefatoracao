/**
 * Cliente HTTP isolado para a área admin global.
 *
 * Isolamento: não importa httpClient do app principal nem qualquer store de tenant.
 * Cookie admin-access-token é lido automaticamente (withCredentials: true).
 *
 * Interceptor de 401: tenta refresh via /api/admin/auth/refresh.
 *                     Se falhar → logout + redirect para /admin/login.
 */
import axios, { type AxiosInstance } from "axios"

// Lazy import para evitar import circular (store precisa do api, api precisa do store para logout).
// Retorna o composable (função), não a instância — instância é criada no momento do uso.
const getStore = () => import("../stores/adminAuthStore").then((m) => m.useAdminAuthStore)

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL
    ? `${(import.meta.env.VITE_API_BASE_URL as string).replace(/\/+$/, "")}/api/admin`
    : "/api/admin"

const adminApi: AxiosInstance = axios.create({
    baseURL: apiBaseUrl,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
    timeout: 30_000,
})

let isRefreshing = false

adminApi.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config

        const is401 = error.response?.status === 401
        const isRefreshEndpoint = originalRequest?.url?.includes("/auth/refresh")
        const alreadyRetried = originalRequest?._retry
        const skipAutoRefresh = (originalRequest as { _noAutoRefresh?: boolean })?._noAutoRefresh === true

        if (is401 && !isRefreshEndpoint && !alreadyRetried && !isRefreshing && !skipAutoRefresh) {
            originalRequest._retry = true
            isRefreshing = true

            try {
                await adminApi.post("/auth/refresh", {}, { _noAutoRefresh: true } as never)
                isRefreshing = false
                return adminApi(originalRequest)
            } catch {
                isRefreshing = false
                const store = await getStore()
                await store().logout()
                window.location.href = "/admin/login"
                return Promise.reject(error)
            }
        }

        return Promise.reject(error)
    },
)

export default adminApi
