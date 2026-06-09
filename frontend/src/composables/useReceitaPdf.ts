/**
 * Gera o PDF de uma receita seguindo o mesmo layout institucional dos demais
 * documentos clínicos (cabeçalho com logo + bloco do paciente + marca d'água
 * "IMEDTO" + rodapé com assinatura). Substitui a impressão via HTML inline.
 *
 * Receita controlada usa o accent vermelho (PDF_THEME.danger) no cabeçalho.
 * Lazy import — só baixa jsPDF + Nunito quando o usuário clica em "Imprimir".
 */
import type { Receita } from "@/services/receitaService"
import { VIAS_ADMINISTRACAO } from "@/services/receitaService"
import type { Paciente } from "@/services/pacienteService"
import { useTenantStore } from "@/stores/tenantStore"

export type PdfSaidaModo = "download" | "visualizar"
export interface PdfResultado { blobUrl: string | null }

function slug(nome: string): string {
    return nome.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "")
        .replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "") || "paciente"
}

function dataFmt(iso: string | null): string {
    if (!iso) return "—"
    const d = new Date(iso)
    return Number.isNaN(d.getTime()) ? "—" : d.toLocaleDateString("pt-BR")
}

function tipoLabel(tipo: string, tipoNotificacao: string | null): string {
    const base = tipo === "Controlada"  ? "Receita de Controle Especial"
        : tipo === "Antibiotico" ? "Receita de Antibiótico"
        : tipo === "Especial"    ? "Receita Especial"
        : "Receita Médica"
    return tipoNotificacao ? `${base} — Notificação ${tipoNotificacao}` : base
}

function viaLabel(via: string | null | undefined): string {
    if (!via) return ""
    return VIAS_ADMINISTRACAO.find(v => v.valor === via)?.label ?? via
}

