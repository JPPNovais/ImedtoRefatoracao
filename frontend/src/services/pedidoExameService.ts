import httpClient from "./httpClient"

/**
 * Pedidos de exame emitidos pelo profissional. Backend é fonte da verdade.
 */

export type TipoPedidoExame = "Laboratorial" | "Imagem" | "Misto"

export interface PedidoExame {
    id: number
    pacienteId: number
    profissionalUsuarioId: string
    profissionalNome: string | null
    tipo: TipoPedidoExame
    exames: string[]
    indicacaoClinica: string
    cid10: string | null
    observacoes: string | null
    criadoEm: string
}

export interface EmitirPedidoExameInput {
    tipo: TipoPedidoExame
    exames: string[]
    indicacaoClinica: string
    cid10?: string | null
    observacoes?: string | null
}

export interface PaginaPedidosExame {
    itens: PedidoExame[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export const pedidoExameService = {
    async emitir(pacienteId: number, input: EmitirPedidoExameInput): Promise<{ pedidoExameId: number }> {
        const { data } = await httpClient.post<{ pedidoExameId: number }>(
            `/pacientes/${pacienteId}/pedidos-exame`,
            input,
        )
        return data
    },

    async listarDoPaciente(
        pacienteId: number,
        params: { pagina?: number; tamanho?: number } = {},
    ): Promise<PaginaPedidosExame> {
        const { data } = await httpClient.get<PaginaPedidosExame>(
            `/pacientes/${pacienteId}/pedidos-exame`,
            { params: { pagina: params.pagina ?? 1, tamanho: params.tamanho ?? 10 } },
        )
        return data
    },

    async obter(id: number): Promise<PedidoExame> {
        const { data } = await httpClient.get<PedidoExame>(`/pedidos-exame/${id}`)
        return data
    },
}
