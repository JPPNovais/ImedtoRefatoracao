import adminApi from "./adminApi"

// ─── DTOs ────────────────────────────────────────────────────────────────────

export interface MigracaoMapaDto {
    id: number
    entidade: string
    /** Addendum 4 — Nome do bloco de origem no dump JSON. Vazio para CSV/JSON-array. */
    nomeBlocoOrigem: string
    mapaJson: string
    revisadoPorUsuarioId: string | null
    revisadoEm: string | null
    criadoEm: string
}

/** Addendum 4 (D-S1) — Lista canônica de entidades suportadas pela carga. */
export const ENTIDADES_CANONICAS = [
    "paciente",
    "agendamento",
    "fornecedor_estoque",
    "categoria_estoque",
    "fabricante_estoque",
    "local_estoque",
    "item_estoque",
    "produto_orcamento",
    "procedimento_orcamento",
    "prontuario",
    "sem_equivalente",
] as const
export type EntidadeCanonica = (typeof ENTIDADES_CANONICAS)[number]

/** Addendum 4 — Campos extras do mapa_json (classificação + flags). */
export interface MapaJsonParsed {
    de_para: Record<string, string>
    confianca: number
    duvidas: string[]
    /** Entidade proposta pela IA (pode diferir da entidade confirmada pelo operador). */
    entidade_classificada?: EntidadeCanonica | string
    confianca_classificacao?: number
    /** Operador marcou como ignorar. */
    ignorado?: boolean
    /** Ingestão detectou encoding suspeito neste bloco. */
    encoding_suspeito?: boolean
    /** Bloco é objeto de config (ex.: estabelecimento{}) — não migrável. */
    eh_config?: boolean
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
    /** CA28 — Categoria legível do motivo da falha, sem PII. Null quando status != falhou. */
    motivoFalha: string | null
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
    /** Addendum 4 — Nome do bloco de origem. Vazio para CSV/JSON-array. */
    nomeBlocoOrigem?: string
    /** Addendum 4 — Entidade reclassificada pelo operador. Null = mantém a atual. */
    entidadeReclassificada?: string | null
    /** Addendum 4 — Operador marcou o bloco como ignorar. */
    ignorado?: boolean
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
    /** CA34 — motivo → quantidade de rejeições (genérico, sem PII). */
    motivosRejeicao: Record<string, number>
    /** CA35 — motivo → quantidade de pulos. */
    motivosPulo: Record<string, number>
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

export interface MigracaoEventoDto {
    statusAnterior: string | null
    statusNovo: string
    usuarioId: string | null
    criadoEm: string
}

export interface ProgressoEntidadeDto {
    total: number
    pendentes: number
    criados: number
    atualizados: number
    rejeitados: number
    pulados: number
    percentual: number
}

export interface ProgressoMigracaoDto {
    porEntidade: Record<string, ProgressoEntidadeDto>
    percentualAgregado: number
}

// ─── Service ─────────────────────────────────────────────────────────────────

const base = "/migracao"

export const migracaoAdminService = {
    async listar(params: {
        estabelecimentoId?: number | null
        status?: string | null
        page?: number
        size?: number
        criadoDe?: string | null
        criadoAte?: string | null
        onda?: string | null
        origem?: string | null
    }): Promise<ListarJobsResult> {
        const { data } = await adminApi.get<ListarJobsResult>(base, {
            params: {
                estabelecimentoId: params.estabelecimentoId ?? undefined,
                status: params.status || undefined,
                page: params.page ?? 1,
                size: params.size ?? 25,
                criadoDe: params.criadoDe || undefined,
                criadoAte: params.criadoAte || undefined,
                onda: params.onda || undefined,
                origem: params.origem || undefined,
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

    /**
     * CA30 — Reprocessa um job em estado "falhou", restaurando o status anterior.
     * Apenas ImedtoAdmin pode chamar este endpoint (RBAC no backend).
     */
    async reprocessar(jobId: number): Promise<void> {
        await adminApi.post(`${base}/${jobId}/reprocessar`)
    },

    /**
     * CA41 — Aprova a análise por IA de um job em aguardando_aprovacao.
     * Transição: aguardando_aprovacao → aguardando_mapa.
     * Apenas ImedtoAdmin pode chamar (RBAC no backend — R-A4/CA43).
     */
    async aprovarAnalise(jobId: number): Promise<void> {
        await adminApi.post(`${base}/${jobId}/aprovar-analise`)
    },

    async obterEventos(jobId: number): Promise<MigracaoEventoDto[]> {
        const { data } = await adminApi.get<MigracaoEventoDto[]>(`${base}/${jobId}/eventos`)
        return data
    },

    async obterProgresso(jobId: number): Promise<ProgressoMigracaoDto> {
        const { data } = await adminApi.get<ProgressoMigracaoDto>(`${base}/${jobId}/progresso`)
        return data
    },
}
