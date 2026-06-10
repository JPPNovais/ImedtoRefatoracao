import httpClient from "./httpClient"

export interface Status2faDto {
    ativo: boolean
}

export interface Iniciar2faAtivacaoDto {
    otpauthUri: string
    segredoBase32: string
}

export interface Confirmar2faAtivacaoDto {
    codigosRecuperacao: string[]
}

export const auth2faService = {
    async obterStatus(): Promise<Status2faDto> {
        const { data } = await httpClient.get<Status2faDto>("/auth/2fa/status")
        return data
    },

    async iniciarAtivacao(): Promise<Iniciar2faAtivacaoDto> {
        const { data } = await httpClient.post<Iniciar2faAtivacaoDto>("/auth/2fa/iniciar-ativacao")
        return data
    },

    async confirmarAtivacao(codigo: string): Promise<Confirmar2faAtivacaoDto> {
        const { data } = await httpClient.post<Confirmar2faAtivacaoDto>("/auth/2fa/confirmar-ativacao", { codigo })
        return data
    },

    async desativar(senha: string, codigo: string): Promise<void> {
        await httpClient.post("/auth/2fa/desativar", { senha, codigo })
    },

    /** Segundo passo do login: troca desafioToken + código TOTP por cookies de sessão. */
    async confirmarLogin(desafioToken: string, codigo: string): Promise<void> {
        await httpClient.post("/auth/login/2fa", { desafioToken, codigo })
    },

    async atualizarExigirDono2fa(estabelecimentoId: number, exigir: boolean): Promise<void> {
        await httpClient.put(`/estabelecimento/${estabelecimentoId}/seguranca/exigir-dono-2fa`, { exigir })
    },
}
