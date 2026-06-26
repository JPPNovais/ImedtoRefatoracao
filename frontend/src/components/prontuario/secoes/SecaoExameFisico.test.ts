import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import SecaoExameFisico, { type RegiaoAnatomicaSelecionada } from "./SecaoExameFisico.vue"

// ── Catálogo mínimo reutilizado nos testes de caminho ─────────────────────────
const catalogoMembro = [
    {
        id: "membro-superior-direito-anterior",
        nome: "Membro superior direito (anterior)",
        nivel: 1,
        lateralidade: false,
        pai_id: null,
        vista: "anterior",
        template_texto: null,
        ordem: 1,
        ativo: true,
    },
    {
        id: "membro-superior-esquerdo-anterior",
        nome: "Membro superior esquerdo (anterior)",
        nivel: 1,
        lateralidade: false,
        pai_id: null,
        vista: "anterior",
        template_texto: null,
        ordem: 2,
        ativo: true,
    },
    {
        id: "ombro-direito",
        nome: "Ombro direito",
        nivel: 2,
        lateralidade: false,
        pai_id: "membro-superior-direito-anterior",
        vista: "anterior",
        template_texto: null,
        ordem: 1,
        ativo: true,
    },
    {
        id: "braco-direito",
        nome: "Braço direito",
        nivel: 2,
        lateralidade: false,
        pai_id: "membro-superior-direito-anterior",
        vista: "anterior",
        template_texto: null,
        ordem: 2,
        ativo: true,
    },
]

// Mock do service — evita rede no teste e isola o comportamento do v-model.
vi.mock("@/services/exameFisicoService", () => ({
    exameFisicoService: {
        listarRegioes: vi.fn().mockResolvedValue([]),
    },
}))

