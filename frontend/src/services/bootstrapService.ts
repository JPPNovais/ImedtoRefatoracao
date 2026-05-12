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
     * Variante usada no boot inicial do SPA. O backend retorna 200 com body `null`
     * quando não há sessão (em vez do antigo 401) — evita 2 chamadas 401 ruidosas
     * (bootstrap + refresh tentado pelo interceptor) e o `console.error` nativo
     * do browser em cada carga anônima da landing/login.
     */
    async obterInicial(): Promise<Bootstrap | null> {
        const { data } = await httpClient.get<Bootstrap | null>("/auth/bootstrap", {
            _noAutoRefresh: true,
        } as any)
        return data ?? null
    },
}
