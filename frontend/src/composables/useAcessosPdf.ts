import type { AcessoResumo } from "@/services/acessoService"
import type { Paciente } from "@/services/pacienteService"
import { useTenantStore } from "@/stores/tenantStore"

/**
 * PDF institucional do relatório de acessos LGPD (Art. 9º/18).
 * Padrão: usePdfHeader (Nunito, logo, cabeçalho, marca d'água, rodapé).
 * Minimização: cabeçalho inclui nome do paciente (entregue ao titular),
 *              sem CPF/telefone (briefing §11.6/CA12).
 */
export function useAcessosPdf() {
    async function gerarPdf(
        acessos: AcessoResumo[],
        paciente: Paciente,
        total: number,
    ): Promise<void> {
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

        const left = PDF_MARGIN.side

        const subtitulo = `Paciente: ${paciente.nomeCompleto}`

        let y = desenharCabecalho(doc, est, logo, {
            docTitle: "RELATÓRIO DE ACESSOS — LGPD",
            docSubtitle: subtitulo,
        })

        // Aviso LGPD acima da tabela
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        doc.text(
            "Relatório gerado em cumprimento ao direito de acesso (Art. 9º e Art. 18 da LGPD).",
            left,
            y,
        )
        y += 5

        if (acessos.length === 0) {
            // Empty state
            const w = doc.internal.pageSize.getWidth()
            const right = w - PDF_MARGIN.side
            doc.setDrawColor(203, 213, 225)
            doc.setLineWidth(0.3)
            doc.setLineDashPattern([1, 1], 0)
            doc.roundedRect(left, y, right - left, 20, 2, 2, "D")
            doc.setLineDashPattern([], 0)
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(9)
            doc.setTextColor(148, 163, 184)
            doc.text("Nenhum acesso registrado para este paciente.", left + (right - left) / 2, y + 10, {
                align: "center",
                baseline: "middle",
            })
        } else {
            autoTable(doc, {
                startY: y,
                head: [["Quem acessou", "O quê", "Quando"]],
                body: acessos.map(a => [
                    a.quem,
                    a.acao,
                    formatarDataHora(a.quando),
                ]),
                styles: {
                    font: NUNITO_FAMILY,
                    fontSize: 8.5,
                    cellPadding: 2.5,
                    textColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                },
                headStyles: {
                    font: NUNITO_FAMILY,
                    fillColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                    textColor: 255,
                    fontStyle: "bold",
                },
                margin: { left, right: PDF_MARGIN.side },
                columnStyles: {
                    0: { cellWidth: 50 },
                    1: { cellWidth: "auto" },
                    2: { cellWidth: 38, halign: "right" },
                },
                theme: "grid",
            })

            // Nota de rodapé quando total > acessos exibidos (teto de 500)
            if (total > acessos.length) {
                const afterTable = (doc as any).lastAutoTable.finalY + 4
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(7.5)
                doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
                doc.text(
                    `Exibindo ${acessos.length} dos ${total} acessos registrados (mais recentes primeiro).`,
                    left,
                    afterTable,
                )
            }
        }

        finalizarPaginas(doc, {
            avisoDireita: "Relatório de acessos — Art. 9º/18 LGPD.",
        })

        const nomeArquivo = `relatorio-acessos-${paciente.id}.pdf`
        doc.save(nomeArquivo)
    }

    return { gerarPdf }
}

function formatarDataHora(iso: string): string {
    try {
        const d = new Date(iso)
        return (
            d.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" }) +
            " " +
            d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
        )
    } catch {
        return iso
    }
}
