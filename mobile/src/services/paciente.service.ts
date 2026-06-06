import { http } from "@/lib/http"
import type { Paciente, PaginaPacientes } from "@/types"

export const pacienteService = {
  async listar(busca?: string, pagina = 1, tamanho = 20): Promise<PaginaPacientes> {
    return http.get("/paciente", { busca, pagina, tamanho })
  },
  async buscaRapida(q?: string, limite = 10): Promise<{ id: number; nomeCompleto: string }[]> {
    return http.get("/paciente/busca-rapida", { q, limite })
  },
  /** Abrir o detalhe dispara o log de acesso no backend (PacienteAcessoLog). */
  async obter(id: number): Promise<Paciente> {
    return http.get(`/paciente/${id}`)
  },
}
