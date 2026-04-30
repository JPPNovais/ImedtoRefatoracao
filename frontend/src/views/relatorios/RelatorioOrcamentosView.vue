<script setup lang="ts">
import { ref } from "vue"
import { relatorioService, type RelatorioOrcamentos } from "@/services/relatorioService"
import { AppPageHeader, AppButton, AppCard, AppField, AppEmptyState } from "@/components/ui"

const dataInicio = ref("")
const dataFim    = ref("")

const dados      = ref<RelatorioOrcamentos | null>(null)
const carregando = ref(false)
const erro       = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await relatorioService.orcamentos({
            dataInicio: dataInicio.value || undefined,
            dataFim:    dataFim.value    || undefined,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar relatorio de orcamentos."
    } finally {
        carregando.value = false
    }
}

carregar()

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function pct(n: number) {
    return `${(n * 100).toFixed(1)}%`
}

// Passos do funil
const etapasFunil = (f: RelatorioOrcamentos["funil"]) => [
    { label: "Criados",   valor: f.totalCriados,   cor: "#6366f1" },
    { label: "Enviados",  valor: f.totalEnviados,   cor: "#3b82f6" },
    { label: "Aprovados", valor: f.totalAprovados,  cor: "#22c55e" },
    { label: "Recusados", valor: f.totalRecusados,  cor: "#ef4444" },
]
</script>

<template>
    <main class="app-page app-page--wide">
        <AppPageHeader
            titulo="Relatorio de Orcamentos"
            subtitulo="Funil de conversao e valor medio dos orcamentos."
        />

        <!-- Filtros -->
        <AppCard padding="sm">
            <div class="filtros-linha">
                <AppField label="De" for="ro2-inicio">
                    <input id="ro2-inicio" v-model="dataInicio" type="date" class="input-data" />
                </AppField>
                <AppField label="Ate" for="ro2-fim">
                    <input id="ro2-fim" v-model="dataFim" type="date" class="input-data" />
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
            <!-- KPIs de conversao -->
            <div class="kpi-grid">
                <AppCard elevated>
                    <div class="kpi">
                        <span class="kpi-label">Taxa de conversao</span>
                        <span class="kpi-valor kpi-valor--primary">{{ pct(dados.funil.taxaConversao) }}</span>
                    </div>
                </AppCard>
                <AppCard elevated>
                    <div class="kpi">
                        <span class="kpi-label">Valor medio aprovado</span>
                        <span class="kpi-valor">{{ moeda(dados.funil.valorMedioAprovado) }}</span>
                    </div>
                </AppCard>
                <AppCard elevated>
                    <div class="kpi">
                        <span class="kpi-label">Total aprovados</span>
                        <span class="kpi-valor kpi-valor--success">{{ dados.funil.totalAprovados }}</span>
                    </div>
                </AppCard>
            </div>

            <!-- Funil visual -->
            <AppCard title="Funil de conversao">
                <AppEmptyState
                    v-if="dados.funil.totalCriados === 0"
                    icone="fa-solid fa-filter"
                    titulo="Nenhum orcamento no periodo"
                    descricao="Ajuste os filtros de data."
                    compacto
                />
                <div v-else class="funil-wrapper">
                    <div
                        v-for="etapa in etapasFunil(dados.funil)"
                        :key="etapa.label"
                        class="funil-etapa"
                    >
                        <span class="funil-label">{{ etapa.label }}</span>
                        <div class="funil-barra-wrapper">
                            <div
                                class="funil-barra"
                                :style="{
                                    width: dados.funil.totalCriados
                                        ? Math.max(4, Math.round((etapa.valor / dados.funil.totalCriados) * 100)) + '%'
                                        : '4px',
                                    background: etapa.cor,
                                }"
                                :aria-label="`${etapa.label}: ${etapa.valor}`"
                            ></div>
                            <span class="funil-valor">{{ etapa.valor }}</span>
                        </div>
                    </div>
                </div>
            </AppCard>

            <!-- Breakdown adicional -->
            <AppCard v-if="dados.breakdown.length > 0" title="Detalhamento">
                <table class="tabela">
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
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
}
.kpi { display: flex; flex-direction: column; gap: 0.35rem; padding: 0.25rem 0; }
.kpi-label { font-size: 0.78em; font-weight: 600; color: hsl(var(--muted-foreground)); text-transform: uppercase; letter-spacing: 0.04em; }
.kpi-valor { font-size: 1.5rem; font-weight: 800; }
.kpi-valor--primary { color: hsl(var(--primary)); }
.kpi-valor--success { color: hsl(var(--success)); }

.funil-wrapper {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}
.funil-etapa { display: flex; align-items: center; gap: 0.75rem; }
.funil-label { width: 90px; flex-shrink: 0; font-size: 0.85em; font-weight: 600; color: hsl(var(--foreground)); }
.funil-barra-wrapper { display: flex; align-items: center; gap: 0.5rem; flex: 1; }
.funil-barra { height: 28px; border-radius: var(--radius-sm); transition: width 0.4s ease; min-width: 4px; }
.funil-valor { font-size: 0.85em; font-weight: 700; color: hsl(var(--foreground)); flex-shrink: 0; }

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
