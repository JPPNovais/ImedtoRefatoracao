<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { AppSearchInput, AppFilterPills, AppEmptyState, AppPagination } from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import type { MovimentacaoEstoque } from "@/services/inventarioService"

const props = defineProps<{
    movimentacoes: MovimentacaoEstoque[]
    total: number
    pagina: number
    tamanho: number
    carregando: boolean
}>()

const emit = defineEmits<{
    "update:pagina": [v: number]
    "update:tamanho": [v: number]
    "busca-change": [busca: string]
    "filtro-tipo-change": [tipo: string]
}>()

type FiltroTipo = "todos" | "Entrada" | "Saida" | "Inativacao"

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const filtroTipo = ref<FiltroTipo>("todos")

watch(busca, (v) => emit("busca-change", v))
watch(filtroTipo, () => emit("filtro-tipo-change", filtroTipo.value))

const opcoesTipo = [
    { valor: "todos" as FiltroTipo, label: "Todas", count: undefined },
    { valor: "Entrada" as FiltroTipo, label: "Entradas", dot: "success" as const },
    { valor: "Saida" as FiltroTipo, label: "Saídas", dot: "error" as const },
    { valor: "Inativacao" as FiltroTipo, label: "Inativações", dot: "warning" as const },
]

const tipoIcone: Record<MovimentacaoEstoque["tipo"], string> = {
    Entrada: "fa-solid fa-arrow-down-to-bracket",
    Saida: "fa-solid fa-arrow-up-from-bracket",
    Inativacao: "fa-solid fa-ban",
}

const tipoLabel: Record<MovimentacaoEstoque["tipo"], string> = {
    Entrada: "Entrada",
    Saida: "Saída",
    Inativacao: "Inativação",
}

function classeTipo(tipo: MovimentacaoEstoque["tipo"]) {
    return tipo === "Entrada" ? "entrada" : tipo === "Saida" ? "saida" : "inativacao"
}

// Agrupamento por dia
const porDia = computed(() => {
    const grupos: Record<string, MovimentacaoEstoque[]> = {}
    for (const m of props.movimentacoes) {
        const dia = m.criadoEm.split("T")[0]
        if (!grupos[dia]) grupos[dia] = []
        grupos[dia].push(m)
    }
    return Object.entries(grupos)
        .sort(([a], [b]) => b.localeCompare(a))
        .map(([dia, lista]) => ({ dia, lista }))
})

function formatarDia(iso: string) {
    const d = new Date(iso + "T00:00:00")
    const hoje = new Date()
    hoje.setHours(0, 0, 0, 0)
    const diff = Math.round((hoje.getTime() - d.getTime()) / 86400000)
    if (diff === 0) return "Hoje"
    if (diff === 1) return "Ontem"
    return d.toLocaleDateString("pt-BR", { weekday: "long", day: "2-digit", month: "long" })
}

function formatarHora(iso: string) {
    return new Date(iso).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
}

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}
</script>

