import { defineStore } from "pinia"
import { ref } from "vue"
import {
    variaveisGlobaisService,
    type VariavelPadraoSistemaListaItemDto,
    type VariavelPadraoSistemaDetalheDto,
} from "../services/catalogosService"

export const useVariaveisGlobaisStore = defineStore("adminVariaveisGlobais", () => {
    const lista = ref<VariavelPadraoSistemaListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(20)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const itemAtual = ref<VariavelPadraoSistemaDetalheDto | null>(null)

    async function carregar(filtros: {
        incluirInativos?: boolean
        busca?: string
        categoria?: string
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result = await variaveisGlobaisService.listar({
                incluirInativos: filtros.incluirInativos ?? false,
                busca: filtros.busca || undefined,
                categoria: filtros.categoria || undefined,
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

    async function carregarItem(id: number): Promise<void> {
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

    async function criar(payload: { nome: string; tipo: string; motivo: string }): Promise<number> {
        const { id } = await variaveisGlobaisService.criar(payload)
        return id
    }

    async function atualizar(id: number, payload: { nome: string; tipo: string; motivo: string }): Promise<void> {
        await variaveisGlobaisService.atualizar(id, payload)
    }

    async function inativar(id: number, motivo: string): Promise<void> {
        await variaveisGlobaisService.inativar(id, motivo)
        const item = lista.value.find((v) => v.id === id)
        if (item) item.ativo = false
    }

    async function reativar(id: number, motivo: string): Promise<void> {
        await variaveisGlobaisService.reativar(id, motivo)
        const item = lista.value.find((v) => v.id === id)
        if (item) item.ativo = true
    }

    return { lista, total, pagina, tamanho, carregando, erro, itemAtual, carregar, carregarItem, criar, atualizar, inativar, reativar }
})
