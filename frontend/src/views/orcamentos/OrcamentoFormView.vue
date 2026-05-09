<script setup lang="ts">
/**
 * OrcamentoFormView — edição de orçamento.
 * Layout: coluna principal (abas) + sidebar sticky de resumo com desconto inline.
 */
import { ref, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    orcamentoService,
    type Orcamento,
    type OrcamentoEquipe,
    type OrcamentoImplante,
    type OrcamentoFormaPagamento,
    type OrcamentoCirurgia,
    type OrcamentoInternacao,
    type OrcamentoAnestesia,
    type PreviewOrcamentoPayload,
} from "@/services/orcamentoService"
import { usePreviewOrcamento } from "@/composables/usePreviewOrcamento"
import {
    orcamentoCatalogoService,
    type CatalogoCirurgia,
    type ValorProfissionalOrcamentoCatalogo,
    type CatalogoEquipe,
    type CatalogoImplante,
    type ConfiguracaoPagamentoCatalogo,
} from "@/services/orcamentoCatalogoService"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"
import {
    AppButton, AppTabs, AppCard,
    AppField, AppInput, AppTextarea, AppSelect,
} from "@/components/ui"
import OrcamentoStatusPill from "@/components/orcamento/OrcamentoStatusPill.vue"
import OrcamentoResumoSidebar from "@/components/orcamento/OrcamentoResumoSidebar.vue"

const route = useRoute()
const router = useRouter()
const orcamentoId = Number(route.params.id)

const orcamento = ref<Orcamento | null>(null)
const carregando = ref(false)
const salvando = ref(false)
const erro = ref<string | null>(null)

// Catálogos
const catCirurgias   = ref<CatalogoCirurgia[]>([])
const catValores     = ref<ValorProfissionalOrcamentoCatalogo[]>([])
const catEquipes     = ref<CatalogoEquipe[]>([])
const catImplantes   = ref<CatalogoImplante[]>([])
const catConfigsPgto = ref<ConfiguracaoPagamentoCatalogo[]>([])
const formasPagamento = ref<FormaPagamento[]>([])

// Estado editável
const validade      = ref("")
const observacoes   = ref("")
const procedimentoCirurgicoId = ref<number | null>(null)

const cirurgias  = ref<OrcamentoCirurgia[]>([])
const equipe     = ref<OrcamentoEquipe[]>([])
const implantes  = ref<OrcamentoImplante[]>([])
const formas     = ref<OrcamentoFormaPagamento[]>([])
const internacao = ref<OrcamentoInternacao | null>(null)
const anestesia  = ref<OrcamentoAnestesia | null>(null)

// Desconto (só frontend — o preview do back não inclui desconto livre, mas o atualizar aceita itens)
// Nota: desconto é calculado client-side para display; totalGeral vem do preview do servidor.
const desconto     = ref(0)
const tipoDesconto = ref<"valor" | "percentual">("valor")

// ── Tabs ──────────────────────────────────────────────────────────────────────
type AbaKey = "paciente" | "cirurgias" | "equipeImplantes" | "pagamento"
const aba = ref<AbaKey>("paciente")
const abas = [
    { valor: "paciente",        label: "Paciente",           icone: "fa-solid fa-user" },
    { valor: "cirurgias",       label: "Cirurgias",          icone: "fa-solid fa-scalpel" },
    { valor: "equipeImplantes", label: "Equipe & implantes", icone: "fa-solid fa-users" },
    { valor: "pagamento",       label: "Local & pagamento",  icone: "fa-solid fa-credit-card" },
]

// ── Forms de adição ───────────────────────────────────────────────────────────
const novaCirurgia = ref({ catalogoId: 0, descricao: "", quantidade: 1, duracaoMinutos: null as number | null, valorUnitario: 0 })
const novoMembro   = ref({ catalogoValorId: 0, profissionalUsuarioId: "", papel: "Cirurgião", valor: 0 })
const novoImplante = ref({ catalogoId: 0, descricao: "", quantidade: 1, custoUnitario: 0 })
const novaEquipeEsp = ref({ catalogoId: 0, descricao: "", valor: 0 })
const novaForma    = ref({ formaPagamentoId: 0, valor: 0, parcelas: 1, acrescimoPercentual: 0, entradaPercentual: 0 })

