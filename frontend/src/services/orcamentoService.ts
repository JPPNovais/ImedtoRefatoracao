import httpClient from "./httpClient"

export interface ItemOrcamento {
    id: number
    descricao: string
    quantidade: number
    valorUnitario: number
    descontoPercent: number
    subtotal: number
}

export interface OrcamentoResumo {
    id: number
    estabelecimentoId: number
    pacienteId: number
    pacienteNome: string
    numero: string
    status: "Pendente" | "Aprovado" | "Recusado" | "Expirado"
    validade: string
    total: number
    criadoPorNome: string
    criadoEm: string
    atualizadoEm: string | null
}

export interface Orcamento extends OrcamentoResumo {
    observacoes: string | null
    itens: ItemOrcamento[]
}

export interface ItemPayload {
    descricao: string
    quantidade: number
    valorUnitario: number
    descontoPercent: number
}

export const orcamentoService = {
    async listar(params?: {
        pacienteId?: number
        status?: string
    }): Promise<OrcamentoResumo[]> {
        const { data } = await httpClient.get<OrcamentoResumo[]>("/orcamentos", { params })
        return data
    },

    async obter(id: number): Promise<Orcamento> {
        const { data } = await httpClient.get<Orcamento>(`/orcamentos/${id}`)
        return data
    },

    async criar(payload: {
        pacienteId: number
        validade: string
        observacoes?: string | null
        itens: ItemPayload[]
    }): Promise<{ orcamentoId: number }> {
        const { data } = await httpClient.post<{ orcamentoId: number }>("/orcamentos", payload)
        return data
    },

    async atualizar(id: number, payload: {
        validade: string
        observacoes?: string | null
        itens: ItemPayload[]
    }): Promise<void> {
        await httpClient.put(`/orcamentos/${id}`, payload)
    },

    async aprovar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/aprovar`)
    },

    async recusar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/recusar`)
    },
}
