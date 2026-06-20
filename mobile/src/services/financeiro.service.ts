import { http } from "@/lib/http"
import type { CaixaDiarioDto, PaginaLancamentosExtratoDto, FormaPagamentoDto } from "@/types"

/** Endpoints de financeiro consumidos pelas telas de Caixa, Pagamento e Início. */
export const financeiroService = {
  /** Caixa do dia — data no formato yyyy-MM-dd; omitir usa hoje. */
  async obterCaixa(data?: string): Promise<CaixaDiarioDto | null> {
    return http.get("/financeiro/caixa", data ? { data } : undefined)
  },

  async listarExtrato(params: {
    dataInicio: string
    dataFim: string
    pagina?: number
    tamanho?: number
  }): Promise<PaginaLancamentosExtratoDto> {
    return http.get("/financeiro/extrato", params)
  },

  async listarFormasPagamento(ativas?: boolean): Promise<FormaPagamentoDto[]> {
    return http.get("/financeiro/formas-pagamento", ativas !== undefined ? { ativas } : undefined)
  },
}
