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
        mockCarregar.mockReset()
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
        // Clica no botão "Ver" na primeira linha.
        await wrapper.find("button").trigger("click")
        expect(mockPush).toHaveBeenCalledWith({
            name: "AdminMigracaoRevisao",
            params: { jobId: "7" },
        })
    })
})
