import httpClient from "./httpClient"
import { useTenantStore } from "@/stores/tenantStore"

export interface ProfissionalVinculado {
    /** null quando a linha representa o Dono (linha sintética sem vínculo formal). */
    vinculoId: number | null
    usuarioId: string
    email: string
    nomeCompleto: string
    status: string
    modeloPermissaoId: number | null
    modeloPermissaoNome: string
    especialidade?: string | null
    conselho?: string | null
    profissao?: string | null
}

export interface ConvitePendente {
    vinculoId: number
    estabelecimentoId: number
    nomeFantasiaEstabelecimento: string
    convidadoPorEmail: string | null
    convidadoPorNome: string | null
    convidadoEm: string
    nomeConvidado: string | null
    telefoneConvidado: string | null
    especialidadeConvidada: string | null
    profissaoConvidadaId: number | null
    profissaoConvidadaNome: string | null
    modeloPermissaoId: number | null
}

export interface ConvidarProfissionalRequest {
    email: string
    modeloPermissaoId: number | null
    nome?: string | null
    telefone?: string | null
    especialidade?: string | null
    profissaoId?: number | null
}

export interface ConvidarProfissionalResponse {
    vinculoId: number
    actionLink?: string | null
}

export const vinculoService = {
    async listarProfissionais(opts?: { incluirInativos?: boolean }): Promise<ProfissionalVinculado[]> {
        const tenantStore = useTenantStore()
        const id = tenantStore.ativo?.id
        if (!id) return []
        const { data } = await httpClient.get<ProfissionalVinculado[]>(
            `/estabelecimento/${id}/profissionais`,
            { params: opts?.incluirInativos ? { incluirInativos: true } : undefined },
        )
        return data
    },

    async convidarProfissional(
        request: ConvidarProfissionalRequest,
    ): Promise<ConvidarProfissionalResponse> {
        const tenantStore = useTenantStore()
        const id = tenantStore.ativo?.id
        if (!id) throw new Error("Nenhum estabelecimento ativo.")
        const { data } = await httpClient.post<ConvidarProfissionalResponse>(
            `/estabelecimento/${id}/profissionais/convidar`,
            request,
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

    async reativarVinculo(vinculoId: number): Promise<void> {
        await httpClient.post(`/vinculo/${vinculoId}/reativar`)
    },

    async reenviarConvite(vinculoId: number): Promise<void> {
        const tenantStore = useTenantStore()
        const id = tenantStore.ativo?.id
        if (!id) throw new Error("Nenhum estabelecimento ativo.")
        await httpClient.post(
            `/estabelecimento/${id}/profissionais/${vinculoId}/reenviar-convite`,
        )
    },
}
