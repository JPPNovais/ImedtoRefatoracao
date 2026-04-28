import httpClient from "./httpClient"
import { useTenantStore } from "@/stores/tenantStore"

export interface ProfissionalVinculado {
    vinculoId: number
    usuarioId: string
    email: string
    nomeCompleto: string
    status: string
    modeloPermissaoId: number
    modeloPermissaoNome: string
    especialidade?: string | null
    conselho?: string | null
}

export interface ConvitePendente {
    vinculoId: number
    estabelecimentoId: number
    estabelecimentoNome: string
    convidadoPorNome: string | null
    modeloPermissaoNome: string
    criadoEm: string
}

export interface ConvidarProfissionalResponse {
    vinculoId: number
    actionLink?: string | null
}

export const vinculoService = {
    async listarProfissionais(): Promise<ProfissionalVinculado[]> {
        const tenantStore = useTenantStore()
        const id = tenantStore.ativo?.id
        if (!id) return []
        const { data } = await httpClient.get<ProfissionalVinculado[]>(
            `/estabelecimento/${id}/profissionais`,
        )
        return data
    },

    async convidarProfissional(
        email: string,
        modeloPermissaoId: number,
    ): Promise<ConvidarProfissionalResponse> {
        const tenantStore = useTenantStore()
        const id = tenantStore.ativo?.id
        if (!id) throw new Error("Nenhum estabelecimento ativo.")
        const { data } = await httpClient.post<ConvidarProfissionalResponse>(
            `/estabelecimento/${id}/profissionais/convidar`,
            { email, modeloPermissaoId },
        )
        return data
    },

    async listarMeusConvites(): Promise<ConvitePendente[]> {
        const { data } = await httpClient.get<ConvitePendente[]>("/vinculo/convites/me")
        return data
    },

    async aceitarConvite(vinculoId: number): Promise<void> {
        await httpClient.post(`/vinculo/${vinculoId}/aceitar`)
    },

    async inativarVinculo(vinculoId: number): Promise<void> {
        await httpClient.post(`/vinculo/${vinculoId}/inativar`)
    },
}
