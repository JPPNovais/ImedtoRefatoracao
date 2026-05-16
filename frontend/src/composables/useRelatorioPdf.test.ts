import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import type { FaturamentoCategoria, RelatorioAgendamentos } from "@/services/dashboardService"

const docMock = {
    internal: { pageSize: { getWidth: () => 210, getHeight: () => 297 } },
    addPage: vi.fn(),
    setPage: vi.fn(),
    getNumberOfPages: vi.fn(() => 1),
    save: vi.fn(),
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
    lastAutoTable: { finalY: 100 },
}

vi.mock("jspdf", () => ({
    jsPDF: vi.fn(() => docMock),
}))
vi.mock("jspdf-autotable", () => ({
    default: vi.fn((d: any) => { d.lastAutoTable = { finalY: 120 } }),
}))

const helperMocks = {
    registrarFontesNunito: vi.fn(),
    carregarEstabelecimentoAtivo: vi.fn(async () => null),
    carregarLogoComoDataUrl: vi.fn(async () => null),
    desenharCabecalho: vi.fn(() => 30),
    finalizarPaginas: vi.fn(),
    PDF_MARGIN: { top: 14, side: 18, bottom: 22 },
    PDF_THEME: {
        ink: [26, 36, 64],
        inkTitle: [29, 53, 87],
        cardBg: [248, 250, 252],
    },
    NUNITO_FAMILY: "Nunito",
}

vi.mock("@/composables/usePdfHeader", () => helperMocks)

import { useRelatorioPdf } from "./useRelatorioPdf"

const dadosFaturamento: FaturamentoCategoria[] = [
    { tipo: "Receita", categoria: "Consulta", totalPago: 5000, totalPendente: 1000, quantidade: 12 },
    { tipo: "Despesa", categoria: "Aluguel", totalPago: 2000, totalPendente: 0, quantidade: 1 },
]

const dadosAgendamentos: RelatorioAgendamentos = {
    total: 30,
    porStatus: [
        { status: "Concluído", quantidade: 20 },
        { status: "No-show", quantidade: 5 },
        { status: "Confirmado", quantidade: 5 },
    ],
    porDia: [
        { data: "2026-05-10", quantidade: 12 },
        { data: "2026-05-11", quantidade: 18 },
    ],
}

describe("useRelatorioPdf — redesign institucional", () => {
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

    it("faturamento: renderiza tabela com receitas + despesas e finaliza com aviso de gestão", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const { gerarFaturamentoPdf } = useRelatorioPdf()
        await gerarFaturamentoPdf(dadosFaturamento, { dataInicio: "2026-05-01", dataFim: "2026-05-31" })

        expect(helperMocks.registrarFontesNunito).toHaveBeenCalledOnce()
        expect(helperMocks.desenharCabecalho).toHaveBeenCalled()
        expect(autoTable).toHaveBeenCalled()
        const chamada = autoTable.mock.calls[0][1]
        expect(chamada.body).toHaveLength(2)

        // Rodapé do relatório NÃO tem assinatura — só aviso de "documento de gestão"
        const rodape = helperMocks.finalizarPaginas.mock.calls[0][1]
        expect(rodape.assinatura).toBeUndefined()
        expect(rodape.avisoDireita).toContain("Documento de gestão")

        expect(docMock.save).toHaveBeenCalledWith("relatorio-faturamento.pdf")
    })

    it("faturamento: dados vazios exibe empty state sem chamar autoTable", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const { gerarFaturamentoPdf } = useRelatorioPdf()
        await gerarFaturamentoPdf([], { dataInicio: "2026-05-01", dataFim: "2026-05-31" })

        // 0 chamadas a autoTable porque tabela só é desenhada quando há dados
        expect(autoTable).not.toHaveBeenCalled()
        // Empty state usa lineDashPattern (caixa pontilhada)
        expect(docMock.setLineDashPattern).toHaveBeenCalled()
        expect(docMock.save).toHaveBeenCalledOnce()
    })

    it("agendamentos: renderiza 2 tabelas (por status + por dia)", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const { gerarAgendamentosPdf } = useRelatorioPdf()
        await gerarAgendamentosPdf(dadosAgendamentos, { dataInicio: "2026-05-01", dataFim: "2026-05-31" })

        expect(autoTable).toHaveBeenCalledTimes(2)
        const status = autoTable.mock.calls[0][1]
        expect(status.body[0][0]).toBe("Concluído")
        const porDia = autoTable.mock.calls[1][1]
        expect(porDia.body).toHaveLength(2)
    })

    it("agendamentos: dados vazios não desenha tabelas", async () => {
        const autoTable = (await import("jspdf-autotable")).default as any
        const vazio: RelatorioAgendamentos = { total: 0, porStatus: [], porDia: [] }
        const { gerarAgendamentosPdf } = useRelatorioPdf()
        await gerarAgendamentosPdf(vazio, { dataInicio: "", dataFim: "" })
        expect(autoTable).not.toHaveBeenCalled()
        expect(docMock.save).toHaveBeenCalledOnce()
    })

    it("CSV: export de faturamento monta BOM + cabeçalho + dados", () => {
        const { exportarFaturamentoCsv } = useRelatorioPdf()
        // mockar Blob/URL.createObjectURL — happy-dom já tem, basta espionar
        const origCreate = URL.createObjectURL
        URL.createObjectURL = vi.fn(() => "blob:fake")
        URL.revokeObjectURL = vi.fn()
        const click = vi.fn()
        const linkOrig = document.createElement
        document.createElement = vi.fn((tag: string) => {
            if (tag === "a") return { click, set href(_v: string) {}, set download(_v: string) {} } as any
            return linkOrig.call(document, tag)
        }) as any

        exportarFaturamentoCsv(dadosFaturamento)
        expect(click).toHaveBeenCalled()

        document.createElement = linkOrig
        URL.createObjectURL = origCreate
    })
})
