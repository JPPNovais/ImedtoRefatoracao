import { describe, it, expect, beforeEach, vi } from "vitest"

vi.mock("@/services/httpClient", () => ({
    default: {
        get:  vi.fn(),
        post: vi.fn(),
    },
}))

import httpClient from "@/services/httpClient"
import { prontuarioService, type ProcedimentoIndicado } from "./prontuarioService"

// ── F5/R2: obterProcedimentosIndicados (CA97/CA99/CA110/CA114) ───────────────

describe("prontuarioService.obterProcedimentosIndicados", () => {
    beforeEach(() => { vi.mocked(httpClient.get).mockReset() })

    it("chama o endpoint correto com pacienteId e evolucaoId", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        await prontuarioService.obterProcedimentosIndicados(10, 55)

        expect(httpClient.get).toHaveBeenCalledWith(
            "/paciente/10/prontuario/evolucoes/55/procedimentos-indicados",
        )
    })

    it("retorna lista de ProcedimentoIndicado (CA99 — já filtrado pelo backend)", async () => {
        const procs: ProcedimentoIndicado[] = [
            { catalogoCirurgiaId: 1, descricao: "Rinoplastia", valor: 1500 },
            { catalogoCirurgiaId: 2, descricao: "Bichectomia", valor: 800 },
        ]
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: procs })

        const result = await prontuarioService.obterProcedimentosIndicados(10, 55)

        expect(result).toHaveLength(2)
        expect(result[0].catalogoCirurgiaId).toBe(1)
        expect(result[1].valor).toBe(800)
    })

    it("retorna lista vazia quando evolução não tem procedimentos de catálogo (CA114)", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        const result = await prontuarioService.obterProcedimentosIndicados(10, 55)

        expect(result).toEqual([])
    })

    it("propaga erro de rede para o chamador (CA114 — form abre com aviso não-bloqueante)", async () => {
        vi.mocked(httpClient.get).mockRejectedValueOnce(new Error("Network Error"))

        await expect(prontuarioService.obterProcedimentosIndicados(10, 55)).rejects.toThrow("Network Error")
    })
})
