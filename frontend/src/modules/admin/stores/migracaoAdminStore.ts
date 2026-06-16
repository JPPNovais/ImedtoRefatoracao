import { defineStore } from "pinia"
import { ref } from "vue"
import {
    migracaoAdminService,
    type MigracaoJobAdminDto,
    type ListarJobsResult,
    type PreviewMigracaoResult,
    type RelatorioMigracaoResult,
    type RelatorioDesfazimentoResult,
    type MigracaoEventoDto,
    type ProgressoMigracaoDto,
} from "../services/migracaoAdminService"

export const useMigracaoAdminStore = defineStore("adminMigracao", () => {
    const jobs = ref<MigracaoJobAdminDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(25)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const jobAtual = ref<MigracaoJobAdminDto | null>(null)
    const preview = ref<PreviewMigracaoResult | null>(null)
    const relatorio = ref<RelatorioMigracaoResult | null>(null)
    const disparando = ref(false)
    const desfazendo = ref(false)
    const relatorioDesfazimento = ref<RelatorioDesfazimentoResult | null>(null)
    const reprocessando = ref(false)
    const aprovando = ref(false)
    const eventos = ref<MigracaoEventoDto[]>([])
    const carregandoEventos = ref(false)
    const progresso = ref<ProgressoMigracaoDto | null>(null)
    const carregandoProgresso = ref(false)
    const atualizandoEmBackground = ref(false)

    async function carregar(filtros: {
        estabelecimentoId?: number | null
        status?: string | null
        page?: number
        size?: number
        criadoDe?: string | null
        criadoAte?: string | null
        onda?: string | null
        origem?: string | null
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result: ListarJobsResult = await migracaoAdminService.listar({
                estabelecimentoId: filtros.estabelecimentoId ?? null,
                status: filtros.status ?? null,
                page: filtros.page ?? pagina.value,
                size: filtros.size ?? tamanho.value,
                criadoDe: filtros.criadoDe ?? null,
                criadoAte: filtros.criadoAte ?? null,
                onda: filtros.onda ?? null,
                origem: filtros.origem ?? null,
            })
            jobs.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanho
        } catch {
            erro.value = "Não foi possível carregar os jobs de migração."
        } finally {
            carregando.value = false
        }
    }

    async function carregarJob(jobId: number, silencioso = false): Promise<void> {
        if (silencioso) {
            atualizandoEmBackground.value = true
            try {
                jobAtual.value = await migracaoAdminService.obter(jobId)
            } catch {
                // Falha silenciosa no polling — não exibe erro para não interromper a revisão
            } finally {
                atualizandoEmBackground.value = false
            }
            return
        }
        carregando.value = true
        erro.value = null
        jobAtual.value = null
        try {
            jobAtual.value = await migracaoAdminService.obter(jobId)
        } catch {
            erro.value = "Job não encontrado."
        } finally {
            carregando.value = false
        }
    }

    async function salvarMapa(
        jobId: number,
        entidade: string,
        dePara: Record<string, string>,
        nomeBlocoOrigem?: string,
        entidadeReclassificada?: string | null,
        ignorado?: boolean,
    ): Promise<void> {
        await migracaoAdminService.salvarMapa(jobId, entidade, {
            dePara,
            nomeBlocoOrigem,
            entidadeReclassificada,
            ignorado,
        })
        // Recarrega o job para refletir revisão.
        if (jobAtual.value?.id === jobId) {
            await carregarJob(jobId)
        }
    }

    async function salvarTemplate(jobId: number, nomeTemplate: string): Promise<void> {
        await migracaoAdminService.salvarTemplate(jobId, { nomeTemplate })
    }

    async function gerarPreview(jobId: number): Promise<void> {
        preview.value = null
        const result = await migracaoAdminService.gerarPreview(jobId)
        preview.value = result
        // Atualiza status no jobAtual para refletir transição preview_pronto.
        if (jobAtual.value?.id === jobId) {
            await carregarJob(jobId)
        }
    }

    async function disparar(jobId: number): Promise<void> {
        disparando.value = true
        try {
            await migracaoAdminService.disparar(jobId)
            if (jobAtual.value?.id === jobId) {
                await carregarJob(jobId)
            }
        } finally {
            disparando.value = false
        }
    }

    async function carregarRelatorio(jobId: number): Promise<void> {
        relatorio.value = null
        relatorio.value = await migracaoAdminService.obterRelatorio(jobId)
    }

    /** CA17 — Desfaz a migração e popula relatorioDesfazimento com o relatório. */
    async function desfazer(jobId: number): Promise<void> {
        desfazendo.value = true
        relatorioDesfazimento.value = null
        try {
            relatorioDesfazimento.value = await migracaoAdminService.desfazer(jobId)
            if (jobAtual.value?.id === jobId) {
                await carregarJob(jobId)
            }
        } finally {
            desfazendo.value = false
        }
    }

    /** CA30 — Reprocessa um job em falhou, restaurando o status anterior. */
    async function reprocessar(jobId: number): Promise<void> {
        reprocessando.value = true
        try {
            await migracaoAdminService.reprocessar(jobId)
            if (jobAtual.value?.id === jobId) {
                await carregarJob(jobId)
            }
        } finally {
            reprocessando.value = false
        }
    }

    /** CA41 — Aprova a análise por IA de um job em aguardando_aprovacao. */
    async function aprovarAnalise(jobId: number): Promise<void> {
        aprovando.value = true
        try {
            await migracaoAdminService.aprovarAnalise(jobId)
            if (jobAtual.value?.id === jobId) {
                await carregarJob(jobId)
            }
        } finally {
            aprovando.value = false
        }
    }

    async function carregarEventos(jobId: number): Promise<void> {
        carregandoEventos.value = true
        try {
            eventos.value = await migracaoAdminService.obterEventos(jobId)
        } finally {
            carregandoEventos.value = false
        }
    }

    async function carregarProgresso(jobId: number, silencioso = false): Promise<void> {
        if (!silencioso) {
            carregandoProgresso.value = true
        }
        try {
            progresso.value = await migracaoAdminService.obterProgresso(jobId)
        } catch {
            // Falha silenciosa no polling — progresso continuará com valor anterior
        } finally {
            if (!silencioso) {
                carregandoProgresso.value = false
            }
        }
    }

    return {
        jobs,
        total,
        pagina,
        tamanho,
        carregando,
        erro,
        jobAtual,
        preview,
        relatorio,
        disparando,
        desfazendo,
        relatorioDesfazimento,
        reprocessando,
        aprovando,
        eventos,
        carregandoEventos,
        progresso,
        carregandoProgresso,
        atualizandoEmBackground,
        carregar,
        carregarJob,
        salvarMapa,
        salvarTemplate,
        gerarPreview,
        disparar,
        carregarRelatorio,
        desfazer,
        reprocessar,
        aprovarAnalise,
        carregarEventos,
        carregarProgresso,
    }
})
