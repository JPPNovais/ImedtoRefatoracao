import httpClient from "./httpClient"

export interface PacienteListaItem {
    id: number
    nomeCompleto: string
    cpf: string | null
    documentoInternacional: string | null
    dataNascimento: string | null
    telefone: string | null
    criadoEm: string
    /** Tags clínicas/operacionais (chaves curtas como "vip", "gestante"). */
    tags: string[]
    /** Quantidade de alertas clínicos do paciente — usada como badge na lista. */
    qtdAlertas: number
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
    documentoInternacional: string | null
    dataNascimento: string | null
    genero: string
    telefone: string | null
    email: string | null
    endereco: string | null
    observacoes: string | null
    /** Tags clínicas/operacionais — chaves curtas (ex: "vip", "gestante"). */
    tags: string[]
    /** Alertas clínicos críticos exibidos em destaque no detalhe do paciente. */
    alertas: string[]
    criadoEm: string
    atualizadoEm: string | null
}

export interface PacientePayload {
    nomeCompleto: string
    cpf?: string
    documentoInternacional?: string
    dataNascimento?: string
    genero?: string
    telefone?: string
    email?: string
    endereco?: string
    observacoes?: string
    tags?: string[]
    alertas?: string[]
}

export interface PacienteStats {
    total: number
    novosMesCorrente: number
}

/**
 * Item mínimo retornado pelo endpoint de autocomplete `/api/paciente/busca-rapida`.
 * Sem PII além do nome (LGPD: minimização — o seletor só exibe nome).
 */
export interface PacienteBuscaRapida {
    id: number
    nomeCompleto: string
}

export const pacienteService = {
    /**
     * Autocomplete leve de paciente — apenas {id, nomeCompleto}, sem CPF/telefone.
     * Sem `q` retorna os últimos cadastrados (até `limite`); com `q` busca por nome.
     * Use para seletores; para a listagem completa, use `listar()`.
     */
    async buscaRapida(q?: string, limite = 10): Promise<PacienteBuscaRapida[]> {
        const { data } = await httpClient.get<PacienteBuscaRapida[]>("/paciente/busca-rapida", {
            params: { q: q || undefined, limite },
        })
        return data
    },

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

    async stats(): Promise<PacienteStats> {
        const { data } = await httpClient.get<PacienteStats>("/paciente/stats")
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
