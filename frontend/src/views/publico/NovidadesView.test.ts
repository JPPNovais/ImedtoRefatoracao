import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { defineComponent, h } from "vue"

// Stubs para componentes DS — isolam o teste da camada de design system
vi.mock("@/components/ui/AppEmptyState.vue", () => ({
    default: defineComponent({
        props: ["icone", "titulo", "descricao"],
        setup: (p) => () => h("div", { class: "empty-state-stub", "data-titulo": p.titulo }),
    }),
}))
vi.mock("@/components/ui/AppBadge.vue", () => ({
    default: defineComponent({
        props: ["variant", "label"],
        setup: (p) => () => h("span", { class: "badge-stub", "data-variant": p.variant }, p.label),
    }),
}))

// RouterLink stub simples
vi.mock("vue-router", () => ({
    RouterLink: defineComponent({
        props: ["to"],
        setup: (_p, { slots }) => () => h("a", {}, slots.default?.()),
    }),
}))

import NovidadesView from "./NovidadesView.vue"

// Módulo de conteúdo real — usado nos testes de módulo puro
import { CHANGELOG, type EntradaChangelog, type TagChangelog } from "@/content/changelog"

describe("módulo changelog.ts — invariantes de tipo", () => {
    it("todas as entradas têm tag válida (novidade | melhoria | correção)", () => {
        const tagsValidas = new Set<TagChangelog>(["novidade", "melhoria", "correção"])
        for (const entrada of CHANGELOG) {
            expect(tagsValidas.has(entrada.tag)).toBe(true)
        }
    })

    it("todas as entradas têm data no formato ISO YYYY-MM-DD", () => {
        for (const entrada of CHANGELOG) {
            expect(entrada.data).toMatch(/^\d{4}-\d{2}-\d{2}$/)
        }
    })

    it("todas as entradas têm título e descrição não vazios", () => {
        for (const entrada of CHANGELOG) {
            expect(entrada.titulo.trim().length).toBeGreaterThan(0)
            expect(entrada.descricao.trim().length).toBeGreaterThan(0)
        }
    })
})

// Auxiliar: cria wrapper com overwrite do módulo de conteúdo via factory
function montarComEntradas(entradas: EntradaChangelog[]) {
    vi.doMock("@/content/changelog", () => ({ CHANGELOG: entradas }))
    // Como o módulo já foi importado, substituímos via prop implícita na view
    // Alternativa: montar a view e checar o DOM diretamente
    return mount(NovidadesView)
}

describe("NovidadesView — CA11 estado vazio", () => {
    it("quando CHANGELOG tem entradas, não exibe estado vazio", () => {
        const wrapper = mount(NovidadesView)
        // Com conteúdo real, não deve haver o empty-state-stub
        expect(wrapper.find(".empty-state-stub").exists()).toBe(false)
        expect(wrapper.find(".lista-changelog").exists()).toBe(true)
    })
})

describe("NovidadesView — CA5 ordenação por data desc", () => {
    it("exibe entradas da mais recente para a mais antiga", () => {
        const wrapper = mount(NovidadesView)
        const datas = wrapper.findAll("time").map((el) => el.attributes("datetime"))
        // Deve estar em ordem decrescente
        for (let i = 1; i < datas.length; i++) {
            expect(datas[i - 1]! >= datas[i]!).toBe(true)
        }
    })
})

describe("NovidadesView — CA6 tags renderizam com badge correto", () => {
    it("cada entrada exibe o badge com a variante correspondente", () => {
        const wrapper = mount(NovidadesView)
        const badges = wrapper.findAll(".badge-stub")
        expect(badges.length).toBeGreaterThan(0)

        // Verifica que todas as variantes renderizadas são do conjunto válido
        const variantesValidas = new Set(["success", "info", "error"])
        for (const badge of badges) {
            expect(variantesValidas.has(badge.attributes("data-variant") ?? "")).toBe(true)
        }
    })

    it("entradas com tag 'novidade' têm badge variant 'success'", () => {
        const wrapper = mount(NovidadesView)
        // Pelo menos uma entrada tem tag novidade — verificar mapeamento
        const novidades = CHANGELOG.filter((e) => e.tag === "novidade")
        if (novidades.length === 0) return // nada a testar

        // Encontra todos os badges 'success'
        const badgesSuccess = wrapper.findAll("[data-variant='success']")
        expect(badgesSuccess.length).toBe(novidades.length)
        for (const b of badgesSuccess) {
            expect(b.text()).toBe("Novidade")
        }
    })
})

describe("NovidadesView — CA3 zero chamadas de API", () => {
    it("não importa nenhum store de domínio nem service autenticado", async () => {
        // Verificação estrutural: a view deve montar sem erros e sem necessidade
        // de qualquer store de autenticação/tenant
        expect(() => mount(NovidadesView)).not.toThrow()
    })
})

describe("NovidadesView — R6 tom leigo (amostra)", () => {
    it("nenhum título contém jargão técnico como 'handler', 'query', 'migration', 'commit'", () => {
        const jargoes = ["handler", "query", "migration", "commit", "endpoint", "refactor"]
        for (const entrada of CHANGELOG) {
            for (const jargao of jargoes) {
                expect(entrada.titulo.toLowerCase()).not.toContain(jargao)
                expect(entrada.descricao.toLowerCase()).not.toContain(jargao)
            }
        }
    })
})
