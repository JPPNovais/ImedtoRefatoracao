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

interface EstabelecimentoListavel {
    id: number
    nomeFantasia: string
    papelDoUsuario: "Dono" | "Profissional"
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

    // true quando o usuário está autenticado e não tem nenhum estabelecimento vinculado.
    // Sinaliza para o guard redirecionar para MeusConvites em vez de tentar carregar.
    const semEstabelecimento = ref(false)

    const estabelecimentoAtivoId = computed(() => ativo.value?.id ?? null)
    const papel = computed(() => ativo.value?.papel ?? null)
    const temTenantSelecionado = computed(() => !!ativo.value)

    function selecionar(estab: EstabelecimentoAtivo) {
        const trocouEstab = ativo.value?.id !== estab.id
        ativo.value = estab
        semEstabelecimento.value = false
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(estab))
        if (trocouEstab) {
            void import("@/stores/assinaturaStore").then((m) => m.useAssinaturaStore().limpar())
        }
    }

    function limpar() {
        ativo.value = null
        semEstabelecimento.value = false
        sessionStorage.removeItem(STORAGE_KEY)
    }

    /**
     * Chamado no boot (main.ts) após auth.init().
     * Auto-seleciona o estabelecimento se possível:
     *   - Já tem em sessionStorage: mantém, retorna.
     *   - 1 estabelecimento: auto-seleciona.
     *   - 2+: auto-seleciona o primeiro (troca via seletor no header).
     *   - 0: seta semEstabelecimento = true.
     */
    async function resolverTenant(listar: () => Promise<EstabelecimentoListavel[]>) {
        if (ativo.value) return

        try {
            const lista = await listar()
            popularEstabelecimentos(lista)
        } catch {
            // Silencia: guard vai lidar com o estado sem tenant.
        }
    }

    /**
     * Variante síncrona de resolverTenant — recebe a lista já carregada (via
     * /auth/bootstrap) e aplica a mesma lógica de auto-seleção, sem fazer HTTP.
     */
    function popularEstabelecimentos(lista: EstabelecimentoListavel[]) {
        if (ativo.value) return
        if (lista.length === 0) {
            semEstabelecimento.value = true
            return
        }
        const primeiro = lista[0]
        selecionar({ id: primeiro.id, nomeFantasia: primeiro.nomeFantasia, papel: primeiro.papelDoUsuario })
    }

    return {
        ativo,
        semEstabelecimento,
        estabelecimentoAtivoId,
        papel,
        temTenantSelecionado,
        selecionar,
        limpar,
        resolverTenant,
        popularEstabelecimentos,
    }
})
