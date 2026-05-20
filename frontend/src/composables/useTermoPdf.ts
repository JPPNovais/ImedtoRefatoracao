/**
 * Gera o PDF de um Termo de Consentimento emitido, seguindo o mesmo layout
 * institucional dos outros documentos clínicos (cabeçalho com logo + bloco do
 * paciente + marca d'água + rodapé). Lazy import — só baixa jsPDF + Nunito
 * quando o usuário clica em "Gerar PDF".
 *
 * O termo já vem com o conteúdo HTML resolvido pelo backend (snapshot imutável).
 * Aqui apenas convertemos esse HTML em texto rico simples (parágrafos +
 * negritos básicos) e mandamos pro jsPDF.
 *
 * Marca d'água:
 *   - Status "Pendente"  → "AGUARDANDO ASSINATURA" (chamativo)
 *   - Status "Assinado"  → "IMEDTO" sutil
 *   - Status "Revogado"  → "REVOGADO" diagonal vermelho
 *   - Demais             → "IMEDTO"
 */
import type { TermoEmitidoDetalhe } from "@/services/pacienteTermoService"
import type { Paciente } from "@/services/pacienteService"
import { useTenantStore } from "@/stores/tenantStore"

export type PdfSaidaModo = "download" | "visualizar"
export interface PdfResultado { blobUrl: string | null }

function slug(nome: string): string {
    return nome.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "")
        .replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "") || "paciente"
}

/**
 * Converte HTML do snapshot em "blocos textuais" pra jsPDF. Não pretende ser
 * renderer completo — só lida com `<p>`, `<br>`, `<h1>`-`<h3>`, `<strong>`,
 * `<em>`, `<ul>`/`<ol>`/`<li>`. O resto vira parágrafo simples.
 */
interface BlocoTexto {
    tipo: "p" | "h1" | "h2" | "h3" | "li"
    texto: string
}

function htmlParaBlocos(html: string): BlocoTexto[] {
    if (!html) return []
    const doc = new DOMParser().parseFromString(`<div>${html}</div>`, "text/html")
    const root = doc.body.firstElementChild
    if (!root) return []

    const blocos: BlocoTexto[] = []

    function visit(node: Node) {
        if (node.nodeType === Node.TEXT_NODE) {
            const txt = (node.textContent || "").trim()
            if (txt) blocos.push({ tipo: "p", texto: txt })
            return
        }
        if (node.nodeType !== Node.ELEMENT_NODE) return
        const el = node as HTMLElement
        const tag = el.tagName.toLowerCase()
        const inner = textoLimpo(el)
        switch (tag) {
            case "h1": if (inner) blocos.push({ tipo: "h1", texto: inner }); return
            case "h2": if (inner) blocos.push({ tipo: "h2", texto: inner }); return
            case "h3": if (inner) blocos.push({ tipo: "h3", texto: inner }); return
            case "p":
            case "div":
            case "blockquote":
                if (inner) blocos.push({ tipo: "p", texto: inner })
                return
            case "ul":
            case "ol":
                el.querySelectorAll(":scope > li").forEach(li => {
                    const t = textoLimpo(li as HTMLElement)
                    if (t) blocos.push({ tipo: "li", texto: t })
                })
                return
            case "br":
                blocos.push({ tipo: "p", texto: "" })
                return
            default:
                el.childNodes.forEach(visit)
        }
    }

    root.childNodes.forEach(visit)
    return blocos
}

function textoLimpo(el: HTMLElement): string {
    // Preserva quebras com `\n` para `<br>` aninhado, comprime espaços.
    const clone = el.cloneNode(true) as HTMLElement
    clone.querySelectorAll("br").forEach(br => br.replaceWith(document.createTextNode("\n")))
    return (clone.textContent || "").replace(/\s+\n/g, "\n").replace(/\n{3,}/g, "\n\n").replace(/[ \t]+/g, " ").trim()
}

// ─── Públicos ────────────────────────────────────────────────────────────────

