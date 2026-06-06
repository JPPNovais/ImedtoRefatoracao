import { http } from "@/lib/http"
import type { Estabelecimento } from "@/types"

export const estabelecimentoService = {
  /** Estabelecimentos/vínculos ativos do usuário (com papel + permissões por vínculo). */
  async listar(): Promise<Estabelecimento[]> {
    return http.get("/estabelecimento")
  },
}
