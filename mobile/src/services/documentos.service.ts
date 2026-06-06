import { http } from "@/lib/http"
import { API_ORIGIN } from "@/lib/config"
import type { ItemReceita, TipoReceita } from "@/types"

/* Receita / Atestado / Pedido de exame — todos compartilham o fluxo
   "emitir → assinar (ANVISA) → share". A assinatura é assíncrona no
   backend (202 + polling de status). */

export const receitaService = {
  async emitir(payload: {
    pacienteId: number
    tipo: TipoReceita
    observacoes?: string
    itens: ItemReceita[]
  }): Promise<{ receitaId: number }> {
    return http.post("/receitas", payload)
  },
  async assinar(id: number): Promise<{ status: string }> {
    return http.post(`/receitas/${id}/assinar`)
  },
  async statusAssinatura(
    id: number,
  ): Promise<{ status: "NaoAssinada" | "AssinadaIcp" | "AssinadaMemed" | "Erro"; urlAssinada?: string }> {
    return http.get(`/receitas/${id}/status-assinatura`)
  },
  pdfUrl(id: number): string {
    return `${API_ORIGIN}/api/receitas/${id}/pdf`
  },
}

export const atestadoService = {
  async emitir(
    pacienteId: number,
    payload: { tipo: string; diasAfastamento?: number; cid10?: string; conteudo: string },
  ): Promise<{ atestadoId: number }> {
    return http.post(`/pacientes/${pacienteId}/atestados`, payload)
  },
}

export const exameService = {
  async emitir(
    pacienteId: number,
    payload: { tipo: string; exames: string[]; indicacaoClinica: string; cid10?: string },
  ): Promise<{ pedidoExameId: number }> {
    return http.post(`/pacientes/${pacienteId}/pedidos-exame`, payload)
  },
}
