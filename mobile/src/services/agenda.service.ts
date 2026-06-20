import { http } from "@/lib/http"
import type { Agendamento, ContagemPorDia, DisponibilidadeSemanaDto, PaginaAgendamentos } from "@/types"

export interface CriarAgendamentoPayload {
  pacienteId: number
  profissionalUsuarioId: string
  inicioPrevisto: string
  fimPrevisto: string
  tipoServico: string
  observacoes?: string
  salaId?: number
}

export const agendaService = {
  async listar(params: {
    dataInicio?: string
    dataFim?: string
    profissionalUsuarioId?: string
    status?: string
    pagina?: number
    tamanho?: number
  }): Promise<PaginaAgendamentos> {
    return http.get("/agendamentos", params)
  },
  async contagemPorDia(dataInicio: string, dataFim: string): Promise<ContagemPorDia[]> {
    return http.get("/agendamentos/contagem-por-dia", { dataInicio, dataFim })
  },
  async obter(id: number): Promise<Agendamento> {
    return http.get(`/agendamentos/${id}`)
  },
  async criar(payload: CriarAgendamentoPayload): Promise<{ agendamentoId: number }> {
    return http.post("/agendamentos", payload)
  },
  async confirmar(id: number): Promise<void> {
    await http.post(`/agendamentos/${id}/confirmar`)
  },
  async concluir(id: number): Promise<void> {
    await http.post(`/agendamentos/${id}/concluir`)
  },
  async cancelar(id: number, motivo: string): Promise<void> {
    await http.post(`/agendamentos/${id}/cancelar`, { motivo })
  },
  async checkin(
    id: number,
    payload: { salaId?: number; tipoAtendimento?: string; valorCobrado?: number },
  ): Promise<void> {
    await http.post(`/agendamentos/${id}/checkin`, payload)
  },
  async disponibilidade(params: {
    profissionalUsuarioId: string
    dataInicio: string
    dataFim: string
    duracaoMinutos?: number
  }): Promise<DisponibilidadeSemanaDto> {
    return http.get("/agendamentos/disponibilidade", params)
  },
}
