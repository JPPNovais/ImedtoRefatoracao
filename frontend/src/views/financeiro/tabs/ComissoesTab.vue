<script setup lang="ts">
import { ref, onMounted } from "vue"
import { financeiroService, type ComissaoPeriodo } from "@/services/financeiroService"
import { AppButton, AppFilterPills, AppDatePicker } from "@/components/ui"

defineProps<{ ehDono: boolean }>()

// Formata Date em YYYY-MM-DD usando getters locais — nunca toISOString() (que devolve UTC
// e pula para o dia seguinte após as 21h BRT). Padrão: briefing 2026-06-24_001 R3/CA11.
function formatarDataLocal(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`
}
function hojeStr() { return formatarDataLocal(new Date()) }
function inicioMes() {
    const d = new Date()
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`
}
function fimMes() {
    const d = new Date()
    const ultimo = new Date(d.getFullYear(), d.getMonth() + 1, 0)
    return formatarDataLocal(ultimo)
}

// ─── Período ──────────────────────────────────────────────────────────────────
type Chip = "mes" | "trimestre" | "personalizado"
const chipAtivo = ref<Chip>("mes")

const chipsMes: { valor: Chip; label: string }[] = [
    { valor: "mes",         label: "Este mês"    },
    { valor: "trimestre",   label: "Trimestre"   },
    { valor: "personalizado", label: "Personalizado" },
]

function aplicarChip(c: Chip) {
    chipAtivo.value = c
    const agora = new Date()
    if (c === "mes") {
        dataInicio.value = inicioMes()
        dataFim.value = fimMes()
    } else if (c === "trimestre") {
        const y = agora.getFullYear()
        const mesNum = agora.getMonth()
        const inicioTri = new Date(y, Math.floor(mesNum / 3) * 3, 1)
        // Fim do trimestre = último dia do mês que encerra o trimestre.
        const fimTri = new Date(y, Math.floor(mesNum / 3) * 3 + 3, 0)
        dataInicio.value = formatarDataLocal(inicioTri)
        dataFim.value = formatarDataLocal(fimTri)
    }
    if (c !== "personalizado") carregar()
}

const dataInicio = ref(inicioMes())
const dataFim = ref(fimMes())

// ─── Dados ────────────────────────────────────────────────────────────────────
const periodo = ref<ComissaoPeriodo | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)
const expandido = ref<Set<string>>(new Set())

function toggleExpand(id: string) {
    if (expandido.value.has(id)) {
        expandido.value.delete(id)
    } else {
        expandido.value.add(id)
    }
}

