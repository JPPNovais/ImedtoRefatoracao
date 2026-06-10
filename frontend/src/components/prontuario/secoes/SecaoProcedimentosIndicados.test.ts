/**
 * Testes de SecaoProcedimentosIndicados — briefing 2026-06-10_011 (F3)
 *
 * Cobre: CA43 (selecionar catálogo), CA44 (observação opcional), CA45 (criar inline +
 * auto-seleção), CA48 (RBAC — oculta criar), CA50 (degradação graciosa), CA52 (legado
 * read-only), CA53 (sem resultado + CTA criar), CA54 (1 carga), CA55 (erro genérico),
 * CA56 (validação mini-form), CA57 (chave dispatcher intacta — estrutural).
 */

import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import SecaoProcedimentosIndicados from "./SecaoProcedimentosIndicados.vue"

// ── Stubs de UI simples ────────────────────────────────────────────────────────

const AppInputStub = {
    name: "AppInput",
    props: ["modelValue", "placeholder", "disabled", "type", "min", "autofocus"],
    emits: ["update:modelValue"],
    template: `<input :value="modelValue" :placeholder="placeholder" :disabled="disabled" @input="$emit('update:modelValue', $event.target.value)" />`,
}

const AppInputDecimalStub = {
    name: "AppInputDecimal",
    props: ["modelValue", "decimals", "placeholder", "disabled"],
    emits: ["update:modelValue"],
    template: `<input :value="modelValue" :placeholder="placeholder" @input="$emit('update:modelValue', $event.target.value)" />`,
}

const AppTextareaStub = {
    name: "AppTextarea",
    props: ["modelValue", "rows", "placeholder", "disabled"],
    emits: ["update:modelValue"],
    template: `<textarea :value="modelValue" :disabled="disabled" @input="$emit('update:modelValue', $event.target.value)" />`,
}

const AppButtonStub = {
    name: "AppButton",
    props: ["variant", "size", "type", "loading", "disabled", "icon"],
    emits: ["click"],
    template: `<button :type="type || 'button'" :disabled="disabled" @click="$emit('click', $event)"><slot /></button>`,
}

const globalStubs = {
    AppInput: AppInputStub,
    AppInputDecimal: AppInputDecimalStub,
    AppTextarea: AppTextareaStub,
    AppButton: AppButtonStub,
}

// ── Catálogo de exemplo ────────────────────────────────────────────────────────

const catalogoFixo = [
    {
        id: 1,
        estabelecimentoId: 10,
        descricao: "Infiltração articular",
        valorBase: 350,
        duracaoPadraoMinutos: 30,
        codigoInterno: null,
        codigoTuss: null,
        categoria: null,
        ativo: true,
        criadaEm: "2026-01-01T00:00:00Z",
        atualizadaEm: null,
    },
    {
        id: 2,
        estabelecimentoId: 10,
        descricao: "Drenagem postural",
        valorBase: 120,
        duracaoPadraoMinutos: null,
        codigoInterno: null,
        codigoTuss: null,
        categoria: null,
        ativo: true,
        criadaEm: "2026-01-01T00:00:00Z",
        atualizadaEm: null,
    },
]

// ── Mock do service ────────────────────────────────────────────────────────────
// Nota: vi.mock é hoisted — não pode referenciar catalogoFixo aqui.
// O valor padrão é definido em cada beforeEach via mockResolvedValue.

vi.mock("@/services/orcamentoCatalogoService", () => ({
    orcamentoCatalogoService: {
        listarProcedimentos: vi.fn(),
        criarProcedimento: vi.fn(),
    },
}))

// ── Mock do permissoesStore ────────────────────────────────────────────────────

vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: vi.fn(),
}))

// ── Mock do useDebouncedRef (retorna o ref original sem delay) ─────────────────

vi.mock("@/composables/useDebouncedRef", () => ({
    useDebouncedRef: (r: unknown) => r,
}))

// ── Helpers ────────────────────────────────────────────────────────────────────

import { usePermissoesStore } from "@/stores/permissoesStore"
import { orcamentoCatalogoService } from "@/services/orcamentoCatalogoService"

