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
    return { AppModal, AppButton, AppField, AppInput }
})

vi.mock("@/services/estoqueCadastrosService", async () => {
    const actual = await vi.importActual<any>("@/services/estoqueCadastrosService")
    return {
        ...actual,
        estoqueCadastrosService: {
            fabricantes: { criar: vi.fn() },
        },
    }
})

import ModalNovoFabricanteRapido from "./ModalNovoFabricanteRapido.vue"
import { estoqueCadastrosService } from "@/services/estoqueCadastrosService"

function montar() {
    return mount(ModalNovoFabricanteRapido, { props: { aberto: true } })
}

function botaoCriar(w: ReturnType<typeof montar>) {
    return w.findAll("[data-test='rodape'] button")[1]
}

describe("ModalNovoFabricanteRapido", () => {
    beforeEach(() => {
        vi.mocked(estoqueCadastrosService.fabricantes.criar).mockReset()
    })

    it("bloqueia submit quando nome vazio", async () => {
        const w = montar()
        await botaoCriar(w).trigger("click")
        await flushPromises()
        expect(estoqueCadastrosService.fabricantes.criar).not.toHaveBeenCalled()
        expect(w.text()).toContain("Nome é obrigatório.")
    })

    it("submete com nome + país (default Brasil) e emite 'criada'", async () => {
        vi.mocked(estoqueCadastrosService.fabricantes.criar).mockResolvedValueOnce({ id: 7 })
        const w = montar()
        await w.findAll("input")[0].setValue("Pfizer")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fabricantes.criar)
            .toHaveBeenCalledWith({ nome: "Pfizer", pais: "Brasil" })
        expect(w.emitted("criada")![0][0]).toEqual({ id: 7, nome: "Pfizer" })
    })

    it("envia pais=null quando usuário apaga o país", async () => {
        vi.mocked(estoqueCadastrosService.fabricantes.criar).mockResolvedValueOnce({ id: 7 })
        const w = montar()
        await w.findAll("input")[0].setValue("EMS")
        await w.findAll("input")[1].setValue("")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fabricantes.criar)
            .toHaveBeenCalledWith({ nome: "EMS", pais: null })
    })
})
