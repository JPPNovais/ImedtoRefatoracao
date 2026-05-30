import { defineStore } from "pinia"
import { ref } from "vue"
import { assinaturasService, type AssinaturaAdminDto } from "../services/assinaturasService"

export const useAssinaturasStore = defineStore("adminAssinaturas", () => {
    const historico = ref<AssinaturaAdminDto[]>([])
    const carregando = ref(false)
    const erro = ref<string | null>(null)

    async function carregarHistorico(estabelecimentoId: number): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            historico.value = await assinaturasService.listarHistorico(estabelecimentoId)
        } catch {
            erro.value = "Não foi possível carregar o histórico de assinaturas."
        } finally {
            carregando.value = false
        }
    }

    async function trocarPlano(
        estabelecimentoId: number,
        payload: {
            planoId: string
            inicio: string
            fimEm?: string | null
            motivo: string
        },
    ): Promise<void> {
        await assinaturasService.trocarPlano(estabelecimentoId, payload)
        await carregarHistorico(estabelecimentoId)
    }

    async function concederGratuidade(
        estabelecimentoId: number,
        payload: {
            gratuidadeMotivo: string
            fimEm?: string | null
            motivo: string
        },
    ): Promise<void> {
        await assinaturasService.concederGratuidade(estabelecimentoId, payload)
        await carregarHistorico(estabelecimentoId)
    }

    async function encerrar(assinaturaId: string, estabelecimentoId: number, motivo: string): Promise<void> {
        await assinaturasService.encerrar(assinaturaId, motivo)
        await carregarHistorico(estabelecimentoId)
    }

    function vigente(): AssinaturaAdminDto | undefined {
        return historico.value.find((a) => a.vigente)
    }

    function limpar(): void {
        historico.value = []
        erro.value = null
    }

    return {
        historico,
        carregando,
        erro,
        carregarHistorico,
        trocarPlano,
        concederGratuidade,
        encerrar,
        vigente,
        limpar,
    }
})
