import { defineStore } from "pinia"
import { ref } from "vue"
import notificacaoService, { type Notificacao } from "@/services/notificacaoService"
import realtimeService from "@/services/realtimeService"

/**
 * Store do sino de notificações.
 *
 * Fontes de dado:
 *  - REST: GET /api/notificacoes (lista) + GET /api/notificacoes/contador-nao-lidas (badge).
 *  - Realtime: evento "notificacao-recebida" do hub SignalR — empurra novas notificações
 *    sem precisar de polling.
 *
 * O handler do hub é registrado uma única vez no setup do store. Mesmo que o componente
 * que usa o store seja montado/desmontado várias vezes, o handler persiste — o
 * realtimeService dedup conforme o Set interno e reaplica em reconexões.
 */
export const useNotificacoesStore = defineStore("notificacoes", () => {
    const notificacoes = ref<Notificacao[]>([])
    const naoLidas = ref(0)
    const carregando = ref(false)
    let handlerRegistrado = false

    async function carregar(params: { lidas?: boolean; pagina?: number; tamanho?: number } = {}) {
        carregando.value = true
        try {
            const pagina = await notificacaoService.listar({
                pagina: params.pagina ?? 1,
                tamanho: params.tamanho ?? 20,
                lidas: params.lidas,
            })
            notificacoes.value = pagina.itens
        } finally {
            carregando.value = false
        }
    }

    async function atualizarContador() {
        try {
            const r = await notificacaoService.contadorNaoLidas()
            naoLidas.value = r.total
        } catch {
            // Silencioso — badge zera apenas se chamada explícita do user falhar.
        }
    }

    async function marcarComoLida(id: number) {
        await notificacaoService.marcarLida(id)
        const item = notificacoes.value.find((n) => n.id === id)
        if (item && !item.lida) {
            item.lida = true
            item.lidaEm = new Date().toISOString()
            naoLidas.value = Math.max(0, naoLidas.value - 1)
        }
    }

    async function marcarTodasLidas() {
        await notificacaoService.marcarTodasLidas()
        const agora = new Date().toISOString()
        for (const n of notificacoes.value) {
            if (!n.lida) {
                n.lida = true
                n.lidaEm = agora
            }
        }
        naoLidas.value = 0
    }

    /**
     * Registra o handler do realtime — chamado pelo authStore após login/init bem-sucedido.
     * Idempotente: protegido por flag `handlerRegistrado`.
     */
    function bindRealtime() {
        if (handlerRegistrado) return
        handlerRegistrado = true
        realtimeService.on<Notificacao>("notificacao-recebida", (n) => {
            // Evita duplicar caso a notificação também tenha vindo via REST por race.
            if (notificacoes.value.some((existente) => existente.id === n.id)) return
            notificacoes.value.unshift(n)
            if (!n.lida) naoLidas.value += 1
        })
    }

    function limpar() {
        notificacoes.value = []
        naoLidas.value = 0
    }

    return {
        notificacoes,
        naoLidas,
        carregando,
        carregar,
        atualizarContador,
        marcarComoLida,
        marcarTodasLidas,
        bindRealtime,
        limpar,
    }
})
