<script setup lang="ts">
/**
 * OrcamentoSettingsView — placeholder funcional dos 6 catálogos da Fase 6.1.
 * Cada aba é uma lista CRUD simples; a UI será polida na Fase 6.2 com sub-componentes
 * (`SettingsCirurgias`, `SettingsValorProfissional`, etc.) seguindo o design system.
 *
 * Por ora, esta view é só um caminho funcional para que o usuário consiga povoar os
 * catálogos enquanto a UX final ainda não está pronta.
 */
import { ref, onMounted, computed, watch } from "vue"
import {
    AppPageHeader, AppTabs, AppButton, AppCard, AppEmptyState, AppField,
    AppInput, AppSelect, AppCheckbox, AppModal,
} from "@/components/ui"
import {
    orcamentoCatalogoService,
    type CatalogoCirurgia,
    type ValorProfissionalOrcamentoCatalogo,
    type ConfiguracaoLocalCirurgia,
    type CatalogoEquipe,
    type CatalogoImplante,
    type ConfiguracaoPagamentoCatalogo,
    type CatalogoProduto,
    type CatalogoCirurgiaProdutoVinculo,
} from "@/services/orcamentoCatalogoService"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"

type AbaKey = "cirurgias" | "valoresProfissional" | "local" | "equipes" | "implantes" | "produtos" | "pagamento"

const aba = ref<AbaKey>("cirurgias")
const carregando = ref(false)
const erro = ref<string | null>(null)
const formasPagamento = ref<FormaPagamento[]>([])

const abas = [
    { valor: "cirurgias", label: "Cirurgias", icone: "fa-solid fa-scalpel" },
    { valor: "valoresProfissional", label: "Profissionais", icone: "fa-solid fa-user-doctor" },
    { valor: "local", label: "Local cirurgia", icone: "fa-solid fa-hospital" },
    { valor: "equipes", label: "Equipes", icone: "fa-solid fa-users" },
    { valor: "implantes", label: "Implantes", icone: "fa-solid fa-microchip" },
    { valor: "produtos", label: "Produtos", icone: "fa-solid fa-boxes-stacked" },
    { valor: "pagamento", label: "Pagamento", icone: "fa-solid fa-credit-card" },
]

// ─────────────────────────── Cirurgias
const cirurgias = ref<CatalogoCirurgia[]>([])
const formCirurgia = ref({ id: 0, descricao: "", valorBase: 0, duracaoPadraoMinutos: null as number | null })
const editandoCirurgia = ref(false)

async function carregarCirurgias() {
    cirurgias.value = await orcamentoCatalogoService.listarCirurgias()
}
function novaCirurgia() {
    formCirurgia.value = { id: 0, descricao: "", valorBase: 0, duracaoPadraoMinutos: null }
    editandoCirurgia.value = true
}
function editarCirurgia(c: CatalogoCirurgia) {
    formCirurgia.value = {
        id: c.id, descricao: c.descricao, valorBase: c.valorBase, duracaoPadraoMinutos: c.duracaoPadraoMinutos,
    }
    editandoCirurgia.value = true
}
async function salvarCirurgia() {
    const { id, descricao, valorBase, duracaoPadraoMinutos } = formCirurgia.value
    if (id === 0) await orcamentoCatalogoService.criarCirurgia({ descricao, valorBase, duracaoPadraoMinutos })
    else await orcamentoCatalogoService.atualizarCirurgia(id, { descricao, valorBase, duracaoPadraoMinutos })
    editandoCirurgia.value = false
    await carregarCirurgias()
}
async function removerCirurgia(c: CatalogoCirurgia) {
    if (!confirm(`Inativar "${c.descricao}"?`)) return
    await orcamentoCatalogoService.removerCirurgia(c.id)
    await carregarCirurgias()
}

