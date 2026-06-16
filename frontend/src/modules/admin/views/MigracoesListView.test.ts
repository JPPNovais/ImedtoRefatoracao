import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import MigracoesListView from "./MigracoesListView.vue"

// Mock do vue-router
const mockPush = vi.fn()
vi.mock("vue-router", () => ({
    useRouter: () => ({ push: mockPush }),
    useRoute: () => ({}),
}))

// Mock do store
const mockCarregar = vi.fn()
let mockJobs: unknown[] = []
let mockCarregando = false
let mockErro: string | null = null

vi.mock("../stores/migracaoAdminStore", () => ({
    useMigracaoAdminStore: () => ({
        get jobs() { return mockJobs },
        get total() { return mockJobs.length },
        get pagina() { return 1 },
        get tamanho() { return 25 },
        get carregando() { return mockCarregando },
        get erro() { return mockErro },
        carregar: mockCarregar,
    }),
}))

// Mock dos componentes UI
vi.mock("@/components/ui", () => ({
    AppPageHeader: { template: "<div><slot /></div>", props: ["titulo"] },
    AppCard: { template: "<div><slot /></div>" },
    AppEmptyState: { template: "<div class=\"empty-state\"><slot /></div>", props: ["titulo", "descricao"] },
    AppButton: { template: "<button type=\"button\" @click=\"$emit('click')\"><slot /></button>", emits: ["click"] },
    AppBadge: { template: "<span class=\"badge\"><slot /></span>", props: ["variant"] },
    AppPagination: { template: "<div />" },
}))

describe("MigracoesListView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        mockPush.mockReset()
        mockCarregar.mockReset().mockResolvedValue(undefined)
        mockJobs = []
        mockCarregando = false
        mockErro = null
    })

    it("chama carregar no onMounted", () => {
        mount(MigracoesListView)
        expect(mockCarregar).toHaveBeenCalledOnce()
    })

    it("exibe AppEmptyState quando lista está vazia", () => {
        mockJobs = []
        const wrapper = mount(MigracoesListView)
        expect(wrapper.find(".empty-state").exists()).toBe(true)
    })

    it("exibe tabela quando há jobs", () => {
        mockJobs = [
            {
                id: 1, estabelecimentoId: 42, status: "mapa_em_revisao",
                origem: "iClinic", criadoPorUsuarioId: "abc",
                criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z", mapas: [],
            },
        ]
        const wrapper = mount(MigracoesListView)
        expect(wrapper.find("table").exists()).toBe(true)
        expect(wrapper.find(".empty-state").exists()).toBe(false)
    })

    it("navega para revisão ao clicar em Ver", async () => {
        mockJobs = [
            {
                id: 7, estabelecimentoId: 1, status: "aguardando_mapa",
                origem: null, criadoPorUsuarioId: "abc",
                criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z", mapas: [],
            },
        ]
        const wrapper = mount(MigracoesListView)
        // Clica no botão "Ver" na linha da tabela (texto = "Ver").
        const botaoVer = wrapper.findAll("button").find(b => b.text() === "Ver")
        expect(botaoVer).toBeDefined()
        await botaoVer!.trigger("click")
        expect(mockPush).toHaveBeenCalledWith({
            name: "AdminMigracaoRevisao",
            params: { jobId: "7" },
        })
    })

    // ─── Addendum 003 — CA40 — badge de aguardando_aprovacao na lista ────────

    it("CA40 — job com status aguardando_aprovacao exibe badge 'Aguardando aprovação'", () => {
        mockJobs = [
            {
                id: 11, estabelecimentoId: 42, status: "aguardando_aprovacao",
                origem: "iClinic", criadoPorUsuarioId: "abc",
                criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
                mapas: [], motivoFalha: null,
            },
        ]
        const wrapper = mount(MigracoesListView)
        const badges = wrapper.findAll(".badge")
        expect(badges.some(b => b.text() === "Aguardando aprovação")).toBe(true)
    })

    // ─── Addendum 002 — CA29 — badge de falhou na lista ─────────────────────

    it("CA29 — job com status falhou exibe badge com label 'Falhou'", () => {
        mockJobs = [
            {
                id: 9, estabelecimentoId: 42, status: "falhou",
                origem: "iClinic", criadoPorUsuarioId: "abc",
                criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
                mapas: [], motivoFalha: "IA não configurada",
            },
        ]
        const wrapper = mount(MigracoesListView)
        const badges = wrapper.findAll(".badge")
        expect(badges.some(b => b.text() === "Falhou")).toBe(true)
    })

    // ─── Addendum 003 — CA61-CA67 — novos filtros ────────────────────────────

    it("CA61 — campos de filtro estão presentes no DOM", () => {
        const wrapper = mount(MigracoesListView)
        const inputs = wrapper.findAll("input")
        const selects = wrapper.findAll("select")
        // Estabelecimento ID, criadoDe, criadoAte, Origem = 4 inputs
        expect(inputs.length).toBeGreaterThanOrEqual(4)
        // Status + Onda = 2 selects
        expect(selects.length).toBeGreaterThanOrEqual(2)
    })

    it("CA67 — limpar filtros reseta e chama carregar", async () => {
        const wrapper = mount(MigracoesListView)
        await wrapper.vm.$nextTick()
        // Clica no botão Limpar
        const botoes = wrapper.findAll("button")
        const botaoLimpar = botoes.find(b => b.text().includes("Limpar"))
        expect(botaoLimpar).toBeDefined()
        await botaoLimpar!.trigger("click")
        await wrapper.vm.$nextTick()
        // carregar é chamado novamente após limpar (já foi chamado no mount, então >= 2)
        expect(mockCarregar.mock.calls.length).toBeGreaterThanOrEqual(2)
    })

    it("CA63 — mudar filtro de status chama carregar com page=1", async () => {
        const wrapper = mount(MigracoesListView)
        await wrapper.vm.$nextTick()
        mockCarregar.mockReset()
        const select = wrapper.find("select")
        await select.setValue("migrando")
        await select.trigger("change")
        await wrapper.vm.$nextTick()
        expect(mockCarregar).toHaveBeenCalled()
        const chamada = mockCarregar.mock.calls[0][0]
        expect(chamada.status).toBe("migrando")
        expect(chamada.page).toBe(1)
    })
})
