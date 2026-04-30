import httpClient from "./httpClient"

export type StatusSolicitacao = "Pendente" | "Aprovada" | "Recusada" | "Cancelada"

export interface SolicitacaoVinculo {
    id: number
    profissionalUsuarioId: string
    profissionalNome?: string
    estabelecimentoId: number
    estabelecimentoNome?: string
    status: StatusSolicitacao
    mensagem: string | null
    criadaEm: string
    respondidaEm: string | null
    respondidaPorUsuarioId: string | null
    motivoRecusa: string | null
}

export interface CriarSolicitacaoPayload {
    estabelecimentoId: number
    mensagem?: string
}

export const solicitacaoVinculoService = {
    async criar(payload: CriarSolicitacaoPayload): Promise<SolicitacaoVinculo> {
        const { data } = await httpClient.post<SolicitacaoVinculo>("/solicitacoes-vinculo", payload)
        return data
    },

    async listarMinhas(): Promise<SolicitacaoVinculo[]> {
        const { data } = await httpClient.get<SolicitacaoVinculo[]>("/solicitacoes-vinculo/minhas")
        return data
    },

    async listarRecebidas(): Promise<SolicitacaoVinculo[]> {
        const { data } = await httpClient.get<SolicitacaoVinculo[]>("/solicitacoes-vinculo/recebidas")
        return data
    },

    async aprovar(id: number): Promise<void> {
        await httpClient.post(`/solicitacoes-vinculo/${id}/aprovar`)
    },

    async recusar(id: number, motivo?: string): Promise<void> {
        await httpClient.post(`/solicitacoes-vinculo/${id}/recusar`, { motivo })
    },

    async cancelar(id: number): Promise<void> {
        await httpClient.post(`/solicitacoes-vinculo/${id}/cancelar`)
    },
}
