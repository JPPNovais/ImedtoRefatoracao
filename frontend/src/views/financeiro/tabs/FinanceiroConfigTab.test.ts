import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import FinanceiroConfigTab from "./FinanceiroConfigTab.vue"

/**
 * FinanceiroConfigTab — regressão do bug Tipo A (QA 2026-06-11_004):
 * AppInputDecimal com decimals=2 (padrão) emitia "0.40" ao digitar "40".
 * Correção: decimals=0 nos campos de percentual + conversão string→Number no payload.
 *
 * Cenários cobertos:
 * 1. Digitar "40" no procedimento → salvarComissao envia { percentualProcedimento: 40 } (number)
 * 2. Digitar "40" na consulta    → salvarComissao envia { percentualConsulta: 40 } (number)
 * 3. Valor null permanece null no payload — nunca vira 0
 * 4. String vazia é tratada como null no payload
 */

// ─── Mock de stores adicionados no briefing 2026-06-13_003 ───────────────────
vi.mock("@/stores/cobrancaStore", () => ({
    useCobrancaConfigStore: vi.fn(() => ({
        tabelaPreco: [],
        configTaxa: [],
        carregando: false,
        carregarTabelaPreco: vi.fn().mockResolvedValue(undefined),
        carregarConfigTaxa: vi.fn().mockResolvedValue(undefined),
        salvarTabelaPreco: vi.fn().mockResolvedValue(undefined),
        inativarTabelaPreco: vi.fn().mockResolvedValue(undefined),
        salvarConfigTaxa: vi.fn().mockResolvedValue(undefined),
    })),
}))

vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: vi.fn(() => ({
        pode: vi.fn(() => true),
        ehDono: true,
    })),
}))

