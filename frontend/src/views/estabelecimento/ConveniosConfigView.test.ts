import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import ConveniosConfigView from "./ConveniosConfigView.vue"
import type { ConvenioListado } from "@/services/convenioService"

// ── Mocks ─────────────────────────────────────────────────────────────────────

vi.mock("@/services/convenioService", () => ({
    convenioService: {
        listar: vi.fn(),
        obter: vi.fn(),
        criar: vi.fn(),
        atualizar: vi.fn(),
        excluir: vi.fn(),
        adicionarPlano: vi.fn(),
        inativarPlano: vi.fn(),
        atualizarPlano: vi.fn(),
    },
}))

let _podeGerenciar = true
let _ehDono = false

vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: () => ({
        get ehDono() { return _ehDono },
        pode: (acao: string) => acao === "convenios.gerenciar" ? _podeGerenciar : true,
    }),
}))

const { convenioService } = await import("@/services/convenioService")

// ── Helpers ───────────────────────────────────────────────────────────────────

const CONVENIO: ConvenioListado = {
    id: 1,
    nome: "Unimed",
    registroAns: "123456",
    ativo: true,
    totalPlanos: 2,
}

const CONVENIO_INATIVO: ConvenioListado = {
    ...CONVENIO,
    id: 2,
    nome: "SulAmérica",
    ativo: false,
}

const globalStubs = {
    AppEmptyState: {
        template: `<div class="app-empty-state" data-testid="empty-state" />`,
        props: ["icone", "titulo", "descricao"],
    },
    AppButton: {
        template: `<button @click="$emit('click')"><slot /></button>`,
        props: ["variante", "icone", "executando"],
        emits: ["click"],
    },
    AppBadge: {
        template: `<span class="app-badge"><slot /></span>`,
        props: ["variante"],
    },
    AppDrawer: {
        template: `<div class="app-drawer" />`,
        props: ["aberto", "titulo"],
        emits: ["update:aberto"],
    },
    AppField: {
        template: `<div class="app-field"><slot /></div>`,
        props: ["label", "required"],
    },
    AppConfirmDialog: {
        template: `<div class="confirm-dialog" />`,
        props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante", "icone", "executando"],
        emits: ["update:aberto", "confirmar"],
    },
    AppToast: {
        template: `<div class="app-toast" />`,
        props: ["mensagem", "variante"],
        emits: ["fechar"],
    },
}

// ── Testes ─────────────────────────────────────────────────────────────────────

describe("ConveniosConfigView — CRUD de convênios (F6/R1-R4)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        _podeGerenciar = true
        _ehDono = false
    })

    it("carrega e exibe lista de convênios", async () => {
        ;(convenioService.listar as any).mockResolvedValue([CONVENIO])
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        expect(wrapper.text()).toContain("Unimed")
        expect(wrapper.text()).toContain("ANS 123456")
    })

    it("exibe empty state quando não há convênios", async () => {
        ;(convenioService.listar as any).mockResolvedValue([])
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        expect(wrapper.find("[data-testid='empty-state']").exists()).toBe(true)
    })

    it("exibe badge Ativo e Inativo corretamente", async () => {
        ;(convenioService.listar as any).mockResolvedValue([CONVENIO, CONVENIO_INATIVO])
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        expect(wrapper.text()).toContain("Ativo")
        expect(wrapper.text()).toContain("Inativo")
    })

    it("exibe mensagem de erro quando falha ao carregar", async () => {
        ;(convenioService.listar as any).mockRejectedValue(new Error("network"))
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        expect(wrapper.text()).toContain("Não foi possível carregar")
    })
})

describe("ConveniosConfigView — RBAC (CA133)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        _podeGerenciar = true
        _ehDono = false
        ;(convenioService.listar as any).mockResolvedValue([CONVENIO])
    })

    it("CA133 — com convenios.gerenciar, botão Novo convênio aparece", async () => {
        _podeGerenciar = true
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Novo convênio"))).toBe(true)
    })

    it("CA133 — sem convenios.gerenciar, botão Novo convênio oculto", async () => {
        _podeGerenciar = false
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Novo convênio"))).toBe(false)
    })

    it("CA133 — sem convenios.gerenciar, botões editar/excluir de convênio ficam ocultos", async () => {
        _podeGerenciar = false
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        expect(wrapper.find(".btn-icon-editar").exists()).toBe(false)
        expect(wrapper.find(".btn-icon-excluir").exists()).toBe(false)
    })

    it("CA133 — Dono sem convenios.gerenciar explícito ainda vê os botões (via ehDono)", async () => {
        _podeGerenciar = false
        _ehDono = true
        const wrapper = mount(ConveniosConfigView, { global: { stubs: globalStubs } })
        await flushPromises()
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Novo convênio"))).toBe(true)
    })
})
