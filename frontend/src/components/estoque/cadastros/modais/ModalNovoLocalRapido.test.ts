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
        props: ["label", "required"],
        template: `<div><label>{{ label }}</label><slot /></div>`,
    }
    const AppInput = {
        props: ["modelValue", "placeholder"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    const AppSelect = {
        props: ["modelValue"],
        emits: ["update:modelValue"],
        template: `<select :value="modelValue" @change="$emit('update:modelValue', $event.target.value)"><slot /></select>`,
    }
    return { AppModal, AppButton, AppField, AppInput, AppSelect }
})

vi.mock("@/services/estoqueCadastrosService", async () => {
    const actual = await vi.importActual<any>("@/services/estoqueCadastrosService")
    return {
        ...actual,
        estoqueCadastrosService: {
            locais: { criar: vi.fn() },
        },
    }
})

import ModalNovoLocalRapido from "./ModalNovoLocalRapido.vue"
import { estoqueCadastrosService } from "@/services/estoqueCadastrosService"

function montar() {
    return mount(ModalNovoLocalRapido, { props: { aberto: true } })
}

function botaoCriar(w: ReturnType<typeof montar>) {
    return w.findAll("[data-test='rodape'] button")[1]
}

describe("ModalNovoLocalRapido", () => {
    beforeEach(() => {
        vi.mocked(estoqueCadastrosService.locais.criar).mockReset()
    })

    it("bloqueia submit quando nome vazio", async () => {
        const w = montar()
        await botaoCriar(w).trigger("click")
        await flushPromises()
        expect(estoqueCadastrosService.locais.criar).not.toHaveBeenCalled()
        expect(w.text()).toContain("Nome é obrigatório.")
    })

    it("submete com defaults (tipo=Armario, andar/responsavel null) e emite 'criada'", async () => {
        vi.mocked(estoqueCadastrosService.locais.criar).mockResolvedValueOnce({ id: 9 })
        const w = montar()
        await w.findAll("input")[0].setValue("Armário Recepção")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.locais.criar).toHaveBeenCalledWith({
            nome: "Armário Recepção",
            tipo: "Armario",
            andarSetor: null,
            responsavel: null,
        })
        expect(w.emitted("criada")![0][0]).toEqual({ id: 9, nome: "Armário Recepção" })
    })

    it("envia andar/responsavel quando preenchidos", async () => {
        vi.mocked(estoqueCadastrosService.locais.criar).mockResolvedValueOnce({ id: 9 })
        const w = montar()
        const inputs = w.findAll("input")
        await inputs[0].setValue("Cofre 1")
        await inputs[1].setValue("2º andar")
        await inputs[2].setValue("Maria")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.locais.criar).toHaveBeenCalledWith({
            nome: "Cofre 1",
            tipo: "Armario",
            andarSetor: "2º andar",
            responsavel: "Maria",
        })
    })
})
