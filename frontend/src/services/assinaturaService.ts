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

// Shape vinda do backend (AssinaturaDto + PlanoDto). Não exportada — o service
// faz o flatten para o contrato consumido pelas views/stores.
interface AssinaturaResponse {
    plano: {
        id: number
        nome: string
        precoMensal: number
        limiteProfissionais: number | null
        limitePacientes: number | null
        features: string[]
        ordem: number
    }
    status: StatusAssinatura
    iniciadaEm: string
    expiraEm: string | null
    diasRestantes: number | null
}

interface PlanoResponse {
    id: number
    nome: string
    precoMensal: number
    limiteProfissionais: number | null
    limitePacientes: number | null
    features: string[]
    ordem: number
}

export const assinaturaService = {
    async obterMinha(): Promise<MinhaAssinatura> {
        const { data } = await httpClient.get<AssinaturaResponse>("/minha-assinatura")
        return {
            planoNome: data?.plano?.nome ?? "",
            status: data?.status,
            iniciadaEm: data?.iniciadaEm,
            expiraEm: data?.expiraEm ?? null,
            diasRestantes: data?.diasRestantes ?? null,
            features: data?.plano?.features ?? [],
            limiteProfissionais: data?.plano?.limiteProfissionais ?? null,
            limitePacientes: data?.plano?.limitePacientes ?? null,
        }
    },

    async listarPlanos(): Promise<Plano[]> {
        const { data } = await httpClient.get<PlanoResponse[]>("/planos")
        return (data ?? []).map((p) => ({
            id: p.id,
            nome: p.nome,
            precoMensal: p.precoMensal,
            limiteProfissionais: p.limiteProfissionais ?? null,
            limitePacientes: p.limitePacientes ?? null,
            featuresJson: p.features ?? [],
            ordem: p.ordem,
        }))
    },
}

export default assinaturaService
