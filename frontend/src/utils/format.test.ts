import { describe, it, expect } from "vitest"
import { formatarMoedaBrl, formatarTelefone } from "./format"

describe("formatarMoedaBrl", () => {
    it("formata valor inteiro com R$", () => {
        expect(formatarMoedaBrl(1000)).toMatch(/R\$/)
    })

    it("formata com duas casas decimais", () => {
        const formatado = formatarMoedaBrl(1234.5)
        expect(formatado).toContain("1.234,50")
    })

    it("formata zero", () => {
        expect(formatarMoedaBrl(0)).toContain("0,00")
    })

    it("formata valores negativos", () => {
        const formatado = formatarMoedaBrl(-50)
        expect(formatado).toContain("50,00")
        expect(formatado).toContain("-")
    })

    it("arredonda valores com mais de 2 casas decimais", () => {
        const formatado = formatarMoedaBrl(1.235)
        // Round-half-to-even (banker's rounding) ou meio-pra-cima — ambos sao validos
        expect(formatado).toMatch(/1,2[34]/)
    })
})

describe("formatarTelefone", () => {
    it("formata celular (11 dígitos)", () => {
        expect(formatarTelefone("31999995555")).toBe("(31) 99999-5555")
    })

    it("formata fixo (10 dígitos)", () => {
        expect(formatarTelefone("3133334444")).toBe("(31) 3333-4444")
    })

    it("aceita valor já mascarado", () => {
        expect(formatarTelefone("(31) 99999-5555")).toBe("(31) 99999-5555")
    })

    it("devolve o original quando não tem 10/11 dígitos", () => {
        expect(formatarTelefone("123")).toBe("123")
    })

    it("devolve null para vazio/nulo", () => {
        expect(formatarTelefone("")).toBeNull()
        expect(formatarTelefone(null)).toBeNull()
        expect(formatarTelefone(undefined)).toBeNull()
    })
})
