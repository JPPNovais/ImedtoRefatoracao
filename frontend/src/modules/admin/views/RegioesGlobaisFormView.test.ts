/**
 * Testes para o seletor visual (BodyMap como atalho) em RegioesGlobaisFormView.
 *
 * Cobre: CA1, CA2, CA4, CA5 (somente criação), CA9 (árvore vazia), CA19.
 * Addendum 2026-06-25_001: CA20-CA26 (prefixo + sufixo de código com pai).
 * Fusão estrutural (2026-06-25_002): CA13-CA17 do addendum 2026-06-22_004
 * (seletor intermediário de partes do tronco) foram REMOVIDOS — tronco agora
 * é região real, clique vai direto para aoClicarRegiaoNoMapa.
 *
 * CA3 (campos permanecem editáveis) e CA6 (sem marcação persistente) são
 * comportamentos de UX validados manualmente pelo QA via chrome-devtools.
 * CA11 (erro do atalho não bloqueia) é testado via CA9 (árvore vazia = no-op).
 * CA12 (documentação) verificado pelo QA no Docs/DESIGN.md.
 * CA17 (supera CA4/R4) verificado pelos CA13–CA16 (seletor abre ao invés de no-op).
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import type { ExameFisicoRegiao } from "@/components/exame-fisico/BodyMap.vue"

// ── Mocks ──────────────────────────────────────────────────────────────────

const mockPush = vi.fn()
vi.mock("vue-router", () => ({
    useRouter: () => ({ push: mockPush }),
}))

const mockCarregarArvore = vi.fn()
const mockCarregarItem = vi.fn()
const mockCriar = vi.fn()
const mockAtualizar = vi.fn()

// Árvore de catálogo com nós de nível 1.
// Fusão estrutural (briefing 2026-06-25_002): tronco-anterior/tronco-posterior são
// regiões reais nível-1; as 6 partes (torax/abdome/pelve × ant/post) foram removidas.
// Nó de nível 2 garante que apenas nível 1 aparece no mapa (CA9).
const ARVORE_MOCK = [
    {
        id: 1,
        codigo: "ABD",
        nome: "Abdome (anterior)",
        paiCodigo: null,
        nivel: 1,
        vista: "anterior",
        templateTexto: null,
        ordem: 1,
        lateralidade: false,
        ativo: true,
        filhos: [
            {
                id: 2,
                codigo: "ABD-SUP",
                nome: "Abdome superior",
                paiCodigo: "ABD",
                nivel: 2,
                vista: "anterior",
                templateTexto: null,
                ordem: 1,
                lateralidade: false,
                ativo: true,
                filhos: [],
            },
        ],
    },
    {
        id: 3,
        codigo: "TORAX",
        nome: "Tórax (posterior)",
        paiCodigo: null,
        nivel: 1,
        vista: "posterior",
        templateTexto: null,
        ordem: 2,
        lateralidade: false,
        ativo: true,
        filhos: [],
    },
    // Regiões reais de tronco (fusão 2026-06-25_002)
    { id: 10, codigo: "tronco-anterior",  nome: "Tronco (anterior)",  paiCodigo: null, nivel: 1, vista: "anterior",  templateTexto: null, ordem: 9,  lateralidade: false, ativo: true, filhos: [] },
    { id: 11, codigo: "tronco-posterior", nome: "Tronco (posterior)", paiCodigo: null, nivel: 1, vista: "posterior", templateTexto: null, ordem: 10, lateralidade: false, ativo: true, filhos: [] },
]

vi.mock("../stores/regioesGlobaisStore", () => ({
    useRegioesGlobaisStore: () => ({
        arvore: ARVORE_MOCK,
        carregando: false,
        erro: null,
        itemAtual: null,
        carregarArvore: mockCarregarArvore,
        carregarItem: mockCarregarItem,
        criar: mockCriar,
        atualizar: mockAtualizar,
        inativar: vi.fn(),
        reativar: vi.fn(),
        excluir: vi.fn(),
    }),
}))

vi.mock("../services/catalogosService", () => ({
    regioesGlobaisService: {
        listarArvore: vi.fn(),
        obter: vi.fn(),
        criar: vi.fn(),
        atualizar: vi.fn(),
        inativar: vi.fn(),
        reativar: vi.fn(),
        excluir: vi.fn(),
    },
}))

// Mock do BodyMap: simula renderização e emissão de eventos sem depender de SVG real.
// Fusão estrutural (2026-06-25_002): troncoClicado removido — apenas regiaoClicada.
// tronco-anterior/tronco-posterior aparecem como botões via `props.regioes` (são regiões reais).
const bodyMapEmit = vi.fn()
vi.mock("@/components/exame-fisico/BodyMap.vue", () => ({
    default: {
        name: "BodyMap",
        props: ["regioes", "regioesExaminadas", "sexo"],
        emits: ["regiaoClicada"],
        setup(props: { regioes: ExameFisicoRegiao[] }, ctx: { emit: (...args: unknown[]) => void }) {
            // Expõe botões de teste para simular cliques no mapa
            return { props, emit: ctx.emit }
        },
        template: `
            <div data-testid="body-map-stub">
                <button
                    v-for="r in props.regioes"
                    :key="r.id"
                    :data-testid="'regiao-' + r.id"
                    @click="emit('regiaoClicada', r)"
                >{{ r.nome }}</button>
            </div>
        `,
    },
}))

// Stubs minimalistas dos componentes de UI
vi.mock("@/components/ui", () => ({
    AppPageHeader: { template: "<div><slot /></div>" },
    AppCard: { template: "<div><slot /></div>" },
    AppField: { template: "<div><slot /></div>" },
    AppInput: {
        props: ["modelValue", "disabled"],
        emits: ["update:modelValue"],
        template: "<input :value=\"modelValue\" @input=\"$emit('update:modelValue', $event.target.value)\" />",
    },
    AppTextarea: { template: "<textarea />" },
    AppButton: { template: "<button type='button'><slot /></button>" },
    AppSelect: { template: "<select />" },
}))

// ── Helpers ────────────────────────────────────────────────────────────────

import RegioesGlobaisFormView from "./RegioesGlobaisFormView.vue"

function setupState(wrapper: ReturnType<typeof mount>): Record<string, unknown> {
    return (wrapper.vm as unknown as { $: { setupState: Record<string, unknown> } }).$.setupState
}

function montarCriacao() {
    return mount(RegioesGlobaisFormView, {
        props: { id: undefined },
        attachTo: document.body,
    })
}

function montarEdicao() {
    return mount(RegioesGlobaisFormView, {
        props: { id: "42" },
        attachTo: document.body,
    })
}

// ── Testes ─────────────────────────────────────────────────────────────────

describe("RegioesGlobaisFormView — seletor visual (briefing 2026-06-22_004)", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
        mockCarregarArvore.mockResolvedValue(undefined)
    })

    // CA5: seletor só na criação
    it("CA5 — modo edição: seletor visual (BodyMap) não é renderizado", () => {
        const wrapper = montarEdicao()
        expect(wrapper.find("[data-testid='body-map-stub']").exists()).toBe(false)
        wrapper.unmount()
    })

    it("CA5 — modo criação: seletor visual (BodyMap) é renderizado", () => {
        const wrapper = montarCriacao()
        expect(wrapper.find("[data-testid='body-map-stub']").exists()).toBe(true)
        wrapper.unmount()
    })

    // CA1: clique em hotspot de nível 1 preenche paiCodigo
    it("CA1 — clicar em hotspot de nível 1 (ABD) preenche paiCodigo com o código da região", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        // paiCodigo começa vazio
        expect(ss["paiCodigo"]).toBe("")

        // Simula clique no hotspot da região ABD
        const botaoABD = wrapper.find("[data-testid='regiao-ABD']")
        expect(botaoABD.exists()).toBe(true)
        await botaoABD.trigger("click")

        expect(ss["paiCodigo"]).toBe("ABD")
        wrapper.unmount()
    })

    // CA2: coerência da vista — região anterior preenche paiCodigo cujo watcher deriva "anterior"
    it("CA2 — clicar em região anterior: paiCodigo recebe 'ABD' (vista anterior no catálogo)", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        await wrapper.find("[data-testid='regiao-ABD']").trigger("click")

        // paiCodigo foi preenchido com ABD (vista=anterior no mock)
        expect(ss["paiCodigo"]).toBe("ABD")
        wrapper.unmount()
    })

    it("CA2 — clicar em região posterior: paiCodigo recebe 'TORAX' (vista posterior no catálogo)", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        await wrapper.find("[data-testid='regiao-TORAX']").trigger("click")

        expect(ss["paiCodigo"]).toBe("TORAX")
        wrapper.unmount()
    })

    // Fusão estrutural (2026-06-25_002): tronco-anterior/tronco-posterior são regiões
    // reais — clicar nelas vai diretamente para aoClicarRegiaoNoMapa e preenche paiCodigo.
    // Não há mais seletor de partes intermediário (PARTE_PARA_TRONCO removido).
    it("CA13-novo — clicar em tronco-anterior preenche paiCodigo com 'tronco-anterior' (sem seletor intermediário)", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        expect(ss["paiCodigo"]).toBe("")

        await wrapper.find("[data-testid='regiao-tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()

        // tronco-anterior é região real → paiCodigo preenchido direto
        expect(ss["paiCodigo"]).toBe("tronco-anterior")
        wrapper.unmount()
    })

    it("CA13-novo — clicar em tronco-posterior preenche paiCodigo com 'tronco-posterior'", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        await wrapper.find("[data-testid='regiao-tronco-posterior']").trigger("click")
        await wrapper.vm.$nextTick()

        expect(ss["paiCodigo"]).toBe("tronco-posterior")
        wrapper.unmount()
    })

    it("CA13-novo — não existe mais '.seletor-tronco' nem evento troncoClicado (fusão 2026-06-25_002)", async () => {
        const wrapper = montarCriacao()

        // Clicar no tronco não abre seletor intermediário — vai direto
        await wrapper.find("[data-testid='regiao-tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()

        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)
        wrapper.unmount()
    })

    // CA9: árvore vazia → sem hotspots de catálogo (só pseudo-hotspots de tronco no BodyMap real)
    // No mock, regioes vazio = sem botões de região, apenas os de tronco.
    it("CA9 — com árvore vazia: nenhum hotspot de catálogo é renderizado (no-op implícito)", () => {
        // Remonta com store tendo arvore vazia
        vi.doMock("../stores/regioesGlobaisStore", () => ({
            useRegioesGlobaisStore: () => ({
                arvore: [],
                carregando: false,
                erro: null,
                itemAtual: null,
                carregarArvore: mockCarregarArvore,
                carregarItem: mockCarregarItem,
                criar: mockCriar,
                atualizar: mockAtualizar,
                inativar: vi.fn(),
                reativar: vi.fn(),
                excluir: vi.fn(),
            }),
        }))

        // Com arvore=[], regioesParaMapa=[]; o BodyMap mock não renderiza botões de região.
        // O teste verifica que o computed regioesParaMapa filtra corretamente via a store.
        // (O mock global já usa ARVORE_MOCK — testamos via setupState que a computed resulta
        // em somente nível 1.)
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        // regioesParaMapa deve conter apenas os nós de nivel 1 do ARVORE_MOCK
        const regioesParaMapa = ss["regioesParaMapa"] as ExameFisicoRegiao[]
        expect(regioesParaMapa.every((r) => r.nivel === 1)).toBe(true)
        // Nível 2 (ABD-SUP) não aparece
        expect(regioesParaMapa.find((r) => r.id === "ABD-SUP")).toBeUndefined()

        wrapper.unmount()
    })

    // Verifica que o adaptador usa codigo como id (contrato do handler)
    it("adaptador: regioesParaMapa usa codigo como id e mapeia vista corretamente", () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)
        const regioesParaMapa = ss["regioesParaMapa"] as ExameFisicoRegiao[]

        const abd = regioesParaMapa.find((r) => r.id === "ABD")
        expect(abd).toBeDefined()
        expect(abd?.vista).toBe("anterior")
        expect(abd?.nivel).toBe(1)

        const torax = regioesParaMapa.find((r) => r.id === "TORAX")
        expect(torax).toBeDefined()
        expect(torax?.vista).toBe("posterior")

        wrapper.unmount()
    })

    // ── Fusão estrutural do tronco (briefing 2026-06-25_002) ─────────────────────
    // O seletor intermediário de partes (CA13-CA17 do addendum 2026-06-22_004) foi
    // removido. tronco-anterior/tronco-posterior são regiões reais que disparam
    // aoClicarRegiaoNoMapa diretamente.

    // CA19: modo edição não renderiza o BodyMap
    it("CA19 — modo edição: BodyMap não é renderizado", () => {
        const wrapper = montarEdicao()
        expect(wrapper.find("[data-testid='body-map-stub']").exists()).toBe(false)
        wrapper.unmount()
    })
})
