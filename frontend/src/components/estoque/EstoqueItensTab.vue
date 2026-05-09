<script setup lang="ts">
import { ref, computed } from "vue"
import { AppSearchInput, AppFilterPills, AppEmptyState, AppButton, AppPagination } from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import EstoqueStatusPill from "./EstoqueStatusPill.vue"
import type { ItemInventario } from "@/services/inventarioService"
import { formatarMoedaBrl } from "@/utils/format"

const props = defineProps<{
    itens: ItemInventario[]
    total: number
    pagina: number
    tamanho: number
    carregando: boolean
    categorias: string[]
}>()

const emit = defineEmits<{
    "update:pagina": [v: number]
    "update:tamanho": [v: number]
    "abrir-item": [item: ItemInventario]
    "nova-movimentacao": [item: ItemInventario]
    "editar": [item: ItemInventario]
    "inativar": [item: ItemInventario]
    "criar": []
    "busca-change": [busca: string]
    "filtro-status-change": [status: string]
    "filtro-categoria-change": [cat: string]
}>()

type FiltroStatus = "todos" | "atencao" | "baixo" | "vencendo"

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const filtroStatus = ref<FiltroStatus>("todos")
const filtroCategoria = ref("")
const agrupar = ref(false)

// Notifica o pai para recarregar quando debounced mudar
import { watch } from "vue"
watch(busca, (v) => emit("busca-change", v))
watch(filtroStatus, () => emit("filtro-status-change", filtroStatus.value))
watch(filtroCategoria, () => emit("filtro-categoria-change", filtroCategoria.value))

const selecionados = ref(new Set<number>())

const opcoesFiltro = computed(() => [
    { valor: "todos" as FiltroStatus, label: "Todos os itens", count: props.total },
    { valor: "atencao" as FiltroStatus, label: "Precisam atenção", dot: "warning" as const },
    { valor: "baixo" as FiltroStatus, label: "Baixos", dot: "error" as const },
    { valor: "vencendo" as FiltroStatus, label: "Vencendo", dot: "warning" as const },
])

const todosChecados = computed(() =>
    props.itens.length > 0 && props.itens.every(it => selecionados.value.has(it.id))
)

function toggleTodos() {
    if (todosChecados.value) {
        selecionados.value = new Set()
    } else {
        selecionados.value = new Set(props.itens.map(it => it.id))
    }
}

function toggleItem(id: number) {
    const next = new Set(selecionados.value)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    selecionados.value = next
}

function limparSelecao() {
    selecionados.value = new Set()
}

function statusDoItem(item: ItemInventario): string {
    if (!item.ativo) return "ok"
    if (item.quantidadeAtual <= 0) return "out"
    if (item.estoqueAbaixoMinimo) return "low"
    return "ok"
}

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}

const itensAgrupados = computed(() => {
    if (!agrupar.value) return null
    const grupos: Record<string, ItemInventario[]> = {}
    for (const item of props.itens) {
        if (!grupos[item.categoria]) grupos[item.categoria] = []
        grupos[item.categoria].push(item)
    }
    return grupos
})
</script>

