import httpClient from "./httpClient"

// Tipos válidos: briefing 2026-06-05_001. Droga e AtividadeFisica removidos.
// Expectativa permanece válido no enum mas sem campo no prontuário nesta entrega.
export type TipoVariavelPool =
    | "Alergia"
    | "Medicamento"
    | "Doenca"
    | "Cirurgia"
    | "RelacaoFamiliar"
    | "Expectativa"

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
