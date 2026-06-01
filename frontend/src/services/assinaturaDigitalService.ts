import httpClient from "./httpClient"

/**
 * Serviço de assinatura digital ICP-Brasil de receitas.
 * Backend é fonte da verdade — toda validação vive no aggregate/handler.
 * Frontend exibe estado atual e dispara ações; confirmação chega via polling.
 */

export type StatusAssinaturaDigital =
    | "NaoAssinada"
    | "AssinaturaPendente"
    | "AssinadaIcp"
    | "FalhaAssinatura"
    | "AssinaturaExpirada"
    | "AssinadaMemed"

export interface StatusAssinaturaResponse {
    status: StatusAssinaturaDigital
    pdfAssinadoUrl: string | null
}

export interface CertificadoVinculado {
    provedor: string
    expiraEm: string | null
}

export interface VincularCertificadoInput {
    provedor: string
    refreshToken: string
    expiraEm?: string | null
}

export const assinaturaDigitalService = {
    async dispararAssinatura(receitaId: number): Promise<void> {
        await httpClient.post(`/receitas/${receitaId}/assinar`)
    },

    async obterStatus(receitaId: number): Promise<StatusAssinaturaResponse> {
        const { data } = await httpClient.get<StatusAssinaturaResponse>(
            `/receitas/${receitaId}/status-assinatura`,
        )
        return data
    },

    async vincularCertificado(input: VincularCertificadoInput): Promise<void> {
        await httpClient.post("/medico/certificado/vincular", input)
    },

    async removerCertificado(): Promise<void> {
        await httpClient.delete("/medico/certificado")
    },

    async obterCertificadoVinculado(): Promise<CertificadoVinculado | null> {
        const { data } = await httpClient.get<CertificadoVinculado | null>("/medico/certificado")
        return data ?? null
    },
}

export default assinaturaDigitalService
