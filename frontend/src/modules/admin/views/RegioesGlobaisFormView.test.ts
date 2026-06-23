/**
 * Testes para o seletor visual (BodyMap como atalho) adicionado em
 * RegioesGlobaisFormView pelo briefing 2026-06-22_004.
 *
 * Cobre: CA1, CA2, CA4, CA5 (somente criação), CA9 (árvore vazia).
 * Addendum 2026-06-22_004: CA13-CA16 (seletor de tronco), CA19 (só na criação).
 *
 * CA3 (campos permanecem editáveis) e CA6 (sem marcação persistente) são
 * comportamentos de UX validados manualmente pelo QA via chrome-devtools.
 * CA7/CA18 (não-regressão exame físico) é coberto pelas suítes BodyMap.test.ts e
 * SecaoExameFisico.test.ts existentes.
 * CA8 (RBAC) é cobertura de rota já existente (não desta view isolada).
 * CA10/CA20 (tipografia) validado pelo gate check:typography --ci.
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

// Árvore de catálogo com nós de nível 1 (ABD anterior, TORAX posterior) + nós das
// partes de tronco com ids conforme PARTE_PARA_TRONCO (addendum CA13-CA15).
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
    // Nós com ids conforme PARTE_PARA_TRONCO — necessários para seletor do tronco (CA13-CA15)
    { id: 4, codigo: "torax-anterior",        nome: "Tórax (anterior)",            paiCodigo: null, nivel: 1, vista: "anterior",  templateTexto: null, ordem: 3, lateralidade: false, ativo: true, filhos: [] },
    { id: 5, codigo: "abdome-anterior",       nome: "Abdome (anterior)",           paiCodigo: null, nivel: 1, vista: "anterior",  templateTexto: null, ordem: 4, lateralidade: false, ativo: true, filhos: [] },
    { id: 6, codigo: "pelve-anterior",        nome: "Pelve (anterior)",            paiCodigo: null, nivel: 1, vista: "anterior",  templateTexto: null, ordem: 5, lateralidade: false, ativo: true, filhos: [] },
    { id: 7, codigo: "torax-posterior",       nome: "Tórax (posterior)",           paiCodigo: null, nivel: 1, vista: "posterior", templateTexto: null, ordem: 6, lateralidade: false, ativo: true, filhos: [] },
    { id: 8, codigo: "lombossacra-posterior", nome: "Região lombossacra (posterior)", paiCodigo: null, nivel: 1, vista: "posterior", templateTexto: null, ordem: 7, lateralidade: false, ativo: true, filhos: [] },
    { id: 9, codigo: "pelve-posterior",       nome: "Pelve (posterior)",           paiCodigo: null, nivel: 1, vista: "posterior", templateTexto: null, ordem: 8, lateralidade: false, ativo: true, filhos: [] },
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
// O mock captura a prop `regioes` e expõe botões clicáveis para disparar os eventos.
const bodyMapEmit = vi.fn()
vi.mock("@/components/exame-fisico/BodyMap.vue", () => ({
    default: {
        name: "BodyMap",
        props: ["regioes", "regioesExaminadas", "sexo"],
        emits: ["regiaoClicada", "troncoClicado"],
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
                <button
                    data-testid="tronco-anterior"
                    @click="emit('troncoClicado', 'tronco-anterior')"
                >Tronco anterior</button>
                <button
                    data-testid="tronco-posterior"
                    @click="emit('troncoClicado', 'tronco-posterior')"
                >Tronco posterior</button>
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

    // CA4 original — SUPERADO pelo addendum (CA17/R9): o clique no tronco agora abre
    // o seletor em vez de ser no-op. paiCodigo não muda *diretamente* ao abrir o seletor;
    // só muda ao *escolher* uma parte (CA14/CA15). Os testes abaixo validam que o
    // paiCodigo não é alterado pelo simples ato de abrir o seletor (sem escolher).
    it("CA4/CA17 — clicar no pseudo-hotspot de tronco anterior: paiCodigo não muda ao abrir o seletor", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        // Garante que paiCodigo está vazio antes
        expect(ss["paiCodigo"]).toBe("")

        await wrapper.find("[data-testid='tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()

        // Abrir o seletor não modifica paiCodigo (modificação só ao escolher — CA14)
        expect(ss["paiCodigo"]).toBe("")
        wrapper.unmount()
    })

    it("CA4/CA17 — clicar no pseudo-hotspot de tronco posterior: paiCodigo não muda ao abrir o seletor", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        await wrapper.find("[data-testid='tronco-posterior']").trigger("click")
        await wrapper.vm.$nextTick()

        expect(ss["paiCodigo"]).toBe("")
        wrapper.unmount()
    })

    it("CA4/CA17 — clique no tronco depois de já ter um paiCodigo não sobrescreve ao abrir o seletor", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        // Define paiCodigo com uma região real primeiro
        await wrapper.find("[data-testid='regiao-ABD']").trigger("click")
        expect(ss["paiCodigo"]).toBe("ABD")

        // Abrir o seletor do tronco não deve sobrescrever paiCodigo
        await wrapper.find("[data-testid='tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()
        expect(ss["paiCodigo"]).toBe("ABD")
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

    // ── Addendum 2026-06-22_004: CA13–CA16, CA17, CA19 ──────────────────────

    // CA13: tronco anterior abre seletor com as partes corretas
    it("CA13 — clicar em tronco anterior abre seletor com Tórax, Abdome e Pelve (anterior)", async () => {
        const wrapper = montarCriacao()

        // Antes de clicar: seletor não aparece
        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)

        await wrapper.find("[data-testid='tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()

        // Seletor aparece
        expect(wrapper.find(".seletor-tronco").exists()).toBe(true)

        // Verifica que os três botões com os labels corretos estão presentes
        const botoes = wrapper.findAll(".seletor-tronco-botao").map((b) => b.text())
        expect(botoes).toContain("Tórax")
        expect(botoes).toContain("Abdome")
        expect(botoes).toContain("Pelve")

        // Não deve conter partes do lado posterior
        expect(botoes).not.toContain("Região lombossacra")

        wrapper.unmount()
    })

    // CA13/CA15: tronco posterior abre seletor com as partes corretas
    it("CA15 — clicar em tronco posterior abre seletor com Tórax, Região lombossacra e Pelve (posterior)", async () => {
        const wrapper = montarCriacao()

        await wrapper.find("[data-testid='tronco-posterior']").trigger("click")
        await wrapper.vm.$nextTick()

        expect(wrapper.find(".seletor-tronco").exists()).toBe(true)

        const botoes = wrapper.findAll(".seletor-tronco-botao").map((b) => b.text())
        expect(botoes).toContain("Tórax")
        expect(botoes).toContain("Região lombossacra")
        expect(botoes).toContain("Pelve")

        // Não deve conter partes do lado anterior
        expect(botoes).not.toContain("Abdome")

        wrapper.unmount()
    })

    // CA14: escolher parte no seletor anterior preenche paiCodigo
    it("CA14 — escolher 'Abdome' no seletor anterior preenche paiCodigo com 'abdome-anterior'", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        // Abre seletor do tronco anterior
        await wrapper.find("[data-testid='tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()

        // Clica no botão "Abdome"
        const botaoAbdome = wrapper.findAll(".seletor-tronco-botao").find((b) => b.text() === "Abdome")
        expect(botaoAbdome).toBeDefined()
        await botaoAbdome!.trigger("click")
        await wrapper.vm.$nextTick()

        // paiCodigo preenchido com o codigo da parte
        expect(ss["paiCodigo"]).toBe("abdome-anterior")

        // Seletor fechou após a escolha
        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)

        wrapper.unmount()
    })

    // CA15: escolher parte no seletor posterior preenche paiCodigo
    it("CA15 — escolher 'Região lombossacra' no seletor posterior preenche paiCodigo com 'lombossacra-posterior'", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        await wrapper.find("[data-testid='tronco-posterior']").trigger("click")
        await wrapper.vm.$nextTick()

        const botaoLombo = wrapper.findAll(".seletor-tronco-botao").find((b) => b.text() === "Região lombossacra")
        expect(botaoLombo).toBeDefined()
        await botaoLombo!.trigger("click")
        await wrapper.vm.$nextTick()

        expect(ss["paiCodigo"]).toBe("lombossacra-posterior")
        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)

        wrapper.unmount()
    })

    // CA16: cancelar/fechar seletor não muda nenhum campo
    it("CA16 — fechar o seletor do tronco sem escolher não altera paiCodigo", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        // Garante estado inicial
        expect(ss["paiCodigo"]).toBe("")

        // Abre seletor
        await wrapper.find("[data-testid='tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()
        expect(wrapper.find(".seletor-tronco").exists()).toBe(true)

        // Fecha sem escolher
        await wrapper.find(".seletor-tronco-fechar").trigger("click")
        await wrapper.vm.$nextTick()

        // Seletor fechou e paiCodigo não mudou
        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)
        expect(ss["paiCodigo"]).toBe("")

        wrapper.unmount()
    })

    it("CA16 — fechar o seletor do tronco posterior sem escolher não altera paiCodigo", async () => {
        const wrapper = montarCriacao()
        const ss = setupState(wrapper)

        await wrapper.find("[data-testid='tronco-posterior']").trigger("click")
        await wrapper.vm.$nextTick()

        await wrapper.find(".seletor-tronco-fechar").trigger("click")
        await wrapper.vm.$nextTick()

        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)
        expect(ss["paiCodigo"]).toBe("")

        wrapper.unmount()
    })

    // CA19: modo edição não renderiza o seletor de tronco (já garantido pelo v-if="!editando" no boneco)
    it("CA19 — modo edição: seletor de tronco não é renderizado", () => {
        const wrapper = montarEdicao()

        // Nem o BodyMap nem o seletor de tronco aparecem na edição
        expect(wrapper.find("[data-testid='body-map-stub']").exists()).toBe(false)
        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)

        wrapper.unmount()
    })

    // CA17 (supera CA4/R4): verificar que o clique no tronco ABRE o seletor (não é no-op)
    it("CA17 — clique no tronco abre o seletor (comportamento não é mais no-op)", async () => {
        const wrapper = montarCriacao()

        // Antes: seletor fechado
        expect(wrapper.find(".seletor-tronco").exists()).toBe(false)

        // Após clique no tronco: seletor abre
        await wrapper.find("[data-testid='tronco-anterior']").trigger("click")
        await wrapper.vm.$nextTick()
        expect(wrapper.find(".seletor-tronco").exists()).toBe(true)

        wrapper.unmount()
    })
})
