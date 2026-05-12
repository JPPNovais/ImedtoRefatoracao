import { describe, it, expect, beforeEach, vi } from "vitest"

// Mock antes do import do service: substitui o httpClient inteiro.
vi.mock("./httpClient", () => ({
    default: {
        get: vi.fn(),
    },
}))

import { bootstrapService } from "./bootstrapService"
import httpClient from "./httpClient"

describe("bootstrapService", () => {
    beforeEach(() => {
        vi.mocked(httpClient.get).mockReset()
    })

    it("retorna o payload agregado de GET /auth/bootstrap", async () => {
        const payload = {
            usuario: {
                id: "u-1",
                email: "x@y.com",
                nomeCompleto: "Fulano",
                telefone: null,
                status: "Ativo",
                onboardingCompleto: true,
            },
            profissional: null,
            estabelecimentos: [
                { id: 10, nomeFantasia: "Clínica A", papelDoUsuario: "Dono" },
            ],
        }
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: payload } as any)

        const r = await bootstrapService.obter()

        expect(httpClient.get).toHaveBeenCalledWith("/auth/bootstrap")
        expect(r).toEqual(payload)
    })

    it("propaga erro quando o backend rejeita (ex: 401 após refresh falhar)", async () => {
        vi.mocked(httpClient.get).mockRejectedValueOnce(new Error("401"))

        await expect(bootstrapService.obter()).rejects.toThrow("401")
    })

    it("obterInicial retorna null quando o backend devolve 200 com body null (sem sessão)", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: null } as any)

        const r = await bootstrapService.obterInicial()

        expect(httpClient.get).toHaveBeenCalledWith("/auth/bootstrap", expect.objectContaining({ _noAutoRefresh: true }))
        expect(r).toBeNull()
    })

    it("obterInicial retorna o payload quando há sessão", async () => {
        const payload = {
            usuario: { id: "u-1", email: "x@y.com", nomeCompleto: "Fulano", telefone: null, status: "Ativo", onboardingCompleto: true },
            profissional: null,
            estabelecimentos: [],
        }
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: payload } as any)

        const r = await bootstrapService.obterInicial()

        expect(r).toEqual(payload)
    })
})