const TIPOS_INTERNACAO = ["Apartamento", "Enfermaria", "UTI", "Ambulatorial"]
const TIPOS_ANESTESIA  = ["Local", "Sedacao", "Geral", "Raquianestesia", "Peridural", "Bloqueio"]

// ── Preview (totais do servidor) ──────────────────────────────────────────────
const previewPayload = computed<PreviewOrcamentoPayload>(() => ({
    itens: orcamento.value?.itens ?? [],
    equipe: equipe.value.map(e => ({ profissionalUsuarioId: e.profissionalUsuarioId, papel: e.papel, valor: Number(e.valor) })),
    implantes: implantes.value.map(i => ({ itemInventarioId: i.itemInventarioId, descricao: i.descricao, quantidade: Number(i.quantidade), custoUnitario: Number(i.custoUnitario) })),
    formasPagamento: formas.value.map(f => ({
        formaPagamentoId: f.formaPagamentoId, valor: Number(f.valor), parcelas: Number(f.parcelas),
        acrescimoPercentual: Number(f.acrescimoPercentual), entradaPercentual: Number(f.entradaPercentual), observacao: f.observacao,
    })),
    cirurgias: cirurgias.value.map(c => ({ procedimentoCirurgicoId: c.procedimentoCirurgicoId, descricao: c.descricao, quantidade: Number(c.quantidade), duracaoMinutos: c.duracaoMinutos, valorTotal: Number(c.valorTotal) })),
    internacao: internacao.value ? { tipo: internacao.value.tipoInternacao, dias: Number(internacao.value.dias), valorDiaria: Number(internacao.value.valorDiaria) } : null,
    anestesia:  anestesia.value  ? { tipo: anestesia.value.tipoAnestesia, valor: Number(anestesia.value.valor), observacao: anestesia.value.observacao } : null,
}))

const { preview, carregando: calculando } = usePreviewOrcamento(previewPayload)

const totalCirurgias = computed(() => preview.value?.totalCirurgias ?? 0)
const totalEquipe    = computed(() => preview.value?.totalEquipe    ?? 0)
const totalImplantes = computed(() => preview.value?.totalImplantes ?? 0)
const totalInternacao= computed(() => preview.value?.totalInternacao?? 0)
const totalAnestesia = computed(() => preview.value?.totalAnestesia ?? 0)
const subtotalPreview= computed(() => preview.value?.totalGeral     ?? 0)
const somaFormas     = computed(() => preview.value?.somaFormas     ?? 0)
const diferenca      = computed(() => preview.value?.diferenca      ?? 0)
const integridadeOk  = computed(() => preview.value?.integridadeOk  ?? true)

// Desconto aplicado
const descontoValor  = computed(() => tipoDesconto.value === "percentual"
    ? subtotalPreview.value * (desconto.value / 100)
    : desconto.value
)
const totalComDesconto = computed(() => Math.max(0, subtotalPreview.value - descontoValor.value))

// ── Watchers auto-cálculo internação ─────────────────────────────────────────
watch(() => internacao.value?.dias, (d) => {
    if (internacao.value && d) internacao.value.valorTotal = Number(d) * Number(internacao.value.valorDiaria)
})
watch(() => internacao.value?.valorDiaria, (vd) => {
    if (internacao.value && vd != null) internacao.value.valorTotal = Number(internacao.value.dias) * Number(vd)
})

function fmt(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }

