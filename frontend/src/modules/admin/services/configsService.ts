import adminApi from "./adminApi"

export interface ConfigAdminDto {
    chave: string
    valor: string
    tipo: "numerico" | "texto" | "email" | "toggle"
    secao: string | null
    descricao: string | null
    atualizadoEm: string
    atualizadoPorAdminId: string | null
}

export interface SecaoConfigsDto {
    secao: string
    configs: ConfigAdminDto[]
}

export const configsService = {
    async listar(): Promise<SecaoConfigsDto[]> {
        const { data } = await adminApi.get<SecaoConfigsDto[]>("/configs")
        return data
    },

    async atualizar(chave: string, valor: string, motivo: string): Promise<void> {
        await adminApi.put(`/configs/${encodeURIComponent(chave)}`, { valor, motivo })
    },
}
