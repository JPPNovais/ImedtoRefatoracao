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
        const r = await httpClient.get<ProfissionalPerfil | "">("/profissional/me")
        // Backend devolve 204 quando o usuário ainda não tem perfil profissional.
        // Não é erro — é um estado válido (ex: durante o onboarding).
        if (r.status === 204) return null
        return r.data as ProfissionalPerfil
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
