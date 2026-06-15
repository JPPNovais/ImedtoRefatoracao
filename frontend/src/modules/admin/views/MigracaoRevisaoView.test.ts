import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import MigracaoRevisaoView from "./MigracaoRevisaoView.vue"

const mockBack = vi.fn()
vi.mock("vue-router", () => ({
    useRouter: () => ({ back: mockBack }),
    useRoute: () => ({}),
}))

const mockCarregarJob = vi.fn()
const mockSalvarMapa = vi.fn()
const mockSalvarTemplate = vi.fn()

const mapaJson = JSON.stringify({
    de_para: { nome: "nome", cpf: "cpf" },
    confianca: 0.85,
    duvidas: ["cpf"],
})

let mockJobAtual: unknown = null
let mockCarregando = false

vi.mock("../stores/migracaoAdminStore", () => ({
    useMigracaoAdminStore: () => ({
        get jobAtual() { return mockJobAtual },
        get carregando() { return mockCarregando },
        get erro() { return null },
        carregarJob: mockCarregarJob,
        salvarMapa: mockSalvarMapa,
        salvarTemplate: mockSalvarTemplate,
    }),
}))

vi.mock("@/components/ui", () => ({
    AppPageHeader: { template: "<div><slot /><slot name=\"acoes\" /></div>", props: ["titulo", "subtitulo"] },
    AppCard: { template: "<div><slot /></div>" },
    AppButton: {
        template: "<button type=\"button\" @click=\"$emit('click')\"><slot /></button>",
        props: ["variant", "loading"],
        emits: ["click"],
    },
    AppBadge: { template: "<span class=\"badge\"><slot /></span>", props: ["variant"] },
    AppModal: {
        template: "<div v-if=\"aberto\"><slot /><slot name=\"rodape\" /></div>",
        props: ["aberto", "titulo"],
        emits: ["fechar"],
    },
    AppField: { template: "<div><slot /></div>", props: ["label"] },
    AppInput: {
        template: "<input :value=\"modelValue\" @input=\"$emit('update:modelValue', $event.target.value)\" />",
        props: ["modelValue"],
        emits: ["update:modelValue"],
    },
}))

describe("MigracaoRevisaoView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        mockCarregarJob.mockReset()
        mockSalvarMapa.mockReset()
        mockSalvarTemplate.mockReset()
        mockJobAtual = null
        mockCarregando = false
    })

    it("chama carregarJob no onMounted", () => {
        mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        expect(mockCarregarJob).toHaveBeenCalledWith(10)
    })

    it("renderiza mapas por entidade quando jobAtual tem mapas", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [{ id: 1, entidade: "paciente", mapaJson, revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z" }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        // Verifica badge de confiança (85% → success).
        const badges = wrapper.findAll(".badge")
        expect(badges.some(b => b.text().includes("85%"))).toBe(true)
    })

    it("exibe ícone de dúvida para colunas duvidosas", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [{ id: 1, entidade: "paciente", mapaJson, revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z" }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        // Linha da coluna "cpf" deve ter classe row-duvida.
        const rows = wrapper.findAll("tr.row-duvida")
        expect(rows.length).toBeGreaterThan(0)
    })
})
