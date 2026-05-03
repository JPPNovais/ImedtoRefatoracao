import httpClient from "./httpClient"

export interface ModeloPermissao {
    id: number
    // estabelecimentoId e atualizadoEm removidos: backend nao envia mais (LGPD - minimizacao).
    nome: string
    tipoAcesso: "Profissional" | "Recepcionista"
    permissoes: string[]
    ehPadrao: boolean
    criadoEm: string
}

export interface SalvarModeloPayload {
    nome: string
    tipoAcesso: string
    permissoes: string[]
}

export const permissaoService = {
    async listar(): Promise<ModeloPermissao[]> {
        const { data } = await httpClient.get<ModeloPermissao[]>("/estabelecimento/modelos-permissao")
        return data
    },

    async criar(payload: SalvarModeloPayload): Promise<{ modeloId: number }> {
        const { data } = await httpClient.post<{ modeloId: number }>(
            "/estabelecimento/modelos-permissao",
            payload,
        )
        return data
    },

    async atualizar(id: number, payload: SalvarModeloPayload): Promise<void> {
        await httpClient.put(`/estabelecimento/modelos-permissao/${id}`, payload)
    },

    async excluir(id: number): Promise<void> {
        await httpClient.delete(`/estabelecimento/modelos-permissao/${id}`)
    },

    /** Atribui (ou troca) o modelo de permissão de um vínculo profissional ativo. */
    async atribuirAoVinculo(vinculoId: number, modeloPermissaoId: number): Promise<void> {
        await httpClient.put(
            `/estabelecimento/profissionais/${vinculoId}/modelo-permissao`,
            { modeloPermissaoId },
        )
    },
}
