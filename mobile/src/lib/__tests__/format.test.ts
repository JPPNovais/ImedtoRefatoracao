import { describe, it, expect, vi, afterEach } from "vitest"
import {
  iniciais,
  idade,
  toISODate,
  valorLegivel,
  renderConteudoEvolucao,
  statusPill,
  moeda,
  dataCurta,
} from "@/lib/format"

// ─── iniciais ───────────────────────────────────────────────────────────────

describe("iniciais", () => {
  it("retorna as duas primeiras iniciais em maiúsculo", () => {
    expect(iniciais("João Silva")).toBe("JS")
  })

  it("retorna só a primeira inicial quando há uma palavra", () => {
    expect(iniciais("Carla")).toBe("C")
  })

  it("ignora espaços extras e retorna no máximo 2 iniciais", () => {
    expect(iniciais("  Ana  Paula  Costa  ")).toBe("AP")
  })

  it("retorna string vazia para nome vazio", () => {
    expect(iniciais("")).toBe("")
  })

  it("converte para maiúsculo mesmo que o nome venha minúsculo", () => {
    expect(iniciais("ana beatriz")).toBe("AB")
  })
})

// ─── idade ───────────────────────────────────────────────────────────────────

describe("idade", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("retorna null para null", () => {
    expect(idade(null)).toBeNull()
  })

  it("retorna null para undefined", () => {
    expect(idade(undefined)).toBeNull()
  })

  it("retorna null para data inválida", () => {
    expect(idade("não-é-data")).toBeNull()
  })

  it("calcula a idade corretamente para um adulto", () => {
    // Fixa 2026-06-20 como 'hoje'
    vi.setSystemTime(new Date("2026-06-20T12:00:00.000Z"))
    expect(idade("1990-06-20")).toBe(36)
  })

  it("ainda não completou aniversário no mesmo ano — borda antes do dia", () => {
    vi.setSystemTime(new Date("2026-06-19T23:59:59.000Z"))
    // Nasceu em 20/06/1990 — ainda não fez 36 neste momento
    expect(idade("1990-06-20")).toBe(35)
  })

  it("completa aniversário exatamente no dia — borda no dia", () => {
    vi.setSystemTime(new Date("2026-06-20T00:00:01.000Z"))
    expect(idade("1990-06-20")).toBe(36)
  })

  it("retorna 0 para recém-nascido no mesmo dia", () => {
    vi.setSystemTime(new Date("2026-06-20T15:00:00.000Z"))
    expect(idade("2026-06-20")).toBe(0)
  })
})

// ─── toISODate ───────────────────────────────────────────────────────────────

describe("toISODate", () => {
  it("formata data local sem shift de fuso", () => {
    // Cria objeto Date com data local clara
    const d = new Date(2026, 5, 20) // 20/06/2026 — mês é zero-based
    expect(toISODate(d)).toBe("2026-06-20")
  })

  it("formata 1º de janeiro sem shift de fuso", () => {
    const d = new Date(2026, 0, 1) // 01/01/2026
    expect(toISODate(d)).toBe("2026-01-01")
  })

  it("mantém a data local mesmo se UTC fosse o dia anterior", () => {
    // UTC-3 (horário de Brasília): 2026-06-20T01:30:00Z = 2026-06-19T22:30 local
    // Mas se criarmos um Date local às 22:30 do dia 19, toISODate deve retornar 2026-06-19
    const d = new Date(2026, 5, 19, 22, 30, 0) // 19/06/2026 22:30 local
    expect(toISODate(d)).toBe("2026-06-19")
  })

  it("mantém dezembro com padding correto", () => {
    const d = new Date(2026, 11, 5) // 05/12/2026
    expect(toISODate(d)).toBe("2026-12-05")
  })
})

// ─── valorLegivel ────────────────────────────────────────────────────────────

describe("valorLegivel", () => {
  it("converte string primitiva diretamente", () => {
    expect(valorLegivel("Hipertensão")).toBe("Hipertensão")
  })

  it("converte número para string", () => {
    expect(valorLegivel(42)).toBe("42")
  })

  it("converte booleano", () => {
    expect(valorLegivel(true)).toBe("true")
  })

  it("NÃO retorna [object Object] — converte objeto em pares legíveis", () => {
    const result = valorLegivel({ medicamento: "Losartana", dose: "50mg" })
    expect(result).not.toContain("[object Object]")
    expect(result).toContain("medicamento: Losartana")
    expect(result).toContain("dose: 50mg")
  })

  it("converte array de strings com vírgulas", () => {
    expect(valorLegivel(["Paracetamol", "Ibuprofeno"])).toBe("Paracetamol, Ibuprofeno")
  })

  it("converte array de objetos sem [object Object]", () => {
    const items = [{ nome: "Losartana" }, { nome: "Atenolol" }]
    const result = valorLegivel(items)
    expect(result).not.toContain("[object Object]")
    expect(result).toContain("Losartana")
    expect(result).toContain("Atenolol")
  })

  it("filtra entradas nulas/vazias dentro de objeto", () => {
    const result = valorLegivel({ campo: "valor", vazio: "", nulo: null })
    expect(result).toContain("campo: valor")
    expect(result).not.toContain("vazio")
    expect(result).not.toContain("nulo")
  })

  it("array vazio retorna string vazia", () => {
    expect(valorLegivel([])).toBe("")
  })
})

