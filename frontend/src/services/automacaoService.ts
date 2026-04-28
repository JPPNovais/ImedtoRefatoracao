import httpClient from "./httpClient"

export interface ConfiguracaoAutomacao {
    lembretesHabilitados: boolean
    horasAntecedenciaLembrete: number
    expiracaoOrcamentosHabilitada: boolean
    emailRemetente: string | null
}

export const automacaoService = {
    async obterConfiguracao(): Promise<ConfiguracaoAutomacao> {
        const { data } = await httpClient.get<ConfiguracaoAutomacao>("/automacoes/configuracao")
        return data
    },

    async salvarConfiguracao(config: ConfiguracaoAutomacao): Promise<void> {
        await httpClient.put("/automacoes/configuracao", config)
    },

    async expirarOrcamentos(): Promise<void> {
        await httpClient.post("/automacoes/expirar-orcamentos")
    },

    async enviarLembretes(): Promise<void> {
        await httpClient.post("/automacoes/enviar-lembretes")
    },
}
