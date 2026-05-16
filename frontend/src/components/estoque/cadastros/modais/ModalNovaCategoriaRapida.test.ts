import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

vi.mock("@/components/ui", () => {
    const AppModal = {
        props: ["aberto", "titulo", "largura", "acimaDeDrawer"],
        emits: ["fechar"],
        template: `
            <div v-if="aberto" data-test="modal">
                <slot />
                <div data-test="rodape"><slot name="rodape" /></div>
            </div>
        `,
    }
    const AppButton = {
        props: ["variant", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
    }
    const AppField = {
        props: ["label", "required", "hint"],
        template: `<div><label>{{ label }}<span v-if="required">*</span></label><slot /></div>`,
    }
    const AppInput = {
        props: ["modelValue", "placeholder", "type"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" :placeholder="placeholder" :type="type || 'text'"
                          @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    return { AppModal, AppButton, AppField, AppInput }
})

vi.mock("@/services/estoqueCadastrosService", async () => {
    const actual = await vi.importActual<any>("@/services/estoqueCadastrosService")
    return {
        ...actual,
        estoqueCadastrosService: {
            categorias: { criar: vi.fn() },
        },
    }
})

import ModalNovaCategoriaRapida from "./ModalNovaCategoriaRapida.vue"
import { estoqueCadastrosService } from "@/services/estoqueCadastrosService"

function montar(props: Partial<{ aberto: boolean }> = {}) {
    return mount(ModalNovaCategoriaRapida, { props: { aberto: true, ...props } })
}

function botaoCriar(w: ReturnType<typeof montar>) {
    return w.findAll("[data-test='rodape'] button")[1]
}

describe("ModalNovaCategoriaRapida", () => {
    beforeEach(() => {
        vi.mocked(estoqueCadastrosService.categorias.criar).mockReset()
    })

    it("não submete quando nome está vazio (regra do form)", async () => {
        const w = montar()
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.categorias.criar).not.toHaveBeenCalled()
        expect(w.text()).toContain("Nome é obrigatório.")
    })

    it("chama service.criar e emite 'criada' com a opção retornada", async () => {
        vi.mocked(estoqueCadastrosService.categorias.criar).mockResolvedValueOnce({ id: 42 })
        const w = montar()
        await w.find("input").setValue("Vacinas")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.categorias.criar).toHaveBeenCalledOnce()
        const eventos = w.emitted("criada")
        expect(eventos).toBeTruthy()
        expect(eventos![0][0]).toEqual({ id: 42, nome: "Vacinas" })
    })

    it("exibe a mensagem do backend em erro 422", async () => {
        vi.mocked(estoqueCadastrosService.categorias.criar).mockRejectedValueOnce({
            response: { status: 422, data: { mensagem: "Já existe uma categoria com esse nome." } },
        })
        const w = montar()
        await w.find("input").setValue("Anestésicos")
        await botaoCriar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Já existe uma categoria com esse nome.")
        expect(w.emitted("criada")).toBeFalsy()
    })
})
