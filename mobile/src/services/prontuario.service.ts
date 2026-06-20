import { http, getBlob } from "@/lib/http"
import { API_BASE } from "@/lib/config"
import { useTenantStore } from "@/stores/tenant"
import type { AnexoDto, AnexoUrlDto, ProntuarioCompleto } from "@/types"

export const prontuarioService = {
  async obter(pacienteId: number, timeline = 50): Promise<ProntuarioCompleto> {
    return http.get(`/paciente/${pacienteId}/prontuario`, { timeline })
  },
  /** Inicia o prontuário do paciente. ModeloDeProntuarioId é opcional (usa padrão do sistema). */
  async iniciarProntuario(pacienteId: number, modeloDeProntuarioId?: number): Promise<void> {
    await http.post(`/paciente/${pacienteId}/prontuario`, modeloDeProntuarioId ? { modeloDeProntuarioId } : {})
  },
  async registrarEvolucao(
    pacienteId: number,
    payload: { conteudoJson: string; modeloDeProntuarioId?: number },
  ): Promise<{ evolucaoId: number }> {
    return http.post(`/paciente/${pacienteId}/prontuario/evolucoes`, payload)
  },
  /** Lista anexos do prontuário. Chame sem evolucaoId para obter todos (fotos clínicas). */
  async listarAnexos(pacienteId: number, evolucaoId?: number): Promise<AnexoDto[]> {
    return http.get(`/paciente/${pacienteId}/prontuario/anexos`, evolucaoId ? { evolucaoId } : undefined)
  },
  /** URL assinada temporária (~5min) para exibir o anexo. */
  async obterUrlAnexo(pacienteId: number, anexoId: number): Promise<AnexoUrlDto> {
    return http.get(`/paciente/${pacienteId}/prontuario/anexos/${anexoId}/url`)
  },
  /** Upload de anexo (multipart) — backend grava no S3 (ProntuarioAnexo).
      Inclui regiaoAnatomica e marcador (Antes/Depois/Evolução) para fotos clínicas. */
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
    const tenantId = useTenantStore().estabelecimentoAtivoId
    const headers: Record<string, string> = {}
    if (tenantId) headers["X-Estabelecimento-Id"] = String(tenantId)
    const res = await fetch(`${API_BASE}/paciente/${pacienteId}/prontuario/anexos`, {
      method: "POST",
      credentials: "include",
      headers,
      body: form,
    })
    if (!res.ok) throw new Error("Falha no upload do anexo")
    return res.json()
  },
  /** Baixa o PDF do prontuário completo — retorna Blob (backend audita LGPD). */
  async baixarPdf(pacienteId: number): Promise<Blob> {
    return getBlob(`/paciente/${pacienteId}/prontuario/pdf`)
  },
  /** Upload de foto clínica com região anatômica e marcador (Antes/Depois/Evolução). */
  async uploadFotoClinica(
    pacienteId: number,
    file: Blob,
    nome: string,
    regiaoAnatomica: string,
    marcador: string,
  ): Promise<{ anexoId: number; storagePath: string }> {
    const form = new FormData()
    form.append("arquivo", file, nome)
    form.append("regiaoAnatomica", regiaoAnatomica)
    form.append("marcador", marcador)
    const tenantId = useTenantStore().estabelecimentoAtivoId
    const headers: Record<string, string> = {}
    if (tenantId) headers["X-Estabelecimento-Id"] = String(tenantId)
    const res = await fetch(`${API_BASE}/paciente/${pacienteId}/prontuario/anexos`, {
      method: "POST",
      credentials: "include",
      headers,
      body: form,
    })
    if (!res.ok) {
      const text = await res.text().catch(() => "")
      let msg = "Falha no upload da foto"
      try { msg = JSON.parse(text)?.mensagem || msg } catch { /* ignora */ }
      throw new Error(msg)
    }
    return res.json()
  },
}
