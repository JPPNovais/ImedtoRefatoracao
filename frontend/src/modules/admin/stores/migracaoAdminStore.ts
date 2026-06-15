import { defineStore } from "pinia"
import { ref } from "vue"
import {
    migracaoAdminService,
    type MigracaoJobAdminDto,
    type ListarJobsResult,
} from "../services/migracaoAdminService"

export const useMigracaoAdminStore = defineStore("adminMigracao", () => {
    const jobs = ref<MigracaoJobAdminDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(25)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const jobAtual = ref<MigracaoJobAdminDto | null>(null)

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

    return {
        jobs,
        total,
        pagina,
        tamanho,
        carregando,
        erro,
        jobAtual,
        carregar,
        carregarJob,
        salvarMapa,
        salvarTemplate,
    }
})
