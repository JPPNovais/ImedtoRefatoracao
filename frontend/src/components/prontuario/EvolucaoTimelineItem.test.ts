import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import type { Evolucao } from "@/services/prontuarioService"

vi.mock("@/components/ui", () => {
    const AppButton = {
        props: ["variant", "size", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button
            :disabled="disabled || loading"
            data-test="btn-pdf"
            :data-loading="loading ? 'true' : 'false'"
            @click="$emit('click')"
        ><slot /></button>`,
    }
    return { AppButton }
})

import EvolucaoTimelineItem from "./EvolucaoTimelineItem.vue"

const evolucaoMock: Evolucao = {
    id: 42,
    prontuarioId: 1,
    autorUsuarioId: "00000000-0000-0000-0000-000000000001",
    autorNome: "Dr. Carlos",
    modeloNome: "Padrão Imedto",
    conteudo: { queixa: "Dor no peito há 3 dias.", conduta: "" },
    modeloSnapshot: [
        { chave: "queixa", titulo: "Queixa principal", tipo: "texto_longo", ordem: 1 },
        { chave: "conduta", titulo: "Conduta", tipo: "texto_longo", ordem: 2 },
    ],
    modeloDeProntuarioIdOrigem: 1,
    criadaEm: "2026-05-12T10:30:00Z",
}

describe("EvolucaoTimelineItem", () => {
    it("renderiza dados principais (modelo, profissional, resumo, contagem de seções)", () => {
        const w = mount(EvolucaoTimelineItem, { props: { evolucao: evolucaoMock } })
        const texto = w.text()
        expect(texto).toContain("Padrão Imedto")
        expect(texto).toContain("Dr. Carlos")
        expect(texto).toContain("Dor no peito")
        // 1 de 2 seções preenchidas (queixa preenchida, conduta vazia)
        expect(texto).toContain("1/2 seções")
    })

    it("emite 'gerar-pdf' com a evolução ao clicar no botão", async () => {
        const w = mount(EvolucaoTimelineItem, { props: { evolucao: evolucaoMock } })
        await w.find("[data-test='btn-pdf']").trigger("click")

        const eventos = w.emitted("gerar-pdf")
        expect(eventos).toBeTruthy()
        expect(eventos![0][0]).toEqual(evolucaoMock)
    })

    it("não emite quando 'gerandoPdf' é true e propaga loading ao botão", async () => {
        const w = mount(EvolucaoTimelineItem, {
            props: { evolucao: evolucaoMock, gerandoPdf: true },
        })
        const botao = w.find("[data-test='btn-pdf']")
        expect(botao.attributes("data-loading")).toBe("true")
        expect((botao.element as HTMLButtonElement).disabled).toBe(true)

        await botao.trigger("click")
        // O AppButton disabled bloqueia o click DOM; o handler do componente
        // também tem guard interno — em ambos os caminhos, não deve emitir.
        expect(w.emitted("gerar-pdf")).toBeFalsy()
    })

    it("exibe badge 'Mais recente' quando destaque=true", () => {
        const w = mount(EvolucaoTimelineItem, {
            props: { evolucao: evolucaoMock, destaque: true },
        })
        expect(w.text()).toContain("Mais recente")
    })

    it("não exibe badge 'Mais recente' quando destaque=false", () => {
        const w = mount(EvolucaoTimelineItem, {
            props: { evolucao: evolucaoMock, destaque: false },
        })
        expect(w.text()).not.toContain("Mais recente")
    })
})
