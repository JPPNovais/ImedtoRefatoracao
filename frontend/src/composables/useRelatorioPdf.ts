import type { FaturamentoCategoria, RelatorioAgendamentos } from "@/services/dashboardService"
import { useTenantStore } from "@/stores/tenantStore"

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function dataLabel(d: string | undefined | null): string {
    if (!d) return "—"
    const dt = new Date(d)
    if (Number.isNaN(dt.getTime())) return d
    return dt.toLocaleDateString("pt-BR")
}

export function useRelatorioPdf() {
    /**
     * PDF de faturamento — segue layout institucional Imedto:
     * cabeçalho com logo + título + bloco "período" (substitui o bloco de
     * paciente) + hero numérico (Receita / Despesa / Saldo) + tabela de
     * categorias + marca d'água + rodapé com aviso "Documento de gestão".
     */
    async function gerarFaturamentoPdf(
        dados: FaturamentoCategoria[],
        periodo: { dataInicio: string; dataFim: string },
    ) {
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

        const w = doc.internal.pageSize.getWidth()
        const left = PDF_MARGIN.side
        const right = w - PDF_MARGIN.side

        let y = desenharCabecalho(doc, est, logo, {
            docTitle: "RELATÓRIO DE FATURAMENTO",
            docSubtitle: `Período ${dataLabel(periodo.dataInicio)} a ${dataLabel(periodo.dataFim)}`,
        })

        // ── KPIs (hero numérico — 4 cartões) ────────────────────────────────
        const receitas = dados.filter(d => d.tipo === "Receita")
        const despesas = dados.filter(d => d.tipo === "Despesa")
        const totalReceita = receitas.reduce((s, d) => s + d.totalPago, 0)
        const totalDespesa = despesas.reduce((s, d) => s + d.totalPago, 0)
        const saldo = totalReceita - totalDespesa

        y = desenharHero(doc, [
            { label: "RECEITAS", valor: moeda(totalReceita), destaque: "verde" },
            { label: "DESPESAS", valor: moeda(totalDespesa), destaque: "vermelho" },
            { label: "SALDO", valor: moeda(saldo), destaque: saldo >= 0 ? "verde" : "vermelho" },
            { label: "CATEGORIAS", valor: String(dados.length) },
        ], y)

        // ── Empty state ─────────────────────────────────────────────────────
        if (dados.length === 0) {
            desenharEmptyState(doc, y, "Sem dados no período selecionado")
        } else {
            autoTable(doc, {
                startY: y,
                head: [["Tipo", "Categoria", "Qtd.", "Total Pago", "Pendente"]],
                body: dados.map(d => [
                    d.tipo,
                    d.categoria,
                    String(d.quantidade),
                    moeda(d.totalPago),
                    moeda(d.totalPendente),
                ]),
                foot: [[
                    "",
                    "TOTAIS",
                    "",
                    moeda(totalReceita - totalDespesa < 0 ? totalReceita : totalReceita),
                    `Saldo: ${moeda(saldo)}`,
                ]],
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
                footStyles: {
                    font: NUNITO_FAMILY,
                    fillColor: [PDF_THEME.cardBg[0], PDF_THEME.cardBg[1], PDF_THEME.cardBg[2]],
                    textColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                    fontStyle: "bold",
                },
                margin: { left, right: PDF_MARGIN.side },
                columnStyles: {
                    2: { halign: "right" },
                    3: { halign: "right" },
                    4: { halign: "right" },
                },
                theme: "grid",
            })
        }

        finalizarPaginas(doc, {
            avisoDireita: "Documento de gestão — não vale como comprovante fiscal.",
        })
        doc.save("relatorio-faturamento.pdf")
    }

    /**
     * PDF de agendamentos — mesmo layout institucional. Hero com total +
     * % de presença/no-show, tabela por status, tabela por dia.
     */
    async function gerarAgendamentosPdf(
        dados: RelatorioAgendamentos,
        periodo: { dataInicio: string; dataFim: string },
    ) {
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

        let y = desenharCabecalho(doc, est, logo, {
            docTitle: "RELATÓRIO DE AGENDAMENTOS",
            docSubtitle: `Período ${dataLabel(periodo.dataInicio)} a ${dataLabel(periodo.dataFim)}`,
        })

        // KPIs
        const concluidos = dados.porStatus.find(s => /conclu/i.test(s.status))?.quantidade ?? 0
        const noShow = dados.porStatus.find(s => /no.?show|falt/i.test(s.status))?.quantidade ?? 0
        const pctNoShow = dados.total > 0 ? Math.round((noShow / dados.total) * 100) : 0

        y = desenharHero(doc, [
            { label: "TOTAL", valor: String(dados.total) },
            { label: "CONCLUÍDOS", valor: String(concluidos), destaque: "verde" },
            { label: "NO-SHOW", valor: String(noShow), destaque: "vermelho" },
            { label: "% NO-SHOW", valor: `${pctNoShow}%` },
        ], y)

        if (dados.total === 0) {
            desenharEmptyState(doc, y, "Sem dados no período selecionado")
        } else {
            // Tabela por status
            autoTable(doc, {
                startY: y,
                head: [["Status", "Quantidade", "% do Total"]],
                body: dados.porStatus.map(s => [
                    s.status,
                    String(s.quantidade),
                    dados.total > 0 ? `${Math.round((s.quantidade / dados.total) * 100)}%` : "0%",
                ]),
                styles: { font: NUNITO_FAMILY, fontSize: 8.5, cellPadding: 2.5 },
                headStyles: {
                    font: NUNITO_FAMILY,
                    fillColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                    textColor: 255,
                    fontStyle: "bold",
                },
                margin: { left, right: PDF_MARGIN.side },
                theme: "grid",
                columnStyles: { 1: { halign: "right" }, 2: { halign: "right" } },
            })

            const afterStatus = (doc as any).lastAutoTable.finalY + 8

            if (dados.porDia.length > 0) {
                doc.setFont(NUNITO_FAMILY, "bold")
                doc.setFontSize(9)
                doc.setTextColor(PDF_THEME.inkTitle[0], PDF_THEME.inkTitle[1], PDF_THEME.inkTitle[2])
                doc.text("POR DIA", left, afterStatus, { charSpace: 0.4 })

                autoTable(doc, {
                    startY: afterStatus + 3,
                    head: [["Data", "Quantidade"]],
                    body: dados.porDia.map(d => [
                        dataLabel(d.data),
                        String(d.quantidade),
                    ]),
                    styles: { font: NUNITO_FAMILY, fontSize: 8.5, cellPadding: 2.5 },
                    headStyles: {
                        font: NUNITO_FAMILY,
                        fillColor: [PDF_THEME.ink[0], PDF_THEME.ink[1], PDF_THEME.ink[2]],
                        textColor: 255,
                        fontStyle: "bold",
                    },
                    margin: { left, right: PDF_MARGIN.side },
                    theme: "grid",
                    columnStyles: { 1: { halign: "right" } },
                })
            }
        }

        finalizarPaginas(doc, {
            avisoDireita: "Documento de gestão — não vale como comprovante fiscal.",
        })
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
        const bom = "﻿"
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

// ─── Helpers locais (apenas para os relatórios) ──────────────────────────────

type Destaque = "verde" | "vermelho" | undefined

interface HeroCard {
    label: string
    valor: string
    destaque?: Destaque
}

function desenharHero(doc: any, cards: HeroCard[], y: number): number {
    const w = doc.internal.pageSize.getWidth()
    // Margem replicada (helper não está importado neste escopo)
    const left = 18
    const right = w - 18
    const total = cards.length
    const gap = 3
    const cardW = (right - left - gap * (total - 1)) / total
    const cardH = 16

    // Cores hardcoded — manter sincronizadas com PDF_THEME do usePdfHeader.
    const ink = [26, 36, 64] as [number, number, number]
    const cardBg = [248, 250, 252] as [number, number, number]
    const border = [226, 232, 240] as [number, number, number]
    const verde = [22, 163, 74] as [number, number, number]
    const vermelho = [220, 38, 38] as [number, number, number]
    const muteLight = [148, 163, 184] as [number, number, number]

    for (let i = 0; i < cards.length; i++) {
        const x = left + i * (cardW + gap)
        doc.setFillColor(cardBg[0], cardBg[1], cardBg[2])
        doc.setDrawColor(border[0], border[1], border[2])
        doc.setLineWidth(0.2)
        doc.roundedRect(x, y, cardW, cardH, 1.5, 1.5, "FD")

        doc.setFont("Nunito", "semibold")
        doc.setFontSize(6.5)
        doc.setTextColor(muteLight[0], muteLight[1], muteLight[2])
        doc.text(cards[i]!.label, x + 3, y + 4, { charSpace: 0.3 })

        let corValor = ink
        if (cards[i]!.destaque === "verde") corValor = verde
        else if (cards[i]!.destaque === "vermelho") corValor = vermelho

        doc.setFont("Nunito", "bold")
        doc.setFontSize(12)
        doc.setTextColor(corValor[0], corValor[1], corValor[2])
        doc.text(cards[i]!.valor, x + 3, y + 11)
    }

    return y + cardH + 6
}

function desenharEmptyState(doc: any, y: number, mensagem: string) {
    const w = doc.internal.pageSize.getWidth()
    const left = 18
    const right = w - 18
    const altura = 28
    doc.setDrawColor(203, 213, 225) // cbd5e1
    doc.setLineWidth(0.3)
    doc.setLineDashPattern([1, 1], 0)
    doc.roundedRect(left, y, right - left, altura, 2, 2, "D")
    doc.setLineDashPattern([], 0)
    doc.setFont("Nunito", "normal")
    doc.setFontSize(10)
    doc.setTextColor(148, 163, 184) // muteLight
    doc.text(mensagem, left + (right - left) / 2, y + altura / 2 + 1, { align: "center", baseline: "middle" })
}