function mockPermissoes(pode: (k: string) => boolean) {
    (usePermissoesStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({ pode })
}

/** Monta com catálogo disponível (mockResolvedValue). */
async function montarComCatalogo(
    modelValue = {},
    readOnly = false,
    podeCatalogo = true,
) {
    const listar = orcamentoCatalogoService.listarProcedimentos as ReturnType<typeof vi.fn>
    listar.mockResolvedValue(catalogoFixo)
    mockPermissoes(() => podeCatalogo)
    const wrapper = mount(SecaoProcedimentosIndicados, {
        props: { modelValue, readOnly },
        global: { stubs: globalStubs },
    })
    await flushPromises()
    return wrapper
}

/** Monta com catálogo indisponível (mockRejectedValue). */
async function montarSemCatalogo(modelValue = {}, readOnly = false) {
    const listar = orcamentoCatalogoService.listarProcedimentos as ReturnType<typeof vi.fn>
    listar.mockRejectedValue(Object.assign(new Error("Forbidden"), { response: { status: 403 } }))
    mockPermissoes(() => false)
    const wrapper = mount(SecaoProcedimentosIndicados, {
        props: { modelValue, readOnly },
        global: { stubs: globalStubs },
    })
    await flushPromises()
    return wrapper
}

// ── Testes ─────────────────────────────────────────────────────────────────────

describe("SecaoProcedimentosIndicados — CA54: 1 carga por abertura", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("chama listarProcedimentos exatamente 1 vez ao montar", async () => {
        await montarComCatalogo()
        expect(orcamentoCatalogoService.listarProcedimentos).toHaveBeenCalledTimes(1)
    })
})

describe("SecaoProcedimentosIndicados — CA43: selecionar do catálogo", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("renderiza opções do catálogo ao focar a busca", async () => {
        const wrapper = await montarComCatalogo()
        const input = wrapper.find(".ip-search-input")
        await input.trigger("focus")
        // Dropdown aberto → deve exibir os 2 itens
        const opcoes = wrapper.findAll(".ip-option")
        expect(opcoes).toHaveLength(2)
        expect(opcoes[0].text()).toContain("Infiltração articular")
    })

    it("ao clicar em + emite update:modelValue com snapshot do catálogo (CA43/R5)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] })
        await wrapper.find(".ip-search-input").trigger("focus")
        const opcoes = wrapper.findAll(".ip-option")
        await opcoes[0].trigger("click")

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        const payload = (eventos![0][0] as { procedimentos: unknown[] })
        const item = payload.procedimentos[0] as Record<string, unknown>
        expect(item.catalogoCirurgiaId).toBe(1)
        expect(item.descricao).toBe("Infiltração articular")
        expect(item.valor).toBe(350)
        expect(item.observacao).toBe("")
    })

    it("item selecionado aparece em Selecionados com descrição e valor", async () => {
        const wrapper = await montarComCatalogo({
            procedimentos: [
                { catalogoCirurgiaId: 1, descricao: "Infiltração articular", valor: 350, observacao: "" },
            ],
        })
        expect(wrapper.find(".item-desc").text()).toContain("Infiltração articular")
        expect(wrapper.find(".item-valor").text()).toContain("350")
    })
})

describe("SecaoProcedimentosIndicados — CA44: observação opcional", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("editando observação emite update sem exigir preenchimento (CA44)", async () => {
        const wrapper = await montarComCatalogo({
            procedimentos: [
                { catalogoCirurgiaId: 1, descricao: "Infiltração articular", valor: 350, observacao: "" },
            ],
        })
        const obsInput = wrapper.find(".item-obs")
        await obsInput.setValue("joelho D")

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        const payload = (eventos![0][0] as { procedimentos: Array<Record<string, unknown>> })
        expect(payload.procedimentos[0].observacao).toBe("joelho D")
    })
})

