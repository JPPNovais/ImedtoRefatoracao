/**
 * Utilitarios de CPF — UX e validacao no frontend.
 *
 * Backend e a fonte da verdade (BusinessException 422 em CPF invalido). Estes
 * helpers existem para feedback inline durante o cadastro: validar antes de
 * enviar, mascarar input, comparar duplicidade local.
 */

/** Remove tudo que nao e digito. */
export function somenteDigitos(valor: string | null | undefined): string {
    return (valor ?? "").replace(/\D/g, "")
}

/**
 * Valida um CPF aplicando o algoritmo de digitos verificadores. Aceita string
 * formatada ou so digitos. Sequencias repetidas (000.000.000-00 ... 999.999.999-99)
 * sao explicitamente invalidas.
 */
export function cpfValido(cpf: string | null | undefined): boolean {
    const d = somenteDigitos(cpf)
    if (d.length !== 11) return false
    if (/^(\d)\1{10}$/.test(d)) return false

    const digito = (len: number) => {
        let soma = 0
        const peso = len + 1
        for (let i = 0; i < len; i++) soma += Number(d[i]) * (peso - i)
        const resto = soma % 11
        return resto < 2 ? 0 : 11 - resto
    }
    return digito(9) === Number(d[9]) && digito(10) === Number(d[10])
}

/** Formata "00000000000" -> "000.000.000-00" para exibicao. */
export function formatarCpf(cpf: string | null | undefined): string {
    const d = somenteDigitos(cpf)
    if (d.length !== 11) return cpf ?? ""
    return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}
