import { describe, it, expect, beforeEach, vi, afterEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"

// Mock do adminApi — isola store do HTTP.
vi.mock("../services/adminApi", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
    },
}))

import adminApi from "../services/adminApi"
import { useAdminAuthStore } from "./adminAuthStore"
import type { AdminInfo } from "./adminAuthStore"

function criarAdmin(overrides: Partial<AdminInfo> = {}): AdminInfo {
    return {
        id: "admin-uuid-1",
        email: "admin@imedto.com",
        nome: "Admin Teste",
        ativo: true,
        forcePasswordReset: false,
        ultimoLoginEm: null,
        ...overrides,
    }
}

describe("adminAuthStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
        vi.useFakeTimers()
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    // ── init() ────────────────────────────────────────────────────────────────

    describe("init()", () => {
        it("popula admin quando /me retorna ok", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.get).mockResolvedValueOnce({ data: adminData })

            const store = useAdminAuthStore()
            await store.init()

            expect(store.admin).toEqual(adminData)
            expect(store.isAuthenticated).toBe(true)
        })

        it("tenta refresh quando /me retorna 401", async () => {
            const adminData = criarAdmin()
            // Primeira chamada a /me → 401
            vi.mocked(adminApi.get).mockRejectedValueOnce({
                response: { status: 401 },
            })
            // Refresh ok
            vi.mocked(adminApi.post).mockResolvedValueOnce({})
            // Segunda chamada a /me (pós-refresh) → ok
            vi.mocked(adminApi.get).mockResolvedValueOnce({ data: adminData })

            const store = useAdminAuthStore()
            await store.init()

            expect(store.admin).toEqual(adminData)
            expect(vi.mocked(adminApi.post)).toHaveBeenCalledWith(
                "/auth/refresh",
                {},
                expect.objectContaining({ _noAutoRefresh: true }),
            )
        })

        it("limpa sessão quando refresh falha", async () => {
            vi.mocked(adminApi.get).mockRejectedValueOnce({ response: { status: 401 } })
            vi.mocked(adminApi.post).mockRejectedValueOnce(new Error("refresh falhou"))

            const store = useAdminAuthStore()
            await store.init()

            expect(store.admin).toBeNull()
            expect(store.isAuthenticated).toBe(false)
        })
    })

    // ── login() ───────────────────────────────────────────────────────────────

    describe("login()", () => {
        it("popula admin após login bem-sucedido", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } })

            const store = useAdminAuthStore()
            const result = await store.login("admin@imedto.com", "Senha@123!")

            expect(result).toEqual(adminData)
            expect(store.admin).toEqual(adminData)
            expect(store.isAuthenticated).toBe(true)
        })

        it("mustResetPassword retorna true quando forcePasswordReset = true", async () => {
            const adminData = criarAdmin({ forcePasswordReset: true })
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } })

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")

            expect(store.mustResetPassword).toBe(true)
        })
    })

    // ── logout() ──────────────────────────────────────────────────────────────

    describe("logout()", () => {
        it("limpa admin e chama POST /auth/logout", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } })
            vi.mocked(adminApi.post).mockResolvedValueOnce({}) // logout

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")
            await store.logout()

            expect(store.admin).toBeNull()
            expect(store.isAuthenticated).toBe(false)
        })

        it("limpa admin mesmo se POST /auth/logout falhar", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } })
            vi.mocked(adminApi.post).mockRejectedValueOnce(new Error("network"))

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")
            // logout usa try/finally — não deve lançar mesmo com erro de rede
            await expect(store.logout()).resolves.toBeUndefined()

            expect(store.admin).toBeNull()
        })
    })

    // ── changePassword() ──────────────────────────────────────────────────────

    describe("changePassword()", () => {
        it("força-reset: chama /auth/change-password SEM senhaAtual (parâmetro omitido)", async () => {
            const adminData = criarAdmin({ forcePasswordReset: true })
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } }) // login
            vi.mocked(adminApi.post).mockResolvedValueOnce({}) // change-password
            vi.mocked(adminApi.get).mockResolvedValueOnce({
                data: { ...adminData, forcePasswordReset: false },
            }) // me pós-troca

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")
            // CA6 — força-reset: omite senhaAtual (undefined → não enviado no body ou enviado como undefined)
            await store.changePassword("NovaSenha@456!")

            expect(vi.mocked(adminApi.post)).toHaveBeenCalledWith(
                "/auth/change-password",
                { novaSenha: "NovaSenha@456!", senhaAtual: undefined },
            )
            expect(store.admin?.forcePasswordReset).toBe(false)
        })

        it("troca voluntária: chama /auth/change-password COM senhaAtual", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } }) // login
            vi.mocked(adminApi.post).mockResolvedValueOnce({}) // change-password
            vi.mocked(adminApi.get).mockResolvedValueOnce({
                data: { ...adminData },
            }) // me pós-troca

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")
            // CA1 / CA7 — troca voluntária: passa senhaAtual
            await store.changePassword("NovaSenha@456!", "SenhaAtual@123!")

            expect(vi.mocked(adminApi.post)).toHaveBeenCalledWith(
                "/auth/change-password",
                { novaSenha: "NovaSenha@456!", senhaAtual: "SenhaAtual@123!" },
            )
        })

        it("reidrata me após troca bem-sucedida", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValueOnce({ data: { admin: adminData } }) // login
            vi.mocked(adminApi.post).mockResolvedValueOnce({}) // change-password
            vi.mocked(adminApi.get).mockResolvedValueOnce({
                data: { ...adminData, forcePasswordReset: false },
            }) // me pós-troca

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")
            await store.changePassword("NovaSenha@456!", "SenhaAtual@123!")

            expect(vi.mocked(adminApi.get)).toHaveBeenCalledWith("/auth/me")
        })
    })

    // ── inatividade ───────────────────────────────────────────────────────────

    describe("timer de inatividade", () => {
        it("inicia timer após login e limpa sessão após 15min", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValue({ data: { admin: adminData } })

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")
            expect(store.isAuthenticated).toBe(true)

            // Avança 15 minutos — timer dispara (async)
            await vi.runAllTimersAsync()

            expect(store.admin).toBeNull()
        })

        it("resetarTimerInatividade reinicia o timer", async () => {
            const adminData = criarAdmin()
            vi.mocked(adminApi.post).mockResolvedValue({ data: { admin: adminData } })

            const store = useAdminAuthStore()
            await store.login("admin@imedto.com", "Senha@123!")

            // Avança 14min — timer não disparou ainda
            vi.advanceTimersByTime(14 * 60 * 1000)
            store.resetarTimerInatividade()
            // Avança mais 14min — ainda dentro da janela de 15min do timer resetado
            vi.advanceTimersByTime(14 * 60 * 1000)

            expect(store.admin).not.toBeNull()
        })
    })
})
