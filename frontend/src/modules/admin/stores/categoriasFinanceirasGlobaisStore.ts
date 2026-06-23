import { defineStore } from "pinia"
import { ref } from "vue"
import {
    categoriasFinanceirasGlobaisService,
    type CategoriaFinanceiraPadraoListaItemDto,
    type TipoCategoriaFinanceira,
} from "../services/catalogosService"

export const useCategoriasFinanceirasGlobaisStore = defineStore("adminCategoriasFinanceirasGlobais", () => {
    const lista = ref<CategoriaFinanceiraPadraoListaItemDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(50)
    const carregando = ref(false)
    const erro = ref<string | null>(null)

    async function carregar(filtros: {
        tipo?: TipoCategoriaFinanceira
        ativas?: boolean
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result = await categoriasFinanceirasGlobaisService.listar({
                tipo: filtros.tipo,
                ativas: filtros.ativas,
                pagina: filtros.page ?? pagina.value,
                tamanhoPagina: filtros.size ?? tamanho.value,
            })
            lista.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanhoPagina
        } catch {
            erro.value = "Não foi possível carregar as categorias financeiras."
        } finally {
            carregando.value = false
        }
    }

    async function criar(payload: { nome: string; tipo: TipoCategoriaFinanceira }): Promise<{ id: number; instanciasPropagadas?: number }> {
        return await categoriasFinanceirasGlobaisService.criar(payload)
    }

    async function inativar(id: number): Promise<void> {
        await categoriasFinanceirasGlobaisService.inativar(id)
        const item = lista.value.find((c) => c.id === id)
        if (item) item.ativo = false
    }

    async function reativar(id: number): Promise<void> {
        await categoriasFinanceirasGlobaisService.reativar(id)
        const item = lista.value.find((c) => c.id === id)
        if (item) item.ativo = true
    }

    return { lista, total, pagina, tamanho, carregando, erro, carregar, criar, inativar, reativar }
})
