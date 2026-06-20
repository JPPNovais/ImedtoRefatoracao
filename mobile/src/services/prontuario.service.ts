import { http, getBlob } from "@/lib/http"
import type { AnexoUrlDto, PaginaAnexosDto, ProntuarioCompleto } from "@/types"

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
  /**
   * Lista anexos do prontuário paginados.
   * Sem evolucaoId: todos os anexos (fotos clínicas). Com evolucaoId: só da evolução.
   * Backend retrocompat: sem pagina/tamanho retorna página 1 com 50 itens.
   */
  async listarAnexos(
    pacienteId: number,
    opcoes?: { evolucaoId?: number; pagina?: number; tamanho?: number },
  ): Promise<PaginaAnexosDto> {
    const params: Record<string, string | number | boolean | null | undefined> = {}
    if (opcoes?.evolucaoId) params.evolucaoId = opcoes.evolucaoId
    if (opcoes?.pagina) params.pagina = opcoes.pagina
    if (opcoes?.tamanho) params.tamanho = opcoes.tamanho
    return http.get(
      `/paciente/${pacienteId}/prontuario/anexos`,
      Object.keys(params).length ? params : undefined,
    )
  },
  /**
   * Batch de URLs assinadas para múltiplos anexos em uma só chamada (elimina N+1).
   * POST /api/paciente/{id}/prontuario/anexos/urls — body: { anexoIds: number[] }
   */
  async obterUrlsAnexos(pacienteId: number, anexoIds: number[]): Promise<AnexoUrlDto[]> {
    return http.post(`/paciente/${pacienteId}/prontuario/anexos/urls`, { anexoIds })
  },
  /** URL assinada temporária (~5min) para exibir um único anexo. */
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
    return http.postForm(`/paciente/${pacienteId}/prontuario/anexos`, form)
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
    return http.postForm(`/paciente/${pacienteId}/prontuario/anexos`, form)
  },
}
