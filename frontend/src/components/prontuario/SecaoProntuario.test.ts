/**
 * Testes de SecaoProntuario — hotfix 2026-06-10_012 (F3B conduta)
 *
 * Foco: gate de despacho da seção conduta.
 * CA73: modelo persistido com tipo "texto_longo" + evolução nova → renderiza checklist.
 * CA73: evolução legada (string não-vazia) → textarea read-only.
 * CA73: modelo novo com tipo "conduta_checklist" → renderiza checklist.
 * CA73: objeto {acoesMarcadas} salvo → renderiza checklist (retrocompat objeto).
 */

import { describe, it, expect } from "vitest"
import { shallowMount } from "@vue/test-utils"
import SecaoProntuario from "./SecaoProntuario.vue"

// shallowMount faz stub automático de todos os filhos registrados
// (SecaoCondutaChecklist, AppTextarea, AppInput, etc.) — testamos o dispatcher.

const propsBase = {
    titulo: "Conduta",
    chave: "conduta",
}

describe("SecaoProntuario — despacho da seção conduta", () => {
    it("modelo antigo (tipo texto_longo) + evolução nova vazia → renderiza SecaoCondutaChecklist", () => {
        // Cenário central do hotfix: estabelecimento pré-F3B tem modelos persistidos
        // com tipo "texto_longo". Ao criar nova evolução o modelValue ainda é "".
        const wrapper = shallowMount(SecaoProntuario, {
            props: { ...propsBase, tipo: "texto_longo", modelValue: "" },
        })
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(true)
        expect(wrapper.findComponent({ name: "AppTextarea" }).exists()).toBe(false)
    })

    it("modelo antigo (tipo texto_longo) + evolução nova null → renderiza SecaoCondutaChecklist", () => {
        const wrapper = shallowMount(SecaoProntuario, {
            props: { ...propsBase, tipo: "texto_longo", modelValue: null },
        })
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(true)
    })

    it("evolução legada (string não-vazia) → textarea read-only (CA73 retrocompat)", () => {
        const wrapper = shallowMount(SecaoProntuario, {
            props: { ...propsBase, tipo: "texto_longo", modelValue: "Prescrever repouso." },
        })
        // Legado: string com conteúdo → AppTextarea desabilitado; checklist não aparece
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(false)
        expect(wrapper.findComponent({ name: "AppTextarea" }).exists()).toBe(true)
    })

    it("modelo novo (tipo conduta_checklist) + modelValue vazio → renderiza SecaoCondutaChecklist", () => {
        const wrapper = shallowMount(SecaoProntuario, {
            props: { ...propsBase, tipo: "conduta_checklist", modelValue: {} },
        })
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(true)
    })

    it("evolução salva como objeto {acoesMarcadas} → renderiza SecaoCondutaChecklist", () => {
        // Evolução já registrada com checklist: objeto persistido no banco
        const wrapper = shallowMount(SecaoProntuario, {
            props: {
                ...propsBase,
                tipo: "texto_longo",
                modelValue: { acoesMarcadas: ["CriarReceita"], observacao: "" },
            },
        })
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(true)
    })

    it("string vazia não ativa legado (porta não fechada para checklist)", () => {
        // String vazia não deve acionar ehCondutaLegado — deve ir para checklist
        const wrapper = shallowMount(SecaoProntuario, {
            props: { ...propsBase, tipo: "texto_longo", modelValue: "" },
        })
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(true)
    })
})

describe("SecaoProntuario — seções não-conduta não afetadas", () => {
    it("chave hpp → renderiza SecaoHistoriaPregressa (não conduta)", () => {
        const wrapper = shallowMount(SecaoProntuario, {
            props: { chave: "hpp", titulo: "HPP", tipo: "estruturado", modelValue: {} },
        })
        expect(wrapper.findComponent({ name: "SecaoHistoriaPregressa" }).exists()).toBe(true)
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(false)
    })

    it("chave genérica tipo texto_longo → textarea (fallback normal)", () => {
        const wrapper = shallowMount(SecaoProntuario, {
            props: { chave: "queixa-principal", titulo: "Queixa", tipo: "texto_longo", modelValue: "" },
        })
        expect(wrapper.findComponent({ name: "AppTextarea" }).exists()).toBe(true)
        expect(wrapper.findComponent({ name: "SecaoCondutaChecklist" }).exists()).toBe(false)
    })
})
