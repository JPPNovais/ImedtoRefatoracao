import httpClient from "./httpClient"

export type StatusAssinatura = "Trial" | "Ativa" | "Suspensa" | "Cancelada" | "Expirada"

export interface Plano {
    id: number
    nome: string
    precoMensal: number
    limiteProfissionais: number | null
    limitePacientes: number | null
    featuresJson: string[]
    ordem: number
}

export interface MinhaAssinatura {
    planoNome: string
    status: StatusAssinatura
    iniciadaEm: string
    expiraEm: string | null
    diasRestantes: number | null
    features: string[]
    limiteProfissionais: number | null
    limitePacientes: number | null
}

export const assinaturaService = {
    async obterMinha(): Promise<MinhaAssinatura> {
        const { data } = await httpClient.get<MinhaAssinatura>("/minha-assinatura")
        return data
    },

    async listarPlanos(): Promise<Plano[]> {
        const { data } = await httpClient.get<Plano[]>("/planos")
        return data
    },
}

export default assinaturaService
