import { ref, computed } from "vue"

/** Tamanho de página padrão — cabe bem em tela mobile sem degradar performance. */
const TAMANHO_PADRAO = 20

/**
 * Composable de paginação por "Carregar mais" para listas que consomem
 * endpoints paginados (retornam { itens, total }).
 *
 * Uso:
 *   const lista = useListaPaginada(
 *     (pagina, tamanho) => meuService.listar({ pagina, tamanho }),
 *     { tamanho: 20 }
 *   )
 *   await lista.recarregar()   // carga inicial ou ao mudar filtros
 *   await lista.carregarMais() // append da próxima página
 */
export function useListaPaginada<T>(
  buscarPagina: (pagina: number, tamanho: number) => Promise<{ itens: T[]; total: number }>,
  opcoes?: { tamanho?: number },
) {
  const tamanho = opcoes?.tamanho ?? TAMANHO_PADRAO

  const itens = ref<T[]>([])
  const total = ref(0)
  const paginaAtual = ref(1)
  const carregando = ref(false)      // 1ª carga (skeleton)
  const carregandoMais = ref(false)  // append (loading no botão)

  // O botão some quando já carregamos todos os itens
  const temMais = computed(() => itens.value.length < total.value)

  /** Carga inicial ou reset por mudança de filtro/busca. Substitui itens. */
  async function recarregar(): Promise<void> {
    carregando.value = true
    paginaAtual.value = 1
    try {
      const res = await buscarPagina(1, tamanho)
      // Substitui — não faz append (filtro/busca mudou)
      itens.value = res.itens as T[]
      total.value = res.total
    } finally {
      carregando.value = false
    }
  }

  /** Busca a próxima página e faz append. Chamado pelo botão "Carregar mais". */
  async function carregarMais(): Promise<void> {
    if (!temMais.value || carregandoMais.value) return
    carregandoMais.value = true
    const proximaPagina = paginaAtual.value + 1
    try {
      const res = await buscarPagina(proximaPagina, tamanho)
      // Append — preserva o que já está na lista
      ;(itens.value as T[]).push(...(res.itens as T[]))
      total.value = res.total
      paginaAtual.value = proximaPagina
    } finally {
      carregandoMais.value = false
    }
  }

  return {
    itens,
    total,
    carregando,
    carregandoMais,
    temMais,
    recarregar,
    carregarMais,
  }
}
