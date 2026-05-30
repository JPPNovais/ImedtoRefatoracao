import adminApi from "./adminApi"

export interface AdminListItem {
    id: string
    email: string
    nome: string
    ativo: boolean
    forcePasswordReset: boolean
    criadoEm: string
    ultimoLoginEm: string | null
}

export interface AdminDetalhe extends AdminListItem {
    desativadoEm: string | null
    desativadoPor: string | null
}

export interface ListarAdminsResult {
    itens: AdminListItem[]
    total: number
    pagina: number
    tamanho: number
}

export interface AdminCriadoResult {
    id: string
    email: string
    nome: string
    senhaTemporaria: string
}

export interface SenhaResetadaResult {
    senhaTemporaria: string
}

export const adminsService = {
    async listar(params: { busca?: string; pagina?: number; tamanho?: number }): Promise<ListarAdminsResult> {
        const { data } = await adminApi.get<ListarAdminsResult>("/admins", { params })
        return data
    },

    async obter(id: string): Promise<AdminDetalhe> {
        const { data } = await adminApi.get<AdminDetalhe>(`/admins/${id}`)
        return data
    },

    async criar(payload: { nome: string; email: string; motivo: string }): Promise<AdminCriadoResult> {
        const { data } = await adminApi.post<AdminCriadoResult>("/admins", payload)
        return data
    },

    async desativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/admins/${id}/desativar`, { motivo })
    },

    async reativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/admins/${id}/reativar`, { motivo })
    },

    async resetarSenha(id: string, motivo: string): Promise<SenhaResetadaResult> {
        const { data } = await adminApi.post<SenhaResetadaResult>(`/admins/${id}/reset-senha`, { motivo })
        return data
    },
}
