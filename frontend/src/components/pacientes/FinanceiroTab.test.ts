import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import FinanceiroTab from "./FinanceiroTab.vue"
import type { FinanceiroAba } from "@/services/cobrancaService"

// ── Mocks de serviços ─────────────────────────────────────────────────────────

vi.mock("@/services/cobrancaService", () => ({
    cobrancaService: {
        obterFinanceiroAba: vi.fn(),
        listarConfigTaxa: vi.fn().mockResolvedValue([]),
        registrarPagamentos: vi.fn(),
        estornarPagamento: vi.fn(),
    },
}))

vi.mock("@/services/categoriaFinanceiraService", () => ({
    formaPagamentoService: {
        listar: vi.fn().mockResolvedValue([]),
    },
}))

// ── Mock da store de permissões ───────────────────────────────────────────────

let _podeFn = vi.fn().mockReturnValue(true)
let _ehDono = false

vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: () => ({
        get ehDono() { return _ehDono },
        pode: (chave: string) => _podeFn(chave),
    }),
}))

// Mock pinia (não precisamos da store real)
vi.mock("pinia", async (importActual) => {
    const actual = await importActual<typeof import("pinia")>()
    return { ...actual }
})

// Acesso ao mock de serviço
const { cobrancaService } = await import("@/services/cobrancaService")

// ── Stub de componentes pesados ───────────────────────────────────────────────

const globalConfig = {
    stubs: {
        AppEmptyState: {
            template: `<div class="app-empty-state"><slot /></div>`,
            props: ["icone", "titulo", "descricao"],
        },
        AppButton: {
            template: `<button @click="$emit('click')"><slot /></button>`,
            props: ["variant", "icon", "loading", "disabled", "size"],
            emits: ["click"],
        },
        PaymentModal: {
            template: `<div class="payment-modal-stub" />`,
            props: ["aberto", "cobranca", "formasPagamento", "podeDesconto", "carregando"],
            emits: ["fechar", "pago"],
        },
        EstornoModal: {
            template: `<div class="estorno-modal-stub" />`,
            props: ["aberto", "pagamento", "carregando"],
            emits: ["fechar", "confirmar"],
        },
    },
}

// ── Dados de teste ─────────────────────────────────────────────────────────────

const dadosMock: FinanceiroAba = {
    totalCobrado: 500,
    totalPagoLiquido: 200,
    saldo: 300,
    cobrancas: [
        {
            id: 1,
            origem: "Consulta",
            tipoAtendimento: "Particular",
            convenioId: null,
            convenioNome: null,
            valorCobrado: 500,
            desconto: 0,
            totalLiquido: 500,
            totalPagoLiquido: 200,
            saldo: 300,
            status: "ParcialmentePaga",
            descricao: "Consulta retorno",
            guiaNumero: null,
            guiaSenha: null,
            guiaAutorizadaEm: null,
            pagamentos: [
                {
                    id: 10,
                    valor: 200,
                    formaPagamentoNome: "Dinheiro",
                    parcelas: 1,
                    taxa: 0,
                    dataPagamento: "2026-06-01",
                    estornado: false,
                    estorno: null,
                },
            ],
            historicoValor: [],
        },
    ],
}

// ── Testes ────────────────────────────────────────────────────────────────────

describe("FinanceiroTab", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        _podeFn = vi.fn().mockReturnValue(true)
        _ehDono = false
    })

    // CA24: lazy-load — não dispara query quando aba não está ativa
    it("nao consulta quando ativa=false", () => {
        mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: false },
            global: globalConfig,
        })
        expect(cobrancaService.obterFinanceiroAba).not.toHaveBeenCalled()
    })

    // CA24: dispara query ao ativar
    it("consulta ao tornar ativa=true com permissao", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosMock)
        mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        expect(cobrancaService.obterFinanceiroAba).toHaveBeenCalledWith(42)
    })

    // CA25: KPIs exibidos
    it("exibe KPIs corretos apos carregar", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosMock)
        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        expect(w.text()).toContain("Total cobrado")
        expect(w.text()).toContain("Total pago")
        expect(w.text()).toContain("Saldo em aberto")
    })

    // CA26: lista cobranças
    it("renderiza cards de cobranças apos carregar", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosMock)
        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        expect(w.findAll(".charge-card")).toHaveLength(1)
        expect(w.text()).toContain("Consulta retorno")
    })

    // CA23: empty state sem cobranças
    it("exibe empty state quando sem cobranças", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue({
            totalCobrado: 0, totalPagoLiquido: 0, saldo: 0, cobrancas: [],
        })
        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        expect(w.find(".app-empty-state").exists()).toBe(true)
    })

    // CA33/CA34: gate de acesso restrito
    it("exibe acesso restrito quando sem permissao", () => {
        _podeFn = vi.fn().mockReturnValue(false)
        _ehDono = false
        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        expect(w.find(".fin-restricted").exists()).toBe(true)
        expect(cobrancaService.obterFinanceiroAba).not.toHaveBeenCalled()
    })

    // CA27: expand de card
    it("expande detalhe ao clicar no card", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosMock)
        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        expect(w.find(".cc-detail").exists()).toBe(false)
        await w.find(".cc-main").trigger("click")
        expect(w.find(".cc-detail").exists()).toBe(true)
    })

    // CA36: mensagem de erro quando API falha
    it("exibe mensagem de erro quando API falha", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockRejectedValue({
            response: { data: { mensagem: "Erro de servidor" } },
        })
        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        expect(w.find(".msg-erro").exists()).toBe(true)
        expect(w.find(".msg-erro").text()).toContain("Erro de servidor")
    })
})
