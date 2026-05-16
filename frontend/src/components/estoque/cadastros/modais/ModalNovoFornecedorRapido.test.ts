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
        props: ["label", "required", "hint"],
        template: `<div><label>{{ label }}</label><slot /></div>`,
    }
    const AppInput = {
        props: ["modelValue", "placeholder", "type", "min", "step"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" :type="type || 'text'"
                          @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    return { AppModal, AppButton, AppField, AppInput }
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
        })
        expect(w.emitted("criada")![0][0]).toEqual({ id: 11, nome: "Distribuidora Saúde Ltda" })
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
