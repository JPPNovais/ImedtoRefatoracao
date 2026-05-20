import { describe, it, expect, beforeEach, vi } from "vitest"

// Mock antes do import do service: substitui o httpClient inteiro.
vi.mock("./httpClient", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        delete: vi.fn(),
    },
}))

import { lgpdService } from "./lgpdService"
import httpClient from "./httpClient"

describe("lgpdService", () => {
    beforeEach(() => {
        vi.mocked(httpClient.get).mockReset()
        vi.mocked(httpClient.post).mockReset()
        vi.mocked(httpClient.delete).mockReset()
    })

    describe("excluirConta", () => {
        it("chama DELETE /minha-conta com a senha no body", async () => {
            vi.mocked(httpClient.delete).mockResolvedValueOnce({ data: null } as any)

            await lgpdService.excluirConta("MinhaSenha123!")

            expect(httpClient.delete).toHaveBeenCalledWith("/minha-conta", {
                data: { password: "MinhaSenha123!" },
            })
        })

        it("propaga erro do servidor", async () => {
            vi.mocked(httpClient.delete).mockRejectedValueOnce(new Error("422"))

            await expect(lgpdService.excluirConta("senha")).rejects.toThrow("422")
        })
    })

    describe("exportarDados", () => {
        it("aciona download como blob JSON do GET /minha-conta/exportar-dados", async () => {
            const blobPayload = new Blob([JSON.stringify({ id: 1 })], { type: "application/json" })
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: blobPayload } as any)

            // Mocks de URL e anchor
            const createObjectURL = vi.fn().mockReturnValue("blob:fake")
            const revokeObjectURL = vi.fn()
            const click = vi.fn()
            globalThis.URL.createObjectURL = createObjectURL as any
            globalThis.URL.revokeObjectURL = revokeObjectURL as any
            const originalCreate = document.createElement.bind(document)
            const link = originalCreate("a")
            link.click = click
            const createSpy = vi.spyOn(document, "createElement").mockReturnValue(link as any)

            await lgpdService.exportarDados()

            expect(httpClient.get).toHaveBeenCalledWith(
                "/minha-conta/exportar-dados",
                expect.objectContaining({ responseType: "blob" }),
            )
            expect(createObjectURL).toHaveBeenCalled()
            expect(click).toHaveBeenCalled()
            expect(revokeObjectURL).toHaveBeenCalledWith("blob:fake")
            expect(link.download).toBe("meus-dados.json")

            createSpy.mockRestore()
        })
    })
})
