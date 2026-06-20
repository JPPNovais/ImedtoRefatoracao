import { http } from "@/lib/http"
import { useTenantStore } from "@/stores/tenant"

export interface SalaDto {
  id: number
  nome: string
  ativo: boolean
  unidadeNome: string
  tipoSalaNome?: string | null
}

export const salaService = {
  async listar(apenasAtivas = true): Promise<SalaDto[]> {
    const tenant = useTenantStore()
    const id = tenant.estabelecimentoAtivoId
    if (!id) return []
    return http.get(`/estabelecimento/${id}/salas`, { apenasAtivas })
  },
}