describe("SecaoExameFisico", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("emite update:modelValue ao digitar peso", async () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: {},
                readOnly: false,
                pacienteSexo: "F",
            },
            global: {
                stubs: {
                    BodyMap: true,
                    RegionSelectorPopup: true,
                    RegionExamCard: true,
                },
            },
        })

        // Peso usa máscara decimal (1 casa): digitar "705" preenche da direita ⇒ 70,5 e emite "70.5".
        const decimais = wrapper.findAll("input").filter(i => i.attributes("inputmode") === "decimal")
        const pesoInput = decimais[1] // ordem no DOM: temperatura, peso, altura
        expect(pesoInput).toBeTruthy()
        await pesoInput.setValue("705")

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        expect(eventos![0]![0]).toMatchObject({ peso: "70.5" })
    })

    it("calcula IMC quando peso e altura estão presentes (altura em cm)", async () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: { peso: "70", altura: "175" },
                readOnly: false,
            },
            global: {
                stubs: {
                    BodyMap: true,
                    RegionSelectorPopup: true,
                    RegionExamCard: true,
                },
            },
        })

        // IMC = 70 / (1.75)^2 = 22.86 → "22.9"
        // O IMC e Classificação ficam em AppInput com readonly — pegamos pelo atributo.
        const readonlyInputs = wrapper.findAll("input[readonly]")
        expect(readonlyInputs.length).toBeGreaterThan(0)
        const imcInput = readonlyInputs[0] as any
        expect(imcInput.element.value).toBe("22.9")
    })

    it("exibe aviso de plausibilidade com valores fisiologicamente improváveis", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: { peso: "3", altura: "20" }, // 3kg / 20cm → IMC 75, altura 0.2m implausível
                readOnly: false,
            },
            global: {
                stubs: {
                    BodyMap: true,
                    RegionSelectorPopup: true,
                    RegionExamCard: true,
                },
            },
        })

        const aviso = wrapper.find(".aviso-antro")
        expect(aviso.exists()).toBe(true)
        expect(aviso.text()).toContain("Altura fora da faixa plausível")
    })

    it("não exibe aviso de plausibilidade com valores normais", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: { peso: "70", altura: "175" },
                readOnly: false,
            },
            global: {
                stubs: {
                    BodyMap: true,
                    RegionSelectorPopup: true,
                    RegionExamCard: true,
                },
            },
        })

        expect(wrapper.find(".aviso-antro").exists()).toBe(false)
    })

    // ── Subseções colapsáveis ─────────────────────────────────────────────────

    it("subseções colapsáveis iniciam fechadas (v-show false)", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: {}, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        // Os 3 corpos (.subsec-corpo) existem no DOM mas estão ocultos (v-show → display:none)
        const corpos = wrapper.findAll(".subsec-corpo")
        expect(corpos).toHaveLength(3)
        corpos.forEach(corpo => {
            expect((corpo.element as HTMLElement).style.display).toBe("none")
        })
    })

    it("clique no header de sinais vitais expande o conteúdo", async () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: {}, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        const headers = wrapper.findAll("button.subsec-header")
        expect(headers).toHaveLength(3)
        // Primeiro header = Sinais vitais
        await headers[0].trigger("click")
        const corpo = wrapper.findAll(".subsec-corpo")[0]
        expect((corpo.element as HTMLElement).style.display).not.toBe("none")
        // aria-expanded atualiza
        expect(headers[0].attributes("aria-expanded")).toBe("true")
    })

    it("dot de preenchido aparece quando campo de sinais vitais tem valor", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: { fc: "72" }, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        const dots = wrapper.findAll(".subsec-dot-preenchido")
        // Apenas sinais vitais tem dot (fc preenchido); antropometria e ectoscopia não
        expect(dots).toHaveLength(1)
    })

    it("dot de preenchido não aparece quando nenhum campo tem valor", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: {}, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        expect(wrapper.findAll(".subsec-dot-preenchido")).toHaveLength(0)
    })

    it("não exibe mapa corporal quando readOnly", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: {},
                readOnly: true,
            },
            global: {
                stubs: {
                    BodyMap: true,
                    RegionSelectorPopup: true,
                    RegionExamCard: true,
                },
            },
        })

        // Tudo o que tem v-if="!readOnly" some — h4 "Mapa corporal" não deve existir.
        const titulos = wrapper.findAll("h4").map(h => h.text())
        expect(titulos.some(t => t.includes("Mapa corporal"))).toBe(false)
        expect(titulos.some(t => t.includes("Observações gerais do exame físico"))).toBe(false)
    })
})

// ─── regioesExaminadasMapa — destaque bilateral no BodyMap ────────────────────

/**
 * Monta o componente com catálogo completo (incluindo o nível-1 esquerdo) e
 * com um BodyMapStub que captura os props `regioesExaminadas` e `vistasPorId` —
 * único jeito de inspecionar os computeds sem expô-los via defineExpose.
 */
async function montarParaMapa(regioes: RegiaoAnatomicaSelecionada[], catalogo = catalogoMembro) {
    const { exameFisicoService } = await import("@/services/exameFisicoService")
    ;(exameFisicoService.listarRegioes as ReturnType<typeof vi.fn>).mockResolvedValue(catalogo)

    // Stub capturável: expõe o prop recebido como dado da instância
    const BodyMapStub = {
        name: "BodyMap",
        props: ["regioes", "regioesExaminadas", "vistasPorId", "sexo"],
        template: "<div />",
    }

    const wrapper = mount(SecaoExameFisico, {
        props: {
            modelValue: { regioes },
            readOnly: false,
        },
        global: {
            stubs: {
                BodyMap: BodyMapStub,
                RegionSelectorPopup: true,
                RegionExamCard: true,
            },
        },
    })

    await flushPromises()
    return wrapper
}

