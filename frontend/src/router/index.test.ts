import { describe, it, expect, vi, beforeEach } from "vitest"

// O router importa stores de Pinia — mockamos as factories para não criar
// dependência circular com httpClient / realtimeService durante o load.
// Os mocks abaixo são compartilhados entre os describes; cada teste pode
// reescrevê-los via `.mockReturnValue(...)`.
const authMock = {
    usuario: null as null | { id: string },
    isAuthenticated: false,
    onboardingPendente: false,
    init: vi.fn(),
}
const tenantMock = {
    estabelecimentoAtual: null,
    temTenantSelecionado: false,
    semEstabelecimento: false,
    papel: null as null | "Dono" | "Profissional",
}
const permissoesMock = {
    ehDono: false,
    pode: vi.fn().mockReturnValue(false),
    podeExtra: vi.fn().mockReturnValue(false),
}

vi.mock("@/stores/authStore",       () => ({ useAuthStore:       vi.fn(() => authMock) }))
vi.mock("@/stores/tenantStore",     () => ({ useTenantStore:     vi.fn(() => tenantMock) }))
vi.mock("@/stores/assinaturaStore", () => ({ useAssinaturaStore: vi.fn(() => ({ statusAtual: null, ensureLoaded: vi.fn(), isBlocked: false })) }))
vi.mock("@/stores/permissoesStore", () => ({ usePermissoesStore: vi.fn(() => permissoesMock) }))


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

describe("router — guard de permissão por rota (Bug C)", () => {
    beforeEach(() => {
        // Estado padrão: usuário autenticado, com tenant ativo, sem permissões.
        authMock.isAuthenticated = true
        authMock.usuario = { id: "user-1" }
        authMock.onboardingPendente = false
        tenantMock.temTenantSelecionado = true
        tenantMock.semEstabelecimento = false
        tenantMock.papel = "Profissional"
        permissoesMock.ehDono = false
        permissoesMock.pode = vi.fn().mockReturnValue(false)
        permissoesMock.podeExtra = vi.fn().mockReturnValue(false)
    })

    it("Profissional sem `equipe.ver` é redirecionado para Home ao tentar /equipe", async () => {
        await router.push({ name: "Home" })

        await router.push({ name: "Equipe" })

        // Após o guard, o router permaneceu/voltou para Home.
        expect(router.currentRoute.value.name).toBe("Home")
    })

    it("Profissional com `equipe.ver` consegue navegar para /equipe", async () => {
        permissoesMock.pode = vi.fn((k: string) => k === "equipe.ver")

        await router.push({ name: "Home" })
        await router.push({ name: "Equipe" })

        expect(router.currentRoute.value.name).toBe("Equipe")
    })

    it("Dono entra em qualquer rota restrita (Equipe, Financeiro, Estabelecimento)", async () => {
        permissoesMock.ehDono = true
        permissoesMock.pode = vi.fn().mockReturnValue(true)
        permissoesMock.podeExtra = vi.fn().mockReturnValue(true)

        for (const nome of ["Equipe", "Financeiro", "Estabelecimento"] as const) {
            await router.push({ name: "Home" })
            await router.push({ name: nome })
            expect(router.currentRoute.value.name, `Dono deveria entrar em ${nome}`).toBe(nome)
        }
    })

    it("Profissional sem `financeiro.ver` é bloqueado em /financeiro e /financeiro/categorias", async () => {
        for (const nome of ["Financeiro", "CategoriasFinanceiras", "FormasPagamento"] as const) {
            await router.push({ name: "Home" })
            await router.push({ name: nome })
            expect(router.currentRoute.value.name, `bloqueio esperado em ${nome}`).toBe("Home")
        }
    })

    it("rotas livres (Home, MeusConvites, MinhaConta) não disparam o guard", async () => {
        for (const nome of ["Home", "MeusConvites", "MinhaConta", "MinhaContaLgpd"] as const) {
            await router.push({ name: "Home" })
            await router.push({ name: nome })
            expect(router.currentRoute.value.name, `rota ${nome} deveria abrir`).toBe(nome)
        }
    })
})
