import type { Evolucao } from "@/services/prontuarioService"

/**
 * Helpers compartilhados pelos cards de timeline de evolução
 * (ConsultasAnterioresTab no prontuário e aba Prontuário do detalhe do paciente).
 *
 * Centraliza a lógica de:
 *  - extrair um resumo textual curto da primeira seção preenchida;
 *  - contar quantas seções do modelo foram efetivamente preenchidas.
 */
export function resumoTextual(e: Evolucao): string {
    for (const s of e.modeloSnapshot) {
        const v = e.conteudo[s.chave]
        if (typeof v === "string" && v.trim()) {
            const t = v.trim().replace(/\s+/g, " ")
            return t.length > 220 ? t.slice(0, 220) + "..." : t
        }
    }
    return "Sem resumo textual disponível."
}

export function contarSecoesPreenchidas(e: Evolucao): { preenchidas: number, total: number } {
    const preenchidas = e.modeloSnapshot.filter(s => {
        const v = e.conteudo[s.chave]
        if (v === null || v === undefined) return false
        if (typeof v === "string") return v.trim().length > 0
        if (Array.isArray(v)) return (v as unknown[]).length > 0
        if (typeof v === "object") return Object.values(v as Record<string, unknown>)
            .some(x => x !== null && x !== undefined && String(x).trim() !== "")
        return true
    }).length

    return { preenchidas, total: e.modeloSnapshot.length }
}
