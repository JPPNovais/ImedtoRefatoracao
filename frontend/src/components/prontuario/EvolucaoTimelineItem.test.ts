import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import type { Evolucao } from "@/services/prontuarioService"

vi.mock("@/components/ui", () => {
    // Stub propaga $attrs (inclusive data-test) para o <button> via v-bind.
    // Assim cada AppButton no template pode ter seu próprio data-test.
    const AppButton = {
        inheritAttrs: false,
        props: ["variant", "size", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button
            v-bind="$attrs"
            :disabled="disabled || loading"
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

    it("clique em 'Ver PDF' emite { evolucao, modo: 'visualizar' }", async () => {
        const w = mount(EvolucaoTimelineItem, { props: { evolucao: evolucaoMock } })
        await w.find("[data-test='btn-pdf-visualizar']").trigger("click")

        const eventos = w.emitted("gerar-pdf")
        expect(eventos).toBeTruthy()
        expect(eventos![0][0]).toEqual({ evolucao: evolucaoMock, modo: "visualizar" })
    })

    it("clique em 'Baixar' emite { evolucao, modo: 'download' }", async () => {
        const w = mount(EvolucaoTimelineItem, { props: { evolucao: evolucaoMock } })
        await w.find("[data-test='btn-pdf-baixar']").trigger("click")

        const eventos = w.emitted("gerar-pdf")
        expect(eventos).toBeTruthy()
        expect(eventos![0][0]).toEqual({ evolucao: evolucaoMock, modo: "download" })
    })

    it("não emite quando 'gerandoPdf' é true e propaga loading aos dois botões", async () => {
        const w = mount(EvolucaoTimelineItem, {
            props: { evolucao: evolucaoMock, gerandoPdf: true },
        })
        const verBtn    = w.find("[data-test='btn-pdf-visualizar']")
        const baixarBtn = w.find("[data-test='btn-pdf-baixar']")
        expect(verBtn.attributes("data-loading")).toBe("true")
        expect(baixarBtn.attributes("data-loading")).toBe("true")
        expect((verBtn.element as HTMLButtonElement).disabled).toBe(true)
        expect((baixarBtn.element as HTMLButtonElement).disabled).toBe(true)

        await verBtn.trigger("click")
        await baixarBtn.trigger("click")
        // Disabled bloqueia o click DOM e o handler tem guard interno.
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
