import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import AppInputDecimal from "./AppInputDecimal.vue"

describe("AppInputDecimal", () => {
    it("preenche da direita (1 casa): '705' ⇒ exibe 70,5 e emite '70.5'", async () => {
        const wrapper = mount(AppInputDecimal, { props: { decimals: 1 } })
        const input = wrapper.find("input")

        await input.setValue("705")

        expect((input.element as HTMLInputElement).value).toBe("70,5")
        expect(wrapper.emitted("update:modelValue")!.at(-1)![0]).toBe("70.5")
    })

    it("digitação incremental com 1 casa: 3 → 6 → 5 vira 0,3 → 3,6 → 36,5", async () => {
        const wrapper = mount(AppInputDecimal, { props: { decimals: 1 } })
        const input = wrapper.find("input")

        await input.setValue("3")
        expect((input.element as HTMLInputElement).value).toBe("0,3")
        await input.setValue("0,36") // estado após digitar o "6" no fim
        expect((input.element as HTMLInputElement).value).toBe("3,6")
        await input.setValue("3,65")
        expect((input.element as HTMLInputElement).value).toBe("36,5")
    })

    it("2 casas (altura): '170' ⇒ 1,70 e emite '1.70'", async () => {
        const wrapper = mount(AppInputDecimal, { props: { decimals: 2 } })
        const input = wrapper.find("input")

        await input.setValue("170")

        expect((input.element as HTMLInputElement).value).toBe("1,70")
        expect(wrapper.emitted("update:modelValue")!.at(-1)![0]).toBe("1.70")
    })

    it("apagar tudo emite string vazia", async () => {
        const wrapper = mount(AppInputDecimal, { props: { decimals: 1, modelValue: "70.5" } })
        const input = wrapper.find("input")

        await input.setValue("")

        expect(wrapper.emitted("update:modelValue")!.at(-1)![0]).toBe("")
    })

    it("hidrata o display a partir do model-value com ponto decimal", () => {
        const wrapper = mount(AppInputDecimal, { props: { decimals: 1, modelValue: "36.5" } })
        expect((wrapper.find("input").element as HTMLInputElement).value).toBe("36,5")
    })
})
