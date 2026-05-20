import httpClient from "./httpClient"

export const lgpdService = {
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
     * Exige reautenticação por senha — o backend valida antes de executar
     * (anonimização é irreversível, proteger contra cookie roubado).
     * Após a conclusão, o chamador deve fazer logout.
     */
    async excluirConta(password: string): Promise<void> {
        await httpClient.delete("/minha-conta", { data: { password } })
    },
}
