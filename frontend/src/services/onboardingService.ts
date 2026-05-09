import httpClient from "./httpClient"

export interface FinalizarOnboardingPayload {
    nomeCompleto: string
    cpf: string
    telefone?: string
    estabelecimento?: {
        nomeFantasia: string
        cnpj?: string
        telefone?: string
        endereco?: string
    }
    profissional?: {
        conselho: string
        uf: string
        numeroRegistro: string
        especialidade?: string
        profissaoId?: number | null
        especialidades?: string[]
    }
    funcionamento?: {
        horarioInicio: string
        horarioFim: string
        duracaoConsultaPadraoMinutos: number
        intervaloEntreConsultasMinutos: number
        diasSemana: number[]
    }
}

export const onboardingService = {
    async finalizar(payload: FinalizarOnboardingPayload): Promise<void> {
        await httpClient.post("/onboarding/finalizar", payload)
    },
}
