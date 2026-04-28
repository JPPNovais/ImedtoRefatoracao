<script setup lang="ts">
import { ref, onMounted } from "vue"
import { relatorioService, type FaturamentoCategoria, type RelatorioAgendamentos } from "@/services/dashboardService"
import { useRelatorioPdf } from "@/composables/useRelatorioPdf"
import { AppButton } from "@/components/ui"

const { gerarFaturamentoPdf, gerarAgendamentosPdf, exportarFaturamentoCsv, exportarAgendamentosCsv } = useRelatorioPdf()

// --- Período compartilhado ---
const dataInicio = ref("")
const dataFim = ref("")

// --- Faturamento ---
const faturamento = ref<FaturamentoCategoria[]>([])
const carregandoFat = ref(false)
const erroFat = ref<string | null>(null)

// --- Agendamentos ---
const relAgend = ref<RelatorioAgendamentos | null>(null)
const carregandoAgend = ref(false)
const erroAgend = ref<string | null>(null)

async function carregarFaturamento() {
    carregandoFat.value = true
    erroFat.value = null
    try {
        faturamento.value = await relatorioService.faturamento({
            dataInicio: dataInicio.value || undefined,
            dataFim: dataFim.value || undefined,
        })
    } catch (e: any) {
        erroFat.value = e?.response?.data?.mensagem ?? "Erro ao carregar relatório."
    } finally {
        carregandoFat.value = false
    }
}

async function carregarAgendamentos() {
    carregandoAgend.value = true
    erroAgend.value = null
    try {
        relAgend.value = await relatorioService.agendamentos({
            dataInicio: dataInicio.value || undefined,
            dataFim: dataFim.value || undefined,
        })
    } catch (e: any) {
        erroAgend.value = e?.response?.data?.mensagem ?? "Erro ao carregar relatório."
    } finally {
        carregandoAgend.value = false
    }
}

async function aplicar() {
    await Promise.all([carregarFaturamento(), carregarAgendamentos()])
}

onMounted(aplicar)

// --- Helpers ---
function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function percentual(n: number, total: number) {
    if (total === 0) return "0%"
    return `${Math.round((n / total) * 100)}%`
}

const totalReceitasPagas = () =>
    faturamento.value.filter(f => f.tipo === "Receita").reduce((s, f) => s + f.totalPago, 0)

const totalDespesasPagas = () =>
    faturamento.value.filter(f => f.tipo === "Despesa").reduce((s, f) => s + f.totalPago, 0)

const statusCor: Record<string, string> = {
    Agendado: "#3b82f6",
    Confirmado: "#10b981",
    Cancelado: "#ef4444",
    Concluido: "#6b7280",
}
</script>

<template>
    <main class="app-page app-page--wide relatorios">
        <header class="page-header">
            <div>
                <h1 class="page-titulo">Relatórios</h1>
                <p class="page-sub">Análises de faturamento e agendamentos por período.</p>
            </div>
        </header>

        <!-- Card com filtro de período -->
        <section class="filtros-card">
            <div class="filtros-grid">
                <div class="filtro-grupo">
                    <label class="campo-label">De</label>
                    <input type="date" v-model="dataInicio" class="input-field" />
                </div>
                <div class="filtro-grupo">
                    <label class="campo-label">Até</label>
                    <input type="date" v-model="dataFim" class="input-field" />
                </div>
                <div class="filtro-grupo filtro-grupo--acao">
                    <AppButton @click="aplicar">Aplicar</AppButton>
                </div>
            </div>
            <span class="hint" v-if="!dataInicio && !dataFim">Sem filtro = todos os períodos</span>
        </section>

        <div class="grid-relatorios">
            <!-- Faturamento por categoria -->
            <section class="bloco">
                <div class="bloco-header">
                    <h2>Faturamento por categoria</h2>
                    <div class="bloco-acoes" v-if="!carregandoFat && faturamento.length > 0">
                        <AppButton variant="ghost" size="sm" @click="gerarFaturamentoPdf(faturamento, { dataInicio, dataFim })">⬇ PDF</AppButton>
                        <AppButton variant="ghost" size="sm" @click="exportarFaturamentoCsv(faturamento)">⬇ CSV</AppButton>
                    </div>
                </div>
                <p v-if="carregandoFat" class="info">Carregando...</p>
                <p v-if="erroFat" class="erro">{{ erroFat }}</p>

                <div v-if="!carregandoFat && faturamento.length > 0">
                    <div class="resumo-fat">
                        <span class="receita-total">Receitas: {{ moeda(totalReceitasPagas()) }}</span>
                        <span class="despesa-total">Despesas: {{ moeda(totalDespesasPagas()) }}</span>
                        <span :class="totalReceitasPagas() - totalDespesasPagas() >= 0 ? 'saldo-pos' : 'saldo-neg'">
                            Saldo: {{ moeda(totalReceitasPagas() - totalDespesasPagas()) }}
                        </span>
                    </div>
                    <table>
                        <thead>
                            <tr>
                                <th>Tipo</th>
                                <th>Categoria</th>
                                <th>Qtd.</th>
                                <th>Total pago</th>
                                <th>Total pendente</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="row in faturamento" :key="`${row.tipo}-${row.categoria}`"
                                :class="row.tipo === 'Receita' ? 'row-receita' : 'row-despesa'">
                                <td>
                                    <span :class="row.tipo === 'Receita' ? 'badge-receita' : 'badge-despesa'">
                                        {{ row.tipo }}
                                    </span>
                                </td>
                                <td>{{ row.categoria }}</td>
                                <td>{{ row.quantidade }}</td>
                                <td>{{ moeda(row.totalPago) }}</td>
                                <td>{{ moeda(row.totalPendente) }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <p v-else-if="!carregandoFat" class="vazio">Nenhum lançamento no período.</p>
            </section>

            <!-- Agendamentos -->
            <section class="bloco">
                <div class="bloco-header">
                    <h2>Agendamentos</h2>
                    <div class="bloco-acoes" v-if="!carregandoAgend && relAgend">
                        <AppButton variant="ghost" size="sm" @click="gerarAgendamentosPdf(relAgend!, { dataInicio, dataFim })">⬇ PDF</AppButton>
                        <AppButton variant="ghost" size="sm" @click="exportarAgendamentosCsv(relAgend!)">⬇ CSV</AppButton>
                    </div>
                </div>
                <p v-if="carregandoAgend" class="info">Carregando...</p>
                <p v-if="erroAgend" class="erro">{{ erroAgend }}</p>

                <div v-if="!carregandoAgend && relAgend">
                    <p class="total-agend">Total: <strong>{{ relAgend.total }}</strong></p>

                    <!-- Por status -->
                    <h4>Por status</h4>
                    <div class="barras">
                        <div v-for="s in relAgend.porStatus" :key="s.status" class="barra-row">
                            <span class="barra-label">{{ s.status }}</span>
                            <div class="barra-track">
                                <div class="barra-fill"
                                    :style="{
                                        width: percentual(s.quantidade, relAgend!.total),
                                        background: statusCor[s.status] ?? '#94a3b8'
                                    }">
                                </div>
                            </div>
                            <span class="barra-valor">{{ s.quantidade }} ({{ percentual(s.quantidade, relAgend!.total) }})</span>
                        </div>
                    </div>

                    <!-- Por dia -->
                    <h4 v-if="relAgend.porDia.length > 0">Por dia</h4>
                    <table v-if="relAgend.porDia.length > 0" class="tabela-compacta">
                        <thead>
                            <tr><th>Data</th><th>Qtd.</th></tr>
                        </thead>
                        <tbody>
                            <tr v-for="d in relAgend.porDia" :key="d.data">
                                <td>{{ new Date(d.data).toLocaleDateString("pt-BR") }}</td>
                                <td>{{ d.quantidade }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <p v-else-if="!carregandoAgend" class="vazio">Nenhum agendamento no período.</p>
            </section>
        </div>
    </main>
</template>

<style scoped>

.page-header { margin-bottom: 1.25rem; }
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

.filtros-card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1rem 1.25rem; margin-bottom: 1.25rem;
}
.filtros-grid {
    display: grid; gap: 1rem; align-items: end;
    grid-template-columns: 180px 180px auto;
}
.filtro-grupo { display: flex; flex-direction: column; gap: 0.25rem; }
.filtro-grupo--acao { justify-content: flex-end; }

