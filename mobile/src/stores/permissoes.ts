import { defineStore } from "pinia"
import { computed, ref } from "vue"
import type { Papel } from "@/types"

/** Espelho do permissoesStore do web: papel + permissões granulares "area.acao".
    Degradação por permissão (G2): a UI esconde o que o vínculo não permite. */
export const usePermissoesStore = defineStore("permissoes", () => {
  const papel = ref<Papel | null>(null)
  const permissoes = ref<string[]>([])
  const permissoesExtras = ref<string[]>([])

  const ehDono = computed(() => papel.value === "Dono")

  function definir(p: { papel: Papel; permissoes: string[]; permissoesExtras?: string[] }) {
    papel.value = p.papel
    permissoes.value = p.permissoes || []
    permissoesExtras.value = p.permissoesExtras || []
  }

  function limpar() {
    papel.value = null
    permissoes.value = []
    permissoesExtras.value = []
  }

  function pode(chave: string): boolean {
    if (!papel.value) return false
    if (ehDono.value) return true
    if (chave.includes(".")) {
      const [area, acao] = chave.split(".", 2)
      return (
        permissoes.value.includes(area) ||
        permissoes.value.includes(`${area}.${acao}`) ||
        permissoes.value.includes(`${area}.*`)
      )
    }
    return permissoes.value.some((p) => p === chave || p.startsWith(`${chave}.`))
  }

  function podeExtra(chave: string): boolean {
    if (!papel.value) return false
    if (ehDono.value) return true
    return permissoesExtras.value.includes(chave)
  }

  return { papel, permissoes, permissoesExtras, ehDono, definir, limpar, pode, podeExtra }
})
