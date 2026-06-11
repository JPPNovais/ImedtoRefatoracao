/**
 * Testes da proximosPassosStore — addendum 2 (CA202–CA215).
 *
 * Cobre:
 *  CA203 — reidratação de sessionStorage ao chamar reidratar()
 *  CA203 — sem sessionStorage, store começa fechada
 *  CA204 — atualizarAbertas detecta "tudo concluído" e transiciona para "concluido"
 *  CA205 — atualizarAbertas faz fetch com pacienteId correto
 *  CA206 — fechar limpa memória + sessionStorage
 *  CA211 — limpar() limpa estado e sessionStorage (usado por authStore.limparSessao)
 *  CA212 — limpar() usado na troca de estabelecimento
 *  R24   — iniciar() substitui estado anterior (uma evolução por vez)
 *  R26   — persistência em sessionStorage (não localStorage)
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { useProximosPassosStore } from "./proximosPassosStore"

// ── Mock pendenciaService ─────────────────────────────────────────────────────
vi.mock("@/services/pendenciaService", async () => {
    const actual = await vi.importActual("@/services/pendenciaService")
    return {
        ...(actual as object),
        pendenciaService: {
            listarAbertas: vi.fn().mockResolvedValue([]),
        },
    }
})

import { pendenciaService } from "@/services/pendenciaService"

function mockListar() {
    return vi.mocked(pendenciaService.listarAbertas)
}

// ── Helpers para sessionStorage simulado ─────────────────────────────────────

const CHAVE = "imedto.proximosPassos"

function limparSessionStorage() {
    sessionStorage.removeItem(CHAVE)
}

function gravarSessionStorage(dados: object) {
    sessionStorage.setItem(CHAVE, JSON.stringify(dados))
}

// ── Setup ─────────────────────────────────────────────────────────────────────

beforeEach(() => {
    setActivePinia(createPinia())
    limparSessionStorage()
    mockListar().mockResolvedValue([])
    vi.clearAllMocks()
})

// ── Testes ────────────────────────────────────────────────────────────────────

describe("proximosPassosStore", () => {

    // R24 ────────────────────────────────────────────────────────────────────
    it("R24 — iniciar() popula estado e transiciona para expandido", async () => {
        // Garante que há pendências abertas para que o estado não vá para "concluido"
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 7, acao: "CriarReceita",   status: "Pendente", criadoEm: "" },
            { id: 2, evolucaoId: 7, acao: "AgendarRetorno", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()

        await store.iniciar({
            pacienteId: 42,
            evolucaoId: 7,
            acoesMarcadas: ["CriarReceita", "AgendarRetorno"],
        })

        expect(store.pacienteId).toBe(42)
        expect(store.evolucaoId).toBe(7)
        expect(store.acoesMarcadas).toEqual(["CriarReceita", "AgendarRetorno"])
        expect(store.estado).toBe("expandido")
        expect(store.visivel).toBe(true)
    })

    it("R24 — iniciar() substitui evolução anterior (uma por vez)", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 1, acao: "CriarReceita",   status: "Pendente", criadoEm: "" },
            { id: 2, evolucaoId: 2, acao: "AgendarRetorno", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()

        await store.iniciar({ pacienteId: 1, evolucaoId: 1, acoesMarcadas: ["CriarReceita"] })
        await store.iniciar({ pacienteId: 2, evolucaoId: 2, acoesMarcadas: ["AgendarRetorno"] })

        expect(store.pacienteId).toBe(2)
        expect(store.acoesMarcadas).toEqual(["AgendarRetorno"])
    })

    // R26 ────────────────────────────────────────────────────────────────────
    it("R26 — iniciar() persiste em sessionStorage (não localStorage)", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 7, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()

        await store.iniciar({
            pacienteId: 42,
            evolucaoId: 7,
            acoesMarcadas: ["CriarReceita"],
        })

        const raw = sessionStorage.getItem(CHAVE)
        expect(raw).not.toBeNull()
        const dados = JSON.parse(raw!)
        expect(dados.pacienteId).toBe(42)
        expect(dados.evolucaoId).toBe(7)

        // Não pode usar localStorage
        expect(localStorage.getItem(CHAVE)).toBeNull()
    })

    // CA203 ──────────────────────────────────────────────────────────────────
    it("CA203 — reidratar() restaura estado do sessionStorage", () => {
        gravarSessionStorage({
            pacienteId: 99,
            evolucaoId: 10,
            acoesMarcadas: ["AgendarRetorno"],
            estado: "minimizado",
        })

        const store = useProximosPassosStore()
        store.reidratar()

        expect(store.pacienteId).toBe(99)
        expect(store.evolucaoId).toBe(10)
        expect(store.acoesMarcadas).toEqual(["AgendarRetorno"])
        expect(store.estado).toBe("minimizado")
        expect(store.visivel).toBe(true)
    })

    it("CA203 — reidratar() sem sessionStorage mantém estado fechado", () => {
        const store = useProximosPassosStore()
        store.reidratar()

        expect(store.visivel).toBe(false)
        expect(store.pacienteId).toBeNull()
    })

    it("CA203 — reidratar() com estado 'expandido' restaura expandido", () => {
        gravarSessionStorage({
            pacienteId: 5,
            evolucaoId: undefined,
            acoesMarcadas: ["CriarReceita"],
            estado: "expandido",
        })
        const store = useProximosPassosStore()
        store.reidratar()

        expect(store.estado).toBe("expandido")
    })

    // CA204 ──────────────────────────────────────────────────────────────────
    it("CA204 — atualizarAbertas transiciona para 'concluido' quando tudo está concluído", async () => {
        const store = useProximosPassosStore()
        // Inicia com 2 ações; fetch retorna lista vazia (ambas concluídas).
        await store.iniciar({
            pacienteId: 42,
            acoesMarcadas: ["CriarReceita", "AgendarRetorno"],
        })
        // Após iniciar, fetch já rodou. Para testar a lógica isolada:
        mockListar().mockResolvedValue([])
        await store.atualizarAbertas()

        expect(store.concluidas).toBe(2)
        expect(store.estado).toBe("concluido")
    })

    it("CA204 — atualizarAbertas NÃO transiciona quando ainda há aberta na mesma evolução", async () => {
        const store = useProximosPassosStore()
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        await store.iniciar({
            pacienteId: 42,
            evolucaoId: 5,
            acoesMarcadas: ["CriarReceita", "AgendarRetorno"],
        })

        expect(store.estado).toBe("expandido")
    })

    // CA205 ──────────────────────────────────────────────────────────────────
    it("CA205 — atualizarAbertas chama listarAbertas com pacienteId correto", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 77, acoesMarcadas: ["CriarReceita"] })
        mockListar().mockClear()
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])

        await store.atualizarAbertas()

        expect(mockListar()).toHaveBeenCalledWith(77)
    })

    it("CA205 — atualizarAbertas sem pacienteId não faz fetch", async () => {
        const store = useProximosPassosStore()
        mockListar().mockClear()

        await store.atualizarAbertas()

        expect(mockListar()).not.toHaveBeenCalled()
    })

    // CA206 / fechar ─────────────────────────────────────────────────────────
    it("CA206 — fechar() limpa memória e sessionStorage", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, acoesMarcadas: ["CriarReceita"] })

        store.fechar()

        expect(store.visivel).toBe(false)
        expect(store.pacienteId).toBeNull()
        expect(sessionStorage.getItem(CHAVE)).toBeNull()
    })

    // CA211 / CA212 — limpar() ───────────────────────────────────────────────
    it("CA211 — limpar() apaga estado e sessionStorage (logout)", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, acoesMarcadas: ["CriarReceita"] })

        store.limpar()

        expect(store.visivel).toBe(false)
        expect(store.estado).toBe("fechado")
        expect(sessionStorage.getItem(CHAVE)).toBeNull()
    })

    it("CA212 — limpar() pode ser chamado sem estado ativo (troca de estab sem widget)", () => {
        const store = useProximosPassosStore()
        // Não lança mesmo sem estado ativo.
        expect(() => store.limpar()).not.toThrow()
        expect(store.visivel).toBe(false)
    })

    // minimizar / expandir ───────────────────────────────────────────────────
    it("minimizar() transiciona para minimizado e persiste", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, evolucaoId: 5, acoesMarcadas: ["CriarReceita"] })
        store.minimizar()

        expect(store.estado).toBe("minimizado")
        const raw = sessionStorage.getItem(CHAVE)
        expect(JSON.parse(raw!).estado).toBe("minimizado")
    })

    it("expandir() transiciona para expandido e persiste", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, evolucaoId: 5, acoesMarcadas: ["CriarReceita"] })
        store.minimizar()
        store.expandir()

        expect(store.estado).toBe("expandido")
        const raw = sessionStorage.getItem(CHAVE)
        expect(JSON.parse(raw!).estado).toBe("expandido")
    })

    // temAberta ──────────────────────────────────────────────────────────────
    it("temAberta é false quando abertas não inclui nenhuma acão marcada", async () => {
        const store = useProximosPassosStore()
        // Nenhuma ação aberta — mas como acoesMarcadas é ["AgendarRetorno"] e abertas não inclui essa ação:
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        await store.iniciar({ pacienteId: 42, acoesMarcadas: ["AgendarRetorno"] })
        // AgendarRetorno não está em abertas → temAberta = false

        expect(store.temAberta).toBe(false)
    })

    it("temAberta é true quando há ao menos uma ação marcada ainda aberta na mesma evolução", async () => {
        mockListar().mockResolvedValue([
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, evolucaoId: 5, acoesMarcadas: ["CriarReceita"] })

        expect(store.temAberta).toBe(true)
    })

    // Regressão QA — pendência de evolução diferente não mascara conclusão ──────
    it("REGRESSÃO — concluidas=1 quando CriarReceita aberta pertence à evolucaoId 56, não à 57", async () => {
        // Cenário exato prescrito pelo QA:
        //   acoesMarcadas=["CriarReceita"], evolucaoId=57,
        //   abertas=[{acao:"CriarReceita", evolucaoId:56}]
        //   → esperado: concluidas === 1 (a ação está concluída na evolução correta)
        mockListar().mockResolvedValue([
            { id: 10, evolucaoId: 56, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, evolucaoId: 57, acoesMarcadas: ["CriarReceita"] })

        expect(store.concluidas).toBe(1)
        expect(store.temAberta).toBe(false)
        expect(store.estaConcluidaAcao("CriarReceita")).toBe(true)
    })

    it("REGRESSÃO — concluidas=0 quando CriarReceita aberta pertence à mesma evolucaoId 57", async () => {
        mockListar().mockResolvedValue([
            { id: 10, evolucaoId: 57, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])
        const store = useProximosPassosStore()
        await store.iniciar({ pacienteId: 42, evolucaoId: 57, acoesMarcadas: ["CriarReceita"] })

        expect(store.concluidas).toBe(0)
        expect(store.temAberta).toBe(true)
        expect(store.estaConcluidaAcao("CriarReceita")).toBe(false)
    })
})
