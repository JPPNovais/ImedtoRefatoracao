import { ref } from "vue"
import { defineStore } from "pinia"
import {
    assinaturaDigitalService,
    type CertificadoVinculado,
    type StatusAssinaturaDigital,
} from "@/services/assinaturaDigitalService"

const POLLING_INTERVALO_MS = 4_000
const POLLING_TIMEOUT_MS = 5 * 60 * 1_000 // 5 min

/**
 * Store Pinia para gerenciar assinatura digital de receitas.
 * - certificadoVinculado: metadados do certificado do médico (sem token).
 * - statusPorReceita: cache de status atual de cada receita.
 * - polling: intervalo de 4s, para após resolução ou timeout de 5 min.
 */
export const useAssinaturaDigitalStore = defineStore("assinatura-digital", () => {
    const certificadoVinculado = ref<CertificadoVinculado | null>(null)
    const statusPorReceita = ref<Record<string, StatusAssinaturaDigital>>({})
    const pdfUrlPorReceita = ref<Record<string, string>>({})

    // Controle de polling por receita_id → intervalId
    const pollingTimers = new Map<string, ReturnType<typeof setInterval>>()
    const pollingTimeouts = new Map<string, ReturnType<typeof setTimeout>>()

    // Callbacks chamados quando polling resolve (pa/ notificar a UI)
    const onPollingResolvido = new Map<string, (status: StatusAssinaturaDigital) => void>()

    async function carregarCertificado() {
        try {
            certificadoVinculado.value = await assinaturaDigitalService.obterCertificadoVinculado()
        } catch {
            certificadoVinculado.value = null
        }
    }

    function setStatus(receitaId: string, status: StatusAssinaturaDigital, pdfUrl?: string | null) {
        statusPorReceita.value[receitaId] = status
        if (pdfUrl) {
            pdfUrlPorReceita.value[receitaId] = pdfUrl
        }
    }

    /** Status resolvido = não mais pendente. Para o polling. */
    function estaResolvido(status: StatusAssinaturaDigital): boolean {
        return status === "AssinadaIcp" || status === "FalhaAssinatura" || status === "AssinaturaExpirada"
    }

    function pararPolling(receitaId: string) {
        const timer = pollingTimers.get(receitaId)
        if (timer) clearInterval(timer)
        pollingTimers.delete(receitaId)

        const timeout = pollingTimeouts.get(receitaId)
        if (timeout) clearTimeout(timeout)
        pollingTimeouts.delete(receitaId)
    }

    /**
     * Inicia polling de 4s para a receita.
     * Para automaticamente quando status resolve ou após 5 min.
     * @param onResolucao callback chamado quando status muda de Pendente para algo final.
     * @param onTimeout callback chamado quando 5 min expiram sem resolução.
     */
    function iniciarPolling(
        receitaId: string,
        onResolucao?: (status: StatusAssinaturaDigital) => void,
        onTimeout?: () => void,
    ) {
        // Para polling anterior da mesma receita (re-disparo).
        pararPolling(receitaId)

        if (onResolucao) onPollingResolvido.set(receitaId, onResolucao)

        const timer = setInterval(async () => {
            try {
                const res = await assinaturaDigitalService.obterStatus(Number(receitaId))
                setStatus(receitaId, res.status, res.pdfAssinadoUrl)

                if (estaResolvido(res.status)) {
                    pararPolling(receitaId)
                    onPollingResolvido.get(receitaId)?.(res.status)
                    onPollingResolvido.delete(receitaId)
                }
            } catch {
                // Falha de rede no polling → ignora e tenta no próximo tick.
            }
        }, POLLING_INTERVALO_MS)

        pollingTimers.set(receitaId, timer)

        // Timeout de 5 min — para o polling e notifica a UI.
        const timeout = setTimeout(() => {
            pararPolling(receitaId)
            onTimeout?.()
        }, POLLING_TIMEOUT_MS)

        pollingTimeouts.set(receitaId, timeout)
    }

    function limparTudo() {
        pollingTimers.forEach((_, id) => pararPolling(id))
        certificadoVinculado.value = null
        statusPorReceita.value = {}
        pdfUrlPorReceita.value = {}
    }

    return {
        certificadoVinculado,
        statusPorReceita,
        pdfUrlPorReceita,
        carregarCertificado,
        setStatus,
        iniciarPolling,
        pararPolling,
        limparTudo,
    }
})
