import adminApi from "./adminApi"

export interface ConfigTrialAdminDto {
    id: string
    planoTrialId: string
    planoTrialNome: string
    duracaoTrialDias: number
    trialHabilitado: boolean
    atualizadoEm: string
}

export interface AtualizarConfigTrialPayload {
    planoTrialId: string
    duracaoTrialDias: number
    trialHabilitado: boolean
    motivo: string
}

export const configTrialService = {
    async obter(): Promise<ConfigTrialAdminDto> {
        const { data } = await adminApi.get<ConfigTrialAdminDto>("/config-trial")
        return data
    },

    async atualizar(payload: AtualizarConfigTrialPayload): Promise<void> {
        await adminApi.put("/config-trial", payload)
    },
}
