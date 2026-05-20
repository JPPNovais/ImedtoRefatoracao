<script setup lang="ts">
import { computed } from "vue"
import { AppDrawer, AppButton } from "@/components/ui"
import EstoqueStatusPill from "./EstoqueStatusPill.vue"
import type { ItemInventario, MovimentacaoEstoque } from "@/services/inventarioService"
import { formatarMoedaBrl } from "@/utils/format"

const props = defineProps<{
    aberto: boolean
    item: ItemInventario | null
    movimentacoes: MovimentacaoEstoque[]
    carregandoMovs: boolean
}>()

const emit = defineEmits<{
    fechar: []
    "nova-movimentacao": [item: ItemInventario]
    editar: [item: ItemInventario]
}>()

function statusDoItem(item: ItemInventario): string {
    if (!item.ativo) return "ok"
    if (item.quantidadeAtual <= 0) return "out"
    if (item.estoqueAbaixoMinimo) return "low"
    return "ok"
}

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}

function formatarData(iso: string) {
    return new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "short", year: "numeric" })
}

function formatarHora(iso: string) {
    return new Date(iso).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
}

const valorTotal = computed(() =>
    props.item ? props.item.custoMedio * props.item.quantidadeAtual : 0
)

const ultimasMovimentacoes = computed(() => props.movimentacoes.slice(0, 6))

function classeTipoMov(tipo: MovimentacaoEstoque["tipo"]) {
    return tipo === "Entrada" ? "entrada" : tipo === "Saida" ? "saida" : "inativacao"
}

function iconeTipoMov(tipo: MovimentacaoEstoque["tipo"]) {
    if (tipo === "Entrada") return "fa-solid fa-arrow-down-to-bracket"
    if (tipo === "Saida") return "fa-solid fa-arrow-up-from-bracket"
    return "fa-solid fa-ban"
}

function rotuloTipoMov(tipo: MovimentacaoEstoque["tipo"]) {
    return tipo === "Entrada" ? "Entrada" : tipo === "Saida" ? "Saída" : "Inativação"
}
</script>

<template>
    <AppDrawer
        :aberto="aberto"
        :largura="540"
        @fechar="emit('fechar')"
    >
        <template #titulo>
            <div v-if="item" class="drawer-titulo">
                <div class="eyebrow-row">
                    <span class="cat-pill">{{ item.categoria }}</span>
                    <EstoqueStatusPill :status="statusDoItem(item)" />
                    <span v-if="!item.ativo" class="badge-inativo">Inativo</span>
                </div>
                <h2 class="titulo-item">{{ item.nome }}</h2>
                <p class="codigo-item">{{ item.codigo }} · {{ item.unidadeMedida }}</p>
            </div>
        </template>

        <template v-if="item">
            <!-- Stats grid -->
            <section>
                <div class="stats-grid">
                    <div class="stat-box">
                        <label>Em estoque</label>
                        <b>{{ formatarQtd(item.quantidadeAtual) }} <span class="unidade">{{ item.unidadeMedida }}</span></b>
                        <small>Mínimo: {{ formatarQtd(item.quantidadeMinima) }}</small>
                    </div>
                    <div class="stat-box">
                        <label>Valor total</label>
                        <b>{{ formatarMoedaBrl(valorTotal) }}</b>
                        <small>Custo médio {{ formatarMoedaBrl(item.custoMedio) }}</small>
                    </div>
                    <div class="stat-box full">
                        <label>Cadastrado em</label>
                        <b>{{ formatarData(item.criadoEm) }}</b>
                    </div>
                </div>
            </section>

            <!-- Barra de estoque -->
            <section>
                <div class="secao-titulo">Nível de estoque</div>
                <div class="nivel-bar-wrap">
                    <div class="nivel-bar">
                        <div
                            class="nivel-bar-fill"
                            :style="{
                                width: `${Math.min(100, Math.max(2, item.quantidadeMinima > 0 ? (item.quantidadeAtual / (item.quantidadeMinima * 2)) * 100 : 100))}%`,
                                background: item.quantidadeAtual <= 0 ? 'hsl(0 70% 50%)' : item.estoqueAbaixoMinimo ? 'hsl(40 90% 50%)' : 'hsl(160 79% 39%)'
                            }"
                        ></div>
                    </div>
                    <div class="nivel-labels">
                        <span>0</span>
                        <span>Mín: {{ formatarQtd(item.quantidadeMinima) }}</span>
                    </div>
                </div>
            </section>

            <!-- Movimentações recentes -->
            <section>
                <div class="secao-titulo">Últimas movimentações</div>

                <div v-if="carregandoMovs" class="mov-loading">
                    <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
                </div>

                <div v-else-if="ultimasMovimentacoes.length === 0" class="mov-vazio">
                    Sem movimentações registradas.
                </div>

                <div v-else class="movs-lista">
                    <div
                        v-for="m in ultimasMovimentacoes"
                        :key="m.id"
                        class="mov-item"
                    >
                        <div class="mov-icone" :class="classeTipoMov(m.tipo)">
                            <i :class="iconeTipoMov(m.tipo)"></i>
                        </div>
                        <div class="mov-desc">
                            <div class="mov-nome">
                                {{ rotuloTipoMov(m.tipo) }}
                                <template v-if="m.tipo !== 'Inativacao'">
                                    · {{ formatarQtd(m.quantidade) }} {{ item.unidadeMedida }}
                                </template>
                            </div>
                            <div class="mov-meta">
                                {{ formatarData(m.criadoEm) }} {{ formatarHora(m.criadoEm) }} · {{ m.usuarioNome }}
                            </div>
                        </div>
                        <div class="mov-valor" :class="classeTipoMov(m.tipo)">
                            <template v-if="m.tipo === 'Inativacao'">—</template>
                            <template v-else>
                                {{ m.tipo === "Entrada" ? "+" : "−" }}{{ formatarQtd(m.quantidade) }}
                            </template>
                        </div>
                    </div>
                </div>
            </section>
        </template>

        <template v-if="item" #rodape>
            <AppButton
                variant="secondary"
                icon="fa-solid fa-arrow-down-to-bracket"
                :disabled="!item.ativo"
                @click="emit('nova-movimentacao', item)"
            >
                Entrada
            </AppButton>
            <AppButton
                variant="secondary"
                icon="fa-solid fa-arrow-up-from-bracket"
                :disabled="!item.ativo"
                @click="emit('nova-movimentacao', item)"
            >
                Saída
            </AppButton>
            <AppButton
                icon="fa-solid fa-pen"
                :disabled="!item.ativo"
                @click="emit('editar', item)"
            >
                Editar
            </AppButton>
        </template>
    </AppDrawer>
