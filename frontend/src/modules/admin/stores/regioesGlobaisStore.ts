import { defineStore } from "pinia"
import { ref } from "vue"
import {
    regioesGlobaisService,
    type RegiaoAnatomicaNoDto,
} from "../services/catalogosService"

export const useRegioesGlobaisStore = defineStore("adminRegioesGlobais", () => {
    const arvore = ref<RegiaoAnatomicaNoDto[]>([])
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const itemAtual = ref<RegiaoAnatomicaNoDto | null>(null)

    async function carregarArvore(incluirInativas = false): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            arvore.value = await regioesGlobaisService.listarArvore(incluirInativas)
        } catch {
            erro.value = "Não foi possível carregar as regiões anatômicas."
        } finally {
            carregando.value = false
        }
    }

    async function carregarItem(id: number): Promise<void> {
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

    async function criar(payload: {
        codigo: string
        nome: string
        paiCodigo: string | null
        nivel: number
        vista: string | null
        templateTexto: string | null
        ordem: number
        lateralidade: boolean
        motivo: string
    }): Promise<number> {
        const { id } = await regioesGlobaisService.criar(payload)
        return id
    }

    async function atualizar(id: number, payload: { nome: string; templateTexto: string | null; motivo: string }): Promise<void> {
        await regioesGlobaisService.atualizar(id, payload)
    }

    async function inativar(id: number, motivo: string): Promise<void> {
        await regioesGlobaisService.inativar(id, motivo)
    }

    async function reativar(id: number, motivo: string): Promise<void> {
        await regioesGlobaisService.reativar(id, motivo)
    }

    async function excluir(id: number, motivo: string): Promise<void> {
        await regioesGlobaisService.excluir(id, motivo)
    }

    return { arvore, carregando, erro, itemAtual, carregarArvore, carregarItem, criar, atualizar, inativar, reativar, excluir }
})
