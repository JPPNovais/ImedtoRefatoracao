import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import AppAvatar from "./AppAvatar.vue"

describe("AppAvatar", () => {
    it("renderiza img quando ha fotoUrl", () => {
        const w = mount(AppAvatar, {
            props: { nome: "João Silva", fotoUrl: "https://x/p.png" },
        })
        const img = w.find("img")
        expect(img.exists()).toBe(true)
        expect(img.attributes("src")).toBe("https://x/p.png")
        expect(img.attributes("alt")).toBe("João Silva")
        expect(w.find(".iniciais").exists()).toBe(false)
    })

    it("renderiza iniciais quando nao ha fotoUrl", () => {
        const w = mount(AppAvatar, {
            props: { nome: "João Silva", fotoUrl: null },
        })
        expect(w.find("img").exists()).toBe(false)
        expect(w.find(".iniciais").text()).toBe("JS")
    })

    it("gera iniciais com 2 caracteres do primeiro nome quando nome simples", () => {
        const w = mount(AppAvatar, { props: { nome: "Maria", fotoUrl: null } })
        expect(w.find(".iniciais").text()).toBe("MA")
    })

    it("usa ? quando nome esta vazio ou nulo", () => {
        const w1 = mount(AppAvatar, { props: { nome: null, fotoUrl: null } })
        expect(w1.find(".iniciais").text()).toBe("?")
        const w2 = mount(AppAvatar, { props: { nome: "", fotoUrl: null } })
        expect(w2.find(".iniciais").text()).toBe("?")
    })

    it("aceita corFundo prop pra sobrescrever a paleta", () => {
        // corFundo explicito = comportamento previsivel (sem depender do hash).
        const w = mount(AppAvatar, {
            props: { nome: "X", fotoUrl: null, corFundo: "red" },
        })
        // Como happy-dom serializa :style inline no atributo style do DOM,
        // checamos via getAttribute (mais robusto que el.style.background).
        const style = w.find(".avatar").element.getAttribute("style") ?? ""
        expect(style).toContain("red")
    })

    it("aplica classe de tamanho", () => {
        const w = mount(AppAvatar, { props: { nome: "X", tamanho: "lg" } })
        expect(w.find(".avatar--lg").exists()).toBe(true)
    })

    it("nao define background quando fotoUrl presente (img cobre o div)", () => {
        const w = mount(AppAvatar, { props: { nome: "X", fotoUrl: "u" } })
        const style = w.find(".avatar").element.getAttribute("style") ?? ""
        expect(style).not.toContain("background")
    })
})
