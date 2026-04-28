import httpClient from "./httpClient"

export interface PacienteListaItem {
    id: number
    nomeCompleto: string
    cpf: string | null
    dataNascimento: string | null
    telefone: string | null
    criadoEm: string
}

export interface PaginaPacientes {
    itens: PacienteListaItem[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface Paciente {
    id: number
    estabelecimentoId: number
    nomeCompleto: string
    cpf: string | null
    dataNascimento: string | null
    genero: string
    telefone: string | null
    email: string | null
    endereco: string | null
    observacoes: string | null
    criadoEm: string
    atualizadoEm: string | null
}

export interface PacientePayload {
    nomeCompleto: string
    cpf?: string
    dataNascimento?: string
    genero?: string
    telefone?: string
    email?: string
    endereco?: string
    observacoes?: string
}

export const pacienteService = {
    async listar(busca?: string, pagina = 1, tamanho = 20): Promise<PaginaPacientes> {
        const { data } = await httpClient.get<PaginaPacientes>("/paciente", {
            params: {
                busca: busca || undefined,
                pagina,
                tamanho,
            },
        })
        return data
    },

    async obter(id: number): Promise<Paciente> {
        const { data } = await httpClient.get<Paciente>(`/paciente/${id}`)
        return data
    },

    async criar(payload: PacientePayload): Promise<void> {
        await httpClient.post("/paciente", payload)
    },

    async atualizar(id: number, payload: PacientePayload): Promise<void> {
        await httpClient.put(`/paciente/${id}`, payload)
    },

    async deletar(id: number): Promise<void> {
        await httpClient.delete(`/paciente/${id}`)
    },

    async exportarDados(id: number): Promise<unknown> {
        const { data } = await httpClient.get(`/paciente/${id}/exportar-dados`)
        return data
    },
}
