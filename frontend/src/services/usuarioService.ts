import httpClient from "./httpClient"

export interface VerificarCpfResult {
    valido: boolean
    disponivel: boolean
    motivo: string | null
}

export const usuarioService = {
    async completarOnboarding(payload: {
        nomeCompleto: string
        cpf: string
        telefone?: string
    }): Promise<void> {
        await httpClient.post("/usuario/me/onboarding", payload)
    },

    async atualizarPerfil(payload: {
        nomeCompleto: string
        telefone?: string
    }): Promise<void> {
        await httpClient.patch("/usuario/me", payload)
    },

    /** Valida formato + duplicidade do CPF informado para o usuário corrente. */
    async verificarCpfDisponivel(cpf: string): Promise<VerificarCpfResult> {
        const { data } = await httpClient.get<VerificarCpfResult>("/usuario/me/cpf-disponivel", {
            params: { cpf },
        })
        return data
    },
}
