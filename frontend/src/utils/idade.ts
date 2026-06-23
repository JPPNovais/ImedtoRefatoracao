/**
 * Utilitário centralizado de cálculo de idade e faixa etária.
 * Briefing 2026-06-23_002 — CA8: fonte única de verdade para lista, detalhe e agenda.
 *
 * Regras de borda (R1):
 *  - No dia em que completa 18 anos: deixa de ser menor (já é adulto).
 *  - No dia em que completa 60 anos: passa a ser idoso.
 *  - Sem data de nascimento: sem faixa.
 */

export type FaixaEtaria = "idoso" | "menor" | null

/**
 * Parseia uma string de data "YYYY-MM-DD" (ou ISO completo) para um objeto Date
 * tratando os componentes como data local (sem conversão de fuso), evitando
 * o problema clássico de UTC offset que desloca a data em 1 dia.
 */
function parsearDataLocal(iso: string): Date | null {
    // Aceita "YYYY-MM-DD" ou "YYYY-MM-DDTHH:mm:ss..." — usa apenas a parte da data.
    const match = iso.match(/^(\d{4})-(\d{2})-(\d{2})/)
    if (!match) return null
    const d = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
    return isNaN(d.getTime()) ? null : d
}

/**
 * Calcula os anos completos a partir de uma string ISO de data ("YYYY-MM-DD" ou ISO 8601).
 * Retorna null se a data for inválida ou ausente.
 * Usa comparação de componentes (ano/mês/dia) para evitar erros de fuso horário.
 */
export function calcularIdadeAnos(dataNascimento: string | null | undefined): number | null {
    if (!dataNascimento) return null
    const nasc = parsearDataLocal(dataNascimento)
    if (!nasc) return null
    const hoje = new Date()
    let anos = hoje.getFullYear() - nasc.getFullYear()
    const m = hoje.getMonth() - nasc.getMonth()
    if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) anos--
    return anos
}

/**
 * Determina a faixa etária a partir de uma string ISO de data.
 * - null → sem data ou adulto (18-59)
 * - "menor" → < 18 anos completos
 * - "idoso" → >= 60 anos completos
 */
export function calcularFaixaEtaria(dataNascimento: string | null | undefined): FaixaEtaria {
    const anos = calcularIdadeAnos(dataNascimento)
    if (anos === null) return null
    if (anos < 18) return "menor"
    if (anos >= 60) return "idoso"
    return null
}

/**
 * Retorna a idade formatada como string para exibição (ex: "42 anos").
 * Retorna string vazia se data inválida ou ausente.
 */
export function formatarIdade(dataNascimento: string | null | undefined): string {
    const anos = calcularIdadeAnos(dataNascimento)
    if (anos === null) return ""
    return `${anos} anos`
}