// ── Catálogo handlers ─────────────────────────────────────────────────────────
function onSelecionarCatCirurgia(id: number) {
    const c = catCirurgias.value.find(x => x.id === id)
    if (!c) return
    novaCirurgia.value.descricao     = c.descricao
    novaCirurgia.value.duracaoMinutos= c.duracaoPadraoMinutos
    novaCirurgia.value.valorUnitario = c.valorBase
}
function onSelecionarCatValor(id: number) {
    const v = catValores.value.find(x => x.id === id)
    if (!v) return
    novoMembro.value.papel                = v.funcao
    novoMembro.value.profissionalUsuarioId= v.profissionalUsuarioId ?? ""
    novoMembro.value.valor               = Number(v.valorTempoBase)
}
function onSelecionarCatImplante(id: number) {
    const i = catImplantes.value.find(x => x.id === id)
    if (!i) return
    novoImplante.value.descricao   = i.descricao
    novoImplante.value.custoUnitario = Number(i.custoUnitario)
}
function onSelecionarCatEquipe(id: number) {
    const e = catEquipes.value.find(x => x.id === id)
    if (!e) return
    novaEquipeEsp.value.descricao = e.descricao
    novaEquipeEsp.value.valor     = Number(e.valorPadrao)
}
function onSelecionarConfigPagto(id: number) {
    const c = catConfigsPgto.value.find(x => x.formaPagamentoId === id)
    if (!c) return
    novaForma.value.acrescimoPercentual = Number(c.acrescimoPercentual)
    novaForma.value.entradaPercentual   = Number(c.entradaPercentualPadrao)
}

// ── Adicionar / remover ───────────────────────────────────────────────────────
function adicionarCirurgia() {
    const f = novaCirurgia.value
    if (!f.descricao || f.quantidade <= 0) return
    cirurgias.value.push({
        procedimentoCirurgicoId: null,
        descricao: f.descricao,
        quantidade: f.quantidade,
        duracaoMinutos: f.duracaoMinutos,
        valorTotal: f.valorUnitario * f.quantidade,
    })
    novaCirurgia.value = { catalogoId: 0, descricao: "", quantidade: 1, duracaoMinutos: null, valorUnitario: 0 }
}
function removerCirurgia(idx: number) { cirurgias.value.splice(idx, 1) }

function adicionarMembro() {
    const f = novoMembro.value
    if (!f.profissionalUsuarioId || f.valor < 0) return
    equipe.value.push({ profissionalUsuarioId: f.profissionalUsuarioId, papel: f.papel, valor: f.valor })
    novoMembro.value = { catalogoValorId: 0, profissionalUsuarioId: "", papel: "Cirurgião", valor: 0 }
}
function removerMembro(idx: number) { equipe.value.splice(idx, 1) }

function adicionarImplante() {
    const f = novoImplante.value
    if (!f.descricao || f.quantidade <= 0) return
    implantes.value.push({ itemInventarioId: null, descricao: f.descricao, quantidade: f.quantidade, custoUnitario: f.custoUnitario, custoTotal: f.quantidade * f.custoUnitario })
    novoImplante.value = { catalogoId: 0, descricao: "", quantidade: 1, custoUnitario: 0 }
}
function removerImplante(idx: number) { implantes.value.splice(idx, 1) }

function adicionarEquipeEsp() {
    const f = novaEquipeEsp.value
    if (!f.descricao) return
    equipe.value.push({ profissionalUsuarioId: "00000000-0000-0000-0000-000000000000", papel: f.descricao, valor: f.valor })
    novaEquipeEsp.value = { catalogoId: 0, descricao: "", valor: 0 }
}

function adicionarForma() {
    const f = novaForma.value
    if (!f.formaPagamentoId || f.valor <= 0) return
    const fp = formasPagamento.value.find(x => x.id === f.formaPagamentoId)
    formas.value.push({ formaPagamentoId: f.formaPagamentoId, formaPagamentoNome: fp?.nome, valor: f.valor, parcelas: f.parcelas, acrescimoPercentual: f.acrescimoPercentual, entradaPercentual: f.entradaPercentual, observacao: null })
    novaForma.value = { formaPagamentoId: 0, valor: 0, parcelas: 1, acrescimoPercentual: 0, entradaPercentual: 0 }
}
function removerForma(idx: number) { formas.value.splice(idx, 1) }

function inicializarInternacao() {
    if (internacao.value) { internacao.value = null; return }
    internacao.value = { tipoInternacao: "Apartamento", dias: 1, valorDiaria: 0, valorTotal: 0 }
}
function inicializarAnestesia() {
    if (anestesia.value) { anestesia.value = null; return }
    anestesia.value = { tipoAnestesia: "Local", valor: 0, observacao: null }
}

