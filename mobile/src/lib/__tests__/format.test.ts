import { describe, it, expect, vi, afterEach, beforeEach } from "vitest"
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
  beforeEach(() => {
    vi.useFakeTimers()
  })

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

  it("retorna null para formato sem YYYY-MM-DD", () => {
    expect(idade("20/06/1990")).toBeNull()
  })

  it("calcula a idade corretamente para um adulto", () => {
    // Fixa 2026-06-20 como 'hoje' (via componentes locais, fuso não importa)
    vi.setSystemTime(new Date(2026, 5, 20, 12, 0, 0)) // 20/06/2026 12:00 local
    expect(idade("1990-06-20")).toBe(36)
  })

  it("ainda não completou aniversário — borda: dia anterior ao aniversário", () => {
    vi.setSystemTime(new Date(2026, 5, 19, 23, 59, 59)) // 19/06/2026 local
    // Nasceu em 20/06/1990 — ainda não fez 36
    expect(idade("1990-06-20")).toBe(35)
  })

  it("completa aniversário exatamente no dia — borda: dia do aniversário", () => {
    vi.setSystemTime(new Date(2026, 5, 20, 0, 0, 1)) // 20/06/2026 local
    expect(idade("1990-06-20")).toBe(36)
  })

  it("dia posterior ao aniversário no mesmo ano — retorna idade correta", () => {
    vi.setSystemTime(new Date(2026, 5, 21, 10, 0, 0)) // 21/06/2026 local
    expect(idade("1990-06-20")).toBe(36)
  })

  it("retorna 0 para recém-nascido no mesmo dia", () => {
    vi.setSystemTime(new Date(2026, 5, 20, 15, 0, 0)) // 20/06/2026 local
    expect(idade("2026-06-20")).toBe(0)
  })

  it("ano bissexto — aniversário em 29/02, hoje é 28/02 (ainda não fez anos)", () => {
    // Hoje é 28/02/2028 (2028 é bissexto) — nasceu em 29/02/2000
    vi.setSystemTime(new Date(2028, 1, 28, 12, 0, 0)) // 28/02/2028 local
    expect(idade("2000-02-29")).toBe(27)
  })

  it("ano bissexto — aniversário em 29/02, hoje é o próprio dia (faz anos)", () => {
    vi.setSystemTime(new Date(2028, 1, 29, 12, 0, 0)) // 29/02/2028 local
    expect(idade("2000-02-29")).toBe(28)
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

  it("YYYY-MM-DD sem hora — não pula dia (parse local, sem shift UTC)", () => {
    // new Date("2026-03-15") seria UTC meia-noite → em UTC-3 viraria 14/03
    // Com parse local, deve retornar 15/03 independente do fuso
    expect(dataCurta("2026-03-15")).toBe("15/03")
  })

  it("YYYY-MM-DD — 01/01 retorna 01/01 sem shift", () => {
    expect(dataCurta("2026-01-01")).toBe("01/01")
  })

  it("YYYY-MM-DD — 31/12 retorna 31/12 sem shift", () => {
    expect(dataCurta("2026-12-31")).toBe("31/12")
  })

  it("ISO com hora UTC — parse normal, formato dd/MM", () => {
    // Com hora explícita o resultado depende do fuso do runner; verifica apenas o formato
    const result = dataCurta("2026-03-15T12:00:00Z")
    expect(result).toMatch(/^\d{2}\/\d{2}$/)
  })

  it("ISO com hora e offset local — parse normal, formato dd/MM", () => {
    const result = dataCurta("2026-06-20T10:30:00-03:00")
    expect(result).toMatch(/^\d{2}\/\d{2}$/)
  })
})
