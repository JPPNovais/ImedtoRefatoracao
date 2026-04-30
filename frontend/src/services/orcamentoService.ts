import httpClient from "./httpClient"

/**
 * Status do orçamento — alinhado com o legado e o backend novo.
 * Rascunho (inicial) → Enviado → Aprovado | Recusado | Cancelado.
 * Expirado é definido pela validade vencida (job/scheduler).
 */
export type OrcamentoStatus =
    | "Rascunho"
    | "Enviado"
    | "Aprovado"
    | "Recusado"
    | "Cancelado"
    | "Expirado"

export interface ItemOrcamento {
    id: number
    descricao: string
    quantidade: number
    valorUnitario: number
    descontoPercent: number
    subtotal: number
}

export interface OrcamentoEquipe {
    id?: number
    profissionalUsuarioId: string
    profissionalNome?: string
    papel: string
    valor: number
    ordem?: number
}

export interface OrcamentoImplante {
    id?: number
    itemInventarioId?: number | null
    descricao: string
    quantidade: number
    custoUnitario: number
    custoTotal: number
}

export interface OrcamentoFormaPagamento {
    id?: number
    formaPagamentoId: number
    formaPagamentoNome?: string
    valor: number
    parcelas: number
    acrescimoPercentual: number
    entradaPercentual: number
    observacao?: string | null
    ordem?: number
}

export interface OrcamentoCirurgia {
    id?: number
    procedimentoCirurgicoId?: number | null
    descricao?: string | null
    quantidade: number
    duracaoMinutos?: number | null
    valorTotal: number
    ordem?: number
}

export interface OrcamentoInternacao {
    tipoInternacao: string
    dias: number
    valorDiaria: number
    valorTotal: number
}

export interface OrcamentoAnestesia {
    tipoAnestesia: string
    valor: number
    observacao?: string | null
}

export interface OrcamentoResumo {
    id: number
    estabelecimentoId: number
    pacienteId: number
    pacienteNome: string
    numero: string
    status: OrcamentoStatus
    validade: string
    total: number
    criadoPorNome: string
    criadoEm: string
    atualizadoEm: string | null
}

/**
 * Detalhe completo do orçamento (aggregate único). Não há mais distinção
 * "resumo simples vs completo" — todas as collections vêm sempre.
 */
export interface Orcamento extends OrcamentoResumo {
    observacoes: string | null
    procedimentoCirurgicoId: number | null
    custoImplantesTotal: number
    itens: ItemOrcamento[]
    equipe: OrcamentoEquipe[]
    implantes: OrcamentoImplante[]
    formasPagamento: OrcamentoFormaPagamento[]
    cirurgias: OrcamentoCirurgia[]
    internacao: OrcamentoInternacao | null
    anestesia: OrcamentoAnestesia | null
}

export interface CriarOrcamentoPayload {
    pacienteId: number
    validade: string
    observacoes?: string | null
    procedimentoCirurgicoId?: number | null
    itens?: Array<{
        descricao: string
        quantidade: number
        valorUnitario: number
        descontoPercent: number
    }>
    equipe?: Array<Pick<OrcamentoEquipe, "profissionalUsuarioId" | "papel" | "valor">>
    implantes?: Array<Pick<OrcamentoImplante, "itemInventarioId" | "descricao" | "quantidade" | "custoUnitario">>
    formasPagamento?: Array<Pick<OrcamentoFormaPagamento,
        "formaPagamentoId" | "valor" | "parcelas" | "acrescimoPercentual" | "entradaPercentual" | "observacao">>
    cirurgias?: Array<Pick<OrcamentoCirurgia,
        "procedimentoCirurgicoId" | "descricao" | "quantidade" | "duracaoMinutos" | "valorTotal">>
    internacao?: { tipo: string; dias: number; valorDiaria: number } | null
    anestesia?: { tipo: string; valor: number; observacao?: string | null } | null
}

export type AtualizarOrcamentoPayload = Omit<CriarOrcamentoPayload, "pacienteId">

// ─────────────────────────── Preview (Fase 6.3)
export interface FormaPagamentoCalculada {
    formaPagamentoId: number
    formaPagamentoNome: string | null
    valor: number
    parcelas: number
    acrescimoPercentual: number
    entradaPercentual: number
    totalBruto: number
    entrada: number
    valorParcela: number
}

export interface PreviewOrcamento {
    totalCirurgias: number
    totalEquipe: number
    totalImplantes: number
    totalInternacao: number
    totalAnestesia: number
    totalItens: number
    totalGeral: number
    somaFormas: number
    diferenca: number
    integridadeOk: boolean
    formas: FormaPagamentoCalculada[]
}

/**
 * Payload do preview — mesma estrutura da criação/atualização, mas só campos
 * que influem no cálculo (sem paciente/validade/observações).
 */
export type PreviewOrcamentoPayload = Pick<CriarOrcamentoPayload,
    "itens" | "equipe" | "implantes" | "formasPagamento" | "cirurgias" | "internacao" | "anestesia">

export const orcamentoService = {
    async listar(params?: {
        pacienteId?: number
        status?: OrcamentoStatus
    }): Promise<OrcamentoResumo[]> {
        const { data } = await httpClient.get<OrcamentoResumo[]>("/orcamentos", { params })
        return data
    },

    async obter(id: number): Promise<Orcamento> {
        const { data } = await httpClient.get<Orcamento>(`/orcamentos/${id}`)
        return data
    },

    async criar(payload: CriarOrcamentoPayload): Promise<{ orcamentoId: number }> {
        const { data } = await httpClient.post<{ orcamentoId: number }>("/orcamentos", payload)
        return data
    },

    async atualizar(id: number, payload: AtualizarOrcamentoPayload): Promise<void> {
        await httpClient.put(`/orcamentos/${id}`, payload)
    },

    async enviar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/enviar`)
    },

    async aprovar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/aprovar`)
    },

    async recusar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/recusar`)
    },

    async cancelar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/cancelar`)
    },

    async converterEmCirurgia(id: number, dataAgendada?: string | null): Promise<{ procedimentoCirurgicoId: number }> {
        const { data } = await httpClient.post<{ procedimentoCirurgicoId: number }>(
            `/orcamentos/${id}/converter-em-cirurgia`,
            { dataAgendada: dataAgendada ?? null },
        )
        return data
    },

    /**
     * Preview dos cálculos sem persistir. Usar com debounce — frontend chama
     * a cada edição relevante do form de orçamento.
     */
    async preview(payload: PreviewOrcamentoPayload): Promise<PreviewOrcamento> {
        const { data } = await httpClient.post<PreviewOrcamento>("/orcamentos/preview", payload)
        return data
    },
}

export default orcamentoService
