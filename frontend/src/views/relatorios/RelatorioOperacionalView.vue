<script setup lang="ts">
import { ref } from "vue"
import {
    relatorioService,
    type RelatorioOperacional,
    type TipoRelatorioOperacional,
} from "@/services/relatorioService"
import { AppPageHeader, AppButton, AppCard, AppField, AppEmptyState } from "@/components/ui"

const dataInicio = ref("")
const dataFim    = ref("")
const tipo       = ref<TipoRelatorioOperacional>("agenda")

const tiposOpcoes: { valor: TipoRelatorioOperacional; label: string; icone: string }[] = [
    { valor: "agenda",    label: "Agenda",    icone: "fa-solid fa-calendar-days" },
    { valor: "dashboard", label: "Dashboard", icone: "fa-solid fa-gauge" },
    { valor: "inventario", label: "Inventario", icone: "fa-solid fa-box" },
]

const dados      = ref<RelatorioOperacional | null>(null)
const carregando = ref(false)
const erro       = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await relatorioService.operacional({
            dataInicio: dataInicio.value || undefined,
            dataFim:    dataFim.value    || undefined,
            tipo:       tipo.value,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar relatorio operacional."
    } finally {
        carregando.value = false
    }
}

carregar()

function numFmt(n: number, unidade?: string) {
    const fmt = n.toLocaleString("pt-BR")
    return unidade ? `${fmt} ${unidade}` : fmt
}
</script>

<template>
    <main class="app-page app-page--wide">
        <AppPageHeader
            titulo="Relatorio Operacional"
            subtitulo="Indicadores de agenda, atendimentos e estoque."
        />

        <!-- Filtros -->
        <AppCard padding="sm">
            <div class="filtros-linha">
                <AppField label="De" for="ro-inicio">
                    <input id="ro-inicio" v-model="dataInicio" type="date" class="input-data" />
                </AppField>
                <AppField label="Ate" for="ro-fim">
                    <input id="ro-fim" v-model="dataFim" type="date" class="input-data" />
                </AppField>
                <AppField label="Tipo" for="ro-tipo">
                    <select id="ro-tipo" v-model="tipo" class="input-data">
                        <option v-for="o in tiposOpcoes" :key="o.valor" :value="o.valor">{{ o.label }}</option>
                    </select>
                </AppField>
                <div class="filtro-acao">
                    <AppButton icon="fa-solid fa-magnifying-glass" :loading="carregando" @click="carregar">
                        Aplicar
                    </AppButton>
                </div>
            </div>
        </AppCard>

        <div v-if="carregando" class="estado-msg">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando...
        </div>

        <div v-else-if="erro" class="erro-banner" role="alert">
            {{ erro }}
            <AppButton variant="ghost" size="sm" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="dados">
            <!-- KPIs -->
            <div class="kpi-grid">
                <AppCard v-for="kpi in dados.kpis" :key="kpi.rotulo" elevated>
                    <div class="kpi">
                        <span class="kpi-label">{{ kpi.rotulo }}</span>
                        <span class="kpi-valor">{{ numFmt(kpi.valor, kpi.unidade) }}</span>
                    </div>
                </AppCard>
                <div v-if="dados.kpis.length === 0" class="kpi-vazio">
                    Nenhum KPI disponivel para este tipo.
                </div>
            </div>

            <!-- Breakdown -->
            <AppCard title="Detalhamento">
                <AppEmptyState
                    v-if="dados.breakdown.length === 0"
                    icone="fa-solid fa-chart-line"
                    titulo="Nenhum dado no periodo"
                    descricao="Ajuste os filtros e tente novamente."
                    compacto
                />
                <table v-else class="tabela">
                    <thead>
                        <tr>
                            <th>Item</th>
                            <th>Valor</th>
                            <th>Qtd.</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="l in dados.breakdown" :key="l.rotulo">
                            <td>{{ l.rotulo }}</td>
                            <td>{{ l.valor.toLocaleString("pt-BR") }}</td>
                            <td class="td-muted">{{ l.quantidade ?? "—" }}</td>
                        </tr>
                    </tbody>
                </table>
            </AppCard>
        </template>
    </main>
</template>

<style scoped>
.filtros-linha { display: flex; flex-wrap: wrap; gap: 1rem; align-items: flex-end; }
.filtro-acao   { align-self: flex-end; }

.input-data {
    padding: 0.45rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    font-family: inherit;
    font-size: 0.875em;
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    min-width: 140px;
}
.input-data:focus { outline: none; border-color: hsl(var(--primary)); }

.estado-msg {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    font-size: 0.9em;
    padding: 2rem 0;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.9em;
}

.kpi-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 1rem;
}
.kpi { display: flex; flex-direction: column; gap: 0.35rem; padding: 0.25rem 0; }
.kpi-label { font-size: 0.78em; font-weight: 600; color: hsl(var(--muted-foreground)); text-transform: uppercase; letter-spacing: 0.04em; }
.kpi-valor { font-size: 1.4rem; font-weight: 800; color: hsl(var(--primary)); }
.kpi-vazio { color: hsl(var(--muted-foreground)); font-size: 0.9em; padding: 1rem 0; }

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9em;
}
.tabela th {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 2px solid hsl(var(--border));
    font-weight: 600;
    font-size: 0.8em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--muted-foreground));
}
.tabela td {
    padding: 0.55rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela tr:hover td { background: hsl(var(--muted) / 0.4); }
.td-muted { color: hsl(var(--muted-foreground)); }
</style>