export function useReceitaPdf() {
    async function gerarPdf(
        receita: Receita,
        paciente: Paciente,
        modo: PdfSaidaModo = "visualizar",
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
        const largura = right - left
        const controlada = receita.tipo === "Controlada"
        const accent = controlada ? PDF_THEME.danger : undefined
        const limiteY = h - PDF_MARGIN.bottom - 12

        let y = desenharCabecalho(doc, est, logo, {
            docTitle: tipoLabel(receita.tipo, receita.tipoNotificacao).toUpperCase(),
            docSubtitle: `Emitida em ${dataFmt(receita.emitidaEm)}`,
            accentOverride: accent,
        })

        y = desenharBlocoPaciente(doc, paciente, y)

        // Validade (quando houver)
        if (receita.validadeAte) {
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(9)
            doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
            doc.text(`Válida até ${dataFmt(receita.validadeAte)}`, left, y + 1)
            y += 6
        }

        // Título da seção de prescrição
        doc.setFont(NUNITO_FAMILY, "semibold")
        doc.setFontSize(9)
        doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
        doc.text("PRESCRIÇÃO", left, y + 3, { charSpace: 0.3 })
        y += 8

        // Garante espaço; senão quebra a página (rodapé/watermark são pintados no fim).
        const garantirEspaco = (alturaNecessaria: number) => {
            if (y + alturaNecessaria > limiteY) {
                doc.addPage()
                y = PDF_MARGIN.top
            }
        }

        // Itens da prescrição — blocos numerados
        receita.itens.forEach((it, i) => {
            const indent = left + 7

            // Pré-mede as linhas para estimar a altura do bloco e decidir quebra.
            const tituloItem = `${it.medicamento}${it.concentracao ? " — " + it.concentracao : ""}`
            const tituloLinhas = doc.splitTextToSize(tituloItem, largura - 7)
            const posologiaLinhas = it.posologia
                ? doc.splitTextToSize(it.posologia, largura - 7)
                : []
            const obsLinhas = it.observacao
                ? doc.splitTextToSize(it.observacao, largura - 7)
                : []
            const subInfo = [it.formaFarmaceutica, it.quantidade].filter(Boolean).join(" · ")
            const viaDur = [
                it.via ? `Via: ${viaLabel(it.via)}` : "",
                it.duracao ? `Duração: ${it.duracao}` : "",
            ].filter(Boolean).join("    ·    ")

            const altura = tituloLinhas.length * 5
                + (subInfo ? 4.5 : 0)
                + posologiaLinhas.length * 4.5 + (posologiaLinhas.length ? 1.5 : 0)
                + (viaDur ? 4.5 : 0)
                + obsLinhas.length * 4 + (obsLinhas.length ? 1 : 0)
                + 6
            garantirEspaco(altura)

            // Número
            doc.setFont(NUNITO_FAMILY, "bold")
            doc.setFontSize(10)
            doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
            doc.text(`${i + 1}.`, left, y)

            // Medicamento (— concentração)
            doc.setFont(NUNITO_FAMILY, "bold")
            doc.setFontSize(10.5)
            doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
            doc.text(tituloLinhas, indent, y)
            y += tituloLinhas.length * 5

            // Forma farmacêutica · quantidade
            if (subInfo) {
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(9)
                doc.setTextColor(PDF_THEME.secondary[0], PDF_THEME.secondary[1], PDF_THEME.secondary[2])
                doc.text(subInfo, indent, y)
                y += 4.5
            }

            // Posologia
            if (posologiaLinhas.length) {
                y += 1.5
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(10)
                doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
                doc.text(posologiaLinhas, indent, y)
                y += posologiaLinhas.length * 4.5
            }

            // Via / Duração
            if (viaDur) {
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(9)
                doc.setTextColor(PDF_THEME.mute[0], PDF_THEME.mute[1], PDF_THEME.mute[2])
                doc.text(viaDur, indent, y)
                y += 4.5
            }

            // Observação do item
            if (obsLinhas.length) {
                y += 1
                doc.setFont(NUNITO_FAMILY, "normal")
                doc.setFontSize(8.5)
                doc.setTextColor(PDF_THEME.muteLight[0], PDF_THEME.muteLight[1], PDF_THEME.muteLight[2])
                doc.text(obsLinhas, indent, y)
                y += obsLinhas.length * 4
            }

            // Separador fino entre itens
            y += 3
            if (i < receita.itens.length - 1) {
                doc.setDrawColor(PDF_THEME.border[0], PDF_THEME.border[1], PDF_THEME.border[2])
                doc.setLineWidth(0.1)
                doc.line(indent, y, right, y)
                y += 3
            }
        })

        // Observações gerais
        if (receita.observacoes) {
            const obsGeralLinhas = doc.splitTextToSize(receita.observacoes, largura)
            garantirEspaco(8 + obsGeralLinhas.length * 4.5)
            y += 4
            doc.setFont(NUNITO_FAMILY, "semibold")
            doc.setFontSize(9)
            doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
            doc.text("ORIENTAÇÕES", left, y, { charSpace: 0.3 })
            y += 5
            doc.setFont(NUNITO_FAMILY, "normal")
            doc.setFontSize(9.5)
            doc.setTextColor(PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2])
            doc.text(obsGeralLinhas, left, y)
            y += obsGeralLinhas.length * 4.5
        }

        // Box de aviso — receita não assinada digitalmente (CFM 2.299/2021)
        const avisoTexto = "Atenção: esta receita não foi assinada digitalmente (ICP-Brasil / Memed). "
            + "Para validade jurídica plena em farmácias que exigem assinatura digital, o profissional "
            + "deve assinar manualmente o documento impresso (CFM 2.299/2021)."
        doc.setFont(NUNITO_FAMILY, "normal")
        doc.setFontSize(8)
        const avisoLinhas = doc.splitTextToSize(avisoTexto, largura - 7)
        const avisoAltura = avisoLinhas.length * 3.8 + 6
        garantirEspaco(avisoAltura + 6)
        y += 6
        doc.setFillColor(254, 243, 199)   // #fef3c7
        doc.setDrawColor(251, 191, 36)    // #fbbf24
        doc.setLineWidth(0.2)
        doc.roundedRect(left, y, largura, avisoAltura, 1.5, 1.5, "FD")
        doc.setTextColor(124, 45, 18)     // #7c2d12
        doc.text(avisoLinhas, left + 3.5, y + 4.5)

        // Rodapé com assinatura manual + marca d'água em todas as páginas
        finalizarPaginas(doc, {
            assinatura: {
                nome: receita.profissionalNome ?? "Profissional responsável",
                aviso: "Assine manualmente no espaço acima",
            },
        }, controlada ? { fontSize: 120 } : {})

        const arquivo = `receita-${slug(paciente.nomeCompleto)}.pdf`

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
