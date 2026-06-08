<script setup lang="ts">
/**
 * Aba "Outras configurações" — D6 do plano 2026-05-16.
 * Agrupa Local cirurgia, Implantes, Pagamento e Equipes legado em uma única
 * tela transitória, mantendo as funcionalidades enquanto o OrcamentoFormView
 * não é reescrito.
 */
import { reactive, ref, onMounted, watch } from "vue"
import { AppTabs, AppEmptyState, AppButton, AppField, AppInput, AppSelect, AppStatusPill, AppToast } from "@/components/ui"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type ConfiguracaoLocalCirurgia, type CatalogoImplante, type ConfiguracaoPagamentoCatalogo,
    type CatalogoEquipe, type TipoLocalCirurgiaCatalogo,
} from "@/services/orcamentoCatalogoService"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"

interface OpcaoLocal {
    tipo: TipoLocalCirurgiaCatalogo
    label: string
    descricao: string
    cobraPorTempo: boolean
}
const OPCOES_LOCAL: OpcaoLocal[] = [
    { tipo: "IntLocal",      label: "Anestesia Local + Sedação", descricao: "Com Internação", cobraPorTempo: true },
    { tipo: "IntPeridural",  label: "Peridural/Raqui + Sedação", descricao: "Com Internação", cobraPorTempo: true },
    { tipo: "IntGeral",      label: "Anestesia Geral + TOT",     descricao: "Com Internação", cobraPorTempo: true },
    { tipo: "SemInternacao", label: "Anestesia Local",           descricao: "Sem Internação", cobraPorTempo: false },
    { tipo: "Ambulatorio",   label: "Anestesia Local",           descricao: "Ambulatório",    cobraPorTempo: false },
]

interface CampoLocal { tempoBaseMinutos: number; valorBase: number; tempoAdicionalMinutos: number; valorAdicional: number }
const camposVazios = (): CampoLocal => ({ tempoBaseMinutos: 60, valorBase: 0, tempoAdicionalMinutos: 30, valorAdicional: 0 })
const formularioLocal = reactive<Record<TipoLocalCirurgiaCatalogo, CampoLocal>>({
    IntLocal: camposVazios(),
    IntPeridural: camposVazios(),
    IntGeral: camposVazios(),
    SemInternacao: { tempoBaseMinutos: 1, valorBase: 0, tempoAdicionalMinutos: 1, valorAdicional: 0 },
    Ambulatorio: { tempoBaseMinutos: 1, valorBase: 0, tempoAdicionalMinutos: 1, valorAdicional: 0 },
})
const salvandoLocal = reactive<Record<TipoLocalCirurgiaCatalogo, boolean>>({
    IntLocal: false, IntPeridural: false, IntGeral: false, SemInternacao: false, Ambulatorio: false,
})

// Toast (substitui window.alert).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}

function hidratarFormularioLocal() {
    for (const l of locais.value) {
        formularioLocal[l.tipoLocal] = {
            tempoBaseMinutos: l.tempoBaseMinutos,
            valorBase: Number(l.valorBase),
            tempoAdicionalMinutos: l.tempoAdicionalMinutos,
            valorAdicional: Number(l.valorAdicional),
        }
    }
}

async function salvarLocal(tipo: TipoLocalCirurgiaCatalogo) {
    salvandoLocal[tipo] = true
    try {
        const f = formularioLocal[tipo]
        await orcamentoCatalogoService.salvarLocal(tipo, {
            tempoBaseMinutos: f.tempoBaseMinutos,
            valorBase: f.valorBase,
            tempoAdicionalMinutos: f.tempoAdicionalMinutos,
            valorAdicional: f.valorAdicional,
        })
        const novos = await orcamentoCatalogoService.listarLocais()
        locais.value = novos
        notificar("Configuração de local cirúrgico salva.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao salvar configuração de local.", "error")
    } finally {
        salvandoLocal[tipo] = false
    }
}

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
    hidratarFormularioLocal()
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

        <!-- LOCAL CIRURGIA — editor inline dos 5 tipos -->
        <div v-if="subAba === 'local'">
            <h3>Local cirúrgico — valores por tipo</h3>
            <p class="hint">
                Para os tipos <strong>Sem Internação</strong> e <strong>Ambulatório</strong> o valor é fixo
                (independe do tempo). Os demais 3 tipos cobram por tempo: <em>valor base</em> até <em>tempo base</em>;
                cada bloco adicional de <em>tempo adicional</em> minutos soma <em>valor adicional</em>.
            </p>
            <div class="locais-grid">
                <div v-for="opc in OPCOES_LOCAL" :key="opc.tipo" class="local-card">
                    <div class="local-card-head">
                        <strong>{{ opc.label }}</strong>
                        <small>{{ opc.descricao }}</small>
                    </div>
                    <div class="local-card-fields">
                        <label>
                            Valor base (R$)
                            <input type="number" step="0.01" v-model.number="formularioLocal[opc.tipo].valorBase" />
                        </label>
                        <template v-if="opc.cobraPorTempo">
                            <label>
                                Tempo base (min)
                                <input type="number" min="1" v-model.number="formularioLocal[opc.tipo].tempoBaseMinutos" />
                            </label>
                            <label>
                                Tempo adicional (min)
                                <input type="number" min="1" v-model.number="formularioLocal[opc.tipo].tempoAdicionalMinutos" />
                            </label>
                            <label>
                                Valor adicional (R$)
                                <input type="number" step="0.01" v-model.number="formularioLocal[opc.tipo].valorAdicional" />
                            </label>
                        </template>
                    </div>
                    <div class="local-card-acoes">
                        <AppButton size="sm" :loading="salvandoLocal[opc.tipo]"
                                   @click="salvarLocal(opc.tipo)">
                            Salvar
                        </AppButton>
                        <small v-if="locais.find(l => l.tipoLocal === opc.tipo)" class="status">
                            Configurado em
                            {{ new Date(locais.find(l => l.tipoLocal === opc.tipo)!.atualizadaEm
                                       ?? locais.find(l => l.tipoLocal === opc.tipo)!.criadaEm)
                                .toLocaleDateString("pt-BR") }}
                        </small>
                    </div>
                </div>
            </div>
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

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
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
h3 { margin: 0 0 12px 0; font-size: var(--text-md); font-weight: var(--font-weight-semibold); }
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

/* Locais cirúrgicos — cards editáveis */
.locais-grid {
    display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 14px;
}
.local-card {
    background: hsl(var(--card)); border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 10px; padding: 14px; display: flex; flex-direction: column; gap: 10px;
}
.local-card-head strong { display: block; font-size: 14px; color: hsl(var(--primary)); }
.local-card-head small { color: hsl(var(--secondary) / 0.6); font-size: 11px; }
.local-card-fields { display: flex; flex-direction: column; gap: 8px; }
.local-card-fields label {
    display: flex; flex-direction: column; gap: 4px;
    font-size: 11px; color: hsl(var(--secondary) / 0.7);
}
.local-card-fields input {
    padding: 6px 8px; border: 1px solid hsl(var(--secondary) / 0.15); border-radius: 5px;
    font-size: 14px; font-family: inherit;
}
.local-card-acoes {
    display: flex; align-items: center; justify-content: space-between; gap: 8px;
    border-top: 1px solid hsl(var(--secondary) / 0.08); padding-top: 8px;
}
.local-card-acoes .status { color: hsl(var(--secondary) / 0.55); font-size: 11px; }
</style>
