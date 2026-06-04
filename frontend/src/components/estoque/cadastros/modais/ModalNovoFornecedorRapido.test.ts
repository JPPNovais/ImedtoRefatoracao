import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

vi.mock("@/components/ui", () => {
    const AppModal = {
        props: ["aberto", "titulo", "largura", "acimaDeDrawer"],
        emits: ["fechar"],
        template: `<div v-if="aberto"><slot /><div data-test="rodape"><slot name="rodape" /></div></div>`,
    }
    const AppButton = {
        props: ["variant", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
    }
    const AppField = {
        props: ["label", "required", "hint", "class"],
        template: `<div><label>{{ label }}</label><slot /></div>`,
    }
    const AppInput = {
        props: ["modelValue", "placeholder", "type", "min", "step"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" :type="type || 'text'"
                          @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    const AppPillToggle = {
        props: ["modelValue", "opcoes"],
        emits: ["update:modelValue"],
        template: `<div data-test="pill-toggle">
            <button v-for="o in opcoes" :key="o.valor"
                    :data-valor="o.valor"
                    :data-selected="modelValue === o.valor"
                    @click="$emit('update:modelValue', o.valor)">{{ o.label }}</button>
        </div>`,
    }
    return { AppModal, AppButton, AppField, AppInput, AppPillToggle }
})

vi.mock("@/services/estoqueCadastrosService", async () => {
    const actual = await vi.importActual<any>("@/services/estoqueCadastrosService")
    return {
        ...actual,
        estoqueCadastrosService: {
            fornecedores: { criar: vi.fn() },
        },
    }
})

import ModalNovoFornecedorRapido from "./ModalNovoFornecedorRapido.vue"
import { estoqueCadastrosService } from "@/services/estoqueCadastrosService"

function montar() {
    return mount(ModalNovoFornecedorRapido, { props: { aberto: true } })
}

function botaoCriar(w: ReturnType<typeof montar>) {
    return w.findAll("[data-test='rodape'] button")[1]
}

describe("ModalNovoFornecedorRapido", () => {
    beforeEach(() => {
        vi.mocked(estoqueCadastrosService.fornecedores.criar).mockReset()
    })

    it("bloqueia submit quando razão social vazia", async () => {
        const w = montar()
        await botaoCriar(w).trigger("click")
        await flushPromises()
        expect(estoqueCadastrosService.fornecedores.criar).not.toHaveBeenCalled()
        expect(w.text()).toContain("Razão social é obrigatória.")
    })

    it("submete com defaults (sem CNPJ, prazo=5) e emite 'criada' com razão social", async () => {
        vi.mocked(estoqueCadastrosService.fornecedores.criar).mockResolvedValueOnce({ id: 11 })
        const w = montar()
        await w.findAll("input")[0].setValue("Distribuidora Saúde Ltda")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fornecedores.criar).toHaveBeenCalledWith({
            razaoSocial: "Distribuidora Saúde Ltda",
            nomeFantasia: null,
            cnpj: null,
            prazoEntregaDias: 5,
            tipoPrazoEntrega: 'corridos',
        })
        expect(w.emitted("criada")![0][0]).toEqual({ id: 11, nome: "Distribuidora Saúde Ltda" })
    })

    it("toggle inicia em corridos por padrão (CA2)", async () => {
        const w = montar()
        const toggle = w.find("[data-test='pill-toggle']")
        expect(toggle.exists()).toBe(true)
        const corridos = toggle.find("[data-valor='corridos']")
        expect(corridos.attributes("data-selected")).toBe("true")
    })

    it("submete com uteis quando toggle é alterado (CA4)", async () => {
        vi.mocked(estoqueCadastrosService.fornecedores.criar).mockResolvedValueOnce({ id: 42 })
        const w = montar()
        await w.findAll("input")[0].setValue("Fornecedor Útil Ltda")
        // Simula clique em "Úteis" no toggle
        await w.find("[data-test='pill-toggle'] [data-valor='uteis']").trigger("click")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fornecedores.criar).toHaveBeenCalledWith(
            expect.objectContaining({ tipoPrazoEntrega: 'uteis' })
        )
    })

    it("bloqueia submit quando CNPJ é inválido", async () => {
        const w = montar()
        await w.findAll("input")[0].setValue("Fornecedor X")
        // CNPJ inválido (14 dígitos mas DVs errados)
        await w.findAll("input")[2].setValue("11111111111111")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fornecedores.criar).not.toHaveBeenCalled()
        expect(w.text()).toContain("CNPJ inválido.")
    })
})
