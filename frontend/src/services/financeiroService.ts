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

// ─── F7: KPIs, Extrato, Caixa Diário, Comissões ────────────────────────────

export interface KpisFinanceiro {
    recebido: number
    aReceber: number
    despesas: number
    saldo: number
    estornos: number
    descontosConcedidos: number
    taxasCartao: number
}

export interface LancamentoExtrato {
    id: number
    tipo: string
    descricao: string
    valor: number
    dataPagamento: string | null
    dataVencimento: string | null
    status: string
    categoria: string
    formaPagamento: string | null
    origem: string | null
    cobrancaId: number | null
    pacienteId: number | null
    pacienteNome: string | null
    criadoPorNome: string
}

export interface PaginaExtrato {
    itens: LancamentoExtrato[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface ResumoCaixaFormaPagamento {
    formaPagamento: string
    total: number
}

export interface CaixaDiario {
    id: number
    data: string
    status: "Aberto" | "Fechado"
    abertoPorUsuarioId: string
    abertoPorNome: string
    abertoEm: string
    fechadoPorUsuarioId: string | null
    fechadoPorNome: string | null
    fechadoEm: string | null
    observacao: string | null
    reabertoPorUsuarioId: string | null
    reabertoEm: string | null
    resumoPorForma: ResumoCaixaFormaPagamento[]
    totalEstornos: number
    totalDia: number
}

export interface ComissaoAtendimento {
    data: string
    tipoAtendimento: string
    pacienteId: number | null
    pacienteNome: string | null
    base: number
    faturamento: number
    comissao: number
    tipoBase: string
}

export interface ComissaoProfissional {
    profissionalUsuarioId: string
    nome: string
    especialidade: string | null
    atendimentos: number
    faturamento: number
    percentualConfig: number | null
    comissao: number
    atendimentos_Detalhes: ComissaoAtendimento[]
}

export interface ComissaoPeriodo {
    totalARepassar: number
    profissionais: ComissaoProfissional[]
}

export interface ConfigComissao {
    percentualConsulta: number | null
    percentualProcedimento: number | null
    percentualPadrao: number
}

export interface ConfigComissaoProfissionalLista {
    profissionalUsuarioId: string
    nome: string
    percentualConsulta: number | null
    percentualProcedimento: number | null
    percentualPadrao: number
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

    // ─── F7: KPIs ─────────────────────────────────────────────────────────────

    async kpis(params: { dataInicio: string; dataFim: string }): Promise<KpisFinanceiro> {
        const { data } = await httpClient.get<KpisFinanceiro>("/financeiro/kpis", { params })
        return data
    },

    // ─── F7: Extrato paginado ─────────────────────────────────────────────────

    async extrato(params: {
        dataInicio: string
        dataFim: string
        tipo?: string
        categoria?: string
        formaPagamento?: string
        origem?: string
        pagina?: number
        tamanho?: number
        // Modo vencidos (R4): ignora dataInicio/dataFim, lista Pendente + vencimento < hoje.
        somenteVencidos?: boolean
    }): Promise<PaginaExtrato> {
        const { data } = await httpClient.get<PaginaExtrato>("/financeiro/extrato", { params })
        return data
    },

    // ─── F7: Caixa Diário ─────────────────────────────────────────────────────

    async obterCaixa(data?: string): Promise<CaixaDiario | null> {
        const { data: res } = await httpClient.get<CaixaDiario | null>("/financeiro/caixa", {
            params: data ? { data } : undefined
        })
        return res
    },

    async abrirCaixa(payload: { data?: string }): Promise<void> {
        await httpClient.post("/financeiro/caixa/abrir", payload)
    },

    async fecharCaixa(payload: { data?: string; observacao?: string }): Promise<void> {
        await httpClient.post("/financeiro/caixa/fechar", payload)
    },

    async reabrirCaixa(payload: { data?: string }): Promise<void> {
        await httpClient.post("/financeiro/caixa/reabrir", payload)
    },

    // ─── F7: Comissões ────────────────────────────────────────────────────────

    async comissoes(params: { dataInicio: string; dataFim: string }): Promise<ComissaoPeriodo> {
        const { data } = await httpClient.get<ComissaoPeriodo>("/financeiro/comissoes", { params })
        return data
    },

    async obterConfigComissao(profissionalUsuarioId: string): Promise<ConfigComissao> {
        const { data } = await httpClient.get<ConfigComissao>(
            `/financeiro/comissoes/config/${profissionalUsuarioId}`
        )
        return data
    },

    async salvarConfigComissao(payload: {
        profissionalUsuarioId: string
        percentualConsulta?: number | null
        percentualProcedimento?: number | null
    }): Promise<void> {
        await httpClient.post("/financeiro/comissoes/config", payload)
    },

    // ─── F7 redesign — Export de extrato (briefing 2026-06-11_002) ───────────────

    async exportarExtrato(params: {
        dataInicio: string
        dataFim: string
        tipo?: string
        categoria?: string
        formaPagamento?: string
        origem?: string
    }): Promise<Blob> {
        const { data } = await httpClient.get<Blob>("/financeiro/extrato/export", {
            params,
            responseType: "blob"
        })
        return data
    },
}