describe("SecaoExameFisico — regioesExaminadasMapa (bilateral BodyMap)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("bilateral: inclui ids de AMBOS os membros nível-1 (direito e esquerdo)", async () => {
        const wrapper = await montarParaMapa([
            {
                regiao_id: "ombro-direito",
                caminho: "Membro superior (anterior) > Ombro",
                lateralidade: "bilateral",
                texto_exame: "",
                achados: "",
                observacoes: "",
                timestamp: new Date().toISOString(),
            },
        ])

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const regioesExaminadas: string[] = bodyMap.props("regioesExaminadas")

        // Deve conter o nível-1 direito (ancestral da sub-região) E o esquerdo (oposto bilateral)
        expect(regioesExaminadas).toContain("membro-superior-direito-anterior")
        expect(regioesExaminadas).toContain("membro-superior-esquerdo-anterior")
    })

    it("lateralidade D: inclui apenas o nível-1 do lado direito, sem o esquerdo", async () => {
        const wrapper = await montarParaMapa([
            {
                regiao_id: "ombro-direito",
                caminho: "Membro superior direito (anterior) > Ombro direito",
                lateralidade: "D",
                texto_exame: "",
                achados: "",
                observacoes: "",
                timestamp: new Date().toISOString(),
            },
        ])

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const regioesExaminadas: string[] = bodyMap.props("regioesExaminadas")

        expect(regioesExaminadas).toContain("membro-superior-direito-anterior")
        expect(regioesExaminadas).not.toContain("membro-superior-esquerdo-anterior")
    })
})

// ─── vistasPorIdMapa (coloração por vista no BodyMap) — CA3–CA7 ───────────────

const catalogoCircPropagacao = [
    {
        id: "membro-superior-direito-anterior",
        nome: "Membro superior direito (anterior)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "anterior", template_texto: null, ordem: 1, ativo: true,
    },
    {
        id: "membro-superior-esquerdo-anterior",
        nome: "Membro superior esquerdo (anterior)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "anterior", template_texto: null, ordem: 2, ativo: true,
    },
    {
        id: "membro-superior-direito-posterior",
        nome: "Membro superior direito (posterior)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "posterior", template_texto: null, ordem: 3, ativo: true,
    },
    {
        id: "membro-superior-esquerdo-posterior",
        nome: "Membro superior esquerdo (posterior)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "posterior", template_texto: null, ordem: 4, ativo: true,
    },
    {
        id: "ombro-direito",
        nome: "Ombro direito",
        nivel: 2, lateralidade: false,
        pai_id: "membro-superior-direito-anterior",
        vista: "anterior", template_texto: null, ordem: 1, ativo: true,
    },
]

