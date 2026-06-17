import httpClient from "./httpClient"

/**
 * Documentos clínicos finalizados de um paciente (receitas emitidas, atestados
 * e pedidos de exame). Endpoint agregado — somente leitura, sem CRUD.
 * Cada tipo de documento tem seu próprio service para obtenção do completo
 * (receitaService.obter / atestadoService.obter / pedidoExameService.obter).
 */

export type TipoDocumento = "Receita" | "Atestado" | "PedidoExame"

export interface DocumentoResumo {
    tipo: TipoDocumento
    id: number
    titulo: string
    data: string
    profissionalNome: string | null
}

export interface PaginaDocumentos {
    itens: DocumentoResumo[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface ListarDocumentosParams {
    tipo?: TipoDocumento | null
    dataInicio?: string | null
    dataFim?: string | null
    busca?: string | null
    pagina?: number
    tamanho?: number
}

export const documentoService = {
    async listarDoPaciente(
        pacienteId: number,
        params: ListarDocumentosParams = {},
    ): Promise<PaginaDocumentos> {
        const { data } = await httpClient.get<PaginaDocumentos>(
            `/paciente/${pacienteId}/documentos`,
            {
                params: {
                    tipo: params.tipo ?? undefined,
                    dataInicio: params.dataInicio ?? undefined,
                    dataFim: params.dataFim ?? undefined,
                    busca: params.busca ?? undefined,
                    pagina: params.pagina ?? 1,
                    tamanho: params.tamanho ?? 10,
                },
            },
        )
        return data
    },
}
