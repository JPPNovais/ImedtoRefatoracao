import { defineStore } from "pinia"
import { ref } from "vue"
import {
    migracaoAdminService,
    type MigracaoJobAdminDto,
    type ListarJobsResult,
    type PreviewMigracaoResult,
    type RelatorioMigracaoResult,
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

    async function carregar(filtros: {
        estabelecimentoId?: number | null
        status?: string | null
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result: ListarJobsResult = await migracaoAdminService.listar({
                estabelecimentoId: filtros.estabelecimentoId ?? null,
                status: filtros.status ?? null,
                page: filtros.page ?? pagina.value,
                size: filtros.size ?? tamanho.value,
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

    async function carregarJob(jobId: number): Promise<void> {
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
    ): Promise<void> {
        await migracaoAdminService.salvarMapa(jobId, entidade, { dePara })
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
        carregar,
        carregarJob,
        salvarMapa,
        salvarTemplate,
        gerarPreview,
        disparar,
        carregarRelatorio,
    }
})
