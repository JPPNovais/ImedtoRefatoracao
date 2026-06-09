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

    // CA20 — badge de vista
    it("CA20 — exibe badge de vista 'Circunferencial' quando vista = circunferencial", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, regiao_id: "torax-circunferencial", caminho: "Tórax (circunferencial)", vista: 'circunferencial' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Circunferencial")
    })

    it("CA20 — exibe badge de vista 'Anterior' quando vista = anterior", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, vista: 'anterior' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Anterior")
    })

    it("CA20 — exibe badge de vista 'Posterior' quando vista = posterior", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, vista: 'posterior' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Posterior")
    })

    // CA21 — membro circunferencial: badge de lado + badge de vista
    it("CA21 — membro circunferencial: exibe badge de lado (Direito) E badge de vista (Circunferencial)", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: {
                    ...regiaoBase,
                    regiao_id: "msd-circunferencial",
                    caminho: "Membro superior direito (circunferencial)",
                    lateralidade: 'D' as const,
                    vista: 'circunferencial' as const,
                },
                index: 0,
                open: true,
            },
        })
        const html = wrapper.html()
        expect(html).toContain("Direito")
        expect(html).toContain("Circunferencial")
    })

    it("CA20 — sem vista: nenhuma badge de vista exibida", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, vista: null },
                index: 0,
                open: true,
            },
        })
        // Texto "Anterior", "Posterior", "Circunferencial" não devem aparecer como badge
        // (podem aparecer no texto do textarea, mas aqui o regiaoBase.texto_exame = "Normal.")
        const html = wrapper.html()
        // Verificar que o label de vista não aparece no cabeçalho
        const header = wrapper.find('.rec-header')
        expect(header.html()).not.toContain("Anterior")
        expect(header.html()).not.toContain("Posterior")
        expect(header.html()).not.toContain("Circunferencial")
    })
})
