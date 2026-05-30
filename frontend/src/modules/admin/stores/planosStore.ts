import { defineStore } from "pinia"
import { ref } from "vue"
import {
    planosService,
    type PlanoAdminDto,
    type ListarPlanosResult,
    type CriarPlanoPayload,
    type AtualizarPlanoPayload,
} from "../services/planosService"

export const usePlanosStore = defineStore("adminPlanos", () => {
    const lista = ref<PlanoAdminDto[]>([])
    const total = ref(0)
    const pagina = ref(1)
    const tamanho = ref(25)
    const carregando = ref(false)
    const erro = ref<string | null>(null)
    const planoAtual = ref<PlanoAdminDto | null>(null)

    async function carregar(filtros: {
        ativo?: boolean | null
        busca?: string | null
        page?: number
        size?: number
    } = {}): Promise<void> {
        carregando.value = true
        erro.value = null
        try {
            const result: ListarPlanosResult = await planosService.listar({
                ativo: filtros.ativo ?? null,
                busca: filtros.busca ?? null,
                page: filtros.page ?? pagina.value,
                size: filtros.size ?? tamanho.value,
            })
            lista.value = result.itens
            total.value = result.total
            pagina.value = result.pagina
            tamanho.value = result.tamanho
        } catch {
            erro.value = "Não foi possível carregar os planos."
        } finally {
            carregando.value = false
        }
    }

    async function carregarPlano(id: string): Promise<void> {
        carregando.value = true
        erro.value = null
        planoAtual.value = null
        try {
            planoAtual.value = await planosService.obter(id)
        } catch {
            erro.value = "Plano não encontrado."
        } finally {
            carregando.value = false
        }
    }

    async function criar(payload: CriarPlanoPayload): Promise<void> {
        await planosService.criar(payload)
    }

    async function atualizar(id: string, payload: AtualizarPlanoPayload): Promise<void> {
        await planosService.atualizar(id, payload)
    }

    async function ativar(id: string, motivo: string): Promise<void> {
        await planosService.ativar(id, motivo)
        const item = lista.value.find((p) => p.id === id)
        if (item) item.ativo = true
    }

    async function desativar(id: string, motivo: string): Promise<void> {
        await planosService.desativar(id, motivo)
        const item = lista.value.find((p) => p.id === id)
        if (item) item.ativo = false
    }

    return {
        lista,
        total,
        pagina,
        tamanho,
        carregando,
        erro,
        planoAtual,
        carregar,
        carregarPlano,
        criar,
        atualizar,
        ativar,
        desativar,
    }
})
