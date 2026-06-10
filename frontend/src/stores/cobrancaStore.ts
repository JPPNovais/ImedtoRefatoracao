import { ref } from "vue"
import { defineStore } from "pinia"
import {
    cobrancaService,
    type CobrancaDetalhe,
    type RegistrarPagamentosRequest,
    type TabelaPrecoConsulta,
    type ConfigTaxaFormaPagamento,
} from "@/services/cobrancaService"

/**
 * Store Pinia para cobranças do paciente (F1).
 * - cobrancaAberta: detalhes da cobrança em exibição no PaymentModal.
 * - carregando/erro: controle de estado para a UI.
 */
export const useCobrancaStore = defineStore("cobranca", () => {
    const cobrancaAberta = ref<CobrancaDetalhe | null>(null)
    const carregando = ref(false)
    const erro = ref<string | null>(null)

    async function carregarPorAgendamento(agendamentoId: number): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            cobrancaAberta.value = await cobrancaService.obterPorAgendamento(agendamentoId)
        } catch {
            erro.value = "Não foi possível carregar a cobrança."
        } finally {
            carregando.value = false
        }
    }

    async function registrarPagamentos(cobrancaId: number, req: RegistrarPagamentosRequest): Promise<void> {
        await cobrancaService.registrarPagamentos(cobrancaId, req)
        // Recarrega para refletir novo status + pagamentos na tela
        if (cobrancaAberta.value) {
            await carregarPorAgendamento(cobrancaAberta.value.agendamentoId ?? 0)
        }
    }

    function limpar(): void {
        cobrancaAberta.value = null
        erro.value = null
    }

    return {
        cobrancaAberta,
        carregando,
        erro,
        carregarPorAgendamento,
        registrarPagamentos,
        limpar,
    }
})

// ── Store de configuração de cobranças (para a tela de configurações) ─────────

export const useCobrancaConfigStore = defineStore("cobranca-config", () => {
    const tabelaPreco = ref<TabelaPrecoConsulta[]>([])
    const configTaxa = ref<ConfigTaxaFormaPagamento[]>([])
    const carregando = ref(false)

    async function carregarTabelaPreco(busca?: string): Promise<void> {
        carregando.value = true
        try {
            tabelaPreco.value = await cobrancaService.listarTabelaPreco(busca)
        } finally {
            carregando.value = false
        }
    }

    async function carregarConfigTaxa(): Promise<void> {
        carregando.value = true
        try {
            configTaxa.value = await cobrancaService.listarConfigTaxa()
        } finally {
            carregando.value = false
        }
    }

    async function salvarTabelaPreco(item: {
        id?: number
        profissionalId?: string | null
        valorSugerido: number
    }): Promise<void> {
        await cobrancaService.salvarTabelaPreco(item)
        await carregarTabelaPreco()
    }

    async function inativarTabelaPreco(id: number): Promise<void> {
        await cobrancaService.inativarTabelaPreco(id)
        await carregarTabelaPreco()
    }

    async function salvarConfigTaxa(item: {
        id?: number | null
        formaPagamentoId: number
        taxaPercentual: number
        ativo: boolean
    }): Promise<void> {
        await cobrancaService.salvarConfigTaxa(item)
        await carregarConfigTaxa()
    }

    return {
        tabelaPreco,
        configTaxa,
        carregando,
        carregarTabelaPreco,
        carregarConfigTaxa,
        salvarTabelaPreco,
        inativarTabelaPreco,
        salvarConfigTaxa,
    }
})
