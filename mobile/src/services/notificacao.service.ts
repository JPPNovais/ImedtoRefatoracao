import { http } from "@/lib/http"
import type { PaginaNotificacoes } from "@/types"

export const notificacaoService = {
  async listar(params: { lidas?: boolean; pagina?: number; tamanho?: number } = {}): Promise<
    PaginaNotificacoes
  > {
    return http.get("/notificacoes", params)
  },
  async contadorNaoLidas(): Promise<{ total: number }> {
    return http.get("/notificacoes/contador-nao-lidas")
  },
  async marcarLida(id: number): Promise<void> {
    await http.post(`/notificacoes/${id}/marcar-lida`)
  },
  async marcarTodasLidas(): Promise<void> {
    await http.post("/notificacoes/marcar-todas-lidas")
  },
}
