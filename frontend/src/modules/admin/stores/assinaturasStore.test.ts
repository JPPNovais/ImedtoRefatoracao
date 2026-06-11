import { describe, it, expect, vi, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { useAssinaturasStore } from "./assinaturasStore"
import * as assinaturasServiceModule from "../services/assinaturasService"

vi.mock("../services/assinaturasService", () => ({
    assinaturasService: {
        listarHistorico: vi.fn(),
        trocarPlano: vi.fn(),
        concederGratuidade: vi.fn(),
        encerrar: vi.fn(),
        liberarVitalicio: vi.fn(),
        liberarAteData: vi.fn(),
        iniciarTrial: vi.fn(),
        suspender: vi.fn(),
        reativar: vi.fn(),
    },
}))

const mockAssinatura = {
    id: "bbbb-2222",
    estabelecimentoId: 1,
    planoId: "cccc-3333",
    planoNome: "Plano Pro",
    planoGratuito: false,
    iniciadaEm: "2026-01-01T00:00:00Z",
    fimEm: null,
    expiraEm: null,
    suspensaEm: null,
    gratuita: false,
    motivo: null,
    criadaEm: "2026-01-01T00:00:00Z",
    vigente: true,
    estado: "Vitalicia",
}

describe("assinaturasStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("carregarHistorico — popula historico ao ter sucesso", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([mockAssinatura])

        const store = useAssinaturasStore()
        await store.carregarHistorico(1)

        expect(store.historico).toHaveLength(1)
        expect(store.historico[0].planoNome).toBe("Plano Pro")
        expect(store.erro).toBeNull()
    })

    it("carregarHistorico — define erro ao falhar", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockRejectedValueOnce(new Error("500"))

        const store = useAssinaturasStore()
        await store.carregarHistorico(1)

        expect(store.erro).toBeTruthy()
        expect(store.historico).toHaveLength(0)
    })

    it("vigente — retorna a assinatura com vigente = true", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([
            { ...mockAssinatura, vigente: true },
            { ...mockAssinatura, id: "dddd-4444", vigente: false, fimEm: "2025-12-31T00:00:00Z" },
        ])

        const store = useAssinaturasStore()
        await store.carregarHistorico(1)

        expect(store.vigente()?.id).toBe("bbbb-2222")
    })

    it("trocarPlano — chama service e recarrega historico", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.trocarPlano).mockResolvedValueOnce(undefined)
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([mockAssinatura])

        const store = useAssinaturasStore()
        await store.trocarPlano(1, { planoId: "cccc-3333", inicio: "2026-01-01", motivo: "upgrade" })

        expect(assinaturasServiceModule.assinaturasService.trocarPlano).toHaveBeenCalledOnce()
        expect(assinaturasServiceModule.assinaturasService.listarHistorico).toHaveBeenCalledWith(1)
    })

    it("liberarVitalicio — chama service e recarrega historico", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.liberarVitalicio).mockResolvedValueOnce(undefined)
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([mockAssinatura])

        const store = useAssinaturasStore()
        await store.liberarVitalicio(1, { planoId: "cccc-3333", motivo: "parceiro" })

        expect(assinaturasServiceModule.assinaturasService.liberarVitalicio).toHaveBeenCalledOnce()
    })

    it("suspender — chama service e recarrega historico", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.suspender).mockResolvedValueOnce(undefined)
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([
            { ...mockAssinatura, estado: "Suspensa", suspensaEm: "2026-06-11T00:00:00Z" },
        ])

        const store = useAssinaturasStore()
        await store.suspender(1, "inadimplência")

        expect(assinaturasServiceModule.assinaturasService.suspender).toHaveBeenCalledWith(1, "inadimplência")
        expect(store.historico[0].estado).toBe("Suspensa")
    })

    it("reativar — chama service e recarrega historico", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.reativar).mockResolvedValueOnce(undefined)
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([mockAssinatura])

        const store = useAssinaturasStore()
        await store.reativar(1, "pagamento regularizado")

        expect(assinaturasServiceModule.assinaturasService.reativar).toHaveBeenCalledWith(1, "pagamento regularizado")
    })

    it("limpar — zera historico e erro", async () => {
        vi.mocked(assinaturasServiceModule.assinaturasService.listarHistorico).mockResolvedValueOnce([mockAssinatura])

        const store = useAssinaturasStore()
        await store.carregarHistorico(1)
        store.limpar()

        expect(store.historico).toHaveLength(0)
        expect(store.erro).toBeNull()
    })
})
