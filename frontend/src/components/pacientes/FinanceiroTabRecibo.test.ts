/**
 * Testes F8 — ação "Emitir recibo" na aba Financeiro (CA118/CA120/CA122/CA129).
 * Porta 2: FinanceiroTab.vue.
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import FinanceiroTab from "./FinanceiroTab.vue"
import type { FinanceiroAba } from "@/services/cobrancaService"

// ── Mocks ─────────────────────────────────────────────────────────────────────
// Todos os vi.fn() devem estar dentro do factory para evitar problema de hoisting.

vi.mock("@/services/cobrancaService", () => ({
    cobrancaService: {
        obterFinanceiroAba: vi.fn(),
        listarConfigTaxa: vi.fn().mockResolvedValue([]),
        registrarPagamentos: vi.fn(),
        estornarPagamento: vi.fn(),
        emitirRecibo: vi.fn(),
    },
}))

vi.mock("@/services/categoriaFinanceiraService", () => ({
    formaPagamentoService: {
        listar: vi.fn().mockResolvedValue([]),
    },
}))

vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: () => ({
        get ehDono() { return false },
        pode: (_: string) => true,
    }),
}))

vi.mock("pinia", async (importActual) => {
    const actual = await importActual<typeof import("pinia")>()
    return { ...actual }
})

const { cobrancaService } = await import("@/services/cobrancaService")

const globalConfig = {
    stubs: {
        AppEmptyState: { template: `<div class="app-empty-state" />`, props: ["icone", "titulo", "descricao"] },
        AppButton: { template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`, props: ["variant", "icon", "loading", "disabled", "size"], emits: ["click"] },
        PaymentModal: { template: `<div class="payment-modal-stub" />`, props: ["aberto", "cobranca", "formasPagamento", "podeDesconto", "carregando"], emits: ["fechar", "pago"] },
        EstornoModal: { template: `<div class="estorno-modal-stub" />`, props: ["aberto", "pagamento", "carregando"], emits: ["fechar", "confirmar"] },
    },
}

// ── Helpers de dados ──────────────────────────────────────────────────────────

function dadosComPagamento(pagamentoId = 10, estornado = false): FinanceiroAba {
    return {
        totalCobrado: 200,
        totalPagoLiquido: estornado ? 0 : 200,
        saldo: estornado ? 200 : 0,
        cobrancas: [{
            id: 1,
            origem: "Consulta",
            tipoAtendimento: "Particular",
            valorCobrado: 200,
            desconto: 0,
            totalLiquido: 200,
            totalPagoLiquido: estornado ? 0 : 200,
            saldo: estornado ? 200 : 0,
            status: estornado ? "Aberta" : "Paga",
            descricao: "Consulta retorno",
            pagamentos: [{
                id: pagamentoId,
                valor: 200,
                formaPagamentoNome: "Dinheiro",
                parcelas: 1,
                taxa: 0,
                dataPagamento: "2026-06-01",
                estornado,
                estorno: estornado ? {
                    id: 99, valor: 200, motivo: "teste",
                    estornadoPorNome: "Admin", dataEstorno: "2026-06-02",
                } : null,
            }],
            historicoValor: [],
        }],
    }
}

// ── Testes ────────────────────────────────────────────────────────────────────

describe("FinanceiroTab — Emitir recibo (F8/CA118/CA120/CA122/CA129)", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        vi.mocked(cobrancaService.emitirRecibo).mockResolvedValue(new Blob(["pdf"], { type: "application/pdf" }))

        // Stub URL.createObjectURL / revokeObjectURL para JSDOM
        Object.defineProperty(URL, "createObjectURL", { value: vi.fn().mockReturnValue("blob:test"), writable: true, configurable: true })
        Object.defineProperty(URL, "revokeObjectURL", { value: vi.fn(), writable: true, configurable: true })
    })

    // CA118/CA122: botão "Emitir recibo" presente em pagamento não estornado
    it("exibe botao de recibo para pagamento nao estornado (CA118/CA122)", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosComPagamento(10, false))

        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()

        // Expande o card para ver os detalhes
        await w.find(".cc-main").trigger("click")

        // Botão de recibo deve existir (btn-icon-ver com title "Emitir recibo")
        const btnRecibo = w.find('[title="Emitir recibo"]')
        expect(btnRecibo.exists()).toBe(true)
    })

    // CA120: botão oculto para pagamento estornado
    it("nao exibe botao de recibo para pagamento estornado (CA120)", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosComPagamento(10, true))

        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        await w.find(".cc-main").trigger("click")

        const btnRecibo = w.find('[title="Emitir recibo"]')
        expect(btnRecibo.exists()).toBe(false)
    })

    // CA122: clique chama cobrancaService.emitirRecibo com pagamentoId correto
    it("chama emitirRecibo com pagamentoId correto ao clicar (CA122)", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosComPagamento(55, false))

        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        await w.find(".cc-main").trigger("click")

        const btnRecibo = w.find('[title="Emitir recibo"]')
        await btnRecibo.trigger("click")
        await flushPromises()

        expect(vi.mocked(cobrancaService.emitirRecibo)).toHaveBeenCalledWith(55)
    })

    // CA129: botão fica desabilitado durante o fetch
    it("botao fica desabilitado durante o fetch do blob (CA129)", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosComPagamento(10, false))

        // Promessa pendente para simular loading
        let resolveBlob: (b: Blob) => void
        vi.mocked(cobrancaService.emitirRecibo).mockReturnValue(new Promise<Blob>((res) => { resolveBlob = res }))

        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        await w.find(".cc-main").trigger("click")

        const btnRecibo = w.find('[title="Emitir recibo"]')
        await btnRecibo.trigger("click")
        // Antes de resolver: botão deve estar desabilitado
        await w.vm.$nextTick()
        expect(btnRecibo.attributes("disabled")).toBeDefined()

        // Libera a promise
        resolveBlob!(new Blob(["pdf"], { type: "application/pdf" }))
        await flushPromises()
        // Após resolver: botão volta ao normal
        expect(btnRecibo.attributes("disabled")).toBeUndefined()
    })

    // CA129: erro no fetch exibe toast via emit
    it("emite notificar com mensagem generica quando API falha (CA129)", async () => {
        vi.mocked(cobrancaService.obterFinanceiroAba).mockResolvedValue(dadosComPagamento(10, false))
        vi.mocked(cobrancaService.emitirRecibo).mockRejectedValue({ response: { data: { mensagem: "Pagamento estornado não pode gerar recibo." } } })

        const w = mount(FinanceiroTab, {
            props: { pacienteId: 42, ativa: true },
            global: globalConfig,
        })
        await flushPromises()
        await w.find(".cc-main").trigger("click")

        const btnRecibo = w.find('[title="Emitir recibo"]')
        await btnRecibo.trigger("click")
        await flushPromises()

        const notificacoes = w.emitted("notificar")
        expect(notificacoes).toBeTruthy()
        expect(notificacoes![0][0]).toContain("estornado")
        expect(notificacoes![0][1]).toBe("error")
    })
})
