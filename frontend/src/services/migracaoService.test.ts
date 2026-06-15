/**
 * Testes do migracaoService (briefing 2026-06-15_001 — Marco 1).
 *
 * CA19: limite de 50MB rejeitado no front antes de enviar (trava de UX; back tem espelho 422).
 * CA2:  X-Estabelecimento-Id passado corretamente em todas as chamadas.
 */

import { describe, it, expect, beforeEach, vi } from "vitest"

vi.mock("@/services/httpClient", () => ({
    default: {
        get:  vi.fn(),
        post: vi.fn(),
    },
}))

import httpClient from "@/services/httpClient"
import migracaoService, {
    LIMITE_UPLOAD_BYTES,
    MENSAGEM_LIMITE,
} from "./migracaoService"

describe("migracaoService", () => {
    const estabelecimentoId = 42
    const jobId = 7

    beforeEach(() => {
        vi.mocked(httpClient.post).mockReset()
        vi.mocked(httpClient.get).mockReset()
    })

    // ─── CA19 — Limite de 50MB no front ─────────────────────────────────────────

    it("LIMITE_UPLOAD_BYTES é exatamente 50MB", () => {
        expect(LIMITE_UPLOAD_BYTES).toBe(50 * 1024 * 1024)
    })

    it("iniciarUpload — arquivo no limite exato (50MB) é enviado", async () => {
        const arquivo = new File(["x".repeat(LIMITE_UPLOAD_BYTES)], "dados.zip", {
            type: "application/zip",
        })
        vi.mocked(httpClient.post).mockResolvedValueOnce({
            data: { jobId: 1, status: "aguardando_mapa" },
        })

        await migracaoService.iniciarUpload(estabelecimentoId, arquivo)

        expect(httpClient.post).toHaveBeenCalledOnce()
    })

    it("iniciarUpload — arquivo 1 byte acima de 50MB lança MENSAGEM_LIMITE", async () => {
        const arquivo = new File(["x"], "dados.zip", { type: "application/zip" })
        Object.defineProperty(arquivo, "size", { value: LIMITE_UPLOAD_BYTES + 1 })

        await expect(migracaoService.iniciarUpload(estabelecimentoId, arquivo))
            .rejects.toThrow(MENSAGEM_LIMITE)

        // Nunca deve ter feito requisição HTTP.
        expect(httpClient.post).not.toHaveBeenCalled()
    })

    it("MENSAGEM_LIMITE menciona 50MB (UX consistente com backend)", () => {
        expect(MENSAGEM_LIMITE).toContain("50MB")
    })

    // ─── CA2 — Tenant header ─────────────────────────────────────────────────────

    it("iniciarUpload — envia X-Estabelecimento-Id correto", async () => {
        const arquivo = new File(["zip"], "a.zip", { type: "application/zip" })
        vi.mocked(httpClient.post).mockResolvedValueOnce({
            data: { jobId: 9, status: "aguardando_mapa" },
        })

        await migracaoService.iniciarUpload(estabelecimentoId, arquivo)

        const [, , config] = vi.mocked(httpClient.post).mock.calls[0]
        expect((config as { headers?: Record<string, string> })?.headers?.["X-Estabelecimento-Id"])
            .toBe(String(estabelecimentoId))
    })

    it("obterStatus — envia X-Estabelecimento-Id correto", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({
            data: { jobId, status: "aguardando_mapa" },
        })

        await migracaoService.obterStatus(estabelecimentoId, jobId)

        const [url, config] = vi.mocked(httpClient.get).mock.calls[0]
        expect(url).toBe(`/api/migracao/${jobId}`)
        expect((config as { headers?: Record<string, string> })?.headers?.["X-Estabelecimento-Id"])
            .toBe(String(estabelecimentoId))
    })

    // ─── Fluxo feliz ─────────────────────────────────────────────────────────────

    it("iniciarUpload — retorna job com status aguardando_mapa", async () => {
        const arquivo = new File(["zip"], "migrar.zip", { type: "application/zip" })
        vi.mocked(httpClient.post).mockResolvedValueOnce({
            data: { jobId: 5, status: "aguardando_mapa" },
        })

        const result = await migracaoService.iniciarUpload(estabelecimentoId, arquivo)

        expect(result.jobId).toBe(5)
        expect(result.status).toBe("aguardando_mapa")
    })

    it("iniciarUpload — inclui origem quando informada", async () => {
        const arquivo = new File(["zip"], "a.zip", { type: "application/zip" })
        vi.mocked(httpClient.post).mockResolvedValueOnce({
            data: { jobId: 3, status: "aguardando_mapa" },
        })

        await migracaoService.iniciarUpload(estabelecimentoId, arquivo, "iClinic")

        const [, formData] = vi.mocked(httpClient.post).mock.calls[0]
        expect((formData as FormData).get("origem")).toBe("iClinic")
    })

    // ─── CA13 — Onda 2 (prontuário) ─────────────────────────────────────────────

    it("iniciarUpload — inclui onda=prontuario quando Onda 2 selecionada (CA13)", async () => {
        const arquivo = new File(["zip"], "prontuarios.zip", { type: "application/zip" })
        vi.mocked(httpClient.post).mockResolvedValueOnce({
            data: { jobId: 11, status: "aguardando_mapa" },
        })

        await migracaoService.iniciarUpload(estabelecimentoId, arquivo, undefined, "prontuario")

        const [, formData] = vi.mocked(httpClient.post).mock.calls[0]
        expect((formData as FormData).get("onda")).toBe("prontuario")
    })

    it("iniciarUpload — NÃO inclui campo onda quando Onda 1 (valor vazio)", async () => {
        const arquivo = new File(["zip"], "pacientes.zip", { type: "application/zip" })
        vi.mocked(httpClient.post).mockResolvedValueOnce({
            data: { jobId: 12, status: "aguardando_mapa" },
        })

        await migracaoService.iniciarUpload(estabelecimentoId, arquivo, undefined, undefined)

        const [, formData] = vi.mocked(httpClient.post).mock.calls[0]
        expect((formData as FormData).get("onda")).toBeNull()
    })
})
