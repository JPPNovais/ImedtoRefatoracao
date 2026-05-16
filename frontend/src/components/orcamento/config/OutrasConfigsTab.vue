<script setup lang="ts">
/**
 * Aba "Outras configurações" — D6 do plano 2026-05-16.
 * Agrupa Local cirurgia, Implantes, Pagamento e Equipes legado em uma única
 * tela transitória, mantendo as funcionalidades enquanto o OrcamentoFormView
 * não é reescrito.
 */
import { ref, onMounted } from "vue"
import { AppTabs, AppEmptyState, AppButton, AppField, AppInput, AppSelect, AppStatusPill } from "@/components/ui"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type ConfiguracaoLocalCirurgia, type CatalogoImplante, type ConfiguracaoPagamentoCatalogo,
    type CatalogoEquipe,
} from "@/services/orcamentoCatalogoService"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"

type SubAba = "local" | "implantes" | "equipes" | "pagamento"
const subAba = ref<SubAba>("local")

const subAbas = [
    { valor: "local",     label: "Local cirurgia", icone: "fa-solid fa-hospital" },
    { valor: "implantes", label: "Implantes",      icone: "fa-solid fa-microchip" },
    { valor: "equipes",   label: "Equipes legado", icone: "fa-solid fa-users-line" },
    { valor: "pagamento", label: "Pagamento",      icone: "fa-solid fa-credit-card" },
]

const locais = ref<ConfiguracaoLocalCirurgia[]>([])
const implantes = ref<CatalogoImplante[]>([])
const equipes = ref<CatalogoEquipe[]>([])
const pagamentos = ref<ConfiguracaoPagamentoCatalogo[]>([])
const formasPagamento = ref<FormaPagamento[]>([])

async function carregarTudo() {
    const [l, i, e, p, fp] = await Promise.all([
        orcamentoCatalogoService.listarLocais(),
        orcamentoCatalogoService.listarImplantes(),
        orcamentoCatalogoService.listarEquipes(),
        orcamentoCatalogoService.listarConfigPagamento(),
        formaPagamentoService.listar(),
    ])
    locais.value = l; implantes.value = i; equipes.value = e; pagamentos.value = p
    formasPagamento.value = fp
}

onMounted(carregarTudo)
</script>

<template>
    <div class="outras-tab">
        <div class="warning">
            <i class="fa-solid fa-info-circle"></i>
            Esta aba agrupa configurações transitórias que ainda são consumidas pelo formulário antigo de orçamento.
            Em breve serão integradas às demais abas.
        </div>

        <AppTabs v-model="subAba" :abas="subAbas" variante="sub" />

        <!-- LOCAL CIRURGIA -->
        <div v-if="subAba === 'local'">
            <h3>Locais de cirurgia configurados</h3>
            <div v-if="locais.length" class="simple-table">
                <div v-for="l in locais" :key="l.id" class="row">
                    <strong>{{ l.tipoInternacao }}</strong>
                    <span>{{ l.tempoBaseMinutos }} min base + {{ l.tempoAdicionalMinutos }} min adicional</span>
                    <span>{{ formatarMoedaBrl(l.valorBase) }} + {{ formatarMoedaBrl(l.valorAdicional) }}</span>
                </div>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-hospital" titulo="Nenhum local configurado"
                descricao="A configuração de tempo/valor por tipo de internação é editada no formulário antigo de orçamento." />
        </div>

        <!-- IMPLANTES -->
        <div v-if="subAba === 'implantes'">
            <h3>Implantes ({{ implantes.length }})</h3>
            <div v-if="implantes.length" class="simple-table">
                <div v-for="i in implantes" :key="i.id" class="row">
                    <strong>{{ i.descricao }}</strong>
                    <span class="muted">{{ i.itemInventarioNome ?? "Sem item de inventário" }}</span>
                    <span>{{ formatarMoedaBrl(i.custoUnitario) }}</span>
                    <AppStatusPill :label="i.ativo ? 'Ativo' : 'Inativo'" :variante="i.ativo ? 'success' : 'muted'" />
                </div>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-microchip" titulo="Nenhum implante cadastrado"
                descricao="Implantes seguem sendo gerenciados no formulário antigo de orçamento até a migração da feature." />
        </div>

        <!-- EQUIPES LEGADO -->
        <div v-if="subAba === 'equipes'">
            <h3>Equipes especializadas ({{ equipes.length }})</h3>
            <p class="hint">
                Modelo antigo de equipe (descrição + valor padrão). O modelo novo está na aba <strong>Equipe</strong> (papéis e honorários).
            </p>
            <div v-if="equipes.length" class="simple-table">
                <div v-for="e in equipes" :key="e.id" class="row">
                    <strong>{{ e.descricao }}</strong>
                    <span>{{ formatarMoedaBrl(e.valorPadrao) }}</span>
                    <AppStatusPill :label="e.ativo ? 'Ativo' : 'Inativo'" :variante="e.ativo ? 'success' : 'muted'" />
                </div>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-users-line" titulo="Nenhuma equipe legado" />
        </div>

        <!-- PAGAMENTO -->
        <div v-if="subAba === 'pagamento'">
            <h3>Configurações de pagamento ({{ pagamentos.length }})</h3>
            <div v-if="pagamentos.length" class="simple-table">
                <div v-for="p in pagamentos" :key="p.id" class="row">
                    <strong>{{ p.formaPagamentoNome ?? `Forma #${p.formaPagamentoId}` }}</strong>
                    <span>Acréscimo: {{ p.acrescimoPercentual }}%</span>
                    <span>Entrada: {{ p.entradaPercentualPadrao }}%</span>
                    <span>Até {{ p.parcelasMaximas }}x</span>
                    <AppStatusPill :label="p.ativo ? 'Ativo' : 'Inativo'" :variante="p.ativo ? 'success' : 'muted'" />
                </div>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-credit-card" titulo="Nenhuma configuração de pagamento"
                descricao="Configure descontos e parcelamento por forma de pagamento no formulário antigo até a migração." />
        </div>
    </div>
</template>

<style scoped>
.outras-tab { display: flex; flex-direction: column; gap: 16px; }
.warning {
    background: hsl(var(--info) / 0.08);
    border-left: 3px solid hsl(var(--info));
    padding: 12px 16px;
    border-radius: 6px;
    color: hsl(var(--secondary) / 0.8);
    font-size: 13px;
}
.warning i { color: hsl(var(--info)); margin-right: 8px; }
h3 { margin: 0 0 12px 0; font-size: 15px; font-weight: 600; }
.simple-table {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
}
.row {
    display: flex; align-items: center; gap: 16px;
    padding: 10px 16px;
    border-top: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 14px;
}
.row:first-child { border-top: none; }
.row strong { flex: 1; min-width: 200px; }
.row .muted { color: hsl(var(--secondary) / 0.6); font-size: 13px; }
.hint { font-size: 13px; color: hsl(var(--secondary) / 0.6); margin-bottom: 12px; }
</style>
