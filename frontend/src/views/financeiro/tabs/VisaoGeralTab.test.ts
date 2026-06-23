import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { defineComponent } from "vue"

/**
 * VisaoGeralTab — regressão do bug Tipo A (QA 2026-06-12, briefing 2026-06-12_001):
 * Race condition em ?filtro=vencidos após F5.
 *
 * Causa: modoVencidos era inicializado como false e setado para true no onMounted.
 * O watch { immediate: true } disparava uma request SEM somenteVencidos antes do
 * onMounted executar. Duas requests concorrentes — a sem filtro resolvia por último.
 *
 * Correção: modoVencidos é inicializado com route.query.filtro === "vencidos"
 * diretamente, eliminando o onMounted e a segunda request.
 *
 * Cenários cobertos:
 * 1. URL com ?filtro=vencidos → primeira (e única) chamada a extrato usa somenteVencidos: true
 * 2. URL sem filtro → chamada a extrato sem somenteVencidos
 */

// ─── Mocks hoisted ─────────────────────────────────────────────────────────────
const mocks = vi.hoisted(() => ({
    extratoSpy: vi.fn(),
    kpisSpy: vi.fn(),
    queryFiltro: { filtro: "vencidos" } as Record<string, string>,
}))

vi.mock("vue-router", async () => {
    const actual = await vi.importActual<typeof import("vue-router")>("vue-router")
    return {
        ...actual,
        useRoute: vi.fn(() => ({ query: mocks.queryFiltro })),
        useRouter: vi.fn(() => ({})),
    }
})

vi.mock("@/services/financeiroService", () => ({
    financeiroService: {
        extrato: (...args: unknown[]) => mocks.extratoSpy(...args),
        kpis:    (...args: unknown[]) => mocks.kpisSpy(...args),
    },
}))

vi.mock("@/services/categoriaFinanceiraService", () => ({
    categoriaFinanceiraService: {
        listar: vi.fn().mockResolvedValue([]),
        criar:  vi.fn().mockResolvedValue({ id: 99 }),
    },
}))

vi.mock("@/stores/tenantStore", () => ({
    useTenantStore: vi.fn(() => ({ ativo: { id: 1, nomeFantasia: "Clínica Teste" } })),
}))

vi.mock("@/components/ui", () => {
    const stub = (name: string) => defineComponent({ name, template: `<div />` })
    const stubSlot = (name: string) => defineComponent({
        name,
        template: `<div><slot /><slot name="rodape" /></div>`,
    })
    return {
        AppKpiCard:      stub("AppKpiCard"),
        AppFilterPills:  stub("AppFilterPills"),
        AppPagination:   stub("AppPagination"),
        AppModal:        stubSlot("AppModal"),
        AppField:        stubSlot("AppField"),
        AppInput:        stub("AppInput"),
        AppSelect:       stubSlot("AppSelect"),
        AppDatePicker:   stub("AppDatePicker"),
        AppButton:       defineComponent({
            name: "AppButton",
            emits: ["click"],
            template: `<button @click="$emit('click')"><slot /></button>`,
        }),
        AppToast:        stub("AppToast"),
        AppSelectCategoriaInline: stub("AppSelectCategoriaInline"),
    }
})

// ─── Import após mocks ─────────────────────────────────────────────────────────
import VisaoGeralTab from "./VisaoGeralTab.vue"

// ─── Fixtures ─────────────────────────────────────────────────────────────────
const paginaVazia = { itens: [], total: 0 }
const kpisVazios  = { recebido: 0, aReceber: 0, despesas: 0, saldo: 0, descontosConcedidos: 0, taxasCartao: 0, estornos: 0 }

// ─── Testes ───────────────────────────────────────────────────────────────────
describe("VisaoGeralTab — CA4: filtro ?filtro=vencidos sobrevive a F5 sem race condition", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mocks.extratoSpy.mockResolvedValue(paginaVazia)
        mocks.kpisSpy.mockResolvedValue(kpisVazios)
    })

    it("regressão: com ?filtro=vencidos a PRIMEIRA chamada a extrato usa somenteVencidos: true", async () => {
        // Garante que o mock de rota está com filtro=vencidos
        mocks.queryFiltro.filtro = "vencidos"

        mount(VisaoGeralTab, {
            global: { stubs: { Teleport: true } },
        })
        await flushPromises()

        // Deve ter chamado extrato pelo menos uma vez
        expect(mocks.extratoSpy).toHaveBeenCalled()

        // TODA chamada deve ter somenteVencidos: true — nenhuma sem o parâmetro
        const todasChamadas = mocks.extratoSpy.mock.calls
        for (const [args] of todasChamadas) {
            expect(args, "chamada sem somenteVencidos: true indica race condition").toMatchObject({
                somenteVencidos: true,
            })
        }
    })

    it("regressão: NENHUMA chamada prévia sem somenteVencidos antes da com filtro", async () => {
        mocks.queryFiltro.filtro = "vencidos"

        mount(VisaoGeralTab, {
            global: { stubs: { Teleport: true } },
        })
        await flushPromises()

        // Verifica que a primeira chamada já tem somenteVencidos: true
        const primeirasChamadas = mocks.extratoSpy.mock.calls
        expect(primeirasChamadas.length).toBeGreaterThan(0)

        const primeiraChamada = primeirasChamadas[0][0]
        expect(primeiraChamada.somenteVencidos).toBe(true)
    })

    it("sem filtro na URL: extrato é chamado sem somenteVencidos", async () => {
        // Remove o filtro da URL
        delete mocks.queryFiltro.filtro

        mount(VisaoGeralTab, {
            global: { stubs: { Teleport: true } },
        })
        await flushPromises()

        expect(mocks.extratoSpy).toHaveBeenCalled()

        const primeirasChamadas = mocks.extratoSpy.mock.calls
        const primeiraChamada = primeirasChamadas[0][0]
        // Sem filtro, não deve ter somenteVencidos: true
        expect(primeiraChamada.somenteVencidos).not.toBe(true)
    })
})
