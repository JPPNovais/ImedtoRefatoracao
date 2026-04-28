import httpClient from "./httpClient"

export interface ProfissionalPerfil {
    usuarioId: string
    conselho: string
    uf: string
    numeroRegistro: string
    especialidade: string | null
    bio: string | null
    fotoUrl: string | null
    criadoEm: string
    atualizadoEm: string | null
}

export interface SalvarProfissionalPayload {
    conselho: string
    uf: string
    numeroRegistro: string
    especialidade?: string
    bio?: string
}

export const profissionalService = {
    async obterMeu(): Promise<ProfissionalPerfil | null> {
        try {
            const { data } = await httpClient.get<ProfissionalPerfil>("/profissional/me")
            return data
        } catch (e: any) {
            if (e?.response?.status === 404) return null
            throw e
        }
    },

    async salvar(payload: SalvarProfissionalPayload, existe: boolean): Promise<void> {
        if (existe) {
            await httpClient.put("/profissional/me", payload)
        } else {
            await httpClient.post("/profissional/me", payload)
        }
    },

    /** Faz upload da foto de perfil; devolve a nova URL pública. */
    async uploadFoto(arquivo: File): Promise<string> {
        const form = new FormData()
        form.append("arquivo", arquivo)
        const { data } = await httpClient.put<{ fotoUrl: string }>(
            "/profissional/me/foto",
            form,
            { headers: { "Content-Type": "multipart/form-data" } },
        )
        return data.fotoUrl
    },
}
