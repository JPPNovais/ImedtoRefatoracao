import { describe, it, expect, vi, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { usePlanosStore } from "./planosStore"
import * as planosServiceModule from "../services/planosService"

vi.mock("../services/planosService", () => ({
    planosService: {
        listar: vi.fn(),
        obter: vi.fn(),
        criar: vi.fn(),
        atualizar: vi.fn(),
        ativar: vi.fn(),
        desativar: vi.fn(),
    },
}))

const mockPlano = {
    id: "aaaa-1111",
    nome: "Plano Pro",
    descricaoCurta: null,
    precoMensalCentavos: 9900,
    gratuito: false,
    ativo: true,
    limitesJson: "{}",
    featuresJson: "{}",
    criadoEm: "2026-01-01T00:00:00Z",
    atualizadoEm: null,
}

describe("planosStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("carregar — popula lista e total ao ter sucesso", async () => {
        vi.mocked(planosServiceModule.planosService.listar).mockResolvedValueOnce({
            itens: [mockPlano],
            total: 1,
            pagina: 1,
            tamanho: 25,
        })

        const store = usePlanosStore()
        await store.carregar()

        expect(store.lista).toHaveLength(1)
        expect(store.lista[0].nome).toBe("Plano Pro")
        expect(store.total).toBe(1)
        expect(store.carregando).toBe(false)
        expect(store.erro).toBeNull()
    })

    it("carregar — define erro ao falhar", async () => {
        vi.mocked(planosServiceModule.planosService.listar).mockRejectedValueOnce(new Error("500"))

        const store = usePlanosStore()
        await store.carregar()

        expect(store.erro).toBeTruthy()
        expect(store.lista).toHaveLength(0)
    })

    it("ativar — atualiza ativo na lista local sem recarregar", async () => {
        vi.mocked(planosServiceModule.planosService.listar).mockResolvedValueOnce({
            itens: [{ ...mockPlano, ativo: false }],
            total: 1,
            pagina: 1,
            tamanho: 25,
        })
        vi.mocked(planosServiceModule.planosService.ativar).mockResolvedValueOnce(undefined)

        const store = usePlanosStore()
        await store.carregar()
        await store.ativar(mockPlano.id, "motivo")

        expect(store.lista[0].ativo).toBe(true)
    })

    it("desativar — atualiza ativo na lista local sem recarregar", async () => {
        vi.mocked(planosServiceModule.planosService.listar).mockResolvedValueOnce({
            itens: [mockPlano],
            total: 1,
            pagina: 1,
            tamanho: 25,
        })
        vi.mocked(planosServiceModule.planosService.desativar).mockResolvedValueOnce(undefined)

        const store = usePlanosStore()
        await store.carregar()
        await store.desativar(mockPlano.id, "motivo")

        expect(store.lista[0].ativo).toBe(false)
    })
})
