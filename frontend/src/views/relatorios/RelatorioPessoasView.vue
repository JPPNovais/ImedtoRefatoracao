<script setup lang="ts">
import { ref } from "vue"
import {
    relatorioService,
    type RelatorioPessoas,
    type TipoRelatorioPessoas,
} from "@/services/relatorioService"
import { AppPageHeader, AppButton, AppCard, AppField, AppEmptyState } from "@/components/ui"

const dataInicio = ref("")
const dataFim    = ref("")
const tipo       = ref<TipoRelatorioPessoas>("pacientes")

const tiposOpcoes: { valor: TipoRelatorioPessoas; label: string }[] = [
    { valor: "pacientes",                label: "Top 10 pacientes" },
    { valor: "profissionais_performance", label: "Performance de profissionais" },
]

const dados      = ref<RelatorioPessoas | null>(null)
const carregando = ref(false)
const erro       = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await relatorioService.pessoas({
            dataInicio: dataInicio.value || undefined,
            dataFim:    dataFim.value    || undefined,
            tipo:       tipo.value,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar relatorio de pessoas."
    } finally {
        carregando.value = false
    }
}

carregar()

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
</script>

<template>
    <main class="app-page app-page--wide">
        <AppPageHeader
            titulo="Relatorio de Pessoas"
            subtitulo="Top pacientes e performance de profissionais."
        />

        <!-- Filtros -->
        <AppCard padding="sm">
            <div class="filtros-linha">
                <AppField label="De" for="rp-inicio">
                    <input id="rp-inicio" v-model="dataInicio" type="date" class="input-data" />
                </AppField>
                <AppField label="Ate" for="rp-fim">
                    <input id="rp-fim" v-model="dataFim" type="date" class="input-data" />
                </AppField>
                <AppField label="Tipo" for="rp-tipo">
                    <select id="rp-tipo" v-model="tipo" class="input-data">
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
            <!-- Top Pacientes -->
            <AppCard v-if="dados.tipo === 'pacientes'" title="Top 10 pacientes">
                <AppEmptyState
                    v-if="!dados.topPacientes?.length"
                    icone="fa-solid fa-user-group"
                    titulo="Nenhum dado no periodo"
                    descricao="Ajuste os filtros de data."
                    compacto
                />
                <table v-else class="tabela">
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Paciente</th>
                            <th>Consultas</th>
                            <th>Total gasto</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(p, idx) in dados.topPacientes" :key="p.nome">
                            <td class="td-rank">{{ idx + 1 }}</td>
                            <td class="td-nome">{{ p.nome }}</td>
                            <td class="td-num">{{ p.totalConsultas }}</td>
                            <td>{{ moeda(p.totalGasto) }}</td>
                        </tr>
                    </tbody>
                </table>
            </AppCard>

            <!-- Ranking Profissionais -->
            <AppCard v-else-if="dados.tipo === 'profissionais_performance'" title="Performance dos profissionais">
                <AppEmptyState
                    v-if="!dados.rankingProfissionais?.length"
                    icone="fa-solid fa-user-doctor"
                    titulo="Nenhum dado no periodo"
                    descricao="Ajuste os filtros de data."
                    compacto
                />
                <table v-else class="tabela">
                    <thead>
                        <tr>
                            <th>#</th>
                            <th>Profissional</th>
                            <th>Atendimentos</th>
                            <th>Faturamento</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(p, idx) in dados.rankingProfissionais" :key="p.nome">
                            <td class="td-rank">{{ idx + 1 }}</td>
                            <td class="td-nome">{{ p.nome }}</td>
                            <td class="td-num">{{ p.totalAtendimentos }}</td>
                            <td>{{ moeda(p.faturamento) }}</td>
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
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela tr:hover td { background: hsl(var(--muted) / 0.4); }

.td-rank {
    font-weight: 800;
    color: hsl(var(--primary));
    width: 40px;
}
.td-nome  { font-weight: 500; }
.td-num   { text-align: right; }
</style>
