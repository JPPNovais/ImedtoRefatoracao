import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import SecaoExameFisico from "./SecaoExameFisico.vue"

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

        const pesoInput = wrapper.findAll("input").find(i => i.attributes("placeholder") === "70.5")
        expect(pesoInput).toBeTruthy()
        await pesoInput!.setValue("80")

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        expect(eventos![0]![0]).toMatchObject({ peso: "80" })
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
        const inputs = wrapper.findAll("input.readonly")
        const imcInput = inputs[0] as any
        expect(imcInput.element.value).toBe("22.9")
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