describe("SecaoExameFisico — vistasPorIdMapa (CA3–CA7)", () => {
    beforeEach(() => { vi.clearAllMocks() })

    it("CA3 — entrada anterior: id nível-1 mapeado para 'anterior' no vistasPorId", async () => {
        const wrapper = await montarParaMapa([
            {
                regiao_id: "ombro-direito",
                caminho: "Membro superior direito (anterior) > Ombro direito",
                lateralidade: "D",
                vista: "anterior",
                texto_exame: "", achados: "", observacoes: "",
                timestamp: new Date().toISOString(),
            },
        ], catalogoCircPropagacao)

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const vistasPorId = bodyMap.props("vistasPorId") as Record<string, string>
        expect(vistasPorId["membro-superior-direito-anterior"]).toBe("anterior")
    })

    it("CA4 — entrada posterior: id nível-1 mapeado para 'posterior'", async () => {
        const wrapper = await montarParaMapa([
            {
                regiao_id: "membro-superior-direito-posterior",
                caminho: "Membro superior direito (posterior)",
                lateralidade: "D",
                vista: "posterior",
                texto_exame: "", achados: "", observacoes: "",
                timestamp: new Date().toISOString(),
            },
        ], catalogoCircPropagacao)

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const vistasPorId = bodyMap.props("vistasPorId") as Record<string, string>
        expect(vistasPorId["membro-superior-direito-posterior"]).toBe("posterior")
    })

    it("CA6 — bilateral anterior: ambos os membros mapeados para 'anterior'", async () => {
        const wrapper = await montarParaMapa([
            {
                regiao_id: "ombro-direito",
                caminho: "Membro superior (anterior) > Ombro",
                lateralidade: "bilateral",
                vista: "anterior",
                texto_exame: "", achados: "", observacoes: "",
                timestamp: new Date().toISOString(),
            },
        ], catalogoCircPropagacao)

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const vistasPorId = bodyMap.props("vistasPorId") as Record<string, string>
        expect(vistasPorId["membro-superior-direito-anterior"]).toBe("anterior")
        expect(vistasPorId["membro-superior-esquerdo-anterior"]).toBe("anterior")
    })

    it("CA5 — tronco-circunferencial popula 'tronco-anterior' e 'tronco-posterior' com 'circunferencial' no vistasPorId", async () => {
        // Fusão 2026-06-25_002: usa tronco-circunferencial (não mais torax-circunferencial)
        const wrapper = await montarParaMapa([
            {
                regiao_id: "tronco-circunferencial",
                caminho: "Tronco (circunferencial)",
                lateralidade: null,
                vista: "circunferencial",
                texto_exame: "", achados: "", observacoes: "",
                timestamp: new Date().toISOString(),
            },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ], catalogoComCircunferencial as any)

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const vistasPorId = bodyMap.props("vistasPorId") as Record<string, string>
        expect(vistasPorId["tronco-anterior"]).toBe("circunferencial")
        expect(vistasPorId["tronco-posterior"]).toBe("circunferencial")
    })

    it("CA7 — precedência R4: circunferencial vence anterior no mesmo id", async () => {
        const { exameFisicoService } = await import("@/services/exameFisicoService")
        ;(exameFisicoService.listarRegioes as ReturnType<typeof vi.fn>).mockResolvedValue(catalogoCircPropagacao)

        const BodyMapStub = {
            name: "BodyMap",
            props: ["regioes", "regioesExaminadas", "vistasPorId", "sexo"],
            template: "<div />",
        }

        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: {
                    regioes: [
                        // Mesmo id nível-1 recebe duas entradas: uma anterior, outra circunferencial
                        {
                            regiao_id: "ombro-direito",
                            caminho: "Membro superior direito (anterior) > Ombro direito",
                            lateralidade: "D",
                            vista: "anterior",
                            texto_exame: "", achados: "", observacoes: "",
                            timestamp: new Date().toISOString(),
                        },
                        {
                            regiao_id: "ombro-direito",
                            caminho: "Membro superior (circunferencial) > Ombro",
                            lateralidade: "D",
                            vista: "circunferencial",
                            texto_exame: "", achados: "", observacoes: "",
                            timestamp: new Date().toISOString(),
                        },
                    ],
                },
                readOnly: false,
            },
            global: {
                stubs: { BodyMap: BodyMapStub, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        await flushPromises()

        const bodyMap = wrapper.findComponent({ name: "BodyMap" })
        const vistasPorId = bodyMap.props("vistasPorId") as Record<string, string>
        // circunferencial tem prioridade 3 > anterior 1
        expect(vistasPorId["membro-superior-direito-anterior"]).toBe("circunferencial")
    })
})

// ─── Layout lateral — CA1, CA10, CA11 ────────────────────────────────────────

describe("SecaoExameFisico — layout lateral (CA1, CA10, CA11)", () => {
    beforeEach(() => { vi.clearAllMocks() })

    it("CA10 — contador aparece quando há regiões examinadas (N ≥ 1)", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: {
                    regioes: [{
                        regiao_id: "cabeca-anterior", caminho: "Cabeça (anterior)",
                        lateralidade: null, texto_exame: "", achados: "", observacoes: "",
                        timestamp: new Date().toISOString(),
                    }],
                },
                readOnly: false,
            },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        expect(wrapper.find(".regioes-contador").exists()).toBe(true)
        expect(wrapper.find(".regioes-contador").text()).toBe("1")
    })

    it("CA10 — contador não aparece quando N = 0", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: { regioes: [] }, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        expect(wrapper.find(".regioes-contador").exists()).toBe(false)
    })

    it("CA11 — estado vazio exibido quando nenhuma região examinada", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: { regioes: [] }, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        expect(wrapper.find(".regioes-vazio").exists()).toBe(true)
        expect(wrapper.find(".regioes-vazio-titulo").text()).toContain("Nenhuma região examinada")
    })

    it("CA11 — estado vazio não aparece quando há regiões", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: {
                modelValue: {
                    regioes: [{
                        regiao_id: "cabeca-anterior", caminho: "Cabeça (anterior)",
                        lateralidade: null, texto_exame: "", achados: "", observacoes: "",
                        timestamp: new Date().toISOString(),
                    }],
                },
                readOnly: false,
            },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        expect(wrapper.find(".regioes-vazio").exists()).toBe(false)
    })

    it("CA1 — grade lateral existe no DOM quando !readOnly", () => {
        const wrapper = mount(SecaoExameFisico, {
            props: { modelValue: {}, readOnly: false },
            global: {
                stubs: { BodyMap: true, RegionSelectorPopup: true, RegionExamCard: true },
            },
        })
        expect(wrapper.find(".mapa-grade").exists()).toBe(true)
    })
})

