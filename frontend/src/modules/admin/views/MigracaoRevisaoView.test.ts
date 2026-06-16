import { describe, it, expect, vi, beforeEach } from "vitest"
import { flushPromises, mount } from "@vue/test-utils"
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
const mockCarregarEventos = vi.fn()
const mockCarregarProgresso = vi.fn()

let mockRelatorioDesfazimento: unknown = null
let mockDesfazendo = false
let mockReprocessando = false
let mockAprovando = false
let mockEventos: unknown[] = []
let mockCarregandoEventos = false
let mockProgresso: unknown = null
let mockAtualizandoEmBackground = false

const mapaJson = JSON.stringify({
    de_para: { nome: "nome", cpf: "cpf" },
    confianca: 0.85,
    duvidas: ["cpf"],
})

// Addendum 4 — helpers para montar mapas de dump aninhado
const mapaJsonComClassificacao = (entidade: string, confiancaClass: number, ignorado = false) =>
    JSON.stringify({
        de_para: { nome: "nome", data: "data_nascimento" },
        confianca: 0.9,
        duvidas: [] as string[],
        entidade_classificada: entidade,
        confianca_classificacao: confiancaClass,
        ignorado,
        encoding_suspeito: false,
    })

const mapaJsonSemEquivalente = JSON.stringify({
    de_para: {} as Record<string, string>,
    confianca: 0,
    duvidas: [] as string[],
    entidade_classificada: "sem_equivalente",
    confianca_classificacao: 0.4,
    ignorado: true,
    encoding_suspeito: false,
})

const mapaJsonConfig = JSON.stringify({
    de_para: {} as Record<string, string>,
    confianca: 0,
    duvidas: [] as string[],
    eh_config: true,
    encoding_suspeito: false,
})

