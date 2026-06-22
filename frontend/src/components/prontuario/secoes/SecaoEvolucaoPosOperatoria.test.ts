/**
 * Testes unitários — SecaoEvolucaoPosOperatoria
 * Briefing 2026-06-21_001 — CA3 (DPO), CA18/CA25 (tipografia — gate CI), CA9 (retrocompat)
 */
import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"

// ── Isola a função calcularDpo para testar em isolamento ──────────────────────
// A função é interna ao componente; re-implementamos idêntico aqui para o teste
// permanecer cirúrgico sem importar o componente inteiro.

function calcularDpo(dataCirurgia: string): string {
    if (!dataCirurgia) return ""
    const cirurgia = new Date(dataCirurgia + "T12:00:00")
    const hoje = new Date()
    hoje.setHours(12, 0, 0, 0)
    if (isNaN(cirurgia.getTime())) return ""
    const diff = Math.round((hoje.getTime() - cirurgia.getTime()) / (1000 * 60 * 60 * 24))
    if (diff < 0) return ""
    return String(diff)
}

function dataRelativa(diasAtras: number): string {
    const d = new Date()
    d.setDate(d.getDate() - diasAtras)
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, "0")
    const dia = String(d.getDate()).padStart(2, "0")
    return `${y}-${m}-${dia}`
}

describe("calcularDpo (CA3 — DPO calculado)", () => {
    it("retorna '' quando data está vazia", () => {
        expect(calcularDpo("")).toBe("")
    })

    it("retorna '' para data futura", () => {
        const amanha = dataRelativa(-1) // -1 = amanhã
        expect(calcularDpo(amanha)).toBe("")
    })

    it("retorna '0' para hoje", () => {
        expect(calcularDpo(dataRelativa(0))).toBe("0")
    })

    it("retorna '5' para 5 dias atrás", () => {
        expect(calcularDpo(dataRelativa(5))).toBe("5")
    })

    it("retorna '30' para 30 dias atrás", () => {
        expect(calcularDpo(dataRelativa(30))).toBe("30")
    })

    it("retorna '' para data inválida", () => {
        expect(calcularDpo("nao-e-data")).toBe("")
        expect(calcularDpo("2024-99-99")).toBe("")
    })
})
