import httpClient from "./httpClient"

export type OrigemProcedimento = "TUSS" | "CBHPM" | "CUSTOMIZADO"

export interface ProcedimentoCatalogo {
    id: number
    codigo: string
    nome: string
    origem: OrigemProcedimento
    capitulo: string | null
}

export interface ProfissaoCatalogo {
    id: number
    nome: string
    conselhoSigla: string | null
    ativo: boolean
}

export interface EspecialidadeCatalogo {
    id: number
    profissaoId: number
    profissaoNome: string
    nome: string
    ativo: boolean
}

// Cache de sessão: Promise cacheada evita requisições concorrentes.
let _profissoesCache: Promise<ProfissaoCatalogo[]> | null = null
const _especialidadesCache = new Map<number, Promise<EspecialidadeCatalogo[]>>()

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

    /** Lista profissões ativas. Resultado cacheado durante a sessão. */
    listarProfissoes(): Promise<ProfissaoCatalogo[]> {
        if (!_profissoesCache) {
            _profissoesCache = httpClient
                .get<ProfissaoCatalogo[]>("/catalogo/profissoes", { params: { ativas: true } })
                .then(r => r.data)
                .catch(err => {
                    _profissoesCache = null  // permite retry em caso de falha
                    return Promise.reject(err)
                })
        }
        return _profissoesCache
    },

    /** Lista especialidades ativas para uma profissão. Resultado cacheado por profissaoId durante a sessão. */
    listarEspecialidades(profissaoId: number): Promise<EspecialidadeCatalogo[]> {
        if (!_especialidadesCache.has(profissaoId)) {
            const promise = httpClient
                .get<EspecialidadeCatalogo[]>("/catalogo/especialidades", {
                    params: { profissaoId, ativas: true },
                })
                .then(r => r.data)
                .catch(err => {
                    _especialidadesCache.delete(profissaoId)  // permite retry
                    return Promise.reject(err)
                })
            _especialidadesCache.set(profissaoId, promise)
        }
        return _especialidadesCache.get(profissaoId)!
    },
}
