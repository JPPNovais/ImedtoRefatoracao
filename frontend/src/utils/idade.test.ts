import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"
import { calcularIdadeAnos, calcularFaixaEtaria, formatarIdade } from "./idade"

/**
 * Testes do util centralizado de faixa etária (briefing 2026-06-23_002).
 * CA4 (sem data), CA5 (borda 18), CA6 (borda 60), CA8 (centralização).
 */

/**
 * Gera uma string "YYYY-MM-DD" usando componentes de data locais (sem UTC shift).
 * deltaAnos: subtrai N anos de hoje. deltaDias: subtrai N dias (negativo = adiciona).
 */
function isoHoje(deltaAnos: number, deltaDias = 0): string {
    const d = new Date()
    d.setFullYear(d.getFullYear() - deltaAnos)
    d.setDate(d.getDate() - deltaDias)
    const ano = d.getFullYear()
    const mes = String(d.getMonth() + 1).padStart(2, "0")
    const dia = String(d.getDate()).padStart(2, "0")
    return `${ano}-${mes}-${dia}`
}

describe("calcularIdadeAnos", () => {
    it("retorna null para data ausente", () => {
        expect(calcularIdadeAnos(null)).toBeNull()
        expect(calcularIdadeAnos(undefined)).toBeNull()
        expect(calcularIdadeAnos("")).toBeNull()
    })

    it("retorna null para data inválida", () => {
        expect(calcularIdadeAnos("nao-e-data")).toBeNull()
    })

    it("retorna 30 para quem nasceu há exatamente 30 anos", () => {
        expect(calcularIdadeAnos(isoHoje(30))).toBe(30)
    })

    it("retorna 17 para quem ainda não completou 18", () => {
        // nasceu exatamente 17 anos e 0 dias atrás
        expect(calcularIdadeAnos(isoHoje(17))).toBe(17)
    })
})

describe("calcularFaixaEtaria", () => {
    // CA4 — sem data de nascimento
    it("retorna null para data ausente", () => {
        expect(calcularFaixaEtaria(null)).toBeNull()
        expect(calcularFaixaEtaria(undefined)).toBeNull()
    })

    // CA3 — adulto (18-59)
    it("retorna null para adulto de 30 anos", () => {
        expect(calcularFaixaEtaria(isoHoje(30))).toBeNull()
    })

    it("retorna null para adulto de 59 anos", () => {
        expect(calcularFaixaEtaria(isoHoje(59))).toBeNull()
    })

    // CA2 — menor de idade
    it("retorna 'menor' para 10 anos", () => {
        expect(calcularFaixaEtaria(isoHoje(10))).toBe("menor")
    })

    it("retorna 'menor' para 1 ano", () => {
        expect(calcularFaixaEtaria(isoHoje(1))).toBe("menor")
    })

    // CA1 — idoso
    it("retorna 'idoso' para 70 anos", () => {
        expect(calcularFaixaEtaria(isoHoje(70))).toBe("idoso")
    })

    it("retorna 'idoso' para 60 anos completos hoje (CA6)", () => {
        // Completa 60 hoje: já é idoso
        expect(calcularFaixaEtaria(isoHoje(60))).toBe("idoso")
    })

    it("retorna null para quem completa 60 anos amanhã (CA6)", () => {
        // Falta 1 dia para completar 60 → ainda não é idoso (59 anos completos)
        expect(calcularFaixaEtaria(isoHoje(60, -1))).toBeNull()
    })

    // CA5 — borda dos 18 anos
    it("retorna null para quem completa 18 anos hoje (CA5)", () => {
        // No dia em que completa 18: deixa de ser menor
        expect(calcularFaixaEtaria(isoHoje(18))).toBeNull()
    })

    it("retorna 'menor' para quem completa 18 anos amanhã (CA5)", () => {
        // Falta 1 dia para 18: ainda menor
        expect(calcularFaixaEtaria(isoHoje(18, -1))).toBe("menor")
    })

    it("retorna 'menor' para quem completa 18 anos daqui 1 dia", () => {
        // nasceu há 17 anos e 364 dias → amanhã completa 18 → hoje ainda é menor
        // Usa isoHoje(18, -1): data local sem UTC shift = mesmo resultado
        expect(calcularFaixaEtaria(isoHoje(18, -1))).toBe("menor")
    })
})

describe("formatarIdade", () => {
    it("retorna string vazia para data ausente", () => {
        expect(formatarIdade(null)).toBe("")
    })

    it("retorna '30 anos' para 30 anos", () => {
        expect(formatarIdade(isoHoje(30))).toBe("30 anos")
    })
})
