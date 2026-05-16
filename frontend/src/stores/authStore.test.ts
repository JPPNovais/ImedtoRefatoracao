import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"

// Mocks declarados antes do import do store.
vi.mock("@/services/httpClient", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        delete: vi.fn(),
    },
}))

vi.mock("@/services/realtimeService", () => ({
    default: {
        start: vi.fn().mockResolvedValue(undefined),
        stop: vi.fn().mockResolvedValue(undefined),
        on: vi.fn(),
        off: vi.fn(),
    },
}))

vi.mock("@/stores/tenantStore", () => ({
    useTenantStore: vi.fn(),
}))

vi.mock("@/stores/profissionalStore", () => ({
    useProfissionalStore: vi.fn(),
}))

vi.mock("@/stores/notificacoesStore", () => ({
    useNotificacoesStore: vi.fn(),
}))

vi.mock("@/stores/assinaturaStore", () => ({
    useAssinaturaStore: vi.fn(),
}))

import httpClient from "@/services/httpClient"
import realtimeService from "@/services/realtimeService"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import { useAssinaturaStore } from "@/stores/assinaturaStore"
import { useAuthStore } from "@/stores/authStore"

// Fábrica de usuário para evitar repetição
function criarUsuario(overrides = {}) {
    return {
        id: "uuid-1",
        email: "joao@imedto.com",
        nomeCompleto: "João",
        telefone: null,
        status: "Ativo" as const,
        onboardingCompleto: true,
        ...overrides,
    }
}

function setupStoreMocks() {
    const tenantLimpar = vi.fn()
    const tenantPopular = vi.fn()
    const profissionalLimpar = vi.fn()
    const profissionalInit = vi.fn().mockResolvedValue(undefined)
    const profissionalSet = vi.fn()  // bootstrapPosAuth chama profissional.setProfissional(data.profissional)
    const notificacoesLimpar = vi.fn()
    const notificacoesBindRealtime = vi.fn()
    const assinaturaLimpar = vi.fn()

    vi.mocked(useTenantStore).mockReturnValue({ limpar: tenantLimpar, popularEstabelecimentos: tenantPopular } as any)
    vi.mocked(useProfissionalStore).mockReturnValue({
        limpar: profissionalLimpar,
        init: profissionalInit,
        setProfissional: profissionalSet,
    } as any)
    vi.mocked(useNotificacoesStore).mockReturnValue({
        limpar: notificacoesLimpar,
        bindRealtime: notificacoesBindRealtime,
    } as any)
    vi.mocked(useAssinaturaStore).mockReturnValue({ limpar: assinaturaLimpar } as any)

    return { tenantLimpar, tenantPopular, profissionalLimpar, profissionalInit, profissionalSet, notificacoesLimpar, notificacoesBindRealtime, assinaturaLimpar }
}

