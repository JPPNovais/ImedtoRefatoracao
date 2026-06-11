import httpClient from "./httpClient"

// ── DTOs ──────────────────────────────────────────────────────────────────────

export type TipoAtendimento = "Particular" | "Convenio"
export type StatusCobranca = "Aberta" | "ParcialmentePaga" | "Paga" | "Cancelada"

export interface PagamentoResumo {
    id: number
    formaPagamentoId: number
    formaPagamentoNome: string
    valor: number
    parcelas: number
    juros: number
    taxa: number
    dataPagamento: string
    registradoPorNome: string
}

export interface CobrancaDetalhe {
    id: number
    pacienteId: number
    agendamentoId: number | null
    tipoAtendimento: TipoAtendimento
    valorCobrado: number
    desconto: number
    status: StatusCobranca
    descricao: string | null
    pagamentos: PagamentoResumo[]
    totalLiquido: number
    totalPago: number
    saldoDevedor: number
}

export interface ValorSugerido {
    valorSugerido: number | null
    origem: "Profissional" | "Estabelecimento" | "NaoConfigurado"
}

export interface FormaPagamentoItemRequest {
    formaPagamentoId: number
    valor: number
    parcelas?: number
    juros?: number
}

export interface RegistrarPagamentosRequest {
    desconto: number
    dataPagamento: string
    formas: FormaPagamentoItemRequest[]
}

export interface TabelaPrecoConsulta {
    id: number
    profissionalId: string | null
    profissionalNome: string | null
    valorSugerido: number
    ativo: boolean
}

export interface ConfigTaxaFormaPagamento {
    id: number | null
    formaPagamentoId: number
    formaPagamentoNome: string
    taxaPercentual: number
    ativo: boolean
}

// ── DTOs da aba Financeiro do paciente (F2) ───────────────────────────────────

export interface EstornoAba {
    id: number
    valor: number
    motivo: string
    estornadoPorNome: string
    dataEstorno: string
}

export interface PagamentoAba {
    id: number
    valor: number
    formaPagamentoNome: string
    parcelas: number
    taxa: number
    dataPagamento: string
    estornado: boolean
    estorno: EstornoAba | null
}

export interface HistoricoValorAba {
    valorAnterior: number
    valorNovo: number
    alteradoPorNome: string
    alteradoEm: string
}

export interface CobrancaAba {
    id: number
    origem: string
    tipoAtendimento: TipoAtendimento
    convenioId: number | null
    convenioNome: string | null
    valorCobrado: number
    desconto: number
    totalLiquido: number
    totalPagoLiquido: number
    saldo: number
    status: StatusCobranca
    descricao: string | null
    /** F6/R10: guia de autorização. Null = guia pendente. */
    guiaNumero: string | null
    guiaSenha: string | null
    guiaAutorizadaEm: string | null
    pagamentos: PagamentoAba[]
    historicoValor: HistoricoValorAba[]
}

export interface FinanceiroAba {
    totalCobrado: number
    totalPagoLiquido: number
    saldo: number
    cobrancas: CobrancaAba[]
}

// ── Service ───────────────────────────────────────────────────────────────────

export const cobrancaService = {
    /** CA4: carrega cobrança + histórico para o PaymentModal. */
    async obterPorAgendamento(agendamentoId: number): Promise<CobrancaDetalhe | null> {
        try {
            const { data } = await httpClient.get<CobrancaDetalhe>(
                `/cobrancas/por-agendamento/${agendamentoId}`,
            )
            return data
        } catch (err: any) {
            if (err?.response?.status === 404) return null
            throw err
        }
    },

    /** CA5/CA7/CA8: registra pagamento(s). */
    async registrarPagamentos(cobrancaId: number, req: RegistrarPagamentosRequest): Promise<void> {
        await httpClient.post(`/cobrancas/${cobrancaId}/pagamentos`, req)
    },

    /** CA2: valor sugerido para preencher o campo no check-in. */
    async obterValorSugerido(profissionalUsuarioId: string): Promise<ValorSugerido> {
        const { data } = await httpClient.get<ValorSugerido>("/cobrancas/valor-sugerido", {
            params: { profissionalUsuarioId },
        })
        return data
    },

    // ── Config (Configurações do estabelecimento) ───────────────────────────

    async listarTabelaPreco(busca?: string): Promise<TabelaPrecoConsulta[]> {
        const { data } = await httpClient.get<TabelaPrecoConsulta[]>("/cobrancas/config/tabela-preco", {
            params: busca ? { busca } : undefined,
        })
        return data
    },

    async salvarTabelaPreco(item: { id?: number; profissionalId?: string | null; valorSugerido: number }): Promise<void> {
        await httpClient.post("/cobrancas/config/tabela-preco", item)
    },

    async inativarTabelaPreco(id: number): Promise<void> {
        await httpClient.delete(`/cobrancas/config/tabela-preco/${id}`)
    },

    async listarConfigTaxa(): Promise<ConfigTaxaFormaPagamento[]> {
        const { data } = await httpClient.get<ConfigTaxaFormaPagamento[]>(
            "/cobrancas/config/taxa-forma-pagamento",
        )
        return data
    },

    async salvarConfigTaxa(item: {
        id?: number | null
        formaPagamentoId: number
        taxaPercentual: number
        ativo: boolean
    }): Promise<void> {
        await httpClient.post("/cobrancas/config/taxa-forma-pagamento", item)
    },

    // ── Aba Financeiro do paciente (F2) ────────────────────────────────────

    /** CA23/CA36: retorna KPIs + cobranças/pagamentos/estornos do paciente. */
    async obterFinanceiroAba(pacienteId: number): Promise<FinanceiroAba> {
        const { data } = await httpClient.get<FinanceiroAba>(
            `/cobrancas/paciente/${pacienteId}/financeiro-aba`,
        )
        return data
    },

    /** CA29/CA31: estorna um pagamento de uma cobrança. */
    async estornarPagamento(cobrancaId: number, pagamentoId: number, motivo: string): Promise<void> {
        await httpClient.post(`/cobrancas/${cobrancaId}/pagamentos/${pagamentoId}/estorno`, { motivo })
    },

    /**
     * F8/CA118: gera e baixa o PDF do recibo de um pagamento quitado.
     * Retorna o Blob para download no front. Lança erro se pagamento estornado (CA120) ou
     * não encontrado (CA121).
     */
    async emitirRecibo(pagamentoId: number): Promise<Blob> {
        const { data } = await httpClient.get<Blob>(
            `/cobrancas/pagamentos/${pagamentoId}/recibo`,
            { responseType: "blob" },
        )
        return data
    },
}