const mapaJsonEncodingSuspeito = JSON.stringify({
    de_para: { especialidade: "Cirurgia PlÃ¡stica" },
    confianca: 0.8,
    duvidas: [] as string[],
    entidade_classificada: "paciente",
    confianca_classificacao: 0.9,
    encoding_suspeito: true,
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
        get eventos() { return mockEventos },
        get carregandoEventos() { return mockCarregandoEventos },
        get progresso() { return mockProgresso },
        get carregandoProgresso() { return false },
        get atualizandoEmBackground() { return mockAtualizandoEmBackground },
        carregarJob: mockCarregarJob,
        salvarMapa: mockSalvarMapa,
        salvarTemplate: mockSalvarTemplate,
        desfazer: mockDesfazer,
        carregarRelatorio: mockCarregarRelatorio,
        disparar: vi.fn(),
        gerarPreview: vi.fn(),
        reprocessar: mockReprocessar,
        aprovarAnalise: mockAprovarAnalise,
        carregarEventos: mockCarregarEventos,
        carregarProgresso: mockCarregarProgresso,
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
        mockCarregarJob.mockReset().mockResolvedValue(undefined)
        mockSalvarMapa.mockReset()
        mockSalvarTemplate.mockReset()
        mockDesfazer.mockReset()
        mockCarregarRelatorio.mockReset()
        mockReprocessar.mockReset()
        mockCarregarEventos.mockReset().mockResolvedValue(undefined)
        mockCarregarProgresso.mockReset().mockResolvedValue(undefined)
        mockJobAtual = null
        mockCarregando = false
        mockRelatorioDesfazimento = null
        mockDesfazendo = false
        mockReprocessando = false
        mockAprovando = false
        mockEventos = []
        mockCarregandoEventos = false
        mockProgresso = null
        mockAtualizandoEmBackground = false
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
            mapas: [{ id: 1, entidade: "paciente", nomeBlocoOrigem: "", mapaJson, revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z" }],
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
            mapas: [{ id: 1, entidade: "paciente", nomeBlocoOrigem: "", mapaJson, revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z" }],
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

    // ─── Addendum 003 — CA51/CA52 — Stepper ─────────────────────────────────

    it("CA51 — stepper mostra passo 'migrando' como atual", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "migrando",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        // O passo "Migrando" deve ter class stepper-item--atual
        const stepperItems = wrapper.findAll(".stepper-item--atual")
        expect(stepperItems.some(el => el.text().includes("Migrando"))).toBe(true)
    })

    it("CA52 — stepper mostra 'Falhou' como terminal quando status é falhou", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "falhou",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: "erro",
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        // O passo terminal de erro deve existir no stepper
        const erros = wrapper.findAll(".stepper-item--erro")
        expect(erros.some(el => el.text().includes("Falhou"))).toBe(true)
    })

    // ─── Addendum 003 — CA56 — Eventos ──────────────────────────────────────

    it("CA56 — lista de eventos vazia exibe mensagem honesta", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "concluido",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        mockEventos = []
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.text()).toContain("Histórico detalhado disponível a partir desta migração.")
    })

    it("CA53 — eventos são renderizados quando existem", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "concluido",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        mockEventos = [
            { statusAnterior: null, statusNovo: "aguardando_aprovacao", usuarioId: null, criadoEm: "2026-06-15T10:00:00Z" },
            { statusAnterior: "aguardando_aprovacao", statusNovo: "aguardando_mapa", usuarioId: "admin-1", criadoEm: "2026-06-15T11:00:00Z" },
        ]
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        const items = wrapper.findAll(".evento-item")
        expect(items.length).toBe(2)
    })

    // ─── Addendum 003 — CA60 — Polling ──────────────────────────────────────

    it("CA60 — polling inicia quando status é migrando", async () => {
        vi.useFakeTimers()
        mockCarregarJob.mockResolvedValue(undefined)
        mockCarregarProgresso.mockResolvedValue(undefined)
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "migrando",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await vi.runOnlyPendingTimersAsync()
        // Polling dispara carregarJob após 4s
        expect(mockCarregarJob.mock.calls.length).toBeGreaterThanOrEqual(2)
        vi.useRealTimers()
    })

    it("CA60 — polling NÃO inicia quando status é aguardando_aprovacao", async () => {
        vi.useFakeTimers()
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "aguardando_aprovacao",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        const chamadas = mockCarregarJob.mock.calls.length
        await vi.runOnlyPendingTimersAsync()
        // Sem polling, carregarJob não é chamado além do mount
        expect(mockCarregarJob.mock.calls.length).toBe(chamadas)
        vi.useRealTimers()
    })

    // ─── Addendum 4 — CA77: reclassificação de bloco pelo operador ──────────

    it("CA77 — seletor de entidade aparece para bloco com entidade_classificada", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 1,
                entidade: "paciente",
                nomeBlocoOrigem: "clientes",
                mapaJson: mapaJsonComClassificacao("paciente", 0.9),
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()
        // Seletor de reclassificação deve estar presente
        const selects = wrapper.findAll("select.classificacao-select")
        expect(selects.length).toBeGreaterThan(0)
    })

    it("CA77 — salvarMapa passa entidadeReclassificada quando operador reclassifica", async () => {
        mockSalvarMapa.mockResolvedValueOnce(undefined)
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 1,
                entidade: "paciente",
                nomeBlocoOrigem: "clientes",
                mapaJson: mapaJsonComClassificacao("agendamento", 0.5),
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Seleciona "paciente" no select de reclassificação
        const select = wrapper.find("select.classificacao-select")
        await select.setValue("paciente")
        await wrapper.vm.$nextTick()

        // Clica salvar
        const botaoSalvar = wrapper.findAll("button").find(b => b.text().includes("Salvar mapa"))
        await botaoSalvar!.trigger("click")
        await flushPromises()

        // store.salvarMapa deve ter sido chamado com entidadeReclassificada = "paciente"
        expect(mockSalvarMapa).toHaveBeenCalledWith(
            10,
            "paciente",
            expect.any(Object),
            "clientes",
            "paciente",
            expect.any(Boolean),
        )
    })

    // ─── Addendum 4 — CA78: sem_equivalente ignorado por padrão ────────────

    it("CA78 — bloco sem_equivalente exibe aviso e checkbox ignorar pré-marcado", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 2,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "dados_internos",
                mapaJson: mapaJsonSemEquivalente,
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Aviso de sem_equivalente deve aparecer
        expect(wrapper.text()).toContain("Nenhuma entidade equivalente foi identificada")

        // Checkbox de ignorar deve estar marcado
        const checkbox = wrapper.find("input.ignorar-checkbox")
        expect((checkbox.element as HTMLInputElement).checked).toBe(true)
    })

    it("CA78 — bloco sem_equivalente não exibe tabela de de-para (ignorado por padrão)", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 2,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "dados_internos",
                mapaJson: mapaJsonSemEquivalente,
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Tabela de-para NÃO deve aparecer (bloco ignorado)
        expect(wrapper.find(".depara-table").exists()).toBe(false)
    })

    // ─── Addendum 4 — CA79: bloco de config não migrável ────────────────────

    it("CA79 — bloco de config exibe badge e texto informativo, sem tabela editável", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 3,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "estabelecimento",
                mapaJson: mapaJsonConfig,
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Badge "Configuração (não migrável)" deve aparecer
        const badges = wrapper.findAll(".badge")
        expect(badges.some(b => b.text().includes("Configuração"))).toBe(true)

        // Texto informativo
        expect(wrapper.text()).toContain("não será migrado")

        // Tabela de-para NÃO deve aparecer
        expect(wrapper.find(".depara-table").exists()).toBe(false)

        // Botão Salvar NÃO deve aparecer
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Salvar mapa"))).toBe(false)
    })

    // ─── Addendum 4 — CA81: alerta de encoding suspeito ─────────────────────

    it("CA81 — alerta de encoding suspeito aparece quando encoding_suspeito=true", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 4,
                entidade: "paciente",
                nomeBlocoOrigem: "especialidades",
                mapaJson: mapaJsonEncodingSuspeito,
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("Encoding suspeito detectado")
    })

    // ─── Addendum 5 — CA92/R-R7: bloco com erro de IA sinalizado no painel ──

    const mapaJsonBlocoErro = (motivo = "limite_taxa_ia") => JSON.stringify({
        de_para: {} as Record<string, string>,
        confianca: 0,
        duvidas: [] as string[],
        entidade_classificada: "sem_equivalente",
        confianca_classificacao: 0,
        ignorado: false,
        encoding_suspeito: false,
        bloco_com_erro: true,
        motivo_erro: motivo,
    })

    it("CA92 — bloco com erro exibe alerta de falha de classificação", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 5,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "entradas",
                mapaJson: mapaJsonBlocoErro("limite_taxa_ia"),
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Deve exibir alerta de erro de classificação no bloco.
        expect(wrapper.text()).toContain("Não foi possível classificar este bloco")
        expect(wrapper.text()).toContain("limite de taxa da IA")
    })

    it("CA92 — badge 'Erro de classificação' aparece no bloco com erro", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 5,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "entradas",
                mapaJson: mapaJsonBlocoErro(),
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        const badges = wrapper.findAll(".badge")
        expect(badges.some(b => b.text().includes("Erro de classificação"))).toBe(true)
    })

    it("CA94 — banner agregado aparece quando há bloco com erro + bloco OK", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [
                {
                    id: 1,
                    entidade: "paciente",
                    nomeBlocoOrigem: "pacientes",
                    mapaJson: mapaJsonComClassificacao("paciente", 0.9),
                    revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
                },
                {
                    id: 2,
                    entidade: "sem_equivalente",
                    nomeBlocoOrigem: "entradas",
                    mapaJson: mapaJsonBlocoErro("limite_taxa_ia"),
                    revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
                },
            ],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Banner agregado: "1 de 2 bloco(s) não foram classificados"
        expect(wrapper.text()).toContain("1 de 2 bloco(s)")
        expect(wrapper.text()).toContain("não foram classificados")
    })

    it("CA96 — botão Salvar mapa não aparece para bloco com erro", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 5,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "entradas",
                mapaJson: mapaJsonBlocoErro(),
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        // Botão "Salvar mapa revisado" NÃO deve aparecer para bloco com erro (CA96).
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Salvar mapa revisado"))).toBe(false)
    })

    // ─── Fix UX — polling silencioso preserva scroll ─────────────────────────

    it("polling silencioso — carregarJob chamado com silencioso=true NÃO seta store.carregando", async () => {
        // Simula o comportamento do store ao chamar carregarJob(id, true):
        // carregando deve permanecer false durante a atualização em background.
        vi.useFakeTimers()
        mockCarregarJob.mockResolvedValue(undefined)
        mockCarregarProgresso.mockResolvedValue(undefined)
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "migrando",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await vi.runOnlyPendingTimersAsync()
        // Polling chamou carregarJob com silencioso=true — carregando nunca deve ter sido true durante o polling
        const chamadas = mockCarregarJob.mock.calls.filter(c => c[1] === true)
        expect(chamadas.length).toBeGreaterThan(0)
        // mockCarregando permanece false (o store real não é executado — apenas verificamos que o argumento silencioso foi passado)
        expect(mockCarregando).toBe(false)
        vi.useRealTimers()
    })

    it("carregamento inicial NÃO passa silencioso — usa loading normal", () => {
        mockJobAtual = null
        mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        // onMounted chama carregarJob(10) sem segundo argumento — silencioso = false (default)
        expect(mockCarregarJob).toHaveBeenCalledWith(10)
        const primeiraChamada = mockCarregarJob.mock.calls[0]
        expect(primeiraChamada[1]).toBeUndefined()
    })

    it("indicador de background aparece quando atualizandoEmBackground=true e carregando=false", async () => {
        mockAtualizandoEmBackground = true
        mockCarregando = false
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "migrando",
            origem: null, criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            mapas: [], templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.find(".polling-indicator").exists()).toBe(true)
        expect(wrapper.text()).toContain("Atualizando")
    })

    it("indicador de background NÃO aparece quando carregando inicial está ativo", async () => {
        mockAtualizandoEmBackground = true
        mockCarregando = true
        mockJobAtual = null
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await wrapper.vm.$nextTick()
        expect(wrapper.find(".polling-indicator").exists()).toBe(false)
    })

    it("CA92 — bloco com erro provider_indisponivel exibe mensagem correta", async () => {
        mockJobAtual = {
            id: 10, estabelecimentoId: 42, status: "mapa_em_revisao",
            origem: "dump.json", criadoPorUsuarioId: "abc",
            criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z",
            templateOrigemId: null, nomeTemplate: null, motivoFalha: null,
            mapas: [{
                id: 5,
                entidade: "sem_equivalente",
                nomeBlocoOrigem: "entradas",
                mapaJson: mapaJsonBlocoErro("provider_indisponivel"),
                revisadoPorUsuarioId: null, revisadoEm: null, criadoEm: "2026-01-01T00:00:00Z",
            }],
        }
        const wrapper = mount(MigracaoRevisaoView, { props: { jobId: "10" } })
        await flushPromises()
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("provider de IA indisponível")
    })
})
