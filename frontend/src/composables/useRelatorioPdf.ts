import { jsPDF } from "jspdf"
import autoTable from "jspdf-autotable"
import type { FaturamentoCategoria, RelatorioAgendamentos } from "@/services/dashboardService"

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function rodape(doc: jsPDF, titulo: string) {
    const y = doc.internal.pageSize.getHeight() - 10
    const w = doc.internal.pageSize.getWidth()
    doc.setFontSize(8)
    doc.setTextColor(150)
    doc.text(`Imedto — ${titulo}  |  ${new Date().toLocaleDateString("pt-BR")}`, 14, y)
    doc.text("Página 1", w - 14 - doc.getTextWidth("Página 1"), y)
}

export function useRelatorioPdf() {
    function gerarFaturamentoPdf(
        dados: FaturamentoCategoria[],
        periodo: { dataInicio: string; dataFim: string },
    ) {
        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })

        doc.setFontSize(18)
        doc.setFont("helvetica", "bold")
        doc.text("Relatório de Faturamento", 14, 22)

        doc.setFontSize(10)
        doc.setFont("helvetica", "normal")
        doc.setTextColor(100)
        if (periodo.dataInicio || periodo.dataFim) {
            doc.text(`Período: ${periodo.dataInicio || "—"} a ${periodo.dataFim || "—"}`, 14, 30)
        } else {
            doc.text("Período: todos", 14, 30)
        }
        doc.setTextColor(30)

        const receitas = dados.filter(d => d.tipo === "Receita")
        const despesas = dados.filter(d => d.tipo === "Despesa")
        const totalReceita = receitas.reduce((s, d) => s + d.totalPago, 0)
        const totalDespesa = despesas.reduce((s, d) => s + d.totalPago, 0)

        autoTable(doc, {
            startY: 38,
            head: [["Tipo", "Categoria", "Qtd.", "Total Pago", "Total Pendente"]],
            body: dados.map(d => [
                d.tipo,
                d.categoria,
                d.quantidade,
                moeda(d.totalPago),
                moeda(d.totalPendente),
            ]),
            foot: [[
                "", "TOTAL RECEITAS / DESPESAS", "",
                `R: ${moeda(totalReceita)} | D: ${moeda(totalDespesa)}`,
                `Saldo: ${moeda(totalReceita - totalDespesa)}`,
            ]],
            styles: { fontSize: 9 },
            headStyles: { fillColor: [37, 99, 235], textColor: 255 },
            footStyles: { fillColor: [243, 244, 246], fontStyle: "bold" },
            columnStyles: {
                2: { halign: "right" },
                3: { halign: "right" },
                4: { halign: "right" },
            },
        })

        rodape(doc, "Relatório de Faturamento")
        doc.save("relatorio-faturamento.pdf")
    }

    function gerarAgendamentosPdf(
        dados: RelatorioAgendamentos,
        periodo: { dataInicio: string; dataFim: string },
    ) {
        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })

        doc.setFontSize(18)
        doc.setFont("helvetica", "bold")
        doc.text("Relatório de Agendamentos", 14, 22)

        doc.setFontSize(10)
        doc.setFont("helvetica", "normal")
        doc.setTextColor(100)
        if (periodo.dataInicio || periodo.dataFim) {
            doc.text(`Período: ${periodo.dataInicio || "—"} a ${periodo.dataFim || "—"}`, 14, 30)
        } else {
            doc.text("Período: todos", 14, 30)
        }
        doc.setTextColor(30)

        doc.setFontSize(11)
        doc.setFont("helvetica", "bold")
        doc.text(`Total: ${dados.total} agendamentos`, 14, 40)

        autoTable(doc, {
            startY: 48,
            head: [["Status", "Quantidade", "% do Total"]],
            body: dados.porStatus.map(s => [
                s.status,
                s.quantidade,
                dados.total > 0 ? `${Math.round((s.quantidade / dados.total) * 100)}%` : "0%",
            ]),
            styles: { fontSize: 9 },
            headStyles: { fillColor: [37, 99, 235], textColor: 255 },
        })

        const afterStatus = (doc as any).lastAutoTable.finalY + 10

        if (dados.porDia.length > 0) {
            doc.setFontSize(11)
            doc.setFont("helvetica", "bold")
            doc.text("Por Dia", 14, afterStatus)

            autoTable(doc, {
                startY: afterStatus + 6,
                head: [["Data", "Quantidade"]],
                body: dados.porDia.map(d => [
                    new Date(d.data).toLocaleDateString("pt-BR"),
                    d.quantidade,
                ]),
                styles: { fontSize: 9 },
                headStyles: { fillColor: [37, 99, 235], textColor: 255 },
            })
        }

        rodape(doc, "Relatório de Agendamentos")
        doc.save("relatorio-agendamentos.pdf")
    }

    function exportarFaturamentoCsv(dados: FaturamentoCategoria[]) {
        const linhas = [
            ["Tipo", "Categoria", "Quantidade", "Total Pago (R$)", "Total Pendente (R$)"],
            ...dados.map(d => [
                d.tipo,
                d.categoria,
                d.quantidade.toString(),
                d.totalPago.toFixed(2).replace(".", ","),
                d.totalPendente.toFixed(2).replace(".", ","),
            ]),
        ]
        const csv = linhas.map(l => l.map(c => `"${c}"`).join(";")).join("\r\n")
        baixarCsv(csv, "relatorio-faturamento.csv")
    }

    function exportarAgendamentosCsv(dados: RelatorioAgendamentos) {
        const linhas = [
            ["Status", "Quantidade"],
            ...dados.porStatus.map(s => [s.status, s.quantidade.toString()]),
        ]
        const csv = linhas.map(l => l.map(c => `"${c}"`).join(";")).join("\r\n")
        baixarCsv(csv, "relatorio-agendamentos.csv")
    }

    function baixarCsv(conteudo: string, nomeArquivo: string) {
        const bom = "\uFEFF"
        const blob = new Blob([bom + conteudo], { type: "text/csv;charset=utf-8;" })
        const url = URL.createObjectURL(blob)
        const a = document.createElement("a")
        a.href = url
        a.download = nomeArquivo
        a.click()
        URL.revokeObjectURL(url)
    }

    return {
        gerarFaturamentoPdf,
        gerarAgendamentosPdf,
        exportarFaturamentoCsv,
        exportarAgendamentosCsv,
    }
}