// ─────────────────────────── Valores profissional
const valores = ref<ValorProfissionalOrcamentoCatalogo[]>([])
const formValor = ref({
    id: 0,
    profissionalUsuarioId: null as string | null,
    funcao: "",
    tempoBaseMinutos: 60,
    valorTempoBase: 0,
    tempoAdicionalMinutos: 30,
    valorAdicional: 0,
    valorPlus: 0,
})
const editandoValor = ref(false)
async function carregarValores() {
    valores.value = await orcamentoCatalogoService.listarValoresProfissional()
}
function novoValor() {
    formValor.value = {
        id: 0, profissionalUsuarioId: null, funcao: "Cirurgião",
        tempoBaseMinutos: 60, valorTempoBase: 0,
        tempoAdicionalMinutos: 30, valorAdicional: 0, valorPlus: 0,
    }
    editandoValor.value = true
}
function editarValor(v: ValorProfissionalOrcamentoCatalogo) {
    formValor.value = {
        id: v.id, profissionalUsuarioId: v.profissionalUsuarioId, funcao: v.funcao,
        tempoBaseMinutos: v.tempoBaseMinutos, valorTempoBase: v.valorTempoBase,
        tempoAdicionalMinutos: v.tempoAdicionalMinutos, valorAdicional: v.valorAdicional,
        valorPlus: v.valorPlus,
    }
    editandoValor.value = true
}
async function salvarValor() {
    const f = formValor.value
    if (f.id === 0) {
        await orcamentoCatalogoService.criarValorProfissional({
            profissionalUsuarioId: f.profissionalUsuarioId,
            funcao: f.funcao,
            tempoBaseMinutos: f.tempoBaseMinutos,
            valorTempoBase: f.valorTempoBase,
            tempoAdicionalMinutos: f.tempoAdicionalMinutos,
            valorAdicional: f.valorAdicional,
            valorPlus: f.valorPlus,
        })
    } else {
        await orcamentoCatalogoService.atualizarValorProfissional(f.id, {
            funcao: f.funcao,
            tempoBaseMinutos: f.tempoBaseMinutos,
            valorTempoBase: f.valorTempoBase,
            tempoAdicionalMinutos: f.tempoAdicionalMinutos,
            valorAdicional: f.valorAdicional,
            valorPlus: f.valorPlus,
        })
    }
    editandoValor.value = false
    await carregarValores()
}
async function removerValor(v: ValorProfissionalOrcamentoCatalogo) {
    if (!confirm(`Inativar "${v.funcao}"?`)) return
    await orcamentoCatalogoService.removerValorProfissional(v.id)
    await carregarValores()
}

// ─────────────────────────── Local cirurgia (1 por tipo)
const TIPOS_INTERNACAO = ["Apartamento", "Enfermaria", "UTI", "Ambulatorial"]
const locais = ref<ConfiguracaoLocalCirurgia[]>([])
const formLocal = ref({
    tipoInternacao: "Apartamento",
    tempoBaseMinutos: 60,
    valorBase: 0,
    tempoAdicionalMinutos: 30,
    valorAdicional: 0,
})
const editandoLocal = ref(false)

async function carregarLocais() {
    locais.value = await orcamentoCatalogoService.listarLocais()
}
function getLocal(tipo: string): ConfiguracaoLocalCirurgia | undefined {
    return locais.value.find(l => l.tipoInternacao === tipo)
}
function abrirLocal(tipo: string) {
    const existente = locais.value.find(l => l.tipoInternacao === tipo)
    formLocal.value = {
        tipoInternacao: tipo,
        tempoBaseMinutos: existente?.tempoBaseMinutos ?? 60,
        valorBase: existente?.valorBase ?? 0,
        tempoAdicionalMinutos: existente?.tempoAdicionalMinutos ?? 30,
        valorAdicional: existente?.valorAdicional ?? 0,
    }
    editandoLocal.value = true
}
async function salvarLocal() {
    const f = formLocal.value
    await orcamentoCatalogoService.salvarLocal(f.tipoInternacao, {
        tempoBaseMinutos: f.tempoBaseMinutos,
        valorBase: f.valorBase,
        tempoAdicionalMinutos: f.tempoAdicionalMinutos,
        valorAdicional: f.valorAdicional,
    })
    editandoLocal.value = false
    await carregarLocais()
}

// ─────────────────────────── Equipes
const equipes = ref<CatalogoEquipe[]>([])
const formEquipe = ref({ id: 0, descricao: "", valorPadrao: 0 })
const editandoEquipe = ref(false)
async function carregarEquipes() {
    equipes.value = await orcamentoCatalogoService.listarEquipes()
}
function novaEquipe() { formEquipe.value = { id: 0, descricao: "", valorPadrao: 0 }; editandoEquipe.value = true }
function editarEquipe(e: CatalogoEquipe) {
    formEquipe.value = { id: e.id, descricao: e.descricao, valorPadrao: e.valorPadrao }
    editandoEquipe.value = true
}
async function salvarEquipe() {
    const { id, descricao, valorPadrao } = formEquipe.value
    if (id === 0) await orcamentoCatalogoService.criarEquipe({ descricao, valorPadrao })
    else await orcamentoCatalogoService.atualizarEquipe(id, { descricao, valorPadrao })
    editandoEquipe.value = false
    await carregarEquipes()
}
async function removerEquipe(e: CatalogoEquipe) {
    if (!confirm(`Inativar "${e.descricao}"?`)) return
    await orcamentoCatalogoService.removerEquipe(e.id)
    await carregarEquipes()
}

