import { defineStore } from "pinia"
import { ref, computed } from "vue"
import { pendenciaService, type AcaoPendencia, type PendenciaAberta } from "@/services/pendenciaService"

/**
 * Store global do widget "Próximos passos do atendimento" (addendum 2 — CA202–CA215).
 *
 * Responsabilidades:
 *  - Guardar o contexto da última evolução salva com ações de conduta (pacienteId,
 *    evolucaoId, acoesMarcadas) e o estado de UI do widget (expandido/minimizado).
 *  - Persistir em sessionStorage (chave imedto.proximosPassos) para sobreviver a
 *    reload na mesma aba; some ao fechar aba/browser (R26).
 *  - Expor `iniciar()` (chamado por ProntuarioView ao salvar) e `fechar()` (chamado
 *    pelo widget ao encerrar).
 *  - Re-fetch de pendências abertas (`atualizarAbertas`) para uso no re-fetch por
 *    troca de rota (CA205).
 *  - Detectar conclusão de todas as ações e emitir o estado "tudo concluído" para
 *    o widget sair sozinho (CA204, R27).
 *
 * Limpeza (R30):
 *  - authStore.limparSessao() → chama `limpar()` (chave em STORAGE_KEYS_SESSAO não
 *    é usada pois é sessionStorage; limpar() chama sessionStorage.removeItem diretamente).
 *  - tenantStore.selecionar() com trocouEstab → chama `limpar()` via import dinâmico,
 *    mesmo padrão que a assinaturaStore (R30).
 */

const STORAGE_KEY = "imedto.proximosPassos"

interface EstadoPersistido {
    pacienteId: number
    evolucaoId: number | undefined
    acoesMarcadas: AcaoPendencia[]
    estado: EstadoWidget
}

export type EstadoWidget = "expandido" | "minimizado" | "fechado" | "concluido"

export const useProximosPassosStore = defineStore("proximosPassos", () => {
    // ── Estado ───────────────────────────────────────────────────────────────
    const pacienteId    = ref<number | null>(null)
    const evolucaoId    = ref<number | undefined>(undefined)
    const acoesMarcadas = ref<AcaoPendencia[]>([])
    const estado        = ref<EstadoWidget>("fechado")
    const abertas       = ref<PendenciaAberta[]>([])
    const buscando      = ref(false)

    // ── Computed ─────────────────────────────────────────────────────────────

    const visivel = computed(() => estado.value !== "fechado")

    const total = computed(() => acoesMarcadas.value.length)

    /**
     * Retorna true se uma ação específica NÃO tem pendência aberta na evolucaoId atual.
     * Filtra por evolucaoId para evitar que pendência de outra evolução (mesmo acao)
     * mascare a conclusão da evolução corrente.
     */
    function estaConcluidaAcao(acao: AcaoPendencia): boolean {
        return !abertas.value.some(
            p => p.acao === acao && p.evolucaoId === evolucaoId.value,
        )
    }

    const concluidas = computed(() =>
        acoesMarcadas.value.filter(a => estaConcluidaAcao(a)).length,
    )

    /** True quando há ≥1 pendência ainda aberta na evolucaoId atual (conforme último fetch). */
    const temAberta = computed(() =>
        acoesMarcadas.value.some(a => !estaConcluidaAcao(a)),
    )

    // ── Persistência ─────────────────────────────────────────────────────────

    function _persistir() {
        if (!pacienteId.value) return
        const dados: EstadoPersistido = {
            pacienteId: pacienteId.value,
            evolucaoId: evolucaoId.value,
            acoesMarcadas: acoesMarcadas.value,
            estado: estado.value === "concluido" ? "expandido" : estado.value,
        }
        try {
            sessionStorage.setItem(STORAGE_KEY, JSON.stringify(dados))
        } catch {
            // Modo privado ou quota — silencioso.
        }
    }

    /**
     * Reidrata o estado da sessão a partir do sessionStorage.
     * Chamado no `main.ts` ou no `App.vue` antes de montar a aplicação (R26).
     */
    function reidratar() {
        try {
            const raw = sessionStorage.getItem(STORAGE_KEY)
            if (!raw) return
            const dados = JSON.parse(raw) as EstadoPersistido
            if (!dados.pacienteId || !Array.isArray(dados.acoesMarcadas)) return
            pacienteId.value    = dados.pacienteId
            evolucaoId.value    = dados.evolucaoId
            acoesMarcadas.value = dados.acoesMarcadas
            estado.value        = dados.estado === "minimizado" ? "minimizado" : "expandido"
        } catch {
            // JSON inválido — ignora.
        }
    }

    // ── Ações ────────────────────────────────────────────────────────────────

    /**
     * Inicia o widget com o contexto de uma nova evolução.
     * Substitui qualquer estado anterior (R24: uma evolução por vez).
     */
    async function iniciar(params: {
        pacienteId: number
        evolucaoId?: number
        acoesMarcadas: AcaoPendencia[]
    }) {
        pacienteId.value    = params.pacienteId
        evolucaoId.value    = params.evolucaoId
        acoesMarcadas.value = params.acoesMarcadas
        estado.value        = "expandido"
        _persistir()
        await atualizarAbertas()
    }

    /**
     * Re-busca pendências abertas. Chamado pelo widget a cada troca de rota (CA205).
     * Após o fetch: se todas estiverem concluídas, transiciona para "concluido" (CA204/R27).
     */
    async function atualizarAbertas() {
        if (!pacienteId.value) return
        buscando.value = true
        try {
            abertas.value = await pendenciaService.listarAbertas(pacienteId.value)
            // CA204/R27: detecta conclusão de todas as ações do widget.
            if (total.value > 0 && concluidas.value === total.value) {
                estado.value = "concluido"
                // Limpa após breve transição (~2s) gerida pelo widget.
            }
        } finally {
            buscando.value = false
        }
    }

    function minimizar() {
        if (estado.value === "expandido") {
            estado.value = "minimizado"
            _persistir()
        }
    }

    function expandir() {
        estado.value = "expandido"
        _persistir()
    }

    /** Fecha e limpa tudo (memória + sessionStorage). */
    function fechar() {
        limpar()
    }

    /**
     * Limpa estado em memória + sessionStorage.
     * Chamado em logout (authStore.limparSessao) e troca de estabelecimento
     * (tenantStore.selecionar com trocouEstab) — R30.
     */
    function limpar() {
        pacienteId.value    = null
        evolucaoId.value    = undefined
        acoesMarcadas.value = []
        estado.value        = "fechado"
        abertas.value       = []
        buscando.value      = false
        try {
            sessionStorage.removeItem(STORAGE_KEY)
        } catch {
            // Modo privado — silencioso.
        }
    }

    return {
        // Estado
        pacienteId,
        evolucaoId,
        acoesMarcadas,
        estado,
        abertas,
        buscando,
        // Computed
        visivel,
        total,
        concluidas,
        temAberta,
        // Helpers
        estaConcluidaAcao,
        // Ações
        iniciar,
        reidratar,
        atualizarAbertas,
        minimizar,
        expandir,
        fechar,
        limpar,
    }
})
