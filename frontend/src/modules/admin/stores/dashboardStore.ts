import { defineStore } from "pinia"
import { ref } from "vue"
import {
    dashboardService,
    type KpisDashboardDto,
    type CrescimentoMensalPontoDto,
    type AlertasDashboardDto,
    type AuditLogPaginadoDto,
    type AuditLogFiltros,
} from "../services/dashboardService"

const CACHE_TTL_MS = 60_000 // 60s por bloco (W6-CA24 / D1)

/**
 * Store Pinia para o dashboard admin (Wave 6).
 *
 * Cache 60s por bloco — botão "Atualizar" força refresh ignorando cache.
 * Cada bloco carrega independente: falha de um não bloqueia os outros (W6-CA3/CA24).
 */
export const useDashboardStore = defineStore("adminDashboard", () => {
    // ── KPIs ──────────────────────────────────────────────────────────────────

    const kpis = ref<KpisDashboardDto | null>(null)
    const carregandoKpis = ref(false)
    const erroKpis = ref<string | null>(null)
    const kpisUltimaCarga = ref<number | null>(null)

    async function carregarKpis(forcar = false) {
        if (!forcar && kpisUltimaCarga.value && Date.now() - kpisUltimaCarga.value < CACHE_TTL_MS) return
        carregandoKpis.value = true
        erroKpis.value = null
        try {
            kpis.value = await dashboardService.obterKpis()
            kpisUltimaCarga.value = Date.now()
        } catch {
            erroKpis.value = "Não foi possível carregar os indicadores."
            kpis.value = null
        } finally {
            carregandoKpis.value = false
        }
    }

    // ── Crescimento mensal ────────────────────────────────────────────────────

    const crescimento = ref<CrescimentoMensalPontoDto[]>([])
    const carregandoCrescimento = ref(false)
    const erroCrescimento = ref<string | null>(null)
    const crescimentoUltimaCarga = ref<number | null>(null)

    async function carregarCrescimento(forcar = false) {
        if (!forcar && crescimentoUltimaCarga.value && Date.now() - crescimentoUltimaCarga.value < CACHE_TTL_MS) return
        carregandoCrescimento.value = true
        erroCrescimento.value = null
        try {
            crescimento.value = await dashboardService.obterCrescimentoMensal()
            crescimentoUltimaCarga.value = Date.now()
        } catch {
            erroCrescimento.value = "Não foi possível carregar o gráfico de crescimento."
            crescimento.value = []
        } finally {
            carregandoCrescimento.value = false
        }
    }

    // ── Alertas ───────────────────────────────────────────────────────────────

    const alertas = ref<AlertasDashboardDto | null>(null)
    const carregandoAlertas = ref(false)
    const erroAlertas = ref<string | null>(null)
    const alertasUltimaCarga = ref<number | null>(null)

    async function carregarAlertas(forcar = false) {
        if (!forcar && alertasUltimaCarga.value && Date.now() - alertasUltimaCarga.value < CACHE_TTL_MS) return
        carregandoAlertas.value = true
        erroAlertas.value = null
        try {
            alertas.value = await dashboardService.obterAlertas()
            alertasUltimaCarga.value = Date.now()
        } catch {
            erroAlertas.value = "Não foi possível carregar os alertas."
            alertas.value = null
        } finally {
            carregandoAlertas.value = false
        }
    }

    // ── Audit log ─────────────────────────────────────────────────────────────

    const auditLog = ref<AuditLogPaginadoDto | null>(null)
    const carregandoAuditLog = ref(false)
    const erroAuditLog = ref<string | null>(null)

    async function carregarAuditLog(filtros: AuditLogFiltros = {}) {
        carregandoAuditLog.value = true
        erroAuditLog.value = null
        try {
            auditLog.value = await dashboardService.listarAuditLog(filtros)
        } catch {
            erroAuditLog.value = "Não foi possível carregar o histórico de atividades."
            auditLog.value = null
        } finally {
            carregandoAuditLog.value = false
        }
    }

    return {
        // kpis
        kpis,
        carregandoKpis,
        erroKpis,
        carregarKpis,
        // crescimento
        crescimento,
        carregandoCrescimento,
        erroCrescimento,
        carregarCrescimento,
        // alertas
        alertas,
        carregandoAlertas,
        erroAlertas,
        carregarAlertas,
        // audit log
        auditLog,
        carregandoAuditLog,
        erroAuditLog,
        carregarAuditLog,
    }
})