</template>

<style scoped>
.drawer-titulo { display: flex; flex-direction: column; gap: 4px; }

.eyebrow-row { display: flex; gap: 6px; align-items: center; flex-wrap: wrap; }

.cat-pill {
    font-size: 11px;
    font-weight: 700;
    padding: 3px 8px;
    border-radius: 999px;
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary));
}

.badge-inativo {
    font-size: 11px;
    font-weight: 700;
    padding: 3px 8px;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary) / 0.6);
}

.titulo-item {
    font-size: 18px;
    font-weight: 800;
    color: hsl(var(--primary-dark));
    margin: 0;
    line-height: 1.3;
}

.codigo-item {
    font-size: 11px;
    color: hsl(var(--secondary) / 0.55);
    font-weight: 700;
    font-family: monospace;
    margin: 0;
}

.secao-titulo {
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
    font-weight: 800;
    margin-bottom: 10px;
}

.stats-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px;
}

.stat-box {
    background: hsl(var(--secondary) / 0.03);
    border-radius: var(--radius-sm);
    padding: 12px 14px;
    border: 1px solid hsl(var(--secondary) / 0.06);
}
.stat-box.full { grid-column: 1 / -1; }
.stat-box label {
    display: block;
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--secondary) / 0.55);
    font-weight: 700;
}
.stat-box b {
    display: block;
    font-size: 18px;
    color: hsl(var(--primary-dark));
    font-weight: 800;
    line-height: 1.2;
    margin-top: 4px;
}
.stat-box small { font-size: 11px; color: hsl(var(--secondary) / 0.6); font-weight: 600; }
.unidade { font-size: 13px; color: hsl(var(--secondary) / 0.5); font-weight: 600; }

.nivel-bar-wrap { display: flex; flex-direction: column; gap: 4px; }
.nivel-bar {
    height: 8px;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.08);
    overflow: hidden;
}
.nivel-bar-fill { height: 100%; border-radius: inherit; transition: width 400ms; }
.nivel-labels {
    display: flex;
    justify-content: space-between;
    font-size: 10px;
    color: hsl(var(--secondary) / 0.5);
    font-weight: 600;
}

.movs-lista { display: flex; flex-direction: column; }

.mov-loading, .mov-vazio {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.5);
    padding: 12px 0;
}

.mov-item {
    display: grid;
    grid-template-columns: 30px 1fr auto;
    gap: 10px;
    align-items: center;
    padding: 8px 0;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
}
.mov-item:first-child { border-top: 0; }

.mov-icone {
    width: 28px;
    height: 28px;
    border-radius: var(--radius-sm);
    display: grid;
    place-items: center;
    color: white;
    font-size: 11px;
}
.mov-icone.entrada { background: hsl(160 79% 39%); }
.mov-icone.saida { background: hsl(0 70% 50%); }
.mov-icone.inativacao { background: hsl(38 92% 50%); }

.mov-nome { font-size: 13px; font-weight: 700; color: hsl(var(--primary-dark)); }
.mov-meta { font-size: 11px; color: hsl(var(--secondary) / 0.55); }

.mov-valor { font-size: 14px; font-weight: 800; }
.mov-valor.entrada { color: hsl(160 79% 32%); }
.mov-valor.saida { color: hsl(0 70% 45%); }
.mov-valor.inativacao { color: hsl(38 92% 40%); }
</style>