// ─── Mock do design system ────────────────────────────────────────────────────
vi.mock("@/components/ui", () => {
    const AppField = {
        props: ["label"],
        template: `<div><label>{{ label }}</label><slot /></div>`,
    }
    // AppInputDecimal stub: emite string como o componente real faz (decimals ignorado no stub).
    // Testamos a lógica de conversão no pai, não no AppInputDecimal.
    const AppInputDecimal = {
        props: ["modelValue", "decimals", "placeholder", "disabled"],
        emits: ["update:modelValue"],
        template: `<input
            data-testid="input-decimal"
            :value="modelValue ?? ''"
            @input="$emit('update:modelValue', $event.target.value)"
        />`,
    }
    const AppButton = {
        props: ["variant", "loading", "disabled", "icon", "size"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
    }
    const AppModal = {
        props: ["aberto", "titulo", "largura"],
        emits: ["fechar"],
        template: `<div v-if="aberto" data-testid="modal"><slot /><div data-testid="rodape"><slot name="rodape" /></div></div>`,
    }
    const AppToast = {
        props: ["mensagem", "variante"],
        emits: ["fechar"],
        template: `<div data-testid="toast">{{ mensagem }}</div>`,
    }
    const AppBadge = {
        props: ["variant", "label"],
        template: `<span>{{ label }}</span>`,
    }
    const AppSelect = {
        props: ["modelValue", "options"],
        emits: ["update:modelValue"],
        template: `<select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)"><option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option></select>`,
    }
    const AppEmptyState = {
        props: ["icone", "titulo", "descricao"],
        template: `<div data-testid="empty-state"><slot /></div>`,
    }
    const AppSearchInput = {
        props: ["modelValue", "placeholder"],
        emits: ["update:modelValue"],
        template: `<input data-testid="search-input" :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    return { AppField, AppInputDecimal, AppButton, AppModal, AppToast, AppBadge, AppSelect, AppEmptyState, AppSearchInput }
})

// useDebouncedRef: retorna o próprio ref de entrada (sem delay) para simplificar testes
vi.mock("@/composables/useDebouncedRef", () => ({
    useDebouncedRef: (source: any) => source,
}))

// ─── Mocks de service ─────────────────────────────────────────────────────────
const mockSalvarConfigComissao   = vi.fn()
const mockObterConfigComissao    = vi.fn()
const mockListarProfissionaisPublico = vi.fn()

vi.mock("@/services/financeiroService", () => ({
    financeiroService: {
        salvarConfigComissao: (...args: unknown[]) => mockSalvarConfigComissao(...args),
        obterConfigComissao:  (...args: unknown[]) => mockObterConfigComissao(...args),
    },
}))

vi.mock("@/services/vinculoService", () => ({
    vinculoService: {
        listarProfissionaisPublico: (...args: unknown[]) => mockListarProfissionaisPublico(...args),
    },
}))

// ─── Fixtures ─────────────────────────────────────────────────────────────────
const profissional = { usuarioId: "u-1", nomeCompleto: "Dr. Teste" }
const configBase = { percentualPadrao: 30, percentualConsulta: null, percentualProcedimento: null }

async function montarEAbrirModal() {
    mockListarProfissionaisPublico.mockResolvedValue([profissional])
    mockObterConfigComissao.mockResolvedValue({ ...configBase })
    mockSalvarConfigComissao.mockResolvedValue(undefined)

    const wrapper = mount(FinanceiroConfigTab, {
        props: { ehDono: true },
        global: { stubs: { teleport: true } },
        attachTo: document.body,
    })
    await flushPromises()

    // Abre o modal clicando no botão editar da primeira linha
    await wrapper.find(".btn-icon-editar").trigger("click")
    await flushPromises()

    return wrapper
}

// ─── Testes ───────────────────────────────────────────────────────────────────
describe("FinanceiroConfigTab — conversão de percentual no payload", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("regressão: digitar '40' no procedimento envia number 40, não string '0.40'", async () => {
        const wrapper = await montarEAbrirModal()

        const inputs = wrapper.findAll("[data-testid='input-decimal']")
        // índice 0 = consulta, índice 1 = procedimento
        await inputs[1].setValue("40")

        // Clica no botão Salvar (último botão do rodape)
        const botoesRodape = wrapper.find("[data-testid='rodape']").findAll("button")
        await botoesRodape[botoesRodape.length - 1].trigger("click")
        await flushPromises()

        expect(mockSalvarConfigComissao).toHaveBeenCalledOnce()
        const payload = mockSalvarConfigComissao.mock.calls[0][0]

        expect(typeof payload.percentualProcedimento).toBe("number")
        expect(payload.percentualProcedimento).toBe(40)
    })

    it("regressão: digitar '40' na consulta envia number 40", async () => {
        const wrapper = await montarEAbrirModal()

        const inputs = wrapper.findAll("[data-testid='input-decimal']")
        await inputs[0].setValue("40")

        const botoesRodape = wrapper.find("[data-testid='rodape']").findAll("button")
        await botoesRodape[botoesRodape.length - 1].trigger("click")
        await flushPromises()

        expect(mockSalvarConfigComissao).toHaveBeenCalledOnce()
        const payload = mockSalvarConfigComissao.mock.calls[0][0]

        expect(typeof payload.percentualConsulta).toBe("number")
        expect(payload.percentualConsulta).toBe(40)
    })

    it("campo vazio ('') é tratado como null no payload — não vira 0", async () => {
        const wrapper = await montarEAbrirModal()

        const inputs = wrapper.findAll("[data-testid='input-decimal']")
        // Limpa o campo de procedimento
        await inputs[1].setValue("")

        const botoesRodape = wrapper.find("[data-testid='rodape']").findAll("button")
        await botoesRodape[botoesRodape.length - 1].trigger("click")
        await flushPromises()

        expect(mockSalvarConfigComissao).toHaveBeenCalledOnce()
        const payload = mockSalvarConfigComissao.mock.calls[0][0]

        expect(payload.percentualProcedimento).toBeNull()
        expect(payload.percentualProcedimento).not.toBe(0)
    })

    it("ambos campos vazios: consulta e procedimento null no payload", async () => {
        const wrapper = await montarEAbrirModal()

        const inputs = wrapper.findAll("[data-testid='input-decimal']")
        await inputs[0].setValue("")
        await inputs[1].setValue("")

        const botoesRodape = wrapper.find("[data-testid='rodape']").findAll("button")
        await botoesRodape[botoesRodape.length - 1].trigger("click")
        await flushPromises()

        expect(mockSalvarConfigComissao).toHaveBeenCalledOnce()
        const payload = mockSalvarConfigComissao.mock.calls[0][0]

        expect(payload.percentualConsulta).toBeNull()
        expect(payload.percentualProcedimento).toBeNull()
    })
})
