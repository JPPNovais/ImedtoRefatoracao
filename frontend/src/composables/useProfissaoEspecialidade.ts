import { computed, ref, watch } from "vue"
import { catalogoService, type EspecialidadeCatalogo, type ProfissaoCatalogo } from "@/services/catalogoService"

export interface UseProfissaoEspecialidadeOptions {
    /** Id inicial da profissão (para pré-seleção ao abrir o modal). */
    profissaoIdInicial?: number | null
    /** Especialidade inicial (para pré-seleção ao abrir o modal). */
    especialidadeInicial?: string | null
}

/**
 * Composable compartilhado para a mecânica profissão → especialidade estrita por catálogo.
 * Usado em ConvidarProfissionalModal e ProfissionalDetalhesModal — fonte única, sem duplicação.
 *
 * Comportamento:
 * - Carrega profissões na montagem (com cache de sessão via catalogoService).
 * - Quando profissaoId muda: limpa especialidade + recarrega especialidades do catálogo da nova profissão.
 * - profissaoTemEspecialidades: false quando profissão sem itens no catálogo → dropdown de especialidade não renderiza.
 * - carregandoEspecialidades: true enquanto a request está em voo → dropdown desabilitado com "Carregando...".
 */
export function useProfissaoEspecialidade(opts?: UseProfissaoEspecialidadeOptions) {
    const profissoes = ref<ProfissaoCatalogo[]>([])
    const especialidades = ref<EspecialidadeCatalogo[]>([])
    const carregandoProfissoes = ref(false)
    const carregandoEspecialidades = ref(false)

    const profissaoId = ref<number | null>(opts?.profissaoIdInicial ?? null)
    const especialidade = ref<string>(opts?.especialidadeInicial ?? "")

    async function carregarProfissoes() {
        carregandoProfissoes.value = true
        try {
            profissoes.value = await catalogoService.listarProfissoes()
        } catch {
            // falha silenciosa — campo fica vazio mas não bloqueia o fluxo
        } finally {
            carregandoProfissoes.value = false
        }
    }

    watch(profissaoId, async (id, idAnterior) => {
        // Só limpa a especialidade quando a profissão EFETIVAMENTE muda (não na carga inicial).
        if (idAnterior !== undefined) {
            especialidade.value = ""
        }
        especialidades.value = []
        if (!id) return
        carregandoEspecialidades.value = true
        try {
            especialidades.value = await catalogoService.listarEspecialidades(id)
        } catch {
            // falha silenciosa
        } finally {
            carregandoEspecialidades.value = false
        }
    })

    /**
     * Reseta o estado para valores iniciais — chamar ao abrir o modal com novos dados.
     * Não limpa profissão/especialidade inicial: isso é responsabilidade do chamador
     * via setProfissaoId/setEspecialidade ou atribuição direta ao ref.
     */
    function reset() {
        profissaoId.value = null
        especialidade.value = ""
        especialidades.value = []
    }

    /**
     * Inicializa com dados vindos de um vínculo existente (pré-seleção no modal de detalhes).
     * Carrega especialidades da profissão sem limpar a especialidade pré-selecionada.
     */
    async function inicializarComVinculo(pId: number | null, esp: string | null) {
        // Atribui profissão primeiro — o watch vai disparar mas idAnterior é undefined,
        // então NÃO limpa a especialidade que vamos setar a seguir.
        profissaoId.value = pId
        // Se houver profissão, espera as especialidades carregarem antes de setar a especialidade.
        if (pId) {
            carregandoEspecialidades.value = true
            try {
                especialidades.value = await catalogoService.listarEspecialidades(pId)
            } catch {
                // falha silenciosa
            } finally {
                carregandoEspecialidades.value = false
            }
        }
        especialidade.value = esp ?? ""
    }

    const profissaoTemEspecialidades = computed(
        () => profissaoId.value !== null && (carregandoEspecialidades.value || especialidades.value.length > 0),
    )

    const profissaoSelecionada = computed(
        () => profissoes.value.find(p => p.id === profissaoId.value) ?? null,
    )

    /** Sigla do conselho derivada da profissão selecionada (ex.: "CRM", "CRO"). */
    const conselhoSigla = computed(() => profissaoSelecionada.value?.conselhoSigla ?? null)

    return {
        profissoes,
        especialidades,
        profissaoId,
        especialidade,
        carregandoProfissoes,
        carregandoEspecialidades,
        profissaoTemEspecialidades,
        profissaoSelecionada,
        conselhoSigla,
        carregarProfissoes,
        reset,
        inicializarComVinculo,
    }
}
