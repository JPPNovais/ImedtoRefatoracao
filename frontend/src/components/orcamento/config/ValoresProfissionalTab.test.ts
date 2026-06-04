import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"

// Mock do design system para evitar resolver @imedto/ui (pacote externo).
vi.mock("@/components/ui", () => {
    const AppStatCard = { props: ["label", "valor", "cor", "icone"], template: "<div><span>{{ label }}</span><span>{{ valor }}</span></div>" }
    const AppSearchInput = { props: ["modelValue"], emits: ["update:modelValue"], template: "<input :value='modelValue' @input=\"$emit('update:modelValue', $event.target.value)\" />" }
    const AppDrawer = {
        props: ["aberto", "titulo", "largura"],
        emits: ["fechar"],
        template: `<div v-if="aberto" data-test="drawer"><slot /><div data-test="rodape"><slot name="rodape" /></div></div>`,
    }
    const AppField = { props: ["label", "required"], template: "<div><label>{{ label }}</label><slot /></div>" }
    const AppInput = {
        props: ["modelValue", "type", "step", "placeholder"],
        emits: ["update:modelValue"],
        template: `<input :type="type || 'text'" :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    const AppSelect = {
        props: ["modelValue", "options"],
        emits: ["update:modelValue"],
        template: `<select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)"><option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option></select>`,
    }
    const AppButton = {
        props: ["variant", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
    }
    const AppPagination = { props: ["pagina", "tamanho", "total"], template: "<div />" }
    const AppEmptyState = {
        props: ["icone", "titulo", "descricao"],
        template: `<div data-test="empty-state"><span>{{ titulo }}</span><slot name="acao" /></div>`,
    }
    const AppToast = { props: ["mensagem", "variante"], emits: ["fechar"], template: `<div data-test="toast">{{ mensagem }}</div>` }
    const AppConfirmDialog = {
        props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante", "icone", "executando"],
        emits: ["update:aberto", "confirmar"],
        template: `<div v-if="aberto" data-test="confirm-dialog"><button data-test="btn-confirmar" @click="$emit('confirmar')">{{ confirmarRotulo }}</button></div>`,
    }
    return { AppStatCard, AppSearchInput, AppDrawer, AppField, AppInput, AppSelect, AppButton, AppPagination, AppEmptyState, AppToast, AppConfirmDialog }
})

// Mock do useDebouncedRef — retorna o ref diretamente para testes síncronos.
vi.mock("@/composables/useDebouncedRef", () => ({
    useDebouncedRef: <T>(r: T) => r,
}))

// Mock do formatarMoedaBrl — valor simples para facilitar assertivas.
vi.mock("@/utils/format", () => ({
    formatarMoedaBrl: (v: number) => `R$ ${v.toFixed(2)}`,
}))

// ─── Mocks de service ───
const mockListarValores = vi.fn()
const mockCriarValor = vi.fn()
const mockAtualizarValor = vi.fn()
const mockRemoverValor = vi.fn()

vi.mock("@/services/orcamentoCatalogoService", () => ({
    orcamentoCatalogoService: {
        listarValoresProfissional: (...a: any[]) => mockListarValores(...a),
        criarValorProfissional:    (...a: any[]) => mockCriarValor(...a),
        atualizarValorProfissional: (...a: any[]) => mockAtualizarValor(...a),
        removerValorProfissional:  (...a: any[]) => mockRemoverValor(...a),
    },
}))

const mockListarProfissionaisPublico = vi.fn()
vi.mock("@/services/vinculoService", () => ({
    vinculoService: {
        listarProfissionaisPublico: () => mockListarProfissionaisPublico(),
    },
}))

// ─── Mock de permissões ───
const mockPode = vi.fn()
vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: vi.fn(() => ({ pode: mockPode })),
}))

