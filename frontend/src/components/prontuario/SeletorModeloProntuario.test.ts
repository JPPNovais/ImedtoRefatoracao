/**
 * SeletorModeloProntuario — testes do componente reutilizável de seleção de modelo.
 * Cobre: placeholder quando modeloId=null, nome quando modeloId!=null,
 * emissão de update:modeloId ao clicar num item, header do menu.
 */
import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import SeletorModeloProntuario from "./SeletorModeloProntuario.vue"

// AppPopover renderiza o conteúdo diretamente no DOM (sem teleport) em testes;
// basta stubar como pass-through que expõe slots.
const AppPopoverStub = {
    name: "AppPopover",
    template: `
        <div>
            <slot name="gatilho" :toggle="toggle" :aberto="aberto" />
            <slot name="conteudo" :fechar="fechar" />
        </div>
    `,
    setup() {
        const aberto = false
        const toggle = vi.fn()
        const fechar = vi.fn()
        return { aberto, toggle, fechar }
    },
}

const MODELOS = [
    { id: 1, nome: "Consulta geral", ehPadraoSistema: true, descricao: null, ativo: true, estabelecimentoId: null, estrutura: [] },
    { id: 2, nome: "Cirúrgico", ehPadraoSistema: false, descricao: "Para procedimentos", ativo: true, estabelecimentoId: 1, estrutura: [] },
]

function montar(modeloId: number | null) {
    return mount(SeletorModeloProntuario, {
        props: { modeloId, modelos: MODELOS },
        global: { stubs: { AppPopover: AppPopoverStub } },
    })
}

describe("SeletorModeloProntuario — placeholder e nome", () => {
    it("exibe placeholder quando modeloId é null", () => {
        const w = montar(null)
        expect(w.find(".tpl-placeholder").exists()).toBe(true)
        expect(w.find(".tpl-placeholder").text()).toContain("Selecione um modelo")
        // Nenhum strong sem classe placeholder (o modelo não está selecionado)
        const strongs = w.findAll("strong")
        expect(strongs.some(s => !s.classes("tpl-placeholder"))).toBe(false)
    })

    it("exibe o nome do modelo quando modeloId está definido", () => {
        const w = montar(1)
        const strong = w.find(".tpl-current strong")
        expect(strong.text()).toBe("Consulta geral")
        expect(w.find(".tpl-placeholder").exists()).toBe(false)
    })
})

describe("SeletorModeloProntuario — cabeçalho do menu", () => {
    it("usa 'Selecionar modelo' no cabeçalho quando modeloId é null", () => {
        const w = montar(null)
        expect(w.find(".tpl-menu-head").text()).toContain("Selecionar modelo")
        expect(w.find(".tpl-menu-head").text()).toContain("2 disponíveis")
    })

    it("usa 'Trocar modelo' no cabeçalho quando modeloId está definido", () => {
        const w = montar(1)
        expect(w.find(".tpl-menu-head").text()).toContain("Trocar modelo")
    })
})

describe("SeletorModeloProntuario — emissão de evento", () => {
    it("emite update:modeloId ao clicar num item diferente do atual", async () => {
        const w = montar(1)
        const itens = w.findAll(".tpl-menu-item")
        // Item com id=2 é diferente do selecionado (id=1)
        await itens[1].trigger("click")
        const emitido = w.emitted("update:modeloId")
        expect(emitido).toBeDefined()
        expect(emitido![0]).toEqual([2])
    })

    it("NÃO emite update:modeloId ao clicar no item já selecionado", async () => {
        const w = montar(1)
        const itens = w.findAll(".tpl-menu-item")
        // Item com id=1 já está selecionado
        await itens[0].trigger("click")
        expect(w.emitted("update:modeloId")).toBeUndefined()
    })

    it("emite update:modeloId quando modeloId era null e usuário clica", async () => {
        const w = montar(null)
        const itens = w.findAll(".tpl-menu-item")
        await itens[0].trigger("click")
        const emitido = w.emitted("update:modeloId")
        expect(emitido).toBeDefined()
        expect(emitido![0]).toEqual([1])
    })
})

describe("SeletorModeloProntuario — check no item selecionado", () => {
    it("exibe ícone de check apenas no item selecionado", () => {
        const w = montar(2)
        const itens = w.findAll(".tpl-menu-item")
        // Primeiro item (id=1) não deve ter check
        expect(itens[0].find(".tpl-menu-check").exists()).toBe(false)
        // Segundo item (id=2) deve ter check
        expect(itens[1].find(".tpl-menu-check").exists()).toBe(true)
    })

    it("nenhum item tem check quando modeloId é null", () => {
        const w = montar(null)
        const checks = w.findAll(".tpl-menu-check")
        expect(checks.length).toBe(0)
    })
})
