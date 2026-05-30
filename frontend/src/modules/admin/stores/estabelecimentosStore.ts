import { defineStore } from "pinia"
import { ref } from "vue"
import {
    estabelecimentosService,
    type EstabelecimentoAdminDetalheDto,
    type EstabelecimentoAdminListaItemDto,
    type ListarEstabelecimentosParams,
} from "../services/estabelecimentosService"

/**
 * Store Pinia para a seção de Estabelecimentos da área admin.
 *
 * Isolamento: sem imports de stores ou services do app principal.
 * Responsabilidade: estado de lista + detalhe + operações de reveal CPF e reset.
 */
export const useEstabelecimentosStore = defineStore("adminEstabelecimentos", () => {
    // ── Lista ─────────────────────────────────────────────────────────────────

    const itens = ref<EstabelecimentoAdminListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanhoPagina = ref(25)
    const carregandoLista = ref(false)
    const erroLista = ref<string | null>(null)

    async function carregarLista(params: ListarEstabelecimentosParams = {}) {
        carregandoLista.value = true
        erroLista.value = null
        try {
            const resultado = await estabelecimentosService.listar({
                ...params,
                page: pagina.value,
                size: tamanhoPagina.value,
            })
            itens.value = resultado.itens
            total.value = resultado.total
        } catch {
            erroLista.value = "Não foi possível carregar a lista de estabelecimentos."
            itens.value = []
            total.value = 0
        } finally {
            carregandoLista.value = false
        }
    }

    // ── Detalhe ───────────────────────────────────────────────────────────────

    const detalhe = ref<EstabelecimentoAdminDetalheDto | null>(null)
    const carregandoDetalhe = ref(false)
    const erroDetalhe = ref<string | null>(null)

    async function carregarDetalhe(id: number) {
        carregandoDetalhe.value = true
        erroDetalhe.value = null
        detalhe.value = null
        try {
            detalhe.value = await estabelecimentosService.obter(id)
        } catch {
            erroDetalhe.value = "Estabelecimento não encontrado."
        } finally {
            carregandoDetalhe.value = false
        }
    }

    // ── Reveal CPF ────────────────────────────────────────────────────────────

    const cpfRevelado = ref<string | null>(null)
    const revelandoCpf = ref(false)
    const erroRevelarCpf = ref<string | null>(null)

    async function revelarCpf(id: number, motivo: string): Promise<boolean> {
        revelandoCpf.value = true
        erroRevelarCpf.value = null
        try {
            const resultado = await estabelecimentosService.revelarCpfDono(id, motivo)
            cpfRevelado.value = resultado.cpf
            return true
        } catch (err: unknown) {
            const msg = extrairMensagem(err)
            erroRevelarCpf.value = msg ?? "Não foi possível revelar o CPF."
            return false
        } finally {
            revelandoCpf.value = false
        }
    }

    function limparCpfRevelado() {
        cpfRevelado.value = null
        erroRevelarCpf.value = null
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    const resetando = ref(false)
    const erroReset = ref<string | null>(null)

    async function resetTenant(id: number, motivo: string, confirmarNomeFantasia: string): Promise<boolean> {
        resetando.value = true
        erroReset.value = null
        try {
            await estabelecimentosService.resetTenant(id, motivo, confirmarNomeFantasia)
            return true
        } catch (err: unknown) {
            const msg = extrairMensagem(err)
            erroReset.value = msg ?? "Não foi possível executar o reset."
            return false
        } finally {
            resetando.value = false
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    function extrairMensagem(err: unknown): string | null {
        if (err && typeof err === "object") {
            const ax = err as { response?: { data?: { mensagem?: string; title?: string } } }
            return ax.response?.data?.mensagem ?? ax.response?.data?.title ?? null
        }
        return null
    }

    return {
        // lista
        itens,
        total,
        pagina,
        tamanhoPagina,
        carregandoLista,
        erroLista,
        carregarLista,
        // detalhe
        detalhe,
        carregandoDetalhe,
        erroDetalhe,
        carregarDetalhe,
        // reveal cpf
        cpfRevelado,
        revelandoCpf,
        erroRevelarCpf,
        revelarCpf,
        limparCpfRevelado,
        // reset
        resetando,
        erroReset,
        resetTenant,
    }
})
