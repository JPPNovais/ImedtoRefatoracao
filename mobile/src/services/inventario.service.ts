import { http } from "@/lib/http"
import type { PaginaItensInventarioDto, RegistrarMovimentacaoDto } from "@/types"

export const inventarioService = {
  async listarItens(params: {
    apenasAbaixoMinimo?: boolean
    pagina?: number
    tamanho?: number
    busca?: string
  }): Promise<PaginaItensInventarioDto> {
    return http.get("/inventario/itens", {
      apenasAbaixoMinimo: params.apenasAbaixoMinimo,
      pagina: params.pagina ?? 1,
      tamanho: params.tamanho ?? 50,
      busca: params.busca || undefined,
    })
  },

  async registrarMovimentacao(dto: RegistrarMovimentacaoDto): Promise<void> {
    await http.post("/inventario/movimentacoes", dto)
  },
}
