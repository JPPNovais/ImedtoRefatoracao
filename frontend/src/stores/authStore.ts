import { defineStore } from "pinia"
import { ref, computed } from "vue"
import httpClient from "@/services/httpClient"
import realtimeService from "@/services/realtimeService"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import { useAssinaturaStore } from "@/stores/assinaturaStore"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { useUpsellStore } from "@/stores/upsellStore"
import { bootstrapService } from "@/services/bootstrapService"

/**
 * Chaves do localStorage que carregam dados/contexto da sessão e PRECISAM ser
 * limpas no logout/login. `imedto-theme` (preferência UI) NÃO entra aqui — é
 * neutra entre contas e deve sobreviver à troca de usuário.
 *
 * Mantida fora do store para ser auditável num lugar só. Se você adicionar
 * uma nova chave persistida com dados de sessão/tenant/PII, adicione aqui.
 */
const STORAGE_KEYS_SESSAO: ReadonlyArray<string> = [
    "imedto.atendimento_ativo", // composables/useAtendimentoAtivo.ts
    "imedto.receitas.v1",        // services/receitaLocalService.ts (PII de paciente)
]

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
            await hidratarUsuario(data.usuario)
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
            await hidratarUsuario(data.usuario)
            ativarRealtime()
        } catch {
            await limparSessao()
        }
    }

    /**
     * Atribui `usuario.value` detectando troca de identidade — se o id que vem
     * do servidor diverge do atual em memória, limpa toda a sessão antes de
     * setar. Defense-in-depth contra qualquer caminho que reidrata sem ter
     * passado por `login()` (cookies trocados externamente, segundo init,
     * recarregarMe após login programático, etc.).
     */
    async function hidratarUsuario(novo: Usuario | null) {
        if (novo && usuario.value && usuario.value.id !== novo.id) {
            await limparSessao()
        }
        usuario.value = novo
    }

    /**
     * Sobe a conexão SignalR e bind do store de notificações. Fire-and-forget — não trava
     * o login se o hub estiver offline. Pulado quando o onboarding ainda não foi concluído:
     * o backend bloqueia /api/notificacoes/* com 403 OnboardingPendente, e ainda não há
     * notificações pra exibir. Após finalizar o onboarding, recarregarMe() roda de novo
     * com o usuário atualizado e o realtime sobe.
     */
    function ativarRealtime() {
        if (!usuario.value?.onboardingCompleto) return
        useNotificacoesStore().bindRealtime()
        void realtimeService.start()
    }

    /**
     * Zera TODO estado em memória + persistido (sessionStorage/localStorage) que
     * carregue identidade, tenant, papel, permissões ou PII.
     *
     * Chamada em DOIS momentos:
     *  1. `logout()` — usuário pediu sair.
     *  2. **Antes** de `bootstrapPosAuth()` em `login`, `signup`, `aceitarConvite` —
     *     garante que o novo usuário não herde resíduo da conta anterior em browser
     *     compartilhado (o flow do `tenantStore.popularEstabelecimentos` faz spread
     *     do `ativo` no sessionStorage e preserva o `papel` antigo se o id casar).
     *
     * O que NÃO é limpo: preferências neutras (`imedto-theme`). Cookies HttpOnly
     * de auth são gerenciados pelo backend (logout limpa via Set-Cookie).
     */
    async function limparSessao() {
        usuario.value = null
        useTenantStore().limpar()
        useProfissionalStore().limpar()
        useNotificacoesStore().limpar()
        useAssinaturaStore().limpar()
        usePermissoesStore().limpar()
        useUpsellStore().fechar()
        for (const key of STORAGE_KEYS_SESSAO) {
            try { localStorage.removeItem(key) } catch { /* modo privado / quota */ }
        }
        await realtimeService.stop()
    }

    async function login(email: string, password: string) {
        const { data } = await httpClient.post("/auth/login", { email, password })
        // Fail-safe: zera qualquer resíduo de sessão anterior (browser compartilhado,
        // logout que falhou, cookie expirado seguido de novo login) antes de hidratar.
        await limparSessao()
        await bootstrapPosAuth()
        return data
    }

    async function signup(email: string, password: string) {
        const { data } = await httpClient.post("/auth/signup", { email, password })
        if (data.requerConfirmacaoEmail) {
            throw Object.assign(new Error("confirm-email"), { requerConfirmacaoEmail: true })
        }
        await limparSessao()
        await bootstrapPosAuth()
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
        await limparSessao()
        await bootstrapPosAuth()
    }

    async function recarregarMe() {
        const { data } = await httpClient.get("/auth/me")
        await hidratarUsuario(data.usuario)
    }

    /**
     * Hidrata usuario + profissional + estabelecimentos via /auth/bootstrap após
     * autenticação bem-sucedida (login/signup/aceitarConvite). Sem isso, o tenant
     * fica null e o HomeView mostra o estado "indeterminado" ("Não conseguimos
     * carregar seus dados") — porque o bootstrap inicial de main.ts só roda uma
     * vez na carga da SPA, não a cada login.
     */
    async function bootstrapPosAuth() {
        const data = await bootstrapService.obter()
        await hidratarUsuario(data.usuario)
        useProfissionalStore().setProfissional(data.profissional)
        if (!onboardingPendente.value) {
            useTenantStore().popularEstabelecimentos(data.estabelecimentos)
        }
        ativarRealtime()
    }

    async function logout() {
        try {
            await httpClient.post("/auth/logout")
        } finally {
            await limparSessao()
        }
    }

    async function alterarSenha(senhaAtual: string, novaSenha: string) {
        await httpClient.post("/auth/alterar-senha", { senhaAtual, novaSenha })
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
        alterarSenha,
        aceitarConvite,
        setUsuario,
        recarregarMe,
        ativarRealtime,
    }
})