<template>
    <div class="itens-tab">
        <!-- Filtros -->
        <div class="filtros-bar">
            <AppSearchInput
                v-model="buscaInput"
                placeholder="Buscar por nome, código..."
            />
            <select
                v-model="filtroCategoria"
                class="select-categoria"
                aria-label="Filtrar por categoria"
            >
                <option value="">Todas as categorias</option>
                <option v-for="cat in categorias" :key="cat" :value="cat">{{ cat }}</option>
            </select>
            <button
                type="button"
                class="btn-agrupar"
                :class="{ ativo: agrupar }"
                @click="agrupar = !agrupar"
                title="Agrupar por categoria"
            >
                <i class="fa-solid fa-layer-group"></i>
                Agrupar
            </button>
        </div>

        <AppFilterPills
            v-model="filtroStatus"
            :opcoes="opcoesFiltro"
            class="mb-pills"
        />

        <!-- Bulk bar -->
        <div v-if="selecionados.size > 0" class="bulk-bar">
            <span>
                <i class="fa-solid fa-circle-check"></i>
                {{ selecionados.size }} {{ selecionados.size === 1 ? "item selecionado" : "itens selecionados" }}
            </span>
            <div class="bulk-acoes">
                <button type="button" @click="limparSelecao">
                    <i class="fa-solid fa-xmark"></i>
                </button>
            </div>
        </div>

        <!-- Tabela -->
        <div class="stock-table">
            <!-- Header -->
            <div class="st-thead" aria-hidden="true">
                <div class="st-checkbox">
                    <input
                        type="checkbox"
                        :checked="todosChecados"
                        :indeterminate="selecionados.size > 0 && !todosChecados"
                        aria-label="Selecionar todos"
                        @change="toggleTodos"
                    />
                </div>
                <div>Item</div>
                <div>Categoria</div>
                <div>Estoque</div>
                <div>Custo médio</div>
                <div>Status</div>
                <div></div>
            </div>

            <!-- Loading -->
            <div v-if="carregando" class="table-loading">
                <i class="fa-solid fa-spinner fa-spin"></i>
                Carregando itens...
            </div>

            <!-- Empty -->
            <div v-else-if="itens.length === 0" class="table-empty">
                <AppEmptyState
                    icone="fa-solid fa-box-open"
                    titulo="Nenhum item encontrado"
                    descricao="Tente ajustar os filtros ou cadastre um novo item."
                    :compacto="true"
                >
                    <template #acao>
                        <AppButton icon="fa-solid fa-plus" @click="emit('criar')">
                            Novo item
                        </AppButton>
                    </template>
                </AppEmptyState>
            </div>

            <!-- Itens agrupados -->
            <template v-else-if="itensAgrupados">
                <template v-for="(lista, categoria) in itensAgrupados" :key="categoria">
                    <div class="st-row cat-group">
                        <i class="fa-solid fa-folder"></i>
                        <span>{{ categoria }}</span>
                        <span class="group-count">{{ lista.length }} itens</span>
                    </div>
                    <div
                        v-for="item in lista"
                        :key="item.id"
                        class="st-row"
                        :class="{ inativo: !item.ativo, alerta: item.estoqueAbaixoMinimo }"
                        role="row"
                        @click="emit('abrir-item', item)"
                    >
                        <div class="st-checkbox" @click.stop>
                            <input
                                type="checkbox"
                                :checked="selecionados.has(item.id)"
                                :aria-label="`Selecionar ${item.nome}`"
                                @change="toggleItem(item.id)"
                            />
                        </div>
                        <div class="st-name">
                            <div class="st-icon">
                                <i class="fa-solid fa-pills"></i>
                            </div>
                            <div>
                                <b>{{ item.nome }}</b>
                                <span>{{ item.codigo }} · {{ item.unidadeMedida }}</span>
                            </div>
                        </div>
                        <div>
                            <span class="cat-pill">{{ item.categoria }}</span>
                        </div>
                        <div class="st-stock-cell">
                            <div class="qty">
                                {{ formatarQtd(item.quantidadeAtual) }}
                                <small>/ mín {{ formatarQtd(item.quantidadeMinima) }} {{ item.unidadeMedida }}</small>
                            </div>
                            <div class="meter">
                                <span
                                    :style="{
                                        width: `${Math.min(100, Math.max(4, item.quantidadeMinima > 0 ? (item.quantidadeAtual / (item.quantidadeMinima * 2)) * 100 : 100))}%`,
                                        background: item.quantidadeAtual <= 0 ? 'hsl(0 70% 50%)' : item.estoqueAbaixoMinimo ? 'hsl(40 90% 50%)' : 'hsl(160 79% 39%)'
                                    }"
                                ></span>
                            </div>
                        </div>
                        <div class="st-cost">
                            {{ formatarMoedaBrl(item.custoMedio) }}
                            <small>por {{ item.unidadeMedida }}</small>
                        </div>
                        <div>
                            <EstoqueStatusPill :status="statusDoItem(item)" />
                        </div>
                        <div class="st-actions">
                            <button
                                type="button"
                                class="btn-icon"
                                title="Registrar movimentação"
                                :disabled="!item.ativo"
                                @click.stop="emit('nova-movimentacao', item)"
                            >
                                <i class="fa-solid fa-right-left"></i>
                            </button>
                            <button
                                type="button"
                                class="btn-icon btn-icon-editar"
                                title="Editar item"
                                :disabled="!item.ativo"
                                @click.stop="emit('editar', item)"
                            >
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button
                                v-if="item.ativo"
                                type="button"
                                class="btn-icon btn-icon-excluir"
                                title="Inativar item"
                                @click.stop="emit('inativar', item)"
                            >
                                <i class="fa-solid fa-ban"></i>
                            </button>
                        </div>
                    </div>
                </template>
            </template>

            <!-- Itens sem agrupamento -->
            <div
                v-else
                v-for="item in itens"
                :key="item.id"
                class="st-row"
                :class="{ inativo: !item.ativo, alerta: item.estoqueAbaixoMinimo }"
                role="row"
                @click="emit('abrir-item', item)"
            >
                <div class="st-checkbox" @click.stop>
                    <input
                        type="checkbox"
                        :checked="selecionados.has(item.id)"
                        :aria-label="`Selecionar ${item.nome}`"
                        @change="toggleItem(item.id)"
                    />
                </div>
                <div class="st-name">
                    <div class="st-icon">
                        <i class="fa-solid fa-pills"></i>
                    </div>
                    <div>
                        <b>{{ item.nome }}</b>
                        <span>{{ item.codigo }} · {{ item.unidadeMedida }}</span>
                    </div>
                </div>
                <div>
                    <span class="cat-pill">{{ item.categoria }}</span>
                </div>
                <div class="st-stock-cell">
                    <div class="qty">
                        {{ formatarQtd(item.quantidadeAtual) }}
                        <small>/ mín {{ formatarQtd(item.quantidadeMinima) }} {{ item.unidadeMedida }}</small>
                    </div>
                    <div class="meter">
                        <span
                            :style="{
                                width: `${Math.min(100, Math.max(4, item.quantidadeMinima > 0 ? (item.quantidadeAtual / (item.quantidadeMinima * 2)) * 100 : 100))}%`,
                                background: item.quantidadeAtual <= 0 ? 'hsl(0 70% 50%)' : item.estoqueAbaixoMinimo ? 'hsl(40 90% 50%)' : 'hsl(160 79% 39%)'
                            }"
                        ></span>
                    </div>
                </div>
                <div class="st-cost">
                    {{ formatarMoedaBrl(item.custoMedio) }}
                    <small>por {{ item.unidadeMedida }}</small>
                </div>
                <div>
                    <EstoqueStatusPill :status="statusDoItem(item)" />
                </div>
                <div class="st-actions" @click.stop>
                    <button
                        type="button"
                        class="btn-icon"
                        title="Registrar movimentação"
                        :disabled="!item.ativo"
                        @click="emit('nova-movimentacao', item)"
                    >
                        <i class="fa-solid fa-right-left"></i>
                    </button>
                    <button
                        type="button"
                        class="btn-icon btn-icon-editar"
                        title="Editar item"
                        :disabled="!item.ativo"
                        @click="emit('editar', item)"
                    >
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button
                        v-if="item.ativo"
                        type="button"
                        class="btn-icon btn-icon-excluir"
                        title="Inativar item"
                        @click="emit('inativar', item)"
                    >
                        <i class="fa-solid fa-ban"></i>
                    </button>
                </div>
            </div>
        </div>

        <AppPagination
            v-if="total > 0 && !carregando"
            :pagina="pagina"
            :tamanho="tamanho"
            :total="total"
            rotulo-itens="itens"
            class="paginacao"
            @update:pagina="emit('update:pagina', $event)"
            @update:tamanho="emit('update:tamanho', $event)"
        />
    </div>
