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

        const campo = wrapper.findAll("textarea").find(t => t.attributes("placeholder")?.includes("Normal"))
        expect(campo).toBeTruthy()
        await campo!.setValue("Sem alterações")

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

    // CA9/CA20 — badge de vista colorida pelo token --vista-*
    it("CA9 — badge de vista 'Circunferencial' usa classe CSS rec-badge-vista--circunferencial", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, regiao_id: "torax-circunferencial", caminho: "Tórax (circunferencial)", vista: 'circunferencial' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Circunferencial")
        expect(wrapper.find('.rec-badge-vista--circunferencial').exists()).toBe(true)
    })

    it("CA9 — badge de vista 'Anterior' usa classe CSS rec-badge-vista--anterior", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, vista: 'anterior' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Anterior")
        expect(wrapper.find('.rec-badge-vista--anterior').exists()).toBe(true)
    })

    it("CA9 — badge de vista 'Posterior' usa classe CSS rec-badge-vista--posterior", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, vista: 'posterior' as const },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.html()).toContain("Posterior")
        expect(wrapper.find('.rec-badge-vista--posterior').exists()).toBe(true)
    })

    // CA9 — badge de lado deve ser neutra (não usa classe de vista)
    it("CA9 — badge de lado usa classe rec-badge-lado (neutra, sem cor de vista)", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, lateralidade: 'bilateral' as const, vista: 'posterior' as const },
                index: 0,
                open: true,
            },
        })
        const badgeLado = wrapper.find('.rec-badge-lado')
        expect(badgeLado.exists()).toBe(true)
        expect(badgeLado.text()).toBe('Bilateral')
    })

    // CA21 — membro circunferencial: badge de lado + badge de vista
    it("CA21 — membro circunferencial: exibe badge de lado (Direito) E badge de vista (Circunferencial) com cor", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: {
                    ...regiaoBase,
                    regiao_id: "membro-superior-direito-circunferencial",
                    caminho: "Membro superior direito (circunferencial)",
                    lateralidade: 'D' as const,
                    vista: 'circunferencial' as const,
                },
                index: 0,
                open: true,
            },
        })
        expect(wrapper.find('.rec-badge-lado').text()).toBe('Direito')
        expect(wrapper.find('.rec-badge-vista--circunferencial').exists()).toBe(true)
    })

    it("CA20 — sem vista: nenhuma badge de vista exibida", () => {
        const wrapper = mount(RegionExamCard, {
            props: {
                regiao: { ...regiaoBase, vista: null },
                index: 0,
                open: true,
            },
        })
        // Verificar que o header não exibe badges de vista
        const header = wrapper.find('.rec-header')
        expect(header.find('.rec-badge-vista--anterior').exists()).toBe(false)
        expect(header.find('.rec-badge-vista--posterior').exists()).toBe(false)
        expect(header.find('.rec-badge-vista--circunferencial').exists()).toBe(false)
    })
})
