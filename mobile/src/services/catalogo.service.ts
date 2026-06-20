import { http } from "@/lib/http"
import type { Cid10Dto, ExameCatalogoDto } from "@/types"

/** Catálogo clínico global — CID-10 e exames.
 *  Endpoints públicos de referência (autenticados, sem filtro de tenant). */
export const catalogoService = {
  /**
   * Busca CID-10 por código ou descrição.
   * GET /api/catalogo/cid?busca=...&limite=20
   * Busca vazia retorna os mais frequentes (servidor decide).
   */
  async buscarCid(busca?: string, limite = 20): Promise<Cid10Dto[]> {
    return http.get("/catalogo/cid", { busca: busca || undefined, limite })
  },

  /**
   * Busca exames por nome.
   * GET /api/catalogo/exames?busca=...&limite=30
   * Busca vazia retorna os mais frequentes.
   */
  async buscarExames(busca?: string, limite = 30): Promise<ExameCatalogoDto[]> {
    return http.get("/catalogo/exames", { busca: busca || undefined, limite })
  },
}