<template>
    <div class="mov-tab">
        <div class="filtros-bar">
            <AppSearchInput
                v-model="buscaInput"
                placeholder="Buscar por item, usuário..."
            />
            <AppFilterPills v-model="filtroTipo" :opcoes="opcoesTipo" />
        </div>

        <div v-if="carregando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i>
            Carregando movimentações...
        </div>

        <div v-else-if="movimentacoes.length === 0" class="estado-vazio">
            <AppEmptyState
                icone="fa-solid fa-clock-rotate-left"
                titulo="Nenhuma movimentação encontrada"
                descricao="Ajuste os filtros ou registre uma nova movimentação."
                :compacto="true"
            />
        </div>

        <div v-else class="mov-timeline">
            <template v-for="grupo in porDia" :key="grupo.dia">
                <div class="dia-header">
                    {{ formatarDia(grupo.dia) }}
                    <span class="dia-count">{{ grupo.lista.length }} movimentação{{ grupo.lista.length !== 1 ? "ões" : "" }}</span>
                </div>

                <div
                    v-for="m in grupo.lista"
                    :key="m.id"
                    class="mov-row"
                >
                    <div class="mov-icone" :class="classeTipo(m.tipo)">
                        <i :class="tipoIcone[m.tipo]"></i>
                    </div>
                    <div class="mov-hora">{{ formatarHora(m.criadoEm) }}</div>
                    <div class="mov-item">
                        <b>{{ m.itemNome }}</b>
                        <span v-if="m.observacao">{{ m.observacao }}</span>
                    </div>
                    <div class="mov-qty" :class="classeTipo(m.tipo)">
                        <template v-if="m.tipo === 'Inativacao'">—</template>
                        <template v-else>
                            {{ m.tipo === "Entrada" ? "+" : "−" }}{{ formatarQtd(m.quantidade) }}
                        </template>
                        <small>{{ tipoLabel[m.tipo] }}</small>
                    </div>
                    <div class="mov-meta">
                        <b>{{ m.usuarioNome }}</b>
                        <span v-if="m.tipo === 'Inativacao'">
                            Estoque no momento: {{ formatarQtd(m.quantidadeApos) }}
                        </span>
                        <span v-else>
                            Antes: {{ formatarQtd(m.quantidadeAnterior) }} → Após: {{ formatarQtd(m.quantidadeApos) }}
                        </span>
                    </div>
                </div>
            </template>
        </div>

        <AppPagination
            v-if="total > 0 && !carregando"
            :pagina="pagina"
            :tamanho="tamanho"
            :total="total"
            rotulo-itens="movimentações"
            class="paginacao"
            @update:pagina="emit('update:pagina', $event)"
            @update:tamanho="emit('update:tamanho', $event)"
        />
    </div>
</template>

<style scoped>
.mov-tab { display: flex; flex-direction: column; gap: 12px; }

.filtros-bar {
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
    align-items: center;
}

.estado-info {
    padding: 40px;
    text-align: center;
    color: hsl(var(--secondary) / 0.6);
    font-size: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
}

.estado-vazio { padding: 20px 0; }

.mov-timeline {
    background: var(--bg-card);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow);
    overflow: hidden;
}

.dia-header {
    font-size: 11px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
    padding: 14px 20px 6px;
    display: flex;
    align-items: center;
    gap: 10px;
}
.dia-header::after { content: ''; flex: 1; height: 1px; background: hsl(var(--secondary) / 0.08); }
.dia-count { font-size: 10px; font-weight: 600; color: hsl(var(--secondary) / 0.45); }

.mov-row {
    display: grid;
    grid-template-columns: 36px 64px 1.6fr 1fr 1.8fr;
    gap: 14px;
    align-items: center;
    padding: 10px 20px;
    border-top: 1px solid hsl(var(--secondary) / 0.04);
    transition: background 150ms;
}
.mov-row:hover { background: hsl(var(--primary) / 0.025); }

.mov-icone {
    width: 32px;
    height: 32px;
    border-radius: var(--radius-sm);
    display: grid;
    place-items: center;
    color: white;
    font-size: 12px;
}
.mov-icone.entrada { background: hsl(160 79% 39%); }
.mov-icone.saida { background: hsl(0 70% 50%); }
.mov-icone.inativacao { background: hsl(38 92% 50%); }

.mov-hora {
    font-size: 11px;
    color: hsl(var(--secondary) / 0.55);
    font-variant-numeric: tabular-nums;
    font-weight: 600;
}

.mov-item b { display: block; font-size: 13px; color: hsl(var(--primary-dark)); font-weight: 700; }
.mov-item span { display: block; font-size: 11px; color: hsl(var(--secondary) / 0.55); margin-top: 2px; }

.mov-qty {
    font-size: 15px;
    font-weight: 800;
    font-variant-numeric: tabular-nums;
}
.mov-qty.entrada { color: hsl(160 79% 32%); }
.mov-qty.saida { color: hsl(0 70% 45%); }
.mov-qty.inativacao { color: hsl(38 92% 40%); }
.mov-qty small { display: block; font-size: 10px; font-weight: 600; color: hsl(var(--secondary) / 0.5); }

.mov-meta b { font-size: 13px; font-weight: 700; color: hsl(var(--primary-dark)); display: block; }
.mov-meta span { font-size: 11px; color: hsl(var(--secondary) / 0.55); display: block; }

.paginacao { margin-top: 4px; }

@media (max-width: 800px) {
    .mov-row { grid-template-columns: 36px 1fr auto; }
    .mov-hora, .mov-meta { display: none; }
}
</style>