// ─────────────────────────── Implantes
const implantes = ref<CatalogoImplante[]>([])
const formImplante = ref({ id: 0, itemInventarioId: null as number | null, descricao: "", custoUnitario: 0 })
const editandoImplante = ref(false)
async function carregarImplantes() {
    implantes.value = await orcamentoCatalogoService.listarImplantes()
}
function novoImplante() {
    formImplante.value = { id: 0, itemInventarioId: null, descricao: "", custoUnitario: 0 }
    editandoImplante.value = true
}
function editarImplante(i: CatalogoImplante) {
    formImplante.value = { id: i.id, itemInventarioId: i.itemInventarioId, descricao: i.descricao, custoUnitario: i.custoUnitario }
    editandoImplante.value = true
}
async function salvarImplante() {
    const { id, itemInventarioId, descricao, custoUnitario } = formImplante.value
    if (id === 0) await orcamentoCatalogoService.criarImplante({ itemInventarioId, descricao, custoUnitario })
    else await orcamentoCatalogoService.atualizarImplante(id, { itemInventarioId, descricao, custoUnitario })
    editandoImplante.value = false
    await carregarImplantes()
}
async function removerImplante(i: CatalogoImplante) {
    if (!confirm(`Inativar "${i.descricao}"?`)) return
    await orcamentoCatalogoService.removerImplante(i.id)
    await carregarImplantes()
}

// ─────────────────────────── Configuração pagamento
const configsPagamento = ref<ConfiguracaoPagamentoCatalogo[]>([])
const formConfig = ref({
    id: 0,
    formaPagamentoId: 0,
    acrescimoPercentual: 0,
    entradaPercentualPadrao: 0,
    taxaParcela: 0,
    parcelasMaximas: 12,
})
const editandoConfig = ref(false)
async function carregarConfigsPagamento() {
    configsPagamento.value = await orcamentoCatalogoService.listarConfigPagamento()
}
function novaConfig() {
    formConfig.value = {
        id: 0, formaPagamentoId: 0,
        acrescimoPercentual: 0, entradaPercentualPadrao: 0,
        taxaParcela: 0, parcelasMaximas: 12,
    }
    editandoConfig.value = true
}
function editarConfig(c: ConfiguracaoPagamentoCatalogo) {
    formConfig.value = {
        id: c.id, formaPagamentoId: c.formaPagamentoId,
        acrescimoPercentual: c.acrescimoPercentual,
        entradaPercentualPadrao: c.entradaPercentualPadrao,
        taxaParcela: c.taxaParcela, parcelasMaximas: c.parcelasMaximas,
    }
    editandoConfig.value = true
}
async function salvarConfig() {
    const f = formConfig.value
    if (f.id === 0) {
        await orcamentoCatalogoService.criarConfigPagamento({
            formaPagamentoId: f.formaPagamentoId,
            acrescimoPercentual: f.acrescimoPercentual,
            entradaPercentualPadrao: f.entradaPercentualPadrao,
            taxaParcela: f.taxaParcela,
            parcelasMaximas: f.parcelasMaximas,
        })
    } else {
        await orcamentoCatalogoService.atualizarConfigPagamento(f.id, {
            acrescimoPercentual: f.acrescimoPercentual,
            entradaPercentualPadrao: f.entradaPercentualPadrao,
            taxaParcela: f.taxaParcela,
            parcelasMaximas: f.parcelasMaximas,
        })
    }
    editandoConfig.value = false
    await carregarConfigsPagamento()
}
async function removerConfig(c: ConfiguracaoPagamentoCatalogo) {
    if (!confirm(`Inativar config de "${c.formaPagamentoNome}"?`)) return
    await orcamentoCatalogoService.removerConfigPagamento(c.id)
    await carregarConfigsPagamento()
}

const formasNaoConfiguradas = computed(() =>
    formasPagamento.value.filter(fp => !configsPagamento.value.some(c => c.formaPagamentoId === fp.id))
)

