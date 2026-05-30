import { defineStore } from "pinia"
import { ref } from "vue"
import {
    variaveisGlobaisService,
    type VariavelGlobalListaItemDto,
    type VariavelGlobalDetalheDto,
} from "../services/catalogosService"

export const useVariaveisGlobaisStore = defineStore("adminVariaveisGlobais", () => {
    const lista = ref<VariavelGlobalListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(20)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const itemAtual = ref<VariavelGlobalDetalheDto | null>(null)

    async function carregar(filtros: {
        incluirInativos?: boolean
        busca?: string
        tipo?: string
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result = await variaveisGlobaisService.listar({
                incluirInativos: filtros.incluirInativos ?? false,
                busca: filtros.busca || undefined,
                tipo: filtros.tipo || undefined,
                pagina: filtros.page ?? pagina.value,
                tamanhoPagina: filtros.size ?? tamanho.value,
            })
            lista.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanhoPagina
        } catch {
            erro.value = "Não foi possível carregar as variáveis pool."
        } finally {
            carregando.value = false
        }
    }

    async function carregarItem(id: string): Promise<void> {
        carregando.value = true
        erro.value = null
        itemAtual.value = null
        try {
            itemAtual.value = await variaveisGlobaisService.obter(id)
        } catch {
            erro.value = "Variável não encontrada."
        } finally {
            carregando.value = false
        }
    }

    async function criar(payload: { nome: string; tipo: string; valoresJson: string | null; descricao: string | null; motivo: string }): Promise<string> {
        const { id } = await variaveisGlobaisService.criar(payload)
        return id
    }

    async function atualizar(id: string, payload: { nome: string; tipo: string; valoresJson: string | null; descricao: string | null; motivo: string }): Promise<void> {
        await variaveisGlobaisService.atualizar(id, payload)
    }

    async function desativar(id: string, motivo: string): Promise<void> {
        await variaveisGlobaisService.desativar(id, motivo)
        const item = lista.value.find((v) => v.id === id)
        if (item) item.ativo = false
    }

    async function reativar(id: string, motivo: string): Promise<void> {
        await variaveisGlobaisService.reativar(id, motivo)
        const item = lista.value.find((v) => v.id === id)
        if (item) item.ativo = true
    }

    return { lista, total, pagina, tamanho, carregando, erro, itemAtual, carregar, carregarItem, criar, atualizar, desativar, reativar }
})
