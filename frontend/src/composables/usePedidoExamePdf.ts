/**
 * Gera o PDF de um pedido de exame seguindo o mesmo layout institucional.
 * Lista de exames vai numerada em tabela compacta.
 */
import type { PedidoExame } from "@/services/pedidoExameService"
import type { Paciente } from "@/services/pacienteService"
import { useTenantStore } from "@/stores/tenantStore"

export type PdfSaidaModo = "download" | "visualizar"
export interface PdfResultado { blobUrl: string | null }

function slug(nome: string): string {
    return nome.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "")
        .replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "") || "paciente"
}

function dataFmt(iso: string): string {
    return new Date(iso).toLocaleDateString("pt-BR")
}

function tipoLabel(tipo: string): string {
    switch (tipo) {
        case "Laboratorial": return "Solicitação de Exames Laboratoriais"
        case "Imagem":       return "Solicitação de Exames de Imagem"
        case "Misto":        return "Solicitação de Exames"
        default:             return "Solicitação de Exames"
    }
}

export function usePedidoExamePdf() {
    async function gerarPdf(
        pedido: PedidoExame,
        paciente: Paciente,
        modo: PdfSaidaModo = "download",
    ): Promise<PdfResultado> {
        const [{ jsPDF }, { default: autoTable }, helper] = await Promise.all([
            import("jspdf"),
            import("jspdf-autotable"),
            import("@/composables/usePdfHeader"),
        ])
        const {
            registrarFontesNunito,
            carregarEstabelecimentoAtivo,
            carregarLogoComoDataUrl,
            desenharCabecalho,
            desenharBlocoPaciente,
            finalizarPaginas,
            PDF_MARGIN,
            PDF_THEME,
            NUNITO_FAMILY,
        } = helper

        const tenant = useTenantStore()
        const est = await carregarEstabelecimentoAtivo(tenant.estabelecimentoAtivoId)
        const logo = await carregarLogoComoDataUrl(est)

        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })
        registrarFontesNunito(doc)

        const w = doc.internal.pageSize.getWidth()
        const left = PDF_MARGIN.side
        const right = w - PDF_MARGIN.side

        let y = desenharCabecalho(doc, est, logo, {
            docTitle: tipoLabel(pedido.tipo).toUpperCase(),
            docSubtitle: `Emitido em ${dataFmt(pedido.criadoEm)}`,
        })

        y = desenharBlocoPaciente(doc, paciente, y)

        // Indicação clínica
        doc.setFont(NUNITO_FAMILY, "semibold")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
        doc.text("INDICAÇÃO CLÍNICA", left, y + 1, { charSpace: 0.3 })

        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(10)
        doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
        const indicLinhas = doc.splitTextToSize(pedido.indicacaoClinica, right - left)
        doc.text(indicLinhas, left, y + 6)
        y += 6 + indicLinhas.length * 5

        if (pedido.cid10) {
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(9)
            doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
            doc.text(`CID-10: ${pedido.cid10}`, left, y + 2)
            y += 6
        }

        // Lista de exames numerada
        doc.setFont(NUNITO_FAMILY, "semibold")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
        doc.text("EXAMES SOLICITADOS", left, y + 3, { charSpace: 0.3 })

        autoTable(doc, {
            startY: y + 6,
            head: [["#", "Exame"]],
            body: pedido.exames.map((e, i) => [String(i + 1), e]),
            styles: {
                font: NUNITO_FAMILY,
                fontSize: 9.5,
                cellPadding: 2.5,
                textColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                lineColor: [PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2]],
                lineWidth: 0.1,
            },
            headStyles: {
                fontStyle: "bold",
                fillColor: [PDF_THEME.cardBg[0], PDF_THEME.cardBg[1], PDF_THEME.cardBg[2]],
                textColor: [PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2]],
            },
            columnStyles: {
                0: { cellWidth: 12, halign: "center" },
                1: { cellWidth: "auto" },
            },
            margin: { left, right: PDF_MARGIN.side },
            theme: "plain",
        })

        y = (doc as any).lastAutoTable.finalY + 6

        if (pedido.observacoes) {
            doc.setFont(NUNITO_FAMILY, "semibold")
            doc.setFontSize(9)
            doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
            doc.text("OBSERVAÇÕES", left, y, { charSpace: 0.3 })
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(9.5)
            doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
            const obsLinhas = doc.splitTextToSize(pedido.observacoes, right - left)
            doc.text(obsLinhas, left, y + 5)
        }

        finalizarPaginas(doc, {
            assinatura: {
                nome: pedido.profissionalNome ?? "Profissional responsável",
                aviso: "Assine manualmente no espaço acima",
            },
        })

        const arquivo = `pedido-exame-${slug(paciente.nomeCompleto)}.pdf`

        if (modo === "download") {
            doc.save(arquivo)
            return { blobUrl: null }
        }
        doc.setProperties({ title: arquivo.replace(/\.pdf$/i, "") })
        const blobUrl: string = doc.output("bloburl") as unknown as string
        setTimeout(() => { try { URL.revokeObjectURL(blobUrl) } catch { /* ignore */ } }, 60_000)
        return { blobUrl }
    }

    return { gerarPdf }
}
