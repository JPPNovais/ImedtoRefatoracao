import { defineStore } from "pinia"
import { computed, ref } from "vue"
import { Preferences } from "@capacitor/preferences"
import type { Estabelecimento } from "@/types"
import { usePermissoesStore } from "./permissoes"

const STORAGE_KEY = "imedto.estabelecimentoAtivo"

/** Multi-tenant cidadão de primeira classe (§4 do brief): o switcher troca o
    tenant ativo e recarrega o contexto. Persistido entre sessões. */
export const useTenantStore = defineStore("tenant", () => {
  const estabelecimentos = ref<Estabelecimento[]>([])
  const ativo = ref<Estabelecimento | null>(null)

  const estabelecimentoAtivoId = computed(() => ativo.value?.id ?? null)
  const temTenantSelecionado = computed(() => !!ativo.value)
  const semEstabelecimento = computed(() => estabelecimentos.value.length === 0)
  const papel = computed(() => ativo.value?.papelDoUsuario ?? null)

  function aplicarPermissoes(e: Estabelecimento | null) {
    const permissoes = usePermissoesStore()
    if (e) permissoes.definir({ papel: e.papelDoUsuario, permissoes: e.permissoes, permissoesExtras: e.permissoesExtras })
    else permissoes.limpar()
  }

  async function selecionar(e: Estabelecimento) {
    ativo.value = e
    aplicarPermissoes(e)
    await Preferences.set({ key: STORAGE_KEY, value: JSON.stringify({ id: e.id }) })
  }

  /** Popula a lista e resolve o ativo: persistido → último do servidor → primeiro. */
  async function popular(lista: Estabelecimento[], ultimoEstabelecimentoId?: number | null) {
    estabelecimentos.value = lista
    if (!lista.length) {
      ativo.value = null
      aplicarPermissoes(null)
      return { usouFallback: false }
    }
    const { value } = await Preferences.get({ key: STORAGE_KEY })
    const persistidoId = value ? (JSON.parse(value).id as number) : null

    let escolhido =
      lista.find((e) => e.id === persistidoId) ||
      lista.find((e) => e.id === ultimoEstabelecimentoId) ||
      null
    const usouFallback = !escolhido
    if (!escolhido && lista.length === 1) escolhido = lista[0]

    if (escolhido) {
      ativo.value = escolhido
      aplicarPermissoes(escolhido)
      await Preferences.set({ key: STORAGE_KEY, value: JSON.stringify({ id: escolhido.id }) })
    }
    return { usouFallback }
  }

  async function limpar() {
    estabelecimentos.value = []
    ativo.value = null
    aplicarPermissoes(null)
    await Preferences.remove({ key: STORAGE_KEY })
  }

  return {
    estabelecimentos,
    ativo,
    estabelecimentoAtivoId,
    temTenantSelecionado,
    semEstabelecimento,
    papel,
    selecionar,
    popular,
    limpar,
  }
})
