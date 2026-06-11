/**
 * Testes F8 — ação "Emitir recibo" no PaymentModal (CA118/CA120/CA122/CA129).
 * Porta 1: PaymentModal.vue.
 *
 * Arquivo separado do PaymentModal.test.ts para isolar os mocks do cobrancaService.
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import type { CobrancaDetalhe } from "@/services/cobrancaService"

// ── Mocks de componentes DS ───────────────────────────────────────────────────

vi.mock("./AppModal.vue",        () => ({ default: { props: ["aberto", "largura", "titulo"], emits: ["fechar"], template: `<div><slot /><div data-rodape><slot name="rodape" /></div></div>` } }))
vi.mock("./AppField.vue",        () => ({ default: { props: ["label"], template: `<div><span>{{ label }}</span><slot /></div>` } }))
vi.mock("./AppButton.vue",       () => ({ default: { props: ["variant", "loading", "disabled", "icon", "size"], emits: ["click"], template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>` } }))
vi.mock("./AppInputDecimal.vue", () => ({ default: { props: ["modelValue", "placeholder", "decimals"], emits: ["update:modelValue"], template: `<input :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />` } }))
vi.mock("./AppSelect.vue",       () => ({ default: { props: ["modelValue", "options"], emits: ["update:modelValue"], template: `<select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)"><option v-for="o in (options||[])" :key="o.value" :value="o.value">{{ o.label }}</option></select>` } }))

// ── Mock do cobrancaService ───────────────────────────────────────────────────
// vi.mock é hoisted — a factory não pode referenciar variáveis const declaradas
// antes dela. Solução: criar o fn dentro do factory e capturar via import dinâmico.

vi.mock("@/services/cobrancaService", () => ({
    cobrancaService: {
        emitirRecibo: vi.fn(),
    },
}))

import PaymentModal from "./PaymentModal.vue"
import { cobrancaService } from "@/services/cobrancaService"

// ── Helpers ───────────────────────────────────────────────────────────────────

type PagamentoResumo = CobrancaDetalhe["pagamentos"][number]

function makePagamento(id: number): PagamentoResumo {
    return {
        id,
        formaPagamentoId: 1,
        formaPagamentoNome: "Dinheiro",
        valor: 200,
        parcelas: 1,
        juros: 0,
        taxa: 0,
        dataPagamento: "2026-06-01",
        registradoPorNome: "Recepcionista",
    }
}

function makeCobrancaPaga(pagamentoId = 10): CobrancaDetalhe {
    return {
        id: 1,
        pacienteId: 10,
        agendamentoId: null,
        tipoAtendimento: "Particular",
        valorCobrado: 200,
        desconto: 0,
        status: "Paga",
        descricao: null,
        pagamentos: [makePagamento(pagamentoId)],
        totalLiquido: 200,
        totalPago: 200,
        saldoDevedor: 0,
    }
}

function montarPago(pagamentoId = 10) {
    return mount(PaymentModal, {
        props: {
            aberto: true,
            cobranca: makeCobrancaPaga(pagamentoId),
            formasPagamento: [{ value: 1, label: "Dinheiro" }],
            podeDesconto: false,
        },
        global: { stubs: { teleport: true } },
    })
}

// ── Testes ────────────────────────────────────────────────────────────────────

describe("PaymentModal — Emitir recibo F8 (CA118/CA122/CA129)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        vi.mocked(cobrancaService.emitirRecibo).mockResolvedValue(new Blob(["pdf"], { type: "application/pdf" }))

        Object.defineProperty(URL, "createObjectURL", { value: vi.fn().mockReturnValue("blob:test"), writable: true, configurable: true })
        Object.defineProperty(URL, "revokeObjectURL", { value: vi.fn(), writable: true, configurable: true })
    })

    // CA118: seção recibo renderizada quando cobrança está Paga e tem pagamentos
    it("exibe secao de recibo para cobranca Paga com pagamentos (CA118)", () => {
        const w = montarPago()
        // Seção Recibo deve aparecer
        expect(w.find(".payment-recibo-section").exists()).toBe(true)
        expect(w.text()).toContain("Recibo")
        // Botão "Emitir recibo" deve aparecer
        expect(w.text()).toContain("Emitir recibo")
    })

    // CA118: seção recibo não renderiza quando cobrança está Aberta
    it("nao exibe secao de recibo para cobranca Aberta (CA118)", () => {
        const w = mount(PaymentModal, {
            props: {
                aberto: true,
                cobranca: {
                    id: 1, pacienteId: 10, agendamentoId: null, tipoAtendimento: "Particular",
                    valorCobrado: 200, desconto: 0, status: "Aberta", descricao: null,
                    pagamentos: [], totalLiquido: 200, totalPago: 0, saldoDevedor: 200,
                } as CobrancaDetalhe,
                formasPagamento: [{ value: 1, label: "Dinheiro" }],
                podeDesconto: false,
            },
            global: { stubs: { teleport: true } },
        })
        expect(w.find(".payment-recibo-section").exists()).toBe(false)
    })

    // CA122: clique aciona emitirRecibo com pagamentoId correto
    it("chama emitirRecibo com o pagamentoId correto ao clicar (CA122)", async () => {
        const w = montarPago(77)
        // O AppButton está stubado como <button>; o texto "Emitir recibo" é o conteúdo do slot
        const btnRecibo = w.findAll("button").find(b => b.text().includes("Emitir recibo"))
        expect(btnRecibo).toBeTruthy()
        await btnRecibo!.trigger("click")
        await flushPromises()
        expect(vi.mocked(cobrancaService.emitirRecibo)).toHaveBeenCalledWith(77)
    })

    // CA129: botão fica desabilitado durante loading
    it("botao fica desabilitado durante fetch do blob (CA129)", async () => {
        let resolveBlob: (b: Blob) => void
        vi.mocked(cobrancaService.emitirRecibo).mockReturnValue(new Promise<Blob>((res) => { resolveBlob = res }))

        const w = montarPago(10)
        const btnRecibo = w.findAll("button").find(b => b.text().includes("Emitir recibo"))

        await btnRecibo!.trigger("click")
        await w.vm.$nextTick()
        // Durante loading: botão desabilitado
        expect(btnRecibo!.attributes("disabled")).toBeDefined()

        resolveBlob!(new Blob(["pdf"], { type: "application/pdf" }))
        await flushPromises()
        // Após resolver: botão habilitado novamente
        expect(btnRecibo!.attributes("disabled")).toBeUndefined()
    })

    // CA129: erro de API exibe mensagem (erroRecibo)
    it("exibe mensagem de erro quando API falha (CA129)", async () => {
        vi.mocked(cobrancaService.emitirRecibo).mockRejectedValue({
            response: { data: { mensagem: "Pagamento estornado não pode gerar recibo." } },
        })

        const w = montarPago()
        const btnRecibo = w.findAll("button").find(b => b.text().includes("Emitir recibo"))
        await btnRecibo!.trigger("click")
        await flushPromises()

        // Mensagem de erro exibida no template (payment-erro)
        expect(w.find(".payment-erro").exists()).toBe(true)
        expect(w.find(".payment-erro").text()).toContain("estornado")
    })
})
