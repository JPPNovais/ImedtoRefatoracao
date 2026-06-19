/**
 * Valida CNPJ no front (apenas UX — backend valida e é a fonte da verdade).
 *
 * Espelha {@link Imedto.Backend.Domain.Inventario.Cadastros.CnpjValidator}.
 * Suporta o formato alfanumérico da IN RFB 2.229/2024: as 12 primeiras posições
 * aceitam [A-Z0-9]; as 2 últimas (DVs) são numéricas. Strings vazias/nulas
 * retornam `true` — CNPJ é opcional. Use `validateCnpjObrigatorio` para forçar.
 */

/**
 * Normaliza CNPJ para a forma canônica: preserva [A-Z0-9], aplica uppercase,
 * remove qualquer outro caractere (pontos, barra, hífen, espaço, etc.).
 * DEDICADA ao CNPJ — não altera `apenasDigitos` usada por CPF/CEP.
 */
export function normalizarCnpj(valor: string | null | undefined): string {
    if (!valor) return ""
    return valor.toUpperCase().replace(/[^A-Z0-9]/g, "")
}

/**
 * Mantida por compatibilidade com CPF/CEP/telefone (continua só dígitos).
 * Para CNPJ use `normalizarCnpj`.
 */
export function apenasDigitos(valor: string | null | undefined): string {
    if (!valor) return ""
    return valor.replace(/\D/g, "")
}

export function validateCnpj(cnpj: string | null | undefined): boolean {
    const d = normalizarCnpj(cnpj)
    if (!d) return true                        // opcional → vazio é válido
    if (d.length !== 14) return false
    // As 2 últimas posições (DVs) devem ser dígitos.
    if (!/^\d$/.test(d[12]) || !/^\d$/.test(d[13])) return false
    // Todos iguais (00...000, AA...AAA) — inválido
    if (d.split("").every(c => c === d[0])) return false

    const dv1 = calcularDigito(d, 12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])
    const dv2 = calcularDigito(d, 13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])

    return (d.charCodeAt(12) - 48) === dv1 && (d.charCodeAt(13) - 48) === dv2
}

export function validateCnpjObrigatorio(cnpj: string | null | undefined): boolean {
    const d = normalizarCnpj(cnpj)
    if (!d) return false
    return validateCnpj(d)
}

/**
 * Calcula um dígito verificador do CNPJ.
 * Valor de cada caractere = (charCode - 48): '0'-'9' → 0-9; 'A'-'Z' → 17-42.
 * Espelha CnpjValidator.CalcularDigito em C#.
 */
function calcularDigito(d: string, len: number, pesos: number[]): number {
    let soma = 0
    for (let i = 0; i < len; i++) soma += (d.charCodeAt(i) - 48) * pesos[i]
    const resto = soma % 11
    return resto < 2 ? 0 : 11 - resto
}

/**
 * Formata 14 caracteres canônicos → "XX.XXX.XXX/XXXX-XX".
 * Preserva letras (CNPJ alfanumérico). Se inválido, devolve original.
 */
export function formatarCnpj(cnpj: string | null | undefined): string {
    const d = normalizarCnpj(cnpj)
    if (d.length !== 14) return cnpj ?? ""
    return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12, 14)}`
}
