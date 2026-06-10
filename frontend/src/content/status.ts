// Estado declarado manualmente do sistema — atualizado via commit.
// EstadoSistema é um conjunto fechado (CA7). Sem uptime medido até F0-E1.

export type EstadoSistema = "operacional" | "instabilidade" | "manutenção"

export interface StatusSistema {
    estado: EstadoSistema
    texto?: string       // detalhe/aviso opcional
    atualizadoEm: string // ISO "YYYY-MM-DD"
}

export const STATUS: StatusSistema = {
    estado: "operacional",
    atualizadoEm: "2026-06-10",
}
