import { defineStore } from "pinia"
import { ref, computed } from "vue"

/**
 * Store de tenant — guarda o estabelecimento ativo escolhido pelo usuário.
 * Persistido em sessionStorage para sobreviver a refresh mas não ao fechar o navegador.
 */
const STORAGE_KEY = "imedto.estabelecimentoAtivo"

export interface EstabelecimentoAtivo {
    id: number
    nomeFantasia: string
    papel: "Dono" | "Profissional"
}

function carregar(): EstabelecimentoAtivo | null {
    try {
        const raw = sessionStorage.getItem(STORAGE_KEY)
        return raw ? (JSON.parse(raw) as EstabelecimentoAtivo) : null
    } catch {
        return null
    }
}

export const useTenantStore = defineStore("tenant", () => {
    const ativo = ref<EstabelecimentoAtivo | null>(carregar())

    const estabelecimentoAtivoId = computed(() => ativo.value?.id ?? null)
    const papel = computed(() => ativo.value?.papel ?? null)
    const temTenantSelecionado = computed(() => !!ativo.value)

    function selecionar(estab: EstabelecimentoAtivo) {
        ativo.value = estab
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(estab))
    }

    function limpar() {
        ativo.value = null
        sessionStorage.removeItem(STORAGE_KEY)
    }

    return {
        ativo,
        estabelecimentoAtivoId,
        papel,
        temTenantSelecionado,
        selecionar,
        limpar,
    }
})
