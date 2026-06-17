import httpClient from "./httpClient"

/**
 * Relatório de acessos LGPD (Art. 9º/18) de um paciente.
 * Endpoint somente leitura — sem CRUD.
 * Gate no backend: apenas papel Dono.
 * Audit: cada carga registra 1 linha em paciente_acesso_log (R4/CA10).
 */

export interface AcessoResumo {
    quem: string
    quando: string
    recurso: string
    acao: string
}

export interface PaginaAcessos {
    itens: AcessoResumo[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface ListarAcessosParams {
    pagina?: number
    tamanho?: number
}

export const acessoService = {
    async listarDoPaciente(
        pacienteId: number,
        params: ListarAcessosParams = {},
    ): Promise<PaginaAcessos> {
        const { data } = await httpClient.get<PaginaAcessos>(
            `/paciente/${pacienteId}/acessos`,
            {
                params: {
                    pagina: params.pagina ?? 1,
                    tamanho: params.tamanho ?? 10,
                },
            },
        )
        return data
    },
}
