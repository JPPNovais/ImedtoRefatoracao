import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"

// assinaturaStore é importado dinamicamente dentro de selecionar() — mockar para evitar side effects.
vi.mock("@/stores/assinaturaStore", () => ({
    useAssinaturaStore: vi.fn(() => ({ limpar: vi.fn() })),
}))

import { useTenantStore, type EstabelecimentoAtivo } from "@/stores/tenantStore"

function criarEstabelecimento(overrides: Partial<EstabelecimentoAtivo> = {}): EstabelecimentoAtivo {
    return {
        id: 1,
        nomeFantasia: "Clínica Teste",
        papel: "Dono",
        permissoes: [],
        permissoesExtras: [],
        ...overrides,
    }
}

describe("tenantStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        // Limpar sessionStorage antes de cada teste para isolar estado persistido.
        sessionStorage.clear()
    })

    afterEach(() => {
        sessionStorage.clear()
        vi.restoreAllMocks()
    })

    // ────────────────────────────────────────────────────────────────────────────
    // Estado inicial
    // ────────────────────────────────────────────────────────────────────────────
    describe("estado inicial", () => {
        it("ativo é null quando sessionStorage está vazio", () => {
            const store = useTenantStore()

            expect(store.ativo).toBeNull()
            expect(store.estabelecimentoAtivoId).toBeNull()
            expect(store.temTenantSelecionado).toBe(false)
            expect(store.semEstabelecimento).toBe(false)
            expect(store.papel).toBeNull()
        })

        it("reidrata ativo do sessionStorage quando já existe valor salvo", () => {
            const estab = criarEstabelecimento({ id: 99, nomeFantasia: "Clínica Salva" })
            sessionStorage.setItem("imedto.estabelecimentoAtivo", JSON.stringify(estab))

            // Criar a pinia depois de popular o sessionStorage simula o reload do browser
            setActivePinia(createPinia())
            const store = useTenantStore()

            expect(store.ativo).toEqual(estab)
            expect(store.estabelecimentoAtivoId).toBe(99)
            expect(store.temTenantSelecionado).toBe(true)
        })

        it("ativo é null se sessionStorage contém JSON inválido", () => {
            sessionStorage.setItem("imedto.estabelecimentoAtivo", "nao-e-json")
            setActivePinia(createPinia())
            const store = useTenantStore()

            expect(store.ativo).toBeNull()
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // selecionar()
    // ────────────────────────────────────────────────────────────────────────────
    describe("selecionar()", () => {
        it("seta ativo e persiste no sessionStorage", () => {
            const store = useTenantStore()
            const estab = criarEstabelecimento({ id: 5, nomeFantasia: "Clínica Nova" })

            store.selecionar(estab)

            expect(store.ativo).toEqual(estab)
            expect(store.estabelecimentoAtivoId).toBe(5)
            expect(store.temTenantSelecionado).toBe(true)
            expect(store.semEstabelecimento).toBe(false)
            expect(sessionStorage.getItem("imedto.estabelecimentoAtivo")).toBe(JSON.stringify(estab))
        })

        it("expõe papel correto após selecionar", () => {
            const store = useTenantStore()
            store.selecionar(criarEstabelecimento({ papel: "Profissional" }))

            expect(store.papel).toBe("Profissional")
        })

        it("trocar para o mesmo id não limpa assinaturaStore", async () => {
            // Arrange: configurar mock antes de qualquer selecionar()
            const mod = await import("@/stores/assinaturaStore")
            const limpar = vi.fn()
            vi.mocked(mod.useAssinaturaStore).mockReturnValue({ limpar } as any)

            const store = useTenantStore()
            const estab = criarEstabelecimento({ id: 10 })

            // Primeira seleção estabelece o id atual
            store.selecionar(estab)
            await Promise.resolve()
            limpar.mockClear()

            // Seleciona o mesmo id novamente → trocouEstab = false → NÃO limpa
            store.selecionar(estab)
            await Promise.resolve()

            expect(limpar).not.toHaveBeenCalled()
        })

        it("trocar para id diferente limpa assinaturaStore", async () => {
            // Arrange: configurar mock antes de qualquer selecionar()
            const mod = await import("@/stores/assinaturaStore")
            const limpar = vi.fn()
            vi.mocked(mod.useAssinaturaStore).mockReturnValue({ limpar } as any)

            const store = useTenantStore()

            // Primeira seleção (id=1) para estabelecer o estado anterior
            store.selecionar(criarEstabelecimento({ id: 1 }))
            await Promise.resolve()
            limpar.mockClear()

            // Troca para id diferente → trocouEstab = true → DEVE chamar limpar()
            store.selecionar(criarEstabelecimento({ id: 2, nomeFantasia: "Outra Clínica" }))

            // O `void import(...).then(...)` é microtask + Promise.then — aguarda a fila
            await new Promise((r) => setTimeout(r, 0))

            expect(limpar).toHaveBeenCalled()
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // limpar()
    // ────────────────────────────────────────────────────────────────────────────
    describe("limpar()", () => {
        it("zera ativo, semEstabelecimento e remove do sessionStorage", () => {
            const store = useTenantStore()
            store.selecionar(criarEstabelecimento())

            store.limpar()

            expect(store.ativo).toBeNull()
            expect(store.estabelecimentoAtivoId).toBeNull()
            expect(store.temTenantSelecionado).toBe(false)
            expect(store.semEstabelecimento).toBe(false)
            expect(sessionStorage.getItem("imedto.estabelecimentoAtivo")).toBeNull()
        })

        it("limpar() é idempotente quando já está zerado", () => {
            const store = useTenantStore()

            // Não deve lançar erro
            expect(() => {
                store.limpar()
                store.limpar()
            }).not.toThrow()

            expect(store.ativo).toBeNull()
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // resolverTenant()
    // ────────────────────────────────────────────────────────────────────────────
    describe("resolverTenant()", () => {
        it("não chama listar se já há tenant ativo", async () => {
            const store = useTenantStore()
            store.selecionar(criarEstabelecimento({ id: 7 }))
            const listar = vi.fn()

            await store.resolverTenant(listar)

            expect(listar).not.toHaveBeenCalled()
            expect(store.estabelecimentoAtivoId).toBe(7)
        })

        it("lista vazia → semEstabelecimento = true", async () => {
            const store = useTenantStore()
            const listar = vi.fn().mockResolvedValue([])

            await store.resolverTenant(listar)

            expect(store.semEstabelecimento).toBe(true)
            expect(store.ativo).toBeNull()
        })

        it("1 estabelecimento → auto-seleciona", async () => {
            const store = useTenantStore()
            const listar = vi.fn().mockResolvedValue([
                { id: 3, nomeFantasia: "Clínica Auto", papelDoUsuario: "Dono" },
            ])

            await store.resolverTenant(listar)

            expect(store.estabelecimentoAtivoId).toBe(3)
            expect(store.ativo?.nomeFantasia).toBe("Clínica Auto")
            expect(store.ativo?.papel).toBe("Dono")
            expect(store.semEstabelecimento).toBe(false)
        })

        it("2+ estabelecimentos → seleciona o primeiro", async () => {
            const store = useTenantStore()
            const listar = vi.fn().mockResolvedValue([
                { id: 10, nomeFantasia: "Primeiro", papelDoUsuario: "Dono" },
                { id: 20, nomeFantasia: "Segundo", papelDoUsuario: "Profissional" },
            ])

            await store.resolverTenant(listar)

            expect(store.estabelecimentoAtivoId).toBe(10)
        })

        it("listar lança erro → silencia, tenant permanece nulo", async () => {
            const store = useTenantStore()
            const listar = vi.fn().mockRejectedValue(new Error("503"))

            await expect(store.resolverTenant(listar)).resolves.toBeUndefined()

            expect(store.ativo).toBeNull()
            expect(store.semEstabelecimento).toBe(false)
        })
    })

    // ────────────────────────────────────────────────────────────────────────────
    // computeds derivados
    // ────────────────────────────────────────────────────────────────────────────
    describe("computeds", () => {
        it("temTenantSelecionado é false quando ativo null; true quando selecionado", () => {
            const store = useTenantStore()
            expect(store.temTenantSelecionado).toBe(false)

            store.selecionar(criarEstabelecimento())
            expect(store.temTenantSelecionado).toBe(true)
        })

        it("estabelecimentoAtivoId retorna o id do ativo ou null", () => {
            const store = useTenantStore()
            expect(store.estabelecimentoAtivoId).toBeNull()

            store.selecionar(criarEstabelecimento({ id: 55 }))
            expect(store.estabelecimentoAtivoId).toBe(55)
        })
    })
})
