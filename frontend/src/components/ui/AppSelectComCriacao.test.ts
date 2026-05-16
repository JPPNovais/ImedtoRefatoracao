import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import AppSelectComCriacao from "./AppSelectComCriacao.vue"

const opcoes = [
    { id: 1, nome: "Anestésicos" },
    { id: 2, nome: "Curativos" },
]

describe("AppSelectComCriacao", () => {
    it("renderiza select com opções recebidas via prop", () => {
        const w = mount(AppSelectComCriacao, { props: { opcoes } })
        const options = w.findAll("option")
        // 1 placeholder ("— Nenhum —") + 2 opções
        expect(options).toHaveLength(3)
        expect(options[1].text()).toBe("Anestésicos")
        expect(options[2].text()).toBe("Curativos")
    })

    it("placeholder customizado é exibido", () => {
        const w = mount(AppSelectComCriacao, {
            props: { opcoes, placeholder: "Escolha uma categoria" },
        })
        expect(w.findAll("option")[0].text()).toBe("Escolha uma categoria")
    })

    it("mostra botão '+ Novo' por padrão e emite 'criar' ao clicar", async () => {
        const w = mount(AppSelectComCriacao, {
            props: { opcoes, rotuloCriar: "Nova categoria" },
        })
        const btn = w.find("button")
        expect(btn.exists()).toBe(true)
        expect(btn.attributes("aria-label")).toBe("Nova categoria")

        await btn.trigger("click")
        expect(w.emitted("criar")).toBeTruthy()
        expect(w.emitted("criar")).toHaveLength(1)
    })

    it("esconde o botão quando permiteCriar=false", () => {
        const w = mount(AppSelectComCriacao, {
            props: { opcoes, permiteCriar: false },
        })
        expect(w.find("button").exists()).toBe(false)
    })

    it("emite update:modelValue como number ao trocar a opção", async () => {
        const w = mount(AppSelectComCriacao, { props: { opcoes, modelValue: 0 } })
        const select = w.find("select")
        await select.setValue("2")

        const eventos = w.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        expect(eventos![0][0]).toBe(2)
        expect(typeof eventos![0][0]).toBe("number")
    })

    it("quando obrigatorio=true, placeholder é desabilitado", () => {
        const w = mount(AppSelectComCriacao, {
            props: { opcoes, obrigatorio: true, placeholder: "Selecione" },
        })
        const primeira = w.findAll("option")[0]
        expect(primeira.text()).toBe("Selecione")
        expect(primeira.attributes("disabled")).toBeDefined()
    })

    it("quando obrigatorio=false (default), placeholder não é desabilitado", () => {
        const w = mount(AppSelectComCriacao, { props: { opcoes } })
        const primeira = w.findAll("option")[0]
        expect(primeira.attributes("disabled")).toBeUndefined()
    })

    it("desabilita select e botão quando desabilitado=true", () => {
        const w = mount(AppSelectComCriacao, { props: { opcoes, desabilitado: true } })
        expect(w.find("select").attributes("disabled")).toBeDefined()
        expect(w.find("button").attributes("disabled")).toBeDefined()
    })
})
