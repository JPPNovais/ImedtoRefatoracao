import { describe, it, expect, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { usePermissoesStore } from "@/stores/permissoes"

describe("usePermissoesStore — pode()", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it("retorna false quando papel não está definido", () => {
    const store = usePermissoesStore()
    expect(store.pode("agenda.visualizar")).toBe(false)
  })

  it("Dono pode qualquer permissão (curto-circuito)", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Dono", permissoes: [] })
    expect(store.pode("agenda.visualizar")).toBe(true)
    expect(store.pode("financeiro.excluir")).toBe(true)
    expect(store.pode("qualquer.coisa")).toBe(true)
  })

  it("concede por permissão exata 'area.acao'", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Profissional", permissoes: ["agenda.visualizar"] })
    expect(store.pode("agenda.visualizar")).toBe(true)
  })

  it("concede por wildcard 'area.*'", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Profissional", permissoes: ["agenda.*"] })
    expect(store.pode("agenda.visualizar")).toBe(true)
    expect(store.pode("agenda.editar")).toBe(true)
  })

  it("concede por permissão de área (sem acao) quando só a area está listada", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Profissional", permissoes: ["agenda"] })
    expect(store.pode("agenda.visualizar")).toBe(true)
    expect(store.pode("agenda.editar")).toBe(true)
  })

  it("nega quando permissão de área diferente", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Profissional", permissoes: ["agenda.visualizar"] })
    expect(store.pode("financeiro.visualizar")).toBe(false)
  })

  it("nega quando ação específica não está incluída", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Profissional", permissoes: ["agenda.visualizar"] })
    expect(store.pode("agenda.excluir")).toBe(false)
  })

  it("chave sem ponto — retorna true se alguma permissão começa com 'chave.'", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Recepcionista", permissoes: ["agenda.visualizar"] })
    expect(store.pode("agenda")).toBe(true)
  })

  it("chave sem ponto — retorna false quando nenhuma permissão começa com a área", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Recepcionista", permissoes: ["financeiro.visualizar"] })
    expect(store.pode("agenda")).toBe(false)
  })

  it("limpar() reseta o estado e passa a negar tudo", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Dono", permissoes: [] })
    store.limpar()
    expect(store.pode("agenda.visualizar")).toBe(false)
    expect(store.papel).toBeNull()
  })

  it("ehDono é true somente para papel Dono", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Dono", permissoes: [] })
    expect(store.ehDono).toBe(true)
    store.definir({ papel: "Profissional", permissoes: [] })
    expect(store.ehDono).toBe(false)
  })

  it("podeExtra() respeita permissoesExtras, não permissoes", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Profissional", permissoes: [], permissoesExtras: ["feature_x"] })
    expect(store.podeExtra("feature_x")).toBe(true)
    expect(store.podeExtra("feature_y")).toBe(false)
  })

  it("podeExtra() Dono pode extra sem estar na lista", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Dono", permissoes: [], permissoesExtras: [] })
    expect(store.podeExtra("qualquer_extra")).toBe(true)
  })

  it("múltiplas permissões — concede a que bate exatamente", () => {
    const store = usePermissoesStore()
    store.definir({ papel: "Recepcionista", permissoes: ["agenda.visualizar", "paciente.visualizar"] })
    expect(store.pode("agenda.visualizar")).toBe(true)
    expect(store.pode("paciente.visualizar")).toBe(true)
    expect(store.pode("financeiro.visualizar")).toBe(false)
  })
})
