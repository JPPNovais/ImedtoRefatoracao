import { http, getBlob } from "@/lib/http"
import type { CobrancaDetalheDto, ValorSugeridoCheckInDto } from "@/types"

export const cobrancaService = {
  async obterPorAgendamento(agendamentoId: number): Promise<CobrancaDetalheDto | null> {
    try {
      return await http.get(`/cobrancas/por-agendamento/${agendamentoId}`)
    } catch {
      return null
    }
  },

  async obterValorSugerido(profissionalUsuarioId: string): Promise<ValorSugeridoCheckInDto> {
    return http.get("/cobrancas/valor-sugerido", { profissionalUsuarioId })
  },

  async registrarPagamentos(
    cobrancaId: number,
    payload: {
      desconto: number
      dataPagamento: string
      formas: Array<{ formaPagamentoId: number; valor: number; parcelas: number; juros: number }>
    },
  ): Promise<void> {
    await http.post(`/cobrancas/${cobrancaId}/pagamentos`, payload)
  },

  /** Baixa o PDF do recibo de um pagamento — GET /api/cobrancas/pagamentos/{pagamentoId}/recibo */
  async baixarRecibo(pagamentoId: number): Promise<Blob> {
    return getBlob(`/cobrancas/pagamentos/${pagamentoId}/recibo`)
  },
}
