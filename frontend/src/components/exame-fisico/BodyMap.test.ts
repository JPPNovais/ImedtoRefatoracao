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
        // regioesTroncoPartes são filtradas pelo BodyMap (nomes no NOMES_TRONCO).
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

    it("aplica classe 'region-selected' no pseudo-hotspot de tronco quando parte anterior examinada (OU das partes)", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [],
                // torax-anterior examinada → PARTE_PARA_TRONCO → 'Tronco (anterior)' deve acender
                regioesExaminadas: ["torax-anterior"],
                sexo: "M",
            },
        })
        const selecionados = wrapper.findAll("path.region-selected")
        // Deve haver exatamente 1 path aceso: o tronco anterior
        expect(selecionados.length).toBe(1)
        expect(selecionados[0]!.attributes("aria-label")).toBe("Tronco (anterior)")
    })

    it("aplica classe 'region-selected' no pseudo-hotspot posterior quando lombossacra examinada", () => {
        const wrapper = mount(BodyMap, {
            props: {
                regioes: [],
                regioesExaminadas: ["lombossacra-posterior"],
                sexo: "M",
            },
        })
        const selecionados = wrapper.findAll("path.region-selected")
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
        const selecionados = wrapper.findAll("path.region-selected")
        expect(selecionados.length).toBe(2)
    })

    it("a <image> de fundo tem pointer-events='none' para não bloquear cliques", () => {
        const wrapper = mount(BodyMap, {
            props: { regioes: [], regioesExaminadas: [], sexo: "F" },
        })
        const img = wrapper.find("image")
        expect(img.exists()).toBe(true)
        expect(img.attributes("pointer-events")).toBe("none")
    })
})