// ── Carregar / salvar ─────────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const [orc, fps, cirs, vals, eqs, imps, cfgPg] = await Promise.all([
            orcamentoService.obter(orcamentoId),
            formaPagamentoService.listar(),
            orcamentoCatalogoService.listarCirurgias(true),
            orcamentoCatalogoService.listarValoresProfissional(true),
            orcamentoCatalogoService.listarEquipes(true),
            orcamentoCatalogoService.listarImplantes(true),
            orcamentoCatalogoService.listarConfigPagamento(true),
        ])
        orcamento.value = orc
        validade.value  = orc.validade
        observacoes.value = orc.observacoes ?? ""
        procedimentoCirurgicoId.value = orc.procedimentoCirurgicoId
        cirurgias.value = [...orc.cirurgias]
        equipe.value    = [...orc.equipe]
        implantes.value = [...orc.implantes]
        formas.value    = [...orc.formasPagamento]
        internacao.value= orc.internacao ? { ...orc.internacao } : null
        anestesia.value = orc.anestesia  ? { ...orc.anestesia  } : null
        formasPagamento.value = fps
        catCirurgias.value    = cirs
        catValores.value      = vals
        catEquipes.value      = eqs
        catImplantes.value    = imps
        catConfigsPgto.value  = cfgPg
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar orçamento."
    } finally {
        carregando.value = false
    }
}

