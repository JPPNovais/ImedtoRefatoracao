/**
 * Gera o PDF de um atestado seguindo o mesmo layout institucional dos demais
 * documentos clínicos (cabeçalho com logo + bloco do paciente + marca d'água
 * "IMEDTO" + rodapé com assinatura). Lazy import — só baixa jsPDF + Nunito
 * quando o usuário clica em "Emitir".
 */
import type { Atestado } from "@/services/atestadoService"
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
        case "Afastamento":    return "Atestado de Afastamento"
        case "Comparecimento": return "Atestado de Comparecimento"
        case "Aptidao":        return "Atestado de Aptidão"
        case "Outro":          return "Atestado Médico"
        default:               return "Atestado Médico"
    }
}

export function useAtestadoPdf() {
    async function gerarPdf(
        atestado: Atestado,
        paciente: Paciente,
        modo: PdfSaidaModo = "download",
    ): Promise<PdfResultado> {
        const [{ jsPDF }, helper] = await Promise.all([
            import("jspdf"),
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
            docTitle: tipoLabel(atestado.tipo).toUpperCase(),
            docSubtitle: `Emitido em ${dataFmt(atestado.criadoEm)}`,
        })

        y = desenharBlocoPaciente(doc, paciente, y)

        // Metadados curtos
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        const meta: string[] = []
        if (atestado.tipo === "Afastamento" && atestado.diasAfastamento)
            meta.push(`Afastamento: ${atestado.diasAfastamento} dia(s)`)
        if (atestado.cid10) meta.push(`CID-10: ${atestado.cid10}`)
        if (meta.length > 0) {
            doc.text(meta.join("   ·   "), left, y + 1)
            y += 6
        }

        // Corpo do atestado
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(11)
        doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
        const linhas = doc.splitTextToSize(atestado.conteudo, right - left)
        doc.text(linhas, left, y + 4)

        // Rodapé com assinatura
        finalizarPaginas(doc, {
            assinatura: {
                nome: atestado.profissionalNome ?? "Profissional responsável",
                aviso: "Assine manualmente no espaço acima",
            },
        })

        const arquivo = `atestado-${slug(paciente.nomeCompleto)}-${slug(atestado.tipo)}.pdf`

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
