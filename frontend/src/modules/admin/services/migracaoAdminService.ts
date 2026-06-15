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

// Marco 3 — preview + disparo + relatório

export interface EntidadePreview {
    pendentes: number
}

export interface PreviewMigracaoResult {
    totalRegistros: number
    porEntidade: Record<string, EntidadePreview>
}

export interface RelatorioEntidadeResult {
    criados: number
    atualizados: number
    rejeitados: number
    pulados: number
}

export interface RelatorioMigracaoResult {
    totalCriados: number
    totalAtualizados: number
    totalRejeitados: number
    totalPulados: number
    porEntidade: Record<string, RelatorioEntidadeResult>
}

/** CA17, R9 — Resultado do desfazer de migração. */
export interface RelatorioDesfazimentoResult {
    /** Registros criados pelo job que foram revertidos com sucesso. */
    totalRevertidos: number
    /** Registros criados que NÃO puderam ser revertidos (FK ativa de outro fluxo). */
    totalNaoRevertidos: number
    /** Registros atualizados pelo upsert — não são revertidos, apenas reportados. */
    totalAtualizadosMantidos: number
    /** Mensagem explicativa exibida ao operador. */
    aviso: string
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

    async gerarPreview(jobId: number): Promise<PreviewMigracaoResult> {
        const { data } = await adminApi.put<PreviewMigracaoResult>(`${base}/${jobId}/preview-pronto`)
        return data
    },

    async disparar(jobId: number): Promise<void> {
        await adminApi.post(`${base}/${jobId}/disparar`)
    },

    async obterRelatorio(jobId: number): Promise<RelatorioMigracaoResult> {
        const { data } = await adminApi.get<RelatorioMigracaoResult>(`${base}/${jobId}/relatorio`)
        return data
    },

    /**
     * CA17 — Desfaz a migração: reverte SOMENTE os registros criados pelo job.
     * Registros atualizados (que já existiam) NÃO são tocados — relatório avisa.
     */
    async desfazer(jobId: number): Promise<RelatorioDesfazimentoResult> {
        const { data } = await adminApi.post<RelatorioDesfazimentoResult>(`${base}/${jobId}/desfazer`)
        return data
    },
}
