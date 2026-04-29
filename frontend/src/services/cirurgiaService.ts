import httpClient from "./httpClient"

export type StatusProcedimento = "Planejado" | "Confirmado" | "Realizado" | "Cancelado"
export type PapelCirurgia = "Cirurgiao" | "Auxiliar" | "Anestesista" | "Instrumentador" | "Circulante"

export interface MembroEquipe {
    id?: number
    profissionalUsuarioId: string
    profissionalNome?: string
    papel: PapelCirurgia
    ordem: number
}

export interface ProcedimentoCirurgico {
    id: number
    pacienteId: number
    prontuarioId: number
    estabelecimentoId: number
    agendamentoId: number | null
    dataAgendada: string | null
    dataRealizada: string | null
    status: StatusProcedimento
    descricaoCirurgica: string | null
    fichaAnestesica: Record<string, unknown> | null
    evolucaoPosOp: string | null
    cirurgiaPrincipal: string
    cirurgiaCodigoTuss: string | null
    observacoes: string | null
    equipe: MembroEquipe[]
    criadoEm: string
}

export interface PaginaCirurgias {
    total: number
    pagina: number
    tamanho: number
    itens: ProcedimentoCirurgico[]
}

export const cirurgiaService = {
    async listar(params?: { pacienteId?: number; pagina?: number; tamanho?: number }): Promise<PaginaCirurgias> {
        const { data } = await httpClient.get<PaginaCirurgias>("/procedimentos-cirurgicos", { params })
        return data
    },

    async obter(id: number): Promise<ProcedimentoCirurgico> {
        const { data } = await httpClient.get<ProcedimentoCirurgico>(`/procedimentos-cirurgicos/${id}`)
        return data
    },

    async planejar(payload: {
        pacienteId: number
        prontuarioId: number
        cirurgiaPrincipal: string
        dataAgendada?: string
        observacoes?: string
        equipe?: Omit<MembroEquipe, "id">[]
    }): Promise<ProcedimentoCirurgico> {
        const { data } = await httpClient.post<ProcedimentoCirurgico>("/procedimentos-cirurgicos", payload)
        return data
    },

    async atualizar(id: number, payload: Partial<ProcedimentoCirurgico>): Promise<void> {
        await httpClient.put(`/procedimentos-cirurgicos/${id}`, payload)
    },

    async confirmar(id: number): Promise<void> {
        await httpClient.post(`/procedimentos-cirurgicos/${id}/confirmar`)
    },

    async registrarRealizacao(id: number, payload: {
        dataRealizada: string
        descricaoCirurgica?: string
        fichaAnestesica?: Record<string, unknown>
        evolucaoPosOp?: string
    }): Promise<void> {
        await httpClient.post(`/procedimentos-cirurgicos/${id}/realizar`, payload)
    },

    async cancelar(id: number, motivo: string): Promise<void> {
        await httpClient.post(`/procedimentos-cirurgicos/${id}/cancelar`, { motivo })
    },
}

export default cirurgiaService

export const PAPEIS_CIRURGIA: { valor: PapelCirurgia; label: string }[] = [
    { valor: "Cirurgiao",      label: "Cirurgiao" },
    { valor: "Auxiliar",       label: "Auxiliar" },
    { valor: "Anestesista",    label: "Anestesista" },
    { valor: "Instrumentador", label: "Instrumentador" },
    { valor: "Circulante",     label: "Circulante" },
]
