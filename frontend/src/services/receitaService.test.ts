/**
 * Testes do campo Quantidade da receita (número + unidade).
 *
 * A quantidade é persistida como string única em `quantidade` (ex.: "30 comprimido(s)").
 * `montarQuantidade` junta número + unidade ao salvar; `parseQuantidade` separa de volta
 * na edição, tolerando dados livres/legados ("1 caixa", "124124asfafaf", "500mg").
 */
import { describe, it, expect } from "vitest"
import { montarQuantidade, parseQuantidade } from "./receitaService"

describe("montarQuantidade", () => {
    it("junta número e unidade", () => {
        expect(montarQuantidade("30", "comprimido(s)")).toBe("30 comprimido(s)")
    })

    it("número sem unidade fica só o número", () => {
        expect(montarQuantidade("30", "")).toBe("30")
    })

    it("aplica trim em número e unidade", () => {
        expect(montarQuantidade(" 30 ", " mL ")).toBe("30 mL")
    })

    it("número vazio retorna null (unidade sozinha é descartada)", () => {
        expect(montarQuantidade("", "caixa(s)")).toBeNull()
        expect(montarQuantidade("   ", "caixa(s)")).toBeNull()
    })
})

describe("parseQuantidade", () => {
    it("separa número + unidade canônica", () => {
        expect(parseQuantidade("30 comprimido(s)")).toEqual({ numero: "30", unidade: "comprimido(s)" })
    })

    it("casa unidade legada no singular/plural sem parênteses", () => {
        expect(parseQuantidade("1 caixa")).toEqual({ numero: "1", unidade: "caixa(s)" })
        expect(parseQuantidade("2 frascos")).toEqual({ numero: "2", unidade: "frasco(s)" })
        expect(parseQuantidade("10 gotas")).toEqual({ numero: "10", unidade: "gota(s)" })
    })

    it("casa unidade sem flexão de plural (mL)", () => {
        expect(parseQuantidade("30 mL")).toEqual({ numero: "30", unidade: "mL" })
    })

    it("extrai o número e descarta texto livre desconhecido", () => {
        expect(parseQuantidade("124124asfafaf")).toEqual({ numero: "124124", unidade: "" })
        expect(parseQuantidade("500mg")).toEqual({ numero: "500", unidade: "" })
    })

    it("texto sem número inicial volta vazio", () => {
        expect(parseQuantidade("tomar conforme orientação")).toEqual({ numero: "", unidade: "" })
    })

    it("trata vazio/null/undefined", () => {
        expect(parseQuantidade("")).toEqual({ numero: "", unidade: "" })
        expect(parseQuantidade(null)).toEqual({ numero: "", unidade: "" })
        expect(parseQuantidade(undefined)).toEqual({ numero: "", unidade: "" })
    })

    it("é o inverso de montarQuantidade para valores do formulário", () => {
        const montado = montarQuantidade("30", "comprimido(s)")
        expect(parseQuantidade(montado)).toEqual({ numero: "30", unidade: "comprimido(s)" })
    })
})
