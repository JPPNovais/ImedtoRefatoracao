import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import BodyMap from "./BodyMap.vue"

// Regiões mínimas (nível-1) com nomes que existem em bodyMapPaths.ts.
const regioes = [
    {
        id: "torax-anterior",
        nome: "Tórax (anterior)",
        nivel: 1 as const,
        lateralidade: false,
        pai_id: null,
        vista: "anterior" as const,
        template_texto: null,
    },
    {
        id: "abdome-anterior",
        nome: "Abdome (anterior)",
        nivel: 1 as const,
        lateralidade: false,
        pai_id: null,
        vista: "anterior" as const,
        template_texto: null,
    },
]

describe("BodyMap", () => {
    it("renderiza um <path> por região conhecida em bodyMapPaths", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes, regioesExaminadas: [], sexo: "M" },
        })

        const paths = wrapper.findAll("path.region-hotspot")
        expect(paths.length).toBe(2)
    })

    it("emite 'regiaoClicada' com a região correta ao clicar no path", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes, regioesExaminadas: [], sexo: "M" },
        })

        const paths = wrapper.findAll("path.region-hotspot")
        await paths[0]!.trigger("click")

        const eventos = wrapper.emitted("regiaoClicada")
        expect(eventos).toBeTruthy()
        // A ordem renderizada respeita zOrder do bodyMapPaths — não comparamos
        // qual veio primeiro, mas garantimos que o id emitido bate com uma
        // das regiões fornecidas.
        const idEmitido = (eventos![0]![0] as { id: string }).id
        expect(["torax-anterior", "abdome-anterior"]).toContain(idEmitido)
    })

    it("aplica classe 'region-selected' quando a região está em regioesExaminadas", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes,
                regioesExaminadas: ["torax-anterior"],
                sexo: "M",
            },
        })

        // Pelo menos um path deve ter a classe 'region-selected'.
        const selecionados = wrapper.findAll("path.region-selected")
        expect(selecionados.length).toBe(1)
    })

    it("a <image> de fundo tem pointer-events='none' para não bloquear cliques", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes, regioesExaminadas: [], sexo: "F" },
        })

        const img = wrapper.find("image")
        expect(img.exists()).toBe(true)
        expect(img.attributes("pointer-events")).toBe("none")
    })
})
