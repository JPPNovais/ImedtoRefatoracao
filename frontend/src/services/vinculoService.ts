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
    fotoUrl?: string | null
}

/**
 * Profissional em formato público/minimizado — só nome, especialidade e
 * conselho. SEM e-mail, SEM modelo de permissão, SEM datas. Usado nos
 * seletores (agenda/prontuário/orçamento) onde qualquer membro do tenant
 * precisa enxergar "com quem agenda", sem ganhar acesso a PII da equipe.
 */
export interface ProfissionalPublico {
    usuarioId: string
    nomeCompleto: string
    especialidade?: string | null
    conselho?: string | null
    /** "Ativo" ou "Dono". Inativos não aparecem aqui. */
    status: string
    /** URL presigned (S3) da foto do profissional, quando houver. */
    fotoUrl?: string | null
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
    /**
     * Listagem COMPLETA da equipe — retorna e-mail, modelo de permissão,
     * datas e status (inclusive Inativo/Convidado). Endpoint é restrito a
     * Dono ou perfis com permissão "equipe.ver" — chamadas a partir de
     * perfis sem permissão devolvem 422 do backend.
     *
     * Use APENAS na tela de gestão de Equipe (/equipe). Para seletores em
     * agenda/prontuário/orçamento, prefira <see cref="listarProfissionaisPublico"/>.
     */
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

    /**
     * Listagem PÚBLICA/MINIMIZADA — só nome, especialidade, conselho e status.
     * Disponível a qualquer membro ativo do tenant (gate via [RequiresEstabelecimento]
     * no backend). Use em seletores onde a UX só precisa de "com quem agenda?".
     *
     * Esta variação foi criada para fechar um vazamento de PII que existia
     * antes (qualquer Médico ou Recepção via a lista completa com e-mails).
     */
    async listarProfissionaisPublico(): Promise<ProfissionalPublico[]> {
        const tenantStore = useTenantStore()
        const id = tenantStore.ativo?.id
        if (!id) return []
        const { data } = await httpClient.get<ProfissionalPublico[]>(
            `/estabelecimento/${id}/profissionais/publico`,
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
