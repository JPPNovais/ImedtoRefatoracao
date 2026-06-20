import { describe, it, expect } from "vitest"
import { useMascaraMoeda } from "@/composables/useMascaraMoeda"

describe("useMascaraMoeda", () => {
  const { formatarValor, valorNumerico, setValorNumerico } = useMascaraMoeda()

  // ─── formatarValor ─────────────────────────────────────────────────────────

  describe("formatarValor", () => {
    it("string vazia retorna string vazia", () => {
      expect(formatarValor("")).toBe("")
    })

    it("só não-dígitos retorna string vazia", () => {
      expect(formatarValor("abc")).toBe("")
    })

    it("'100' → '1,00' (centavos)", () => {
      expect(formatarValor("100")).toBe("1,00")
    })

    it("'10000' → '100,00'", () => {
      expect(formatarValor("10000")).toBe("100,00")
    })

    it("'1000000' → '10.000,00' (separador de milhar)", () => {
      expect(formatarValor("1000000")).toBe("10.000,00")
    })

    it("ignora caracteres não-dígitos na entrada (ex.: já formatado)", () => {
      // Usuário digita "1.234,56" — o handler extrai só dígitos "123456" → "1.234,56"
      expect(formatarValor("1.234,56")).toBe("1.234,56")
    })

    it("'1' → '0,01' (centavo único)", () => {
      expect(formatarValor("1")).toBe("0,01")
    })
  })

  // ─── valorNumerico ─────────────────────────────────────────────────────────

  describe("valorNumerico", () => {
    it("converte '1.234,56' para 1234.56", () => {
      expect(valorNumerico("1.234,56")).toBeCloseTo(1234.56)
    })

    it("converte '100,00' para 100", () => {
      expect(valorNumerico("100,00")).toBe(100)
    })

    it("converte '0,01' para 0.01", () => {
      expect(valorNumerico("0,01")).toBeCloseTo(0.01)
    })

    it("string vazia retorna 0", () => {
      expect(valorNumerico("")).toBe(0)
    })

    it("string não-numérica retorna 0", () => {
      expect(valorNumerico("abc")).toBe(0)
    })
  })

  // ─── setValorNumerico ──────────────────────────────────────────────────────

  describe("setValorNumerico", () => {
    it("define 1234.56 como '1.234,56' no ref", () => {
      const valorStr = { value: "" }
      setValorNumerico(1234.56, valorStr)
      expect(valorStr.value).toBe("1.234,56")
    })

    it("define 0 como '0,00'", () => {
      const valorStr = { value: "" }
      setValorNumerico(0, valorStr)
      expect(valorStr.value).toBe("0,00")
    })

    it("define 100 como '100,00'", () => {
      const valorStr = { value: "" }
      setValorNumerico(100, valorStr)
      expect(valorStr.value).toBe("100,00")
    })
  })

  // ─── round-trip ────────────────────────────────────────────────────────────

  describe("round-trip formatarValor → valorNumerico", () => {
    it("'12345' (centavos) → formata → volta ao número original", () => {
      const formatted = formatarValor("12345")
      const num = valorNumerico(formatted)
      expect(num).toBeCloseTo(123.45)
    })
  })
})
