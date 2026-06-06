import { http } from "@/lib/http"
import type { Orcamento } from "@/types"

export const orcamentoService = {
  async listarPorPaciente(pacienteId: number): Promise<Orcamento[]> {
    return http.get("/orcamentos", { pacienteId })
  },
  async obter(id: number): Promise<Orcamento> {
    return http.get(`/orcamentos/${id}`)
  },
  /** Requer permissão orcamento.aprovar — a UI já esconde o botão sem permissão (G2). */
  async aprovar(id: number): Promise<void> {
    await http.post(`/orcamentos/${id}/aprovar`)
  },
  async recusar(id: number): Promise<void> {
    await http.post(`/orcamentos/${id}/recusar`)
  },
}
