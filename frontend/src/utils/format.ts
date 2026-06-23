export function formatarMoedaBrl(valor: number): string {
    return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

/**
 * Formata um telefone BR para exibição: "(##) #####-####" (celular, 11 díg.) ou
 * "(##) ####-####" (fixo, 10 díg.). Aceita string com ou sem máscara. Devolve
 * o valor original quando não bate 10/11 dígitos, e null quando vazio.
 */
export function formatarTelefone(tel: string | null | undefined): string | null {
    if (!tel) return null
    const d = tel.replace(/\D/g, "")
    if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
    if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`
    return tel
}
