import httpClient from "./httpClient"

export type TipoCategoria = "Receita" | "Despesa"

export interface CategoriaFinanceira {
    id: number
    nome: string
    tipo: TipoCategoria
    padrao: boolean
    ativo: boolean
}

export interface FormaPagamento {
    id: number
    nome: string
    padrao: boolean
    ativo: boolean
}

export const categoriaFinanceiraService = {
    async listar(): Promise<CategoriaFinanceira[]> {
        const { data } = await httpClient.get<CategoriaFinanceira[]>("/financeiro/categorias")
        return data
    },

    async criar(payload: { nome: string; tipo: TipoCategoria }): Promise<void> {
        await httpClient.post("/financeiro/categorias", payload)
    },

    async atualizar(id: number, payload: { nome: string; tipo: TipoCategoria }): Promise<void> {
        await httpClient.put(`/financeiro/categorias/${id}`, payload)
    },

    async excluir(id: number): Promise<void> {
        await httpClient.delete(`/financeiro/categorias/${id}`)
    },
}

export const formaPagamentoService = {
    async listar(): Promise<FormaPagamento[]> {
        const { data } = await httpClient.get<FormaPagamento[]>("/financeiro/formas-pagamento")
        return data
    },

    async criar(payload: { nome: string }): Promise<void> {
        await httpClient.post("/financeiro/formas-pagamento", payload)
    },

    async atualizar(id: number, payload: { nome: string }): Promise<void> {
        await httpClient.put(`/financeiro/formas-pagamento/${id}`, payload)
    },

    async excluir(id: number): Promise<void> {
        await httpClient.delete(`/financeiro/formas-pagamento/${id}`)
    },
}