describe("SecaoProcedimentosIndicados — CA45: criação inline + auto-seleção", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("cria procedimento e auto-seleciona; 1 chamada ao service (CA45)", async () => {
        const novoId = 99
        ;(orcamentoCatalogoService.criarProcedimento as ReturnType<typeof vi.fn>).mockResolvedValue({ id: novoId })

        const wrapper = await montarComCatalogo({ procedimentos: [] })
        await wrapper.find(".ip-search-input").trigger("focus")

        // Clica em "Criar procedimento"
        const criarBtn = wrapper.find(".ip-criar-btn")
        expect(criarBtn.exists()).toBe(true)
        await criarBtn.trigger("click")

        // Preenche o mini-form
        const inputs = wrapper.findAll(".ip-create-form input")
        await inputs[0].setValue("Curativo simples")   // nome
        await inputs[1].setValue("5000")               // valor (centavos via AppInputDecimal stub)

        // Confirma
        const btnCriar = wrapper.findAll(".ip-create-acoes button")[1]
        await btnCriar.trigger("click")
        await flushPromises()

        expect(orcamentoCatalogoService.criarProcedimento).toHaveBeenCalledTimes(1)

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        const payload = (eventos![0][0] as { procedimentos: Array<Record<string, unknown>> })
        const item = payload.procedimentos[0]
        expect(item.catalogoCirurgiaId).toBe(novoId)
        expect(item.descricao).toBe("Curativo simples")
    })

    it("pré-preenche nome com o termo buscado ao acionar criar (CA53)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] })
        const input = wrapper.find(".ip-search-input")
        await input.trigger("focus")
        await input.setValue("Curativo")

        const criarBtn = wrapper.find(".ip-criar-btn")
        await criarBtn.trigger("click")

        // O input de nome do mini-form deve conter o termo
        const formNome = wrapper.find(".ip-create-form input") as any
        expect(formNome.element.value).toBe("Curativo")
    })
})

describe("SecaoProcedimentosIndicados — CA48: RBAC oculta criar sem permissão", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("sem orcamento.configurar, botão Criar não é renderizado (CA48)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] }, false, false)
        await wrapper.find(".ip-search-input").trigger("focus")
        expect(wrapper.find(".ip-criar-btn").exists()).toBe(false)
    })

    it("com orcamento.configurar, botão Criar é renderizado", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] }, false, true)
        await wrapper.find(".ip-search-input").trigger("focus")
        expect(wrapper.find(".ip-criar-btn").exists()).toBe(true)
    })
})

describe("SecaoProcedimentosIndicados — CA50: degradação graciosa sem acesso", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("quando GET retorna 403, exibe modo manual sem busca e sem botão criar (CA50)", async () => {
        const wrapper = await montarSemCatalogo({ procedimentos: [] })

        expect(wrapper.find(".ip-search-input").exists()).toBe(false)
        expect(wrapper.find(".ip-criar-btn").exists()).toBe(false)
        // Botão adicionar do modo manual presente
        expect(wrapper.find("button").exists()).toBe(true)
    })

    it("modo degradado permanece utilizável (adiciona linha manual)", async () => {
        const wrapper = await montarSemCatalogo({ procedimentos: [] })

        // Deve ter o botão Adicionar procedimento (modo manual)
        const botao = wrapper.findAll("button").find(b => b.text().includes("Adicionar"))
        expect(botao?.exists()).toBe(true)
        await botao!.trigger("click")

        const eventos = wrapper.emitted("update:modelValue")
        expect(eventos).toBeTruthy()
        const payload = (eventos![0][0] as { procedimentos: Array<Record<string, unknown>> })
        expect(payload.procedimentos).toHaveLength(1)
        // Formato legado: sem catalogoCirurgiaId
        expect(payload.procedimentos[0].catalogoCirurgiaId).toBeUndefined()
    })
})

describe("SecaoProcedimentosIndicados — CA52: retrocompat legado read-only", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("itens legados (sem catalogoCirurgiaId) renderizam em read-only sem quebrar (CA52)", async () => {
        const wrapper = await montarComCatalogo(
            {
                procedimentos: [
                    { descricao: "Curativo simples", observacao: "tecido granulado" },
                    { catalogoCirurgiaId: 1, descricao: "Infiltração articular", valor: 350, observacao: "" },
                ],
            },
            true, // readOnly
        )

        const itens = wrapper.findAll(".item-selecionado")
        expect(itens).toHaveLength(2)

        // Item legado: sem .item-valor
        const legado = itens[0]
        expect(legado.classes()).toContain("item-legado")
        expect(legado.find(".item-valor").exists()).toBe(false)
        expect(legado.find(".item-desc").text()).toContain("Curativo simples")

        // Item catálogo: com .item-valor
        const catalogo = itens[1]
        expect(catalogo.classes()).not.toContain("item-legado")
        expect(catalogo.find(".item-valor").text()).toContain("350")
    })
})

