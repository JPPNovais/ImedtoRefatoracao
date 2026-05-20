import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import RegionExamCard from "./RegionExamCard.vue"

const regiaoBase = {
    regiao_id: "torax-anterior",
    caminho: "Tórax (anterior)",
    lateralidade: null as 'D' | 'E' | 'bilateral' | null,
    texto_exame: "Normal.",
    achados: "",
    observacoes: "",
    timestamp: new Date().toISOString(),
}

describe("RegionExamCard", () => {
    it("emite 'atualizar' com patch quando o usuário edita o texto do exame", async () => {
        const wrapper = mount(RegionExamCard, {
            props: { regiao: { ...regiaoBase }, index: 2, open: true },
        })

        const textarea = wrapper.findAll("textarea").find(t => t.attributes("placeholder")?.includes("Descreva"))
        expect(textarea).toBeTruthy()

        await textarea!.setValue("Murmúrio vesicular preservado bilateral.")

        const eventos = wrapper.emitted("atualizar")
        expect(eventos).toBeTruthy()
        expect(eventos![0]![0]).toEqual({
            index: 2,
            patch: { texto_exame: "Murmúrio vesicular preservado bilateral." },
        })
    })

    it("emite 'atualizar' com patch quando o usuário edita o campo achados", async () => {
        const wrapper = mount(RegionExamCard, {
            props: { regiao: { ...regiaoBase }, index: 0, open: true },
        })

        const input = wrapper.findAll("input").find(i => i.attributes("placeholder")?.includes("Normal"))
        expect(input).toBeTruthy()
        await input!.setValue("Sem alterações")

        const eventos = wrapper.emitted("atualizar")
        expect(eventos).toBeTruthy()
        expect(eventos![0]![0]).toEqual({
            index: 0,
            patch: { achados: "Sem alterações" },
        })
    })

    it("readonly impede que o botão de remover seja renderizado", () => {
        const wrapper = mount(RegionExamCard, {
            props: { regiao: { ...regiaoBase }, index: 0, readonly: true, open: true },
        })

        const remover = wrapper.findAll("button").find(b => b.html().includes("fa-xmark"))
        expect(remover).toBeFalsy()
    })

    it("propaga lateralidade no header (badge)", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, lateralidade: 'D' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Direito")
    })
})
