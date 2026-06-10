<script setup lang="ts">
/**
 * Aba "Outras configurações" — D6 do plano 2026-05-16.
 * Agrupa Local cirurgia, Implantes, Equipes legado e Pagamento.
 * Implantes, Equipes e Pagamento são editáveis (drawer + confirm + toast).
 * Local cirurgia permanece com editor inline já existente.
 */
import { reactive, ref, computed, onMounted } from "vue"
import {
    AppTabs, AppEmptyState, AppButton, AppField, AppInput, AppInputDecimal,
    AppSelect, AppStatusPill, AppToast, AppDrawer, AppConfirmDialog,
} from "@/components/ui"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type ConfiguracaoLocalCirurgia, type CatalogoImplante, type ConfiguracaoPagamentoCatalogo,
    type CatalogoEquipe, type TipoLocalCirurgiaCatalogo,
    type CatalogoImplantePayload, type CatalogoEquipePayload,
    type CriarConfigPagamentoPayload, type AtualizarConfigPagamentoPayload,
} from "@/services/orcamentoCatalogoService"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"
import { inventarioService, type ItemInventario } from "@/services/inventarioService"

// ─── Sub-abas ────────────────────────────────────────────────────────────────
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
        locais.value = await orcamentoCatalogoService.listarLocais()
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

// ─── Dados ───────────────────────────────────────────────────────────────────
const locais = ref<ConfiguracaoLocalCirurgia[]>([])
const implantes = ref<CatalogoImplante[]>([])
const equipes = ref<CatalogoEquipe[]>([])
const pagamentos = ref<ConfiguracaoPagamentoCatalogo[]>([])
const formasPagamento = ref<FormaPagamento[]>([])
const itensInventario = ref<ItemInventario[]>([])

// ─── Toast ────────────────────────────────────────────────────────────────────
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}

// ─── Carregamento ─────────────────────────────────────────────────────────────
async function carregarTudo() {
    const [l, i, e, p, fp, inv] = await Promise.all([
        orcamentoCatalogoService.listarLocais(),
        orcamentoCatalogoService.listarImplantes(),
        orcamentoCatalogoService.listarEquipes(),
        orcamentoCatalogoService.listarConfigPagamento(),
        formaPagamentoService.listar(),
        inventarioService.listarItens({ apenasAtivos: true, tamanho: 500 }),
    ])
    locais.value = l
    implantes.value = i
    equipes.value = e
    pagamentos.value = p
    formasPagamento.value = fp
    itensInventario.value = inv.itens
    hidratarFormularioLocal()
}

onMounted(carregarTudo)

// ─── Seletores ────────────────────────────────────────────────────────────────
const opcoesInventario = computed(() => [
    { value: "", label: "Sem item de inventário" },
    ...itensInventario.value.map(i => ({ value: String(i.id), label: `${i.codigo} — ${i.nome}` })),
])

const opcoesFormaPagamento = computed(() =>
    formasPagamento.value.map(f => ({ value: String(f.id), label: f.nome })),
)

