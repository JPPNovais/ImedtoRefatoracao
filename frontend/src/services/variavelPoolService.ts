import httpClient from "./httpClient"

export type TipoVariavelPool =
    | "Alergia"
    | "Medicamento"
    | "Doenca"
    | "Cirurgia"
    | "Droga"
    | "RelacaoFamiliar"
    | "Expectativa"
    | "AtividadeFisica"

export interface VariavelPool {
    id: number
    estabelecimentoId: number | null
    tipo: TipoVariavelPool
    nome: string
    ativo: boolean
    ehPadraoSistema: boolean
}

export const variavelPoolService = {
    /** Lista padrão-sistema + customizadas do estabelecimento, opcionalmente filtrado por tipo. */
    async listar(tipo?: TipoVariavelPool, apenasAtivos = true): Promise<VariavelPool[]> {
        const params: Record<string, string | boolean> = { apenasAtivos }
        if (tipo) params.tipo = tipo
        const { data } = await httpClient.get<VariavelPool[]>("/prontuario/pool", { params })
        return data
    },

    async adicionar(tipo: TipoVariavelPool, nome: string): Promise<void> {
        await httpClient.post("/prontuario/pool", { tipo, nome })
    },

    async atualizar(id: number, nome: string): Promise<void> {
        await httpClient.put(`/prontuario/pool/${id}`, { nome })
    },

    async excluir(id: number): Promise<void> {
        await httpClient.delete(`/prontuario/pool/${id}`)
    },
}
