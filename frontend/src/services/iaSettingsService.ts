import httpClient from "./httpClient"

export interface IaSettings {
    aiEnabled: boolean
    aiProvider: string
    aiModel: string
    rateLimitPerMinute: number
    rateLimitPerDay: number
    dataMinimizationLevel: "standard" | "minimized"
}

export const iaSettingsService = {
    async obter(): Promise<IaSettings> {
        const { data } = await httpClient.get<IaSettings>("/estabelecimento/ia-settings")
        return data
    },

    async salvar(settings: IaSettings): Promise<void> {
        await httpClient.put("/estabelecimento/ia-settings", settings)
    },
}

export default iaSettingsService