async function carregar() {
    if (!dataInicio.value || !dataFim.value) return
    carregando.value = true; erro.value = null
    try {
        periodo.value = await financeiroService.comissoes({ dataInicio: dataInicio.value, dataFim: dataFim.value })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar comissões."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)

// ─── Helpers ──────────────────────────────────────────────────────────────────
function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
function pct(n: number | null) {
    if (n === null) return "30%"
    return `${n % 1 === 0 ? n : n.toFixed(1)}%`
}
function formatarData(s: string) {
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}
function iniciais(nome: string) {
    return nome.split(" ").slice(0, 2).map(p => p[0].toUpperCase()).join("")
}
// Cor do avatar baseado no nome (determinístico)
const AVATAR_COLORS = [
    "hsl(211 91% 60%)",
    "hsl(271 91% 65%)",
    "hsl(142 71% 45%)",
    "hsl(28 90% 55%)",
    "hsl(335 80% 60%)",
    "hsl(190 80% 45%)",
]
function corAvatar(nome: string): string {
    let hash = 0
    for (let i = 0; i < nome.length; i++) hash = (hash * 31 + nome.charCodeAt(i)) >>> 0
    return AVATAR_COLORS[hash % AVATAR_COLORS.length]
}
</script>

<template>
    <div class="comissoes-tab">
        <!-- Barra de período -->
        <div class="periodo-bar">
            <AppFilterPills
                v-model="chipAtivo"
                :opcoes="chipsMes"
                @update:modelValue="aplicarChip($event as Chip)"
            />
            <template v-if="chipAtivo === 'personalizado'">
                <AppDatePicker v-model="dataInicio" aria-label="Data início" />
                <span class="sep">até</span>
                <AppDatePicker v-model="dataFim" aria-label="Data fim" />
                <AppButton
                    icon="fa-solid fa-sync"
                    :loading="carregando"
                    @click="carregar"
                >
                    Aplicar
                </AppButton>
            </template>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <div v-if="carregando" class="info">Carregando...</div>

        <template v-if="!carregando && periodo">
            <!-- Total a repassar pill -->
            <div class="cm-total-bar">
                <span class="cm-total-label">Total a repassar no período</span>
                <span class="cm-total-val">{{ moeda(periodo.totalARepassar) }}</span>
            </div>

            <div v-if="periodo.profissionais.length === 0" class="cm-empty">
                <i class="fa-solid fa-percent" aria-hidden="true" />
                <b>Nenhuma comissão no período</b>
                <p>Não há atendimentos faturados com comissão configurada neste intervalo.</p>
            </div>

            <div v-else class="cm-cards">
                <div
                    v-for="p in periodo.profissionais"
                    :key="p.profissionalUsuarioId"
                    class="cm-card"
                    :class="{ 'is-open': expandido.has(p.profissionalUsuarioId) }"
                >
                    <!-- Cabeçalho do profissional -->
                    <button
                        class="cm-header"
                        :aria-expanded="expandido.has(p.profissionalUsuarioId)"
                        @click="toggleExpand(p.profissionalUsuarioId)"
                    >
                        <!-- Avatar -->
                        <span
                            class="cm-av"
                            :style="{ background: corAvatar(p.nome) }"
                            aria-hidden="true"
                        >{{ iniciais(p.nome) }}</span>

                        <!-- Info -->
                        <div class="cm-info">
                            <b>{{ p.nome }}</b>
                            <span>{{ p.especialidade ?? "Sem especialidade" }}</span>
                        </div>

                        <!-- Stats -->
                        <div class="cm-stats">
                            <div class="cm-stat">
                                <span>{{ p.atendimentos }}</span>
                                <label>atend.</label>
                            </div>
                            <div class="cm-stat">
                                <span>{{ moeda(p.faturamento) }}</span>
                                <label>faturado</label>
                            </div>
                            <div class="cm-stat">
                                <span class="cm-base" :class="p.percentualConfig === null ? 'is-padrao' : ''">
                                    {{ pct(p.percentualConfig) }}
                                    <i v-if="p.percentualConfig === null" class="fa-solid fa-info-circle" :title="'Percentual padrão do estabelecimento'" />
                                </span>
                                <label>percentual</label>
                            </div>
                        </div>

                        <!-- Total repasse -->
                        <span class="cm-repasse-pill">{{ moeda(p.comissao) }}</span>

                        <!-- Chevron -->
                        <i
                            class="cm-chevron fa-solid"
                            :class="expandido.has(p.profissionalUsuarioId) ? 'fa-chevron-up' : 'fa-chevron-down'"
                            aria-hidden="true"
                        />
                    </button>

                    <!-- Detalhe por atendimento -->
                    <div v-if="expandido.has(p.profissionalUsuarioId)" class="cm-detail">
                        <div class="cm-detail-head">
                            <span>Data</span>
                            <span>Paciente</span>
                            <span>Tipo</span>
                            <span class="ta-r">Faturado</span>
                            <span class="ta-r">Comissão</span>
                        </div>
                        <div
                            v-for="(a, i) in p.atendimentos_Detalhes"
                            :key="i"
                            class="cm-detail-row"
                        >
                            <span class="cm-d-date">{{ formatarData(a.data) }}</span>
                            <span class="cm-d-pac">{{ a.pacienteNome ?? "—" }}</span>
                            <span class="cm-d-tipo">{{ a.tipoAtendimento }}</span>
                            <span class="ta-r">{{ moeda(a.faturamento) }}</span>
                            <span class="ta-r cm-d-val">{{ moeda(a.comissao) }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </template>
    </div>
</template>

<style scoped>
.comissoes-tab { display: flex; flex-direction: column; gap: 1rem; }

/* Barra de período */
.periodo-bar {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}
.sep { font-size: var(--text-sm); color: hsl(var(--secondary) / 0.55); }

/* Total bar */
.cm-total-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: hsl(var(--primary) / 0.05);
    border: 1px solid hsl(var(--primary) / 0.15);
    border-radius: 12px;
    padding: 14px 20px;
}
.cm-total-label {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--c-primary-dark);
}
.cm-total-val {
    font-size: var(--text-xl);
    font-weight: var(--font-weight-extrabold);
    color: hsl(var(--primary));
}

/* Empty */
.cm-empty {
    text-align: center;
    padding: 48px 24px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
}
.cm-empty i { font-size: var(--text-3xl); color: hsl(var(--secondary) / 0.3); }
.cm-empty b  { font-size: var(--text-md); color: var(--c-primary-dark); }
.cm-empty p  { font-size: var(--text-sm); color: hsl(var(--secondary) / 0.55); margin: 0; max-width: 360px; }

/* Cards */
.cm-cards { display: flex; flex-direction: column; gap: 10px; }

.cm-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
    transition: box-shadow 0.15s;
}
.cm-card.is-open { box-shadow: 0 2px 12px hsl(var(--primary) / 0.08); }

