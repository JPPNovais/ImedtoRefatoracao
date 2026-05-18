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

export const pedidoExameService = {
    async emitir(pacienteId: number, input: EmitirPedidoExameInput): Promise<{ pedidoExameId: number }> {
        const { data } = await httpClient.post<{ pedidoExameId: number }>(
            `/pacientes/${pacienteId}/pedidos-exame`,
            input,
        )
        return data
    },

    async listarDoPaciente(pacienteId: number): Promise<PedidoExame[]> {
        const { data } = await httpClient.get<PedidoExame[]>(`/pacientes/${pacienteId}/pedidos-exame`)
        return data
    },

    async obter(id: number): Promise<PedidoExame> {
        const { data } = await httpClient.get<PedidoExame>(`/pedidos-exame/${id}`)
        return data
    },
}
