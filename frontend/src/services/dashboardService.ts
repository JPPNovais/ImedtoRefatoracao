import httpClient from "./httpClient"

export interface ProximoAgendamento {
    id: number
    pacienteNome: string
    profissionalNome: string
    inicioPrevisto: string
    tipoServico: string
    status: string
}

export interface ItemAbaixoMinimo {
    id: number
    nome: string
    quantidadeAtual: number
    quantidadeMinima: number
    unidadeMedida: string
}

export interface DashboardData {
    totalPacientesAtivos: number
    agendamentosHoje: number
    agendamentosSemana: number
    receitasMes: number
    despesasMes: number
    saldoMes: number
    itensAbaixoMinimo: number
    orcamentosPendentes: number
    lancamentosVencidos: number
    // Valores somados dos vencidos por tipo (R2/CA12): mesma regra do LancamentosVencidos.
    vencidosAReceber: number
    vencidosAPagar: number
    proximosAgendamentos: ProximoAgendamento[]
    itensAbaixoMinimoLista: ItemAbaixoMinimo[]
}

export interface FaturamentoCategoria {
    categoria: string
    tipo: string
    totalPago: number
    totalPendente: number
    quantidade: number
}

export interface RelatorioAgendamentos {
    total: number
    porStatus: { status: string; quantidade: number }[]
    porDia: { data: string; quantidade: number }[]
}

export const dashboardService = {
    async obter(): Promise<DashboardData> {
        const { data } = await httpClient.get<DashboardData>("/dashboard")
        return data
    },
}

export const relatorioService = {
    async faturamento(params?: { dataInicio?: string; dataFim?: string }): Promise<FaturamentoCategoria[]> {
        const { data } = await httpClient.get<FaturamentoCategoria[]>("/relatorios/faturamento", { params })
        return data
    },

    async agendamentos(params?: { dataInicio?: string; dataFim?: string }): Promise<RelatorioAgendamentos> {
        const { data } = await httpClient.get<RelatorioAgendamentos>("/relatorios/agendamentos", { params })
        return data
    },
}
