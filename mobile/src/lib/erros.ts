import type { ApiError } from "@/types"

/** Extrai a mensagem de um erro capturado.
 *  Se for ApiError (tem `status`), retorna a mensagem do backend (que pode ser um 422 com BusinessException).
 *  Caso contrário (erro de rede, sem status), retorna o fallback fornecido. */
export function mensagemDeErro(err: unknown, fallback: string): string {
  if (err && typeof err === "object" && "status" in err) {
    return (err as ApiError).mensagem || fallback
  }
  return fallback
}
