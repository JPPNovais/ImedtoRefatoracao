import httpClient from "@/services/httpClient"

/**
 * Categoria espelhada do enum CategoriaNotificacao do backend.
 * Mantemos como union string para o type-checker pegar typos sem precisar
 * importar enum runtime.
 */
export type CategoriaNotificacao =
    | "Convite"
    | "Agenda"
    | "Financeiro"
    | "Sistema"
    | "Automacao"

export interface Notificacao {
    id: number
    estabelecimentoId: number | null
    titulo: string
    mensagem: string
    categoria: CategoriaNotificacao | string
    linkAcao: string | null
    lida: boolean
    criadaEm: string
    lidaEm: string | null
}

export interface PaginaNotificacoes {
    total: number
    pagina: number
    tamanho: number
    itens: Notificacao[]
}

export interface ContadorNaoLidas {
    total: number
}

/**
 * HTTP do domínio de notificações. Toda chamada passa pelo httpClient (BFF + cookie).
 * Push em tempo real chega via realtimeService — o store junta as duas fontes.
 */
export const notificacaoService = {
    listar(params: { lidas?: boolean; pagina?: number; tamanho?: number } = {}) {
        return httpClient
            .get<PaginaNotificacoes>("/notificacoes", { params })
            .then((r) => r.data)
    },

    contadorNaoLidas() {
        return httpClient
            .get<ContadorNaoLidas>("/notificacoes/contador-nao-lidas")
            .then((r) => r.data)
    },

    marcarLida(id: number) {
        return httpClient.post(`/notificacoes/${id}/marcar-lida`).then(() => undefined)
    },

    marcarTodasLidas() {
        return httpClient.post("/notificacoes/marcar-todas-lidas").then(() => undefined)
    },
}

export default notificacaoService
