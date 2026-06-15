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
const mockDesfazer = vi.fn()
const mockCarregarRelatorio = vi.fn()
const mockReprocessar = vi.fn()
const mockAprovarAnalise = vi.fn()

let mockRelatorioDesfazimento: unknown = null
let mockDesfazendo = false
let mockReprocessando = false
let mockAprovando = false

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
        get relatorioDesfazimento() { return mockRelatorioDesfazimento },
        get desfazendo() { return mockDesfazendo },
        get relatorio() { return null },
        get preview() { return null },
        get disparando() { return false },
        get reprocessando() { return mockReprocessando },
        get aprovando() { return mockAprovando },
        carregarJob: mockCarregarJob,
        salvarMapa: mockSalvarMapa,
        salvarTemplate: mockSalvarTemplate,
        desfazer: mockDesfazer,
        carregarRelatorio: mockCarregarRelatorio,
        disparar: vi.fn(),
        gerarPreview: vi.fn(),
        reprocessar: mockReprocessar,
        aprovarAnalise: mockAprovarAnalise,
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
        mockDesfazer.mockReset()
        mockCarregarRelatorio.mockReset()
        mockReprocessar.mockReset()
        mockJobAtual = null
        mockCarregando = false
        mockRelatorioDesfazimento = null
        mockDesfazendo = false
        mockReprocessando = false
        mockAprovando = false
        mockAprovarAnalise.mockReset()
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

    // ─── CA17 — Desfazer ─────────────────────────────────────────────────────

    it("CA17 — botão Desfazer aparece quando job está concluído", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "concluido",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [],
            templateOrigemId: null, nomeTemplate: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        // Deve existir botão com texto "Desfazer"
        const botoes = wrapper.findAll("button")
        const botaoDesfazer = botoes.find(b => b.text().includes("Desfazer"))
        expect(botaoDesfazer).toBeDefined()
    })

    it("CA17 — botão Desfazer aparece quando job está concluido_com_erros", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "concluido_com_erros",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [],
            templateOrigemId: null, nomeTemplate: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Desfazer"))).toBe(true)
    })

    it("CA17 — clicar Desfazer abre modal de confirmação", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "concluido",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [],
            templateOrigemId: null, nomeTemplate: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const botaoDesfazer = wrapper.findAll("button").find(b => b.text().includes("Desfazer"))
        await botaoDesfazer!.trigger("click")
        await wrapper.vm.$nextTick()
        // Modal deve aparecer: texto de confirmação deve estar visível
        expect(wrapper.text()).toContain("somente os registros criados")
    })

    it("CA17 — confirmar desfazer chama store.desfazer com o jobId correto", async () => {
        mockDesfazer.mockResolvedValueOnce(undefined)
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "concluido",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [],
            templateOrigemId: null, nomeTemplate: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()

        // Abre o modal
        const botaoDesfazer = wrapper.findAll("button").find(b => b.text().includes("Desfazer") && !b.text().includes("Confirmar"))
        await botaoDesfazer!.trigger("click")
        await wrapper.vm.$nextTick()

        // Clica "Confirmar desfazer"
        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar desfazer"))
        await botaoConfirmar!.trigger("click")
        await wrapper.vm.$nextTick()

        expect(mockDesfazer).toHaveBeenCalledWith(10)
    })

    it("CA17 — painel de desfeito aparece quando status é desfeito", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "desfeito",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [],
            templateOrigemId: null, nomeTemplate: null,
        }
        mockRelatorioDesfazimento = {
            totalRevertidos: 800,
            totalNaoRevertidos: 0,
            totalAtualizadosMantidos: 200,
            aviso: "800 registros criados revertidos. 200 registros atualizados mantidos (não revertidos — pré-existiam antes da migração).",
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        // Painel de desfeito visível e aviso com 200
        expect(wrapper.text()).toContain("Migração desfeita")
        expect(wrapper.text()).toContain("200")
        expect(wrapper.text()).toContain("não revertidos")
    })

    // ─── Addendum 002 — CA29/CA30 — estado falhou e reprocessar ─────────────

    it("CA29 — painel de falha aparece quando status é falhou", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "falhou",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: "IA não configurada",
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.text()).toContain("Job com falha")
        expect(wrapper.text()).toContain("IA não configurada")
    })

    it("CA30 — botão Reprocessar aparece no painel de falha", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "falhou",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: "falha ao baixar o arquivo",
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Reprocessar"))).toBe(true)
    })

    it("CA30 — clicar Reprocessar chama store.reprocessar com jobId correto", async () => {
        mockReprocessar.mockResolvedValueOnce(undefined)
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "falhou",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: "falha inesperada na carga",
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const botaoReprocessar = wrapper.findAll("button").find(b => b.text().includes("Reprocessar"))
        await botaoReprocessar!.trigger("click")
        await wrapper.vm.$nextTick()
        expect(mockReprocessar).toHaveBeenCalledWith(10)
    })

    it("CA29 — painel de falhou NÃO aparece quando status é migrando", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "migrando",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.text()).not.toContain("Job com falha")
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Reprocessar"))).toBe(false)
    })

    // ─── Addendum 003 — CA41 — gate de aprovação humana ─────────────────────

    it("CA41 — painel de aprovação aparece quando status é aguardando_aprovacao", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "aguardando_aprovacao",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.text()).toContain("Aguardando aprovação")
        expect(wrapper.text()).toContain("análise por IA")
    })

    it("CA41 — botão 'Aprovar análise' aparece no painel de aprovação", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "aguardando_aprovacao",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Aprovar análise"))).toBe(true)
    })

    it("CA41 — clicar 'Aprovar análise' chama store.aprovarAnalise com jobId correto", async () => {
        mockAprovarAnalise.mockResolvedValueOnce(undefined)
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "aguardando_aprovacao",
            origem: "iClinic", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const botaoAprovar = wrapper.findAll("button").find(b => b.text().includes("Aprovar análise"))
        await botaoAprovar!.trigger("click")
        await wrapper.vm.$nextTick()
        expect(mockAprovarAnalise).toHaveBeenCalledWith(10)
    })

    it("CA41 — painel de aprovação NÃO aparece quando status é aguardando_mapa", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "aguardando_mapa",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null,
            motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.text()).not.toContain("Aprovar análise")
    })
})
