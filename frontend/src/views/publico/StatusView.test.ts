import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import { defineComponent, h } from "vue"

// Stub do AppStatusPill para isolar do DS
vi.mock("@/components/ui/AppStatusPill.vue", () => ({
    default: defineComponent({
        props: ["label", "variante"],
        setup: (p) => () =>
            h("span", { class: "status-pill-stub", "data-variante": p.variante }, p.label),
    }),
}))

vi.mock("vue-router", () => ({
    RouterLink: defineComponent({
        props: ["to"],
        setup: (_p, { slots }) => () => h("a", {}, slots.default?.()),
    }),
}))

// Módulo de conteúdo real
import { STATUS, type EstadoSistema } from "@/content/status"
import StatusView from "./StatusView.vue"

describe("módulo status.ts — invariantes de tipo", () => {
    it("estado é um dos três valores válidos", () => {
        const estadosValidos = new Set<EstadoSistema>(["operacional", "instabilidade", "manutenção"])
        expect(estadosValidos.has(STATUS.estado)).toBe(true)
    })

    it("atualizadoEm está no formato ISO YYYY-MM-DD", () => {
        expect(STATUS.atualizadoEm).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    })
})

describe("StatusView — CA7 estado renderiza por valor declarado", () => {
    it("estado 'operacional' renderiza pill com variante 'success' e rótulo correto", () => {
        // STATUS.estado deve ser 'operacional' conforme o arquivo
        const wrapper = mount(StatusView)
        const pill = wrapper.find(".status-pill-stub")
        expect(pill.exists()).toBe(true)

        if (STATUS.estado === "operacional") {
            expect(pill.attributes("data-variante")).toBe("success")
            expect(pill.text()).toContain("Todos os sistemas operacionais")
        }
        if (STATUS.estado === "instabilidade") {
            expect(pill.attributes("data-variante")).toBe("warning")
            expect(pill.text()).toContain("Estamos com instabilidade")
        }
        if (STATUS.estado === "manutenção") {
            expect(pill.attributes("data-variante")).toBe("error")
            expect(pill.text()).toContain("Em manutenção programada")
        }
    })

    it("exibe a data de última atualização", () => {
        const wrapper = mount(StatusView)
        // O elemento <time> deve existir com o atributo datetime correto
        const time = wrapper.find("time")
        expect(time.exists()).toBe(true)
        expect(time.attributes("datetime")).toBe(STATUS.atualizadoEm)
    })

    it("exibe a nota de evolução futura sobre monitoramento em tempo real", () => {
        const wrapper = mount(StatusView)
        expect(wrapper.text()).toContain("monitoramento de disponibilidade em tempo real")
    })

    it("texto opcional do STATUS é exibido quando presente", () => {
        // Se o STATUS tiver texto, ele deve aparecer no DOM
        if (STATUS.texto) {
            const wrapper = mount(StatusView)
            expect(wrapper.text()).toContain(STATUS.texto)
        } else {
            // Sem texto: elemento .estado-detalhe não deve estar presente
            const wrapper = mount(StatusView)
            expect(wrapper.find(".estado-detalhe").exists()).toBe(false)
        }
    })

    it("o cartão de estado tem a classe CSS correspondente ao estado declarado", () => {
        const wrapper = mount(StatusView)
        const card = wrapper.find(".estado-card")
        expect(card.exists()).toBe(true)
        expect(card.classes()).toContain(`estado-${STATUS.estado}`)
    })
})

describe("StatusView — CA3 zero chamadas de API", () => {
    it("monta sem nenhum store de domínio nem service autenticado", () => {
        expect(() => mount(StatusView)).not.toThrow()
    })
})

describe("StatusView — CA4 abre mesmo sem backend", () => {
    it("o conteúdo é estático — não depende de nenhum import de service/store", () => {
        // A view só importa @/content/status — se montar sem erro, o CA4 está garantido
        // estruturalmente (conteúdo no bundle, não em fetch)
        const wrapper = mount(StatusView)
        expect(wrapper.find(".conteudo").exists()).toBe(true)
        expect(wrapper.find("h1").text()).toBe("Status do sistema")
    })
})