async function salvar() {
    if (!orcamento.value) return
    if (!integridadeOk.value) {
        erro.value = "A soma das formas de pagamento deve ser igual ao total do orçamento."
        return
    }
    salvando.value = true
    erro.value = null
    try {
        await orcamentoService.atualizar(orcamentoId, {
            validade: validade.value,
            observacoes: observacoes.value || null,
            procedimentoCirurgicoId: procedimentoCirurgicoId.value,
            itens: orcamento.value.itens,
            equipe: equipe.value,
            implantes: implantes.value,
            formasPagamento: formas.value,
            cirurgias: cirurgias.value,
            internacao: internacao.value ? { tipo: internacao.value.tipoInternacao, dias: internacao.value.dias, valorDiaria: internacao.value.valorDiaria } : null,
            anestesia:  anestesia.value  ? { tipo: anestesia.value.tipoAnestesia, valor: anestesia.value.valor, observacao: anestesia.value.observacao } : null,
        })
        router.push({ name: "OrcamentoDetalhe", params: { id: String(orcamentoId) } })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

function voltar() { router.push({ name: "OrcamentoDetalhe", params: { id: String(orcamentoId) } }) }

onMounted(carregar)
</script>

<template>
    <div class="app-page app-page--wide">
        <!-- Loading inicial -->
        <div v-if="carregando && !orcamento" class="estado-loading">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando orçamento...
        </div>

        <!-- Erro fatal -->
        <div v-else-if="erro && !orcamento" class="erro-banner">
            {{ erro }}
            <AppButton size="sm" variant="ghost" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="orcamento">
            <!-- Header -->
            <div class="form-header">
                <div class="form-header-l">
                    <button type="button" class="btn-back" @click="voltar" aria-label="Cancelar edição">
                        <i class="fa-solid fa-arrow-left"></i>
                    </button>
                    <div>
                        <div class="form-crumb">Orçamentos / {{ orcamento.numero || `#${orcamento.id}` }}</div>
                        <h1 class="form-titulo">{{ orcamento.numero ? `Editar ${orcamento.numero}` : "Editar orçamento" }}</h1>
                    </div>
                    <OrcamentoStatusPill :status="orcamento.status" />
                </div>
                <div class="form-header-r">
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Cancelar</AppButton>
                    <AppButton
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        :disabled="!integridadeOk"
                        :title="integridadeOk ? '' : 'Corrija a integridade das formas de pagamento'"
                        @click="salvar"
                    >Salvar</AppButton>
                </div>
            </div>

            <!-- Erro de ação -->
            <div v-if="erro" class="erro-banner" role="alert">{{ erro }}</div>

            <!-- Abas de seção -->
            <AppTabs v-model="aba" :abas="abas" variante="underline" aria-label="Seções do orçamento" />

            <!-- Grid: conteúdo + sidebar -->
            <div class="form-grid">
                <div class="col-principal">

                    <!-- ──── Paciente ────────────────────────────────────── -->
                    <section v-if="aba === 'paciente'">
                        <AppCard title="Cabeçalho do orçamento">
                            <div class="fg">
                                <AppField label="Paciente">
                                    <AppInput :model-value="orcamento.pacienteNome" readonly />
                                </AppField>
                                <AppField label="Número">
                                    <AppInput :model-value="orcamento.numero || `#${orcamento.id}`" readonly />
                                </AppField>
                                <AppField label="Validade" for="form-validade">
                                    <AppInput id="form-validade" type="date" v-model="validade" />
                                </AppField>
                                <AppField label="Procedimento cirúrgico (ID, opcional)" for="form-proc">
                                    <AppInput id="form-proc" type="number" v-model="procedimentoCirurgicoId" />
                                </AppField>
                            </div>
                            <AppField label="Observações" for="form-obs" class="mt-2">
                                <AppTextarea
                                    id="form-obs"
                                    v-model="observacoes"
                                    :rows="3"
                                    placeholder="Anotações livres sobre este orçamento"
                                />
                            </AppField>
                        </AppCard>
                    </section>

                    <!-- ──── Cirurgias ───────────────────────────────────── -->
                    <section v-else-if="aba === 'cirurgias'">
                        <AppCard title="Cirurgias incluídas">
                            <table v-if="cirurgias.length" class="tabela">
                                <thead>
                                    <tr><th>Descrição</th><th>Qtd</th><th>Duração</th><th class="r">Total</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(c, idx) in cirurgias" :key="idx">
                                        <td>{{ c.descricao }}</td>
                                        <td>{{ c.quantidade }}</td>
                                        <td>{{ c.duracaoMinutos ? `${c.duracaoMinutos} min` : "—" }}</td>
                                        <td class="r">{{ fmt(c.valorTotal) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerCirurgia(idx)" title="Remover">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhuma cirurgia adicionada ainda.</p>

                            <div class="add-section">
                                <h4 class="add-titulo">Adicionar cirurgia</h4>
                                <div class="fg">
                                    <AppField label="Do catálogo (auto-preenche)">
                                        <AppSelect
                                            :model-value="novaCirurgia.catalogoId"
                                            @update:model-value="(v: unknown) => onSelecionarCatCirurgia(Number(v))"
                                        >
                                            <option :value="0">Selecione...</option>
                                            <option v-for="c in catCirurgias" :key="c.id" :value="c.id">{{ c.descricao }}</option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Descrição">
                                        <AppInput v-model="novaCirurgia.descricao" />
                                    </AppField>
                                    <AppField label="Quantidade">
                                        <AppInput type="number" :min="1" v-model="novaCirurgia.quantidade" />
                                    </AppField>
                                    <AppField label="Duração (min)">
                                        <AppInput type="number" v-model="novaCirurgia.duracaoMinutos" />
                                    </AppField>
                                    <AppField label="Valor unitário (R$)">
                                        <AppInput type="number" :step="0.01" v-model="novaCirurgia.valorUnitario" />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarCirurgia">Adicionar</AppButton>
                                </div>
                            </div>
                        </AppCard>
                    </section>

                    <!-- ──── Equipe & Implantes ──────────────────────────── -->
                    <section v-else-if="aba === 'equipeImplantes'" class="secao-multi">
                        <AppCard title="Equipe profissional">
                            <table v-if="equipe.length" class="tabela">
                                <thead>
                                    <tr><th>Profissional</th><th>Função</th><th class="r">Honorário</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(m, idx) in equipe" :key="idx">
                                        <td>{{ m.profissionalNome ?? m.profissionalUsuarioId.slice(0, 8) }}</td>
                                        <td>{{ m.papel }}</td>
                                        <td class="r">{{ fmt(m.valor) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerMembro(idx)" title="Remover">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhum membro adicionado.</p>

                            <div class="add-section">
                                <h4 class="add-titulo">Adicionar membro</h4>
                                <div class="fg">
                                    <AppField label="Tabela de valor (catálogo)">
                                        <AppSelect
                                            :model-value="novoMembro.catalogoValorId"
                                            @update:model-value="(v: unknown) => onSelecionarCatValor(Number(v))"
                                        >
                                            <option :value="0">Selecione...</option>
                                            <option v-for="v in catValores" :key="v.id" :value="v.id">
                                                {{ v.funcao }} {{ v.profissionalNome ? `— ${v.profissionalNome}` : "(padrão)" }}
                                            </option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Profissional UUID">
                                        <AppInput v-model="novoMembro.profissionalUsuarioId" placeholder="UUID" />
                                    </AppField>
                                    <AppField label="Função">
                                        <AppInput v-model="novoMembro.papel" />
                                    </AppField>
                                    <AppField label="Honorário (R$)">
                                        <AppInput type="number" :step="0.01" v-model="novoMembro.valor" />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarMembro">Adicionar</AppButton>
                                </div>
                            </div>

                            <div class="add-section mt-3">
                                <h4 class="add-titulo">Equipe especializada (do catálogo)</h4>
                                <div class="fg">
                                    <AppField label="Catálogo de equipes">
                                        <AppSelect
                                            :model-value="novaEquipeEsp.catalogoId"
                                            @update:model-value="(v: unknown) => onSelecionarCatEquipe(Number(v))"
                                        >
                                            <option :value="0">Selecione...</option>
                                            <option v-for="e in catEquipes" :key="e.id" :value="e.id">{{ e.descricao }}</option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Descrição">
                                        <AppInput v-model="novaEquipeEsp.descricao" />
                                    </AppField>
                                    <AppField label="Valor (R$)">
                                        <AppInput type="number" :step="0.01" v-model="novaEquipeEsp.valor" />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" variant="ghost" icon="fa-solid fa-plus" @click="adicionarEquipeEsp">Incluir equipe</AppButton>
                                </div>
                            </div>
                        </AppCard>

                        <AppCard title="Implantes">
                            <table v-if="implantes.length" class="tabela">
                                <thead>
                                    <tr><th>Descrição</th><th>Qtd</th><th class="r">Custo unit.</th><th class="r">Total</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(imp, idx) in implantes" :key="idx">
                                        <td>{{ imp.descricao }}</td>
                                        <td>{{ imp.quantidade }}</td>
                                        <td class="r">{{ fmt(imp.custoUnitario) }}</td>
                                        <td class="r">{{ fmt(imp.custoTotal) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerImplante(idx)" title="Remover">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhum implante adicionado.</p>

                            <div class="add-section">
                                <h4 class="add-titulo">Adicionar implante</h4>
                                <div class="fg">
                                    <AppField label="Catálogo de implantes">
                                        <AppSelect
                                            :model-value="novoImplante.catalogoId"
                                            @update:model-value="(v: unknown) => onSelecionarCatImplante(Number(v))"
                                        >
                                            <option :value="0">Selecione...</option>
                                            <option v-for="i in catImplantes" :key="i.id" :value="i.id">{{ i.descricao }}</option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Descrição">
                                        <AppInput v-model="novoImplante.descricao" />
                                    </AppField>
                                    <AppField label="Quantidade">
                                        <AppInput type="number" :min="1" :step="0.001" v-model="novoImplante.quantidade" />
                                    </AppField>
                                    <AppField label="Custo unitário (R$)">
                                        <AppInput type="number" :step="0.01" v-model="novoImplante.custoUnitario" />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarImplante">Adicionar</AppButton>
                                </div>
                            </div>
                        </AppCard>
                    </section>

                    <!-- ──── Local & Pagamento ───────────────────────────── -->
                    <section v-else-if="aba === 'pagamento'" class="secao-multi">
                        <AppCard title="Internação">
                            <p v-if="!internacao" class="texto-aux">
                                Sem internação configurada.
                                <AppButton size="sm" variant="ghost" @click="inicializarInternacao">Adicionar</AppButton>
                            </p>
                            <template v-else>
                                <div class="fg">
                                    <AppField label="Tipo">
                                        <AppSelect v-model="internacao.tipoInternacao">
                                            <option v-for="t in TIPOS_INTERNACAO" :key="t" :value="t">{{ t }}</option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Dias">
                                        <AppInput type="number" :min="1" v-model="internacao.dias" />
                                    </AppField>
                                    <AppField label="Diária (R$)">
                                        <AppInput type="number" :step="0.01" v-model="internacao.valorDiaria" />
                                    </AppField>
                                    <AppField label="Total">
                                        <AppInput :model-value="fmt(internacao.valorTotal)" readonly />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" variant="ghost" @click="inicializarInternacao">Remover</AppButton>
                                </div>
                            </template>
                        </AppCard>

                        <AppCard title="Anestesia">
                            <p v-if="!anestesia" class="texto-aux">
                                Sem anestesia configurada.
                                <AppButton size="sm" variant="ghost" @click="inicializarAnestesia">Adicionar</AppButton>
                            </p>
                            <template v-else>
                                <div class="fg">
                                    <AppField label="Tipo">
                                        <AppSelect v-model="anestesia.tipoAnestesia">
                                            <option v-for="t in TIPOS_ANESTESIA" :key="t" :value="t">{{ t }}</option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Valor (R$)">
                                        <AppInput type="number" :step="0.01" v-model="anestesia.valor" />
                                    </AppField>
                                    <AppField label="Observação">
                                        <AppInput v-model="anestesia.observacao" />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" variant="ghost" @click="inicializarAnestesia">Remover</AppButton>
                                </div>
                            </template>
                        </AppCard>

                        <AppCard title="Formas de pagamento">
                            <table v-if="formas.length" class="tabela">
                                <thead>
                                    <tr><th>Forma</th><th class="r">Valor</th><th class="r">Parcelas</th><th class="r">Acréscimo</th><th class="r">Entrada</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(f, idx) in formas" :key="idx">
                                        <td>{{ f.formaPagamentoNome }}</td>
                                        <td class="r">{{ fmt(f.valor) }}</td>
                                        <td class="r">{{ f.parcelas }}x</td>
                                        <td class="r">{{ f.acrescimoPercentual }}%</td>
                                        <td class="r">{{ f.entradaPercentual }}%</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerForma(idx)" title="Remover">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhuma forma de pagamento adicionada.</p>

                            <div class="add-section">
                                <h4 class="add-titulo">Adicionar forma</h4>
                                <div class="fg">
                                    <AppField label="Forma de pagamento">
                                        <AppSelect
                                            :model-value="novaForma.formaPagamentoId"
                                            @update:model-value="(v: unknown) => { novaForma.formaPagamentoId = Number(v); onSelecionarConfigPagto(Number(v)) }"
                                        >
                                            <option :value="0">Selecione...</option>
                                            <option v-for="fp in formasPagamento" :key="fp.id" :value="fp.id">{{ fp.nome }}</option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Valor (R$)">
                                        <AppInput type="number" :step="0.01" v-model="novaForma.valor" />
                                    </AppField>
                                    <AppField label="Parcelas">
                                        <AppInput type="number" :min="1" v-model="novaForma.parcelas" />
                                    </AppField>
                                    <AppField label="Acréscimo (%)">
                                        <AppInput type="number" :step="0.01" v-model="novaForma.acrescimoPercentual" />
                                    </AppField>
                                    <AppField label="Entrada (%)">
                                        <AppInput type="number" :step="0.01" v-model="novaForma.entradaPercentual" />
                                    </AppField>
                                </div>
                                <div class="add-acoes">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarForma">Adicionar</AppButton>
                                </div>
                            </div>
                        </AppCard>
                    </section>
                </div>

                <!-- Sidebar de resumo -->
                <aside class="col-resumo">
                    <OrcamentoResumoSidebar
                        :subtotal="subtotalPreview"
                        :desconto="desconto"
                        :tipo-desconto="tipoDesconto"
                        :total-geral="totalComDesconto"
                        :soma-formas="somaFormas"
                        :integridade-ok="integridadeOk"
                        :diferenca="diferenca"
                        :salvando="salvando"
                        :calculando="calculando"
                        @update:desconto="desconto = $event"
                        @update:tipo-desconto="tipoDesconto = $event"
                        @salvar="salvar"
                    />
                </aside>
            </div>

            <!-- Sticky bar inferior -->
            <div class="sticky-bar">
                <div class="sticky-l">
                    <div class="sticky-total">
                        <span class="sticky-label">Total do orçamento</span>
                        <strong>{{ fmt(totalComDesconto) }}</strong>
                    </div>
                    <div class="sticky-sub" v-if="cirurgias.length">
                        {{ cirurgias.length }} {{ cirurgias.length === 1 ? "cirurgia" : "cirurgias" }}
                        · validade {{ new Date(validade + "T00:00:00").toLocaleDateString("pt-BR") }}
                    </div>
                </div>
                <div class="sticky-r">
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Cancelar</AppButton>
                    <AppButton
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        :disabled="!integridadeOk"
                        @click="salvar"
                    >Salvar orçamento</AppButton>
                </div>
            </div>
        </template>
    </div>
</template>

<style scoped>
.estado-loading {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    padding: 3rem 0;
    font-size: 0.9em;
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
    font-size: 0.875rem;
}

/* Header */
.form-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
}
.form-header-l {
    display: flex;
    align-items: center;
    gap: 14px;
}
.form-header-r { display: flex; gap: 8px; }

.btn-back {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.12);
    display: flex;
    align-items: center;
    justify-content: center;
    color: hsl(var(--secondary));
    cursor: pointer;
    flex-shrink: 0;
    font-size: 14px;
    transition: background 0.12s;
}
.btn-back:hover { background: hsl(var(--secondary) / 0.04); }

.form-crumb  { font-size: 11.5px; color: hsl(var(--secondary) / 0.55); margin-bottom: 2px; }
.form-titulo { font-size: 20px; font-weight: 700; color: hsl(var(--secondary)); margin: 0; }

/* Grid */
.form-grid {
    display: grid;
    grid-template-columns: 1fr 300px;
    gap: 22px;
    align-items: start;
}
@media (max-width: 1100px) {
    .form-grid { grid-template-columns: 1fr; }
    .col-resumo { position: static; }
}

.col-principal { display: flex; flex-direction: column; gap: 16px; }
.col-resumo { position: sticky; top: 80px; }

.secao-multi { display: flex; flex-direction: column; gap: 16px; }

/* Form grid de campos */
.fg {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 0.75rem;
}

/* Adicionar section */
.add-section {
    margin-top: 0.85rem;
    padding-top: 0.85rem;
    border-top: 1px dashed hsl(var(--secondary) / 0.12);
}
.add-titulo {
    font-size: 0.82em;
    font-weight: 700;
    color: var(--text-muted);
    margin: 0 0 0.6rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.add-acoes {
    display: flex;
    justify-content: flex-end;
    margin-top: 0.5rem;
}

/* Tabela */
.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875em;
    margin-bottom: 0.25rem;
}
.tabela th, .tabela td {
    padding: 0.5rem 0.75rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.tabela th {
    font-size: 0.75em;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--text-muted);
    background: hsl(var(--secondary) / 0.03);
}
.tabela tr:last-child td { border-bottom: none; }
.tabela .r { text-align: right; }

.texto-aux { color: var(--text-muted); font-size: 0.87em; margin: 0.5rem 0; }

.mt-2 { margin-top: 0.5rem; }
.mt-3 { margin-top: 0.75rem; }

/* Sticky bar */
.sticky-bar {
    position: fixed;
    bottom: 0;
    left: var(--sidebar-w, 240px);
    right: 0;
    background: hsl(var(--card));
    border-top: 1px solid hsl(var(--secondary) / 0.1);
    box-shadow: 0 -4px 14px hsl(var(--secondary) / 0.06);
    padding: 12px 26px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 14px;
    z-index: 50;
}
.sticky-l { display: flex; flex-direction: column; gap: 2px; }
.sticky-total {
    display: flex;
    align-items: baseline;
    gap: 8px;
}
.sticky-label { font-size: 12px; color: hsl(var(--secondary) / 0.6); }
.sticky-total strong { font-size: 20px; font-weight: 700; color: hsl(var(--primary)); }
.sticky-sub { font-size: 11.5px; color: hsl(var(--secondary) / 0.55); }
.sticky-r { display: flex; gap: 8px; }

/* Padding-bottom para a sticky bar não cobrir conteúdo */
.app-page { padding-bottom: 100px; }
</style>
