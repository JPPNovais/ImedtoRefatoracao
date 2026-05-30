/**
 * Service HTTP para o dashboard admin (Wave 6).
 *
 * Usa adminApi (axios isolado, cookie imedto_admin_session).
 * Zero importação de stores/services do app principal.
 * Leitura não gera audit (W6-CA22).
 */
import adminApi from "./adminApi"

// ── DTOs ─────────────────────────────────────────────────────────────────────

export interface KpisDashboardDto {
    estabelecimentosAtivos: number
    estabelecimentosInativos: number
    adminsAtivos: number
    trialsEmAndamento: number
    trialsExpirandoEm7Dias: number
    assinaturasVigentes: number
    assinaturasGratuitas: number
}

export interface CrescimentoMensalPontoDto {
    /** Formato "YYYY-MM" */
    mes: string
    total: number
}

export interface TrialExpirandoDto {
    estabelecimentoId: number
    nomeFantasia: string
    donoNome: string
    fimEm: string
    diasRestantes: number
}

export interface SemAssinaturaDto {
    estabelecimentoId: number
    nomeFantasia: string
    donoNome: string
    criadoEm: string
}

export interface AlertasDashboardDto {
    trialsExpirando: TrialExpirandoDto[]
    semAssinatura: SemAssinaturaDto[]
    semAssinaturaTotal: number
}

export interface AuditLogItemDto {
    id: string
    criadoEm: string
    adminId: string | null
    adminNome: string | null
    adminEmail: string | null
    adminAtivo: boolean
    acao: string
    recursoTipo: string | null
    recursoId: string | null
    tenantAfetadoId: number | null
    tenantNomeFantasia: string | null
    motivo: string | null
}

export interface AuditLogPaginadoDto {
    itens: AuditLogItemDto[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface AuditLogFiltros {
    acao?: string
    adminId?: string
    periodo?: string
    pagina?: number
    tamanhoPagina?: number
}

// ── Service ───────────────────────────────────────────────────────────────────

export const dashboardService = {
    async obterKpis(): Promise<KpisDashboardDto> {
        const { data } = await adminApi.get<KpisDashboardDto>("/dashboard/kpis")
        return data
    },

    async obterCrescimentoMensal(): Promise<CrescimentoMensalPontoDto[]> {
        const { data } = await adminApi.get<CrescimentoMensalPontoDto[]>("/dashboard/crescimento-mensal")
        return data
    },

    async obterAlertas(): Promise<AlertasDashboardDto> {
        const { data } = await adminApi.get<AlertasDashboardDto>("/dashboard/alertas")
        return data
    },

    async listarAuditLog(filtros: AuditLogFiltros = {}): Promise<AuditLogPaginadoDto> {
        const { data } = await adminApi.get<AuditLogPaginadoDto>("/dashboard/audit-log", {
            params: {
                acao: filtros.acao || undefined,
                adminId: filtros.adminId || undefined,
                periodo: filtros.periodo ?? "7d",
                pagina: filtros.pagina ?? 1,
                tamanhoPagina: filtros.tamanhoPagina ?? 20,
            },
        })
        return data
    },
}
