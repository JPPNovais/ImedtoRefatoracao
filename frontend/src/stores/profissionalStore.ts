import { defineStore } from "pinia"
import { ref } from "vue"
import { profissionalService } from "@/services/profissionalService"

/**
 * Estado reativo do perfil profissional do usuário logado.
 * Usado pelo sidebar (avatar/foto) e pela tela "Minha conta" (formulário).
 *
 * Carregado uma vez no bootstrap (após o auth.init resolver) e atualizado
 * sempre que a foto for trocada — assim o sidebar reage na hora.
 */
export const useProfissionalStore = defineStore("profissional", () => {
    const fotoUrl = ref<string | null>(null)
    const existe  = ref(false)
    const carregado = ref(false)

    async function init() {
        try {
            const p = await profissionalService.obterMeu()
            fotoUrl.value = p?.fotoUrl ?? null
            existe.value  = !!p
        } catch {
            fotoUrl.value = null
            existe.value  = false
        } finally {
            carregado.value = true
        }
    }

    function setFotoUrl(url: string | null) {
        fotoUrl.value = url
    }

    function marcarComoExistente() {
        existe.value = true
    }

    function limpar() {
        fotoUrl.value = null
        existe.value  = false
        carregado.value = false
    }

    return {
        fotoUrl,
        existe,
        carregado,
        init,
        setFotoUrl,
        marcarComoExistente,
        limpar,
    }
})
