/**
 * useRelatorioCsv — exportadores CSV por aba de Relatórios.
 *
 * Cada função recebe os dados já carregados na view e gera/baixa o CSV
 * espelhando exatamente as colunas exibidas na tela (minimização LGPD).
 *
 * Formato: UTF-8 BOM + separador ";" + datas dd/MM/yyyy + decimal vírgula.
 * Células monetárias sem símbolo "R$" (cabeçalho indica a moeda).
 */
import {
    construirCsv,
    formatarDecimal,
    formatarInteiro,
    formatarData,
    baixarCsv,
    nomeArquivoCsv,
} from "@/utils/csv"
import type {
    RelatorioFinanceiro,
    RelatorioOperacional,
    RelatorioPessoas,
    RelatorioOrcamentos,
    CustoLucroPaciente,
} from "@/services/relatorioService"

export function useRelatorioCsv() {
    /** Período formatado para o cabeçalho do arquivo CSV. */
    function cabecalhoPeriodo(dataInicio: string, dataFim: string): string[][] {
        return [
            [`Período: ${formatarData(dataInicio)} a ${formatarData(dataFim)}`],
            [],
        ]
    }

    /**
     * Exporta a aba Financeiro.
     * Colunas: Item; Valor (R$); Qtd.; % do total
     * + linha de KPIs de topo (Receitas, Despesas, Saldo) e linha Total.
     */
    function exportarFinanceiro(
        dados: RelatorioFinanceiro,
        periodo: { dataInicio: string; dataFim: string },
    ): void {
        const totalBreakdown = dados.breakdown.reduce((s, l) => s + l.valor, 0)

        const linhas: string[][] = [
            ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
            ["Receitas (R$)", "Despesas (R$)", "Saldo (R$)"],
            [
                formatarDecimal(dados.totalReceitas),
                formatarDecimal(dados.totalDespesas),
                formatarDecimal(dados.saldo),
            ],
            [],
            ["Item", "Valor (R$)", "Qtd.", "% do total"],
            ...dados.breakdown.map(l => [
                l.rotulo,
                formatarDecimal(l.valor),
                l.quantidade != null ? formatarInteiro(l.quantidade) : "",
                totalBreakdown > 0
                    ? `${Math.round((l.valor / totalBreakdown) * 100)}%`
                    : "0%",
            ]),
            ["Total", formatarDecimal(totalBreakdown), "", ""],
        ]

        baixarCsv(
            construirCsv(linhas),
            nomeArquivoCsv("financeiro", periodo.dataInicio, periodo.dataFim),
        )
    }

    /**
     * Exporta a aba Agenda.
     * KPIs (Rótulo; Valor; Unidade) + breakdown (Item; Valor; Qtd.).
     */
    function exportarAgenda(
        dados: RelatorioOperacional,
        periodo: { dataInicio: string; dataFim: string },
    ): void {
        const linhas: string[][] = [
            ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
            ["Indicador", "Valor", "Unidade"],
            ...dados.kpis.map(k => [
                k.rotulo,
                formatarInteiro(k.valor),
                k.unidade ?? "",
            ]),
            [],
            ["Item", "Valor", "Qtd."],
            ...dados.breakdown.map(l => [
                l.rotulo,
                formatarInteiro(l.valor),
                l.quantidade != null ? formatarInteiro(l.quantidade) : "",
            ]),
        ]

        baixarCsv(
            construirCsv(linhas),
            nomeArquivoCsv("agenda", periodo.dataInicio, periodo.dataFim),
        )
    }

    /**
     * Exporta a aba Pessoas.
     * Profissionais: #; Profissional; Atendimentos; Faturamento (R$)
     * Pacientes:     #; Paciente; Consultas; Total gasto (R$)
     * LGPD: sem CPF, telefone, e-mail ou qualquer PII além do nome exibido na tela.
     */
    function exportarPessoas(
        dados: RelatorioPessoas,
        periodo: { dataInicio: string; dataFim: string },
    ): void {
        let linhas: string[][]

        if (dados.tipo === "profissionais" && dados.rankingProfissionais?.length) {
            linhas = [
                ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
                ["#", "Profissional", "Atendimentos", "Faturamento (R$)"],
                ...dados.rankingProfissionais.map((p, i) => [
                    formatarInteiro(i + 1),
                    p.nome,
                    formatarInteiro(p.totalAtendimentos),
                    formatarDecimal(p.faturamento),
                ]),
            ]
        } else if (dados.tipo === "pacientes" && dados.topPacientes?.length) {
            linhas = [
                ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
                ["#", "Paciente", "Consultas", "Total gasto (R$)"],
                ...dados.topPacientes.map((p, i) => [
                    formatarInteiro(i + 1),
                    p.nome,
                    formatarInteiro(p.totalConsultas),
                    formatarDecimal(p.totalGasto),
                ]),
            ]
        } else {
            // Sem dados — não deve ser chamada com dados vazios, mas defende
            return
        }

        baixarCsv(
            construirCsv(linhas),
            nomeArquivoCsv("pessoas", periodo.dataInicio, periodo.dataFim),
        )
    }

    /**
     * Exporta a aba Orçamentos.
     * Funil (Etapa; Quantidade; % do criado) + KPIs + breakdown se houver.
     */
    function exportarOrcamentos(
        dados: RelatorioOrcamentos,
        periodo: { dataInicio: string; dataFim: string },
    ): void {
        const f = dados.funil
        const etapasFunil = [
            { label: "Criados", valor: f.totalCriados },
            { label: "Enviados", valor: f.totalEnviados },
            { label: "Aprovados", valor: f.totalAprovados },
            { label: "Recusados", valor: f.totalRecusados },
        ]

        const linhas: string[][] = [
            ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
            ["Etapa", "Quantidade", "% do criado"],
            ...etapasFunil.map(e => [
                e.label,
                formatarInteiro(e.valor),
                f.totalCriados > 0
                    ? `${Math.round((e.valor / f.totalCriados) * 100)}%`
                    : "0%",
            ]),
            [],
            ["Taxa de conversão", `${(f.taxaConversao * 100).toFixed(1).replace(".", ",")}%`, ""],
            ["Valor médio aprovado (R$)", formatarDecimal(f.valorMedioAprovado), ""],
        ]

        if (dados.breakdown.length > 0) {
            linhas.push(
                [],
                ["Item", "Valor", "Qtd."],
                ...dados.breakdown.map(l => [
                    l.rotulo,
                    formatarInteiro(l.valor),
                    l.quantidade != null ? formatarInteiro(l.quantidade) : "",
                ]),
            )
        }

        baixarCsv(
            construirCsv(linhas),
            nomeArquivoCsv("orcamentos", periodo.dataInicio, periodo.dataFim),
        )
    }

    /**
     * Exporta a aba Visão geral.
     * Resumo de KPIs consolidados: Indicador; Valor (tabela flat).
     */
    function exportarVisaoGeral(
        dados: {
            financeiro: RelatorioFinanceiro | null
            operacional: RelatorioOperacional | null
            orcamentos: RelatorioOrcamentos | null
        },
        periodo: { dataInicio: string; dataFim: string },
    ): void {
        const linhas: string[][] = [
            ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
            ["Indicador", "Valor"],
        ]

        if (dados.financeiro) {
            linhas.push(
                ["Receitas (R$)", formatarDecimal(dados.financeiro.totalReceitas)],
                ["Despesas (R$)", formatarDecimal(dados.financeiro.totalDespesas)],
                ["Saldo (R$)", formatarDecimal(dados.financeiro.saldo)],
            )
        }

        if (dados.operacional) {
            for (const kpi of dados.operacional.kpis) {
                linhas.push([kpi.rotulo, formatarInteiro(kpi.valor)])
            }
        }

        if (dados.orcamentos) {
            const f = dados.orcamentos.funil
            linhas.push(
                ["Orçamentos criados", formatarInteiro(f.totalCriados)],
                ["Orçamentos aprovados", formatarInteiro(f.totalAprovados)],
                ["Taxa de conversão", `${(f.taxaConversao * 100).toFixed(1).replace(".", ",")}%`],
                ["Valor médio aprovado (R$)", formatarDecimal(f.valorMedioAprovado)],
            )
        }

        baixarCsv(
            construirCsv(linhas),
            nomeArquivoCsv("visao-geral", periodo.dataInicio, periodo.dataFim),
        )
    }

    /**
     * F7/CA182 — Exporta a seção "Por paciente" do relatório financeiro.
     * Colunas: Paciente; Cobrado (R$); Pago (R$); Desconto (R$); Taxa (R$); Custo (R$); Lucro (R$).
     * LGPD: apenas nome + valores financeiros — sem PII adicional.
     */
    function exportarPorPaciente(
        linhasTab: CustoLucroPaciente[],
        periodo: { dataInicio: string; dataFim: string },
    ): void {
        const linhas: string[][] = [
            ...cabecalhoPeriodo(periodo.dataInicio, periodo.dataFim),
            ["Paciente", "Cobrado (R$)", "Pago (R$)", "Desconto (R$)", "Taxa (R$)", "Custo (R$)", "Lucro (R$)"],
            ...linhasTab.map(p => [
                p.pacienteNome,
                formatarDecimal(p.cobrado),
                formatarDecimal(p.pago),
                formatarDecimal(p.desconto),
                formatarDecimal(p.taxa),
                formatarDecimal(p.custo),
                formatarDecimal(p.lucro),
            ]),
        ]

        baixarCsv(
            construirCsv(linhas),
            nomeArquivoCsv("custo-lucro-por-paciente", periodo.dataInicio, periodo.dataFim),
        )
    }

    return {
        exportarFinanceiro,
        exportarAgenda,
        exportarPessoas,
        exportarOrcamentos,
        exportarVisaoGeral,
        exportarPorPaciente,
    }
}
