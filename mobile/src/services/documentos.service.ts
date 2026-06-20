import { http, getBlob } from "@/lib/http"
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
  ): Promise<{ status: "NaoAssinada" | "AssinadaIcp" | "AssinadaMemed" | "FalhaAssinatura" | "AssinaturaExpirada" | string; pdfAssinadoUrl?: string | null }> {
    return http.get(`/receitas/${id}/status-assinatura`)
  },
  pdfUrl(id: number): string {
    return `/receitas/${id}/pdf`
  },
  /** Baixa o PDF da receita como Blob (cookie BFF + tenant header). */
  async baixarPdf(id: number): Promise<Blob> {
    return getBlob(`/receitas/${id}/pdf`)
  },
  /** Lista receitas emitidas para um paciente — GET /api/pacientes/{id}/receitas */
  async listarReceitas(pacienteId: number, pagina = 1, tamanho = 20): Promise<PaginaReceitasDto> {
    return http.get(`/pacientes/${pacienteId}/receitas`, { pagina, tamanho })
  },
}

export const atestadoService = {
  async emitir(
    pacienteId: number,
    payload: { tipo: string; diasAfastamento?: number; cid10?: string; conteudo: string },
  ): Promise<{ atestadoId: number }> {
    return http.post(`/pacientes/${pacienteId}/atestados`, payload)
  },
  /** Lista atestados emitidos para um paciente — GET /api/pacientes/{id}/atestados */
  async listarAtestados(pacienteId: number, pagina = 1, tamanho = 20): Promise<PaginaAtestadosDto> {
    return http.get(`/pacientes/${pacienteId}/atestados`, { pagina, tamanho })
  },
}

export const exameService = {
  async emitir(
    pacienteId: number,
    payload: { tipo: string; exames: string[]; indicacaoClinica: string; cid10?: string },
  ): Promise<{ pedidoExameId: number }> {
    return http.post(`/pacientes/${pacienteId}/pedidos-exame`, payload)
  },
  /** Lista pedidos de exame emitidos para um paciente — GET /api/pacientes/{id}/pedidos-exame */
  async listarPedidosExame(pacienteId: number, pagina = 1, tamanho = 20): Promise<PaginaPedidosExameDto> {
    return http.get(`/pacientes/${pacienteId}/pedidos-exame`, { pagina, tamanho })
  },
}

// ─── DTOs de listagem (espelham os contratos do backend) ────────────────────

export interface ReceitaResumoDto {
  id: number
  pacienteId: number
  prontuarioId: number
  tipo: string
  tipoNotificacao?: string | null
  status: string
  emitidaEm?: string | null
  validadeAte?: string | null
  requerRetencao: boolean
  quantidadeItens: number
  profissionalNome?: string | null
  assinaturaDigitalStatus: string
}

export interface PaginaReceitasDto {
  itens: ReceitaResumoDto[]
  total: number
  pagina: number
  tamanhoPagina: number
}

export interface AtestadoDto {
  id: number
  pacienteId: number
  profissionalNome?: string | null
  tipo: string
  diasAfastamento?: number | null
  cid10?: string | null
  conteudo: string
  criadoEm: string
}

export interface PaginaAtestadosDto {
  itens: AtestadoDto[]
  total: number
  pagina: number
  tamanhoPagina: number
}

export interface PedidoExameDto {
  id: number
  pacienteId: number
  profissionalNome?: string | null
  tipo: string
  exames: string[]
  indicacaoClinica: string
  cid10?: string | null
  observacoes?: string | null
  criadoEm: string
}

export interface PaginaPedidosExameDto {
  itens: PedidoExameDto[]
  total: number
  pagina: number
  tamanhoPagina: number
}
