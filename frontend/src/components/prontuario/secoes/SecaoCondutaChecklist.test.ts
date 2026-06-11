import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import SecaoCondutaChecklist from "./SecaoCondutaChecklist.vue"

// Stubs simples para componentes de UI que não têm registro global nos testes
const AppTextareaStub = {
    name: "AppTextarea",
    template: `<textarea :value="modelValue" :disabled="disabled" @input="$emit('update:modelValue', $event.target.value)" />`,
    props: ["modelValue", "rows", "placeholder", "disabled"],
    emits: ["update:modelValue"],
}

const stubs = { AppTextarea: AppTextareaStub }

// ── Modo edição ───────────────────────────────────────────────────────────────

describe("SecaoCondutaChecklist — modo edição", () => {
    it("renderiza 6 checkboxes (ações fixas do sistema)", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: {}, readOnly: false },
        })
        const checkboxes = wrapper.findAll("input[type='checkbox']")
        expect(checkboxes).toHaveLength(6)
    })

    it("checkbox desmarcado por padrão quando acoesMarcadas está vazio", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: [] }, readOnly: false },
        })
        const checkboxes = wrapper.findAll<HTMLInputElement>("input[type='checkbox']")
        for (const cb of checkboxes) {
            expect(cb.element.checked).toBe(false)
        }
    })

    it("CriarReceita marcada quando presente em acoesMarcadas", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: ["CriarReceita"] }, readOnly: false },
        })
        const checkboxes = wrapper.findAll<HTMLInputElement>("input[type='checkbox']")
        // CriarReceita é o primeiro da lista
        expect(checkboxes[0].element.checked).toBe(true)
    })

    it("emite update:modelValue ao clicar num checkbox — adiciona ação", async () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: [] }, readOnly: false },
        })
        const primeiroCheckbox = wrapper.findAll("input[type='checkbox']")[0]
        await primeiroCheckbox.trigger("change")

        const emitidos = wrapper.emitted("update:modelValue")
        expect(emitidos).toBeTruthy()
        const valor = (emitidos![0][0] as { acoesMarcadas: string[] }).acoesMarcadas
        expect(valor).toContain("CriarReceita")
    })

    it("emite update:modelValue ao desmarcar — remove ação", async () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: ["CriarReceita"] }, readOnly: false },
        })
        const primeiroCheckbox = wrapper.findAll("input[type='checkbox']")[0]
        await primeiroCheckbox.trigger("change")

        const emitidos = wrapper.emitted("update:modelValue")
        expect(emitidos).toBeTruthy()
        const valor = (emitidos![0][0] as { acoesMarcadas: string[] }).acoesMarcadas
        expect(valor).not.toContain("CriarReceita")
    })
})

// ── Modo somente leitura ───────────────────────────────────────────────────────

describe("SecaoCondutaChecklist — modo readOnly", () => {
    it("checkboxes ficam disabled em readOnly=true", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: ["CriarReceita"] }, readOnly: true },
        })
        const checkboxes = wrapper.findAll<HTMLInputElement>("input[type='checkbox']")
        for (const cb of checkboxes) {
            expect(cb.element.disabled).toBe(true)
        }
    })

    it("exibe mensagem 'Nenhuma ação' se nenhuma marcada em readOnly", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: [] }, readOnly: true },
        })
        expect(wrapper.text()).toContain("Nenhuma ação de conduta registrada")
    })

    it("não exibe mensagem 'Nenhuma ação' quando há ações marcadas", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: { acoesMarcadas: ["AgendarRetorno"] }, readOnly: true },
        })
        expect(wrapper.text()).not.toContain("Nenhuma ação de conduta registrada")
    })
})

// ── Retrocompat: observação herdada ───────────────────────────────────────────

describe("SecaoCondutaChecklist — observacao", () => {
    it("renderiza observação existente no textarea", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: {
                modelValue: { acoesMarcadas: [], observacao: "Manter hidratação" },
                readOnly: true,
            },
        })
        const textarea = wrapper.find("textarea")
        expect(textarea.element.value).toBe("Manter hidratação")
    })

    it("modelValue sem observacao não quebra renderização (CA73 retrocompat)", () => {
        const wrapper = mount(SecaoCondutaChecklist, {
            global: { stubs },
            props: { modelValue: {}, readOnly: false },
        })
        // não lança e renderiza 6 checkboxes
        expect(wrapper.findAll("input[type='checkbox']")).toHaveLength(6)
    })
})
