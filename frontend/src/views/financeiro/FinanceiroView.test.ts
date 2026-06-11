import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { defineComponent, h } from "vue"

/**
 * FinanceiroView — CA5 (export CSV dispara via ref na VisaoGeralTab)
 *
 * Regra testada: o ref="visaoGeralRef" NÃO pode estar dentro de um v-for.
 * Em Vue 3, ref dentro de v-for vira array; .value?.exportar?.() avalia undefined
 * silenciosamente e o export nunca dispara. A correção estrutura os painéis como
 * elementos irmãos diretos (v-if/hidden por aba) — ref funciona normalmente.
 */

const mocks = vi.hoisted(() => ({
    exportarSpy: vi.fn(),
    permissoes: { ehDono: true },
    tenant:     { ativo: { id: 1, nomeFantasia: "Clínica X" } },
}))

vi.mock("@/stores/permissoesStore", () => ({ usePermissoesStore: vi.fn(() => mocks.permissoes) }))
vi.mock("@/stores/tenantStore",     () => ({ useTenantStore:     vi.fn(() => mocks.tenant)     }))

// AppPageHeader e AppButton — stubs que preservam o slot #acoes e propagam @click.
vi.mock("@/components/ui", () => ({
    AppPageHeader: defineComponent({
        props: ["titulo", "subtitulo"],
        setup(_p, { slots }) {
            return () => h("header", [
                slots.acoes ? h("div", { class: "acoes" }, slots.acoes()) : null,
            ])
        },
    }),
    AppButton: defineComponent({
        props: ["variant", "icon"],
        emits: ["click"],
        setup(_p, { slots, emit }) {
            return () => h("button", { onClick: () => emit("click") }, slots.default?.())
        },
    }),
}))

// VisaoGeralTabStub expõe `exportar` via setup return — idêntico ao contrato do componente real.
const VisaoGeralTabStub = defineComponent({
    name: "VisaoGeralTabStub",
    props: ["modalAbertoExterno"],
    emits: ["update:modalAbertoExterno"],
    setup() {
        // defineExpose é equivalente a retornar do setup em Options API:
        // qualquer propriedade retornada do setup fica acessível via templateRef.
        return { exportar: mocks.exportarSpy }
    },
    template: `<div data-testid="visao-geral-tab" />`,
})

const CaixaTabStub     = defineComponent({ template: `<div data-testid="caixa-tab" />` })
const ComissoesTabStub = defineComponent({ template: `<div data-testid="comissoes-tab" />` })
const ConfigTabStub    = defineComponent({ template: `<div data-testid="config-tab" />` })

import FinanceiroView from "./FinanceiroView.vue"

function montar() {
    return mount(FinanceiroView, {
        global: {
            // Registrar os stubs pelos nomes que o defineAsyncComponent resolve.
            // O vue-test-utils faz substituição por nome de componente.
            stubs: {
                VisaoGeralTab:        VisaoGeralTabStub,
                CaixaTab:             CaixaTabStub,
                ComissoesTab:         ComissoesTabStub,
                FinanceiroConfigTab:  ConfigTabStub,
                Teleport:             true,
                // Suspense renderiza normalmente para que o slot default monte o stub.
                Suspense:             false,
            },
        },
    })
}

describe("FinanceiroView — CA5 (ref da VisaoGeralTab não é array — regressão v-for)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("acionar o botão Exportar chama exportar() na VisaoGeralTab exatamente 1 vez", async () => {
        const w = montar()
        await flushPromises()

        // Aba visao-geral é a ativa por padrão — botão dispara direto.
        const botaoExportar = w.findAll("button").find(b => b.text().includes("Exportar"))
        expect(botaoExportar, "botão Exportar deve existir no header").toBeTruthy()

        await botaoExportar!.trigger("click")
        await flushPromises()

        expect(mocks.exportarSpy).toHaveBeenCalledTimes(1)
    })

    it("acionar Exportar com outra aba ativa troca para visao-geral sem chamar exportar()", async () => {
        const w = montar()
        await flushPromises()

        // Seleciona aba Caixa.
        const abaCaixa = w.findAll("button[role='tab']").find(b => b.text().includes("Caixa"))
        expect(abaCaixa, "botão da aba Caixa deve existir").toBeTruthy()
        await abaCaixa!.trigger("click")
        await flushPromises()

        const botaoExportar = w.findAll("button").find(b => b.text().includes("Exportar"))
        await botaoExportar!.trigger("click")
        await flushPromises()

        // Troca de aba sem chamar exportar() — usuário verá o botão na aba.
        expect(mocks.exportarSpy).not.toHaveBeenCalled()
        // Painel visao-geral deve ter voltado a ficar visível (sem atributo hidden).
        expect(w.find("#painel-visao-geral").attributes("hidden")).toBeUndefined()
    })
})
