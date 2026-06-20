import { describe, it, expect, vi } from "vitest"
import { useListaPaginada } from "@/composables/useListaPaginada"

/** Gera uma função de busca simulada com itens numerados. */
function criarBuscador(totalItens: number) {
  return vi.fn(async (pagina: number, tam: number) => {
    const inicio = (pagina - 1) * tam
    const fatia = Array.from({ length: Math.min(tam, totalItens - inicio) }, (_, i) => ({
      id: inicio + i + 1,
    }))
    return { itens: fatia, total: totalItens }
  })
}

describe("useListaPaginada", () => {
  it("carrega a primeira página em recarregar()", async () => {
    const buscar = criarBuscador(45)
    const lista = useListaPaginada(buscar, { tamanho: 20 })

    await lista.recarregar()

    expect(lista.itens.value).toHaveLength(20)
    expect(lista.total.value).toBe(45)
    expect(buscar).toHaveBeenCalledWith(1, 20)
  })

  it("temMais é true quando há mais itens do que os carregados", async () => {
    const lista = useListaPaginada(criarBuscador(45), { tamanho: 20 })
    await lista.recarregar()
    expect(lista.temMais.value).toBe(true)
  })

  it("temMais é false quando todos os itens foram carregados", async () => {
    const lista = useListaPaginada(criarBuscador(15), { tamanho: 20 })
    await lista.recarregar()
    expect(lista.temMais.value).toBe(false)
  })

  it("temMais é false quando total === itens.length exatamente", async () => {
    const lista = useListaPaginada(criarBuscador(20), { tamanho: 20 })
    await lista.recarregar()
    expect(lista.itens.value).toHaveLength(20)
    expect(lista.temMais.value).toBe(false)
  })

  it("carregarMais() faz append da próxima página", async () => {
    const lista = useListaPaginada(criarBuscador(45), { tamanho: 20 })
    await lista.recarregar()
    await lista.carregarMais()

    expect(lista.itens.value).toHaveLength(40)
    expect(lista.total.value).toBe(45)
  })

  it("carregarMais() carrega o restante quando a última página tem menos itens", async () => {
    const lista = useListaPaginada(criarBuscador(45), { tamanho: 20 })
    // p1=20, p2=20, p3=5 → total 45
    await lista.recarregar()         // p1 → 20 itens
    await lista.carregarMais()       // p2 → 20 itens (total 40)
    await lista.carregarMais()       // p3 → 5 itens (total 45)

    expect(lista.itens.value).toHaveLength(45)
    expect(lista.temMais.value).toBe(false)
  })

  it("carregarMais() não dispara quando temMais é false", async () => {
    const buscar = criarBuscador(10)
    const lista = useListaPaginada(buscar, { tamanho: 20 })
    await lista.recarregar()

    const chamadas = buscar.mock.calls.length
    await lista.carregarMais() // não deve chamar de novo
    expect(buscar.mock.calls.length).toBe(chamadas)
  })

  it("recarregar() reseta a lista — não faz append do que já existia", async () => {
    const lista = useListaPaginada(criarBuscador(45), { tamanho: 20 })
    await lista.recarregar()
    await lista.carregarMais() // 40 itens

    await lista.recarregar()  // deve voltar para 20 (reset)
    expect(lista.itens.value).toHaveLength(20)
  })

  it("recarregar() reseta paginaAtual para 1 — carregarMais vai para p2", async () => {
    const buscar = criarBuscador(45)
    const lista = useListaPaginada(buscar, { tamanho: 20 })

    await lista.recarregar()
    await lista.carregarMais() // agora em p2
    await lista.recarregar()   // volta p1
    await lista.carregarMais() // deve ir para p2 (não p3)

    // Última chamada deve ter sido com pagina=2
    const chamadas = buscar.mock.calls
    const ultimaChamada = chamadas[chamadas.length - 1]
    expect(ultimaChamada[0]).toBe(2)
  })

  it("carregando é true durante recarregar() e false depois", async () => {
    let resolvePromise!: (v: { itens: never[]; total: number }) => void
    const buscarLento = () =>
      new Promise<{ itens: never[]; total: number }>((res) => {
        resolvePromise = res
      })

    const lista = useListaPaginada(buscarLento)
    const carregarPromise = lista.recarregar()
    expect(lista.carregando.value).toBe(true)
    resolvePromise({ itens: [], total: 0 })
    await carregarPromise
    expect(lista.carregando.value).toBe(false)
  })

  it("usa tamanho padrão 20 quando não especificado", async () => {
    const buscar = criarBuscador(100)
    const lista = useListaPaginada(buscar) // sem opcoes
    await lista.recarregar()
    expect(buscar).toHaveBeenCalledWith(1, 20)
  })

  it("respeita tamanho customizado nas chamadas", async () => {
    const buscar = criarBuscador(100)
    const lista = useListaPaginada(buscar, { tamanho: 10 })
    await lista.recarregar()
    expect(buscar).toHaveBeenCalledWith(1, 10)
  })
})
