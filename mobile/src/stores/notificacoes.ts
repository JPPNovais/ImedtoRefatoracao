import { defineStore } from "pinia"
import { computed, ref } from "vue"
import type { Notificacao } from "@/types"
import { notificacaoService } from "@/services/notificacao.service"

export const useNotificacoesStore = defineStore("notificacoes", () => {
  const notificacoes = ref<Notificacao[]>([])
  const naoLidas = ref(0)
  const carregando = ref(false)

  const temNaoLidas = computed(() => naoLidas.value > 0)

  async function carregar() {
    carregando.value = true
    try {
      const pagina = await notificacaoService.listar({ pagina: 1, tamanho: 50 })
      notificacoes.value = pagina.itens
      naoLidas.value = pagina.itens.filter((n) => !n.lida).length
    } finally {
      carregando.value = false
    }
  }

  async function atualizarContador() {
    try {
      const { total } = await notificacaoService.contadorNaoLidas()
      naoLidas.value = total
    } catch {
      /* offline: mantém o contador atual */
    }
  }

  async function marcarLida(id: number) {
    const n = notificacoes.value.find((x) => x.id === id)
    if (n && !n.lida) {
      n.lida = true
      naoLidas.value = Math.max(0, naoLidas.value - 1)
      void notificacaoService.marcarLida(id)
    }
  }

  async function marcarTodasLidas() {
    notificacoes.value.forEach((n) => (n.lida = true))
    naoLidas.value = 0
    await notificacaoService.marcarTodasLidas()
  }

  /** Push em foreground insere a notificação no topo (deep-link no corpo). */
  function receberPush(n: Notificacao) {
    if (notificacoes.value.some((x) => x.id === n.id)) return
    notificacoes.value.unshift(n)
    if (!n.lida) naoLidas.value += 1
  }

  function limpar() {
    notificacoes.value = []
    naoLidas.value = 0
  }

  return {
    notificacoes,
    naoLidas,
    carregando,
    temNaoLidas,
    carregar,
    atualizarContador,
    marcarLida,
    marcarTodasLidas,
    receberPush,
    limpar,
  }
})
