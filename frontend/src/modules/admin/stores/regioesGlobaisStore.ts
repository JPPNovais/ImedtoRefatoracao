import { defineStore } from "pinia"
import { ref } from "vue"
import {
    regioesGlobaisService,
    type RegiaoGlobalListaItemDto,
} from "../services/catalogosService"

export const useRegioesGlobaisStore = defineStore("adminRegioesGlobais", () => {
    const lista = ref<RegiaoGlobalListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(20)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const itemAtual = ref<RegiaoGlobalListaItemDto | null>(null)

    async function carregar(filtros: {
        incluirInativos?: boolean
        busca?: string
        sistemaCorporal?: string
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result = await regioesGlobaisService.listar({
                incluirInativos: filtros.incluirInativos ?? false,
                busca: filtros.busca || undefined,
                sistemaCorporal: filtros.sistemaCorporal || undefined,
                pagina: filtros.page ?? pagina.value,
                tamanhoPagina: filtros.size ?? tamanho.value,
            })
            lista.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanhoPagina
        } catch {
            erro.value = "Não foi possível carregar as regiões anatômicas."
        } finally {
            carregando.value = false
        }
    }

    async function carregarItem(id: string): Promise<void> {
        carregando.value = true
        erro.value = null
        itemAtual.value = null
        try {
            itemAtual.value = await regioesGlobaisService.obter(id)
        } catch {
            erro.value = "Região não encontrada."
        } finally {
            carregando.value = false
        }
    }

    async function criar(payload: { nome: string; sinonimos: string[] | null; sistemaCorporal: string | null; motivo: string }): Promise<string> {
        const { id } = await regioesGlobaisService.criar(payload)
        return id
    }

    async function atualizar(id: string, payload: { nome: string; sinonimos: string[] | null; sistemaCorporal: string | null; motivo: string }): Promise<void> {
        await regioesGlobaisService.atualizar(id, payload)
    }

    async function desativar(id: string, motivo: string): Promise<void> {
        await regioesGlobaisService.desativar(id, motivo)
        const item = lista.value.find((r) => r.id === id)
        if (item) item.ativo = false
    }

    async function reativar(id: string, motivo: string): Promise<void> {
        await regioesGlobaisService.reativar(id, motivo)
        const item = lista.value.find((r) => r.id === id)
        if (item) item.ativo = true
    }

    return { lista, total, pagina, tamanho, carregando, erro, itemAtual, carregar, carregarItem, criar, atualizar, desativar, reativar }
})