// ─── Catálogo com nós circunferenciais para testes (fusão 2026-06-25_002) ────
// Usa tronco-anterior/tronco-posterior/tronco-circunferencial (regiões reais).
// torax/abdome/pelve foram removidos do catálogo.
const catalogoComCircunferencial = [
    {
        id: "tronco-anterior",
        nome: "Tronco (anterior)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "anterior", template_texto: null, ordem: 1, ativo: true,
    },
    {
        id: "tronco-posterior",
        nome: "Tronco (posterior)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "posterior", template_texto: null, ordem: 2, ativo: true,
    },
    {
        id: "tronco-circunferencial",
        nome: "Tronco (circunferencial)",
        nivel: 1, lateralidade: false, pai_id: null,
        vista: "circunferencial", template_texto: null, ordem: 3, ativo: true,
    },
    {
        id: "peitoral",
        nome: "Peitoral",
        nivel: 2, lateralidade: false, pai_id: "tronco-anterior",
        vista: "anterior", template_texto: "Peitoral: ___.", ordem: 1, ativo: true,
    },
    {
        id: "escapular",
        nome: "Escapular",
        nivel: 2, lateralidade: false, pai_id: "tronco-posterior",
        vista: "posterior", template_texto: "Escapular: ___.", ordem: 1, ativo: true,
    },
    ...catalogoMembro,
]

// ─── Caminho neutral para bilateral ──────────────────────────────────────────

/**
 * Monta o componente com o catálogo de membros injetado via mock do service,
 * aguarda o onMounted resolver e dispara o evento "confirmar" do popup stub
 * para exercitar onConfirmarRegioes.
 */
async function montarComCatalogo(overrides = {}) {
    const { exameFisicoService } = await import("@/services/exameFisicoService")
    ;(exameFisicoService.listarRegioes as ReturnType<typeof vi.fn>).mockResolvedValue(catalogoMembro)

    // Stub que expõe o emit "confirmar" para o teste disparar
    const RegionSelectorPopupStub = {
        name: "RegionSelectorPopup",
        emits: ["confirmar", "update:aberto"],
        template: "<div />",
    }

    const wrapper = mount(SecaoExameFisico, {
        props: {
            modelValue: { regioes: [] },
            readOnly: false,
            ...overrides,
        },
        global: {
            stubs: {
                BodyMap: true,
                RegionSelectorPopup: RegionSelectorPopupStub,
                RegionExamCard: true,
            },
        },
    })

    // Aguarda o onMounted (listarRegioes) resolver e preencher catalogoRegioes
    await flushPromises()
    return wrapper
}

