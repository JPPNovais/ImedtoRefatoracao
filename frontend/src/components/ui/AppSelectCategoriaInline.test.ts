import { describe, it, expect } from "vitest"
import { nextTick } from "vue"
import { mount } from "@vue/test-utils"
import AppSelectCategoriaInline from "./AppSelectCategoriaInline.vue"

const opcoes = ["Aluguel", "Folha de pagamento", "Insumos e materiais"]

describe("AppSelectCategoriaInline", () => {
    it("renderiza select com opções recebidas via prop", () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes },
        })
        const options = w.findAll("option")
        // placeholder desabilitado + 3 opções
        expect(options).toHaveLength(4)
        expect(options[1].text()).toBe("Aluguel")
        expect(options[3].text()).toBe("Insumos e materiais")
    })

    it("emite update:modelValue ao selecionar opção", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes },
        })
        await w.find("select").setValue("Aluguel")
        expect(w.emitted("update:modelValue")?.[0][0]).toBe("Aluguel")
    })

    it("valor transitório aparece quando modelValue não está nas opcoes", () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "Categoria antiga", opcoes },
        })
        const options = w.findAll("option")
        const textos = options.map((o) => o.text())
        expect(textos).toContain("Categoria antiga")
        // transitória fica na frente
        expect(textos[1]).toBe("Categoria antiga")
    })

    it("abre mini-form ao clicar em Adicionar nova categoria", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes },
        })
        expect(w.find("select").exists()).toBe(true)
        await w.find(".sca-btn-adicionar").trigger("click")
        expect(w.find(".sca-novo-form").exists()).toBe(true)
        expect(w.find("select").exists()).toBe(false)
    })

    it("cancela mini-form e volta ao select", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes },
        })
        await w.find(".sca-btn-adicionar").trigger("click")
        await w.find(".sca-btn-cancelar").trigger("click")
        expect(w.find("select").exists()).toBe(true)
        expect(w.find(".sca-novo-form").exists()).toBe(false)
    })

    it("emite criar com o nome digitado ao confirmar", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes },
        })
        await w.find(".sca-btn-adicionar").trigger("click")
        await w.find(".sca-novo-input").setValue("Marketing")
        await w.find(".sca-btn-confirmar").trigger("click")
        expect(w.emitted("criar")?.[0][0]).toBe("Marketing")
    })

    it("não emite criar se nome vazio", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes },
        })
        await w.find(".sca-btn-adicionar").trigger("click")
        await w.find(".sca-btn-confirmar").trigger("click")
        expect(w.emitted("criar")).toBeUndefined()
    })

    it("exibe erroCriar quando prop é fornecida no modo inline", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes, erroCriar: "Categoria já existe." },
        })
        await w.find(".sca-btn-adicionar").trigger("click")
        expect(w.find(".sca-erro-inline").text()).toBe("Categoria já existe.")
    })

    it("desabilita select e botão quando desabilitado=true", () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes, desabilitado: true },
        })
        expect(w.find("select").attributes("disabled")).toBeDefined()
        expect(w.find(".sca-btn-adicionar").attributes("disabled")).toBeDefined()
    })

    it("filtra corretamente: não duplica transitória quando ela já está nas opcoes", () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "Aluguel", opcoes },
        })
        const options = w.findAll("option")
        const textos = options.map((o) => o.text())
        const ocorrencias = textos.filter((t) => t === "Aluguel")
        expect(ocorrencias).toHaveLength(1)
    })

    // CA13: ao criar inline com sucesso, o mini-form fecha e o select aparece
    it("CA13: fecha mini-form quando salvandoCriar transita true→false sem erroCriar", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes, salvandoCriar: false, erroCriar: null },
        })
        // Abre o mini-form
        await w.find(".sca-btn-adicionar").trigger("click")
        expect(w.find(".sca-novo-form").exists()).toBe(true)

        // Simula pai iniciando o POST (salvandoCriar=true)
        await w.setProps({ salvandoCriar: true })
        expect(w.find(".sca-novo-form").exists()).toBe(true)

        // Simula pai concluindo com sucesso (salvandoCriar=false, sem erro)
        await w.setProps({ salvandoCriar: false, erroCriar: null })
        await nextTick()

        // Mini-form deve ter fechado
        expect(w.find(".sca-novo-form").exists()).toBe(false)
        expect(w.find("select").exists()).toBe(true)
    })

    it("CA13: mantém mini-form aberto quando salvandoCriar termina com erro", async () => {
        const w = mount(AppSelectCategoriaInline, {
            props: { modelValue: "", opcoes, salvandoCriar: false, erroCriar: null },
        })
        await w.find(".sca-btn-adicionar").trigger("click")

        await w.setProps({ salvandoCriar: true })
        await w.setProps({ salvandoCriar: false, erroCriar: "Já existe uma categoria com este nome e tipo." })
        await nextTick()

        // Com erro, permanece no mini-form
        expect(w.find(".sca-novo-form").exists()).toBe(true)
        expect(w.find(".sca-erro-inline").text()).toContain("Já existe")
    })
})
