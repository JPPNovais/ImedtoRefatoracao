import httpClient from "./httpClient"

export interface HorarioBloqueado {
    id: string
    inicio: string  // "HH:mm" ou "HH:mm:ss"
    fim: string
    descricao: string
}

export interface DataBloqueada {
    id: string
    data: string    // "YYYY-MM-DD"
    descricao: string
}

export interface Estabelecimento {
    id: number
    donoUsuarioId: string
    nomeFantasia: string
    razaoSocial: string | null
    cnpj: string | null
    telefone: string | null
    endereco: string | null
    fotoUrl: string | null
    status: string
    criadoEm: string
    papelDoUsuario: "Dono" | "Profissional"
    horarioInicio: string                       // "HH:mm" ou "HH:mm:ss"
    horarioFim: string
    diasSemanaFuncionamento: number[]           // 0=Domingo .. 6=Sábado
    horariosBloqueados: HorarioBloqueado[]
    datasBloqueadas: DataBloqueada[]
}

export interface CriarEstabelecimentoPayload {
    nomeFantasia: string
    razaoSocial?: string
    cnpj?: string
    telefone?: string
    endereco?: string
}

export interface AtualizarFuncionamentoPayload {
    horarioInicio: string
    horarioFim: string
    diasSemana: number[]
    horariosBloqueados: { id?: string; inicio: string; fim: string; descricao: string }[]
    datasBloqueadas:    { id?: string; data: string; descricao: string }[]
}

export const estabelecimentoService = {
    async listarMeus(): Promise<Estabelecimento[]> {
        const { data } = await httpClient.get<Estabelecimento[]>("/estabelecimento")
        return data
    },

    async criar(payload: CriarEstabelecimentoPayload): Promise<void> {
        await httpClient.post("/estabelecimento", payload)
    },

    async atualizar(id: number, payload: CriarEstabelecimentoPayload): Promise<void> {
        await httpClient.put(`/estabelecimento/${id}`, payload)
    },

    async atualizarFuncionamento(id: number, payload: AtualizarFuncionamentoPayload): Promise<void> {
        await httpClient.put(`/estabelecimento/${id}/funcionamento`, payload)
    },

    /** Faz upload da foto/logo do estabelecimento; devolve a nova URL pública. */
    async uploadFoto(id: number, arquivo: File): Promise<string> {
        const form = new FormData()
        form.append("arquivo", arquivo)
        const { data } = await httpClient.put<{ fotoUrl: string }>(
            `/estabelecimento/${id}/foto`,
            form,
            { headers: { "Content-Type": "multipart/form-data" } },
        )
        return data.fotoUrl
    },
}
