import { ref, watch, type Ref } from "vue"
import {
    orcamentoService,
    type PreviewOrcamento,
    type PreviewOrcamentoPayload,
} from "@/services/orcamentoService"

/**
 * Espelha em servidor o cálculo do orçamento em construção. Recebe um ref
 * reativo do payload e devolve o `PreviewOrcamento` calculado pelo backend
 * (fonte da verdade), com debounce de 250ms.
 *
 * Cancela requisições antigas via AbortController — se o usuário continua
 * editando, só a última request retornada é aplicada.
 */
export function usePreviewOrcamento(payload: Ref<PreviewOrcamentoPayload>) {
    const preview = ref<PreviewOrcamento | null>(null)
    const carregando = ref(false)
    const erro = ref<string | null>(null)

    let timer: ReturnType<typeof setTimeout> | null = null
    let abortAtual: AbortController | null = null

    async function executar() {
        abortAtual?.abort()
        const ctrl = new AbortController()
        abortAtual = ctrl
        carregando.value = true
        erro.value = null
        try {
            const r = await orcamentoService.preview(payload.value)
            if (!ctrl.signal.aborted) preview.value = r
        } catch (e: any) {
            if (e?.name !== "CanceledError" && e?.name !== "AbortError") {
                erro.value = e?.response?.data?.mensagem ?? "Erro no preview."
            }
        } finally {
            if (!ctrl.signal.aborted) carregando.value = false
        }
    }

    watch(payload, () => {
        if (timer) clearTimeout(timer)
        timer = setTimeout(executar, 250)
    }, { deep: true, immediate: true })

    return { preview, carregando, erro }
}
