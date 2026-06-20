import { http } from "@/lib/http"
import type { DadosSensiveisPaciente, Paciente, PaginaPacientes, PacientePayloadRapido } from "@/types"

export const pacienteService = {
  async listar(busca?: string, pagina = 1, tamanho = 20): Promise<PaginaPacientes> {
    return http.get("/paciente", { busca, pagina, tamanho })
  },
  async buscaRapida(q?: string, limite = 10): Promise<{ id: number; nomeCompleto: string }[]> {
    return http.get("/paciente/busca-rapida", { q, limite })
  },
  /**
   * Abre a ficha do paciente com CPF e telefone já mascarados pelo backend
   * (ex.: "•••.•••.•••-09" / "(••) •••••-1234"). Dispara PacienteAcessoLog.
   * Nunca use sem o param contato=mascarado no mobile — PII completa não sai para o device.
   */
  async obter(id: number): Promise<Paciente> {
    return http.get(`/paciente/${id}`, { contato: "mascarado" })
  },
  /**
   * Revelação auditada de CPF e telefone completos (LGPD).
   * Só chame após biometria confirmada. Registra trilha RevelacaoDadosSensiveis no backend.
   */
  async obterDadosSensiveis(id: number): Promise<DadosSensiveisPaciente> {
    return http.get(`/paciente/${id}/dados-sensiveis`)
  },
  /**
   * Cria um novo paciente no estabelecimento ativo.
   * Backend: POST /api/paciente → 201 Created (sem body de retorno).
   * Exige papel Profissional ou Dono ([RequiresPapel]).
   * Validação real vive no backend — 422 é a fonte de verdade.
   */
  async criar(payload: PacientePayloadRapido): Promise<void> {
    return http.post("/paciente", payload)
  },
  /**
   * Atualiza dados básicos de um paciente existente.
   * Backend: PUT /api/paciente/{id} → 204 No Content.
   * Exige papel Profissional ou Dono ([RequiresPapel]).
   * Enviar apenas os campos que o formulário expõe (LGPD: minimização).
   */
  async atualizar(id: number, payload: PacientePayloadRapido): Promise<void> {
    return http.put(`/paciente/${id}`, payload)
  },
}
