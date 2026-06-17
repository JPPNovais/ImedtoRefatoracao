import { defineStore } from "pinia"
import { ref } from "vue"
import {
    permissoesGlobaisService,
    type ModeloPermissaoPadraoListaItemDto,
    type ModeloPermissaoPadraoDetalheDto,
} from "../services/catalogosService"

export const usePermissoesGlobaisStore = defineStore("adminPermissoesGlobais", () => {
    const lista = ref<ModeloPermissaoPadraoListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(10)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const itemAtual = ref<ModeloPermissaoPadraoDetalheDto | null>(null)

    async function carregar(filtros: { busca?: string; page?: number; size?: number } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result = await permissoesGlobaisService.listar({
                busca: filtros.busca || undefined,
                pagina: filtros.page ?? pagina.value,
                tamanhoPagina: filtros.size ?? tamanho.value,
            })
            lista.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanhoPagina
        } catch {
            erro.value = "Não foi possível carregar os modelos de permissão."
        } finally {
            carregando.value = false
        }
    }

    async function carregarItem(id: number): Promise<void> {
        carregando.value = true
        erro.value = null
        itemAtual.value = null
        try {
            itemAtual.value = await permissoesGlobaisService.obter(id)
        } catch {
            erro.value = "Modelo não encontrado."
        } finally {
            carregando.value = false
        }
    }

    async function criar(payload: {
        nome: string
        tipoAcesso: string
        permissoes: string[]
        icone: string | null
        cor: string | null
        descricao: string | null
    }): Promise<number> {
        const { id } = await permissoesGlobaisService.criar(payload)
        return id
    }

    async function atualizar(id: number, payload: {
        nome: string
        tipoAcesso: string
        permissoes: string[]
        icone: string | null
        cor: string | null
        descricao: string | null
    }): Promise<void> {
        await permissoesGlobaisService.atualizar(id, payload)
    }

    async function excluir(id: number): Promise<void> {
        await permissoesGlobaisService.excluir(id)
        lista.value = lista.value.filter((m) => m.id !== id)
        total.value = Math.max(0, total.value - 1)
    }

    return { lista, total, pagina, tamanho, carregando, erro, itemAtual, carregar, carregarItem, criar, atualizar, excluir }
})
