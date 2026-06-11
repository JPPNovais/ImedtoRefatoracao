import adminApi from "./adminApi"

export interface AssinaturaAdminDto {
    id: string
    estabelecimentoId: number
    planoId: string
    planoNome: string
    planoGratuito: boolean
    iniciadaEm: string
    fimEm: string | null
    expiraEm: string | null
    suspensaEm: string | null
    gratuita: boolean
    motivo: string | null
    criadaEm: string
    vigente: boolean
    /** Estado derivado: Vitalicia | Temporaria | Suspensa | Expirada | Encerrada */
    estado: string
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
        payload: { planoId: string; inicio: string; fimEm?: string | null; motivo: string },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/trocar-plano`, payload)
    },

    async concederGratuidade(
        estabelecimentoId: number,
        payload: { gratuidadeMotivo: string; fimEm?: string | null; motivo: string },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/gratuidade`, payload)
    },

    async encerrar(assinaturaId: string, motivo: string): Promise<void> {
        await adminApi.post(`/assinaturas/${assinaturaId}/encerrar`, { motivo })
    },

    // --- F4: novos endpoints de estado ---

    async liberarVitalicio(
        estabelecimentoId: number,
        payload: { planoId: string; motivo: string },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/liberar-vitalicio`, payload)
    },

    async liberarAteData(
        estabelecimentoId: number,
        payload: { planoId: string; dataExpiracao: string; motivo: string },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/liberar-ate-data`, payload)
    },

    async iniciarTrial(
        estabelecimentoId: number,
        payload: { planoId: string; dias: number; motivo: string },
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/iniciar-trial`, payload)
    },

    async suspender(
        estabelecimentoId: number,
        motivo: string,
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/suspender`, { motivo })
    },

    async reativar(
        estabelecimentoId: number,
        motivo: string,
    ): Promise<void> {
        await adminApi.post(`/estabelecimentos/${estabelecimentoId}/assinaturas/reativar`, { motivo })
    },
}
