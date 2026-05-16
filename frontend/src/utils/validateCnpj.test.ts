import { describe, it, expect } from "vitest"
import { validateCnpj, validateCnpjObrigatorio, formatarCnpj, apenasDigitos } from "./validateCnpj"

describe("validateCnpj", () => {
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

    it("rejeita todos os dígitos iguais", () => {
        expect(validateCnpj("00000000000000")).toBe(false)
        expect(validateCnpj("11111111111111")).toBe(false)
    })

    it("aceita CNPJ válido conhecido", () => {
        // 11.222.333/0001-81 — base de teste comum
        expect(validateCnpj("11222333000181")).toBe(true)
        // Formatado
        expect(validateCnpj("11.222.333/0001-81")).toBe(true)
    })

    it("aceita outro CNPJ válido", () => {
        expect(validateCnpj("11444777000161")).toBe(true)
    })
})

describe("validateCnpjObrigatorio", () => {
    it("rejeita vazio", () => {
        expect(validateCnpjObrigatorio("")).toBe(false)
        expect(validateCnpjObrigatorio(null)).toBe(false)
    })

    it("aceita CNPJ válido", () => {
        expect(validateCnpjObrigatorio("11.222.333/0001-81")).toBe(true)
    })
})

describe("formatarCnpj", () => {
    it("formata 14 dígitos crus", () => {
        expect(formatarCnpj("11222333000181")).toBe("11.222.333/0001-81")
    })

    it("devolve original quando não tem 14 dígitos", () => {
        expect(formatarCnpj("123")).toBe("123")
        expect(formatarCnpj("")).toBe("")
        expect(formatarCnpj(null)).toBe("")
    })
})

describe("apenasDigitos", () => {
    it("remove pontuação", () => {
        expect(apenasDigitos("11.222.333/0001-81")).toBe("11222333000181")
    })

    it("retorna vazio para null/undefined/empty", () => {
        expect(apenasDigitos(null)).toBe("")
        expect(apenasDigitos(undefined)).toBe("")
        expect(apenasDigitos("")).toBe("")
    })
})
