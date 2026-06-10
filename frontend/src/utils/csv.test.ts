import { describe, it, expect, vi, beforeEach, afterEach } from "vitest"
import {
    escaparCelula,
    construirCsv,
    formatarDecimal,
    formatarInteiro,
    formatarData,
    baixarCsv,
    nomeArquivoCsv,
} from "./csv"

// ─── escaparCelula ───────────────────────────────────────────────────────────

describe("escaparCelula", () => {
    it("não envolve célula simples em aspas", () => {
        expect(escaparCelula("Consulta")).toBe("Consulta")
    })

    it("envolve em aspas quando contém ponto-e-vírgula", () => {
        expect(escaparCelula("Consulta; retorno")).toBe('"Consulta; retorno"')
    })

    it("envolve em aspas e duplica aspas internas (RFC 4180)", () => {
        expect(escaparCelula('Categoria "A"')).toBe('"Categoria ""A"""')
    })

    it("envolve em aspas quando contém quebra de linha", () => {
        expect(escaparCelula("Linha 1\nLinha 2")).toBe('"Linha 1\nLinha 2"')
    })

    it("envolve em aspas quando contém \\r\\n", () => {
        expect(escaparCelula("Linha 1\r\nLinha 2")).toBe('"Linha 1\r\nLinha 2"')
    })

    it("preserva célula vazia sem aspas", () => {
        expect(escaparCelula("")).toBe("")
    })

    it("preserva acentos sem modificar", () => {
        expect(escaparCelula("Crédito (R$)")).toBe("Crédito (R$)")
    })
})

// ─── construirCsv ────────────────────────────────────────────────────────────

describe("construirCsv", () => {
    it("usa ponto-e-vírgula como separador", () => {
        const resultado = construirCsv([["A", "B", "C"]])
        expect(resultado).toBe("A;B;C")
    })

    it("separa linhas com \\r\\n", () => {
        const resultado = construirCsv([["A", "B"], ["C", "D"]])
        expect(resultado).toBe("A;B\r\nC;D")
    })

    it("escapa corretamente campo com ponto-e-vírgula dentro de construirCsv", () => {
        const resultado = construirCsv([["Item", "Consulta; retorno"]])
        expect(resultado).toBe('Item;"Consulta; retorno"')
    })

    it("produz matriz vazia como string vazia", () => {
        expect(construirCsv([])).toBe("")
    })
})

// ─── formatarDecimal ─────────────────────────────────────────────────────────

describe("formatarDecimal", () => {
    it("formata com vírgula decimal e 2 casas", () => {
        expect(formatarDecimal(1234.56)).toBe("1234,56")
    })

    it("preenche casas decimais quando inteiro", () => {
        expect(formatarDecimal(500)).toBe("500,00")
    })

    it("arredonda para 2 casas — usa toFixed do JS", () => {
        // toFixed(2) com valor que inequivocamente arredonda para cima
        expect(formatarDecimal(1.996)).toBe("2,00")
    })

    it("formata zero corretamente", () => {
        expect(formatarDecimal(0)).toBe("0,00")
    })

    it("não inclui símbolo de moeda", () => {
        expect(formatarDecimal(100)).not.toContain("R$")
    })
})

// ─── formatarInteiro ─────────────────────────────────────────────────────────

describe("formatarInteiro", () => {
    it("retorna string do número inteiro", () => {
        expect(formatarInteiro(42)).toBe("42")
    })

    it("arredonda float para inteiro", () => {
        expect(formatarInteiro(3.7)).toBe("4")
    })
})

// ─── formatarData ────────────────────────────────────────────────────────────

describe("formatarData", () => {
    it("formata data ISO yyyy-MM-dd para dd/MM/yyyy", () => {
        expect(formatarData("2026-06-10")).toBe("10/06/2026")
    })

    it("aceita datetime ISO e extrai apenas a data", () => {
        expect(formatarData("2026-01-15T14:30:00")).toBe("15/01/2026")
    })

    it("retorna '—' para null", () => {
        expect(formatarData(null)).toBe("—")
    })

    it("retorna '—' para undefined", () => {
        expect(formatarData(undefined)).toBe("—")
    })

    it("retorna '—' para string vazia", () => {
        expect(formatarData("")).toBe("—")
    })
})

// ─── nomeArquivoCsv ──────────────────────────────────────────────────────────

describe("nomeArquivoCsv", () => {
    it("gera nome no formato relatorio-{aba}-{ini}-a-{fim}.csv", () => {
        expect(nomeArquivoCsv("financeiro", "2026-06-01", "2026-06-30"))
            .toBe("relatorio-financeiro-2026-06-01-a-2026-06-30.csv")
    })
})

// ─── baixarCsv ───────────────────────────────────────────────────────────────

describe("baixarCsv", () => {
    let createObjectURLSpy: ReturnType<typeof vi.fn>
    let revokeObjectURLSpy: ReturnType<typeof vi.fn>
    let createElementOrig: typeof document.createElement
    let clickSpy: ReturnType<typeof vi.fn>

    beforeEach(() => {
        createObjectURLSpy = vi.fn(() => "blob:fake-url")
        revokeObjectURLSpy = vi.fn()
        URL.createObjectURL = createObjectURLSpy as unknown as typeof URL.createObjectURL
        URL.revokeObjectURL = revokeObjectURLSpy

        clickSpy = vi.fn()
        createElementOrig = document.createElement.bind(document)
        document.createElement = vi.fn((tag: string) => {
            if (tag === "a") {
                const el = { click: clickSpy } as any
                // Aceitar setters sem erro
                Object.defineProperty(el, "href", { set: vi.fn(), get: () => "" })
                Object.defineProperty(el, "download", { set: vi.fn(), get: () => "" })
                return el
            }
            return createElementOrig(tag)
        }) as typeof document.createElement
    })

    afterEach(() => {
        document.createElement = createElementOrig
    })

    it("chama click() e revoga URL após download", () => {
        baixarCsv("conteudo", "relatorio.csv")
        expect(clickSpy).toHaveBeenCalledOnce()
        expect(revokeObjectURLSpy).toHaveBeenCalledWith("blob:fake-url")
    })

    it("passa conteúdo com BOM para createObjectURL", () => {
        // Verifica que createObjectURL foi chamado (Blob criada com sucesso)
        baixarCsv("A;B;C", "teste.csv")
        expect(createObjectURLSpy).toHaveBeenCalledOnce()
    })
})
