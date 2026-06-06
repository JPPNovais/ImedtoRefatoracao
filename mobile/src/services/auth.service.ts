import { http } from "@/lib/http"
import type { BootstrapMe, Usuario } from "@/types"

export const authService = {
  async login(email: string, password: string): Promise<{ usuario: Usuario }> {
    return http.post("/auth/login", { email, password })
  },
  async logout(): Promise<void> {
    await http.post("/auth/logout")
  },
  /** Rehidrata a sessão: usuário + profissional + estabelecimentos (com papel/permissões). */
  async bootstrap(): Promise<BootstrapMe | null> {
    return http.get("/auth/bootstrap")
  },
  async registrarUltimoEstabelecimento(estabelecimentoId: number): Promise<void> {
    await http.post("/auth/ultimo-estabelecimento", { estabelecimentoId })
  },
  async forgotPassword(email: string): Promise<void> {
    await http.post("/auth/forgot-password", { email })
  },
}
