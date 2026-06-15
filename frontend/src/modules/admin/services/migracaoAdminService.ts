import adminApi from "./adminApi"

// ─── DTOs ────────────────────────────────────────────────────────────────────

export interface MigracaoMapaDto {
    id: number
    entidade: string
    mapaJson: string
    revisadoPorUsuarioId: string | null
    revisadoEm: string | null
    criadoEm: string
}

export interface MigracaoJobAdminDto {
    id: number
    estabelecimentoId: number
    status: string
    origem: string | null
    criadoPorUsuarioId: string
    criadoEm: string
    atualizadoEm: string
    /** CA18 — Id do template de origem usado para pré-preencher mapas. Null = sem template. */
    templateOrigemId: number | null
    /** Nome do template (join na query). Null quando templateOrigemId é null. */
    nomeTemplate: string | null
    mapas: MigracaoMapaDto[]
}

export interface ListarJobsResult {
    itens: MigracaoJobAdminDto[]
    total: number
    pagina: number
    tamanho: number
}

export interface SalvarMapaPayload {
    dePara: Record<string, string>
}

export interface SalvarTemplatePayload {
    nomeTemplate: string
}

// ─── Service ─────────────────────────────────────────────────────────────────

const base = "/migracao"

export const migracaoAdminService = {
    async listar(params: {
        estabelecimentoId?: number | null
        status?: string | null
        page?: number
        size?: number
    }): Promise<ListarJobsResult> {
        const { data } = await adminApi.get<ListarJobsResult>(base, {
            params: {
                estabelecimentoId: params.estabelecimentoId ?? undefined,
                status: params.status || undefined,
                page: params.page ?? 1,
                size: params.size ?? 25,
            },
        })
        return data
    },

    async obter(jobId: number): Promise<MigracaoJobAdminDto> {
        const { data } = await adminApi.get<MigracaoJobAdminDto>(`${base}/${jobId}`)
        return data
    },

    async salvarMapa(jobId: number, entidade: string, payload: SalvarMapaPayload): Promise<void> {
        await adminApi.put(`${base}/${jobId}/mapas/${entidade}`, payload)
    },

    async salvarTemplate(jobId: number, payload: SalvarTemplatePayload): Promise<void> {
        await adminApi.post(`${base}/${jobId}/template`, payload)
    },
}
