import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import type { ProntuarioCompleto } from "@/services/prontuarioService"
import type { Paciente } from "@/services/pacienteService"

// ─── Mocks de pesados (jsPDF + autotable + helper) ──────────────────────────
// O helper expõe funções de desenho que dependem de canvas/fontes (não dá pra
// rodar no happy-dom). Aqui validamos apenas que o composable:
//   1) registra fontes,
//   2) busca estabelecimento e logo,
//   3) chama desenharCabecalho/Bloco/Rodape/Watermark sem crashar com
//      dados vazios e com dados longos.

const docMock = {
    internal: { pageSize: { getWidth: () => 210, getHeight: () => 297 } },
    addPage: vi.fn(),
    setPage: vi.fn(),
    getNumberOfPages: vi.fn(() => 1),
    getCurrentPageInfo: vi.fn(() => ({ pageNumber: 1 })),
    save: vi.fn(),
    output: vi.fn((kind: string) => kind === "bloburl" ? "blob:https://app.imedto.com/fake-blob" : ""),
    setProperties: vi.fn(),
    setFont: vi.fn(),
    setFontSize: vi.fn(),
    setTextColor: vi.fn(),
    setDrawColor: vi.fn(),
    setLineWidth: vi.fn(),
    setLineDashPattern: vi.fn(),
    setFillColor: vi.fn(),
    text: vi.fn(),
    line: vi.fn(),
    roundedRect: vi.fn(),
    addImage: vi.fn(),
    splitTextToSize: vi.fn((s: string) => [s]),
}

vi.mock("jspdf", () => ({
    jsPDF: vi.fn(() => docMock),
}))
vi.mock("jspdf-autotable", () => ({
    default: vi.fn((d: any) => {
        d.lastAutoTable = { finalY: 100 }
    }),
}))

const helperMocks = {
    registrarFontesNunito: vi.fn(),
    carregarEstabelecimentoAtivo: vi.fn(async () => null),
    carregarLogoComoDataUrl: vi.fn(async () => null),
    desenharCabecalho: vi.fn(() => 30),
    desenharBlocoPaciente: vi.fn((_: any, _p: any, y: number) => y + 30),
    finalizarPaginas: vi.fn(),
    PDF_MARGIN: { top: 14, side: 18, bottom: 22 },
    PDF_THEME: {
        ink: [26, 36, 64],
        inkTitle: [29, 53, 87],
        mute: [100, 116, 139],
        muteLight: [148, 163, 184],
        border: [226, 232, 240],
        borderStrong: [203, 213, 225],
        cardBg: [248, 250, 252],
    },
    NUNITO_FAMILY: "Nunito",
}

vi.mock("@/composables/usePdfHeader", () => helperMocks)

import { useProntuarioPdf } from "./useProntuarioPdf"

const prontVazio: ProntuarioCompleto = {
    prontuario: {
        id: 1,
        pacienteId: 10,
        estabelecimentoId: 1,
        modeloDeProntuarioId: 1,
        modeloNome: "Padrão Imedto",
        modeloEstrutura: [],
        criadoEm: "2026-05-12T10:00:00Z",
    },
    evolucoes: [],
}

const prontComEvolucoes: ProntuarioCompleto = {
    prontuario: {
        id: 1,
        pacienteId: 10,
        estabelecimentoId: 1,
        modeloDeProntuarioId: 1,
        modeloNome: "Padrão Imedto",
        modeloEstrutura: [
            { chave: "queixa", titulo: "Queixa principal", tipo: "texto_longo", ordem: 1 },
            { chave: "conduta", titulo: "Conduta", tipo: "texto_longo", ordem: 2 },
        ],
        criadoEm: "2026-05-12T10:00:00Z",
    },
    evolucoes: [
        {
            id: 1,
            prontuarioId: 1,
            autorUsuarioId: "00000000-0000-0000-0000-000000000001",
            autorNome: "Dra. Joana",
            modeloNome: "Padrão Imedto",
            conteudo: {
                queixa: "Dor no peito há 3 dias.",
                conduta: "Solicitar ECG e troponina.",
            },
            modeloSnapshot: [
                { chave: "queixa", titulo: "Queixa principal", tipo: "texto_longo", ordem: 1 },
                { chave: "conduta", titulo: "Conduta", tipo: "texto_longo", ordem: 2 },
            ],
            modeloDeProntuarioIdOrigem: 1,
            criadaEm: "2026-05-12T10:30:00Z",
        },
    ],
}

const pacienteCompleto: Paciente = {
    id: 10,
    estabelecimentoId: 1,
    nomeCompleto: "Maria Aparecida da Silva",
    cpf: "12345678901",
    documentoInternacional: null,
    dataNascimento: "1985-04-12",
    genero: "F",
    telefone: "11999998888",
    email: null,
    endereco: null,
    observacoes: null,
    tags: [],
    alertas: [],
    criadoEm: "2026-01-01T00:00:00Z",
    atualizadoEm: null,
}

