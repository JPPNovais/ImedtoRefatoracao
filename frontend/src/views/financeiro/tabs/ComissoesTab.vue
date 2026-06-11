<script setup lang="ts">
import { ref, onMounted } from "vue"
import { financeiroService, type ComissaoPeriodo, type ComissaoProfissional } from "@/services/financeiroService"
import { AppDatePicker, AppButton } from "@/components/ui"

const props = defineProps<{ ehDono: boolean }>()

function hojeStr() { return new Date().toISOString().split("T")[0] }
function inicioMes() {
    const d = new Date()
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`
}

const dataInicio = ref(inicioMes())
const dataFim = ref(hojeStr())
const periodo = ref<ComissaoPeriodo | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)

// Linhas expansíveis (CA172).
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

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
function pct(n: number | null) {
    if (n === null) return "30% (padrão)"
    return `${n.toFixed(2)}%`
}
function formatarData(s: string) {
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}
</script>

<template>
    <div class="comissoes-tab">
        <div class="periodo-row">
            <AppDatePicker v-model="dataInicio" aria-label="Data início" />
            <span class="sep">até</span>
            <AppDatePicker v-model="dataFim" aria-label="Data fim" />
            <AppButton @click="carregar" :loading="carregando" icon="fa-solid fa-sync">Atualizar</AppButton>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="carregando" class="info">Carregando...</p>

        <div v-if="!carregando && periodo" class="comissoes-content">
            <!-- Total a repassar -->
            <div class="total-repassar">
                <span class="tr-label">Total a repassar no período</span>
                <span class="tr-valor">{{ moeda(periodo.totalARepassar) }}</span>
            </div>

            <p v-if="periodo.profissionais.length === 0" class="vazio">
                Nenhuma comissão calculada no período.
            </p>

            <!-- Tabela expansível por profissional (CA172) -->
            <table v-else class="comissoes-table">
                <thead>
                    <tr>
                        <th style="width: 32px;" />
                        <th>Profissional</th>
                        <th>Especialidade</th>
                        <th class="col-num">Atendimentos</th>
                        <th class="col-num">Faturamento</th>
                        <th class="col-num">Percentual</th>
                        <th class="col-num">Comissão</th>
                    </tr>
                </thead>
                <tbody>
                    <template v-for="p in periodo.profissionais" :key="p.profissionalUsuarioId">
                        <!-- Linha do profissional -->
                        <tr class="prof-row" @click="toggleExpand(p.profissionalUsuarioId)">
                            <td>
                                <button
                                    class="expand-btn"
                                    :aria-label="expandido.has(p.profissionalUsuarioId) ? 'Recolher' : 'Expandir'"
                                    :aria-expanded="expandido.has(p.profissionalUsuarioId)"
                                >
                                    <i :class="expandido.has(p.profissionalUsuarioId)
                                        ? 'fa-solid fa-chevron-up'
                                        : 'fa-solid fa-chevron-down'" />
                                </button>
                            </td>
                            <td class="prof-nome">{{ p.nome }}</td>
                            <td>{{ p.especialidade ?? "—" }}</td>
                            <td class="col-num">{{ p.atendimentos }}</td>
                            <td class="col-num">{{ moeda(p.faturamento) }}</td>
                            <td class="col-num">{{ pct(p.percentualConfig) }}</td>
                            <td class="col-num comissao-valor">{{ moeda(p.comissao) }}</td>
                        </tr>
                        <!-- Detalhe por atendimento -->
                        <template v-if="expandido.has(p.profissionalUsuarioId)">
                            <tr
                                v-for="(a, i) in p.atendimentos_Detalhes"
                                :key="i"
                                class="detalhe-row"
                            >
                                <td />
                                <td colspan="2" class="detalhe-pac">
                                    {{ a.pacienteNome ?? "—" }}
                                    <span class="detalhe-tipo">· {{ a.tipoAtendimento }}</span>
                                </td>
                                <td class="col-num">{{ formatarData(a.data) }}</td>
                                <td class="col-num">{{ moeda(a.faturamento) }}</td>
                                <td class="col-num">
                                    {{ a.tipoBase === 'orcamento_equipe' ? 'Valor fixo' : pct(p.percentualConfig) }}
                                </td>
                                <td class="col-num comissao-valor">{{ moeda(a.comissao) }}</td>
                            </tr>
                        </template>
                    </template>
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan="6" class="total-foot-label">Total</td>
                        <td class="col-num comissao-valor total-foot-valor">
                            {{ moeda(periodo.totalARepassar) }}
                        </td>
                    </tr>
                </tfoot>
            </table>
        </div>
    </div>
</template>

<style scoped>
.comissoes-tab { display: flex; flex-direction: column; gap: 1.25rem; }

.periodo-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}
.sep { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }

.total-repassar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    padding: 0.85rem 1.25rem;
}
.tr-label { font-size: var(--text-sm); color: hsl(var(--muted-foreground)); font-weight: var(--font-weight-medium); }
.tr-valor  { font-size: var(--text-xl); font-weight: var(--font-weight-bold); color: hsl(var(--primary)); }

.comissoes-table { width: 100%; border-collapse: collapse; font-size: var(--text-sm); }
.comissoes-table th {
    background: hsl(var(--muted));
    text-align: left;
    padding: 0.45rem 0.75rem;
    border-bottom: 2px solid hsl(var(--border));
    font-weight: var(--font-weight-semibold);
}
.col-num { text-align: right; }

.prof-row {
    cursor: pointer;
    background: hsl(var(--background));
}
.prof-row:hover { background: hsl(var(--muted) / 0.4); }
.prof-row td { padding: 0.6rem 0.75rem; border-bottom: 1px solid hsl(var(--border)); font-weight: var(--font-weight-medium); }
.prof-nome { font-weight: var(--font-weight-semibold); }

.detalhe-row td {
    padding: 0.4rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
    background: hsl(var(--muted) / 0.3);
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
}
.detalhe-pac { font-weight: var(--font-weight-medium); color: hsl(var(--foreground)); }
.detalhe-tipo { font-weight: normal; color: hsl(var(--muted-foreground)); }

.comissao-valor { color: hsl(var(--primary)); font-weight: var(--font-weight-semibold); }
.total-foot-label { text-align: right; font-weight: var(--font-weight-semibold); padding: 0.6rem 0.75rem; background: hsl(var(--muted)); }
.total-foot-valor { padding: 0.6rem 0.75rem; background: hsl(var(--muted)); font-size: var(--text-base); }

.expand-btn {
    border: none;
    background: transparent;
    cursor: pointer;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
    padding: 0.15rem;
}
.expand-btn:hover { color: hsl(var(--foreground)); }

.info, .vazio { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
