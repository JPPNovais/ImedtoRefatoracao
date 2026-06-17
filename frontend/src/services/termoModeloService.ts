import httpClient from "./httpClient"

/**
 * Service de modelos (templates) de termo de consentimento.
 * Espelho 1:1 dos endpoints `/api/termos/modelos/*` (TermoModeloController).
 *
 * Permissões (espelho do back — defense-in-depth UX):
 *   - Listar/Obter: qualquer ação da área `termos`
 *   - CRUD/Clonar: `termos.gerenciar_modelos`
 *
 * Multi-tenant: o backend extrai `EstabelecimentoId` do tenant claim — front
 * NUNCA envia esse id. Isso impede troca de tenant por payload.
 */

export type CategoriaTermo =
    | "lgpd"
    | "cirurgico"
    | "imagem"
    | "financeiro"
    | "telemedicina"
    | "geral"

export interface TermoModeloDto {
    id: number
    /** null = modelo padrão do sistema (compartilhado). */
    estabelecimentoId: number | null
    categoria: CategoriaTermo
    titulo: string
    conteudoHtml: string
    ativo: boolean
    versaoAtual: number
    padraoClonadoDeId: number | null
    ehPadraoDoSistema: boolean
    criadoEm: string
    atualizadoEm: string | null
}

export interface PaginaModelosTermoDto {
    itens: TermoModeloDto[]
    pagina: number
    tamanho: number
    total: number
}

export interface ListarModelosFiltros {
    busca?: string
    categoria?: CategoriaTermo
    somenteAtivos?: boolean
    incluirPadroes?: boolean
    pagina?: number
    tamanho?: number
}

export interface SalvarModeloPayload {
    categoria: CategoriaTermo
    titulo: string
    conteudoHtml: string
}

export const termoModeloService = {
    async listarModelos(filtros: ListarModelosFiltros = {}): Promise<PaginaModelosTermoDto> {
        const { data } = await httpClient.get<PaginaModelosTermoDto>("/termos/modelos", {
            params: {
                busca: filtros.busca || undefined,
                categoria: filtros.categoria || undefined,
                somenteAtivos: filtros.somenteAtivos ?? false,
                incluirPadroes: filtros.incluirPadroes ?? false,
                pagina: filtros.pagina ?? 1,
                tamanho: filtros.tamanho ?? 10,
            },
        })
        return data
    },

    async listarPadroes(): Promise<TermoModeloDto[]> {
        const { data } = await httpClient.get<TermoModeloDto[]>("/termos/modelos/padroes")
        return data
    },

    async obterModelo(id: number): Promise<TermoModeloDto> {
        const { data } = await httpClient.get<TermoModeloDto>(`/termos/modelos/${id}`)
        return data
    },

    async criarModelo(payload: SalvarModeloPayload): Promise<number> {
        const { data } = await httpClient.post<{ modeloId: number }>("/termos/modelos", payload)
        return data.modeloId
    },

    async atualizarModelo(id: number, payload: SalvarModeloPayload): Promise<void> {
        await httpClient.put(`/termos/modelos/${id}`, payload)
    },

    async alterarAtivo(id: number, ativo: boolean): Promise<void> {
        await httpClient.patch(`/termos/modelos/${id}/ativo`, { ativo })
    },

    async excluirModelo(id: number): Promise<void> {
        await httpClient.delete(`/termos/modelos/${id}`)
    },

    async clonarPadrao(id: number): Promise<number> {
        const { data } = await httpClient.post<{ modeloId: number }>(`/termos/modelos/${id}/clonar`)
        return data.modeloId
    },
}