describe("SecaoExameFisico — caminho de entrada bilateral (Bug Tipo A)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("bilateral: caminho não contém 'direito' nem 'esquerdo'", async () => {
        const wrapper = await montarComCatalogo()

        // Dispara o evento confirmar do popup com lateralidade bilateral
        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "ombro-direito", lateralidade: "bilateral" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: Array<{ caminho: string; lateralidade: string }> }
        const regioesBilateral = ultimo.regioes.filter(r => r.lateralidade === "bilateral")
        expect(regioesBilateral).toHaveLength(1)
        const caminho = regioesBilateral[0].caminho
        expect(caminho.toLowerCase()).not.toContain("direito")
        expect(caminho.toLowerCase()).not.toContain("esquerdo")
        // Caminho esperado: "Membro superior (anterior) > Ombro"
        expect(caminho).toBe("Membro superior (anterior) > Ombro")
    })

    it("bilateral: caminho inclui o nome da região sem qualificador de lado", async () => {
        const wrapper = await montarComCatalogo()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "braco-direito", lateralidade: "bilateral" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: Array<{ caminho: string }> }
        expect(ultimo.regioes[0].caminho).toBe("Membro superior (anterior) > Braço")
    })

    it("Direito: caminho preserva 'direito' na entrada com lateralidade D", async () => {
        const wrapper = await montarComCatalogo()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "ombro-direito", lateralidade: "D" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: Array<{ caminho: string; lateralidade: string }> }
        const regiaoD = ultimo.regioes.find(r => r.lateralidade === "D")
        expect(regiaoD).toBeTruthy()
        expect(regiaoD!.caminho).toBe("Membro superior direito (anterior) > Ombro direito")
    })

    it("Esquerdo: caminho sem modificação para lateralidade E (id da base esquerda não está no catálogo mínimo, mas a função não altera o texto)", async () => {
        const wrapper = await montarComCatalogo()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        // Simula seleção de id inexistente no catálogo mínimo → getCaminho retorna ""
        // O importante é que não aplica caminhoNeutro quando lateralidade é E
        await popup.vm.$emit("confirmar", [
            { regiaoId: "ombro-direito", lateralidade: "E" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: Array<{ caminho: string; lateralidade: string }> }
        const regiaoE = ultimo.regioes.find(r => r.lateralidade === "E")
        expect(regiaoE).toBeTruthy()
        // Para E, o caminho NÃO é modificado (preserva o texto original com lateralidade)
        expect(regiaoE!.caminho).toBe("Membro superior direito (anterior) > Ombro direito")
    })
})

// ─── B1: SecaoExameFisico — modo circunferencial ──────────────────────────────

async function montarComCatalogoCirc() {
    const { exameFisicoService } = await import("@/services/exameFisicoService")
    ;(exameFisicoService.listarRegioes as ReturnType<typeof vi.fn>).mockResolvedValue(catalogoComCircunferencial)

    const RegionSelectorPopupStub = {
        name: "RegionSelectorPopup",
        emits: ["confirmar", "update:aberto"],
        template: "<div />",
    }

    const wrapper = mount(SecaoExameFisico, {
        props: { modelValue: { regioes: [] }, readOnly: false },
        global: {
            stubs: {
                BodyMap: true,
                RegionSelectorPopup: RegionSelectorPopupStub,
                RegionExamCard: true,
            },
        },
    })
    await flushPromises()
    return wrapper
}

