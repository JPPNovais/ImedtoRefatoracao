import httpClient from "./httpClient"
import type { Usuario } from "@/stores/authStore"
import type { ProfissionalPerfil } from "./profissionalService"
import type { Estabelecimento } from "./estabelecimentoService"

/**
 * Resposta agregada de GET /auth/bootstrap. Substitui /auth/me + /profissional/me +
 * /estabelecimento no boot do SPA — um único round-trip em vez de três serializados.
 */
export interface Bootstrap {
    usuario: Usuario
    profissional: ProfissionalPerfil | null
    estabelecimentos: Estabelecimento[]
}

export const bootstrapService = {
    async obter(): Promise<Bootstrap> {
        const { data } = await httpClient.get<Bootstrap>("/auth/bootstrap")
        return data
    },

    /**
     * Variante usada no boot inicial do SPA. Marca a request com `_noAutoRefresh: true`
     * para o interceptor de 401 não tentar /auth/refresh quando o usuário não tem
     * sessão (carga da landing/login). Evita 2 chamadas 401 ruidosas em cada carga.
     */
    async obterInicial(): Promise<Bootstrap> {
        const { data } = await httpClient.get<Bootstrap>("/auth/bootstrap", {
            _noAutoRefresh: true,
        } as any)
        return data
    },
}
