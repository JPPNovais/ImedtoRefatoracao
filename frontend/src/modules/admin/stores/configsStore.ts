import { defineStore } from "pinia"
import { ref } from "vue"
import { configsService, type SecaoConfigsDto } from "../services/configsService"

export const useConfigsStore = defineStore("adminConfigs", () => {
    const secoes = ref<SecaoConfigsDto[]>([])
    const carregando = ref(false)
    const erro = ref<string | null>(null)

    async function carregar(): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            secoes.value = await configsService.listar()
        } catch {
            erro.value = "Não foi possível carregar as configurações."
        } finally {
            carregando.value = false
        }
    }

    async function atualizar(chave: string, valor: string, motivo: string): Promise<void> {
        await configsService.atualizar(chave, valor, motivo)
        await carregar()
    }

    return { secoes, carregando, erro, carregar, atualizar }
})
