import { describe, it, expect, vi } from "vitest"

// O router importa stores de Pinia — mockamos as factories para não criar
// dependência circular com httpClient / realtimeService durante o load.
vi.mock("@/stores/authStore",       () => ({ useAuthStore:       vi.fn(() => ({ usuario: null, isAuthenticated: false, init: vi.fn() })) }))
vi.mock("@/stores/tenantStore",     () => ({ useTenantStore:     vi.fn(() => ({ estabelecimentoAtual: null })) }))
vi.mock("@/stores/assinaturaStore", () => ({ useAssinaturaStore: vi.fn(() => ({ statusAtual: null })) }))

import router from "./index"

describe("router — rota MinhaContaLgpd (Correção 3)", () => {
    it("expõe rota nomeada 'MinhaContaLgpd' em /minha-conta/lgpd", () => {
        const rota = router.getRoutes().find(r => r.name === "MinhaContaLgpd")

        expect(rota).toBeTruthy()
        expect(rota!.path).toBe("/minha-conta/lgpd")
    })

    it("MinhaContaLgpd exige autenticação", () => {
        const rota = router.getRoutes().find(r => r.name === "MinhaContaLgpd")
        expect(rota!.meta.requiresAuth).toBe(true)
    })

    it("MinhaContaLgpd usa o layout 'app' (renderiza dentro do AppLayout)", () => {
        const rota = router.getRoutes().find(r => r.name === "MinhaContaLgpd")
        expect(rota!.meta.layout).toBe("app")
    })

    it("rota MinhaConta (mãe) também continua existindo (regressão)", () => {
        // Garantia de que ao adicionar /lgpd não removemos a rota original.
        const mae = router.getRoutes().find(r => r.name === "MinhaConta")
        expect(mae).toBeTruthy()
        expect(mae!.path).toBe("/minha-conta")
    })
})