describe("authStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(httpClient.get).mockReset()
        vi.mocked(httpClient.post).mockReset()
        vi.mocked(realtimeService.start).mockReset()
        vi.mocked(realtimeService.stop).mockReset()
        vi.mocked(realtimeService.start).mockResolvedValue(undefined)
        vi.mocked(realtimeService.stop).mockResolvedValue(undefined)
    })

    // ────────────────────────────────────────────────────────────────────────────
    // init()
    // ────────────────────────────────────────────────────────────────────────────
    describe("init()", () => {
        it("GET /auth/me 200 → popula usuario e ativa realtime", async () => {
            const { notificacoesBindRealtime } = setupStoreMocks()
            const usuario = criarUsuario()
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { usuario } })

            const store = useAuthStore()
            await store.init()

            expect(store.usuario).toEqual(usuario)
            expect(store.isAuthenticated).toBe(true)
            expect(notificacoesBindRealtime).toHaveBeenCalled()
            expect(realtimeService.start).toHaveBeenCalled()
        })

        it("GET /auth/me 401 → tenta refresh → GET /auth/me 200 → usuario populado", async () => {
            const { notificacoesBindRealtime } = setupStoreMocks()
            const usuario = criarUsuario()
            const erro401 = { response: { status: 401 } }

            vi.mocked(httpClient.get)
                .mockRejectedValueOnce(erro401)     // tentativa 1 → 401
                .mockResolvedValueOnce({ data: { usuario } }) // tentativa 2 → OK
            vi.mocked(httpClient.post).mockResolvedValueOnce({}) // refresh OK

            const store = useAuthStore()
            await store.init()

            expect(store.usuario).toEqual(usuario)
            expect(httpClient.post).toHaveBeenCalledWith(
                "/auth/refresh",
                {},
                expect.objectContaining({ _noAutoRefresh: true }),
            )
            expect(notificacoesBindRealtime).toHaveBeenCalled()
        })

        it("GET /auth/me 401 → refresh 401 → usuario null, limparSessao chamado", async () => {
            const { tenantLimpar, profissionalLimpar, notificacoesLimpar, assinaturaLimpar } = setupStoreMocks()
            const erro401 = { response: { status: 401 } }

            vi.mocked(httpClient.get).mockRejectedValue(erro401)
            vi.mocked(httpClient.post).mockRejectedValueOnce(erro401) // refresh falha

            const store = useAuthStore()
            await store.init()

            expect(store.usuario).toBeNull()
            expect(store.isAuthenticated).toBe(false)
            expect(tenantLimpar).toHaveBeenCalled()
            expect(profissionalLimpar).toHaveBeenCalled()
            expect(notificacoesLimpar).toHaveBeenCalled()
            expect(assinaturaLimpar).toHaveBeenCalled()
            expect(realtimeService.stop).toHaveBeenCalled()
        })

        it("GET /auth/me 500 → limparSessao (não tenta refresh)", async () => {
            const { tenantLimpar } = setupStoreMocks()
            const erro500 = { response: { status: 500 } }
            vi.mocked(httpClient.get).mockRejectedValueOnce(erro500)

            const store = useAuthStore()
            await store.init()

            expect(store.usuario).toBeNull()
            // Não deve ter chamado refresh
            expect(httpClient.post).not.toHaveBeenCalled()
            expect(tenantLimpar).toHaveBeenCalled()
        })

        it("erro de rede (sem response) → limparSessao, não tenta refresh", async () => {
            const { tenantLimpar } = setupStoreMocks()
            vi.mocked(httpClient.get).mockRejectedValueOnce(new Error("Network Error"))

            const store = useAuthStore()
            await store.init()

            expect(store.usuario).toBeNull()
            expect(httpClient.post).not.toHaveBeenCalled()
            expect(tenantLimpar).toHaveBeenCalled()
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // login()
    // ────────────────────────────────────────────────────────────────────────────
    describe("login()", () => {
        it("POST /auth/login → bootstrapPosAuth (profissional.setProfissional, ativaRealtime)", async () => {
            const { profissionalSet, notificacoesBindRealtime } = setupStoreMocks()
            const usuario = criarUsuario()

            // login() agora chama POST /auth/login e depois bootstrapService.obter()
            // que faz GET /auth/bootstrap retornando { usuario, profissional, estabelecimentos }.
            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { token: "x" } })
            vi.mocked(httpClient.get).mockResolvedValueOnce({
                data: { usuario, profissional: null, estabelecimentos: [] },
            })

            const store = useAuthStore()
            const resultado = await store.login("joao@imedto.com", "senha123")

            expect(httpClient.post).toHaveBeenCalledWith("/auth/login", {
                email: "joao@imedto.com",
                password: "senha123",
            })
            expect(store.usuario).toEqual(usuario)
            expect(profissionalSet).toHaveBeenCalledWith(null)
            expect(notificacoesBindRealtime).toHaveBeenCalled()
            expect(realtimeService.start).toHaveBeenCalled()
            expect(resultado).toEqual({ token: "x" })
        })

        it("propaga erro se POST /auth/login falhar", async () => {
            setupStoreMocks()
            vi.mocked(httpClient.post).mockRejectedValueOnce(new Error("401"))

            const store = useAuthStore()

            await expect(store.login("joao@imedto.com", "errada")).rejects.toThrow("401")
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // logout()
    // ────────────────────────────────────────────────────────────────────────────
    describe("logout()", () => {
        it("POST /auth/logout → limparSessao (usuario null, realtime.stop chamado)", async () => {
            const { tenantLimpar, profissionalLimpar, notificacoesLimpar, assinaturaLimpar } = setupStoreMocks()
            vi.mocked(httpClient.post).mockResolvedValueOnce({})

            const store = useAuthStore()
            store.setUsuario(criarUsuario())
            await store.logout()

            expect(httpClient.post).toHaveBeenCalledWith("/auth/logout")
            expect(store.usuario).toBeNull()
            expect(tenantLimpar).toHaveBeenCalled()
            expect(profissionalLimpar).toHaveBeenCalled()
            expect(notificacoesLimpar).toHaveBeenCalled()
            expect(assinaturaLimpar).toHaveBeenCalled()
            expect(realtimeService.stop).toHaveBeenCalled()
        })

        it("limparSessao é chamado mesmo se POST /auth/logout falhar", async () => {
            const { tenantLimpar } = setupStoreMocks()
            vi.mocked(httpClient.post).mockRejectedValueOnce(new Error("503"))

            const store = useAuthStore()
            store.setUsuario(criarUsuario())

            // logout usa try/finally sem catch: o erro do POST é relançado,
            // mas o finally garante que limparSessao foi executado.
            await store.logout().catch(() => {})

            expect(store.usuario).toBeNull()
            expect(tenantLimpar).toHaveBeenCalled()
            expect(realtimeService.stop).toHaveBeenCalled()
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // signup()
    // ────────────────────────────────────────────────────────────────────────────
    describe("signup()", () => {
        it("requerConfirmacaoEmail: true → lança erro com flag requerConfirmacaoEmail", async () => {
            setupStoreMocks()
            vi.mocked(httpClient.post).mockResolvedValueOnce({
                data: { requerConfirmacaoEmail: true },
            })

            const store = useAuthStore()

            const erro = await store.signup("joao@imedto.com", "senha123").catch((e) => e)

            expect(erro).toBeInstanceOf(Error)
            expect(erro.message).toBe("confirm-email")
            expect((erro as any).requerConfirmacaoEmail).toBe(true)
        })

        it("signup normal → recarregaMe e popula usuario", async () => {
            setupStoreMocks()
            const usuario = criarUsuario()
            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { requerConfirmacaoEmail: false } })
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { usuario } })

            const store = useAuthStore()
            await store.signup("joao@imedto.com", "senha123")

            expect(store.usuario).toEqual(usuario)
        })

        it("propaga erro se POST /auth/signup falhar", async () => {
            setupStoreMocks()
            vi.mocked(httpClient.post).mockRejectedValueOnce(new Error("422"))

            const store = useAuthStore()

            await expect(store.signup("joao@imedto.com", "fraca")).rejects.toThrow("422")
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // Regressão: vazamento de sessão entre contas (logout/login na mesma aba)
    // ────────────────────────────────────────────────────────────────────────────
    describe("limpeza de sessão entre contas", () => {
        beforeEach(() => {
            sessionStorage.clear()
            localStorage.clear()
        })

        it("logout() limpa todas as chaves persistidas de sessão (sessionStorage + localStorage)", async () => {
            setupStoreMocks()

            // Simula estado herdado da Conta A
            sessionStorage.setItem("imedto.estabelecimentoAtivo", JSON.stringify({
                id: 1, nomeFantasia: "Clínica A", papel: "Dono", permissoes: [], permissoesExtras: [],
            }))
            localStorage.setItem("imedto.atendimento_ativo", JSON.stringify({
                agendamentoId: 99, pacienteId: 42, iniciadoEm: "2026-05-16T10:00:00.000Z",
            }))
            localStorage.setItem("imedto.receitas.v1", JSON.stringify([{ id: "r_x", pacienteId: 42 }]))
            localStorage.setItem("imedto-theme", "dark") // preferência neutra — NÃO deve ser apagada

            vi.mocked(httpClient.post).mockResolvedValueOnce({})

            const store = useAuthStore()
            store.setUsuario(criarUsuario())
            await store.logout()

            // O tenantStore real (não mockado neste cenário) limparia o sessionStorage,
            // mas como mockamos o store, validamos no nível do authStore: as chaves de
            // localStorage gerenciadas diretamente pelo limparSessao devem ter sumido.
            expect(localStorage.getItem("imedto.atendimento_ativo")).toBeNull()
            expect(localStorage.getItem("imedto.receitas.v1")).toBeNull()
            // Tema NÃO deve ser tocado — é preferência de UI neutra entre contas
            expect(localStorage.getItem("imedto-theme")).toBe("dark")
        })

        it("login() chama limparSessao ANTES de bootstrapPosAuth (estado herdado é zerado)", async () => {
            const { tenantLimpar, profissionalLimpar, notificacoesLimpar, assinaturaLimpar, profissionalSet } = setupStoreMocks()
            const usuario = criarUsuario()

            // Simula PII residual da Conta A em localStorage
            localStorage.setItem("imedto.atendimento_ativo", JSON.stringify({
                agendamentoId: 99, pacienteId: 42, iniciadoEm: "2026-05-16T10:00:00.000Z",
            }))
            localStorage.setItem("imedto.receitas.v1", JSON.stringify([{ id: "r_x" }]))

            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { token: "x" } })
            vi.mocked(httpClient.get).mockResolvedValueOnce({
                data: { usuario, profissional: null, estabelecimentos: [] },
            })

            const store = useAuthStore()
            await store.login("conta-b@imedto.com", "senha123")

            // limparSessao executado antes do bootstrap → stores limpos
            expect(tenantLimpar).toHaveBeenCalled()
            expect(profissionalLimpar).toHaveBeenCalled()
            expect(notificacoesLimpar).toHaveBeenCalled()
            expect(assinaturaLimpar).toHaveBeenCalled()
            // PII de paciente removida do localStorage
            expect(localStorage.getItem("imedto.atendimento_ativo")).toBeNull()
            expect(localStorage.getItem("imedto.receitas.v1")).toBeNull()
            // bootstrap rodou DEPOIS (hidratou o usuario novo)
            expect(store.usuario).toEqual(usuario)
            expect(profissionalSet).toHaveBeenCalledWith(null)
        })

        it("signup() (sem confirmação) também chama limparSessao antes do bootstrap", async () => {
            const { tenantLimpar } = setupStoreMocks()
            const usuario = criarUsuario()

            localStorage.setItem("imedto.atendimento_ativo", "{}")
            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { requerConfirmacaoEmail: false } })
            vi.mocked(httpClient.get).mockResolvedValueOnce({
                data: { usuario, profissional: null, estabelecimentos: [] },
            })

            const store = useAuthStore()
            await store.signup("novo@imedto.com", "senha123")

            expect(tenantLimpar).toHaveBeenCalled()
            expect(localStorage.getItem("imedto.atendimento_ativo")).toBeNull()
        })

        it("aceitarConvite() também chama limparSessao antes do bootstrap", async () => {
            const { tenantLimpar } = setupStoreMocks()
            const usuario = criarUsuario()

            localStorage.setItem("imedto.receitas.v1", "[]")
            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: {} })
            vi.mocked(httpClient.get).mockResolvedValueOnce({
                data: { usuario, profissional: null, estabelecimentos: [] },
            })

            const store = useAuthStore()
            await store.aceitarConvite("tok", "convidado@imedto.com", "senha123")

            expect(tenantLimpar).toHaveBeenCalled()
            expect(localStorage.getItem("imedto.receitas.v1")).toBeNull()
        })

        it("limparSessao não quebra se localStorage lançar (modo privado / quota)", async () => {
            setupStoreMocks()
            const original = Storage.prototype.removeItem
            Storage.prototype.removeItem = vi.fn().mockImplementation(() => {
                throw new Error("QuotaExceeded")
            })

            vi.mocked(httpClient.post).mockResolvedValueOnce({})

            const store = useAuthStore()
            store.setUsuario(criarUsuario())

            await expect(store.logout()).resolves.toBeUndefined()
            expect(store.usuario).toBeNull()

            Storage.prototype.removeItem = original
        })

        it("recarregarMe() força limparSessao se o id do usuário muda (defense-in-depth contra relogin sem logout)", async () => {
            const { tenantLimpar } = setupStoreMocks()
            const store = useAuthStore()

            // Estado inicial: Médico (Conta A)
            const medico = criarUsuario({ id: "user-a-medico" })
            store.setUsuario(medico)
            localStorage.setItem("imedto.atendimento_ativo", "{}")

            // Servidor agora responde com OUTRA identidade (Conta B fez login
            // sem que o app tenha passado por logout — cenário: cookies trocados
            // externamente, login programático bypassando authStore.login()).
            const dono = criarUsuario({ id: "user-b-dono", email: "dono@imedto.com" })
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { usuario: dono } })

            await store.recarregarMe()

            // Detectou troca → limparSessao foi chamado ANTES de setar o novo usuario.
            expect(tenantLimpar).toHaveBeenCalled()
            expect(localStorage.getItem("imedto.atendimento_ativo")).toBeNull()
            // Estado final: usuário novo já hidratado.
            expect(store.usuario).toEqual(dono)
        })

        it("recarregarMe() NÃO limpa sessão quando o id é o mesmo (refresh de dados)", async () => {
            const { tenantLimpar } = setupStoreMocks()
            const store = useAuthStore()

            const usuario = criarUsuario({ id: "user-a", nomeCompleto: "Antigo" })
            store.setUsuario(usuario)

            // Mesmo id, apenas nomeCompleto atualizado
            const atualizado = criarUsuario({ id: "user-a", nomeCompleto: "Novo Nome" })
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { usuario: atualizado } })

            await store.recarregarMe()

            expect(tenantLimpar).not.toHaveBeenCalled()
            expect(store.usuario?.nomeCompleto).toBe("Novo Nome")
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // setUsuario()
    // ────────────────────────────────────────────────────────────────────────────
    describe("setUsuario()", () => {
        it("setUsuario(null) → isAuthenticated = false", () => {
            setupStoreMocks()
            const store = useAuthStore()
            store.setUsuario(criarUsuario())
            store.setUsuario(null)

            expect(store.isAuthenticated).toBe(false)
            expect(store.usuario).toBeNull()
        })

        it("setUsuario(usuario) → isAuthenticated = true", () => {
            setupStoreMocks()
            const store = useAuthStore()
            const usuario = criarUsuario()
            store.setUsuario(usuario)

            expect(store.isAuthenticated).toBe(true)
            expect(store.usuario).toEqual(usuario)
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // alterarSenha() — Correção 2 (troca de senha autenticada)
    // ────────────────────────────────────────────────────────────────────────────
    describe("alterarSenha()", () => {
        it("POST /auth/alterar-senha com senhaAtual e novaSenha", async () => {
            setupStoreMocks()
            vi.mocked(httpClient.post).mockResolvedValueOnce({})

            const store = useAuthStore()
            await store.alterarSenha("atual123", "nova456789")

            expect(httpClient.post).toHaveBeenCalledWith("/auth/alterar-senha", {
                senhaAtual: "atual123",
                novaSenha: "nova456789",
            })
        })

        it("propaga erro 422 do backend com mensagem", async () => {
            setupStoreMocks()
            const erro422 = {
                response: { status: 422, data: { mensagem: "Senha atual incorreta." } },
            }
            vi.mocked(httpClient.post).mockRejectedValueOnce(erro422)

            const store = useAuthStore()

            const capturado = await store.alterarSenha("errada", "nova456789").catch((e) => e)
            expect(capturado).toBe(erro422)
            expect(capturado.response.data.mensagem).toBe("Senha atual incorreta.")
        })

        it("não altera estado de sessão (usuario continua válido)", async () => {
            // O backend revoga sessões — mas o cookie atual fica válido até o próximo refresh.
            // O store só faz POST; cabe à UI decidir se chama logout.
            setupStoreMocks()
            const usuario = criarUsuario()
            vi.mocked(httpClient.post).mockResolvedValueOnce({})

            const store = useAuthStore()
            store.setUsuario(usuario)
            await store.alterarSenha("a", "novaforte123")

            expect(store.usuario).toEqual(usuario)
            expect(store.isAuthenticated).toBe(true)
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // onboardingPendente computed
    // ────────────────────────────────────────────────────────────────────────────
    describe("onboardingPendente", () => {
        it("é true quando onboardingCompleto = false", () => {
            setupStoreMocks()
            const store = useAuthStore()
            store.setUsuario(criarUsuario({ onboardingCompleto: false }))

            expect(store.onboardingPendente).toBe(true)
        })

        it("é false quando onboardingCompleto = true", () => {
            setupStoreMocks()
            const store = useAuthStore()
            store.setUsuario(criarUsuario({ onboardingCompleto: true }))

            expect(store.onboardingPendente).toBe(false)
        })

        it("é false quando usuario é null", () => {
            setupStoreMocks()
            const store = useAuthStore()

            expect(store.onboardingPendente).toBe(false)
        })
    })
})
