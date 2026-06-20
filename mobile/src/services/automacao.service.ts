import { http } from "@/lib/http"
import type { ConfiguracaoAutomacaoDto } from "@/types"

export const automacaoService = {
  async obterConfiguracao(): Promise<ConfiguracaoAutomacaoDto> {
    return http.get("/automacoes/configuracao")
  },

  async salvarConfiguracao(dto: Partial<ConfiguracaoAutomacaoDto>): Promise<void> {
    await http.put("/automacoes/configuracao", dto)
  },
}
