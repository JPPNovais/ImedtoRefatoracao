import httpClient from "./httpClient"

// ─── Disponibilidade ──────────────────────────────────────────────────────────

export interface DisponibilidadeSlot {
    hora: string
    disponivel: boolean
    motivo: "agendado" | "bloqueado" | "passado" | null
    pacienteNome: string | null
}

export interface DisponibilidadeDia {
    data: string        // "YYYY-MM-DD"
    diaSemana: string   // "DOM" | "SEG" | ...
    status: "fechado" | "disponivel" | "indisponivel"
    slots: DisponibilidadeSlot[]
}

export interface DisponibilidadeSemana {
    profissionalUsuarioId: string
    dias: DisponibilidadeDia[]
}

export interface Agendamento {
    id: number
    estabelecimentoId: number
    pacienteId: number
    pacienteNome: string
    profissionalUsuarioId: string
    profissionalNome: string
    criadoPorNome: string
    inicioPrevisto: string
    fimPrevisto: string
    tipoServico: string
    observacoes: string | null
    status: "Agendado" | "Confirmado" | "Cancelado" | "Concluido"
    motivoCancelamento: string | null
    criadoEm: string
    atualizadoEm: string | null
}

export interface CriarAgendamentoPayload {
    pacienteId: number
    profissionalUsuarioId: string
    inicioPrevisto: string
    fimPrevisto: string
    tipoServico: string
    observacoes?: string | null
}

export interface AtualizarAgendamentoPayload {
    profissionalUsuarioId: string
    inicioPrevisto: string
    fimPrevisto: string
    tipoServico: string
    observacoes?: string | null
}

export const agendaService = {
    async listar(params?: {
        dataInicio?: string
        dataFim?: string
        profissionalUsuarioId?: string
        pacienteId?: number
        status?: string
    }): Promise<Agendamento[]> {
        const { data } = await httpClient.get<Agendamento[]>("/agendamentos", { params })
        return data
    },

    async contarPorDia(params: {
        dataInicio: string
        dataFim: string
        profissionalUsuarioId?: string
    }): Promise<{ data: string; total: number }[]> {
        const { data } = await httpClient.get<{ data: string; total: number }[]>(
            "/agendamentos/contagem-por-dia", { params })
        return data
    },

    async obter(id: number): Promise<Agendamento> {
        const { data } = await httpClient.get<Agendamento>(`/agendamentos/${id}`)
        return data
    },

    async criar(payload: CriarAgendamentoPayload): Promise<{ agendamentoId: number }> {
        const { data } = await httpClient.post<{ agendamentoId: number }>("/agendamentos", payload)
        return data
    },

    async atualizar(id: number, payload: AtualizarAgendamentoPayload): Promise<void> {
        await httpClient.put(`/agendamentos/${id}`, payload)
    },

    async confirmar(id: number): Promise<void> {
        await httpClient.post(`/agendamentos/${id}/confirmar`)
    },

    async cancelar(id: number, motivo: string): Promise<void> {
        await httpClient.post(`/agendamentos/${id}/cancelar`, { motivo })
    },

    async concluir(id: number): Promise<void> {
        await httpClient.post(`/agendamentos/${id}/concluir`)
    },

    async consultarDisponibilidade(
        profissionalUsuarioId: string,
        dataInicio: string,
        dataFim: string,
    ): Promise<DisponibilidadeSemana> {
        const { data } = await httpClient.get<DisponibilidadeSemana>("/agendamentos/disponibilidade", {
            params: { profissionalUsuarioId, dataInicio, dataFim },
        })
        return data
    },
}