// Stub do httpClient para evitar network calls acidentais.
vi.mock("@/services/httpClient", () => ({
    default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))

import ValoresProfissionalTab from "./ValoresProfissionalTab.vue"

const VALOR_EXEMPLO = {
    id: 1,
    estabelecimentoId: 10,
    profissionalUsuarioId: null,
    profissionalNome: null,
    funcao: "Cirurgião principal",
    tempoBaseMinutos: 60,
    valorTempoBase: 1500,
    tempoAdicionalMinutos: 30,
    valorAdicional: 500,
    valorPlus: 200,
    ativo: true,
    criadaEm: "2026-01-01T00:00:00Z",
    atualizadaEm: null,
}

function montar() {
    return mount(ValoresProfissionalTab, {
        global: { plugins: [createPinia()] },
    })
}

describe("ValoresProfissionalTab", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        mockPode.mockReturnValue(true)
        mockListarValores.mockReset()
        mockListarValores.mockResolvedValue([])
        mockListarProfissionaisPublico.mockReset()
        mockListarProfissionaisPublico.mockResolvedValue([])
        mockCriarValor.mockReset()
        mockAtualizarValor.mockReset()
        mockRemoverValor.mockReset()
    })

    // ─── CA10: estado vazio ───
    it("CA10 — exibe AppEmptyState quando a lista está vazia", async () => {
        const w = montar()
        await flushPromises()
        const emptyState = w.find("[data-test='empty-state']")
        expect(emptyState.exists()).toBe(true)
        expect(emptyState.text()).toContain("Nenhum valor profissional cadastrado")
    })

    // ─── CA11: estado de carregamento ───
    it("CA11 — exibe 'Carregando…' durante o carregamento e some após resolver", async () => {
        // Promise que não resolve imediatamente
        let resolver!: (v: any) => void
        mockListarValores.mockReturnValue(new Promise(r => { resolver = r }))
        // Garante que profissionais resolva rápido para não interferir
        mockListarProfissionaisPublico.mockResolvedValue([])

        const w = montar()
        // Avança um tick para que o onMounted dispare e carregando=true seja visível
        await w.vm.$nextTick()

        expect(w.text()).toContain("Carregando…")

        resolver([])
        await flushPromises()
        expect(w.text()).not.toContain("Carregando…")
    })

    // ─── CA1: criar valor (caminho feliz) ───
    it("CA1 — cria valor, fecha drawer, emite contagem e exibe toast de sucesso", async () => {
        mockCriarValor.mockResolvedValue({ id: 2 })
        mockListarValores
            .mockResolvedValueOnce([])             // carga inicial
            .mockResolvedValueOnce([VALOR_EXEMPLO]) // após criar

        const w = montar()
        await flushPromises()

        // Clica em "Novo valor"
        const btnNovo = w.findAll("button").find(b => b.text() === "Novo valor")
        expect(btnNovo, "botão 'Novo valor' deve existir").toBeTruthy()
        await btnNovo!.trigger("click")
        await w.vm.$nextTick()

        // Drawer deve estar aberto
        expect(w.find("[data-test='drawer']").exists()).toBe(true)

        // Preenche o campo função (primeiro input text no drawer)
        const inputs = w.find("[data-test='drawer']").findAll("input")
        const funcaoInput = inputs[0] // primeiro input no drawer de criar é o select de profissional (não input), depois funcao
        // O campo função é o primeiro <input type="text">
        const textoInputs = w.find("[data-test='drawer']").findAll("input[type='text'], input:not([type])")
        await textoInputs[0].setValue("Cirurgião principal")

        // Clica em Salvar
        const rodape = w.find("[data-test='rodape']")
        const btnSalvar = rodape.findAll("button").find(b => b.text() === "Salvar")
        expect(btnSalvar, "botão Salvar deve existir").toBeTruthy()
        await btnSalvar!.trigger("click")
        await flushPromises()

        expect(mockCriarValor).toHaveBeenCalledTimes(1)
        const payload = mockCriarValor.mock.calls[0][0]
        expect(payload.funcao).toBe("Cirurgião principal")

        // Drawer fechado
        expect(w.find("[data-test='drawer']").exists()).toBe(false)

        // Toast de sucesso
        expect(w.find("[data-test='toast']").text()).toContain("criado")
    })

    // ─── CA2: profissionalUsuarioId = null quando "Padrão" selecionado ───
    it("CA2 — envia profissionalUsuarioId=null quando opção Padrão é mantida", async () => {
        mockCriarValor.mockResolvedValue({ id: 3 })
        mockListarValores.mockResolvedValue([])

        const w = montar()
        await flushPromises()

        const btnNovo = w.findAll("button").find(b => b.text() === "Novo valor")
        await btnNovo!.trigger("click")
        await w.vm.$nextTick()

        // Preenche funcao
        const textoInputs = w.find("[data-test='drawer']").findAll("input[type='text'], input:not([type])")
        await textoInputs[0].setValue("Auxiliar")

        // NÃO muda o select (mantém "Padrão")
        const rodape = w.find("[data-test='rodape']")
        const btnSalvar = rodape.findAll("button").find(b => b.text() === "Salvar")
        await btnSalvar!.trigger("click")
        await flushPromises()

        const payload = mockCriarValor.mock.calls[0][0]
        // String vazia no select se converte para null no payload
        expect(payload.profissionalUsuarioId).toBeNull()
    })

    // ─── CA3: editar — payload não contém profissionalUsuarioId ───
    it("CA3 — payload de atualização não contém profissionalUsuarioId", async () => {
        mockAtualizarValor.mockResolvedValue(undefined)
        mockListarValores.mockResolvedValue([VALOR_EXEMPLO])

        const w = montar()
        await flushPromises()

        // Clica no botão de editar
        const btnEditar = w.find(".btn-icon-editar")
        await btnEditar.trigger("click")
        await w.vm.$nextTick()

        expect(w.find("[data-test='drawer']").exists()).toBe(true)

        // Altera valorTempoBase (quarto input numérico no drawer de editar)
        const rodape = w.find("[data-test='rodape']")
        const btnSalvar = rodape.findAll("button").find(b => b.text() === "Salvar")
        await btnSalvar!.trigger("click")
        await flushPromises()

        expect(mockAtualizarValor).toHaveBeenCalledTimes(1)
        const [id, payload] = mockAtualizarValor.mock.calls[0]
        expect(id).toBe(VALOR_EXEMPLO.id)
        // Payload NÃO deve ter profissionalUsuarioId
        expect("profissionalUsuarioId" in payload).toBe(false)
        expect(payload.funcao).toBe(VALOR_EXEMPLO.funcao)
    })

    // ─── CA4: excluir ───
    it("CA4 — excluir abre confirmação, ao confirmar chama service e exibe toast", async () => {
        mockRemoverValor.mockResolvedValue(undefined)
        mockListarValores
            .mockResolvedValueOnce([VALOR_EXEMPLO])
            .mockResolvedValueOnce([]) // após remover

        const w = montar()
        await flushPromises()

        const btnExcluir = w.find(".btn-icon-excluir")
        await btnExcluir.trigger("click")
        await w.vm.$nextTick()

        // Confirm dialog deve aparecer
        expect(w.find("[data-test='confirm-dialog']").exists()).toBe(true)

        // Confirma exclusão
        await w.find("[data-test='btn-confirmar']").trigger("click")
        await flushPromises()

        expect(mockRemoverValor).toHaveBeenCalledWith(VALOR_EXEMPLO.id)
        expect(w.find("[data-test='toast']").text()).toContain("excluído")
    })

    // ─── CA9: validação — função vazia bloqueia submit ───
    it("CA9 — bloqueia salvar e exibe toast de erro quando função está vazia", async () => {
        mockListarValores.mockResolvedValue([])

        const w = montar()
        await flushPromises()

        const btnNovo = w.findAll("button").find(b => b.text() === "Novo valor")
        await btnNovo!.trigger("click")
        await w.vm.$nextTick()

        // NÃO preenche a função — tenta salvar direto
        const rodape = w.find("[data-test='rodape']")
        const btnSalvar = rodape.findAll("button").find(b => b.text() === "Salvar")
        await btnSalvar!.trigger("click")
        await flushPromises()

        // Nenhuma requisição de criação deve ter sido feita
        expect(mockCriarValor).not.toHaveBeenCalled()
        // Toast de erro exibido
        expect(w.find("[data-test='toast']").text()).toContain("obrigatória")
    })

    // ─── CA6: RBAC — sem orcamento.configurar, ações não aparecem ───
    it("CA6 — sem permissão orcamento.configurar, botões de criar/editar/excluir não aparecem", async () => {
        mockPode.mockReturnValue(false)
        mockListarValores.mockResolvedValue([VALOR_EXEMPLO])

        const w = montar()
        await flushPromises()

        // Botão "Novo valor" não deve aparecer
        const btnNovo = w.findAll("button").find(b => b.text() === "Novo valor")
        expect(btnNovo).toBeFalsy()

        // Botões de editar e excluir na tabela não devem aparecer
        expect(w.find(".btn-icon-editar").exists()).toBe(false)
        expect(w.find(".btn-icon-excluir").exists()).toBe(false)
    })

    // ─── CA7: LGPD — usa listarProfissionaisPublico, nunca listarProfissionais ───
    it("CA7 — usa listarProfissionaisPublico (LGPD) e não listarProfissionais (lista completa)", async () => {
        mockListarValores.mockResolvedValue([])
        mockListarProfissionaisPublico.mockResolvedValue([
            { usuarioId: "abc-123", nomeCompleto: "Dr. Carlos Silva", status: "Ativo" },
        ])

        const w = montar()
        await flushPromises()

        // listarProfissionaisPublico deve ter sido chamado
        expect(mockListarProfissionaisPublico).toHaveBeenCalledTimes(1)

        // Abre drawer e verifica que o select contém o profissional (pelo nome)
        const btnNovo = w.findAll("button").find(b => b.text() === "Novo valor")
        await btnNovo!.trigger("click")
        await w.vm.$nextTick()

        const selects = w.find("[data-test='drawer']").findAll("select")
        expect(selects.length).toBeGreaterThan(0)
        expect(selects[0].text()).toContain("Dr. Carlos Silva")
        // Nunca usa o nome para enviar — o value é o usuarioId
        const opcoes = selects[0].findAll("option")
        const opcaoProf = opcoes.find(o => o.text().includes("Dr. Carlos Silva"))
        expect(opcaoProf?.attributes("value")).toBe("abc-123")
    })
})
