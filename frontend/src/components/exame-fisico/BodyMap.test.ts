/**
 * Testes de BodyMap.vue — atualizado para fusão estrutural do tronco (briefing 2026-06-25_002).
 * Mudanças:
 * - Pseudo-hotspots sintéticos removidos; tronco-anterior/tronco-posterior são hotspots reais.
 * - Evento troncoClicado removido; apenas regiaoClicada.
 * - CODIGOS_TRONCO e PARTE_PARA_TRONCO removidos.
 * - tronco-anterior/tronco-posterior passam por regioesComPath como qualquer outro nó.
 */
import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import BodyMap, { type ExameFisicoRegiao } from "./BodyMap.vue"

const fazRegiao = (partial: Partial<ExameFisicoRegiao> & { id: string }): ExameFisicoRegiao => ({
    nome: partial.id,
    nivel: 1,
    lateralidade: false,
    pai_id: null,
    vista: "anterior",
    template_texto: null,
    ...partial,
})

// Regiões com path real em bodyMapPaths (maleRegionPaths)
const regioesComPath: ExameFisicoRegiao[] = [
    fazRegiao({ id: "cabeca-anterior",  nome: "Cabeça (anterior)",  vista: "anterior" }),
    fazRegiao({ id: "pescoco-anterior", nome: "Pescoço (anterior)", vista: "anterior" }),
]

// tronco-anterior/tronco-posterior agora são hotspots reais (briefing 2026-06-25_002)
const regioesTronco: ExameFisicoRegiao[] = [
    fazRegiao({ id: "tronco-anterior",  nome: "Tronco (anterior)",  vista: "anterior"  }),
    fazRegiao({ id: "tronco-posterior", nome: "Tronco (posterior)", vista: "posterior" }),
]

