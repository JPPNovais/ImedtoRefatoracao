import { jsPDF } from "jspdf"
import autoTable from "jspdf-autotable"
import type { Orcamento } from "@/services/orcamentoService"

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function dataFmt(s: string) {
    return new Date(s).toLocaleDateString("pt-BR")
}

export function useOrcamentoPdf() {
    function gerarPdf(orc: Orcamento) {
        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })
        const pageW = doc.internal.pageSize.getWidth()

        // Cabeçalho
        doc.setFontSize(20)
        doc.setFont("helvetica", "bold")
        doc.text("Imedto", 14, 20)

        doc.setFontSize(10)
        doc.setFont("helvetica", "normal")
        doc.setTextColor(100)
        doc.text("Sistema de Gestão em Saúde", 14, 27)

        // Linha separadora
        doc.setDrawColor(200)
        doc.line(14, 32, pageW - 14, 32)

        // Título do documento
        doc.setFontSize(16)
        doc.setFont("helvetica", "bold")
        doc.setTextColor(30)
        doc.text(`Orçamento ${orc.numero}`, 14, 42)

        const statusCor: Record<string, [number, number, number]> = {
            Pendente:  [146, 64, 14],
            Aprovado:  [6, 95, 70],
            Recusado:  [153, 27, 27],
            Expirado:  [107, 114, 128],
        }
        const [r, g, b] = statusCor[orc.status] ?? [50, 50, 50]
        doc.setTextColor(r, g, b)
        doc.setFontSize(11)
        doc.text(orc.status, pageW - 14 - doc.getTextWidth(orc.status), 42)
        doc.setTextColor(30)

        // Dados do paciente / datas
        doc.setFontSize(10)
        doc.setFont("helvetica", "normal")
        let y = 54
        doc.text(`Paciente: ${orc.pacienteNome}`, 14, y)
        doc.text(`Validade: ${dataFmt(orc.validade)}`, pageW - 14 - 55, y)
        y += 7
        doc.text(`Emitido por: ${orc.criadoPorNome}`, 14, y)
        doc.text(`Emitido em: ${dataFmt(orc.criadoEm)}`, pageW - 14 - 55, y)

        if (orc.observacoes) {
            y += 10
            doc.setFont("helvetica", "bold")
            doc.text("Observações:", 14, y)
            y += 5
            doc.setFont("helvetica", "normal")
            const linhas = doc.splitTextToSize(orc.observacoes, pageW - 28)
            doc.text(linhas, 14, y)
            y += linhas.length * 5
        }

        y += 6

        // Tabela de itens
        autoTable(doc, {
            startY: y,
            head: [["Descrição", "Qtd.", "Valor Unit.", "Desc. %", "Subtotal"]],
            body: orc.itens.map(i => [
                i.descricao,
                i.quantidade.toLocaleString("pt-BR"),
                moeda(i.valorUnitario),
                `${i.descontoPercent}%`,
                moeda(i.subtotal),
            ]),
            foot: [["", "", "", "Total:", moeda(orc.total)]],
            styles: { fontSize: 9 },
            headStyles: { fillColor: [37, 99, 235], textColor: 255 },
            footStyles: { fillColor: [243, 244, 246], textColor: 30, fontStyle: "bold" },
            columnStyles: {
                1: { halign: "right" },
                2: { halign: "right" },
                3: { halign: "right" },
                4: { halign: "right" },
            },
        })

        // Rodapé
        const rodapeY = doc.internal.pageSize.getHeight() - 10
        doc.setFontSize(8)
        doc.setTextColor(150)
        doc.text("Imedto — Sistema de Gestão em Saúde  |  contato@imedto.com", 14, rodapeY)
        doc.text(`Página 1`, pageW - 14 - doc.getTextWidth("Página 1"), rodapeY)

        doc.save(`orcamento-${orc.numero}.pdf`)
    }

    return { gerarPdf }
}
