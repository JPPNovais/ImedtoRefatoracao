import { defineStore } from "pinia"
import { ref } from "vue"

/**
 * Store do modal de upsell global.
 * Quando o httpClient recebe 402, chama abrir() com a mensagem do backend.
 * O modal é renderizado no App.vue e escuta este store.
 */
export const useUpsellStore = defineStore("upsell", () => {
    const visivel = ref(false)
    const mensagem = ref("")

    function abrir(msg: string) {
        mensagem.value = msg
        visivel.value = true
    }

    function fechar() {
        visivel.value = false
    }

    return { visivel, mensagem, abrir, fechar }
})