// ─── IMPLANTES ────────────────────────────────────────────────────────────────
const drawerImplante = ref(false)
const idEditandoImplante = ref<number | null>(null)
const formImplante = ref<CatalogoImplantePayload & { itemInventarioIdStr: string }>({
    descricao: "", custoUnitario: 0, itemInventarioId: null, itemInventarioIdStr: "",
})
const confirmImplante = ref<{ aberto: boolean; alvo: CatalogoImplante | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

function novoImplante() {
    idEditandoImplante.value = null
    formImplante.value = { descricao: "", custoUnitario: 0, itemInventarioId: null, itemInventarioIdStr: "" }
    drawerImplante.value = true
}

function editarImplante(item: CatalogoImplante) {
    idEditandoImplante.value = item.id
    formImplante.value = {
        descricao: item.descricao,
        custoUnitario: item.custoUnitario,
        itemInventarioId: item.itemInventarioId,
        itemInventarioIdStr: item.itemInventarioId ? String(item.itemInventarioId) : "",
    }
    drawerImplante.value = true
}

async function salvarImplante() {
    if (!formImplante.value.descricao.trim()) {
        notificar("Descrição é obrigatória.", "error")
        return
    }
    const itemId = formImplante.value.itemInventarioIdStr
        ? Number(formImplante.value.itemInventarioIdStr)
        : null
    const payload: CatalogoImplantePayload = {
        descricao: formImplante.value.descricao.trim(),
        custoUnitario: Number(formImplante.value.custoUnitario) || 0,
        itemInventarioId: itemId,
    }
    try {
        if (idEditandoImplante.value === null) {
            await orcamentoCatalogoService.criarImplante(payload)
            notificar("Implante criado.", "success")
        } else {
            await orcamentoCatalogoService.atualizarImplante(idEditandoImplante.value, payload)
            notificar("Implante atualizado.", "success")
        }
        drawerImplante.value = false
        implantes.value = await orcamentoCatalogoService.listarImplantes()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao salvar implante.", "error")
    }
}

function pedirRemocaoImplante(item: CatalogoImplante) {
    confirmImplante.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocaoImplante() {
    const alvo = confirmImplante.value.alvo
    if (!alvo) return
    confirmImplante.value.executando = true
    try {
        await orcamentoCatalogoService.removerImplante(alvo.id)
        confirmImplante.value = { aberto: false, alvo: null, executando: false }
        notificar("Implante removido.", "success")
        implantes.value = await orcamentoCatalogoService.listarImplantes()
    } catch (e: any) {
        confirmImplante.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao remover implante.", "error")
    }
}

// ─── EQUIPES LEGADO ───────────────────────────────────────────────────────────
const drawerEquipe = ref(false)
const idEditandoEquipe = ref<number | null>(null)
const formEquipe = ref<CatalogoEquipePayload>({ descricao: "", valorPadrao: 0 })
const confirmEquipe = ref<{ aberto: boolean; alvo: CatalogoEquipe | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

function novaEquipe() {
    idEditandoEquipe.value = null
    formEquipe.value = { descricao: "", valorPadrao: 0 }
    drawerEquipe.value = true
}

function editarEquipe(item: CatalogoEquipe) {
    idEditandoEquipe.value = item.id
    formEquipe.value = { descricao: item.descricao, valorPadrao: item.valorPadrao }
    drawerEquipe.value = true
}

async function salvarEquipe() {
    if (!formEquipe.value.descricao.trim()) {
        notificar("Descrição é obrigatória.", "error")
        return
    }
    const payload: CatalogoEquipePayload = {
        descricao: formEquipe.value.descricao.trim(),
        valorPadrao: Number(formEquipe.value.valorPadrao) || 0,
    }
    try {
        if (idEditandoEquipe.value === null) {
            await orcamentoCatalogoService.criarEquipe(payload)
            notificar("Equipe criada.", "success")
        } else {
            await orcamentoCatalogoService.atualizarEquipe(idEditandoEquipe.value, payload)
            notificar("Equipe atualizada.", "success")
        }
        drawerEquipe.value = false
        equipes.value = await orcamentoCatalogoService.listarEquipes()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao salvar equipe.", "error")
    }
}

function pedirRemocaoEquipe(item: CatalogoEquipe) {
    confirmEquipe.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocaoEquipe() {
    const alvo = confirmEquipe.value.alvo
    if (!alvo) return
    confirmEquipe.value.executando = true
    try {
        await orcamentoCatalogoService.removerEquipe(alvo.id)
        confirmEquipe.value = { aberto: false, alvo: null, executando: false }
        notificar("Equipe removida.", "success")
        equipes.value = await orcamentoCatalogoService.listarEquipes()
    } catch (e: any) {
        confirmEquipe.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao remover equipe.", "error")
    }
}

// ─── PAGAMENTO ────────────────────────────────────────────────────────────────
const drawerPagamento = ref(false)
const idEditandoPagamento = ref<number | null>(null)
const formPagamento = ref<{
    formaPagamentoIdStr: string
    acrescimoPercentual: number
    entradaPercentualPadrao: number
    taxaParcela: number
    parcelasMaximas: number
}>({
    formaPagamentoIdStr: "",
    acrescimoPercentual: 0,
    entradaPercentualPadrao: 0,
    taxaParcela: 0,
    parcelasMaximas: 1,
})
const confirmPagamento = ref<{ aberto: boolean; alvo: ConfiguracaoPagamentoCatalogo | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

function novoPagamento() {
    idEditandoPagamento.value = null
    formPagamento.value = {
        formaPagamentoIdStr: "",
        acrescimoPercentual: 0,
        entradaPercentualPadrao: 0,
        taxaParcela: 0,
        parcelasMaximas: 1,
    }
    drawerPagamento.value = true
}

function editarPagamento(item: ConfiguracaoPagamentoCatalogo) {
    idEditandoPagamento.value = item.id
    formPagamento.value = {
        formaPagamentoIdStr: String(item.formaPagamentoId),
        acrescimoPercentual: item.acrescimoPercentual,
        entradaPercentualPadrao: item.entradaPercentualPadrao,
        taxaParcela: item.taxaParcela,
        parcelasMaximas: item.parcelasMaximas,
    }
    drawerPagamento.value = true
}

async function salvarPagamento() {
    if (!formPagamento.value.formaPagamentoIdStr) {
        notificar("Forma de pagamento é obrigatória.", "error")
        return
    }
    try {
        if (idEditandoPagamento.value === null) {
            const payload: CriarConfigPagamentoPayload = {
                formaPagamentoId: Number(formPagamento.value.formaPagamentoIdStr),
                acrescimoPercentual: Number(formPagamento.value.acrescimoPercentual) || 0,
                entradaPercentualPadrao: Number(formPagamento.value.entradaPercentualPadrao) || 0,
                taxaParcela: Number(formPagamento.value.taxaParcela) || 0,
                parcelasMaximas: Number(formPagamento.value.parcelasMaximas) || 1,
            }
            await orcamentoCatalogoService.criarConfigPagamento(payload)
            notificar("Configuração de pagamento criada.", "success")
        } else {
            const payload: AtualizarConfigPagamentoPayload = {
                acrescimoPercentual: Number(formPagamento.value.acrescimoPercentual) || 0,
                entradaPercentualPadrao: Number(formPagamento.value.entradaPercentualPadrao) || 0,
                taxaParcela: Number(formPagamento.value.taxaParcela) || 0,
                parcelasMaximas: Number(formPagamento.value.parcelasMaximas) || 1,
            }
            await orcamentoCatalogoService.atualizarConfigPagamento(idEditandoPagamento.value, payload)
            notificar("Configuração de pagamento atualizada.", "success")
        }
        drawerPagamento.value = false
        pagamentos.value = await orcamentoCatalogoService.listarConfigPagamento()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao salvar configuração de pagamento.", "error")
    }
}

function pedirRemocaoPagamento(item: ConfiguracaoPagamentoCatalogo) {
    confirmPagamento.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocaoPagamento() {
    const alvo = confirmPagamento.value.alvo
    if (!alvo) return
    confirmPagamento.value.executando = true
    try {
        await orcamentoCatalogoService.removerConfigPagamento(alvo.id)
        confirmPagamento.value = { aberto: false, alvo: null, executando: false }
        notificar("Configuração de pagamento removida.", "success")
        pagamentos.value = await orcamentoCatalogoService.listarConfigPagamento()
    } catch (e: any) {
        confirmPagamento.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao remover configuração de pagamento.", "error")
    }
}
</script>

<template>
    <div class="outras-tab">
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
            <div class="secao-header">
                <h3>Implantes ({{ implantes.length }})</h3>
                <AppButton icon="fa-solid fa-plus" size="sm" @click="novoImplante">Novo implante</AppButton>
            </div>
            <div v-if="implantes.length" class="table-wrap">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Descrição</th>
                            <th>Item de inventário</th>
                            <th>Custo unitário</th>
                            <th>Status</th>
                            <th class="acoes-col"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in implantes" :key="item.id">
                            <td><span class="cell-bold">{{ item.descricao }}</span></td>
                            <td class="cell-sub">{{ item.itemInventarioNome ?? "—" }}</td>
                            <td>{{ formatarMoedaBrl(item.custoUnitario) }}</td>
                            <td><AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" /></td>
                            <td class="acoes-col">
                                <button class="btn-icon btn-icon-editar" title="Editar" @click="editarImplante(item)">
                                    <i class="fa-solid fa-pen"></i>
                                </button>
                                <button class="btn-icon btn-icon-excluir" title="Excluir" @click="pedirRemocaoImplante(item)">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-microchip" titulo="Nenhum implante cadastrado"
                descricao="Clique em 'Novo implante' para adicionar o primeiro.">
                <template #acao>
                    <AppButton icon="fa-solid fa-plus" @click="novoImplante">Novo implante</AppButton>
                </template>
            </AppEmptyState>
        </div>

        <!-- EQUIPES LEGADO -->
        <div v-if="subAba === 'equipes'">
            <div class="secao-header">
                <div>
                    <h3>Equipes especializadas ({{ equipes.length }})</h3>
                    <p class="hint">Modelo antigo de equipe (descrição + valor padrão). O modelo novo está na aba <strong>Equipe</strong> (papéis e honorários).</p>
                </div>
                <AppButton icon="fa-solid fa-plus" size="sm" @click="novaEquipe">Nova equipe</AppButton>
            </div>
            <div v-if="equipes.length" class="table-wrap">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Descrição</th>
                            <th>Valor padrão</th>
                            <th>Status</th>
                            <th class="acoes-col"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in equipes" :key="item.id">
                            <td><span class="cell-bold">{{ item.descricao }}</span></td>
                            <td>{{ formatarMoedaBrl(item.valorPadrao) }}</td>
                            <td><AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" /></td>
                            <td class="acoes-col">
                                <button class="btn-icon btn-icon-editar" title="Editar" @click="editarEquipe(item)">
                                    <i class="fa-solid fa-pen"></i>
                                </button>
                                <button class="btn-icon btn-icon-excluir" title="Excluir" @click="pedirRemocaoEquipe(item)">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-users-line" titulo="Nenhuma equipe legado"
                descricao="Clique em 'Nova equipe' para adicionar.">
                <template #acao>
                    <AppButton icon="fa-solid fa-plus" @click="novaEquipe">Nova equipe</AppButton>
                </template>
            </AppEmptyState>
        </div>

        <!-- PAGAMENTO -->
        <div v-if="subAba === 'pagamento'">
            <div class="secao-header">
                <h3>Configurações de pagamento ({{ pagamentos.length }})</h3>
                <AppButton icon="fa-solid fa-plus" size="sm" @click="novoPagamento">Nova configuração</AppButton>
            </div>
            <div v-if="pagamentos.length" class="table-wrap">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Forma de pagamento</th>
                            <th>Acréscimo</th>
                            <th>Entrada padrão</th>
                            <th>Taxa parcela</th>
                            <th>Máx. parcelas</th>
                            <th>Status</th>
                            <th class="acoes-col"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in pagamentos" :key="item.id">
                            <td><span class="cell-bold">{{ item.formaPagamentoNome ?? `Forma #${item.formaPagamentoId}` }}</span></td>
                            <td>{{ item.acrescimoPercentual }}%</td>
                            <td>{{ item.entradaPercentualPadrao }}%</td>
                            <td>{{ item.taxaParcela }}%</td>
                            <td>{{ item.parcelasMaximas }}x</td>
                            <td><AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" /></td>
                            <td class="acoes-col">
                                <button class="btn-icon btn-icon-editar" title="Editar" @click="editarPagamento(item)">
                                    <i class="fa-solid fa-pen"></i>
                                </button>
                                <button class="btn-icon btn-icon-excluir" title="Excluir" @click="pedirRemocaoPagamento(item)">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <AppEmptyState v-else icone="fa-solid fa-credit-card" titulo="Nenhuma configuração de pagamento"
                descricao="Clique em 'Nova configuração' para definir descontos e parcelamento por forma de pagamento.">
                <template #acao>
                    <AppButton icon="fa-solid fa-plus" @click="novoPagamento">Nova configuração</AppButton>
                </template>
            </AppEmptyState>
        </div>

        <!-- ─── DRAWERS ─────────────────────────────────────────────────────── -->

        <!-- Drawer implante -->
        <AppDrawer
            :aberto="drawerImplante"
            :titulo="idEditandoImplante === null ? 'Novo implante' : 'Editar implante'"
            :largura="520"
            @fechar="drawerImplante = false"
        >
            <AppField label="Descrição" required>
                <AppInput v-model="formImplante.descricao" placeholder="Ex: Tela de polipropileno 15x15 cm" />
            </AppField>
            <AppField label="Custo unitário (R$)">
                <AppInputDecimal v-model="formImplante.custoUnitario" :decimals="2" placeholder="0,00" />
            </AppField>
            <AppField label="Item de inventário (opcional)">
                <AppSelect v-model="formImplante.itemInventarioIdStr" :options="opcoesInventario" />
            </AppField>
            <template #rodape>
                <AppButton variant="secondary" @click="drawerImplante = false">Cancelar</AppButton>
                <AppButton @click="salvarImplante">Salvar</AppButton>
            </template>
        </AppDrawer>

        <!-- Drawer equipe -->
        <AppDrawer
            :aberto="drawerEquipe"
            :titulo="idEditandoEquipe === null ? 'Nova equipe' : 'Editar equipe'"
            :largura="480"
            @fechar="drawerEquipe = false"
        >
            <AppField label="Descrição" required>
                <AppInput v-model="formEquipe.descricao" placeholder="Ex: Equipe de cirurgia plástica" />
            </AppField>
            <AppField label="Valor padrão (R$)">
                <AppInputDecimal v-model="formEquipe.valorPadrao" :decimals="2" placeholder="0,00" />
            </AppField>
            <template #rodape>
                <AppButton variant="secondary" @click="drawerEquipe = false">Cancelar</AppButton>
                <AppButton @click="salvarEquipe">Salvar</AppButton>
            </template>
        </AppDrawer>

        <!-- Drawer pagamento -->
        <AppDrawer
            :aberto="drawerPagamento"
            :titulo="idEditandoPagamento === null ? 'Nova configuração de pagamento' : 'Editar configuração de pagamento'"
            :largura="520"
            @fechar="drawerPagamento = false"
        >
            <AppField label="Forma de pagamento" required
                :hint="idEditandoPagamento !== null ? 'A forma de pagamento não pode ser alterada após a criação.' : undefined">
                <AppSelect v-model="formPagamento.formaPagamentoIdStr" :options="opcoesFormaPagamento"
                    placeholder="Selecionar forma de pagamento..."
                    :disabled="idEditandoPagamento !== null" />
            </AppField>
            <div class="grid-2">
                <AppField label="Acréscimo (%)">
                    <AppInput type="number" step="0.01" min="0"
                        :model-value="formPagamento.acrescimoPercentual"
                        @update:model-value="(v: any) => formPagamento.acrescimoPercentual = Number(v) || 0" />
                </AppField>
                <AppField label="Entrada padrão (%)">
                    <AppInput type="number" step="0.01" min="0"
                        :model-value="formPagamento.entradaPercentualPadrao"
                        @update:model-value="(v: any) => formPagamento.entradaPercentualPadrao = Number(v) || 0" />
                </AppField>
            </div>
            <div class="grid-2">
                <AppField label="Taxa por parcela (%)">
                    <AppInput type="number" step="0.01" min="0"
                        :model-value="formPagamento.taxaParcela"
                        @update:model-value="(v: any) => formPagamento.taxaParcela = Number(v) || 0" />
                </AppField>
                <AppField label="Parcelas máximas">
                    <AppInput type="number" step="1" min="1"
                        :model-value="formPagamento.parcelasMaximas"
                        @update:model-value="(v: any) => formPagamento.parcelasMaximas = Number(v) || 1" />
                </AppField>
            </div>
            <template #rodape>
                <AppButton variant="secondary" @click="drawerPagamento = false">Cancelar</AppButton>
                <AppButton @click="salvarPagamento">Salvar</AppButton>
            </template>
        </AppDrawer>

        <!-- ─── CONFIRM DIALOGS ────────────────────────────────────────────── -->
        <AppConfirmDialog
            v-model:aberto="confirmImplante.aberto"
            titulo="Excluir implante?"
            :mensagem="confirmImplante.alvo ? `Deseja excluir o implante '${confirmImplante.alvo.descricao}'?` : ''"
            confirmar-rotulo="Excluir"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="confirmImplante.executando"
            @confirmar="executarRemocaoImplante"
        />
        <AppConfirmDialog
            v-model:aberto="confirmEquipe.aberto"
            titulo="Excluir equipe?"
            :mensagem="confirmEquipe.alvo ? `Deseja excluir a equipe '${confirmEquipe.alvo.descricao}'?` : ''"
            confirmar-rotulo="Excluir"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="confirmEquipe.executando"
            @confirmar="executarRemocaoEquipe"
        />
        <AppConfirmDialog
            v-model:aberto="confirmPagamento.aberto"
            titulo="Excluir configuração de pagamento?"
            :mensagem="confirmPagamento.alvo ? `Deseja excluir a configuração de '${confirmPagamento.alvo.formaPagamentoNome ?? 'pagamento'}'?` : ''"
            confirmar-rotulo="Excluir"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="confirmPagamento.executando"
            @confirmar="executarRemocaoPagamento"
        />

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

h3 { margin: 0 0 4px 0; font-size: var(--text-md); font-weight: var(--font-weight-semibold); }
.hint { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.6); margin: 0 0 12px 0; }

.secao-header {
    display: flex; align-items: flex-start; justify-content: space-between; gap: 12px;
    margin-bottom: 12px;
}
.secao-header > :first-child { flex: 1; }

.table-wrap {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
}
.table { width: 100%; border-collapse: collapse; font-size: var(--text-sm); }
.table thead th {
    text-align: left; padding: 10px 16px;
    background: hsl(var(--secondary) / 0.04);
    color: hsl(var(--secondary) / 0.7);
    font-weight: var(--font-weight-semibold);
    font-size: var(--text-xs);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.table tbody td { padding: 10px 16px; border-top: 1px solid hsl(var(--secondary) / 0.08); }
.cell-bold { font-weight: var(--font-weight-semibold); }
.cell-sub { color: hsl(var(--secondary) / 0.6); font-size: var(--text-xs); }
.acoes-col { width: 90px; text-align: right; white-space: nowrap; }
.acoes-col .btn-icon + .btn-icon { margin-left: 4px; }

.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

/* Locais cirúrgicos — cards editáveis */
.locais-grid {
    display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 14px;
}
.local-card {
    background: hsl(var(--card)); border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 10px; padding: 14px; display: flex; flex-direction: column; gap: 10px;
}
.local-card-head strong { display: block; font-size: var(--text-sm); color: hsl(var(--primary)); }
.local-card-head small { color: hsl(var(--secondary) / 0.6); font-size: var(--text-xs); }
.local-card-fields { display: flex; flex-direction: column; gap: 8px; }
.local-card-fields label {
    display: flex; flex-direction: column; gap: 4px;
    font-size: var(--text-xs); color: hsl(var(--secondary) / 0.7);
}
.local-card-fields input {
    padding: 6px 8px; border: 1px solid hsl(var(--secondary) / 0.15); border-radius: 5px;
    font-size: var(--text-sm); font-family: inherit;
}
.local-card-acoes {
    display: flex; align-items: center; justify-content: space-between; gap: 8px;
    border-top: 1px solid hsl(var(--secondary) / 0.08); padding-top: 8px;
}
.local-card-acoes .status { color: hsl(var(--secondary) / 0.55); font-size: var(--text-xs); }
</style>
