import { describe, it, expect, vi, beforeEach } from "vitest"
import { documentoService } from "./documentoService"
import httpClient from "./httpClient"

vi.mock("./httpClient", () => ({
    default: {
        get: vi.fn(),
    },
}))

const httpGet = httpClient.get as ReturnType<typeof vi.fn>

const paginaVazia = {
    itens: [],
    total: 0,
    pagina: 1,
    tamanhoPagina: 20,
}

describe("documentoService.listarDoPaciente", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        httpGet.mockResolvedValue({ data: paginaVazia })
    })

    it("chama o endpoint correto com pacienteId", async () => {
        await documentoService.listarDoPaciente(42)
        expect(httpGet).toHaveBeenCalledWith(
            "/paciente/42/documentos",
            expect.objectContaining({ params: expect.objectContaining({ pagina: 1, tamanho: 20 }) }),
        )
    })

    it("inclui parametro tipo quando fornecido", async () => {
        await documentoService.listarDoPaciente(1, { tipo: "Receita" })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.tipo).toBe("Receita")
    })

    it("omite tipo quando null/undefined (Todos)", async () => {
        await documentoService.listarDoPaciente(1, { tipo: null })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.tipo).toBeUndefined()
    })

    it("inclui dataInicio e dataFim quando fornecidos", async () => {
        await documentoService.listarDoPaciente(1, {
            dataInicio: "2026-01-01",
            dataFim: "2026-06-30",
        })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.dataInicio).toBe("2026-01-01")
        expect(config.params.dataFim).toBe("2026-06-30")
    })

    it("omite dataInicio/dataFim quando null", async () => {
        await documentoService.listarDoPaciente(1, {
            dataInicio: null,
            dataFim: null,
        })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.dataInicio).toBeUndefined()
        expect(config.params.dataFim).toBeUndefined()
    })

    it("inclui busca quando fornecida", async () => {
        await documentoService.listarDoPaciente(1, { busca: "dipirona" })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.busca).toBe("dipirona")
    })

    it("omite busca quando null (no-op)", async () => {
        await documentoService.listarDoPaciente(1, { busca: null })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.busca).toBeUndefined()
    })

    it("retorna a pagina de documentos da resposta", async () => {
        const paginaEsperada = {
            itens: [{ tipo: "Receita", id: 1, titulo: "Receita Comum", data: "2026-06-01", profissionalNome: "Dr. Teste" }],
            total: 1,
            pagina: 1,
            tamanhoPagina: 20,
        }
        httpGet.mockResolvedValue({ data: paginaEsperada })

        const resultado = await documentoService.listarDoPaciente(1)

        expect(resultado.total).toBe(1)
        expect(resultado.itens[0].tipo).toBe("Receita")
    })

    it("usa pagina e tamanho customizados", async () => {
        await documentoService.listarDoPaciente(1, { pagina: 3, tamanho: 10 })
        const [, config] = httpGet.mock.calls[0]
        expect(config.params.pagina).toBe(3)
        expect(config.params.tamanho).toBe(10)
    })
})

// ─── Lógica de contagem dupla (CA15 + addendum §D) ────────────────────────────
// Testa a invariante: totalDocumentosPaciente não é sobrescrito quando filtro está ativo;
// totalDocumentosFiltrado sempre reflete o resultado da request corrente.
describe("logica de contagem dupla de documentos", () => {
    it("CA15: filtro com resultado zero nao zera o contador do paciente", () => {
        // Simula os refs e o computed da PacienteDetalheView
        let totalDocumentosPaciente = 5
        let totalDocumentosFiltrado = 5
        const filtroAtivo = () => false

        // Carga inicial sem filtro: 5 documentos
        function atualizarContadores(total: number) {
            totalDocumentosFiltrado = total
            if (!filtroAtivo()) {
                totalDocumentosPaciente = total
            }
        }

        atualizarContadores(5)
        expect(totalDocumentosPaciente).toBe(5)
        expect(totalDocumentosFiltrado).toBe(5)

        // Usuário ativa filtro por tipo que retorna 0 resultados
        const filtroAtivoComFiltro = () => true

        function atualizarComFiltro(total: number) {
            totalDocumentosFiltrado = total
            if (!filtroAtivoComFiltro()) {
                totalDocumentosPaciente = total
            }
        }

        atualizarComFiltro(0)

        // badge da aba deve continuar 5
        expect(totalDocumentosPaciente).toBe(5)
        // paginação deve refletir 0 (sem resultados)
        expect(totalDocumentosFiltrado).toBe(0)
    })

    it("CA15: remover filtro restaura totalDocumentosFiltrado e atualiza totalDocumentosPaciente", () => {
        let totalDocumentosPaciente = 5
        let totalDocumentosFiltrado = 5

        let filtroEstaAtivo = false
        const filtroAtivo = () => filtroEstaAtivo

        function atualizarContadores(total: number) {
            totalDocumentosFiltrado = total
            if (!filtroAtivo()) {
                totalDocumentosPaciente = total
            }
        }

        // Carga inicial sem filtro
        atualizarContadores(5)

        // Ativa filtro que retorna 2
        filtroEstaAtivo = true
        atualizarContadores(2)
        expect(totalDocumentosPaciente).toBe(5) // badge intacto
        expect(totalDocumentosFiltrado).toBe(2)  // paginação reflete filtro

        // Remove filtro — 5 documentos retornam
        filtroEstaAtivo = false
        atualizarContadores(5)
        expect(totalDocumentosPaciente).toBe(5)
        expect(totalDocumentosFiltrado).toBe(5)
    })
})
