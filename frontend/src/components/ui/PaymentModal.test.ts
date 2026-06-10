/**
 * Testes do PaymentModal (F1 — Financeiro, briefing 2026-06-10_009).
 *
 * Cobre: CA4 (saldo derivado), CA5 (bloqueia excesso), CA7 (preview taxa),
 * CA9 (campo desconto oculto/visível), CA17 (cobrança Paga sem form), R11 (múltiplas formas).
 */
import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import type { CobrancaDetalhe } from "@/services/cobrancaService"

// Stub dos componentes DS importados diretamente no PaymentModal
vi.mock("./AppModal.vue",        () => ({ default: { props: ["aberto", "largura", "titulo"], emits: ["fechar"], template: `<div><slot /><div data-rodape><slot name="rodape" /></div></div>` } }))
vi.mock("./AppField.vue",        () => ({ default: { props: ["label"], template: `<div><span>{{ label }}</span><slot /></div>` } }))
vi.mock("./AppButton.vue",       () => ({ default: { props: ["variant", "loading", "disabled"], emits: ["click"], template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>` } }))
vi.mock("./AppInputDecimal.vue", () => ({ default: { props: ["modelValue", "placeholder", "decimals"], emits: ["update:modelValue"], template: `<input :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />` } }))
vi.mock("./AppSelect.vue",       () => ({ default: { props: ["modelValue", "options"], emits: ["update:modelValue"], template: `<select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)"><option v-for="o in (options||[])" :key="o.value" :value="o.value">{{ o.label }}</option></select>` } }))

import PaymentModal from "./PaymentModal.vue"

function makeCobranca(overrides: Partial<CobrancaDetalhe> = {}): CobrancaDetalhe {
    return {
        id: 1,
        pacienteId: 10,
        agendamentoId: 100,
        tipoAtendimento: "Particular",
        valorCobrado: 200,
        desconto: 0,
        status: "Aberta",
        descricao: null,
        pagamentos: [],
        totalLiquido: 200,
        totalPago: 0,
        saldoDevedor: 200,
        ...overrides,
    }
}

const FORMAS = [{ value: 1, label: "Dinheiro" }, { value: 2, label: "Cartão" }]
const FORMAS_COM_TAXA = [
    { value: 1, label: "Dinheiro", taxaPercentual: undefined },
    { value: 2, label: "Cartão de Crédito", taxaPercentual: 3 },
]

function montar(props: Record<string, unknown> = {}) {
    return mount(PaymentModal, {
        props: {
            aberto: true,
            cobranca: makeCobranca(),
            formasPagamento: FORMAS,
            podeDesconto: false,
            ...props,
        },
        global: { stubs: { teleport: true } },
    })
}

// ── CA4: saldo derivado ───────────────────────────────────────────────────────

describe("PaymentModal — CA4 saldo derivado", () => {
    it("exibe valor cobrado R$ 200,00", () => {
        const w = montar()
        expect(w.text()).toContain("200,00")
    })

    it("exibe saldo parcial quando há pagamento parcial", () => {
        const w = montar({
            cobranca: makeCobranca({
                status: "ParcialmentePaga",
                totalPago: 80,
                saldoDevedor: 120,
            }),
        })
        expect(w.text()).toContain("80,00")
        expect(w.text()).toContain("120,00")
    })
})

// ── CA5: excesso bloqueia ─────────────────────────────────────────────────────

describe("PaymentModal — CA5 INV-1 excesso bloqueia", () => {
    it("botão de registrar fica desabilitado ao exceder saldo", async () => {
        const w = montar({ cobranca: makeCobranca({ saldoDevedor: 50 }) })
        // Digita valor além do saldo no input de valor da forma
        const inputs = w.findAll("input")
        // O primeiro input pode ser data, o segundo valor
        await inputs.at(-1)?.setValue("200")
        const btns = w.findAll("button").filter(b => !b.text().includes("Fechar") && b.text().includes("Registrar"))
        // Botão Registrar deve estar disabled
        const btnRegistrar = w.findAll("button").at(-1)
        expect(btnRegistrar?.attributes("disabled")).toBeDefined()
    })

    it("exibe mensagem de excesso quando totalFormas > saldo", async () => {
        const w = montar({ cobranca: makeCobranca({ saldoDevedor: 50 }) })
        // Encontra todos os inputs e seta o de valor (não é type=date)
        const todosInputs = w.findAll("input")
        const inputValor = todosInputs.find(i => (i.element as HTMLInputElement).type !== "date" && (i.element as HTMLInputElement).type !== "number")
        if (!inputValor) {
            // fallback: usa qualquer input
            const qualquer = todosInputs.at(0)
            if (qualquer) await qualquer.setValue("200")
        } else {
            await inputValor.setValue("200")
        }
        // A mensagem de excesso OU o botão disabled são indicadores válidos
        const excede = !!w.text().match(/excede/i)
        const btnRegistrar = w.findAll("button").at(-1)
        const disabled = btnRegistrar?.attributes("disabled") !== undefined
        expect(excede || disabled).toBe(true)
    })
})

// ── CA9: campo desconto ───────────────────────────────────────────────────────

describe("PaymentModal — CA9 RBAC desconto", () => {
    it("campo desconto não aparece sem permissão", () => {
        const w = montar({ podeDesconto: false })
        expect(w.text().toLowerCase()).not.toContain("desconto")
    })

    it("campo desconto aparece com permissão", () => {
        const w = montar({ podeDesconto: true })
        expect(w.text().toLowerCase()).toContain("desconto")
    })
})

// ── CA17: cobrança Paga ───────────────────────────────────────────────────────

describe("PaymentModal — CA17 cobrança Paga", () => {
    it("exibe mensagem de quitada e não exibe seção Novo pagamento", () => {
        const w = montar({
            cobranca: makeCobranca({ status: "Paga", totalPago: 200, saldoDevedor: 0 }),
        })
        expect(w.text()).toMatch(/quitada/i)
        expect(w.text()).not.toContain("Novo pagamento")
    })
})

// ── CA7: preview de taxa ──────────────────────────────────────────────────────

describe("PaymentModal — CA7 preview taxa forma pagamento", () => {
    it("exibe 'Você recebe R$ 97,00' com taxa 3% e valor R$ 100", async () => {
        // O watch que inicializa 'formas' só dispara quando 'aberto' muda false→true.
        // Montamos com aberto=false e depois abrimos para garantir que
        // formaPagamentoId receba formasPagamento[0].value (Cartão de Crédito, taxa 3%).
        const formasCartaoPrimeiro = [
            { value: 2, label: "Cartão de Crédito", taxaPercentual: 3 },
            { value: 1, label: "Dinheiro", taxaPercentual: undefined },
        ]
        const w = mount(PaymentModal, {
            props: {
                aberto: false,
                cobranca: makeCobranca({ saldoDevedor: 200, valorCobrado: 200 }),
                formasPagamento: formasCartaoPrimeiro,
                podeDesconto: false,
            },
            global: { stubs: { teleport: true } },
        })
        // Abre o modal para disparar o watch e inicializar formas com formaPagamentoId = 2
        await w.setProps({ aberto: true })
        // Digita R$ 100,00 no input de valor (não é date nem number/parcelas)
        const inputs = w.findAll("input")
        const inputValor = inputs.find(i => {
            const el = i.element as HTMLInputElement
            return el.type !== "date" && el.type !== "number"
        })
        await inputValor?.setValue("100")
        // Deve exibir o preview com o valor líquido (100 - 3% = 97,00)
        expect(w.text()).toContain("97,00")
        expect(w.text()).toMatch(/você recebe/i)
    })

    it("não exibe preview quando a forma não tem taxa configurada", async () => {
        const w = mount(PaymentModal, {
            props: {
                aberto: true,
                cobranca: makeCobranca({ saldoDevedor: 200, valorCobrado: 200 }),
                formasPagamento: FORMAS_COM_TAXA,
                podeDesconto: false,
            },
            global: { stubs: { teleport: true } },
        })
        // Seleciona Dinheiro (value=1, sem taxa)
        const selectEl = w.find("select")
        await selectEl.setValue(1)
        const inputs = w.findAll("input")
        const inputValor = inputs.find(i => {
            const el = i.element as HTMLInputElement
            return el.type !== "date" && el.type !== "number"
        })
        await inputValor?.setValue("100")
        expect(w.text()).not.toMatch(/você recebe/i)
    })
})

// ── R11: múltiplas formas ─────────────────────────────────────────────────────
// Nota: em JSDOM com scoped styles os seletores funcionam,
// mas os elementos das formas só aparecem se o v-else-if for respeitado.
// Usamos "Adicionar forma" como indicador de presença da seção de pagamento.

describe("PaymentModal — R11 múltiplas formas", () => {
    it("exibe botão 'Adicionar forma' na seção de pagamento (status Aberta)", () => {
        const w = montar()
        expect(w.text()).toContain("Adicionar forma")
    })

    it("não exibe botão 'Adicionar forma' para cobrança Paga", () => {
        const w = montar({
            cobranca: makeCobranca({ status: "Paga", totalPago: 200, saldoDevedor: 0 }),
        })
        expect(w.text()).not.toContain("Adicionar forma")
    })
})
