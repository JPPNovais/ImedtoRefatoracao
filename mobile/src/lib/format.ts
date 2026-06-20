/** Helpers de formatação puros (sem PII em log, mensagens genéricas). */

export function iniciais(nome: string): string {
  return nome
    .trim()
    .split(/\s+/)
    .map((w) => w[0])
    .filter(Boolean)
    .slice(0, 2)
    .join("")
    .toUpperCase()
}

export function idade(dataNascimento?: string | null): number | null {
  if (!dataNascimento) return null
  const d = new Date(dataNascimento)
  if (Number.isNaN(d.getTime())) return null
  const diff = Date.now() - d.getTime()
  return Math.floor(diff / (365.25 * 24 * 3600 * 1000))
}

/** Data local em yyyy-MM-dd (sem shift de fuso, ao contrário de toISOString). */
export function toISODate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, "0")
  const day = String(d.getDate()).padStart(2, "0")
  return `${y}-${m}-${day}`
}

export function horaDe(iso: string): string {
  const d = new Date(iso)
  return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`
}

export function dataCurta(iso?: string | null): string {
  if (!iso) return "—"
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return "—"
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`
}

export function moeda(v: number): string {
  return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

/** Busca acento-insensível. */
export function norm(s: string): string {
  return s
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
}

/** Mapeia status (agendamento ou orçamento) → variante de pill (cor + ícone + label). */
export function statusPill(status: string): { cls: string; icon: string; label: string } {
  switch (status) {
    case "Confirmado":
      return { cls: "p-success", icon: "fa-circle-check", label: "Confirmou" }
    case "Concluido":
      return { cls: "p-muted", icon: "fa-check", label: "Atendido" }
    case "Cancelado":
      return { cls: "p-error", icon: "fa-circle-xmark", label: "Cancelado" }
    case "Faltou":
      return { cls: "p-error", icon: "fa-circle-xmark", label: "Faltou" }
    case "EmAtendimento":
      return { cls: "p-info", icon: "fa-circle", label: "Em atendimento" }
    // Orçamentos
    case "Aprovado":
      return { cls: "p-success", icon: "fa-circle-check", label: "Aprovado" }
    case "Recusado":
      return { cls: "p-error", icon: "fa-circle-xmark", label: "Recusado" }
    case "Aguardando aprovação":
    case "Aguardando":
      return { cls: "p-warning", icon: "fa-circle", label: "Aguardando aprovação" }
    case "Agendado":
      return { cls: "p-warning", icon: "fa-circle", label: "Agendado" }
    default:
      return { cls: "p-warning", icon: "fa-circle", label: status || "Agendado" }
  }
}

/** Categoria de notificação → ícone + classe de cor. */
export function categoriaNotif(cat: string): { cls: string; icon: string } {
  switch (cat) {
    case "NovoAgendamento":
      return { cls: "c-info", icon: "fa-calendar-plus" }
    case "Cancelamento":
      return { cls: "c-error", icon: "fa-calendar-xmark" }
    case "Lembrete":
      return { cls: "c-warning", icon: "fa-bell" }
    case "Receita":
      return { cls: "c-success", icon: "fa-file-circle-check" }
    case "Confirmacao":
      return { cls: "c-success", icon: "fa-circle-check" }
    case "Vinculo":
      return { cls: "c-violet", icon: "fa-user-plus" }
    default:
      return { cls: "c-violet", icon: "fa-bell" }
  }
}

export function tempoRelativo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const min = Math.floor(diff / 60000)
  if (min < 1) return "agora"
  if (min < 60) return `há ${min} min`
  const h = Math.floor(min / 60)
  if (h < 24) return `há ${h} h`
  const d = new Date(iso)
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`
}

/** Converte valor aninhado de conteúdo de evolução para string legível.
 *  Array → itens separados por vírgula; objeto → pares "chave: valor"; primitivo → String(). */
export function valorLegivel(v: unknown): string {
  if (Array.isArray(v)) return v.map((item) => valorLegivel(item)).join(", ")
  if (v !== null && typeof v === "object") {
    return Object.entries(v as Record<string, unknown>)
      .filter(([, val]) => val !== null && val !== undefined && val !== "")
      .map(([k, val]) => `${k}: ${valorLegivel(val)}`)
      .join("; ")
  }
  return String(v)
}

/** Renderiza conteúdo estruturado de evolução como pares chave/valor legíveis. */
export function renderConteudoEvolucao(conteudo: Record<string, unknown>): Array<{ chave: string; valor: string }> {
  return Object.entries(conteudo)
    .filter(([, v]) => v !== null && v !== undefined && v !== "")
    .map(([k, v]) => ({ chave: k, valor: valorLegivel(v) }))
}

export function grupoNotif(iso: string): "Hoje" | "Ontem" | "Anteriores" {
  const d = new Date(iso)
  const hoje = new Date()
  const ontem = new Date()
  ontem.setDate(hoje.getDate() - 1)
  const sameDay = (a: Date, b: Date) => a.toDateString() === b.toDateString()
  if (sameDay(d, hoje)) return "Hoje"
  if (sameDay(d, ontem)) return "Ontem"
  return "Anteriores"
}
