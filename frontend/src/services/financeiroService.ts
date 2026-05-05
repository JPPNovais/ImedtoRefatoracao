import httpClient from "./httpClient"

export interface Lancamento {
    id: number
    // estabelecimentoId removido: backend nao envia mais (LGPD - minimizacao).
    tipo: "Receita" | "Despesa"
    descricao: string
    valor: number
    dataVencimento: string
    dataPagamento: string | null
    status: "Pendente" | "Pago" | "Cancelado"
    categoria: string
    orcamentoId: number | null
    orcamentoNumero: string | null
    criadoPorNome: string
    criadoEm: string
}

export interface ResumoFinanceiro {
    totalReceitasPagas: number
    totalDespesasPagas: number
    saldo: number
    receitasPendentes: number
    despesasPendentes: number
}

export interface PaginaLancamentos {
    itens: Lancamento[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export const financeiroService = {
    async listar(params?: {
        tipo?: string
        status?: string
        categoria?: string
        dataInicio?: string
        dataFim?: string
        pagina?: number
        tamanho?: number
    }): Promise<PaginaLancamentos> {
        const { data } = await httpClient.get<PaginaLancamentos>("/financeiro/lancamentos", { params })
        return data
    },

    async resumo(params?: { dataInicio?: string; dataFim?: string }): Promise<ResumoFinanceiro> {
        const { data } = await httpClient.get<ResumoFinanceiro>("/financeiro/resumo", { params })
        return data
    },

    async criar(payload: {
        tipo: string
        descricao: string
        valor: number
        dataVencimento: string
        categoria: string
        orcamentoId?: number | null
    }): Promise<void> {
        await httpClient.post("/financeiro/lancamentos", payload)
    },

    async atualizar(id: number, payload: {
        descricao: string
        valor: number
        dataVencimento: string
        categoria: string
    }): Promise<void> {
        await httpClient.put(`/financeiro/lancamentos/${id}`, payload)
    },

    async pagar(id: number, dataPagamento?: string): Promise<void> {
        await httpClient.post(`/financeiro/lancamentos/${id}/pagar`, { dataPagamento: dataPagamento ?? null })
    },

    async cancelar(id: number): Promise<void> {
        await httpClient.post(`/financeiro/lancamentos/${id}/cancelar`)
    },
}
