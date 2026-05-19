import type { Orcamento } from "@/services/orcamentoService"
import { useTenantStore } from "@/stores/tenantStore"

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function dataFmt(s: string) {
    return new Date(s).toLocaleDateString("pt-BR")
}

const STATUS_COR: Record<string, [number, number, number]> = {
    Rascunho:  [107, 114, 128],
    Enviado:   [37, 99, 235],
    Aprovado:  [6, 95, 70],
    Recusado:  [153, 27, 27],
    Cancelado: [107, 114, 128],
    Expirado:  [180, 83, 9],
}

/**
 * Gera PDF do orçamento com todas as seções do aggregate completo: cirurgias,
 * equipe, implantes, internação/anestesia, formas de pagamento detalhadas e
 * resumo final. Usa jsPDF + jspdf-autotable.
 *
 * Cabeçalho institucional via `usePdfHeader` (logo do estabelecimento + nome +
 * endereço/CNPJ) — mesmo padrão de Prontuário/Relatórios. Corpo da tabela
 * mantém o design legado (helvetica) para evitar regressão visual no PDF de
 * orçamento.
 */
export function useOrcamentoPdf() {
    async function gerarPdf(orc: Orcamento) {
        // Lazy: jsPDF + autotable + helper (Nunito) só carregam quando o usuário clica em "Baixar PDF".
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
        } = helper

        const tenant = useTenantStore()
        const est = await carregarEstabelecimentoAtivo(tenant.estabelecimentoAtivoId)
        const logo = await carregarLogoComoDataUrl(est)

        const doc = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" })
        registrarFontesNunito(doc)
        const pageW = doc.internal.pageSize.getWidth()
        const margem = 14

        // ─── Cabeçalho institucional (logo + nome + endereço/CNPJ) ───────────
        // Fallback gracioso: sem foto, desenharCabecalho renderiza placeholder
        // com iniciais do estabelecimento (mesma regra de Receita/Prontuário).
        let y = desenharCabecalho(doc, est, logo, {
            docTitle: `ORÇAMENTO ${orc.numero || `#${orc.id}`}`,
            docSubtitle: `Emitido em ${dataFmt(orc.criadoEm)}`,
        })

        // ─── Status (badge à direita) — mantido logo abaixo do cabeçalho ─────
        const [r, g, b] = STATUS_COR[orc.status] ?? [50, 50, 50]
        doc.setFont("helvetica", "bold")
        doc.setFontSize(11)
        doc.setTextColor(r, g, b)
        doc.text(orc.status, pageW - margem, y + 1, { align: "right" })
        doc.setTextColor(30)
        y += 6

        // ─── Cabeçalho de dados (paciente / validade / emitido por) ──────────
        doc.setFontSize(10)
        doc.setFont("helvetica", "normal")
        doc.text(`Paciente: ${orc.pacienteNome}`, margem, y)
        doc.text(`Validade: ${dataFmt(orc.validade)}`, pageW - margem - 55, y)
        y += 7
        doc.text(`Emitido por: ${orc.criadoPorNome}`, margem, y)

        if (orc.observacoes) {
            y += 10
            doc.setFont("helvetica", "bold")
            doc.text("Observações:", margem, y)
            y += 5
            doc.setFont("helvetica", "normal")
            const linhas = doc.splitTextToSize(orc.observacoes, pageW - 2 * margem)
            doc.text(linhas, margem, y)
            y += linhas.length * 5
        }

        y += 4

        const headStyle = { fillColor: [37, 99, 235] as [number, number, number], textColor: 255 }
        const tableBase = {
            styles: { fontSize: 9 },
            headStyles: headStyle,
            margin: { left: margem, right: margem },
        }

        // ─── Cirurgias ────────────────────────────────────────────────────
        if (orc.cirurgias.length > 0) {
            autoTable(doc, {
                ...tableBase,
                startY: y,
                head: [["Cirurgia", "Qtd", "Duração", "Valor"]],
                body: orc.cirurgias.map(c => [
                    c.descricao || "—",
                    String(c.quantidade),
                    c.duracaoMinutos ? `${c.duracaoMinutos} min` : "—",
                    moeda(c.valorTotal),
                ]),
                columnStyles: { 1: { halign: "right" }, 3: { halign: "right" } },
            })
            y = (doc as any).lastAutoTable.finalY + 4
        }

        // ─── Equipe ───────────────────────────────────────────────────────
        if (orc.equipe.length > 0) {
            autoTable(doc, {
                ...tableBase,
                startY: y,
                head: [["Profissional", "Função", "Honorário"]],
                body: orc.equipe.map(e => [
                    e.profissionalNome ?? e.profissionalUsuarioId.slice(0, 8),
                    e.papel,
                    moeda(e.valor),
                ]),
                columnStyles: { 2: { halign: "right" } },
            })
            y = (doc as any).lastAutoTable.finalY + 4
        }

        // ─── Implantes ────────────────────────────────────────────────────
        if (orc.implantes.length > 0) {
            autoTable(doc, {
                ...tableBase,
                startY: y,
                head: [["Implante", "Qtd", "Custo unit.", "Total"]],
                body: orc.implantes.map(i => [
                    i.descricao,
                    String(i.quantidade),
                    moeda(i.custoUnitario),
                    moeda(i.custoTotal),
                ]),
                columnStyles: { 1: { halign: "right" }, 2: { halign: "right" }, 3: { halign: "right" } },
            })
            y = (doc as any).lastAutoTable.finalY + 4
        }

        // ─── Local cirúrgico + Anestesia ───────────────────────────────────
        if (orc.localCirurgia || orc.anestesia) {
            const linhas: string[][] = []
            if (orc.localCirurgia) {
                linhas.push([
                    `Local cirúrgico ${orc.localCirurgia.tipo}`,
                    `${orc.localCirurgia.tempoMinutos} min`,
                    moeda(orc.localCirurgia.valor),
                ])
            }
            if (orc.anestesia) {
                linhas.push([
                    `Anestesia ${orc.anestesia.tipoAnestesia}`,
                    orc.anestesia.observacao ?? "",
                    moeda(orc.anestesia.valor),
                ])
            }
            autoTable(doc, {
                ...tableBase,
                startY: y,
                head: [["Item", "Detalhe", "Valor"]],
                body: linhas,
                columnStyles: { 2: { halign: "right" } },
            })
            y = (doc as any).lastAutoTable.finalY + 4
        }

        // ─── Itens avulsos ────────────────────────────────────────────────
        if (orc.itens.length > 0) {
            autoTable(doc, {
                ...tableBase,
                startY: y,
                head: [["Item", "Qtd", "Valor unit.", "Desc.", "Subtotal"]],
                body: orc.itens.map(i => [
                    i.descricao,
                    i.quantidade.toLocaleString("pt-BR"),
                    moeda(i.valorUnitario),
                    `${i.descontoPercent}%`,
                    moeda(i.subtotal),
                ]),
                columnStyles: {
                    1: { halign: "right" }, 2: { halign: "right" },
                    3: { halign: "right" }, 4: { halign: "right" },
                },
            })
            y = (doc as any).lastAutoTable.finalY + 4
        }

        // ─── Formas de pagamento ──────────────────────────────────────────
        if (orc.formasPagamento.length > 0) {
            autoTable(doc, {
                ...tableBase,
                startY: y,
                head: [["Forma", "Valor", "Parcelas", "Acréscimo", "Entrada"]],
                body: orc.formasPagamento.map(f => [
                    f.formaPagamentoNome ?? `Forma ${f.formaPagamentoId}`,
                    moeda(f.valor),
                    `${f.parcelas}x`,
                    `${f.acrescimoPercentual}%`,
                    `${f.entradaPercentual}%`,
                ]),
                columnStyles: {
                    1: { halign: "right" }, 2: { halign: "right" },
                    3: { halign: "right" }, 4: { halign: "right" },
                },
            })
            y = (doc as any).lastAutoTable.finalY + 4
        }

        // ─── Total final ──────────────────────────────────────────────────
        const totalY = y + 6
        doc.setDrawColor(30)
        doc.setLineWidth(0.5)
        doc.line(margem, totalY - 4, pageW - margem, totalY - 4)
        doc.setFontSize(13)
        doc.setFont("helvetica", "bold")
        doc.setTextColor(30)
        const totalTxt = `Total: ${moeda(orc.total)}`
        doc.text(totalTxt, pageW - margem - doc.getTextWidth(totalTxt), totalY + 2)
        doc.setLineWidth(0.2)

        // ─── Rodapé ────────────────────────────────────────────────────────
        const totalPaginas = doc.getNumberOfPages()
        for (let p = 1; p <= totalPaginas; p++) {
            doc.setPage(p)
            const pH = doc.internal.pageSize.getHeight()
            doc.setFontSize(8)
            doc.setTextColor(150)
            doc.setFont("helvetica", "normal")
            doc.text("Imedto — Sistema de Gestão em Saúde  |  contato@imedto.com", margem, pH - 8)
            const pagTxt = `Página ${p} de ${totalPaginas}`
            doc.text(pagTxt, pageW - margem - doc.getTextWidth(pagTxt), pH - 8)
        }

        const nomeArq = `orcamento-${orc.numero || orc.id}.pdf`.toLowerCase().replace(/[^a-z0-9.-]+/g, "-")
        doc.save(nomeArq)
    }

    return { gerarPdf }
}
