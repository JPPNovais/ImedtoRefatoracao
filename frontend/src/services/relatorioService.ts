import httpClient from "./httpClient"

// ─── DTOs ────────────────────────────────────────────────────────────────────

export type AgruparPorFinanceiro = "dia" | "semana" | "mes" | "categoria" | "forma_pagamento"
export type TipoRelatorioOperacional = "agenda" | "dashboard" | "inventario"
export type TipoRelatorioPessoas = "pacientes" | "profissionais_performance"

export interface LinhaRelatorio {
    rotulo: string
    valor: number
    quantidade?: number
}

export interface RelatorioFinanceiro {
    totalReceitas: number
    totalDespesas: number
    saldo: number
    breakdown: LinhaRelatorio[]
    agrupadoPor: AgruparPorFinanceiro
}

export interface KpiOperacional {
    rotulo: string
    valor: number
    unidade?: string
}

export interface RelatorioOperacional {
    tipo: TipoRelatorioOperacional
    kpis: KpiOperacional[]
    breakdown: LinhaRelatorio[]
}

export interface LinhaRankingPaciente {
    nome: string
    totalConsultas: number
    totalGasto: number
}

export interface LinhaRankingProfissional {
    nome: string
    totalAtendimentos: number
    faturamento: number
}

export interface RelatorioPessoas {
    tipo: TipoRelatorioPessoas
    topPacientes?: LinhaRankingPaciente[]
    rankingProfissionais?: LinhaRankingProfissional[]
}

export interface FunilOrcamento {
    totalCriados: number
    totalEnviados: number
    totalAprovados: number
    totalRecusados: number
    valorMedioAprovado: number
    taxaConversao: number
}

export interface RelatorioOrcamentos {
    funil: FunilOrcamento
    breakdown: LinhaRelatorio[]
}

// ─── Parâmetros de filtro ─────────────────────────────────────────────────────

export interface FiltroBase {
    dataInicio?: string
    dataFim?: string
}

export interface FiltroFinanceiro extends FiltroBase {
    agruparPor?: AgruparPorFinanceiro
}

export interface FiltroOperacional extends FiltroBase {
    tipo?: TipoRelatorioOperacional
}

export interface FiltroPessoas extends FiltroBase {
    tipo?: TipoRelatorioPessoas
}

// ─── Service ─────────────────────────────────────────────────────────────────

export const relatorioService = {
    async financeiro(filtro: FiltroFinanceiro = {}): Promise<RelatorioFinanceiro> {
        const { data } = await httpClient.get<RelatorioFinanceiro>("/relatorios/financeiro", {
            params: filtro,
        })
        return data
    },

    async operacional(filtro: FiltroOperacional = {}): Promise<RelatorioOperacional> {
        const { data } = await httpClient.get<RelatorioOperacional>("/relatorios/operacional", {
            params: filtro,
        })
        return data
    },

    async pessoas(filtro: FiltroPessoas = {}): Promise<RelatorioPessoas> {
        const { data } = await httpClient.get<RelatorioPessoas>("/relatorios/pessoas", {
            params: filtro,
        })
        return data
    },

    async orcamentos(filtro: FiltroBase = {}): Promise<RelatorioOrcamentos> {
        const { data } = await httpClient.get<RelatorioOrcamentos>("/relatorios/orcamentos", {
            params: filtro,
        })
        return data
    },
}
