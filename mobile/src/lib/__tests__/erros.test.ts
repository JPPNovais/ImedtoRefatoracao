import { describe, it, expect } from "vitest"
import { mensagemDeErro } from "@/lib/erros"
import type { ApiError } from "@/types"

describe("mensagemDeErro", () => {
  it("retorna a mensagem do ApiError quando há campo 'status'", () => {
    const err: ApiError = { status: 422, mensagem: "Paciente não encontrado" }
    expect(mensagemDeErro(err, "Erro genérico")).toBe("Paciente não encontrado")
  })

  it("retorna a mensagem mesmo para status 400", () => {
    const err: ApiError = { status: 400, mensagem: "Campos inválidos" }
    expect(mensagemDeErro(err, "Erro genérico")).toBe("Campos inválidos")
  })

  it("retorna a mensagem mesmo para status 500", () => {
    const err: ApiError = { status: 500, mensagem: "Erro interno" }
    expect(mensagemDeErro(err, "Fallback")).toBe("Erro interno")
  })

  it("retorna o fallback para erro de rede (sem 'status')", () => {
    // Simula TypeError lançado pelo fetch quando não há conexão
    const err = new TypeError("Failed to fetch")
    expect(mensagemDeErro(err, "Sem conexão com a internet")).toBe("Sem conexão com a internet")
  })

  it("retorna o fallback para erro genérico Error", () => {
    expect(mensagemDeErro(new Error("qualquer"), "Fallback offline")).toBe("Fallback offline")
  })

  it("retorna o fallback para null", () => {
    expect(mensagemDeErro(null, "Fallback")).toBe("Fallback")
  })

  it("retorna o fallback para undefined", () => {
    expect(mensagemDeErro(undefined, "Fallback")).toBe("Fallback")
  })

  it("retorna o fallback para string pura (não é ApiError)", () => {
    expect(mensagemDeErro("algum erro", "Fallback")).toBe("Fallback")
  })

  it("retorna o fallback quando ApiError.mensagem está vazio", () => {
    // Caso raro: status presente mas mensagem vazia → fallback
    const err = { status: 422, mensagem: "" }
    expect(mensagemDeErro(err, "Fallback vazio")).toBe("Fallback vazio")
  })
})
