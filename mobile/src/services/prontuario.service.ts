import { http } from "@/lib/http"
import { API_BASE } from "@/lib/config"
import type { ProntuarioCompleto } from "@/types"

export const prontuarioService = {
  async obter(pacienteId: number, timeline = 50): Promise<ProntuarioCompleto> {
    return http.get(`/paciente/${pacienteId}/prontuario`, { timeline })
  },
  async registrarEvolucao(
    pacienteId: number,
    payload: { conteudoJson: unknown; modeloDeProntuarioId?: number },
  ): Promise<{ evolucaoId: number }> {
    return http.post(`/paciente/${pacienteId}/prontuario/evolucoes`, payload)
  },
  /** Upload de anexo (multipart) — usa a request crua; backend grava no S3 (ProntuarioAnexo). */
  async uploadAnexo(
    pacienteId: number,
    file: Blob,
    nome: string,
    evolucaoId?: number,
  ): Promise<{ anexoId: number; storagePath: string }> {
    const form = new FormData()
    form.append("arquivo", file, nome)
    if (evolucaoId) form.append("evolucaoId", String(evolucaoId))
    // FormData precisa de fetch direto (sem Content-Type json).
    const res = await fetch(`${API_BASE}/paciente/${pacienteId}/prontuario/anexos`, {
      method: "POST",
      credentials: "include",
      body: form,
    })
    if (!res.ok) throw new Error("Falha no upload do anexo")
    return res.json()
  },
}
