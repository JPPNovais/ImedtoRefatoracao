import httpClient from "./httpClient"

// ─── DTOs ────────────────────────────────────────────────────────────────────

export type AgruparPorFinanceiro = "dia" | "semana" | "mes" | "categoria" | "forma_pagamento"
export type TipoRelatorioOperacional = "agenda" | "dashboard" | "inventario"
export type TipoRelatorioPessoas = "pacientes" | "profissionais"

export interface LinhaRelatorio {
    rotulo: string
    valor: number
    quantidade?: number
}

// F7/CA178 — custo/lucro por paciente no relatório financeiro.
export interface CustoLucroPaciente {
    pacienteId: number
    pacienteNome: string
    cobrado: number
    pago: number
    desconto: number
    taxa: number
    custo: number
    lucro: number
}

export interface RelatorioFinanceiro {
    totalReceitas: number
    totalDespesas: number
    saldo: number
    breakdown: LinhaRelatorio[]
    agrupadoPor: AgruparPorFinanceiro
    /** F7/CA178 — presente apenas quando incluirPorPaciente=true */
    porPaciente?: CustoLucroPaciente[]
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
    /** F7/CA178 — solicita seção porPaciente no retorno */
    incluirPorPaciente?: boolean
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
        // Backend devolve { tipo, pacientes?: { ... topAtivos[] }, profissionais?: { desempenho[] } }.
        // O front consome topPacientes / rankingProfissionais — faz o mapping aqui.
        const { data } = await httpClient.get<RelatorioPessoasBackend>("/relatorios/pessoas", {
            params: filtro,
        })
        const tipo = (data?.tipo ?? filtro.tipo ?? "pacientes") as TipoRelatorioPessoas
        return {
            tipo,
            topPacientes: data?.pacientes?.topAtivos?.map((p) => ({
                nome: p.nome,
                totalConsultas: p.atendimentos,
                totalGasto: 0,
            })),
            rankingProfissionais: data?.profissionais?.desempenho?.map((p) => ({
                nome: p.nome,
                totalAtendimentos: p.atendimentos,
                faturamento: p.faturamento,
            })),
        }
    },

    async orcamentos(filtro: FiltroBase = {}): Promise<RelatorioOrcamentos> {
        // Backend devolve campos achatados (totalEmitidos, totalAprovados, valorMedio…);
        // o front agrupa em `funil`. Faz o mapping aqui para isolar a divergência.
        const { data } = await httpClient.get<RelatorioOrcamentosBackend>("/relatorios/orcamentos", {
            params: filtro,
        })
        return {
            funil: {
                totalCriados: data?.totalEmitidos ?? 0,
                totalEnviados: data?.totalEmitidos ?? 0,
                totalAprovados: data?.totalAprovados ?? 0,
                totalRecusados: data?.totalRecusados ?? 0,
                valorMedioAprovado: data?.valorMedio ?? 0,
                taxaConversao: data?.taxaConversao ?? 0,
            },
            breakdown: data?.breakdown ?? [],
        }
    },
}

interface RelatorioOrcamentosBackend {
    totalEmitidos: number
    totalAprovados: number
    totalRecusados: number
    valorMedio: number
    taxaConversao: number
    breakdown: LinhaRelatorio[]
}

interface RelatorioPessoasBackend {
    tipo: string
    pacientes?: {
        novos: number
        retornos: number
        topAtivos?: Array<{ nome: string, atendimentos: number }>
    }
    profissionais?: {
        desempenho?: Array<{ nome: string, atendimentos: number, faturamento: number }>
    }
}
