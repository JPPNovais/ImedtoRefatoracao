import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { defineComponent, h } from "vue"

/**
 * HomeView — filtragem de cards do dashboard por papel/permissão (Item 1)
 * + toast pós-redirect (Item 2).
 *
 * Foco do teste: garantir que o catálogo de atalhos do Home consome o mesmo
 * `routePermissions.podeAcessarRota` que o router guard. Sem isso, o Médico
 * via "Financeiro"/"Inventário" no dashboard, clicava e era jogado de volta
 * para Home — UX confusa apesar do defense-in-depth no router.
 */

// Vitest hoisting: `vi.mock` é içado para o topo do arquivo. Para referenciar
// estado mutável entre testes, usamos `vi.hoisted` (também içado) e expomos
// um único objeto que os factories de mock consultam.
const mocks = vi.hoisted(() => {
    const queryRef = { value: {} as Record<string, string> }
    return {
        dashboardService: {
            obter: vi.fn().mockResolvedValue({
                totalPacientesAtivos: 0,
                agendamentosHoje: 0,
                agendamentosSemana: 0,
                receitasMes: 0,
                despesasMes: 0,
                saldoMes: 0,
                itensAbaixoMinimo: 0,
                orcamentosPendentes: 0,
                lancamentosVencidos: 0,
                proximosAgendamentos: [],
                itensAbaixoMinimoLista: [],
            }),
        },
        auth: { usuario: { id: "u-1", email: "x@y.com", nomeCompleto: "João Médico" } },
        tenant: {
            ativo: { id: 1, nomeFantasia: "Clínica" } as object | null,
            semEstabelecimento: false,
        },
        permissoes: {
            ehDono: false,
            pode: vi.fn().mockReturnValue(false) as (k: string) => boolean,
            podeExtra: vi.fn().mockReturnValue(false) as (k: string) => boolean,
        },
        rotaQuery: queryRef,
        routerReplace: vi.fn(),
    }
})

vi.mock("@/services/dashboardService", () => ({ dashboardService: mocks.dashboardService }))
vi.mock("@/stores/authStore",       () => ({ useAuthStore:       vi.fn(() => mocks.auth) }))
vi.mock("@/stores/tenantStore",     () => ({ useTenantStore:     vi.fn(() => mocks.tenant) }))
vi.mock("@/stores/permissoesStore", () => ({ usePermissoesStore: vi.fn(() => mocks.permissoes) }))
vi.mock("vue-router", () => ({
    useRoute:         () => ({ get query() { return mocks.rotaQuery.value } }),
    useRouter:        () => ({ replace: mocks.routerReplace }),
    createRouter:     vi.fn(() => ({ beforeEach: vi.fn(), push: vi.fn(), currentRoute: { value: {} } })),
    createWebHistory: vi.fn(() => ({})),
}))

import HomeView from "./HomeView.vue"

function montar() {
    return mount(HomeView, {
        global: {
            stubs: {
                // Stub AppToast para inspecionar prop `mensagem` sem renderizar CSS.
                AppToast: defineComponent({
                    props: ["mensagem", "variante"],
                    emits: ["fechar"],
                    setup(props) {
                        return () => h("div", { class: "toast-stub", "data-mensagem": props.mensagem })
                    },
                }),
            },
        },
    })
}

function titulosDosCards(w: ReturnType<typeof montar>): string[] {
    return w.findAll(".menu .card h3").map(n => n.text())
}

describe("HomeView — Item 1 (cards respeitam papel/permissão)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mocks.permissoes.ehDono = false
        mocks.permissoes.pode = vi.fn().mockReturnValue(false)
        mocks.permissoes.podeExtra = vi.fn().mockReturnValue(false)
        mocks.tenant.ativo = { id: 1, nomeFantasia: "Clínica" }
        mocks.tenant.semEstabelecimento = false
        mocks.rotaQuery.value = {}
    })

    it("Dono vê os 6 cards do dashboard (Agenda, Pacientes, Financeiro, Orçamentos, Inventário, Relatórios)", async () => {
        mocks.permissoes.ehDono = true
        mocks.permissoes.pode = vi.fn().mockReturnValue(true)
        mocks.permissoes.podeExtra = vi.fn().mockReturnValue(true)

        const w = montar()
        await flushPromises()

        expect(titulosDosCards(w)).toEqual([
            "Agenda", "Pacientes", "Financeiro", "Orçamentos", "Inventário", "Relatórios",
        ])
    })

    it("Médico padrão NÃO vê Financeiro nem Inventário (papel = Profissional sem essas permissões)", async () => {
        // Mesmo conjunto do modelo MedicoPadrao usado no routePermissions.test.ts.
        const perms = new Set([
            "agenda.ver", "agenda.criar", "agenda.editar", "agenda.excluir",
            "prontuario.ver", "prontuario.editar", "prontuario.assinar",
            "prescricao.criar", "prescricao.assinar",
            "pacientes.ver", "pacientes.criar", "pacientes.editar",
            "orcamento.ver", "orcamento.criar",
            "relatorios.ver",
        ])
        mocks.permissoes.pode = vi.fn((k: string) => perms.has(k))

        const w = montar()
        await flushPromises()

        const titulos = titulosDosCards(w)
        expect(titulos).toContain("Agenda")
        expect(titulos).toContain("Pacientes")
        expect(titulos).toContain("Orçamentos")
        expect(titulos).toContain("Relatórios")
        expect(titulos).not.toContain("Financeiro")
        expect(titulos).not.toContain("Inventário")
    })

    it("Profissional sem nenhuma permissão não vê nenhum card de atalho", async () => {
        const w = montar()
        await flushPromises()

        expect(titulosDosCards(w)).toEqual([])
        // O <nav class="menu"> some quando não há cards — evita um bloco vazio.
        expect(w.find(".menu").exists()).toBe(false)
    })
})

describe("HomeView — Item 2 (toast pós-redirect por permissão)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mocks.permissoes.ehDono = true
        mocks.permissoes.pode = vi.fn().mockReturnValue(true)
        mocks.permissoes.podeExtra = vi.fn().mockReturnValue(true)
        mocks.tenant.ativo = { id: 1, nomeFantasia: "Clínica" }
        mocks.tenant.semEstabelecimento = false
        mocks.rotaQuery.value = {}
    })

    it("exibe AppToast quando `?bloqueado=<rota>` está presente na URL", async () => {
        mocks.rotaQuery.value = { bloqueado: "Financeiro" }

        const w = montar()
        await flushPromises()

        const toast = w.find(".toast-stub")
        expect(toast.exists()).toBe(true)
        expect(toast.attributes("data-mensagem")).toContain("restrita ao seu papel")
    })

    it("limpa o querystring `bloqueado` após exibir o toast (evita ressurgir em F5)", async () => {
        mocks.rotaQuery.value = { bloqueado: "Inventario", outro: "preservar" }

        montar()
        await flushPromises()

        expect(mocks.routerReplace).toHaveBeenCalledTimes(1)
        const arg = mocks.routerReplace.mock.calls[0][0]
        expect(arg).toEqual({ query: { outro: "preservar" } })
    })

    it("NÃO exibe toast quando não há `?bloqueado` (navegação normal)", async () => {
        const w = montar()
        await flushPromises()

        expect(w.find(".toast-stub").exists()).toBe(false)
        expect(mocks.routerReplace).not.toHaveBeenCalled()
    })
})
