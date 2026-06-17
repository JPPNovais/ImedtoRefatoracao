import { describe, it, expect, beforeEach, vi } from "vitest"

vi.mock("./httpClient", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        patch: vi.fn(),
        delete: vi.fn(),
    },
}))

import { termoModeloService } from "./termoModeloService"
import httpClient from "./httpClient"

describe("termoModeloService", () => {
    beforeEach(() => {
        vi.mocked(httpClient.get).mockReset()
        vi.mocked(httpClient.post).mockReset()
        vi.mocked(httpClient.put).mockReset()
        vi.mocked(httpClient.patch).mockReset()
        vi.mocked(httpClient.delete).mockReset()
    })

    describe("listarModelos", () => {
        it("envia filtros padrão e devolve pagina", async () => {
            const resp = { itens: [], pagina: 1, tamanho: 10, total: 0 }
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: resp } as any)

            const r = await termoModeloService.listarModelos()

            expect(httpClient.get).toHaveBeenCalledWith("/termos/modelos", {
                params: {
                    busca: undefined,
                    categoria: undefined,
                    somenteAtivos: false,
                    incluirPadroes: false,
                    pagina: 1,
                    tamanho: 10,
                },
            })
            expect(r).toEqual(resp)
        })

        it("propaga filtros não-default", async () => {
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { itens: [], pagina: 1, tamanho: 10, total: 0 } } as any)

            await termoModeloService.listarModelos({
                busca: "consentimento",
                categoria: "lgpd",
                somenteAtivos: true,
                incluirPadroes: true,
                pagina: 2,
                tamanho: 10,
            })

            expect(httpClient.get).toHaveBeenCalledWith("/termos/modelos", {
                params: {
                    busca: "consentimento",
                    categoria: "lgpd",
                    somenteAtivos: true,
                    incluirPadroes: true,
                    pagina: 2,
                    tamanho: 10,
                },
            })
        })
    })

    describe("criarModelo", () => {
        it("retorna o id criado vindo do backend", async () => {
            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { modeloId: 42 } } as any)

            const id = await termoModeloService.criarModelo({
                categoria: "geral",
                titulo: "Termo X",
                conteudoHtml: "<p>conteúdo</p>",
            })

            expect(httpClient.post).toHaveBeenCalledWith("/termos/modelos", {
                categoria: "geral",
                titulo: "Termo X",
                conteudoHtml: "<p>conteúdo</p>",
            })
            expect(id).toBe(42)
        })
    })

    describe("atualizarModelo", () => {
        it("envia PUT com payload completo", async () => {
            vi.mocked(httpClient.put).mockResolvedValueOnce({ data: null } as any)

            await termoModeloService.atualizarModelo(7, {
                categoria: "cirurgico",
                titulo: "Novo título",
                conteudoHtml: "<p>HTML</p>",
            })

            expect(httpClient.put).toHaveBeenCalledWith("/termos/modelos/7", {
                categoria: "cirurgico",
                titulo: "Novo título",
                conteudoHtml: "<p>HTML</p>",
            })
        })
    })

    describe("alterarAtivo", () => {
        it("envia PATCH com flag boolean", async () => {
            vi.mocked(httpClient.patch).mockResolvedValueOnce({ data: null } as any)

            await termoModeloService.alterarAtivo(7, false)

            expect(httpClient.patch).toHaveBeenCalledWith("/termos/modelos/7/ativo", { ativo: false })
        })
    })

    describe("clonarPadrao", () => {
        it("retorna o id do novo modelo clonado", async () => {
            vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { modeloId: 99 } } as any)

            const id = await termoModeloService.clonarPadrao(3)

            expect(httpClient.post).toHaveBeenCalledWith("/termos/modelos/3/clonar")
            expect(id).toBe(99)
        })
    })

    describe("excluirModelo", () => {
        it("envia DELETE no id correto", async () => {
            vi.mocked(httpClient.delete).mockResolvedValueOnce({ data: null } as any)

            await termoModeloService.excluirModelo(5)

            expect(httpClient.delete).toHaveBeenCalledWith("/termos/modelos/5")
        })
    })

    describe("listarPadroes", () => {
        it("não envia params e devolve array", async () => {
            const padroes = [{ id: 1, titulo: "LGPD", ehPadraoDoSistema: true }]
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: padroes } as any)

            const r = await termoModeloService.listarPadroes()

            expect(httpClient.get).toHaveBeenCalledWith("/termos/modelos/padroes")
            expect(r).toBe(padroes)
        })
    })
})