describe("SecaoProcedimentosIndicados — CA53: sem resultado + CTA", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("busca sem resultado mostra mensagem e CTA criar com termo (CA53)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] })
        const input = wrapper.find(".ip-search-input")
        await input.trigger("focus")
        await input.setValue("TermoInexistente")

        expect(wrapper.find(".ip-noresult").exists()).toBe(true)
        expect(wrapper.find(".ip-noresult").text()).toContain("TermoInexistente")

        // CTA criar com destaque
        const criarBtn = wrapper.find(".ip-criar-btn")
        expect(criarBtn.exists()).toBe(true)
        expect(criarBtn.classes()).toContain("ip-criar-btn--destaque")
        expect(criarBtn.text()).toContain('"TermoInexistente"')
    })

    it("sem permissão, busca sem resultado mostra apenas mensagem, sem CTA criar (CA53/CA48)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] }, false, false)
        const input = wrapper.find(".ip-search-input")
        await input.trigger("focus")
        await input.setValue("TermoInexistente")

        expect(wrapper.find(".ip-noresult").exists()).toBe(true)
        expect(wrapper.find(".ip-criar-btn").exists()).toBe(false)
    })
})

describe("SecaoProcedimentosIndicados — CA55: erro genérico ao criar", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("erro no criarProcedimento exibe mensagem genérica, sem PII (CA55)", async () => {
        (orcamentoCatalogoService.criarProcedimento as ReturnType<typeof vi.fn>).mockRejectedValue(
            new Error("Internal Server Error — tenant_id=10"),
        )

        const wrapper = await montarComCatalogo({ procedimentos: [] })
        await wrapper.find(".ip-search-input").trigger("focus")
        await wrapper.find(".ip-criar-btn").trigger("click")

        const inputs = wrapper.findAll(".ip-create-form input")
        await inputs[0].setValue("Procedimento X")
        await inputs[1].setValue("200")

        const btnCriar = wrapper.findAll(".ip-create-acoes button")[1]
        await btnCriar.trigger("click")
        await flushPromises()

        const erro = wrapper.find(".ip-create-erro")
        expect(erro.exists()).toBe(true)
        expect(erro.text()).toContain("Não foi possível criar o procedimento")
        // Não vaza detalhe técnico
        expect(erro.text()).not.toContain("tenant_id")
        expect(erro.text()).not.toContain("Internal Server Error")
    })
})

describe("SecaoProcedimentosIndicados — CA56: validação mini-form", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("tenta criar com nome vazio: bloqueia e exibe erro (CA56)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] })
        await wrapper.find(".ip-search-input").trigger("focus")
        await wrapper.find(".ip-criar-btn").trigger("click")

        // Deixa nome vazio, preenche só valor
        const inputs = wrapper.findAll(".ip-create-form input")
        await inputs[0].setValue("")
        await inputs[1].setValue("100")

        const btnCriar = wrapper.findAll(".ip-create-acoes button")[1]
        await btnCriar.trigger("click")
        await flushPromises()

        expect(orcamentoCatalogoService.criarProcedimento).not.toHaveBeenCalled()
        expect(wrapper.find(".ip-create-erro").exists()).toBe(true)
        expect(wrapper.find(".ip-create-erro").text()).toContain("Nome obrigatório")
    })

    it("tenta criar com valor inválido: bloqueia e exibe erro (CA56)", async () => {
        const wrapper = await montarComCatalogo({ procedimentos: [] })
        await wrapper.find(".ip-search-input").trigger("focus")
        await wrapper.find(".ip-criar-btn").trigger("click")

        const inputs = wrapper.findAll(".ip-create-form input")
        await inputs[0].setValue("Procedimento Y")
        await inputs[1].setValue("abc") // inválido

        const btnCriar = wrapper.findAll(".ip-create-acoes button")[1]
        await btnCriar.trigger("click")
        await flushPromises()

        expect(orcamentoCatalogoService.criarProcedimento).not.toHaveBeenCalled()
        expect(wrapper.find(".ip-create-erro").text()).toContain("Valor inválido")
    })
})

describe("SecaoProcedimentosIndicados — modo readOnly", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("em readOnly, não renderiza busca nem botão remover", async () => {
        const wrapper = await montarComCatalogo(
            { procedimentos: [{ catalogoCirurgiaId: 1, descricao: "Infiltração", valor: 350, observacao: "" }] },
            true,
        )
        expect(wrapper.find(".ip-search-input").exists()).toBe(false)
        expect(wrapper.find(".btn-icon-excluir").exists()).toBe(false)
    })
})
