/**
 * Helpers de formatação de data/hora SEMPRE em horário de Brasília.
 *
 * Por que existe: `Date.toLocaleString("pt-BR")` sem `timeZone` usa o fuso do SO do
 * navegador. Se o usuário viaja, está em VPN com fuso diferente ou o relógio do SO
 * está errado, a hora exibida mistura. Aqui forçamos `America/Sao_Paulo` em todo
 * formato — backend, banco e front falam o mesmo fuso.
 *
 * Aceita string ISO (vinda da API) ou Date.
 */

const TZ = "America/Sao_Paulo"
const LOCALE = "pt-BR"

function toDate(value: string | Date): Date {
    return value instanceof Date ? value : new Date(value)
}

/** "09/05/2026 às 14:30" */
export function formatDataHora(value: string | Date, opts: Intl.DateTimeFormatOptions = {}): string {
    return toDate(value).toLocaleString(LOCALE, {
        timeZone: TZ,
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        ...opts,
    })
}

/** "09/05/2026" */
export function formatData(value: string | Date, opts: Intl.DateTimeFormatOptions = {}): string {
    return toDate(value).toLocaleDateString(LOCALE, {
        timeZone: TZ,
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        ...opts,
    })
}

/** "14:30" */
export function formatHora(value: string | Date, opts: Intl.DateTimeFormatOptions = {}): string {
    return toDate(value).toLocaleTimeString(LOCALE, {
        timeZone: TZ,
        hour: "2-digit",
        minute: "2-digit",
        ...opts,
    })
}

/** "YYYY-MM-DD" do dia atual em Brasília (independente do fuso do navegador). */
export function hojeISO(): string {
    return dataISO(new Date())
}

/** "YYYY-MM-DD" da data informada em Brasília. Use em vez de getFullYear/getMonth/getDate. */
export function dataISO(value: string | Date): string {
    // pt-CA gera "YYYY-MM-DD" — atalho confiável pra ISO date no fuso desejado.
    return toDate(value).toLocaleDateString("en-CA", { timeZone: TZ })
}
