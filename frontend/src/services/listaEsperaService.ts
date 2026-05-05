import httpClient from "./httpClient"

export type ListaEsperaPrioridade = "Rotina" | "Prioritario" | "Urgente"
export type ListaEsperaPreferenciaPeriodo = "Qualquer" | "Manha" | "Tarde"

export interface ListaEsperaItem {
    id: number
    pacienteId: number
    pacienteNome: string
    pacienteTelefone: string | null
    motivo: string
    profissionalPreferidoId: string | null
    profissionalPreferidoNome: string | null
    prioridade: ListaEsperaPrioridade
    preferenciaPeriodo: ListaEsperaPreferenciaPeriodo
    criadoEm: string
    minutosDesdeQueEntrou: number
}

export interface AdicionarListaEsperaPayload {
    pacienteId: number
    motivo: string
    profissionalPreferidoId?: string | null
    prioridade?: ListaEsperaPrioridade
    preferenciaPeriodo?: ListaEsperaPreferenciaPeriodo
}

export interface PaginaListaEspera {
    itens: ListaEsperaItem[]
    total: number
    pagina: number
    tamanhoPagina: number
}

const BASE = "/agendamentos/lista-espera"

export const listaEsperaService = {
    async listar(pagina = 1, tamanho = 50): Promise<PaginaListaEspera> {
        const { data } = await httpClient.get<PaginaListaEspera>(BASE, { params: { pagina, tamanho } })
        return data
    },
    async adicionar(payload: AdicionarListaEsperaPayload): Promise<{ id: number }> {
        const { data } = await httpClient.post<{ id: number }>(BASE, payload)
        return data
    },
    async remover(id: number): Promise<void> {
        await httpClient.delete(`${BASE}/${id}`)
    },
}

export default listaEsperaService
