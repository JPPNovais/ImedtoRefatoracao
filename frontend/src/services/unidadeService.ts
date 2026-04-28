import httpClient from "./httpClient"

export interface Unidade {
    id: number
    estabelecimentoId: number
    nome: string
    isPrincipal: boolean
    cep: string | null
    logradouro: string | null
    numero: string | null
    complemento: string | null
    bairro: string | null
    cidade: string | null
    estado: string | null
    telefone: string | null
    ativo: boolean
    criadoEm: string
}

export interface UnidadePayload {
    nome: string
    isPrincipal: boolean
    cep?: string | null
    logradouro?: string | null
    numero?: string | null
    complemento?: string | null
    bairro?: string | null
    cidade?: string | null
    estado?: string | null
    telefone?: string | null
}

export const unidadeService = {
    async listar(estabelecimentoId: number): Promise<Unidade[]> {
        const { data } = await httpClient.get<Unidade[]>(`/estabelecimento/${estabelecimentoId}/unidades`)
        return data
    },

    async criar(estabelecimentoId: number, payload: UnidadePayload): Promise<void> {
        await httpClient.post(`/estabelecimento/${estabelecimentoId}/unidades`, payload)
    },

    async atualizar(estabelecimentoId: number, unidadeId: number, payload: UnidadePayload): Promise<void> {
        await httpClient.put(`/estabelecimento/${estabelecimentoId}/unidades/${unidadeId}`, payload)
    },

    async excluir(estabelecimentoId: number, unidadeId: number): Promise<void> {
        await httpClient.delete(`/estabelecimento/${estabelecimentoId}/unidades/${unidadeId}`)
    },
}
