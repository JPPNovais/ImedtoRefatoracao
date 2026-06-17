import { defineStore } from "pinia"
import { ref, computed } from "vue"
import adminApi from "../services/adminApi"

export interface AdminInfo {
    id: string
    email: string
    nome: string
    ativo: boolean
    forcePasswordReset: boolean
    ultimoLoginEm: string | null
}

const INATIVIDADE_MS = 15 * 60 * 1000 // 15 minutos

/**
 * Store de autenticação do módulo admin global.
 *
 * Isolamento: NÃO usa stores/authStore, NÃO usa httpClient do app principal.
 * Tokens ficam em cookies HttpOnly geridos pelo backend (BFF pattern).
 * Estado local: apenas { id, email, nome, forcePasswordReset } — sem PII de tenant.
 */
export const useAdminAuthStore = defineStore("adminAuth", () => {
    const admin = ref<AdminInfo | null>(null)
    let inactivityTimer: ReturnType<typeof setTimeout> | null = null

    const isAuthenticated = computed(() => !!admin.value)
    const mustResetPassword = computed(() => admin.value?.forcePasswordReset === true)

    /**
     * Reidrata sessão via GET /api/admin/auth/me ao entrar nas rotas /admin/*.
     * Tenta refresh automático se access token expirou.
     */
    async function init() {
        try {
            const { data } = await adminApi.get<AdminInfo>("/auth/me", {
                _noAutoRefresh: true,
            } as never)
            admin.value = data
            iniciarTimerInatividade()
            return
        } catch (err: unknown) {
            const status = (err as { response?: { status: number } })?.response?.status
            if (status !== 401) {
                await limparSessao()
                return
            }
        }

        // Tenta refresh.
        try {
            await adminApi.post("/auth/refresh", {}, { _noAutoRefresh: true } as never)
            const { data } = await adminApi.get<AdminInfo>("/auth/me", {
                _noAutoRefresh: true,
            } as never)
            admin.value = data
            iniciarTimerInatividade()
        } catch {
            await limparSessao()
        }
    }

    async function login(email: string, senha: string) {
        const { data } = await adminApi.post<{ admin: AdminInfo; accessToken?: string }>(
            "/auth/login",
            { email, senha },
        )
        admin.value = data.admin
        iniciarTimerInatividade()
        return data.admin
    }

    async function logout() {
        clearInactivityTimer()
        try {
            await adminApi.post("/auth/logout")
        } catch {
            // Silencia erro de rede — sessão local é limpa independentemente.
        }
        await limparSessao()
    }

    async function recarregarMe() {
        const { data } = await adminApi.get<AdminInfo>("/auth/me")
        admin.value = data
    }

    /**
     * Troca a própria senha.
     *
     * - Troca voluntária (admin regular): fornecer senhaAtual — obrigatória no backend.
     * - Força-reset (must_reset_password = true): omitir senhaAtual (backend ignora se fornecida).
     */
    async function changePassword(novaSenha: string, senhaAtual?: string) {
        await adminApi.post("/auth/change-password", { novaSenha, senhaAtual })
        // Após troca, os cookies são renovados pelo backend — reidrata.
        await recarregarMe()
    }

    // ── Inatividade 15min (CA5) ───────────────────────────────────────────────

    function iniciarTimerInatividade() {
        clearInactivityTimer()
        inactivityTimer = setTimeout(async () => {
            await logout()
            // Toast é responsabilidade do componente que ouve o store.
        }, INATIVIDADE_MS)
    }

    function resetarTimerInatividade() {
        if (!admin.value) return
        iniciarTimerInatividade()
    }

    function clearInactivityTimer() {
        if (inactivityTimer !== null) {
            clearTimeout(inactivityTimer)
            inactivityTimer = null
        }
    }

    async function limparSessao() {
        clearInactivityTimer()
        admin.value = null
    }

    return {
        admin,
        isAuthenticated,
        mustResetPassword,
        init,
        login,
        logout,
        recarregarMe,
        changePassword,
        resetarTimerInatividade,
    }
})
