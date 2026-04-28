import { defineStore } from "pinia"
import { ref, computed } from "vue"
import httpClient from "@/services/httpClient"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"

/**
 * Auth store — BFF pattern. Tokens ficam em cookies HttpOnly geridos pelo backend.
 * O estado local é o objeto do usuário (id, email, dados locais de domínio).
 */
export interface Usuario {
    id: string
    email: string
    nomeCompleto: string | null
    cpf: string | null
    telefone: string | null
    status: "Pendente" | "Ativo" | "Inativo"
    onboardingCompleto: boolean
    ultimoAcessoEm: string | null
}

export const useAuthStore = defineStore("auth", () => {
    const usuario = ref<Usuario | null>(null)

    const isAuthenticated = computed(() => !!usuario.value)
    const onboardingPendente = computed(
        () => !!usuario.value && !usuario.value.onboardingCompleto,
    )

    /** Reidrata a sessão via GET /auth/me — chamado no bootstrap antes de montar a app. */
    async function init() {
        const noAutoRefresh = { _noAutoRefresh: true } as any

        // Tentativa 1: token atual (válido por até 1h do Supabase)
        try {
            const { data } = await httpClient.get("/auth/me", noAutoRefresh)
            usuario.value = data.usuario
            return
        } catch (err: any) {
            if (err?.response?.status !== 401) {
                // Erro de rede ou servidor — não tenta refresh
                usuario.value = null
                useTenantStore().limpar()
                useProfissionalStore().limpar()
                return
            }
        }

        // Tentativa 2: access token expirado — renova via refresh token (cookie HttpOnly)
        try {
            await httpClient.post("/auth/refresh", {}, noAutoRefresh)
            const { data } = await httpClient.get("/auth/me", noAutoRefresh)
            usuario.value = data.usuario
        } catch {
            usuario.value = null
            useTenantStore().limpar()
            useProfissionalStore().limpar()
        }
    }

    async function login(email: string, password: string) {
        const { data } = await httpClient.post("/auth/login", { email, password })
        await recarregarMe()
        // Recarrega perfil profissional para o avatar do sidebar refletir o novo usuário.
        await useProfissionalStore().init()
        return data
    }

    async function signup(email: string, password: string) {
        const { data } = await httpClient.post("/auth/signup", { email, password })
        if (data.requerConfirmacaoEmail) {
            throw Object.assign(new Error("confirm-email"), { requerConfirmacaoEmail: true })
        }
        await recarregarMe()
        return data
    }

    async function recarregarMe() {
        const { data } = await httpClient.get("/auth/me")
        usuario.value = data.usuario
    }

    async function logout() {
        try {
            await httpClient.post("/auth/logout")
        } finally {
            usuario.value = null
            useTenantStore().limpar()
            useProfissionalStore().limpar()
        }
    }

    function setUsuario(novoUsuario: Usuario | null) {
        usuario.value = novoUsuario
    }

    return {
        usuario,
        isAuthenticated,
        onboardingPendente,
        init,
        login,
        signup,
        logout,
        setUsuario,
        recarregarMe,
    }
})