describe("BodyMap", () => {
    it("renderiza paths de catálogo para regiões com entry em bodyMapPaths", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesComPath, regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        // cabeça + pescoço = 2 hotspots (sem pseudo-hotspot de tronco)
        expect(paths.length).toBe(2)
    })

    it("renderiza hotspots de tronco-anterior e tronco-posterior quando passados como regiões reais", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesTronco, regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        // Fusão: 2 regiões reais de tronco aparecem como hotspots normais
        expect(paths.length).toBe(2)
        const labels = paths.map(p => p.attributes("aria-label") ?? "")
        expect(labels).toContain("Tronco (anterior)")
        expect(labels).toContain("Tronco (posterior)")
    })

    it("não renderiza hotspot para regiões sem path em bodyMapPaths", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [fazRegiao({ id: "id-sem-path", nome: "Sem path" })],
                regioesExaminadas: [],
                sexo: "M",
            },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        expect(paths.length).toBe(0)
    })

    it("emite 'regiaoClicada' ao clicar em hotspot do catálogo (cabeça/pescoço)", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesComPath, regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        expect(paths.length).toBe(2)
        await paths[0]!.trigger("click")

        const eventos = wrapper.emitted("regiaoClicada")
        expect(eventos).toBeTruthy()
        const idEmitido = (eventos![0]![0] as { id: string }).id
        expect(["cabeca-anterior", "pescoco-anterior"]).toContain(idEmitido)
    })

    it("emite 'regiaoClicada' ao clicar em hotspot de tronco-anterior (região real)", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesTronco, regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        const troncoAntPath = paths.find(p => p.attributes("aria-label") === "Tronco (anterior)")
        expect(troncoAntPath).toBeTruthy()
        await troncoAntPath!.trigger("click")

        const eventos = wrapper.emitted("regiaoClicada")
        expect(eventos).toBeTruthy()
        const emitido = eventos![0]![0] as ExameFisicoRegiao
        expect(emitido.id).toBe("tronco-anterior")
    })

    it("não emite evento obsoleto 'troncoClicado' (removido na fusão 2026-06-25_002)", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesTronco, regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        await paths[0]!.trigger("click")
        // troncoClicado foi removido — só regiaoClicada deve existir
        expect(wrapper.emitted("troncoClicado")).toBeFalsy()
        expect(wrapper.emitted("regiaoClicada")).toBeTruthy()
    })

    // CA3/CA4/CA5 — coloração por vista
    it("CA3 — hotspot aceso com vista anterior recebe classe 'region-selected-ant'", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesComPath,
                regioesExaminadas: ["cabeca-anterior"],
                vistasPorId: { "cabeca-anterior": "anterior" },
                sexo: "M",
            },
        })
        expect(wrapper.findAll("path.region-selected-ant").length).toBeGreaterThan(0)
        expect(wrapper.findAll("path.region-selected").length).toBe(0)
    })

    it("CA4 — hotspot aceso com vista posterior recebe classe 'region-selected-post'", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesComPath,
                regioesExaminadas: ["cabeca-anterior"],
                vistasPorId: { "cabeca-anterior": "posterior" },
                sexo: "M",
            },
        })
        expect(wrapper.findAll("path.region-selected-post").length).toBeGreaterThan(0)
    })

    it("CA5 — hotspot aceso com vista circunferencial recebe classe 'region-selected-circ'", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesComPath,
                regioesExaminadas: ["cabeca-anterior"],
                vistasPorId: { "cabeca-anterior": "circunferencial" },
                sexo: "M",
            },
        })
        expect(wrapper.findAll("path.region-selected-circ").length).toBeGreaterThan(0)
    })

    it("CA5 — tronco real: ambos hotspots recebem region-selected-circ quando vistasPorId='circunferencial'", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesTronco,
                regioesExaminadas: ["tronco-anterior", "tronco-posterior"],
                vistasPorId: {
                    "tronco-anterior":  "circunferencial",
                    "tronco-posterior": "circunferencial",
                },
                sexo: "M",
            },
        })
        const acesosCirc = wrapper.findAll("path.region-selected-circ")
        expect(acesosCirc.length).toBe(2)
        const labels = acesosCirc.map(p => p.attributes("aria-label"))
        expect(labels).toContain("Tronco (anterior)")
        expect(labels).toContain("Tronco (posterior)")
        expect(wrapper.findAll("path.region-selected-ant").length).toBe(0)
        expect(wrapper.findAll("path.region-selected-post").length).toBe(0)
    })

    it("tronco anterior puro = region-selected-ant, posterior puro = region-selected-post", () => {
        const wrapperAnt = mount(BodyMap, {
            props: {
                regioes: regioesTronco,
                regioesExaminadas: ["tronco-anterior"],
                vistasPorId: { "tronco-anterior": "anterior" },
                sexo: "M",
            },
        })
        expect(wrapperAnt.findAll("path.region-selected-ant").length).toBe(1)
        expect(wrapperAnt.findAll("path.region-selected-circ").length).toBe(0)

        const wrapperPost = mount(BodyMap, {
            props: {
                regioes: regioesTronco,
                regioesExaminadas: ["tronco-posterior"],
                vistasPorId: { "tronco-posterior": "posterior" },
                sexo: "M",
            },
        })
        expect(wrapperPost.findAll("path.region-selected-post").length).toBe(1)
        expect(wrapperPost.findAll("path.region-selected-circ").length).toBe(0)
    })

    it("a <image> de fundo tem pointer-events='none' para não bloquear cliques", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: [], regioesExaminadas: [], sexo: "F" },
        })
        const img = wrapper.find("image")
        expect(img.exists()).toBe(true)
        expect(img.attributes("pointer-events")).toBe("none")
    })

    // CA8 — legenda visível abaixo do mapa
    it("CA8 — renderiza legenda com Anterior, Posterior e Circunferencial", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: [], regioesExaminadas: [], sexo: "M" },
        })
        const html = wrapper.html()
        expect(html).toContain("Anterior")
        expect(html).toContain("Posterior")
        expect(html).toContain("Circunferencial")
        expect(wrapper.find(".legenda-dot--ant").exists()).toBe(true)
        expect(wrapper.find(".legenda-dot--post").exists()).toBe(true)
        expect(wrapper.find(".legenda-dot--circ").exists()).toBe(true)
    })

    it("fallback — vistasPorId ausente: hotspot aceso usa classe anterior", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesComPath,
                regioesExaminadas: ["cabeca-anterior"],
                sexo: "M",
            },
        })
        expect(wrapper.findAll("path.region-selected-ant").length).toBeGreaterThan(0)
    })
})