function fmt(v: number) {
    return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

// ─────────────────────────── Produtos
const produtos = ref<CatalogoProduto[]>([])
const formProduto = ref({
    id: 0, nome: "", descricao: "" as string | null,
    valorReferencia: null as number | null, usoUnico: false,
})
const editandoProduto = ref(false)

async function carregarProdutos() {
    produtos.value = await orcamentoCatalogoService.listarProdutos()
}
function novoProduto() {
    formProduto.value = { id: 0, nome: "", descricao: null, valorReferencia: null, usoUnico: false }
    editandoProduto.value = true
}
function editarProduto(p: CatalogoProduto) {
    formProduto.value = {
        id: p.id, nome: p.nome, descricao: p.descricao,
        valorReferencia: p.valorReferencia, usoUnico: p.usoUnico,
    }
    editandoProduto.value = true
}
async function salvarProduto() {
    const f = formProduto.value
    const payload = {
        nome: f.nome,
        descricao: f.descricao || null,
        valorReferencia: f.valorReferencia,
        usoUnico: f.usoUnico,
    }
    if (f.id === 0) await orcamentoCatalogoService.criarProduto(payload)
    else await orcamentoCatalogoService.atualizarProduto(f.id, payload)
    editandoProduto.value = false
    await carregarProdutos()
}
async function removerProduto(p: CatalogoProduto) {
    if (!confirm(`Inativar "${p.nome}"?`)) return
    await orcamentoCatalogoService.removerProduto(p.id)
    await carregarProdutos()
}

// ─────────────────────────── Vínculo produtos × cirurgias
const modalProdutosCirurgia = ref(false)
const cirurgiaAtual = ref<CatalogoCirurgia | null>(null)
const vinculosCirurgia = ref<CatalogoCirurgiaProdutoVinculo[]>([])
const formVincular = ref({
    produtoId: 0, quantidadePadrao: 1, obrigatorio: false,
})

async function abrirProdutosCirurgia(c: CatalogoCirurgia) {
    cirurgiaAtual.value = c
    modalProdutosCirurgia.value = true
    formVincular.value = { produtoId: 0, quantidadePadrao: 1, obrigatorio: false }
    vinculosCirurgia.value = await orcamentoCatalogoService.listarProdutosDaCirurgia(c.id)
}

async function vincularProduto() {
    if (!cirurgiaAtual.value || !formVincular.value.produtoId || formVincular.value.quantidadePadrao <= 0) return
    await orcamentoCatalogoService.vincularProdutoCirurgia(cirurgiaAtual.value.id, {
        produtoId: formVincular.value.produtoId,
        quantidadePadrao: formVincular.value.quantidadePadrao,
        obrigatorio: formVincular.value.obrigatorio,
    })
    formVincular.value = { produtoId: 0, quantidadePadrao: 1, obrigatorio: false }
    vinculosCirurgia.value = await orcamentoCatalogoService.listarProdutosDaCirurgia(cirurgiaAtual.value.id)
}

async function desvincularProduto(v: CatalogoCirurgiaProdutoVinculo) {
    if (!cirurgiaAtual.value) return
    if (!confirm(`Remover "${v.produtoNome}" desta cirurgia?`)) return
    await orcamentoCatalogoService.desvincularProdutoCirurgia(v.id)
    vinculosCirurgia.value = await orcamentoCatalogoService.listarProdutosDaCirurgia(cirurgiaAtual.value.id)
}

const produtosNaoVinculados = computed(() =>
    produtos.value.filter(p => p.ativo && !vinculosCirurgia.value.some(v => v.catalogoProdutoId === p.id))
)

// ─────────────────────────── Carregamento sob demanda por aba
const abasCarregadas = new Set<AbaKey>()

const carregadoresPorAba: Record<AbaKey, () => Promise<void>> = {
    cirurgias:          carregarCirurgias,
    valoresProfissional: carregarValores,
    local:              carregarLocais,
    equipes:            carregarEquipes,
    implantes:          carregarImplantes,
    produtos:           carregarProdutos,
    pagamento:          carregarConfigsPagamento,
}

async function garantirAba(a: AbaKey) {
    if (abasCarregadas.has(a)) return
    carregando.value = true
    erro.value = null
    try {
        await carregadoresPorAba[a]()
        abasCarregadas.add(a)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar configurações."
    } finally {
        carregando.value = false
    }
}

watch(aba, garantirAba, { immediate: true })

onMounted(async () => {
    try {
        formasPagamento.value = await formaPagamentoService.listar()
    } catch { /* formas de pagamento não bloqueiam a view */ }
})
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Configurações de orçamento"
            subtitulo="Catálogos e tabelas de valores que alimentam o formulário de orçamento."
        />

        <div v-if="erro" class="erro-banner">{{ erro }}</div>

        <AppTabs v-model="aba" :abas="abas" variante="underline" class="mb-4" aria-label="Catálogos" />

        <!-- ─────── Cirurgias ─────── -->
        <section v-if="aba === 'cirurgias'">
            <div class="acoes-topo">
                <AppButton icon="fa-solid fa-plus" @click="novaCirurgia">Nova cirurgia</AppButton>
            </div>
            <AppEmptyState
                v-if="!carregando && cirurgias.length === 0"
                titulo="Nenhuma cirurgia cadastrada"
                descricao="Cadastre cirurgias com valor base e duração padrão para usar no orçamento."
            />
            <table v-else-if="cirurgias.length > 0" class="tabela">
                <thead>
                    <tr><th>Descrição</th><th>Valor base</th><th>Duração</th><th>Status</th><th></th></tr>
                </thead>
                <tbody>
                    <tr v-for="c in cirurgias" :key="c.id">
                        <td>{{ c.descricao }}</td>
                        <td>{{ fmt(c.valorBase) }}</td>
                        <td>{{ c.duracaoPadraoMinutos ? `${c.duracaoPadraoMinutos} min` : "—" }}</td>
                        <td>{{ c.ativo ? "Ativa" : "Inativa" }}</td>
                        <td>
                            <button class="btn-icon" @click="abrirProdutosCirurgia(c)" title="Produtos vinculados">
                                <i class="fa-solid fa-boxes-stacked"></i>
                            </button>
                            <button class="btn-icon btn-icon-editar" @click="editarCirurgia(c)" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button v-if="c.ativo" class="btn-icon btn-icon-excluir" @click="removerCirurgia(c)" title="Inativar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppCard v-if="editandoCirurgia" :title="formCirurgia.id === 0 ? 'Nova cirurgia' : 'Editar cirurgia'" class="mt-4">
                <div class="form-grid">
                    <AppField label="Descrição" for="cir-desc">
                        <AppInput id="cir-desc" v-model="formCirurgia.descricao" />
                    </AppField>
                    <AppField label="Valor base (R$)" for="cir-val">
                        <AppInput id="cir-val" type="number" :step="0.01" v-model="formCirurgia.valorBase" />
                    </AppField>
                    <AppField label="Duração padrão (min)" for="cir-dur">
                        <AppInput id="cir-dur" type="number" v-model="formCirurgia.duracaoPadraoMinutos" />
                    </AppField>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoCirurgia = false">Cancelar</AppButton>
                    <AppButton @click="salvarCirurgia">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- ─────── Valor profissional ─────── -->
        <section v-else-if="aba === 'valoresProfissional'">
            <div class="acoes-topo">
                <AppButton icon="fa-solid fa-plus" @click="novoValor">Nova tabela</AppButton>
            </div>
            <AppEmptyState
                v-if="!carregando && valores.length === 0"
                titulo="Nenhuma tabela cadastrada"
                descricao="Defina honorários por função e tempo de cirurgia."
            />
            <table v-else-if="valores.length > 0" class="tabela">
                <thead>
                    <tr>
                        <th>Função</th><th>Profissional</th>
                        <th>Tempo base</th><th>Valor base</th>
                        <th>Tempo adic.</th><th>Valor adic.</th>
                        <th>Plus</th><th>Status</th><th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="v in valores" :key="v.id">
                        <td>{{ v.funcao }}</td>
                        <td>{{ v.profissionalNome ?? "Padrão da função" }}</td>
                        <td>{{ v.tempoBaseMinutos }} min</td>
                        <td>{{ fmt(v.valorTempoBase) }}</td>
                        <td>{{ v.tempoAdicionalMinutos }} min</td>
                        <td>{{ fmt(v.valorAdicional) }}</td>
                        <td>{{ fmt(v.valorPlus) }}</td>
                        <td>{{ v.ativo ? "Ativa" : "Inativa" }}</td>
                        <td>
                            <button class="btn-icon btn-icon-editar" @click="editarValor(v)" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button v-if="v.ativo" class="btn-icon btn-icon-excluir" @click="removerValor(v)" title="Inativar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppCard v-if="editandoValor" :title="formValor.id === 0 ? 'Nova tabela' : 'Editar tabela'" class="mt-4">
                <div class="form-grid">
                    <AppField label="Função" for="val-funcao">
                        <AppSelect id="val-funcao" v-model="formValor.funcao">
                            <option value="Cirurgião">Cirurgião</option>
                            <option value="Auxiliar">Auxiliar</option>
                            <option value="Anestesista">Anestesista</option>
                            <option value="Instrumentador">Instrumentador</option>
                            <option value="Circulante">Circulante</option>
                        </AppSelect>
                    </AppField>
                    <AppField label="Tempo base (min)" for="val-tb">
                        <AppInput id="val-tb" type="number" v-model="formValor.tempoBaseMinutos" />
                    </AppField>
                    <AppField label="Valor tempo base (R$)" for="val-vb">
                        <AppInput id="val-vb" type="number" :step="0.01" v-model="formValor.valorTempoBase" />
                    </AppField>
                    <AppField label="Tempo adicional (min)" for="val-ta">
                        <AppInput id="val-ta" type="number" v-model="formValor.tempoAdicionalMinutos" />
                    </AppField>
                    <AppField label="Valor adicional (R$)" for="val-va">
                        <AppInput id="val-va" type="number" :step="0.01" v-model="formValor.valorAdicional" />
                    </AppField>
                    <AppField label="Valor plus (R$)" for="val-vp">
                        <AppInput id="val-vp" type="number" :step="0.01" v-model="formValor.valorPlus" />
                    </AppField>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoValor = false">Cancelar</AppButton>
                    <AppButton @click="salvarValor">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- ─────── Local cirurgia ─────── -->
        <section v-else-if="aba === 'local'">
            <p class="texto-aux">Configure tempo base + período adicional por tipo de internação. Cada tipo tem 1 linha.</p>
            <div class="cards-locais">
                <AppCard
                    v-for="tipo in TIPOS_INTERNACAO"
                    :key="tipo"
                    :title="tipo"
                >
                    <div v-if="getLocal(tipo)">
                        <p>Tempo base: <strong>{{ getLocal(tipo)!.tempoBaseMinutos }} min</strong> · Valor: <strong>{{ fmt(getLocal(tipo)!.valorBase) }}</strong></p>
                        <p>Adicional: <strong>{{ getLocal(tipo)!.tempoAdicionalMinutos }} min</strong> · {{ fmt(getLocal(tipo)!.valorAdicional) }}</p>
                    </div>
                    <p v-else class="texto-aux">Não configurado.</p>
                    <template #footer>
                        <AppButton size="sm" variant="ghost" @click="abrirLocal(tipo)">Configurar</AppButton>
                    </template>
                </AppCard>
            </div>

            <AppCard v-if="editandoLocal" :title="`Configurar: ${formLocal.tipoInternacao}`" class="mt-4">
                <div class="form-grid">
                    <AppField label="Tempo base (min)" for="loc-tb">
                        <AppInput id="loc-tb" type="number" v-model="formLocal.tempoBaseMinutos" />
                    </AppField>
                    <AppField label="Valor base (R$)" for="loc-vb">
                        <AppInput id="loc-vb" type="number" :step="0.01" v-model="formLocal.valorBase" />
                    </AppField>
                    <AppField label="Tempo adicional (min)" for="loc-ta">
                        <AppInput id="loc-ta" type="number" v-model="formLocal.tempoAdicionalMinutos" />
                    </AppField>
                    <AppField label="Valor adicional (R$)" for="loc-va">
                        <AppInput id="loc-va" type="number" :step="0.01" v-model="formLocal.valorAdicional" />
                    </AppField>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoLocal = false">Cancelar</AppButton>
                    <AppButton @click="salvarLocal">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- ─────── Equipes ─────── -->
        <section v-else-if="aba === 'equipes'">
            <div class="acoes-topo">
                <AppButton icon="fa-solid fa-plus" @click="novaEquipe">Nova equipe</AppButton>
            </div>
            <AppEmptyState v-if="!carregando && equipes.length === 0" titulo="Nenhuma equipe cadastrada" descricao="Cadastre equipes especializadas (ex: neurocirurgia)." />
            <table v-else-if="equipes.length > 0" class="tabela">
                <thead><tr><th>Descrição</th><th>Valor padrão</th><th>Status</th><th></th></tr></thead>
                <tbody>
                    <tr v-for="e in equipes" :key="e.id">
                        <td>{{ e.descricao }}</td>
                        <td>{{ fmt(e.valorPadrao) }}</td>
                        <td>{{ e.ativo ? "Ativa" : "Inativa" }}</td>
                        <td>
                            <button class="btn-icon btn-icon-editar" @click="editarEquipe(e)"><i class="fa-solid fa-pen"></i></button>
                            <button v-if="e.ativo" class="btn-icon btn-icon-excluir" @click="removerEquipe(e)"><i class="fa-solid fa-trash"></i></button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppCard v-if="editandoEquipe" :title="formEquipe.id === 0 ? 'Nova equipe' : 'Editar equipe'" class="mt-4">
                <div class="form-grid">
                    <AppField label="Descrição" for="eq-desc">
                        <AppInput id="eq-desc" v-model="formEquipe.descricao" />
                    </AppField>
                    <AppField label="Valor padrão (R$)" for="eq-val">
                        <AppInput id="eq-val" type="number" :step="0.01" v-model="formEquipe.valorPadrao" />
                    </AppField>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoEquipe = false">Cancelar</AppButton>
                    <AppButton @click="salvarEquipe">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- ─────── Implantes ─────── -->
        <section v-else-if="aba === 'implantes'">
            <div class="acoes-topo">
                <AppButton icon="fa-solid fa-plus" @click="novoImplante">Novo implante</AppButton>
            </div>
            <AppEmptyState v-if="!carregando && implantes.length === 0" titulo="Nenhum implante cadastrado" descricao="Cadastre implantes para uso em orçamentos." />
            <table v-else-if="implantes.length > 0" class="tabela">
                <thead><tr><th>Descrição</th><th>Custo unit.</th><th>Inventário</th><th>Status</th><th></th></tr></thead>
                <tbody>
                    <tr v-for="i in implantes" :key="i.id">
                        <td>{{ i.descricao }}</td>
                        <td>{{ fmt(i.custoUnitario) }}</td>
                        <td>{{ i.itemInventarioNome ?? "—" }}</td>
                        <td>{{ i.ativo ? "Ativo" : "Inativo" }}</td>
                        <td>
                            <button class="btn-icon btn-icon-editar" @click="editarImplante(i)"><i class="fa-solid fa-pen"></i></button>
                            <button v-if="i.ativo" class="btn-icon btn-icon-excluir" @click="removerImplante(i)"><i class="fa-solid fa-trash"></i></button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppCard v-if="editandoImplante" :title="formImplante.id === 0 ? 'Novo implante' : 'Editar implante'" class="mt-4">
                <div class="form-grid">
                    <AppField label="Descrição" for="imp-desc">
                        <AppInput id="imp-desc" v-model="formImplante.descricao" />
                    </AppField>
                    <AppField label="Custo unitário (R$)" for="imp-val">
                        <AppInput id="imp-val" type="number" :step="0.01" v-model="formImplante.custoUnitario" />
                    </AppField>
                    <AppField label="ID item inventário (opcional)" for="imp-inv">
                        <AppInput id="imp-inv" type="number" v-model="formImplante.itemInventarioId" />
                    </AppField>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoImplante = false">Cancelar</AppButton>
                    <AppButton @click="salvarImplante">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- ─────── Produtos ─────── -->
        <section v-else-if="aba === 'produtos'">
            <div class="acoes-topo">
                <AppButton icon="fa-solid fa-plus" @click="novoProduto">Novo produto</AppButton>
            </div>
            <AppEmptyState
                v-if="!carregando && produtos.length === 0"
                titulo="Nenhum produto cadastrado"
                descricao="Cadastre produtos (insumos, materiais) com valor de referência. Marque 'uso único' para cobrar apenas uma vez por orçamento mesmo com várias cirurgias."
            />
            <table v-else-if="produtos.length > 0" class="tabela">
                <thead>
                    <tr>
                        <th>Nome</th><th>Valor referência</th><th>Uso único</th><th>Status</th><th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="p in produtos" :key="p.id">
                        <td>{{ p.nome }}</td>
                        <td>{{ p.valorReferencia != null ? fmt(p.valorReferencia) : "—" }}</td>
                        <td>{{ p.usoUnico ? "Sim" : "Não" }}</td>
                        <td>{{ p.ativo ? "Ativo" : "Inativo" }}</td>
                        <td>
                            <button class="btn-icon btn-icon-editar" @click="editarProduto(p)"><i class="fa-solid fa-pen"></i></button>
                            <button v-if="p.ativo" class="btn-icon btn-icon-excluir" @click="removerProduto(p)"><i class="fa-solid fa-trash"></i></button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppCard v-if="editandoProduto" :title="formProduto.id === 0 ? 'Novo produto' : 'Editar produto'" class="mt-4">
                <div class="form-grid">
                    <AppField label="Nome" for="prod-nome">
                        <AppInput id="prod-nome" v-model="formProduto.nome" />
                    </AppField>
                    <AppField label="Valor de referência (R$)" for="prod-val">
                        <AppInput id="prod-val" type="number" :step="0.01" v-model="formProduto.valorReferencia" />
                    </AppField>
                </div>
                <AppField label="Descrição" for="prod-desc" class="mt-2">
                    <AppInput id="prod-desc" v-model="formProduto.descricao" placeholder="Descrição opcional" />
                </AppField>
                <div class="checkbox-row mt-2">
                    <AppCheckbox v-model="formProduto.usoUnico">Uso único (cobrar uma vez por orçamento)</AppCheckbox>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoProduto = false">Cancelar</AppButton>
                    <AppButton @click="salvarProduto">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- ─────── Configuração pagamento ─────── -->
        <section v-else-if="aba === 'pagamento'">
            <div class="acoes-topo">
                <AppButton icon="fa-solid fa-plus" :disabled="formasNaoConfiguradas.length === 0" @click="novaConfig">Nova configuração</AppButton>
            </div>
            <AppEmptyState v-if="!carregando && configsPagamento.length === 0" titulo="Nenhuma configuração" descricao="Configure acréscimos e entrada padrão por forma de pagamento." />
            <table v-else-if="configsPagamento.length > 0" class="tabela">
                <thead>
                    <tr>
                        <th>Forma</th><th>Acréscimo %</th><th>Entrada %</th>
                        <th>Taxa parcela</th><th>Parcelas máx.</th><th>Status</th><th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="c in configsPagamento" :key="c.id">
                        <td>{{ c.formaPagamentoNome }}</td>
                        <td>{{ c.acrescimoPercentual }}%</td>
                        <td>{{ c.entradaPercentualPadrao }}%</td>
                        <td>{{ c.taxaParcela }}</td>
                        <td>{{ c.parcelasMaximas }}x</td>
                        <td>{{ c.ativo ? "Ativa" : "Inativa" }}</td>
                        <td>
                            <button class="btn-icon btn-icon-editar" @click="editarConfig(c)"><i class="fa-solid fa-pen"></i></button>
                            <button v-if="c.ativo" class="btn-icon btn-icon-excluir" @click="removerConfig(c)"><i class="fa-solid fa-trash"></i></button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppCard v-if="editandoConfig" :title="formConfig.id === 0 ? 'Nova configuração' : 'Editar configuração'" class="mt-4">
                <div class="form-grid">
                    <AppField v-if="formConfig.id === 0" label="Forma de pagamento" for="cp-forma">
                        <AppSelect id="cp-forma" v-model="formConfig.formaPagamentoId">
                            <option :value="0">Selecione...</option>
                            <option v-for="fp in formasNaoConfiguradas" :key="fp.id" :value="fp.id">{{ fp.nome }}</option>
                        </AppSelect>
                    </AppField>
                    <AppField label="Acréscimo (%)" for="cp-acresc">
                        <AppInput id="cp-acresc" type="number" :step="0.01" v-model="formConfig.acrescimoPercentual" />
                    </AppField>
                    <AppField label="Entrada padrão (%)" for="cp-entr">
                        <AppInput id="cp-entr" type="number" :step="0.01" v-model="formConfig.entradaPercentualPadrao" />
                    </AppField>
                    <AppField label="Taxa por parcela" for="cp-tx">
                        <AppInput id="cp-tx" type="number" :step="0.0001" v-model="formConfig.taxaParcela" />
                    </AppField>
                    <AppField label="Parcelas máximas" for="cp-parc">
                        <AppInput id="cp-parc" type="number" v-model="formConfig.parcelasMaximas" />
                    </AppField>
                </div>
                <template #footer>
                    <AppButton variant="ghost" @click="editandoConfig = false">Cancelar</AppButton>
                    <AppButton @click="salvarConfig">Salvar</AppButton>
                </template>
            </AppCard>
        </section>

        <!-- Modal: Produtos da cirurgia -->
        <AppModal
            :aberto="modalProdutosCirurgia"
            :titulo="cirurgiaAtual ? `Produtos: ${cirurgiaAtual.descricao}` : 'Produtos'"
            largura="lg"
            @fechar="modalProdutosCirurgia = false"
        >
            <table v-if="vinculosCirurgia.length" class="tabela">
                <thead>
                    <tr><th>Produto</th><th>Quantidade padrão</th><th>Uso único</th><th>Obrigatório</th><th></th></tr>
                </thead>
                <tbody>
                    <tr v-for="v in vinculosCirurgia" :key="v.id">
                        <td>{{ v.produtoNome }}</td>
                        <td>{{ v.quantidadePadrao }}</td>
                        <td>{{ v.produtoUsoUnico ? "Sim" : "Não" }}</td>
                        <td>{{ v.obrigatorio ? "Sim" : "Não" }}</td>
                        <td>
                            <button class="btn-icon btn-icon-excluir" @click="desvincularProduto(v)" title="Remover">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
            <p v-else class="texto-aux">Nenhum produto vinculado a esta cirurgia.</p>

            <div class="form-add">
                <h4 class="add-title">Vincular produto</h4>
                <p v-if="produtosNaoVinculados.length === 0" class="texto-aux">
                    Todos os produtos cadastrados já estão vinculados. Crie mais produtos na aba "Produtos".
                </p>
                <div v-else class="form-grid">
                    <AppField label="Produto">
                        <AppSelect v-model="formVincular.produtoId">
                            <option :value="0">Selecione...</option>
                            <option v-for="p in produtosNaoVinculados" :key="p.id" :value="p.id">
                                {{ p.nome }}{{ p.usoUnico ? " (uso único)" : "" }}
                            </option>
                        </AppSelect>
                    </AppField>
                    <AppField label="Quantidade padrão">
                        <AppInput type="number" :min="0.001" :step="0.001" v-model="formVincular.quantidadePadrao" />
                    </AppField>
                    <AppField label="Obrigatório">
                        <AppCheckbox v-model="formVincular.obrigatorio">Sim</AppCheckbox>
                    </AppField>
                </div>
                <div class="acoes-add" v-if="produtosNaoVinculados.length > 0">
                    <AppButton size="sm" icon="fa-solid fa-plus" :disabled="!formVincular.produtoId" @click="vincularProduto">Vincular</AppButton>
                </div>
            </div>

            <template #footer>
                <AppButton @click="modalProdutosCirurgia = false">Fechar</AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.erro-banner {
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    margin-bottom: 1rem;
}
.acoes-topo {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 0.75rem;
}
.tabela {
    width: 100%;
    border-collapse: collapse;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    overflow: hidden;
    font-size: 0.88em;
}
.tabela th, .tabela td {
    padding: 0.6rem 0.85rem;
    text-align: left;
    border-bottom: 1px solid var(--border);
}
.tabela th {
    background: var(--bg-muted);
    font-weight: 600;
    font-size: 0.78em;
    text-transform: uppercase;
    color: var(--text-muted);
}
.tabela tr:last-child td { border-bottom: none; }
.form-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 0.75rem;
}
.cards-locais {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 0.85rem;
}
.texto-aux {
    color: var(--text-muted);
    font-size: 0.88em;
    margin: 0 0 0.75rem;
}
.mb-4 { margin-bottom: 1rem; }
.mt-4 { margin-top: 1rem; }
.mt-2 { margin-top: 0.5rem; }
.form-add {
    margin-top: 0.85rem;
    padding-top: 0.85rem;
    border-top: 1px dashed var(--border);
}
.add-title {
    font-size: 0.85em;
    font-weight: 600;
    color: var(--text-muted);
    margin: 0 0 0.5rem;
}
.acoes-add {
    display: flex;
    justify-content: flex-end;
    margin-top: 0.5rem;
}
.checkbox-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
</style>
