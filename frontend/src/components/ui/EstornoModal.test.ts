import { describe, it, expect } from "vitest"
import { nextTick } from "vue"
import { mount } from "@vue/test-utils"
import EstornoModal from "./EstornoModal.vue"
import type { PagamentoAba } from "@/services/cobrancaService"

// Stub do AppModal para isolar o componente
const AppModalStub = {
    name: "AppModal",
    template: `<div v-if="aberto"><slot /><slot name="rodape" /></div>`,
    props: ["aberto", "titulo"],
}
// Stub do AppButton que emite 'click' como evento nativo ao ser clicado
const AppButtonStub = {
    name: "AppButton",
    template: `<button @click="$emit('click')"><slot /></button>`,
    props: ["variant", "icon", "loading", "disabled"],
    emits: ["click"],
}

const globalConfig = {
    stubs: { AppModal: AppModalStub, AppButton: AppButtonStub },
}

const pagamentoMock: PagamentoAba = {
    id: 10,
    valor: 200,
    formaPagamentoNome: "Cartão de Crédito",
    parcelas: 2,
    taxa: 0,
    dataPagamento: "2026-06-01",
    estornado: false,
    estorno: null,
}

describe("EstornoModal", () => {
    it("nao renderiza quando aberto=false", () => {
        const w = mount(EstornoModal, {
            props: { aberto: false, pagamento: pagamentoMock },
            global: globalConfig,
        })
        expect(w.find(".estorno-summary").exists()).toBe(false)
    })

    it("renderiza forma de pagamento e valor quando aberto=true", () => {
        const w = mount(EstornoModal, {
            props: { aberto: true, pagamento: pagamentoMock },
            global: globalConfig,
        })
        expect(w.text()).toContain("Cartão de Crédito")
        // fmtMoeda usa locale pt-BR: "R$ 200,00" com separadores variáveis por ambiente
        expect(w.text()).toMatch(/200/)
    })

    it("emite confirmar com motivo preenchido", async () => {
        const w = mount(EstornoModal, {
            props: { aberto: true, pagamento: pagamentoMock },
            global: globalConfig,
        })
        await w.find("textarea").setValue("valor incorreto")
        await nextTick()

        const botoes = w.findAll("button")
        const botaoConfirmar = botoes.find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")
        await nextTick()

        expect(w.emitted("confirmar")).toBeTruthy()
        expect(w.emitted("confirmar")![0]).toEqual(["valor incorreto"])
    })

    it("nao emite confirmar quando motivo vazio e exibe erro", async () => {
        const w = mount(EstornoModal, {
            props: { aberto: true, pagamento: pagamentoMock },
            global: globalConfig,
        })
        await w.find("textarea").setValue("")
        await nextTick()

        const botoes = w.findAll("button")
        const botaoConfirmar = botoes.find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")
        await nextTick()

        expect(w.emitted("confirmar")).toBeFalsy()
        // A mensagem de erro deve aparecer no DOM
        const erroEl = w.find(".form-error-msg")
        expect(erroEl.exists()).toBe(true)
    })

    it("emite fechar ao clicar em Cancelar", async () => {
        const w = mount(EstornoModal, {
            props: { aberto: true, pagamento: pagamentoMock },
            global: globalConfig,
        })
        const botoes = w.findAll("button")
        const botaoCancelar = botoes.find(b => b.text().includes("Cancelar"))
        await botaoCancelar!.trigger("click")
        expect(w.emitted("fechar")).toBeTruthy()
    })

    it("limpa motivo ao reabrir o modal", async () => {
        const w = mount(EstornoModal, {
            props: { aberto: true, pagamento: pagamentoMock },
            global: globalConfig,
        })
        await w.find("textarea").setValue("motivo antigo")
        await w.setProps({ aberto: false })
        await w.setProps({ aberto: true })
        await nextTick()
        expect((w.find("textarea").element as HTMLTextAreaElement).value).toBe("")
    })
})
