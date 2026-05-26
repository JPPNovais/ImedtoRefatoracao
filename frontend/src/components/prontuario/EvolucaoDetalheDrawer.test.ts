import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import type { Evolucao } from "@/services/prontuarioService"

// Stub dos componentes do design system para isolar o teste
vi.mock("@/components/ui", () => {
    const AppDrawer = {
        props: ["aberto", "titulo", "largura"],
        emits: ["fechar"],
        template: `
            <div v-if="aberto" data-test="drawer">
                <slot name="titulo" />
                <slot />
                <slot name="rodape" />
            </div>
        `,
    }
    const AppEmptyState = {
        props: ["icone", "titulo", "descricao", "compacto"],
        template: `<div data-test="empty-state"><slot name="acao" /></div>`,
    }
    const AppButton = {
        props: ["variant", "size"],
        emits: ["click"],
        template: `<button v-bind="$attrs" @click="$emit('click')"><slot /></button>`,
    }
    return { AppDrawer, AppEmptyState, AppButton }
})

import EvolucaoDetalheDrawer from "./EvolucaoDetalheDrawer.vue"

const evolucaoBase: Evolucao = {
    id: 1,
    prontuarioId: 10,
    autorUsuarioId: "user-1",
    autorNome: "Dr. Ana",
    modeloNome: "Anamnese Padrão",
    conteudo: {
        queixa: "Dor de cabeça há 2 dias.",
        conduta: "Prescrição de analgésico.",
        observacao: "",
        exame: null,
        historico: [],
    },
    modeloSnapshot: [
        { chave: "queixa",    titulo: "Queixa principal",      tipo: "texto_longo", ordem: 1 },
        { chave: "conduta",   titulo: "Conduta",               tipo: "texto_longo", ordem: 2 },
        { chave: "observacao",titulo: "Observação",            tipo: "texto_longo", ordem: 3 },
        { chave: "exame",     titulo: "Exame físico",          tipo: "texto_longo", ordem: 4 },
        { chave: "historico", titulo: "Histórico patológico",  tipo: "texto_longo", ordem: 5 },
    ],
    modeloDeProntuarioIdOrigem: 2,
    criadaEm: "2026-05-22T14:00:00Z",
}

describe("EvolucaoDetalheDrawer", () => {
    it("não renderiza nada quando aberto=false", () => {
        const w = mount(EvolucaoDetalheDrawer, {
            props: { evolucao: evolucaoBase, aberto: false },
        })
        expect(w.find("[data-test='drawer']").exists()).toBe(false)
    })

    it("CA2 — renderiza somente as seções preenchidas (3 de 5)", () => {
        const w = mount(EvolucaoDetalheDrawer, {
            props: { evolucao: evolucaoBase, aberto: true },
        })
        // queixa e conduta preenchidas; observacao (""), exame (null) e historico ([]) vazios
        expect(w.find("[data-test='secao-queixa']").exists()).toBe(true)
        expect(w.find("[data-test='secao-conduta']").exists()).toBe(true)
        expect(w.find("[data-test='secao-observacao']").exists()).toBe(false)
        expect(w.find("[data-test='secao-exame']").exists()).toBe(false)
        expect(w.find("[data-test='secao-historico']").exists()).toBe(false)
    })

    it("CA2 — exibe o conteúdo correto nas seções preenchidas", () => {
        const w = mount(EvolucaoDetalheDrawer, {
            props: { evolucao: evolucaoBase, aberto: true },
        })
        expect(w.text()).toContain("Dor de cabeça há 2 dias.")
        expect(w.text()).toContain("Prescrição de analgésico.")
    })

    it("CA7 — exibe empty state quando todas as seções estão vazias", () => {
        const evolucaoVazia: Evolucao = {
            ...evolucaoBase,
            conteudo: {
                queixa:    "",
                conduta:   null,
                observacao: "   ",
                exame:     [],
                historico: {},
            },
        }
        const w = mount(EvolucaoDetalheDrawer, {
            props: { evolucao: evolucaoVazia, aberto: true },
        })
        expect(w.find("[data-test='empty-state']").exists()).toBe(true)
        expect(w.find("[data-test='secao-queixa']").exists()).toBe(false)
    })

    it("emite 'fechar' ao clicar no botão Fechar do rodapé", async () => {
        const w = mount(EvolucaoDetalheDrawer, {
            props: { evolucao: evolucaoBase, aberto: true },
        })
        await w.find("[data-test='btn-fechar']").trigger("click")
        expect(w.emitted("fechar")).toBeTruthy()
    })

    it("exibe nome do profissional e nome do modelo no cabeçalho", () => {
        const w = mount(EvolucaoDetalheDrawer, {
            props: { evolucao: evolucaoBase, aberto: true },
        })
        expect(w.text()).toContain("Dr. Ana")
        expect(w.text()).toContain("Anamnese Padrão")
    })
})
