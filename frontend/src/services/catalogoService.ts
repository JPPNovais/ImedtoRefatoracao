import httpClient from "./httpClient"

export type OrigemProcedimento = "TUSS" | "CBHPM" | "CUSTOMIZADO"

export interface ProcedimentoCatalogo {
    id: number
    codigo: string
    nome: string
    origem: OrigemProcedimento
    capitulo: string | null
}

export const catalogoService = {
    /**
     * Busca procedimentos no catálogo TUSS/CBHPM pelo termo informado.
     * Retorna no máximo `limit` resultados (padrão 20).
     * Deve ser chamado com debounce — mínimo 2 caracteres no `termo`.
     */
    async buscarProcedimentos(termo: string, limit = 20): Promise<ProcedimentoCatalogo[]> {
        const { data } = await httpClient.get<ProcedimentoCatalogo[]>("/catalogo/procedimentos", {
            params: { termo, limit },
        })
        return data
    },
}