</template>

<style scoped>
.itens-tab { display: flex; flex-direction: column; gap: 12px; }

.filtros-bar {
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
    align-items: center;
}

.select-categoria {
    padding: 9px 28px 9px 12px;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: var(--radius-lg);
    font-size: 13px;
    font-family: inherit;
    background: var(--bg-card);
    color: hsl(var(--secondary));
    font-weight: 600;
    cursor: pointer;
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='10' viewBox='0 0 10 10'%3E%3Cpath fill='%2399999d' d='M5 7L1 3h8z'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 10px center;
    background-color: var(--bg-card);
}

.btn-agrupar {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 9px 14px;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: var(--radius-lg);
    font-size: 13px;
    font-weight: 600;
    background: var(--bg-card);
    color: hsl(var(--secondary) / 0.7);
    cursor: pointer;
    transition: all 150ms;
}
.btn-agrupar.ativo {
    background: hsl(var(--primary) / 0.08);
    border-color: hsl(var(--primary) / 0.4);
    color: hsl(var(--primary));
}

.mb-pills { margin-bottom: 4px; }

.bulk-bar {
    background: hsl(var(--primary));
    color: white;
    border-radius: var(--radius-lg);
    padding: 10px 16px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 13px;
    font-weight: 600;
    animation: slideUp 200ms ease-out;
}
@keyframes slideUp { from { transform: translateY(6px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

.bulk-acoes { display: flex; gap: 6px; }
.bulk-acoes button {
    background: rgb(255 255 255 / 0.15);
    color: white;
    border: 0;
    padding: 6px 10px;
    border-radius: var(--radius-sm);
    font-size: 13px;
    cursor: pointer;
}
.bulk-acoes button:hover { background: rgb(255 255 255 / 0.25); }

/* Tabela */
.stock-table {
    background: var(--bg-card);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow);
}

.st-thead,
.st-row {
    display: grid;
    grid-template-columns: 32px 2.2fr 1fr 1.4fr 1fr 110px auto;
    gap: 12px;
    align-items: center;
    padding: 11px 16px;
}

.st-thead {
    background: hsl(var(--secondary) / 0.025);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 10px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
}

.st-row {
    border-bottom: 1px solid hsl(var(--secondary) / 0.05);
    cursor: pointer;
    transition: background 150ms;
}
.st-row:hover { background: hsl(var(--primary) / 0.025); }
.st-row:last-child { border-bottom: 0; }
.st-row.inativo { opacity: 0.55; }
.st-row.alerta { background: hsl(var(--warning) / 0.05); }

.st-row.cat-group {
    background: hsl(var(--secondary) / 0.03);
    font-size: 11px;
    font-weight: 800;
    text-transform: uppercase;
    color: hsl(var(--secondary) / 0.6);
    letter-spacing: 0.05em;
    cursor: default;
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 7px 16px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.group-count { margin-left: auto; font-weight: 600; color: hsl(var(--secondary) / 0.5); }

.st-checkbox { display: grid; place-items: center; }
.st-checkbox input { width: 14px; height: 14px; cursor: pointer; accent-color: hsl(var(--primary)); }

.st-name { display: flex; align-items: center; gap: 12px; min-width: 0; }
.st-icon {
    width: 34px;
    height: 34px;
    border-radius: var(--radius-sm);
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary));
    display: grid;
    place-items: center;
    font-size: 13px;
    flex-shrink: 0;
}
.st-name b { display: block; font-size: 13px; font-weight: 700; color: hsl(var(--primary-dark)); line-height: 1.3; }
.st-name span { display: block; font-size: 11px; color: hsl(var(--secondary) / 0.6); margin-top: 2px; font-family: monospace; }

.cat-pill {
    display: inline-flex;
    align-items: center;
    font-size: 11px;
    font-weight: 700;
    padding: 3px 8px;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.07);
    color: hsl(var(--secondary) / 0.75);
    white-space: nowrap;
}

