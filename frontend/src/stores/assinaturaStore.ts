import { computed, ref } from "vue"
import { defineStore } from "pinia"
import { assinaturaService, type MinhaAssinatura, type StatusAssinatura } from "@/services/assinaturaService"

/**
 * Store global da assinatura do estabelecimento ativo.
 *
 * Espelha o comportamento do `subscription.ts` legado:
 * - `ensureLoaded()` carrega 1x antes de cada navegação (router guard).
 * - `isBlocked` = trial expirado / suspensa / cancelada / expirada / sem assinatura.
 *   Quando true, o router redireciona para /assinatura-expirada.
 * - `hasFeatureAccess(feature)` é fail-open enquanto não carregou (backend bloqueia se necessário).
 *
 * Status efetivo: re-avalia trial expirado lazy (caso o job de expirar trials ainda não
 * tenha rodado e o banco diga "Trial" mas a data já passou).
 */
export const useAssinaturaStore = defineStore("assinatura", () => {
    const assinatura = ref<MinhaAssinatura | null>(null)
    const carregada = ref(false)
    const carregando = ref(false)

    const isTrialExpiradoAgora = computed(() => {
        if (!assinatura.value) return false
        if (assinatura.value.status !== "Trial") return false
        if (assinatura.value.expiraEm == null) return false
        return new Date(assinatura.value.expiraEm) < new Date()
    })

    const statusEfetivo = computed<StatusAssinatura | null>(() => {
        if (!assinatura.value) return null
        if (isTrialExpiradoAgora.value) return "Expirada"
        return assinatura.value.status
    })

    const isAtiva = computed(() => {
        const s = statusEfetivo.value
        return s === "Trial" || s === "Ativa"
    })

    const isBlocked = computed(() => {
        const s = statusEfetivo.value
        return s === "Expirada" || s === "Suspensa" || s === "Cancelada"
    })

    const diasRestantesTrial = computed(() => {
        if (statusEfetivo.value !== "Trial") return 0
        return assinatura.value?.diasRestantes ?? 0
    })

    async function carregar() {
        carregando.value = true
        try {
            assinatura.value = await assinaturaService.obterMinha()
            carregada.value = true
        } catch (err) {
            console.warn("[assinatura] Falha ao carregar assinatura.", err)
            assinatura.value = null
            carregada.value = true
        } finally {
            carregando.value = false
        }
    }

    async function ensureLoaded() {
        if (carregada.value || carregando.value) return
        await carregar()
    }

    async function recarregar() {
        carregada.value = false
        assinatura.value = null
        await carregar()
    }

    function hasFeatureAccess(feature: string | null): boolean {
        if (!feature) return true
        // Fail-open enquanto não carregou — backend é fonte da verdade via 402.
        if (!carregada.value) return true
        if (isBlocked.value) return false
        return assinatura.value?.features.includes(feature) ?? false
    }

    function limpar() {
        assinatura.value = null
        carregada.value = false
        carregando.value = false
    }

    return {
        assinatura,
        carregada,
        carregando,
        isAtiva,
        isBlocked,
        isTrialExpiradoAgora,
        statusEfetivo,
        diasRestantesTrial,
        carregar,
        ensureLoaded,
        recarregar,
        hasFeatureAccess,
        limpar,
    }
})
