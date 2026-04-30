import httpClient from "./httpClient"

export type TipoConsentimento = "TermosUso" | "PoliticaPrivacidade" | "UsoIA"

export interface Consentimento {
    id: number
    tipo: TipoConsentimento
    versao: string
    aceitoEm: string
}

export const lgpdService = {
    /** Retorna lista de consentimentos registrados do usuário logado. */
    async listarConsentimentos(): Promise<Consentimento[]> {
        const { data } = await httpClient.get<Consentimento[]>("/lgpd/consentimentos/meus")
        return data
    },

    /**
     * Exporta todos os dados do usuário como JSON e aciona o download no
     * navegador. Não retorna um valor — o download é disparado internamente.
     */
    async exportarDados(): Promise<void> {
        const { data } = await httpClient.get("/minha-conta/exportar-dados", {
            responseType: "blob",
        })
        const url = URL.createObjectURL(new Blob([data], { type: "application/json" }))
        const link = document.createElement("a")
        link.href = url
        link.download = "meus-dados.json"
        link.click()
        URL.revokeObjectURL(url)
    },

    /**
     * Solicita a exclusão (anonimização LGPD) da conta do usuário logado.
     * Após a conclusão, o chamador deve fazer logout.
     */
    async excluirConta(): Promise<void> {
        await httpClient.delete("/minha-conta")
    },
}
