import { describe, it, expect } from "vitest"
import { validateCnpj, validateCnpjObrigatorio, formatarCnpj, apenasDigitos, normalizarCnpj } from "./validateCnpj"

describe("validateCnpj — retrocompatibilidade numérica (CA1, CA7)", () => {
    it("aceita vazio (CNPJ é opcional)", () => {
        expect(validateCnpj("")).toBe(true)
        expect(validateCnpj(null)).toBe(true)
        expect(validateCnpj(undefined)).toBe(true)
    })

    it("rejeita 14 dígitos com DV errado", () => {
        expect(validateCnpj("12345678000100")).toBe(false)
    })

    it("rejeita comprimento diferente de 14", () => {
        expect(validateCnpj("123")).toBe(false)
        expect(validateCnpj("123456780001990")).toBe(false)
    })

    it("rejeita todos os caracteres iguais", () => {
        expect(validateCnpj("00000000000000")).toBe(false)
        expect(validateCnpj("11111111111111")).toBe(false)
    })

    it("aceita CNPJ numérico válido — 11.222.333/0001-81 (CA1)", () => {
        expect(validateCnpj("11222333000181")).toBe(true)
        expect(validateCnpj("11.222.333/0001-81")).toBe(true)
    })

    it("aceita outro CNPJ numérico válido", () => {
        expect(validateCnpj("11444777000161")).toBe(true)
    })
})

describe("validateCnpj — alfanumérico (CA2, CA3, CA12)", () => {
    // Vetor canônico validado à mão: base 12ABC34501DE, DV 35
    it("aceita CNPJ alfanumérico válido 12.ABC.345/01DE-35 (CA2)", () => {
        expect(validateCnpj("12ABC34501DE35")).toBe(true)
        expect(validateCnpj("12.ABC.345/01DE-35")).toBe(true)
        // Minúscula é normalizada para upper
        expect(validateCnpj("12.abc.345/01de-35")).toBe(true)
    })

    it("rejeita CNPJ alfanumérico com DV errado 12.ABC.345/01DE-34 (CA3)", () => {
        expect(validateCnpj("12ABC34501DE34")).toBe(false)
        expect(validateCnpj("12.ABC.345/01DE-34")).toBe(false)
    })

    it("rejeita CNPJ com letra nas posições de DV (CA6)", () => {
        // DV com letra — inválido
        expect(validateCnpj("12ABC34501DE3A")).toBe(false)
    })

    it("paridade back+front: mesmo veredicto para todos os vetores canônicos (CA12)", () => {
        // Válidos
        expect(validateCnpj("11222333000181")).toBe(true)   // numérico
        expect(validateCnpj("12ABC34501DE35")).toBe(true)   // alfanumérico válido
        // Inválidos
        expect(validateCnpj("12ABC34501DE34")).toBe(false)  // DV errado
        expect(validateCnpj("12345678000100")).toBe(false)  // numérico DV errado
    })
})

describe("validateCnpjObrigatorio", () => {
    it("rejeita vazio", () => {
        expect(validateCnpjObrigatorio("")).toBe(false)
        expect(validateCnpjObrigatorio(null)).toBe(false)
    })

    it("aceita CNPJ numérico válido", () => {
        expect(validateCnpjObrigatorio("11.222.333/0001-81")).toBe(true)
    })

    it("aceita CNPJ alfanumérico válido", () => {
        expect(validateCnpjObrigatorio("12.ABC.345/01DE-35")).toBe(true)
    })
})

describe("formatarCnpj (CA8)", () => {
    it("formata 14 dígitos crus — numérico", () => {
        expect(formatarCnpj("11222333000181")).toBe("11.222.333/0001-81")
    })

    it("formata 14 chars alfanuméricos — preserva letras (CA8)", () => {
        expect(formatarCnpj("12ABC34501DE35")).toBe("12.ABC.345/01DE-35")
    })

    it("normaliza para upper ao formatar", () => {
        expect(formatarCnpj("12abc34501de35")).toBe("12.ABC.345/01DE-35")
    })

    it("devolve original quando não tem 14 chars canônicos", () => {
        expect(formatarCnpj("123")).toBe("123")
        expect(formatarCnpj("")).toBe("")
        expect(formatarCnpj(null)).toBe("")
    })
})

describe("normalizarCnpj (CA4, CA7)", () => {
    it("remove máscara e aplica uppercase", () => {
        expect(normalizarCnpj("12.abc.345/01de-35")).toBe("12ABC34501DE35")
    })

    it("remove apenas pontuação, preserva letras e dígitos", () => {
        expect(normalizarCnpj("12.ABC.345/01DE-35")).toBe("12ABC34501DE35")
    })

    it("retorna vazio para null/undefined/empty", () => {
        expect(normalizarCnpj(null)).toBe("")
        expect(normalizarCnpj(undefined)).toBe("")
        expect(normalizarCnpj("")).toBe("")
    })
})

describe("apenasDigitos — continua só dígitos (CA7)", () => {
    it("remove pontuação e letras", () => {
        expect(apenasDigitos("11.222.333/0001-81")).toBe("11222333000181")
        // Letras são removidas — mantém apenas dígitos (CPF/CEP/telefone)
        expect(apenasDigitos("AB.123")).toBe("123")
    })

    it("retorna vazio para null/undefined/empty", () => {
        expect(apenasDigitos(null)).toBe("")
        expect(apenasDigitos(undefined)).toBe("")
        expect(apenasDigitos("")).toBe("")
    })
})