// ─── renderConteudoEvolucao ───────────────────────────────────────────────────

describe("renderConteudoEvolucao", () => {
  it("converte objeto em pares chave/valor", () => {
    const result = renderConteudoEvolucao({ queixa: "Dor de cabeça", intensidade: "7" })
    expect(result).toEqual([
      { chave: "queixa", valor: "Dor de cabeça" },
      { chave: "intensidade", valor: "7" },
    ])
  })

  it("filtra campos nulos e vazios", () => {
    const result = renderConteudoEvolucao({ campo: "ok", vazio: "", nulo: null, indefinido: undefined })
    expect(result).toHaveLength(1)
    expect(result[0].chave).toBe("campo")
  })

  it("campo array renderiza com vírgulas", () => {
    const result = renderConteudoEvolucao({ sintomas: ["febre", "tosse"] })
    expect(result[0].valor).toBe("febre, tosse")
  })

  it("NÃO gera [object Object] em campo aninhado", () => {
    const result = renderConteudoEvolucao({ medicamento: { nome: "Losartana", dose: "50mg" } })
    expect(result[0].valor).not.toContain("[object Object]")
    expect(result[0].valor).toContain("Losartana")
  })

  it("retorna array vazio para objeto sem campos válidos", () => {
    expect(renderConteudoEvolucao({ a: null, b: "" })).toEqual([])
  })
})

// ─── statusPill ──────────────────────────────────────────────────────────────

describe("statusPill", () => {
  it("Confirmado retorna variante success", () => {
    const p = statusPill("Confirmado")
    expect(p.cls).toBe("p-success")
    expect(p.label).toBe("Confirmou")
  })

  it("Cancelado retorna variante error", () => {
    const p = statusPill("Cancelado")
    expect(p.cls).toBe("p-error")
  })

  it("Faltou retorna variante error", () => {
    const p = statusPill("Faltou")
    expect(p.cls).toBe("p-error")
    expect(p.label).toBe("Faltou")
  })

  it("Concluido retorna variante muted com label Atendido", () => {
    const p = statusPill("Concluido")
    expect(p.cls).toBe("p-muted")
    expect(p.label).toBe("Atendido")
  })

  it("EmAtendimento retorna variante info", () => {
    const p = statusPill("EmAtendimento")
    expect(p.cls).toBe("p-info")
  })

  it("Aprovado (orçamento) retorna success", () => {
    expect(statusPill("Aprovado").cls).toBe("p-success")
  })

  it("Recusado (orçamento) retorna error", () => {
    expect(statusPill("Recusado").cls).toBe("p-error")
  })

  it("status desconhecido retorna warning com label igual ao status", () => {
    const p = statusPill("StatusNovo")
    expect(p.cls).toBe("p-warning")
    expect(p.label).toBe("StatusNovo")
  })

  it("string vazia no status retorna label padrão 'Agendado'", () => {
    const p = statusPill("")
    expect(p.label).toBe("Agendado")
  })
})

// ─── moeda ───────────────────────────────────────────────────────────────────

describe("moeda", () => {
  it("formata zero em BRL", () => {
    expect(moeda(0)).toMatch(/R\$/)
    expect(moeda(0)).toContain("0,00")
  })

  it("formata valor positivo com casas decimais", () => {
    const result = moeda(1234.56)
    expect(result).toContain("1.234,56")
  })

  it("formata valor negativo", () => {
    const result = moeda(-50)
    expect(result).toContain("50,00")
  })
})

// ─── dataCurta ───────────────────────────────────────────────────────────────

describe("dataCurta", () => {
  it("retorna — para null", () => {
    expect(dataCurta(null)).toBe("—")
  })

  it("retorna — para undefined", () => {
    expect(dataCurta(undefined)).toBe("—")
  })

  it("retorna — para string inválida", () => {
    expect(dataCurta("invalida")).toBe("—")
  })

  it("formata data ISO como dd/MM", () => {
    // Usando um ISO que tem hora para garantir parse
    const result = dataCurta("2026-03-15T12:00:00Z")
    // O resultado depende do fuso local — só verifica o formato dd/MM
    expect(result).toMatch(/^\d{2}\/\d{2}$/)
  })
})
