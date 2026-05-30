import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"

// Mock do service antes do import do store
vi.mock("../services/adminsService", () => ({
    adminsService: {
        listar: vi.fn(),
        criar: vi.fn(),
        desativar: vi.fn(),
        reativar: vi.fn(),
        resetarSenha: vi.fn(),
    },
}))

import { adminsService } from "../services/adminsService"
import { useAdminsStore } from "./adminsStore"

const mockListar = adminsService.listar as ReturnType<typeof vi.fn>
const mockCriar = adminsService.criar as ReturnType<typeof vi.fn>
const mockDesativar = adminsService.desativar as ReturnType<typeof vi.fn>
const mockReativar = adminsService.reativar as ReturnType<typeof vi.fn>
const mockResetarSenha = adminsService.resetarSenha as ReturnType<typeof vi.fn>

function adminFake(overrides = {}) {
    return {
        id: "uuid-admin",
        email: "a@imedto.com",
        nome: "Admin Teste",
        ativo: true,
        forcePasswordReset: false,
        criadoEm: "2026-01-01T00:00:00Z",
        ultimoLoginEm: null,
        ...overrides,
    }
}

describe("adminsStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("carregar", () => {
        it("popula itens e total com retorno do service", async () => {
            const store = useAdminsStore()
            mockListar.mockResolvedValue({
                itens: [adminFake()],
                total: 1,
                pagina: 1,
                tamanho: 25,
            })

            await store.carregar()

            expect(store.itens).toHaveLength(1)
            expect(store.total).toBe(1)
            expect(store.carregando).toBe(false)
            expect(store.erro).toBeNull()
        })

        it("define erro em caso de falha do service", async () => {
            const store = useAdminsStore()
            mockListar.mockRejectedValue({
                response: { data: { mensagem: "Acesso negado." } },
            })

            await store.carregar()

            expect(store.itens).toHaveLength(0)
            expect(store.erro).toBe("Acesso negado.")
        })

        it("passa busca para o service", async () => {
            const store = useAdminsStore()
            mockListar.mockResolvedValue({ itens: [], total: 0, pagina: 1, tamanho: 25 })

            await store.carregar("joao")

            expect(mockListar).toHaveBeenCalledWith(
                expect.objectContaining({ busca: "joao" }),
            )
        })
    })

    describe("criar", () => {
        it("retorna resultado com senhaTemporaria e armazena ultimoCriado", async () => {
            const store = useAdminsStore()
            const resultado = {
                id: "uuid-novo",
                email: "novo@imedto.com",
                nome: "Novo Admin",
                senhaTemporaria: "SenhaTmpAleatorio123!",
            }
            mockCriar.mockResolvedValue(resultado)

            const r = await store.criar("Novo Admin", "novo@imedto.com", "Motivo suficiente aqui")

            expect(r.senhaTemporaria).toBe("SenhaTmpAleatorio123!")
            expect(store.ultimoCriado).toEqual(resultado)
        })

        it("propaga erro do service (e.g. 422 e-mail duplicado)", async () => {
            const store = useAdminsStore()
            const erroApi = {
                response: { data: { mensagem: "Já existe um admin com este e-mail." } },
            }
            mockCriar.mockRejectedValue(erroApi)

            await expect(store.criar("X", "dup@x.com", "Motivo suficiente aqui")).rejects.toEqual(erroApi)
        })
    })

    describe("desativar", () => {
        it("chama service com id e motivo", async () => {
            const store = useAdminsStore()
            mockDesativar.mockResolvedValue(undefined)

            await store.desativar("uuid-1", "Saiu da empresa")

            expect(mockDesativar).toHaveBeenCalledWith("uuid-1", "Saiu da empresa")
        })
    })

    describe("reativar", () => {
        it("chama service com id e motivo", async () => {
            const store = useAdminsStore()
            mockReativar.mockResolvedValue(undefined)

            await store.reativar("uuid-1", "Retornou para o time")

            expect(mockReativar).toHaveBeenCalledWith("uuid-1", "Retornou para o time")
        })
    })

    describe("resetarSenha", () => {
        it("retorna senha temporária e armazena ultimaSenhaResetada", async () => {
            const store = useAdminsStore()
            mockResetarSenha.mockResolvedValue({ senhaTemporaria: "NovaSenha123!@#" })

            const senha = await store.resetarSenha("uuid-1", "Motivo de reset aqui")

            expect(senha).toBe("NovaSenha123!@#")
            expect(store.ultimaSenhaResetada).toBe("NovaSenha123!@#")
        })
    })

    describe("limparUltimoCriado / limparUltimaSenhaResetada", () => {
        it("zera ultimoCriado", async () => {
            const store = useAdminsStore()
            mockCriar.mockResolvedValue({ id: "x", email: "e", nome: "n", senhaTemporaria: "s" })
            await store.criar("n", "e@x.com", "Motivo suficiente aqui")

            store.limparUltimoCriado()

            expect(store.ultimoCriado).toBeNull()
        })

        it("zera ultimaSenhaResetada", async () => {
            const store = useAdminsStore()
            mockResetarSenha.mockResolvedValue({ senhaTemporaria: "abc" })
            await store.resetarSenha("id", "motivo longo o suficiente")

            store.limparUltimaSenhaResetada()

            expect(store.ultimaSenhaResetada).toBeNull()
        })
    })
})
