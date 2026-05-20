import httpClient from "./httpClient"

/**
 * Atestados emitidos pelo profissional. Backend é fonte da verdade — todas as
 * validações (afastamento exige dias > 0, CID regex, conteúdo obrigatório)
 * vivem no aggregate; frontend só replica para UX antes do submit.
 */

export type TipoAtestado = "Afastamento" | "Comparecimento" | "Aptidao" | "Outro"

export interface Atestado {
    id: number
    pacienteId: number
    profissionalUsuarioId: string
    profissionalNome: string | null
    tipo: TipoAtestado
    diasAfastamento: number | null
    cid10: string | null
    conteudo: string
    criadoEm: string
}

export interface ModeloAtestado {
    id: number
    nome: string
    tipo: TipoAtestado
    conteudo: string
    criadoEm: string
    atualizadoEm: string | null
}

export interface EmitirAtestadoInput {
    tipo: TipoAtestado
    diasAfastamento?: number | null
    cid10?: string | null
    conteudo: string
}

export interface SalvarModeloAtestadoInput {
    nome: string
    tipo: TipoAtestado
    conteudo: string
}

export interface PaginaAtestados {
    itens: Atestado[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export const atestadoService = {
    async emitir(pacienteId: number, input: EmitirAtestadoInput): Promise<{ atestadoId: number }> {
        const { data } = await httpClient.post<{ atestadoId: number }>(
            `/pacientes/${pacienteId}/atestados`,
            input,
        )
        return data
    },

    async listarDoPaciente(
        pacienteId: number,
        params: { pagina?: number; tamanho?: number } = {},
    ): Promise<PaginaAtestados> {
        const { data } = await httpClient.get<PaginaAtestados>(
            `/pacientes/${pacienteId}/atestados`,
            { params: { pagina: params.pagina ?? 1, tamanho: params.tamanho ?? 10 } },
        )
        return data
    },

    async obter(id: number): Promise<Atestado> {
        const { data } = await httpClient.get<Atestado>(`/atestados/${id}`)
        return data
    },

    async listarModelos(): Promise<ModeloAtestado[]> {
        const { data } = await httpClient.get<ModeloAtestado[]>(`/modelos-atestado`)
        return data
    },

    async criarModelo(input: SalvarModeloAtestadoInput): Promise<{ modeloId: number }> {
        const { data } = await httpClient.post<{ modeloId: number }>(`/modelos-atestado`, input)
        return data
    },

    async atualizarModelo(id: number, input: SalvarModeloAtestadoInput): Promise<void> {
        await httpClient.put(`/modelos-atestado/${id}`, input)
    },

    async excluirModelo(id: number): Promise<void> {
        await httpClient.delete(`/modelos-atestado/${id}`)
    },
}
