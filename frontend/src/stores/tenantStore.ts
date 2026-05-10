import { defineStore } from "pinia"
import { ref, computed } from "vue"
import { usePermissoesStore } from "@/stores/permissoesStore"

/**
 * Store de tenant — guarda o estabelecimento ativo escolhido pelo usuário.
 * Persistido em sessionStorage para sobreviver a refresh mas não ao fechar o navegador.
 */
const STORAGE_KEY = "imedto.estabelecimentoAtivo"

export interface EstabelecimentoAtivo {
    id: number
    nomeFantasia: string
    papel: "Dono" | "Profissional"
    /** Permissões granulares ("area.acao"). Vazio para Dono = "tudo". */
    permissoes: string[]
    /** Permissões finas extras. Vazio para Dono = "tudo". */
    permissoesExtras: string[]
}

interface EstabelecimentoListavel {
    id: number
    nomeFantasia: string
    papelDoUsuario: "Dono" | "Profissional"
    permissoes?: string[]
    permissoesExtras?: string[]
}

function carregar(): EstabelecimentoAtivo | null {
    try {
        const raw = sessionStorage.getItem(STORAGE_KEY)
        if (!raw) return null
        const parsed = JSON.parse(raw) as Partial<EstabelecimentoAtivo>
        // Backwards-compat: registros antigos no sessionStorage não têm os arrays.
        return {
            id: parsed.id!,
            nomeFantasia: parsed.nomeFantasia!,
            papel: parsed.papel!,
            permissoes: parsed.permissoes ?? [],
            permissoesExtras: parsed.permissoesExtras ?? [],
        }
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
        usePermissoesStore().definir({
            papel: estab.papel,
            permissoes: estab.permissoes,
            permissoesExtras: estab.permissoesExtras,
        })
        if (trocouEstab) {
            void import("@/stores/assinaturaStore").then((m) => m.useAssinaturaStore().limpar())
        }
    }

    function limpar() {
        ativo.value = null
        semEstabelecimento.value = false
        sessionStorage.removeItem(STORAGE_KEY)
        usePermissoesStore().limpar()
    }

    /**
     * Atualiza apenas as permissões do tenant ativo (sem trocar de tenant).
     * Usado após `permissoesStore.revalidar()` para que o sessionStorage reflita
     * as novas permissões ao recarregar a página.
     */
    function atualizarPermissoesAtivo(p: { permissoes: string[]; permissoesExtras: string[] }) {
        if (!ativo.value) return
        ativo.value = {
            ...ativo.value,
            permissoes: p.permissoes,
            permissoesExtras: p.permissoesExtras,
        }
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(ativo.value))
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
        if (ativo.value) {
            // Já tem tenant em memória (sessionStorage). Re-hidrata o permissoesStore
            // a partir da lista atual — o sessionStorage pode estar desatualizado se as
            // permissões mudaram desde o último login.
            const match = lista.find(e => e.id === ativo.value!.id)
            if (match) {
                ativo.value = {
                    ...ativo.value,
                    permissoes: match.permissoes ?? [],
                    permissoesExtras: match.permissoesExtras ?? [],
                }
                sessionStorage.setItem(STORAGE_KEY, JSON.stringify(ativo.value))
                usePermissoesStore().definir({
                    papel: ativo.value.papel,
                    permissoes: ativo.value.permissoes,
                    permissoesExtras: ativo.value.permissoesExtras,
                })
            } else {
                // Tenant em sessionStorage não está mais na lista (vínculo removido) — limpa.
                limpar()
                if (lista.length === 0) {
                    semEstabelecimento.value = true
                    return
                }
                const primeiro = lista[0]
                selecionar({
                    id: primeiro.id,
                    nomeFantasia: primeiro.nomeFantasia,
                    papel: primeiro.papelDoUsuario,
                    permissoes: primeiro.permissoes ?? [],
                    permissoesExtras: primeiro.permissoesExtras ?? [],
                })
            }
            return
        }
        if (lista.length === 0) {
            semEstabelecimento.value = true
            usePermissoesStore().limpar()
            return
        }
        const primeiro = lista[0]
        selecionar({
            id: primeiro.id,
            nomeFantasia: primeiro.nomeFantasia,
            papel: primeiro.papelDoUsuario,
            permissoes: primeiro.permissoes ?? [],
            permissoesExtras: primeiro.permissoesExtras ?? [],
        })
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
        atualizarPermissoesAtivo,
    }
})
