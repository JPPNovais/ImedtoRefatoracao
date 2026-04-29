import httpClient from "./httpClient"

export interface ItemInventario {
    id: number
    estabelecimentoId: number
    codigo: string
    nome: string
    categoria: string
    unidadeMedida: string
    quantidadeAtual: number
    quantidadeMinima: number
    custoMedio: number
    estoqueAbaixoMinimo: boolean
    ativo: boolean
    criadoEm: string
    atualizadoEm: string | null
}

export interface MovimentacaoEstoque {
    id: number
    itemInventarioId: number
    itemNome: string
    tipo: "Entrada" | "Saida"
    quantidade: number
    quantidadeAnterior: number
    quantidadeApos: number
    custoUnitario: number
    custoTotal: number
    observacao: string | null
    usuarioNome: string
    criadoEm: string
}

export const inventarioService = {
    async listarItens(params?: {
        categoria?: string
        apenasAbaixoMinimo?: boolean
        apenasAtivos?: boolean
    }): Promise<ItemInventario[]> {
        const { data } = await httpClient.get<ItemInventario[]>("/inventario/itens", { params })
        return data
    },

    async criarItem(payload: {
        codigo: string
        nome: string
        categoria: string
        unidadeMedida: string
        quantidadeInicial: number
        quantidadeMinima: number
        custoUnitarioInicial?: number
    }): Promise<{ itemId: number }> {
        const { data } = await httpClient.post<{ itemId: number }>("/inventario/itens", payload)
        return data
    },

    async atualizarItem(id: number, payload: {
        nome: string
        categoria: string
        unidadeMedida: string
        quantidadeMinima: number
    }): Promise<void> {
        await httpClient.put(`/inventario/itens/${id}`, payload)
    },

    async inativarItem(id: number): Promise<void> {
        await httpClient.post(`/inventario/itens/${id}/inativar`)
    },

    async listarMovimentacoes(params?: {
        itemInventarioId?: number
        dataInicio?: string
        dataFim?: string
        limite?: number
    }): Promise<MovimentacaoEstoque[]> {
        const { data } = await httpClient.get<MovimentacaoEstoque[]>("/inventario/movimentacoes", { params })
        return data
    },

    async registrarMovimentacao(payload: {
        itemInventarioId: number
        tipo: "Entrada" | "Saida"
        quantidade: number
        custoUnitario?: number
        observacao?: string | null
    }): Promise<void> {
        await httpClient.post("/inventario/movimentacoes", payload)
    },
}
