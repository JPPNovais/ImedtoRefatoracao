import httpClient from "./httpClient"

// ── DTOs ──────────────────────────────────────────────────────────────────────

export interface ConvenioPlanoDto {
    id: number
    nome: string
    ativo: boolean
}

export interface ConvenioListado {
    id: number
    nome: string
    registroAns: string | null
    ativo: boolean
    totalPlanos: number
}

export interface ConvenioDetalhe {
    id: number
    nome: string
    registroAns: string | null
    ativo: boolean
    planos: ConvenioPlanoDto[]
}

/** Usado nos selects de check-in e carteirinha. */
export interface ConvenioSelect {
    id: number
    nome: string
    planos: ConvenioPlanoDto[]
}

export interface PacienteConvenioDto {
    id: number
    convenioId: number
    convenioNome: string
    planoId: number | null
    planoNome: string | null
    numeroCarteirinha: string
    validade: string | null
    ativo: boolean
}

/** Calcula em runtime se a carteirinha está vencida (R6 — front é a fonte da verdade). */
export function estaVencida(validade: string | null): boolean {
    return !!validade && new Date(validade) < new Date()
}

/** Carteirinha resumida para pré-seleção no check-in. */
export interface CarteirinhaCheckIn {
    carteirinhaId: number
    convenioId: number
    convenioNome: string
    planoId: number | null
    planoNome: string | null
    numeroCarteirinha: string
    validade: string | null
}

// ── Service ───────────────────────────────────────────────────────────────────

export const convenioService = {
    // ── Convênios (CRUD) ──────────────────────────────────────────────────────

    async listar(apenasAtivos = false): Promise<ConvenioListado[]> {
        const { data } = await httpClient.get<ConvenioListado[]>("/convenios", {
            params: apenasAtivos ? { apenasAtivos: true } : undefined,
        })
        return data
    },

    async obter(id: number): Promise<ConvenioDetalhe | null> {
        try {
            const { data } = await httpClient.get<ConvenioDetalhe>(`/convenios/${id}`)
            return data
        } catch (err: any) {
            if (err?.response?.status === 404) return null
            throw err
        }
    },

    /** Endpoint rápido (Dapper) para selects de check-in e carteirinha. */
    async listarAtivos(): Promise<ConvenioSelect[]> {
        const { data } = await httpClient.get<ConvenioSelect[]>("/convenios/ativos")
        return data
    },

    async criar(nome: string, registroAns: string | null): Promise<void> {
        await httpClient.post("/convenios", { nome, registroAns })
    },

    async atualizar(id: number, nome: string, registroAns: string | null, ativo: boolean): Promise<void> {
        await httpClient.put(`/convenios/${id}`, { nome, registroAns, ativo })
    },

    async excluir(id: number): Promise<void> {
        await httpClient.delete(`/convenios/${id}`)
    },

    // ── Planos (via convênio) ─────────────────────────────────────────────────

    async adicionarPlano(convenioId: number, nome: string): Promise<void> {
        await httpClient.post(`/convenios/${convenioId}/planos`, { nome })
    },

    async atualizarPlano(convenioId: number, planoId: number, nome: string): Promise<void> {
        await httpClient.put(`/convenios/${convenioId}/planos/${planoId}`, { nome })
    },

    async inativarPlano(convenioId: number, planoId: number): Promise<void> {
        await httpClient.delete(`/convenios/${convenioId}/planos/${planoId}`)
    },

    // ── Carteirinhas do paciente ──────────────────────────────────────────────

    async listarCarteirinhasPaciente(pacienteId: number): Promise<PacienteConvenioDto[]> {
        const { data } = await httpClient.get<PacienteConvenioDto[]>(
            `/pacientes/${pacienteId}/convenios`,
        )
        return data
    },

    async criarCarteirinha(
        pacienteId: number,
        payload: {
            convenioId: number
            planoId: number | null
            numeroCarteirinha: string
            validade: string | null
        },
    ): Promise<void> {
        await httpClient.post(`/pacientes/${pacienteId}/convenios`, payload)
    },

    async atualizarCarteirinha(
        pacienteId: number,
        carteirinhaId: number,
        payload: {
            convenioId: number
            planoId: number | null
            numeroCarteirinha: string
            validade: string | null
            ativo: boolean
        },
    ): Promise<void> {
        await httpClient.put(`/pacientes/${pacienteId}/convenios/${carteirinhaId}`, payload)
    },

    async excluirCarteirinha(pacienteId: number, carteirinhaId: number): Promise<void> {
        await httpClient.delete(`/pacientes/${pacienteId}/convenios/${carteirinhaId}`)
    },

    /** CA143: carteirinhas ativas para pré-seleção no check-in (sem audit). */
    async listarCarteirinhasCheckIn(pacienteId: number): Promise<CarteirinhaCheckIn[]> {
        const { data } = await httpClient.get<CarteirinhaCheckIn[]>(
            `/pacientes/${pacienteId}/convenios/check-in`,
        )
        return data
    },

    // ── Guia/autorização da cobrança (F6/R10/R13) ────────────────────────────

    async registrarGuia(
        cobrancaId: number,
        payload: {
            guiaNumero: string
            guiaSenha: string | null
            guiaAutorizadaEm: string | null
        },
    ): Promise<void> {
        await httpClient.post(`/cobrancas/${cobrancaId}/guia`, payload)
    },
}
