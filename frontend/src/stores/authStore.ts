import { defineStore } from "pinia"
import { ref, computed } from "vue"
import httpClient from "@/services/httpClient"
import realtimeService from "@/services/realtimeService"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import { useAssinaturaStore } from "@/stores/assinaturaStore"

/**
 * Auth store — BFF pattern. Tokens ficam em cookies HttpOnly geridos pelo backend.
 * O estado local é o objeto do usuário (id, email, dados locais de domínio).
 */
export interface Usuario {
    id: string
    email: string
    nomeCompleto: string | null
    // cpf e ultimoAcessoEm removidos: backend nao envia mais (LGPD - minimizacao).
    // Telefone mantido — usado em MinhaContaView como round-trip do form.
    telefone: string | null
    status: "Pendente" | "Ativo" | "Inativo"
    onboardingCompleto: boolean
}

export const useAuthStore = defineStore("auth", () => {
    const usuario = ref<Usuario | null>(null)

    const isAuthenticated = computed(() => !!usuario.value)
    const onboardingPendente = computed(
        () => !!usuario.value && !usuario.value.onboardingCompleto,
    )

    /**
     * Reidrata a sessão via GET /auth/me — chamado no bootstrap antes de montar a app.
     * Após confirmar sessão válida, abre conexão realtime (SignalR) e registra handler de
     * notificações no store. A conexão é fire-and-forget: se falhar, a app continua
     * funcionando (sem realtime — usuário verá notificações apenas em refresh).
     */
    async function init() {
        const noAutoRefresh = { _noAutoRefresh: true } as any

        // Tentativa 1: token atual (cookie de access-token)
        try {
            const { data } = await httpClient.get("/auth/me", noAutoRefresh)
            usuario.value = data.usuario
            ativarRealtime()
            return
        } catch (err: any) {
            if (err?.response?.status !== 401) {
                // Erro de rede ou servidor — não tenta refresh
                await limparSessao()
                return
            }
        }

        // Tentativa 2: access token expirado — renova via refresh token (cookie HttpOnly)
        try {
            await httpClient.post("/auth/refresh", {}, noAutoRefresh)
            const { data } = await httpClient.get("/auth/me", noAutoRefresh)
            usuario.value = data.usuario
            ativarRealtime()
        } catch {
            await limparSessao()
        }
    }

    /**
     * Sobe a conexão SignalR e bind do store de notificações. Fire-and-forget — não trava
     * o login se o hub estiver offline.
     */
    function ativarRealtime() {
        useNotificacoesStore().bindRealtime()
        void realtimeService.start()
    }

    async function limparSessao() {
        usuario.value = null
        useTenantStore().limpar()
        useProfissionalStore().limpar()
        useNotificacoesStore().limpar()
        useAssinaturaStore().limpar()
        await realtimeService.stop()
    }

    async function login(email: string, password: string) {
        const { data } = await httpClient.post("/auth/login", { email, password })
        await recarregarMe()
        // Recarrega perfil profissional para o avatar do sidebar refletir o novo usuário.
        await useProfissionalStore().init()
        ativarRealtime()
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

    async function confirmarEmail(token: string) {
        await httpClient.post("/auth/confirmar-email", { token })
    }

    async function reenviarConfirmacao(email: string) {
        await httpClient.post("/auth/reenviar-confirmacao", { email })
    }

    async function redefinirSenha(token: string, novaSenha: string) {
        await httpClient.post("/auth/redefinir-senha", { token, novaSenha })
    }

    async function aceitarConvite(token: string, email: string, novaSenha: string) {
        // Backend já loga o usuário (cookies HttpOnly setados na resposta).
        await httpClient.post("/auth/aceitar-convite", { token, email, novaSenha })
        await recarregarMe()
        await useProfissionalStore().init()
        ativarRealtime()
    }

    async function recarregarMe() {
        const { data } = await httpClient.get("/auth/me")
        usuario.value = data.usuario
    }

    async function logout() {
        try {
            await httpClient.post("/auth/logout")
        } finally {
            await limparSessao()
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
        confirmarEmail,
        reenviarConfirmacao,
        redefinirSenha,
        aceitarConvite,
        setUsuario,
        recarregarMe,
        ativarRealtime,
    }
})
