import httpClient from "./httpClient"

export interface ModeloDescricaoCirurgica {
    id: number
    titulo: string
    corpo: string
    ativo: boolean
    ehPadraoSistema: boolean
}

export const modeloDescricaoCirurgicaService = {
    /** Lista padrão-sistema + do estabelecimento, apenas ativos por padrão. */
    async listar(apenasAtivos = true): Promise<ModeloDescricaoCirurgica[]> {
        const { data } = await httpClient.get<ModeloDescricaoCirurgica[]>(
            "/prontuario/modelos-cirurgia",
            { params: { apenasAtivos } },
        )
        return data
    },

    async criar(titulo: string, corpo: string): Promise<void> {
        await httpClient.post("/prontuario/modelos-cirurgia", { titulo, corpo })
    },

    async editar(id: number, titulo: string, corpo: string): Promise<void> {
        await httpClient.put(`/prontuario/modelos-cirurgia/${id}`, { titulo, corpo })
    },

    async excluir(id: number): Promise<void> {
        await httpClient.delete(`/prontuario/modelos-cirurgia/${id}`)
    },
}
