import httpClient from "./httpClient"

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
    observacao?: string
    ordem?: number
}

export interface OrcamentoCompleto {
    id: number
    pacienteId: number
    pacienteNome: string
    tipo: "Simples" | "Cirurgico"
    status: string
    totalBruto: number
    desconto: number
    totalLiquido: number
    custoImplantesTotal: number
    procedimentoCirurgicoId?: number | null
    equipe: OrcamentoEquipe[]
    implantes: OrcamentoImplante[]
    formasPagamento: OrcamentoFormaPagamento[]
    validade?: string | null
    observacoes?: string | null
}

export interface CriarOrcamentoCompletoPayload {
    pacienteId: number
    tipo: "Simples" | "Cirurgico"
    procedimentoCirurgicoId?: number | null
    validade?: string | null
    observacoes?: string | null
    itens: Array<{
        descricao: string
        quantidade: number
        valorUnitario: number
        descontoPercent?: number
    }>
    equipe?: OrcamentoEquipe[]
    implantes?: OrcamentoImplante[]
    formasPagamento?: OrcamentoFormaPagamento[]
}

export const orcamentoCompletoService = {
    async obter(id: number): Promise<OrcamentoCompleto> {
        const { data } = await httpClient.get<OrcamentoCompleto>(`/orcamentos/${id}/completo`)
        return data
    },

    async criar(payload: CriarOrcamentoCompletoPayload): Promise<OrcamentoCompleto> {
        const { data } = await httpClient.post<OrcamentoCompleto>("/orcamentos/completo", payload)
        return data
    },

    async atualizar(id: number, payload: CriarOrcamentoCompletoPayload): Promise<void> {
        await httpClient.put(`/orcamentos/${id}/completo`, payload)
    },
}

export default orcamentoCompletoService
