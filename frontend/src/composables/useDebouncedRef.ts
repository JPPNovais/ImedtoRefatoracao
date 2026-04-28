import { onScopeDispose, ref, watch, type Ref } from "vue"

/**
 * Devolve um Ref que reflete `source` com atraso (debounce). Use em qualquer
 * input cujo valor dispare requisições à API: a UI reage instantaneamente
 * (`v-model` continua escrevendo direto no `source`), mas o ref retornado
 * só atualiza após `delay` ms sem novas mudanças — evitando uma chamada
 * por caractere digitado.
 *
 * Padrão de uso (ver CLAUDE.md, seção "Buscas que tocam a API"):
 *
 *   const buscaInput = ref("")                       // <-- v-model do <input>
 *   const busca      = useDebouncedRef(buscaInput)   // <-- watcher dispara o reload
 *
 *   watch(busca, () => { pagina.value = 1 })
 *   watch([busca, pagina, tamanho], () => carregar(), { immediate: true })
 *
 * O timer é limpo automaticamente quando o componente é desmontado
 * (`onScopeDispose`) — não precisa de cleanup manual.
 *
 * @param source ref que recebe o valor "cru" (geralmente o `v-model` do input)
 * @param delay  ms a aguardar após a última mudança (padrão: 300)
 */
export function useDebouncedRef<T>(source: Ref<T>, delay = 300): Readonly<Ref<T>> {
    const debounced = ref(source.value) as Ref<T>
    let timer: ReturnType<typeof setTimeout> | null = null

    watch(source, (valor) => {
        if (timer) clearTimeout(timer)
        timer = setTimeout(() => { debounced.value = valor }, delay)
    })

    onScopeDispose(() => {
        if (timer) clearTimeout(timer)
    })

    return debounced
}
