import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import AppAutocompleteCriavel from "./AppAutocompleteCriavel.vue"

const OPCOES = ["Penicilina", "Dipirona", "Hipertensão"]

describe("AppAutocompleteCriavel", () => {
    // ── CA1: sugestões carregadas aparecem no dropdown ───────────────────────

    it("abre dropdown com todas as opções ao focar", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES },
        })
        await w.find("input").trigger("focus")
        const items = w.findAll(".dropdown-item")
        expect(items).toHaveLength(3)
        expect(items[0].text()).toBe("Penicilina")
    })

    // ── Filtro client-side ───────────────────────────────────────────────────

    it("filtra opções conforme o texto digitado", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "dip", opcoes: OPCOES },
        })
        await w.find("input").trigger("focus")
        const items = w.findAll(".dropdown-item")
        expect(items).toHaveLength(1)
        expect(items[0].text()).toBe("Dipirona")
    })

    it("filtro é case-insensitive e ignora acentos", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "hipertensao", opcoes: OPCOES },
        })
        await w.find("input").trigger("focus")
        const items = w.findAll(".dropdown-item")
        expect(items).toHaveLength(1)
        expect(items[0].text()).toBe("Hipertensão")
    })

    // ── CA10: estado vazio ────────────────────────────────────────────────────

    it("exibe mensagem de vazio quando lista está vazia", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: [] },
        })
        await w.find("input").trigger("focus")
        expect(w.find(".dropdown-vazio").exists()).toBe(true)
        expect(w.find(".dropdown-vazio").text()).toContain("Nenhuma opção cadastrada")
        expect(w.find(".dropdown-vazio").text()).toContain("digite para criar")
    })

    // ── CA11: degradação em erro ──────────────────────────────────────────────

    it("não exibe dropdown quando erro=true", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: [], erro: true },
        })
        await w.find("input").trigger("focus")
        expect(w.find(".dropdown").exists()).toBe(false)
    })

    it("input permanece editável quando erro=true (degrada para input puro)", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: [], erro: true },
        })
        const input = w.find("input")
        expect(input.attributes("disabled")).toBeUndefined()
    })

    // ── Emissão de update:modelValue ─────────────────────────────────────────

    it("emite update:modelValue ao digitar", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES },
        })
        await w.find("input").setValue("Sulfa")
        const events = w.emitted("update:modelValue")
        expect(events).toBeTruthy()
        expect(events![events!.length - 1][0]).toBe("Sulfa")
    })

    it("emite update:modelValue com o nome ao selecionar opção do dropdown", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES },
        })
        await w.find("input").trigger("focus")
        await w.findAll(".dropdown-item")[0].trigger("mousedown")
        const events = w.emitted("update:modelValue")
        expect(events).toBeTruthy()
        expect(events![0][0]).toBe("Penicilina")
    })

    it("valor inédito é aceito sem bloquear o campo", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES },
        })
        await w.find("input").setValue("Sulfa")
        // Apenas verifica que o campo aceita o valor e emite (não lança erro)
        const events = w.emitted("update:modelValue")
        expect(events).toBeTruthy()
    })

    // ── Estado carregando ─────────────────────────────────────────────────────

    it("input desabilitado e sem dropdown quando carregando=true", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: [], carregando: true },
        })
        await w.find("input").trigger("focus")
        expect(w.find(".dropdown").exists()).toBe(false)
        expect(w.find("input").attributes("disabled")).toBeDefined()
    })

    // ── Disabled ─────────────────────────────────────────────────────────────

    it("input desabilitado quando disabled=true", () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES, disabled: true },
        })
        expect(w.find("input").attributes("disabled")).toBeDefined()
    })

    // ── Navegação por teclado ─────────────────────────────────────────────────

    it("Enter seleciona item destacado com seta ↓", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES },
        })
        const input = w.find("input")
        await input.trigger("focus")
        await input.trigger("keydown", { key: "ArrowDown" })
        await input.trigger("keydown", { key: "Enter" })

        const events = w.emitted("update:modelValue")
        expect(events).toBeTruthy()
        expect(events![0][0]).toBe("Penicilina")
    })

    it("Escape fecha o dropdown", async () => {
        const w = mount(AppAutocompleteCriavel, {
            props: { modelValue: "", opcoes: OPCOES },
        })
        await w.find("input").trigger("focus")
        expect(w.find(".dropdown").exists()).toBe(true)
        await w.find("input").trigger("keydown", { key: "Escape" })
        expect(w.find(".dropdown").exists()).toBe(false)
    })
})
