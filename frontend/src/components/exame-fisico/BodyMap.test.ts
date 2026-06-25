import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import BodyMap from "./BodyMap.vue"

// Regiões mínimas (nível-1) que existem em bodyMapPaths.ts.
// Nota: Tórax/Abdome/Pelve (anterior/posterior) foram removidos como hotspots (B2 — fusão do tronco).
// O mapa agora renderiza pseudo-hotspots de tronco internamente ("Tronco (anterior/posterior)").
const regioesComPath = [
    {
        id: "cabeca-anterior",
        nome: "Cabeça (anterior)",
        nivel: 1 as const,
        lateralidade: false,
        pai_id: null,
        vista: "anterior" as const,
        template_texto: null,
    },
    {
        id: "pescoco-anterior",
        nome: "Pescoço (anterior)",
        nivel: 1 as const,
        lateralidade: false,
        pai_id: null,
        vista: "anterior" as const,
        template_texto: null,
    },
]

// Regiões de partes do tronco (ex-hotspots): devem ser filtradas para fora
// pelo BodyMap (não são mais clicáveis), já que o tronco fundido usa pseudo-hotspots.
const regioesTroncoPartes = [
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
    it("renderiza paths de catálogo para regiões com entry em bodyMapPaths (excluindo tronco-partes)", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesComPath, regioesExaminadas: [], sexo: "M" },
        })
        // 2 paths do catálogo (cabeça + pescoço) + 2 pseudo-hotspots de tronco
        const paths = wrapper.findAll("path.region-hotspot")
        expect(paths.length).toBe(4)
    })

    it("não renderiza hotspots clicáveis para Tórax/Abdome/Pelve (fusão B2 — viraram pseudo-hotspot)", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesTroncoPartes, regioesExaminadas: [], sexo: "M" },
        })
        // regioesTroncoPartes são filtradas pelo BodyMap (ids em CODIGOS_TRONCO).
        // Somente os 2 pseudo-hotspots de tronco devem aparecer.
        const paths = wrapper.findAll("path.region-hotspot")
        expect(paths.length).toBe(2)
    })

    it("emite 'regiaoClicada' ao clicar em hotspot do catálogo (cabeça/pescoço/membro)", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: regioesComPath, regioesExaminadas: [], sexo: "M" },
        })
        // Os 2 pseudo-hotspots de tronco vêm antes (zOrder 0); os de catálogo depois.
        // A ordem no DOM é: [tronco-ant, tronco-post, cabeça, pescoço] (tronco renderizado primeiro).
        const paths = wrapper.findAll("path.region-hotspot")
        // Clicar no 3º path (índice 2) → cabeça ou pescoço
        await paths[2]!.trigger("click")

        const eventos = wrapper.emitted("regiaoClicada")
        expect(eventos).toBeTruthy()
        const idEmitido = (eventos![0]![0] as { id: string }).id
        expect(["cabeca-anterior", "pescoco-anterior"]).toContain(idEmitido)
    })

    it("emite 'troncoClicado' com 'tronco-anterior' ao clicar no pseudo-hotspot de tronco anterior", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: [], regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        // Com regioes=[], só existem 2 pseudo-hotspots: [tronco-ant, tronco-post]
        expect(paths.length).toBe(2)
        await paths[0]!.trigger("click")

        const eventos = wrapper.emitted("troncoClicado")
        expect(eventos).toBeTruthy()
        expect(eventos![0]![0]).toBe("tronco-anterior")
    })

    it("emite 'troncoClicado' com 'tronco-posterior' ao clicar no pseudo-hotspot de tronco posterior", async () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: [], regioesExaminadas: [], sexo: "M" },
        })
        const paths = wrapper.findAll("path.region-hotspot")
        await paths[1]!.trigger("click")

        const eventos = wrapper.emitted("troncoClicado")
        expect(eventos).toBeTruthy()
        expect(eventos![0]![0]).toBe("tronco-posterior")
    })

    // CA3/CA4/CA5 — coloração por vista substitui region-selected uniforme
    it("CA3 — hotspot aceso com vista anterior recebe classe 'region-selected-ant' (não region-selected)", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesComPath,
                regioesExaminadas: ["cabeca-anterior"],
                vistasPorId: { "cabeca-anterior": "anterior" },
                sexo: "M",
            },
        })
        const acesos = wrapper.findAll("path.region-selected-ant")
        expect(acesos.length).toBeGreaterThan(0)
        // Não deve haver region-selected uniforme antigo
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
        const acesos = wrapper.findAll("path.region-selected-post")
        expect(acesos.length).toBeGreaterThan(0)
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
        const acesos = wrapper.findAll("path.region-selected-circ")
        expect(acesos.length).toBeGreaterThan(0)
    })

    // Tronco usa classe de vista padrão (anterior/posterior) pelo lado do hotspot
    it("aplica classe 'region-selected-ant' no pseudo-hotspot de tronco anterior quando parte examinada (OU das partes)", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [],
                // torax-anterior examinada → PARTE_PARA_TRONCO → 'Tronco (anterior)' deve acender
                regioesExaminadas: ["torax-anterior"],
                sexo: "M",
            },
        })
        const selecionados = wrapper.findAll("path.region-selected-ant")
        // Deve haver exatamente 1 path aceso: o tronco anterior
        expect(selecionados.length).toBe(1)
        expect(selecionados[0]!.attributes("aria-label")).toBe("Tronco (anterior)")
    })

    it("aplica classe 'region-selected-post' no pseudo-hotspot posterior quando lombossacra examinada", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [],
                regioesExaminadas: ["lombossacra-posterior"],
                sexo: "M",
            },
        })
        const selecionados = wrapper.findAll("path.region-selected-post")
        expect(selecionados.length).toBe(1)
        expect(selecionados[0]!.attributes("aria-label")).toBe("Tronco (posterior)")
    })

    it("acende ambos os polígonos de tronco quando há examinada em ambos os lados", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [],
                regioesExaminadas: ["torax-anterior", "torax-posterior"],
                sexo: "F",
            },
        })
        // Um tronco anterior + um tronco posterior
        const acesosAnt  = wrapper.findAll("path.region-selected-ant")
        const acesosPost = wrapper.findAll("path.region-selected-post")
        expect(acesosAnt.length + acesosPost.length).toBe(2)
    })

    // CA5 — tronco circunferencial: ambos os pseudo-hotspots devem acender em âmbar
    it("CA5 — tronco circunferencial: ambos pseudo-hotspots recebem region-selected-circ quando vistasPorId passa 'circunferencial'", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [],
                regioesExaminadas: ["torax-anterior", "torax-posterior"],
                vistasPorId: {
                    "tronco-anterior": "circunferencial",
                    "tronco-posterior": "circunferencial",
                },
                sexo: "M",
            },
        })
        const acesosCirc = wrapper.findAll("path.region-selected-circ")
        // Ambos os pseudo-hotspots devem ser âmbar
        expect(acesosCirc.length).toBe(2)
        const labels = acesosCirc.map(p => p.attributes("aria-label"))
        expect(labels).toContain("Tronco (anterior)")
        expect(labels).toContain("Tronco (posterior)")
        // Não deve haver ant/post acesos ao mesmo tempo
        expect(wrapper.findAll("path.region-selected-ant").length).toBe(0)
        expect(wrapper.findAll("path.region-selected-post").length).toBe(0)
    })

    // Não-regressão: tronco anterior puro = azul, posterior puro = violeta
    it("não-regressão CA5 — tronco anterior puro = azul (ant), posterior puro = violeta (post)", () => {
        const wrapperAnt = mount(BodyMap, {
            props: {
                regioes: [],
                regioesExaminadas: ["torax-anterior"],
                vistasPorId: { "tronco-anterior": "anterior" },
                sexo: "M",
            },
        })
        expect(wrapperAnt.findAll("path.region-selected-ant").length).toBe(1)
        expect(wrapperAnt.findAll("path.region-selected-circ").length).toBe(0)

        const wrapperPost = mount(BodyMap, {
            props: {
                regioes: [],
                regioesExaminadas: ["torax-posterior"],
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
        // Pontos coloridos de cada vista
        expect(wrapper.find(".legenda-dot--ant").exists()).toBe(true)
        expect(wrapper.find(".legenda-dot--post").exists()).toBe(true)
        expect(wrapper.find(".legenda-dot--circ").exists()).toBe(true)
    })

    // vistasPorId ausente → hotspot aceso usa fallback (region-selected-ant)
    it("fallback — vistasPorId ausente: hotspot aceso usa classe anterior (sem quebrar usos legados)", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: regioesComPath,
                regioesExaminadas: ["cabeca-anterior"],
                // vistasPorId não passado
                sexo: "M",
            },
        })
        // Fallback é anterior
        expect(wrapper.findAll("path.region-selected-ant").length).toBeGreaterThan(0)
    })
})
