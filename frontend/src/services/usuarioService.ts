import httpClient from "./httpClient"

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
}