.st-stock-cell { display: flex; flex-direction: column; gap: 4px; }
.qty {
    display: flex;
    align-items: baseline;
    gap: 4px;
    font-size: 14px;
    font-weight: 800;
    color: hsl(var(--primary-dark));
}
.qty small { font-size: 11px; font-weight: 600; color: hsl(var(--secondary) / 0.55); }
.meter {
    height: 4px;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.08);
    overflow: hidden;
}
.meter > span { display: block; height: 100%; border-radius: inherit; transition: width 300ms; }

.st-cost { font-size: 12px; font-weight: 700; color: hsl(var(--primary-dark)); font-variant-numeric: tabular-nums; }
.st-cost small { display: block; font-size: 10px; color: hsl(var(--secondary) / 0.55); font-weight: 500; }

.st-actions { display: flex; gap: 4px; align-items: center; }

.table-loading {
    padding: 40px;
    text-align: center;
    color: hsl(var(--secondary) / 0.6);
    font-size: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
}

.table-empty { padding: 32px 16px; }

.paginacao { margin-top: 4px; }

@media (max-width: 900px) {
    .st-thead, .st-row {
        grid-template-columns: 32px 2fr 1fr 1fr 110px auto;
    }
    .st-thead > div:nth-child(5),
    .st-row > div:nth-child(5) { display: none; }
}
</style>