export function useTermoPdf() {
    /**
     * Gera o PDF do termo emitido. `paciente` vem da view (já carregado), evita
     * uma chamada extra.
     */
    async function gerarPdf(
        termo: TermoEmitidoDetalhe,
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
        const h = doc.internal.pageSize.getHeight()
        const left = PDF_MARGIN.side
        const right = w - PDF_MARGIN.side
        const larguraTexto = right - left
        const bottomLimite = h - PDF_MARGIN.bottom - 20 // reserva pro rodapé

        let y = desenharCabecalho(doc, est, logo, {
            docTitle: `TERMO DE CONSENTIMENTO — ${(termo.categoria || "").toUpperCase()}`,
            docSubtitle: `Modelo: ${termo.termoModeloTitulo} (v${termo.versaoModelo})`,
        })

        y = desenharBlocoPaciente(doc, paciente, y)

        // ── Metadados curtos ────────────────────────────────────────────────
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
        const meta: string[] = []
        meta.push(`Emitido em ${fmtDataHora(termo.criadoEm)}`)
        if (termo.emitidoPorNome) meta.push(`Emitido por: ${termo.emitidoPorNome}`)
        meta.push(`ID #${termo.id}`)
        doc.text(meta.join("   ·   "), left, y + 1)
        y += 6

        // ── Corpo: blocos extraídos do HTML ─────────────────────────────────
        const blocos = htmlParaBlocos(termo.conteudoSnapshotHtml || "")

        // Helper local: empurra texto, paginando quando estourar.
        const desenharLinhas = (linhas: string[], fontSize: number, font: "normal" | "semibold" | "bold", cor: [number, number, number], leading: number) => {
            doc.setFont(NUNITO_FAMILY, font)
            doc.setFontSize(fontSize)
            doc.setTextColor(cor[0], cor[1], cor[2])
            for (const linha of linhas) {
                if (y + leading > bottomLimite) {
                    doc.addPage()
                    y = PDF_MARGIN.top
                }
                doc.text(linha, left, y)
                y += leading
            }
        }

        for (const b of blocos) {
            if (!b.texto && b.tipo === "p") {
                y += 3
                continue
            }
            if (b.tipo === "h1") {
                y += 4
                const linhas = doc.splitTextToSize(b.texto, larguraTexto)
                desenharLinhas(linhas, 14, "bold", PDF_THEME.inkTitle, 6.5)
                y += 1.5
            } else if (b.tipo === "h2") {
                y += 3
                const linhas = doc.splitTextToSize(b.texto, larguraTexto)
                desenharLinhas(linhas, 12, "bold", PDF_THEME.inkTitle, 5.8)
                y += 1
            } else if (b.tipo === "h3") {
                y += 2
                const linhas = doc.splitTextToSize(b.texto, larguraTexto)
                desenharLinhas(linhas, 10.5, "semibold", PDF_THEME.inkTitle, 5)
            } else if (b.tipo === "li") {
                const indent = 5
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(10.5)
                const linhas = doc.splitTextToSize(b.texto, larguraTexto - indent)
                if (linhas.length > 0) {
                    if (y + 5 > bottomLimite) { doc.addPage(); y = PDF_MARGIN.top }
                    doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
                    doc.text("•", left, y)
                    doc.text(linhas[0], left + indent, y)
                    y += 5
                    for (let i = 1; i < linhas.length; i++) {
                        if (y + 5 > bottomLimite) { doc.addPage(); y = PDF_MARGIN.top }
                        doc.text(linhas[i], left + indent, y)
                        y += 5
                    }
                }
            } else {
                const linhas = doc.splitTextToSize(b.texto, larguraTexto)
                desenharLinhas(linhas, 10.5, "normal", PDF_THEME.ink, 5)
                y += 1.5
            }
        }

        // ── Bloco de revogação (se houver) ──────────────────────────────────
        if (termo.status === "Revogado" && termo.revogadoEm) {
            y += 4
            if (y + 20 > bottomLimite) { doc.addPage(); y = PDF_MARGIN.top }
            doc.setFillColor(254, 226, 226)
            doc.setDrawColor(PDF_THEME.danger[0], PDF_THEME.danger[1], PDF_THEME.danger[2])
            doc.setLineWidth(0.4)
            doc.roundedRect(left, y, larguraTexto, 18, 1.5, 1.5, "FD")
            doc.setFont(NUNITO_FAMILY, "bold")
            doc.setFontSize(11)
            doc.setTextColor(PDF_THEME.danger[0], PDF_THEME.danger[1], PDF_THEME.danger[2])
            doc.text(`REVOGADO EM ${fmtDataHora(termo.revogadoEm)}`, left + 3, y + 6)
            if (termo.revogadoMotivo) {
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(9)
                doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
                const motivo = doc.splitTextToSize(`Motivo: ${termo.revogadoMotivo}`, larguraTexto - 6)
                doc.text(motivo, left + 3, y + 11)
            }
            y += 22
        }

        // ── Rodapé + watermark conforme status ──────────────────────────────
        const watermark = montarWatermark(termo.status, PDF_THEME.danger)

        finalizarPaginas(doc, {
            assinatura: {
                nome: paciente.nomeCompleto,
                aviso: termo.status === "Pendente"
                    ? "Assine no espaço acima e devolva o PDF assinado para anexar."
                    : termo.status === "Assinado"
                        ? "Documento assinado fisicamente — PDF original em anexo."
                        : "Documento sem assinatura ativa.",
            },
            nota: `Hash: ${(termo.hashIntegridade || "").slice(0, 16)}…`,
        }, watermark)

        const arquivo = `termo-${slug(termo.termoModeloTitulo)}-${slug(paciente.nomeCompleto)}.pdf`

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

function montarWatermark(status: string, danger: [number, number, number]) {
    if (status === "Pendente") {
        return { texto: "AGUARDANDO ASSINATURA", opacidade: 0.10, fontSize: 70 }
    }
    if (status === "Revogado") {
        return { texto: "REVOGADO", cor: danger, opacidade: 0.14, fontSize: 110 }
    }
    return {}
}

function fmtDataHora(iso: string): string {
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return iso
    return d.toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
}
