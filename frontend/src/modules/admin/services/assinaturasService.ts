import adminApi from "./adminApi"

export interface AssinaturaAdminDto {
    id: string
    estabelecimentoId: number
    planoId: string
    planoNome: string
    planoGratuito: boolean
    iniciadaEm: string
    fimEm: string | null
    gratuita: boolean
    motivo: string | null
    criadaEm: string
    vigente: boolean
}

export const assinaturasService = {
    async listarHistorico(estabelecimentoId: number): Promise<AssinaturaAdminDto[]> {
        const { data } = await adminApi.get<AssinaturaAdminDto[]>(
            `/estabelecimentos/${estabelecimentoId}/assinaturas`,
        )
        return data
    },

    async trocarPlano(
        estabelecimentoId: number,
        payload: {
            planoId: string
            inicio: string
            fimEm?: string | null
            motivo: string
        },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/trocar-plano`, payload)
    },

    async concederGratuidade(
        estabelecimentoId: number,
        payload: {
            gratuidadeMotivo: string
            fimEm?: string | null
            motivo: string
        },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/gratuidade`, payload)
    },

    async encerrar(assinaturaId: string, motivo: string): Promise<void> {
        await adminApi.post(`/assinaturas/${assinaturaId}/encerrar`, { motivo })
    },
}
