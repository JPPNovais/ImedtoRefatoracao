import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import ConveniosTab from "./ConveniosTab.vue"
import type { PacienteConvenioDto, ConvenioSelect } from "@/services/convenioService"
import { estaVencida } from "@/services/convenioService"

// ── Mocks ─────────────────────────────────────────────────────────────────────

vi.mock("@/services/convenioService", () => ({
    convenioService: {
        listarCarteirinhasPaciente: vi.fn(),
        listarAtivos: vi.fn(),
        criarCarteirinha: vi.fn(),
        atualizarCarteirinha: vi.fn(),
        excluirCarteirinha: vi.fn(),
    },
    estaVencida: (validade: string | null) => !!validade && new Date(validade) < new Date(),
}))

let _podeEditar = true
vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: () => ({
        get ehDono() { return false },
        pode: () => _podeEditar,
    }),
}))

const { convenioService } = await import("@/services/convenioService")

// ── Helpers ───────────────────────────────────────────────────────────────────

const CONVENIOS_ATIVOS: ConvenioSelect[] = [
    { id: 1, nome: "Unimed", planos: [{ id: 10, nome: "Fácil", ativo: true }] },
    { id: 2, nome: "SulAmérica", planos: [] },
]

const CARTEIRINHA_ATIVA: PacienteConvenioDto = {
    id: 100,
    convenioId: 1,
    convenioNome: "Unimed",
    planoId: 10,
    planoNome: "Fácil",
    numeroCarteirinha: "98765",
    validade: "2027-12-31",
    ativo: true,
}

// Validade passada: `estaVencida` calculado no front a partir desta data (R6)
const CARTEIRINHA_VENCIDA: PacienteConvenioDto = {
    ...CARTEIRINHA_ATIVA,
    id: 101,
    validade: "2024-01-01",
}

const globalStubs = {
    AppEmptyState: {
        template: `<div class="app-empty-state" data-testid="empty-state" />`,
        props: ["icone", "titulo", "descricao"],
    },
    AppButton: {
        template: `<button @click="$emit('click')"><slot /></button>`,
        props: ["variante", "icone", "tamanho", "executando"],
        emits: ["click"],
    },
    AppBadge: {
        template: `<span class="app-badge"><slot /></span>`,
        props: ["variante", "tamanho"],
    },
    AppDrawer: {
        template: `<div class="app-drawer" v-if="aberto"><slot /><slot name="titulo" /></div>`,
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

describe("ConveniosTab — comportamento geral (F6)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        _podeEditar = true
        ;(convenioService.listarAtivos as any).mockResolvedValue(CONVENIOS_ATIVOS)
    })

    // CA154: lazy-load — dispara HTTP na primeira vez que `ativa` vai a true

    it("CA154 — não carrega dados enquanto inativa", async () => {
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([])
        mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: false },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        expect(convenioService.listarCarteirinhasPaciente).not.toHaveBeenCalled()
    })

    it("CA154 — carrega ao ativar", async () => {
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([])
        mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: true },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        expect(convenioService.listarCarteirinhasPaciente).toHaveBeenCalledWith(10)
    })

    // CA140: RBAC — sem permissão, botão de adicionar oculto

    it("CA140 — sem pacientes.editar, botão Adicionar não aparece", async () => {
        _podeEditar = false
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([CARTEIRINHA_ATIVA])
        const wrapper = mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: true },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        // Botão "Adicionar" é condicional v-if="podeEditar()"
        const botoes = wrapper.findAll("button")
        const btnAdicionar = botoes.find(b => b.text().includes("Adicionar"))
        expect(btnAdicionar).toBeUndefined()
    })

    // CA141: alerta de carteirinha vencida

    it("CA141 — carteirinha vencida exibe alerta", async () => {
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([CARTEIRINHA_VENCIDA])
        const wrapper = mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: true },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        expect(wrapper.text()).toContain("vencida")
    })

    it("CA141 — carteirinha válida não exibe alerta de vencida", async () => {
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([CARTEIRINHA_ATIVA])
        const wrapper = mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: true },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        expect(wrapper.text()).not.toContain("vencida")
    })

    // Empty state quando não há carteirinhas

    it("exibe empty state quando não há carteirinhas", async () => {
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([])
        const wrapper = mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: true },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        expect(wrapper.find("[data-testid='empty-state']").exists()).toBe(true)
    })

    // CA155: cards "Em breve" sempre visíveis

    it("CA155 — cards Em breve exibidos", async () => {
        ;(convenioService.listarCarteirinhasPaciente as any).mockResolvedValue([])
        const wrapper = mount(ConveniosTab, {
            props: { pacienteId: 10, ativa: true },
            global: { stubs: globalStubs },
        })
        await flushPromises()
        expect(wrapper.text()).toContain("Coparticipação")
        expect(wrapper.text()).toContain("Conciliação")
        expect(wrapper.text()).toContain("Glosas")
        expect(wrapper.text()).toContain("Em breve")
    })
})