.cm-header {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 16px 18px;
    background: transparent;
    border: none;
    cursor: pointer;
    text-align: left;
}
.cm-header:hover { background: hsl(var(--secondary) / 0.03); }

/* Avatar com iniciais */
.cm-av {
    flex-shrink: 0;
    width: 40px;
    height: 40px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-bold);
    color: #fff;
    letter-spacing: 0.5px;
}

/* Info (nome + especialidade) */
.cm-info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 160px;
    flex: 1;
}
.cm-info b {
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: var(--c-primary-dark);
}
.cm-info span { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.6); }

/* Stats */
.cm-stats {
    display: flex;
    gap: 24px;
    flex-shrink: 0;
}
.cm-stat {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 1px;
}
.cm-stat span {
    font-size: var(--text-base);
    font-weight: var(--font-weight-semibold);
    color: var(--c-primary-dark);
    display: inline-flex;
    align-items: center;
    gap: 5px;
}
.cm-stat label {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.55);
}
.cm-base { color: var(--c-primary-dark); }
.cm-base.is-padrao { color: hsl(28 90% 45%); }
.cm-base i { font-size: var(--text-xs); cursor: default; }

/* Pill total repasse */
.cm-repasse-pill {
    flex-shrink: 0;
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary));
    border: 1px solid hsl(var(--primary) / 0.2);
    border-radius: 999px;
    padding: 5px 14px;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-bold);
}

/* Chevron */
.cm-chevron {
    flex-shrink: 0;
    color: hsl(var(--secondary) / 0.5);
    font-size: var(--text-sm);
}

/* Detalhe */
.cm-detail {
    border-top: 1px solid hsl(var(--secondary) / 0.08);
    padding: 0 18px 14px;
}
.cm-detail-head {
    display: grid;
    grid-template-columns: 90px 1fr 130px 130px 110px;
    padding: 8px 0 6px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.55);
}
.cm-detail-row {
    display: grid;
    grid-template-columns: 90px 1fr 130px 130px 110px;
    padding: 8px 0;
    border-bottom: 1px solid hsl(var(--secondary) / 0.04);
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.8);
}
.cm-detail-row:last-child { border-bottom: none; }
.cm-d-date  { color: hsl(var(--secondary) / 0.6); }
.cm-d-pac   { font-weight: var(--font-weight-medium); color: var(--c-primary-dark); }
.cm-d-tipo  { color: hsl(var(--secondary) / 0.7); }
.cm-d-val   { font-weight: var(--font-weight-semibold); color: hsl(var(--primary)); }
.ta-r { text-align: right; }

.info { color: hsl(var(--secondary) / 0.6); font-size: var(--text-sm); }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
