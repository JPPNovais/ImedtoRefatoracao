import { defineStore } from "pinia"
import { ref } from "vue"
import {
    modelosGlobaisService,
    type ModeloGlobalListaItemDto,
    type ModeloGlobalDetalheDto,
} from "../services/catalogosService"

export const useModelosGlobaisStore = defineStore("adminModelosGlobais", () => {
    const lista = ref<ModeloGlobalListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(20)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const itemAtual = ref<ModeloGlobalDetalheDto | null>(null)

    async function carregar(filtros: {
        incluirInativos?: boolean
        busca?: string
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result = await modelosGlobaisService.listar({
                incluirInativos: filtros.incluirInativos ?? false,
                busca: filtros.busca || undefined,
                pagina: filtros.page ?? pagina.value,
                tamanhoPagina: filtros.size ?? tamanho.value,
            })
            lista.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanhoPagina
        } catch {
            erro.value = "Não foi possível carregar os modelos de prontuário."
        } finally {
            carregando.value = false
        }
    }

    async function carregarItem(id: string): Promise<void> {
        carregando.value = true
        erro.value = null
        itemAtual.value = null
        try {
            itemAtual.value = await modelosGlobaisService.obter(id)
        } catch {
            erro.value = "Modelo não encontrado."
        } finally {
            carregando.value = false
        }
    }

    async function criar(payload: { nome: string; descricao: string | null; conteudoJson: string; motivo: string }): Promise<string> {
        const { id } = await modelosGlobaisService.criar(payload)
        return id
    }

    async function atualizar(id: string, payload: { nome: string; descricao: string | null; conteudoJson: string; motivo: string }): Promise<void> {
        await modelosGlobaisService.atualizar(id, payload)
    }

    async function desativar(id: string, motivo: string): Promise<void> {
        await modelosGlobaisService.desativar(id, motivo)
        const item = lista.value.find((m) => m.id === id)
        if (item) item.ativo = false
    }

    async function reativar(id: string, motivo: string): Promise<void> {
        await modelosGlobaisService.reativar(id, motivo)
        const item = lista.value.find((m) => m.id === id)
        if (item) item.ativo = true
    }

    return { lista, total, pagina, tamanho, carregando, erro, itemAtual, carregar, carregarItem, criar, atualizar, desativar, reativar }
})
