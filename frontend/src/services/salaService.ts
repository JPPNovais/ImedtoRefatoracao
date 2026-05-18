import httpClient from "./httpClient"

export interface Sala {
    id: number
    estabelecimentoId: number
    unidadeId: number
    unidadeNome: string
    tipoSalaId: number | null
    tipoSalaNome: string | null
    nome: string
    descricao: string | null
    ativo: boolean
    criadoEm: string
}

export interface TipoSala {
    id: number
    nome: string
    descricao: string | null
}

export interface SalaPayload {
    unidadeId: number
    tipoSalaId: number | null
    nome: string
    descricao?: string | null
}

export const salaService = {
    async listar(estabelecimentoId: number, apenasAtivas?: boolean): Promise<Sala[]> {
        const params = apenasAtivas ? { apenasAtivas: true } : undefined
        const { data } = await httpClient.get<Sala[]>(
            `/estabelecimento/${estabelecimentoId}/salas`,
            { params },
        )
        return data
    },

    async criar(estabelecimentoId: number, payload: SalaPayload): Promise<void> {
        await httpClient.post(`/estabelecimento/${estabelecimentoId}/salas`, payload)
    },

    async atualizar(estabelecimentoId: number, salaId: number, payload: SalaPayload): Promise<void> {
        await httpClient.put(`/estabelecimento/${estabelecimentoId}/salas/${salaId}`, payload)
    },

    async excluir(estabelecimentoId: number, salaId: number): Promise<void> {
        await httpClient.delete(`/estabelecimento/${estabelecimentoId}/salas/${salaId}`)
    },

    async desativar(estabelecimentoId: number, salaId: number): Promise<void> {
        await httpClient.put(`/estabelecimento/${estabelecimentoId}/salas/${salaId}/desativar`)
    },

    async reativar(estabelecimentoId: number, salaId: number): Promise<void> {
        await httpClient.put(`/estabelecimento/${estabelecimentoId}/salas/${salaId}/reativar`)
    },

    async listarTipos(): Promise<TipoSala[]> {
        const { data } = await httpClient.get<TipoSala[]>("/tipos-sala")
        return data
    },
}