describe("useProntuarioPdf — redesign institucional", () => {
    beforeEach(async () => {
        setActivePinia(createPinia())
        Object.values(helperMocks).forEach(m => {
            if (typeof m === "function") (m as any).mockClear?.()
        })
        Object.values(docMock).forEach(m => {
            if (typeof m === "function") (m as any).mockClear?.()
        })
        docMock.save.mockClear()
        const autoTable = (await import("jspdf-autotable")).default as any
        autoTable.mockClear()
    })

    it("gera PDF sem crash quando prontuário não tem evoluções e exibe empty state", async () => {
        const { gerarPdf } = useProntuarioPdf()
        await gerarPdf(prontVazio, pacienteCompleto)

        expect(helperMocks.registrarFontesNunito).toHaveBeenCalledOnce()
        expect(helperMocks.desenharCabecalho).toHaveBeenCalled()
        expect(helperMocks.desenharBlocoPaciente).toHaveBeenCalled()
        expect(helperMocks.finalizarPaginas).toHaveBeenCalledOnce()
        expect(docMock.save).toHaveBeenCalledWith(expect.stringMatching(/^prontuario-/))
        // Confere desenho do empty state (roundedRect com lineDashPattern)
        expect(docMock.setLineDashPattern).toHaveBeenCalled()
    })

    it("aceita paciente como string (modo legado) sem crashar", async () => {
        const { gerarPdf } = useProntuarioPdf()
        await gerarPdf(prontVazio, "Joao Soares")
        expect(docMock.save).toHaveBeenCalled()
    })

    it("renderiza evoluções via autoTable com seções preenchidas", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const { gerarPdf } = useProntuarioPdf()
        await gerarPdf(prontComEvolucoes, pacienteCompleto)

        expect(autoTable).toHaveBeenCalled()
        const chamada = autoTable.mock.calls[0][1]
        // body com 2 linhas (queixa + conduta)
        expect(chamada.body).toHaveLength(2)
        expect(chamada.body[0][0]).toBe("Queixa principal")
        expect(chamada.body[0][1]).toContain("Dor no peito")
    })

    it("filtra seções vazias antes de renderizar tabela", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const pront = {
            ...prontComEvolucoes,
            evolucoes: [{
                ...prontComEvolucoes.evolucoes[0]!,
                conteudo: { queixa: "Algo", conduta: "" },
            }],
        }
        const { gerarPdf } = useProntuarioPdf()
        await gerarPdf(pront, pacienteCompleto)

        const chamada = autoTable.mock.calls[0][1]
        expect(chamada.body).toHaveLength(1)
        expect(chamada.body[0][0]).toBe("Queixa principal")
    })

    it("chama finalizarPaginas com aviso de assinatura manual no rodapé", async () => {
        const { gerarPdf } = useProntuarioPdf()
        await gerarPdf(prontComEvolucoes, pacienteCompleto)

        const rodape = helperMocks.finalizarPaginas.mock.calls[0][1]
        expect(rodape.assinatura.aviso).toBe("Assine manualmente no espaço acima")
        expect(rodape.assinatura.assinadoDigitalmente).toBeUndefined()
    })
})

describe("useProntuarioPdf — gerarPdfEvolucao (PDF individual)", () => {
    beforeEach(async () => {
        setActivePinia(createPinia())
        Object.values(helperMocks).forEach(m => {
            if (typeof m === "function") (m as any).mockClear?.()
        })
        Object.values(docMock).forEach(m => {
            if (typeof m === "function") (m as any).mockClear?.()
        })
        docMock.save.mockClear()
        const autoTable = (await import("jspdf-autotable")).default as any
        autoTable.mockClear()
    })

    const evolucao = prontComEvolucoes.evolucoes[0]!

    it("usa título institucional e subtítulo com a data da evolução", async () => {
        const { gerarPdfEvolucao } = useProntuarioPdf()
        await gerarPdfEvolucao(prontComEvolucoes, evolucao, pacienteCompleto)

        expect(helperMocks.desenharCabecalho).toHaveBeenCalled()
        const argumentos = helperMocks.desenharCabecalho.mock.calls[0] as unknown as any[]
        const opts = argumentos[3] as { docTitle: string, docSubtitle: string }
        expect(opts.docTitle).toBe("PRONTUÁRIO MÉDICO — EVOLUÇÃO")
        // data da evolução (2026-05-12T10:30:00Z) — tolerante a timezone:
        // o subtítulo precisa começar com "Evolução de " e conter o ano.
        expect(opts.docSubtitle).toMatch(/^Evolução de /)
        expect(opts.docSubtitle).toContain("2026")
    })

    it("nome do arquivo contém slug do paciente e timestamp YYYYMMDD-HHmm da evolução", async () => {
        const { gerarPdfEvolucao } = useProntuarioPdf()
        await gerarPdfEvolucao(prontComEvolucoes, evolucao, pacienteCompleto)

        expect(docMock.save).toHaveBeenCalled()
        const nomeArquivo = docMock.save.mock.calls[0][0] as string
        expect(nomeArquivo).toMatch(/^evolucao-maria-aparecida-da-silva-\d{8}-\d{4}\.pdf$/)
    })

    it("renderiza apenas as seções preenchidas dessa evolução", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const evolPreenchidaParcial = {
            ...evolucao,
            conteudo: { queixa: "Algo", conduta: "" },
        }
        const { gerarPdfEvolucao } = useProntuarioPdf()
        await gerarPdfEvolucao(prontComEvolucoes, evolPreenchidaParcial, pacienteCompleto)

        const chamada = autoTable.mock.calls[0][1]
        expect(chamada.body).toHaveLength(1)
        expect(chamada.body[0][0]).toBe("Queixa principal")
    })

    it("aceita paciente como string sem crashar", async () => {
        const { gerarPdfEvolucao } = useProntuarioPdf()
        await gerarPdfEvolucao(prontComEvolucoes, evolucao, "Joao Soares")
        const nomeArquivo = docMock.save.mock.calls[0][0] as string
        expect(nomeArquivo).toContain("joao-soares")
    })
})

