import adminApi from "./adminApi"

export interface PlanoAdminDto {
    id: string
    nome: string
    descricaoCurta: string | null
    precoMensalCentavos: number | null
    gratuito: boolean
    ativo: boolean
    limitesJson: string
    /** JSON com 8 flags booleanas de feature (receitas, ia, etc.) */
    featuresJson: string
    criadoEm: string
    atualizadoEm: string | null
}

export interface ListarPlanosResult {
    itens: PlanoAdminDto[]
    total: number
    pagina: number
    tamanho: number
}

export interface CriarPlanoPayload {
    nome: string
    descricaoCurta: string | null
    precoMensalCentavos: number | null
    gratuito: boolean
    limitesJson: string
    featuresJson: string
    motivo: string
}

export interface AtualizarPlanoPayload extends CriarPlanoPayload {}

export const planosService = {
    async listar(params: {
        ativo?: boolean | null
        busca?: string | null
        page?: number
        size?: number
    }): Promise<ListarPlanosResult> {
        const { data } = await adminApi.get<ListarPlanosResult>("/planos", {
            params: {
                ativo: params.ativo ?? undefined,
                busca: params.busca || undefined,
                page: params.page ?? 1,
                size: params.size ?? 25,
            },
        })
        return data
    },

    async obter(id: string): Promise<PlanoAdminDto> {
        const { data } = await adminApi.get<PlanoAdminDto>(`/planos/${id}`)
        return data
    },

    async criar(payload: CriarPlanoPayload): Promise<void> {
        await adminApi.post("/planos", payload)
    },

    async atualizar(id: string, payload: AtualizarPlanoPayload): Promise<void> {
        await adminApi.put(`/planos/${id}`, payload)
    },

    async ativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/planos/${id}/ativar`, { motivo })
    },

    async desativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/planos/${id}/desativar`, { motivo })
    },
}
