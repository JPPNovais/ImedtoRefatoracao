import { describe, it, expect, beforeAll } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import TermoEditorTipTap from "./TermoEditorTipTap.vue"

// JSDOM/happy-dom não implementa Range/Selection completas que o ProseMirror usa.
// Stub mínimo para o editor inicializar sem explodir.
beforeAll(() => {
    if (!(globalThis as any).document.createRange) {
        (globalThis as any).document.createRange = () => ({
            setStart: () => undefined,
            setEnd: () => undefined,
            commonAncestorContainer: { nodeName: "BODY", ownerDocument: document },
            getBoundingClientRect: () => ({ left: 0, top: 0, right: 0, bottom: 0, width: 0, height: 0 }),
        })
    }
})

describe("TermoEditorTipTap", () => {
    it("monta com toolbar e expõe o método inserirVariavel", async () => {
        const wrapper = mount(TermoEditorTipTap, {
            props: { modelValue: "<p>Olá</p>" },
        })
        await flushPromises()

        // Toolbar com pelo menos os botões principais
        const botoes = wrapper.findAll(".tb-btn")
        expect(botoes.length).toBeGreaterThan(5)

        // API pública pra view-pai chamar
        expect(typeof (wrapper.vm as any).inserirVariavel).toBe("function")
    })

    it("respeita o estado disabled", async () => {
        const wrapper = mount(TermoEditorTipTap, {
            props: { modelValue: "", disabled: true },
        })
        await flushPromises()

        expect(wrapper.classes()).toContain("is-disabled")
        // Toolbar todos disabled quando o editor está disabled.
        const habilitados = wrapper.findAll(".tb-btn:not([disabled])")
        expect(habilitados.length).toBe(0)
    })
})