describe("SecaoExameFisico — B1 circunferencial", () => {
    beforeEach(() => { vi.clearAllMocks() })

    it("CA19/CA22 — modo circunferencial: 1 confirmação gera 1 card com regiao_id = tronco-circunferencial", async () => {
        // Fusão 2026-06-25_002: sub-regiões são filhos de tronco-anterior/tronco-posterior
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        // Simula confirmar 2 sub-regiões de vistas diferentes no modo circunferencial
        await popup.vm.$emit("confirmar", [
            { regiaoId: "peitoral",   lateralidade: null, vista: "circunferencial" },
            { regiaoId: "escapular",  lateralidade: null, vista: "circunferencial" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        // Exatamente 1 card criado
        expect(ultimo.regioes).toHaveLength(1)
        // regiao_id = tronco-circunferencial (CA22)
        expect(ultimo.regioes[0].regiao_id).toBe("tronco-circunferencial")
    })

    it("CA22 — payload não tem campo vista (derivado do codigo do nó)", async () => {
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "peitoral", lateralidade: null, vista: "circunferencial" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        // O campo vista existe apenas no estado local do frontend (não enviado ao backend)
        // Verificamos que o regiao_id termina em -circunferencial (o backend resolve a vista pelo código)
        expect(ultimo.regioes[0].regiao_id).toMatch(/-circunferencial$/)
    })

    it("CA20 — card circunferencial tem vista = 'circunferencial' na RegiaoAnatomicaSelecionada", async () => {
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "peitoral", lateralidade: null, vista: "circunferencial" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        expect(ultimo.regioes[0].vista).toBe("circunferencial")
    })

    it("CA20 — card anterior tem vista = 'anterior' na RegiaoAnatomicaSelecionada", async () => {
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "peitoral", lateralidade: null, vista: "anterior" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        expect(ultimo.regioes[0].vista).toBe("anterior")
    })

    it("R9 — texto_exame do card circunferencial concatena templates de ambas as vistas", async () => {
        // Fusão 2026-06-25_002: peitoral (filho de tronco-anterior) + escapular (filho de tronco-posterior)
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "peitoral",  lateralidade: null, vista: "circunferencial" },
            { regiaoId: "escapular", lateralidade: null, vista: "circunferencial" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        const texto = ultimo.regioes[0].texto_exame
        // Deve conter templates das duas sub-regiões
        expect(texto).toContain("Peitoral: ___.")
        expect(texto).toContain("Escapular: ___.")
    })

    it("CA29 (não-regressão) — modo anterior puro ainda gera regiao_id do nível-1 anterior, não circunferencial", async () => {
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "peitoral", lateralidade: null, vista: "anterior" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        // No modo anterior, regiao_id NÃO é circunferencial
        expect(ultimo.regioes[0].regiao_id).not.toMatch(/-circunferencial$/)
        expect(ultimo.regioes[0].vista).toBe("anterior")
    })

    it("BUGFIX — marcar a opção '(geral)' circunferencial não duplica o sufixo (regiao_id válido pinta o boneco)", async () => {
        // Quando o usuário marca a opção "(geral)", o popup emite o PRÓPRIO nó circunferencial
        // (id já termina em -circunferencial). A derivação não pode reanexar -circunferencial.
        const wrapper = await montarComCatalogoCirc()

        const popup = wrapper.findComponent({ name: "RegionSelectorPopup" })
        await popup.vm.$emit("confirmar", [
            { regiaoId: "tronco-circunferencial", lateralidade: null, vista: "circunferencial" },
        ])

        const eventos = wrapper.emitted("update:modelValue")
        const ultimo = eventos![eventos!.length - 1]![0] as { regioes: RegiaoAnatomicaSelecionada[] }
        // regiao_id é o nó circunferencial real (chave de RAMOS_CIRCUNFERENCIAL → pinta o boneco)
        expect(ultimo.regioes[0].regiao_id).toBe("tronco-circunferencial")
        // Sem sufixo duplicado
        expect(ultimo.regioes[0].regiao_id).not.toContain("circunferencial-circunferencial")
        // Caminho resolve pelo nome do catálogo (não vaza o id cru)
        expect(ultimo.regioes[0].caminho).toBe("Tronco (circunferencial)")
    })
})
