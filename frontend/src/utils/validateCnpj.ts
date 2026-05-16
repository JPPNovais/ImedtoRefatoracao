/**
 * Valida CNPJ no front (apenas UX — backend valida e é a fonte da verdade).
 *
 * Espelha {@link Imedto.Backend.Domain.Inventario.Cadastros.CnpjValidator}.
 * Remove qualquer formatação ao validar; retorna `true` se o número tem 14
 * dígitos e os dois dígitos verificadores conferem. Strings vazias ou nulas
 * retornam `true` — CNPJ é opcional. Use `validateCnpjObrigatorio` para
 * forçar preenchimento.
 */
export function apenasDigitos(valor: string | null | undefined): string {
    if (!valor) return ""
    return valor.replace(/\D/g, "")
}

export function validateCnpj(cnpj: string | null | undefined): boolean {
    const digits = apenasDigitos(cnpj)
    if (!digits) return true                  // opcional → vazio é válido
    if (digits.length !== 14) return false
    // Todos iguais (00...000, 11...111) — inválido
    if (/^(\d)\1{13}$/.test(digits)) return false

    const dv1 = calcularDigito(digits, 12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])
    const dv2 = calcularDigito(digits, 13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])

    return Number(digits[12]) === dv1 && Number(digits[13]) === dv2
}

export function validateCnpjObrigatorio(cnpj: string | null | undefined): boolean {
    const digits = apenasDigitos(cnpj)
    if (!digits) return false
    return validateCnpj(digits)
}

function calcularDigito(digits: string, len: number, pesos: number[]): number {
    let soma = 0
    for (let i = 0; i < len; i++) soma += Number(digits[i]) * pesos[i]
    const resto = soma % 11
    return resto < 2 ? 0 : 11 - resto
}

/** Formata 14 dígitos → "12.345.678/0001-90". Se inválido, devolve original. */
export function formatarCnpj(cnpj: string | null | undefined): string {
    const d = apenasDigitos(cnpj)
    if (d.length !== 14) return cnpj ?? ""
    return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12, 14)}`
}