.campo-label { font-size: 0.78em; font-weight: 600; color: var(--text-muted); }

.input-field {
    padding: 0.45rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text);
}
.input-field:focus { outline: none; border-color: var(--primary); }

.hint { font-size: 0.78em; color: var(--text-faint); display: block; margin-top: 0.5rem; }
h2 { margin: 0 0 1rem 0; font-size: 1.1rem; }
h4 { margin: 1rem 0 0.5rem; font-size: 0.9rem; color: #374151; }

.filtro-periodo { display: flex; gap: 1rem; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; }
.filtro-periodo label { display: flex; align-items: center; gap: 0.4rem; font-size: 0.9em; }
.filtro-periodo input { padding: 0.3rem 0.6rem; border: 1px solid #ccc; border-radius: 4px; }
.hint { font-size: 0.82em; color: #9ca3af; }

.grid-relatorios { display: grid; grid-template-columns: repeat(auto-fit, minmax(460px, 1fr)); gap: 1.5rem; }
.bloco { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 1.25rem; }
.bloco-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
.bloco-header h2 { margin: 0; }
.bloco-acoes { display: flex; gap: 0.4rem; }

.resumo-fat { display: flex; gap: 1.5rem; margin-bottom: 1rem; flex-wrap: wrap; font-size: 0.9em; font-weight: 600; }
.receita-total { color: #059669; }
.despesa-total { color: #dc2626; }
.saldo-pos { color: #059669; }
.saldo-neg { color: #dc2626; }

table { width: 100%; border-collapse: collapse; font-size: 0.88em; }
th { background: #f3f4f6; text-align: left; padding: 0.4rem 0.6rem; border-bottom: 2px solid #e5e7eb; }
td { padding: 0.4rem 0.6rem; border-bottom: 1px solid #f0f0f0; }
tr:hover { background: #f9fafb; }
.badge-receita { background: #d1fae5; color: #065f46; padding: 0.1rem 0.4rem; border-radius: 999px; font-size: 0.8em; }
.badge-despesa { background: #fee2e2; color: #991b1b; padding: 0.1rem 0.4rem; border-radius: 999px; font-size: 0.8em; }

.total-agend { margin: 0 0 1rem; font-size: 0.9em; color: #374151; }
.barras { display: flex; flex-direction: column; gap: 0.6rem; }
.barra-row { display: flex; align-items: center; gap: 0.6rem; font-size: 0.85em; }
.barra-label { width: 90px; flex-shrink: 0; color: #374151; }
.barra-track { flex: 1; height: 16px; background: #f3f4f6; border-radius: 8px; overflow: hidden; }
.barra-fill { height: 100%; border-radius: 8px; transition: width 0.3s; }
.barra-valor { width: 100px; text-align: right; color: #6b7280; }
.tabela-compacta { font-size: 0.85em; }

.info { color: #9ca3af; font-size: 0.9em; }
.erro { color: #b00020; font-size: 0.9em; }
.vazio { color: #9ca3af; font-style: italic; }
</style>
