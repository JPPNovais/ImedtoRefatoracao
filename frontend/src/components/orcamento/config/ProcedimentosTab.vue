<script setup lang="ts">
/**
 * Aba "Procedimentos" — config-orcamento 2026-05-16.
 * Stats cards, busca debounced, filtro de status, paginação, drawer lateral.
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    AppStatCard, AppSearchInput, AppFilterPills, AppDrawer, AppField, AppInput,
    AppButton, AppStatusPill, AppPagination, AppEmptyState,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type CatalogoCirurgia,
    type CatalogoCirurgiaPayload,
} from "@/services/orcamentoCatalogoService"
import ProcedimentoProdutosLink from "./ProcedimentoProdutosLink.vue"

const emit = defineEmits<{ (e: "contagem", v: number): void }>()

const carregando = ref(false)
const erro = ref<string | null>(null)
const lista = ref<CatalogoCirurgia[]>([])

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
type FiltroStatus = "todos" | "ativos" | "inativos"
const filtroStatus = ref<FiltroStatus>("ativos")
const pagina = ref(1)
const tamanho = ref(20)

const drawerAberto = ref(false)
const idEditando = ref<number | null>(null)
const form = ref<CatalogoCirurgiaPayload>({
    descricao: "", valorBase: 0, duracaoPadraoMinutos: null,
    codigoInterno: null, codigoTuss: null, categoria: null,
})

const filtrada = computed(() => {
    let l = lista.value
    if (filtroStatus.value === "ativos") l = l.filter(x => x.ativo)
    else if (filtroStatus.value === "inativos") l = l.filter(x => !x.ativo)
    if (busca.value.trim()) {
        const q = busca.value.trim().toLowerCase()
        l = l.filter(x =>
            x.descricao.toLowerCase().includes(q)
            || (x.codigoTuss ?? "").toLowerCase().includes(q)
            || (x.codigoInterno ?? "").toLowerCase().includes(q)
            || (x.categoria ?? "").toLowerCase().includes(q),
        )
    }
    return l
})

const total = computed(() => filtrada.value.length)
const inicio = computed(() => (pagina.value - 1) * tamanho.value)
const pagina_itens = computed(() => filtrada.value.slice(inicio.value, inicio.value + tamanho.value))

const totalAtivos = computed(() => lista.value.filter(x => x.ativo).length)
const totalInativos = computed(() => lista.value.length - totalAtivos.value)
const ticketMedio = computed(() => {
    const ativos = lista.value.filter(x => x.ativo)
    if (!ativos.length) return 0
    return ativos.reduce((s, x) => s + x.valorBase, 0) / ativos.length
})
const totalCategorias = computed(() => new Set(lista.value.map(x => x.categoria).filter(Boolean)).size)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        lista.value = await orcamentoCatalogoService.listarProcedimentos()
        emit("contagem", lista.value.length)
    } catch {
        erro.value = "Não foi possível carregar procedimentos."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)
watch(busca, () => { pagina.value = 1 })
watch(filtroStatus, () => { pagina.value = 1 })

function novo() {
    idEditando.value = null
    form.value = {
        descricao: "", valorBase: 0, duracaoPadraoMinutos: null,
        codigoInterno: null, codigoTuss: null, categoria: null,
    }
    drawerAberto.value = true
}

function editar(item: CatalogoCirurgia) {
    idEditando.value = item.id
    form.value = {
        descricao: item.descricao,
        valorBase: item.valorBase,
        duracaoPadraoMinutos: item.duracaoPadraoMinutos,
        codigoInterno: item.codigoInterno,
        codigoTuss: item.codigoTuss,
        categoria: item.categoria,
    }
    drawerAberto.value = true
}

async function salvar() {
    if (!form.value.descricao.trim()) {
        alert("Descrição é obrigatória.")
        return
    }
    try {
        if (idEditando.value === null) {
            await orcamentoCatalogoService.criarProcedimento(form.value)
        } else {
            await orcamentoCatalogoService.atualizarProcedimento(idEditando.value, form.value)
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Falha ao salvar.")
    }
}

async function remover(item: CatalogoCirurgia) {
    if (!confirm(`Inativar "${item.descricao}"?`)) return
    try {
        await orcamentoCatalogoService.removerProcedimento(item.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Falha ao inativar.")
    }
}

defineExpose({ recarregar: carregar })
</script>

<template>
    <div class="config-tab">
        <div class="stats-grid">
            <AppStatCard label="Cadastrados" :valor="lista.length" cor="primary" icone="fa-solid fa-scalpel" />
            <AppStatCard label="Ativos" :valor="totalAtivos" cor="success" icone="fa-solid fa-circle-check" />
            <AppStatCard label="Ticket médio" :valor="formatarMoedaBrl(ticketMedio)" cor="info" icone="fa-solid fa-coins" />
            <AppStatCard label="Categorias" :valor="totalCategorias" cor="warning" icone="fa-solid fa-tags" />
        </div>

        <div class="toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por descrição, TUSS, categoria..." />
            <AppFilterPills
                v-model="filtroStatus"
                :opcoes="[
                    { valor: 'ativos', label: 'Ativos', count: totalAtivos, dot: 'success' },
                    { valor: 'inativos', label: 'Inativos', count: totalInativos, dot: 'muted' },
                    { valor: 'todos', label: 'Todos', count: lista.length },
                ]"
            />
            <AppButton icon="fa-solid fa-plus" @click="novo">Novo procedimento</AppButton>
        </div>

        <div v-if="carregando" class="loading">Carregando…</div>
        <AppEmptyState
            v-else-if="!lista.length"
            icone="fa-solid fa-scalpel"
            titulo="Nenhum procedimento cadastrado"
            descricao="Cadastre o primeiro procedimento para começar a montar orçamentos."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="novo">Criar primeiro procedimento</AppButton>
            </template>
        </AppEmptyState>
        <AppEmptyState
            v-else-if="!pagina_itens.length"
            icone="fa-solid fa-magnifying-glass"
            titulo="Nenhum resultado"
            descricao="Ajuste a busca ou os filtros."
        />
        <div v-else class="table-wrap">
            <table class="table">
                <thead>
                    <tr>
                        <th>Descrição</th>
                        <th>Categoria</th>
                        <th>TUSS</th>
                        <th class="num">Duração</th>
                        <th class="num">Valor base</th>
                        <th>Status</th>
                        <th class="acoes-col"></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="item in pagina_itens" :key="item.id">
                        <td>
                            <div class="cell-desc">{{ item.descricao }}</div>
                            <div v-if="item.codigoInterno" class="cell-sub">Cód.: {{ item.codigoInterno }}</div>
                        </td>
                        <td>{{ item.categoria ?? "—" }}</td>
                        <td>{{ item.codigoTuss ?? "—" }}</td>
                        <td class="num">{{ item.duracaoPadraoMinutos ? `${item.duracaoPadraoMinutos} min` : "—" }}</td>
                        <td class="num">{{ formatarMoedaBrl(item.valorBase) }}</td>
                        <td><AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" /></td>
                        <td class="acoes-col">
                            <button class="btn-icon btn-icon-editar" title="Editar" @click="editar(item)">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button v-if="item.ativo" class="btn-icon btn-icon-excluir" title="Inativar" @click="remover(item)">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
            <AppPagination v-model:pagina="pagina" v-model:tamanho="tamanho" :total="total" />
        </div>

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="idEditando === null ? 'Novo procedimento' : 'Editar procedimento'"
            :largura="640"
            @fechar="drawerAberto = false"
        >
            <AppField label="Descrição" required>
                <AppInput v-model="form.descricao" placeholder="Ex: Colecistectomia videolaparoscópica" />
            </AppField>
            <div class="grid-2">
                <AppField label="Categoria">
                    <AppInput v-model="form.categoria" placeholder="Ex: Cirurgia geral" />
                </AppField>
                <AppField label="Código TUSS">
                    <AppInput v-model="form.codigoTuss" placeholder="Ex: 30912025" />
                </AppField>
            </div>
            <div class="grid-2">
                <AppField label="Código interno">
                    <AppInput v-model="form.codigoInterno" placeholder="Opcional" />
                </AppField>
                <AppField label="Duração padrão (min)">
                    <AppInput
                        type="number"
                        :model-value="form.duracaoPadraoMinutos ?? ''"
                        @update:model-value="(v: any) => form.duracaoPadraoMinutos = v === '' ? null : Number(v)"
                        placeholder="90"
                    />
                </AppField>
            </div>
            <AppField label="Valor base (R$)" required>
                <AppInput
                    type="number"
                    step="0.01"
                    :model-value="form.valorBase"
                    @update:model-value="(v: any) => form.valorBase = Number(v) || 0"
                />
            </AppField>

            <ProcedimentoProdutosLink
                v-if="idEditando !== null"
                :cirurgia-id="idEditando"
                :valor-base="form.valorBase"
            />
            <p v-else class="hint-criar">Salve o procedimento primeiro para vincular produtos.</p>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>
    </div>
</template>

<style scoped>
.config-tab { display: flex; flex-direction: column; gap: 16px; }
.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
.toolbar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.toolbar > :first-child { flex: 1 1 280px; min-width: 220px; }
.loading { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.7); }
.table-wrap {
    background: white; border-radius: 14px;
    border: 1px solid hsl(var(--secondary) / 0.1); overflow: hidden;
}
.table { width: 100%; border-collapse: collapse; font-size: 14px; }
.table thead th {
    text-align: left; padding: 12px 16px;
    background: hsl(var(--secondary) / 0.04);
    color: hsl(var(--secondary) / 0.7);
    font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.04em;
}
.table tbody td { padding: 12px 16px; border-top: 1px solid hsl(var(--secondary) / 0.08); }
.table .num { text-align: right; }
.cell-desc { font-weight: 600; }
.cell-sub { font-size: 12px; color: hsl(var(--secondary) / 0.6); }
.acoes-col { width: 90px; text-align: right; white-space: nowrap; }
.acoes-col .btn-icon + .btn-icon { margin-left: 4px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.hint-criar {
    font-size: 13px; color: hsl(var(--secondary) / 0.6);
    background: hsl(var(--secondary) / 0.04); padding: 12px; border-radius: 8px;
}
</style>