describe("useProntuarioPdf — modo visualizar (blob URL)", () => {
    const evolucao = prontComEvolucoes.evolucoes[0]!

    beforeEach(async () => {
        setActivePinia(createPinia())
        Object.values(helperMocks).forEach(m => {
            if (typeof m === "function") (m as any).mockClear?.()
        })
        Object.values(docMock).forEach(m => {
            if (typeof m === "function") (m as any).mockClear?.()
        })
        const autoTable = (await import("jspdf-autotable")).default as any
        autoTable.mockClear()
        // Stub global de URL.revokeObjectURL (chamado via setTimeout 60s — basta existir).
        if (!(globalThis as any).URL.revokeObjectURL) {
            (globalThis as any).URL.revokeObjectURL = vi.fn()
        }
    })

    it("gerarPdf('visualizar') chama output('bloburl'), NÃO chama save e retorna blobUrl", async () => {
        const { gerarPdf } = useProntuarioPdf()
        const resultado = await gerarPdf(prontComEvolucoes, pacienteCompleto, "visualizar")

        expect(docMock.output).toHaveBeenCalledWith("bloburl")
        expect(docMock.save).not.toHaveBeenCalled()
        expect(resultado.blobUrl).toBe("blob:https://app.imedto.com/fake-blob")
        // Título da aba: nome do arquivo sem .pdf
        expect(docMock.setProperties).toHaveBeenCalledWith(
            expect.objectContaining({ title: expect.stringMatching(/^prontuario-/) }),
        )
        expect(docMock.setProperties.mock.calls[0][0].title).not.toMatch(/\.pdf$/)
    })

    it("gerarPdf('download') chama save e retorna blobUrl=null (sem alterar comportamento atual)", async () => {
        const { gerarPdf } = useProntuarioPdf()
        const resultado = await gerarPdf(prontComEvolucoes, pacienteCompleto, "download")

        expect(docMock.save).toHaveBeenCalledOnce()
        expect(docMock.output).not.toHaveBeenCalled()
        expect(resultado.blobUrl).toBeNull()
    })

    it("gerarPdf() sem modo cai em 'download' (default não-breaking)", async () => {
        const { gerarPdf } = useProntuarioPdf()
        await gerarPdf(prontComEvolucoes, pacienteCompleto)
        expect(docMock.save).toHaveBeenCalledOnce()
        expect(docMock.output).not.toHaveBeenCalled()
    })

    it("gerarPdfEvolucao('visualizar') chama output('bloburl'), NÃO chama save e retorna blobUrl", async () => {
        const { gerarPdfEvolucao } = useProntuarioPdf()
        const resultado = await gerarPdfEvolucao(prontComEvolucoes, evolucao, pacienteCompleto, "visualizar")

        expect(docMock.output).toHaveBeenCalledWith("bloburl")
        expect(docMock.save).not.toHaveBeenCalled()
        expect(resultado.blobUrl).toBe("blob:https://app.imedto.com/fake-blob")
        expect(docMock.setProperties).toHaveBeenCalledWith(
            expect.objectContaining({ title: expect.stringMatching(/^evolucao-/) }),
        )
    })

    it("gerarPdfEvolucao('download') chama save (sem alterar comportamento atual)", async () => {
        const { gerarPdfEvolucao } = useProntuarioPdf()
        const resultado = await gerarPdfEvolucao(prontComEvolucoes, evolucao, pacienteCompleto, "download")
        expect(docMock.save).toHaveBeenCalledOnce()
        expect(docMock.output).not.toHaveBeenCalled()
        expect(resultado.blobUrl).toBeNull()
    })
})
