import { defineStore } from "pinia"
import { ref } from "vue"
import { adminsService, type AdminListItem, type AdminCriadoResult } from "../services/adminsService"

/**
 * Store de CRUD de admins adicionais.
 *
 * Mantém a lista paginada e expõe ações que delegam ao adminsService.
 */
export const useAdminsStore = defineStore("admins", () => {
    const itens = ref<AdminListItem[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(25)
    const carregando = ref(false)
    const erro = ref<string | null>(null)

    // Resultado do último admin criado (senha temporária exibida uma vez)
    const ultimoCriado = ref<AdminCriadoResult | null>(null)
    // Senha temporária de reset (exibida uma vez)
    const ultimaSenhaResetada = ref<string | null>(null)

    async function carregar(busca?: string) {
        carregando.value = true
        erro.value = null
        try {
            const result = await adminsService.listar({
                busca: busca || undefined,
                pagina: pagina.value,
                tamanho: tamanho.value,
            })
            itens.value = result.itens
            total.value = result.total
        } catch (e: any) {
            erro.value = e?.response?.data?.mensagem ?? "Não foi possível carregar os administradores."
        } finally {
            carregando.value = false
        }
    }

    async function criar(nome: string, email: string, motivo: string): Promise<AdminCriadoResult> {
        const result = await adminsService.criar({ nome, email, motivo })
        ultimoCriado.value = result
        return result
    }

    async function desativar(id: string, motivo: string): Promise<void> {
        await adminsService.desativar(id, motivo)
    }

    async function reativar(id: string, motivo: string): Promise<void> {
        await adminsService.reativar(id, motivo)
    }

    async function resetarSenha(id: string, motivo: string): Promise<string> {
        const result = await adminsService.resetarSenha(id, motivo)
        ultimaSenhaResetada.value = result.senhaTemporaria
        return result.senhaTemporaria
    }

    function limparUltimoCriado() {
        ultimoCriado.value = null
    }

    function limparUltimaSenhaResetada() {
        ultimaSenhaResetada.value = null
    }

    return {
        itens,
        total,
        pagina,
        tamanho,
        carregando,
        erro,
        ultimoCriado,
        ultimaSenhaResetada,
        carregar,
        criar,
        desativar,
        reativar,
        resetarSenha,
        limparUltimoCriado,
        limparUltimaSenhaResetada,
    }
})
