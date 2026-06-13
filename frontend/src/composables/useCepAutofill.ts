import { ref, watch, type Ref } from "vue"
import { useDebouncedRef } from "./useDebouncedRef"
import { buscarPorCep, type EnderecoEncontrado } from "@/services/viaCepService"

export interface CepAutofillOptions {
    /** Callback disparado de forma síncrona ao CEP encolher abaixo de 8 dígitos (R2). */
    onLimpar?: () => void
    /** Delay do debounce em ms — padrão 300ms (R3). */
    delay?: number
}

/**
 * Composable padronizado para busca automática de endereço por CEP (ViaCEP).
 *
 * Comportamento garantido:
 *  - R1: busca só dispara com exatamente 8 dígitos.
 *  - R2: limpeza síncrona imediata quando CEP cai abaixo de 8 dígitos.
 *  - R3: debounce ~300-400ms via useDebouncedRef.
 *  - R4: guard de race condition (reqId) — última requisição vence.
 *  - R6: erro é silencioso (retorna null sem quebrar o formulário).
 *
 * Em telas de edição, chame `marcarCarga(cep)` logo após popular o form
 * programaticamente — isso previne que o watch debounced dispare uma busca
 * no mount sobrescrevendo dados já carregados do registro (CA12, ponto de
 * atenção do briefing 2026-06-13_001).
 *
 * Uso padrão:
 *   const cepRef = toRef(form, "cep")
 *   const { buscando, marcarCarga } = useCepAutofill(cepRef, (e) => {
 *     form.logradouro = e.logradouro || form.logradouro
 *     form.bairro     = e.bairro     || form.bairro
 *     form.cidade     = e.cidade     || form.cidade
 *     form.uf         = e.uf         || form.uf
 *   }, {
 *     onLimpar: () => { form.logradouro = ""; form.bairro = ""; form.cidade = ""; form.uf = "" },
 *   })
 *
 *   // em modo edição, ao popular o form:
 *   form.cep = paciente.cep
 *   marcarCarga(paciente.cep)  // composable ignora o próximo disparo com este valor
 *
 * @param cepRef      Ref reativo do valor do CEP (com ou sem máscara).
 * @param onEndereco  Callback com os dados encontrados; cada tela mapeia para o seu form.
 * @param options     Configurações opcionais: onLimpar, delay.
 */
export function useCepAutofill(
    cepRef: Ref<string | null | undefined>,
    onEndereco: (endereco: EnderecoEncontrado) => void,
    options: CepAutofillOptions = {},
): { buscando: Ref<boolean>; marcarCarga: (cep: string) => void } {
    const { onLimpar, delay = 300 } = options

    const buscando = ref(false)
    let reqId = 0

    // CEP de carga: quando o chamador popula o form programaticamente em modo
    // edição, chama marcarCarga(cep). O próximo disparo do watch debounced com
    // exatamente este valor é silenciosamente ignorado — evita sobrescrever
    // dados já carregados do registro. Após pular uma vez, o registro é limpo
    // e buscas subsequentes (mesmo CEP digitado novamente) funcionam normalmente.
    let cepDeCarga: string | null = null

    function marcarCarga(cep: string) {
        cepDeCarga = (cep ?? "").replace(/\D/g, "")
    }

    // Watch síncrono sobre o ref cru — detecta encolhimento imediatamente (R2).
    watch(cepRef, (valor) => {
        const digitos = (valor ?? "").replace(/\D/g, "")
        if (digitos.length < 8 && onLimpar) {
            cepDeCarga = null  // limpeza pelo usuário reseta o guard de carga
            onLimpar()
        }
    })

    // Ref debounced — só atualiza após `delay` ms sem novas mudanças (R3).
    const cepDebounced = useDebouncedRef(cepRef, delay)

    watch(cepDebounced, async (valor) => {
        const digitos = (valor ?? "").replace(/\D/g, "")
        if (digitos.length !== 8) return  // R1: só 8 dígitos

        // Guard de carga: pula a busca se este CEP foi marcado como "valor de
        // carga programática" — evita disparo automático no mount em modo edição.
        if (cepDeCarga !== null && digitos === cepDeCarga) {
            cepDeCarga = null  // pula somente uma vez; próxima edição busca normalmente
            return
        }

        buscando.value = true
        const id = ++reqId

        let endereco = null
        try {
            endereco = await buscarPorCep(digitos)  // já captura erro de rede (R6)
        } catch {
            // Camada extra de proteção — não propaga (R6)
        }

        if (id !== reqId) return  // R4: descarta resposta obsoleta
        buscando.value = false

        if (endereco) {
            onEndereco(endereco)
        }
    })

    return { buscando, marcarCarga }
}
