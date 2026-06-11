import { describe, it, expect, beforeEach, vi } from "vitest"

vi.mock("@/services/httpClient", () => ({
    default: {
        get:  vi.fn(),
        post: vi.fn(),
    },
}))

import httpClient from "@/services/httpClient"
import { pendenciaService, ACAO_LABELS, rotaParaAcao, type AcaoPendencia } from "./pendenciaService"

// ── ACAO_LABELS ───────────────────────────────────────────────────────────────

describe("ACAO_LABELS", () => {
    it("cobre as 6 ações fixas do sistema", () => {
        const acoes: AcaoPendencia[] = [
            "CriarReceita", "CriarAtestado", "PedirExame",
            "CriarOrcamento", "MarcarProcedimentoRealizado", "AgendarRetorno",
        ]
        for (const acao of acoes) {
            expect(ACAO_LABELS[acao]).toBeTruthy()
        }
    })

    it("label de CriarReceita é humanizado em pt-BR", () => {
        expect(ACAO_LABELS["CriarReceita"]).toBe("Criar receita")
    })
})

// ── rotaParaAcao ───────────────────────────────────────────────────────────────

describe("rotaParaAcao", () => {
    const pacienteId = 42

    it("CriarReceita → /pacientes/:id?aba=documentos&tipo=Receita", () => {
        const rota = rotaParaAcao(pacienteId, "CriarReceita")
        expect(rota).toContain("/pacientes/42")
        expect(rota).toContain("Receita")
    })

    it("AgendarRetorno → /agenda?pacienteId=:id", () => {
        const rota = rotaParaAcao(pacienteId, "AgendarRetorno")
        expect(rota).toContain("agenda")
        expect(rota).toContain("42")
    })

    it("MarcarProcedimentoRealizado → null (conclusão manual só pelo painel, CA66)", () => {
        expect(rotaParaAcao(pacienteId, "MarcarProcedimentoRealizado")).toBeNull()
    })

    // F5/R1: CriarOrcamento com e sem evolucaoId (CA97/CA98)
    it("CriarOrcamento sem evolucaoId → aba de orçamentos do paciente", () => {
        const rota = rotaParaAcao(pacienteId, "CriarOrcamento")
        expect(rota).toBe("/pacientes/42?aba=orcamentos")
    })

    it("CriarOrcamento com evolucaoId → form novo com query params evolucaoId+pacienteId (CA97)", () => {
        const rota = rotaParaAcao(pacienteId, "CriarOrcamento", 7)
        expect(rota).toBe("/orcamentos/novo?evolucaoId=7&pacienteId=42")
    })

    it("CriarOrcamento com evolucaoId preserva ambos os params (CA98 — refresh mantém estado)", () => {
        const rota = rotaParaAcao(99, "CriarOrcamento", 123)
        expect(rota).toContain("evolucaoId=123")
        expect(rota).toContain("pacienteId=99")
    })

    it("CriarOrcamento com evolucaoId zero é tratado como sem evolucaoId (falsy)", () => {
        // evolucaoId=0 não é um id válido; o ternário (evolucaoId ?) retorna a rota sem prefill.
        const rota = rotaParaAcao(pacienteId, "CriarOrcamento", 0)
        expect(rota).toBe("/pacientes/42?aba=orcamentos")
    })
})

// ── pendenciaService ───────────────────────────────────────────────────────────

describe("pendenciaService.listarAbertas", () => {
    beforeEach(() => { vi.mocked(httpClient.get).mockReset() })

    it("chama GET /api/paciente/:id/pendencias", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        await pendenciaService.listarAbertas(10)

        expect(httpClient.get).toHaveBeenCalledWith("/api/paciente/10/pendencias")
    })

    it("retorna o array da resposta", async () => {
        const pendencias = [
            { id: 1, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "2026-06-10T10:00:00Z" },
        ]
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: pendencias })

        const result = await pendenciaService.listarAbertas(10)

        expect(result).toEqual(pendencias)
    })
})

describe("pendenciaService.concluirManual", () => {
    beforeEach(() => { vi.mocked(httpClient.post).mockReset() })

    it("chama POST /api/paciente/:pacienteId/pendencias/:pendenciaId/concluir", async () => {
        vi.mocked(httpClient.post).mockResolvedValueOnce({ data: undefined })

        await pendenciaService.concluirManual(10, 55)

        expect(httpClient.post).toHaveBeenCalledWith(
            "/api/paciente/10/pendencias/55/concluir",
        )
    })
})
