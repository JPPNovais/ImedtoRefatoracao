import httpClient from "./httpClient"

export interface ItemInventario {
    id: number
    // estabelecimentoId e atualizadoEm removidos: backend nao envia mais (LGPD - minimizacao).
    codigo: string
    nome: string
    categoria: string
    categoriaId: number | null
    categoriaCor: string | null
    categoriaIcone: string | null
    fabricanteId: number | null
    fabricanteNome: string | null
    fornecedorPadraoId: number | null
    fornecedorPadraoNome: string | null
    localPadraoId: number | null
    localPadraoNome: string | null
    unidadeMedida: string
    quantidadeAtual: number
    quantidadeMinima: number
    custoMedio: number
    custoUnitario: number | null
    estoqueAbaixoMinimo: boolean
    ativo: boolean
    criadoEm: string
}

export type TipoMovimentacao = "Entrada" | "Saida" | "Inativacao"

export interface MovimentacaoEstoque {
    id: number
    itemInventarioId: number
    itemNome: string
    tipo: TipoMovimentacao
    quantidade: number
    quantidadeAnterior: number
    quantidadeApos: number
    custoUnitario: number
    custoTotal: number
    observacao: string | null
    usuarioNome: string
    criadoEm: string
}

export interface PaginaItensInventario {
    itens: ItemInventario[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface PaginaMovimentacoesEstoque {
    itens: MovimentacaoEstoque[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface CriarItemInventarioPayload {
    codigo: string
    nome: string
    categoriaId: number
    fabricanteId?: number | null
    fornecedorPadraoId?: number | null
    localPadraoId?: number | null
    unidadeMedida: string
    quantidadeInicial: number
    quantidadeMinima: number
    custoUnitarioInicial?: number
    custoUnitario?: number | null
}

export interface AtualizarItemInventarioPayload {
    nome: string
    categoriaId: number
    fabricanteId?: number | null
    fornecedorPadraoId?: number | null
    localPadraoId?: number | null
    unidadeMedida: string
    quantidadeMinima: number
    custoUnitario?: number | null
}

export const inventarioService = {
    async listarItens(params?: {
        categoria?: string
        apenasAbaixoMinimo?: boolean
        apenasAtivos?: boolean
        pagina?: number
        tamanho?: number
    }): Promise<PaginaItensInventario> {
        const { data } = await httpClient.get<PaginaItensInventario>("/inventario/itens", { params })
        return data
    },

    async criarItem(payload: CriarItemInventarioPayload): Promise<{ itemId: number }> {
        const { data } = await httpClient.post<{ itemId: number }>("/inventario/itens", payload)
        return data
    },

    async atualizarItem(id: number, payload: AtualizarItemInventarioPayload): Promise<void> {
        await httpClient.put(`/inventario/itens/${id}`, payload)
    },

    async inativarItem(id: number, observacao?: string | null): Promise<void> {
        await httpClient.post(`/inventario/itens/${id}/inativar`, { observacao: observacao ?? null })
    },

    async listarMovimentacoes(params?: {
        itemInventarioId?: number
        dataInicio?: string
        dataFim?: string
        pagina?: number
        tamanho?: number
    }): Promise<PaginaMovimentacoesEstoque> {
        const { data } = await httpClient.get<PaginaMovimentacoesEstoque>("/inventario/movimentacoes", { params })
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
