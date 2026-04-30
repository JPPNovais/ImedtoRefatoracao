<script setup lang="ts">
/**
 * OrcamentoFormView — formulário tabbed do orçamento (paridade UX legado).
 * Consome os catálogos da Fase 6.1 para autocompletar valores ao adicionar
 * cirurgias, profissionais, equipes e implantes.
 *
 * Abas:
 *   1. Paciente       — validade, observações, procedimento cirúrgico ref.
 *   2. Cirurgias      — adicionar do catálogo, ajustar qtd/duração.
 *   3. Equipe & Implantes — honorários (catálogo de valor) + implantes.
 *   4. Local & Pagamento — internação + anestesia + formas de pagamento.
 *
 * Resumo sticky lateral com integridade (soma das formas vs. total).
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
    AppPageHeader, AppButton, AppBadge, AppTabs, AppCard,
    AppField, AppInput, AppTextarea, AppSelect,
} from "@/components/ui"

const route = useRoute()
const router = useRouter()
const orcamentoId = Number(route.params.id)

const orcamento = ref<Orcamento | null>(null)
const carregando = ref(false)
const salvando = ref(false)
const erro = ref<string | null>(null)

// Catálogos.
const catCirurgias = ref<CatalogoCirurgia[]>([])
const catValores = ref<ValorProfissionalOrcamentoCatalogo[]>([])
const catEquipes = ref<CatalogoEquipe[]>([])
const catImplantes = ref<CatalogoImplante[]>([])
const catConfigsPagto = ref<ConfiguracaoPagamentoCatalogo[]>([])
const formasPagamento = ref<FormaPagamento[]>([])

// Estado editável (cópia do aggregate carregado).
const validade = ref("")
const observacoes = ref("")
const procedimentoCirurgicoId = ref<number | null>(null)

const cirurgias = ref<OrcamentoCirurgia[]>([])
const equipe = ref<OrcamentoEquipe[]>([])
const implantes = ref<OrcamentoImplante[]>([])
const formas = ref<OrcamentoFormaPagamento[]>([])
const internacao = ref<OrcamentoInternacao | null>(null)
const anestesia = ref<OrcamentoAnestesia | null>(null)

// ─── Tabs ───
type AbaKey = "paciente" | "cirurgias" | "equipeImplantes" | "pagamento"
const aba = ref<AbaKey>("paciente")
const abas = [
    { valor: "paciente", label: "Paciente", icone: "fa-solid fa-user" },
    { valor: "cirurgias", label: "Cirurgias", icone: "fa-solid fa-scalpel" },
    { valor: "equipeImplantes", label: "Equipe & implantes", icone: "fa-solid fa-users" },
    { valor: "pagamento", label: "Local & pagamento", icone: "fa-solid fa-credit-card" },
]

// ─── Form helpers para adições ───
const novaCirurgia = ref({
    catalogoId: 0, descricao: "", quantidade: 1, duracaoMinutos: null as number | null, valorUnitario: 0,
})
const novoMembro = ref({
    catalogoValorId: 0, profissionalUsuarioId: "", papel: "Cirurgião", valor: 0,
})
const novoImplante = ref({
    catalogoId: 0, descricao: "", quantidade: 1, custoUnitario: 0,
})
const novaEquipeEsp = ref({ catalogoId: 0, descricao: "", valor: 0 })
const novaForma = ref({
    formaPagamentoId: 0,
    valor: 0,
    parcelas: 1,
    acrescimoPercentual: 0,
    entradaPercentual: 0,
})

const TIPOS_INTERNACAO = ["Apartamento", "Enfermaria", "UTI", "Ambulatorial"]
const TIPOS_ANESTESIA = ["Local", "Sedacao", "Geral", "Raquianestesia", "Peridural", "Bloqueio"]

// ─── Totais & integridade — vêm do servidor (Fase 6.3 — fonte da verdade) ───
// Payload reativo enviado ao endpoint POST /orcamentos/preview com debounce de 250ms.
const previewPayload = computed<PreviewOrcamentoPayload>(() => ({
    itens: orcamento.value?.itens ?? [],
    equipe: equipe.value.map(e => ({
        profissionalUsuarioId: e.profissionalUsuarioId,
        papel: e.papel,
        valor: Number(e.valor),
    })),
    implantes: implantes.value.map(i => ({
        itemInventarioId: i.itemInventarioId,
        descricao: i.descricao,
        quantidade: Number(i.quantidade),
        custoUnitario: Number(i.custoUnitario),
    })),
    formasPagamento: formas.value.map(f => ({
        formaPagamentoId: f.formaPagamentoId,
        valor: Number(f.valor),
        parcelas: Number(f.parcelas),
        acrescimoPercentual: Number(f.acrescimoPercentual),
        entradaPercentual: Number(f.entradaPercentual),
        observacao: f.observacao,
    })),
    cirurgias: cirurgias.value.map(c => ({
        procedimentoCirurgicoId: c.procedimentoCirurgicoId,
        descricao: c.descricao,
        quantidade: Number(c.quantidade),
        duracaoMinutos: c.duracaoMinutos,
        valorTotal: Number(c.valorTotal),
    })),
    internacao: internacao.value
        ? { tipo: internacao.value.tipoInternacao, dias: Number(internacao.value.dias), valorDiaria: Number(internacao.value.valorDiaria) }
        : null,
    anestesia: anestesia.value
        ? { tipo: anestesia.value.tipoAnestesia, valor: Number(anestesia.value.valor), observacao: anestesia.value.observacao }
        : null,
}))

const { preview, carregando: calculando } = usePreviewOrcamento(previewPayload)

// Acessores convenientes — todos derivados do preview do servidor.
const totalCirurgias = computed(() => preview.value?.totalCirurgias ?? 0)
const totalEquipe = computed(() => preview.value?.totalEquipe ?? 0)
const totalImplantes = computed(() => preview.value?.totalImplantes ?? 0)
const totalInternacao = computed(() => preview.value?.totalInternacao ?? 0)
const totalAnestesia = computed(() => preview.value?.totalAnestesia ?? 0)
const totalGeral = computed(() => preview.value?.totalGeral ?? 0)
const somaFormas = computed(() => preview.value?.somaFormas ?? 0)
const diferenca = computed(() => preview.value?.diferenca ?? 0)
const integridadeOk = computed(() => preview.value?.integridadeOk ?? true)

// ─── Watchers de auto-cálculo ───
watch(() => internacao.value?.dias, (d) => {
    if (internacao.value && d) internacao.value.valorTotal = Number(d) * Number(internacao.value.valorDiaria)
})
watch(() => internacao.value?.valorDiaria, (vd) => {
    if (internacao.value && vd != null) internacao.value.valorTotal = Number(internacao.value.dias) * Number(vd)
})

function fmt(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }

// ─── Catálogo handlers (selecionar pré-preenche form de adição) ───
function onSelecionarCatCirurgia(id: number) {
    const c = catCirurgias.value.find(x => x.id === id)
    if (!c) return
    novaCirurgia.value.descricao = c.descricao
    novaCirurgia.value.duracaoMinutos = c.duracaoPadraoMinutos
    novaCirurgia.value.valorUnitario = c.valorBase
}

function onSelecionarCatValor(id: number) {
    const v = catValores.value.find(x => x.id === id)
    if (!v) return
    novoMembro.value.papel = v.funcao
    novoMembro.value.profissionalUsuarioId = v.profissionalUsuarioId ?? ""
    novoMembro.value.valor = Number(v.valorTempoBase)
}

function onSelecionarCatImplante(id: number) {
    const i = catImplantes.value.find(x => x.id === id)
    if (!i) return
    novoImplante.value.descricao = i.descricao
    novoImplante.value.custoUnitario = Number(i.custoUnitario)
}

function onSelecionarCatEquipe(id: number) {
    const e = catEquipes.value.find(x => x.id === id)
    if (!e) return
    novaEquipeEsp.value.descricao = e.descricao
    novaEquipeEsp.value.valor = Number(e.valorPadrao)
}

function onSelecionarConfigPagto(id: number) {
    const c = catConfigsPagto.value.find(x => x.formaPagamentoId === id)
    if (!c) return
    novaForma.value.acrescimoPercentual = Number(c.acrescimoPercentual)
    novaForma.value.entradaPercentual = Number(c.entradaPercentualPadrao)
}

// ─── Adicionar / remover ───
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
    equipe.value.push({
        profissionalUsuarioId: f.profissionalUsuarioId,
        papel: f.papel,
        valor: f.valor,
    })
    novoMembro.value = { catalogoValorId: 0, profissionalUsuarioId: "", papel: "Cirurgião", valor: 0 }
}
function removerMembro(idx: number) { equipe.value.splice(idx, 1) }

function adicionarImplante() {
    const f = novoImplante.value
    if (!f.descricao || f.quantidade <= 0) return
    implantes.value.push({
        itemInventarioId: null,
        descricao: f.descricao,
        quantidade: f.quantidade,
        custoUnitario: f.custoUnitario,
        custoTotal: f.quantidade * f.custoUnitario,
    })
    novoImplante.value = { catalogoId: 0, descricao: "", quantidade: 1, custoUnitario: 0 }
}
function removerImplante(idx: number) { implantes.value.splice(idx, 1) }

function adicionarEquipeEsp() {
    const f = novaEquipeEsp.value
    if (!f.descricao) return
    // Equipe especializada vira um "papel" no array de equipe (sem profissional individual).
    equipe.value.push({
        profissionalUsuarioId: "00000000-0000-0000-0000-000000000000",
        papel: f.descricao,
        valor: f.valor,
    })
    novaEquipeEsp.value = { catalogoId: 0, descricao: "", valor: 0 }
}

function adicionarForma() {
    const f = novaForma.value
    if (!f.formaPagamentoId || f.valor <= 0) return
    const fp = formasPagamento.value.find(x => x.id === f.formaPagamentoId)
    formas.value.push({
        formaPagamentoId: f.formaPagamentoId,
        formaPagamentoNome: fp?.nome,
        valor: f.valor,
        parcelas: f.parcelas,
        acrescimoPercentual: f.acrescimoPercentual,
        entradaPercentual: f.entradaPercentual,
        observacao: null,
    })
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

// ─── Carregar / salvar ───
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
        validade.value = orc.validade
        observacoes.value = orc.observacoes ?? ""
        procedimentoCirurgicoId.value = orc.procedimentoCirurgicoId
        cirurgias.value = [...orc.cirurgias]
        equipe.value = [...orc.equipe]
        implantes.value = [...orc.implantes]
        formas.value = [...orc.formasPagamento]
        internacao.value = orc.internacao ? { ...orc.internacao } : null
        anestesia.value = orc.anestesia ? { ...orc.anestesia } : null
        formasPagamento.value = fps
        catCirurgias.value = cirs
        catValores.value = vals
        catEquipes.value = eqs
        catImplantes.value = imps
        catConfigsPagto.value = cfgPg
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
            internacao: internacao.value
                ? { tipo: internacao.value.tipoInternacao, dias: internacao.value.dias, valorDiaria: internacao.value.valorDiaria }
                : null,
            anestesia: anestesia.value
                ? { tipo: anestesia.value.tipoAnestesia, valor: anestesia.value.valor, observacao: anestesia.value.observacao }
                : null,
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
    <main class="app-page app-page--wide">
        <div v-if="carregando" class="estado">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>
        <div v-else-if="erro && !orcamento" class="erro-banner">
            {{ erro }}
            <AppButton size="sm" variant="ghost" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="orcamento">
            <AppPageHeader
                :titulo="`Editar orçamento ${orcamento.numero || `#${orcamento.id}`}`"
                :subtitulo="orcamento.pacienteNome"
            >
                <template #acoes>
                    <AppBadge :status="orcamento.status" />
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Cancelar</AppButton>
                    <AppButton
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        :disabled="!integridadeOk"
                        :title="integridadeOk ? '' : 'Corrija a integridade das formas de pagamento'"
                        @click="salvar"
                    >Salvar</AppButton>
                </template>
            </AppPageHeader>

            <div v-if="erro" class="erro-banner">{{ erro }}</div>

            <AppTabs v-model="aba" :abas="abas" variante="underline" class="mb-3" aria-label="Seções do orçamento" />

            <div class="grid-form">
                <div class="col-principal">
                    <!-- ──── Aba: Paciente ──── -->
                    <section v-if="aba === 'paciente'">
                        <AppCard title="Cabeçalho do orçamento">
                            <div class="form-grid">
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
                                <AppTextarea id="form-obs" v-model="observacoes" :rows="3" placeholder="Anotações livres sobre este orçamento" />
                            </AppField>
                        </AppCard>
                    </section>

                    <!-- ──── Aba: Cirurgias ──── -->
                    <section v-else-if="aba === 'cirurgias'">
                        <AppCard title="Cirurgias incluídas">
                            <table v-if="cirurgias.length" class="tabela">
                                <thead>
                                    <tr><th>Descrição</th><th>Qtd</th><th>Duração</th><th>Total</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(c, idx) in cirurgias" :key="idx">
                                        <td>{{ c.descricao }}</td>
                                        <td>{{ c.quantidade }}</td>
                                        <td>{{ c.duracaoMinutos ? `${c.duracaoMinutos} min` : "—" }}</td>
                                        <td>{{ fmt(c.valorTotal) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerCirurgia(idx)" title="Remover">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhuma cirurgia adicionada.</p>

                            <div class="form-add">
                                <h4 class="add-title">Adicionar cirurgia</h4>
                                <div class="form-grid">
                                    <AppField label="Do catálogo (auto-preenche)">
                                        <AppSelect :model-value="novaCirurgia.catalogoId" @update:model-value="(v: any) => onSelecionarCatCirurgia(Number(v))">
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
                                <div class="acoes-add">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarCirurgia">Adicionar</AppButton>
                                </div>
                            </div>
                        </AppCard>
                    </section>

                    <!-- ──── Aba: Equipe & Implantes ──── -->
                    <section v-else-if="aba === 'equipeImplantes'">
                        <AppCard title="Equipe profissional">
                            <table v-if="equipe.length" class="tabela">
                                <thead>
                                    <tr><th>Profissional</th><th>Função</th><th>Honorário</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(m, idx) in equipe" :key="idx">
                                        <td>{{ m.profissionalNome ?? m.profissionalUsuarioId.slice(0, 8) }}</td>
                                        <td>{{ m.papel }}</td>
                                        <td>{{ fmt(m.valor) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerMembro(idx)">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhum membro da equipe.</p>

                            <div class="form-add">
                                <h4 class="add-title">Adicionar membro</h4>
                                <div class="form-grid">
                                    <AppField label="Tabela de valor (catálogo)">
                                        <AppSelect :model-value="novoMembro.catalogoValorId" @update:model-value="(v: any) => onSelecionarCatValor(Number(v))">
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
                                <div class="acoes-add">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarMembro">Adicionar</AppButton>
                                </div>
                            </div>

                            <h4 class="add-title mt-3">Equipe especializada (do catálogo)</h4>
                            <div class="form-grid">
                                <AppField label="Catálogo de equipes">
                                    <AppSelect :model-value="novaEquipeEsp.catalogoId" @update:model-value="(v: any) => onSelecionarCatEquipe(Number(v))">
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
                            <div class="acoes-add">
                                <AppButton size="sm" variant="ghost" icon="fa-solid fa-plus" @click="adicionarEquipeEsp">Incluir equipe</AppButton>
                            </div>
                        </AppCard>

                        <AppCard title="Implantes" class="mt-3">
                            <table v-if="implantes.length" class="tabela">
                                <thead>
                                    <tr><th>Descrição</th><th>Qtd</th><th>Custo unit.</th><th>Total</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(imp, idx) in implantes" :key="idx">
                                        <td>{{ imp.descricao }}</td>
                                        <td>{{ imp.quantidade }}</td>
                                        <td>{{ fmt(imp.custoUnitario) }}</td>
                                        <td>{{ fmt(imp.custoTotal) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerImplante(idx)">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhum implante.</p>

                            <div class="form-add">
                                <h4 class="add-title">Adicionar implante</h4>
                                <div class="form-grid">
                                    <AppField label="Catálogo de implantes">
                                        <AppSelect :model-value="novoImplante.catalogoId" @update:model-value="(v: any) => onSelecionarCatImplante(Number(v))">
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
                                <div class="acoes-add">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarImplante">Adicionar</AppButton>
                                </div>
                            </div>
                        </AppCard>
                    </section>

                    <!-- ──── Aba: Local & Pagamento ──── -->
                    <section v-else-if="aba === 'pagamento'">
                        <AppCard title="Internação">
                            <p v-if="!internacao" class="texto-aux">
                                Sem internação configurada.
                                <AppButton size="sm" variant="ghost" @click="inicializarInternacao">Adicionar internação</AppButton>
                            </p>
                            <template v-else>
                                <div class="form-grid">
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
                                <div class="acoes-add">
                                    <AppButton size="sm" variant="ghost" @click="inicializarInternacao">Remover</AppButton>
                                </div>
                            </template>
                        </AppCard>

                        <AppCard title="Anestesia" class="mt-3">
                            <p v-if="!anestesia" class="texto-aux">
                                Sem anestesia configurada.
                                <AppButton size="sm" variant="ghost" @click="inicializarAnestesia">Adicionar anestesia</AppButton>
                            </p>
                            <template v-else>
                                <div class="form-grid">
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
                                <div class="acoes-add">
                                    <AppButton size="sm" variant="ghost" @click="inicializarAnestesia">Remover</AppButton>
                                </div>
                            </template>
                        </AppCard>

                        <AppCard title="Formas de pagamento" class="mt-3">
                            <table v-if="formas.length" class="tabela">
                                <thead>
                                    <tr><th>Forma</th><th>Valor</th><th>Parcelas</th><th>Acréscimo</th><th>Entrada</th><th></th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(f, idx) in formas" :key="idx">
                                        <td>{{ f.formaPagamentoNome }}</td>
                                        <td>{{ fmt(f.valor) }}</td>
                                        <td>{{ f.parcelas }}x</td>
                                        <td>{{ f.acrescimoPercentual }}%</td>
                                        <td>{{ f.entradaPercentual }}%</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" @click="removerForma(idx)">
                                                <i class="fa-solid fa-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="texto-aux">Nenhuma forma de pagamento adicionada.</p>

                            <div class="form-add">
                                <h4 class="add-title">Adicionar forma</h4>
                                <div class="form-grid">
                                    <AppField label="Forma de pagamento">
                                        <AppSelect
                                            :model-value="novaForma.formaPagamentoId"
                                            @update:model-value="(v: any) => { novaForma.formaPagamentoId = Number(v); onSelecionarConfigPagto(Number(v)); }"
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
                                <div class="acoes-add">
                                    <AppButton size="sm" icon="fa-solid fa-plus" @click="adicionarForma">Adicionar</AppButton>
                                </div>
                            </div>
                        </AppCard>
                    </section>
                </div>

                <aside class="col-resumo">
                    <AppCard :title="calculando ? 'Resumo (calculando…)' : 'Resumo'" elevated>
                        <div class="resumo">
                            <div v-if="totalCirurgias" class="lin"><span>Cirurgias</span><strong>{{ fmt(totalCirurgias) }}</strong></div>
                            <div v-if="totalEquipe" class="lin"><span>Honorários</span><strong>{{ fmt(totalEquipe) }}</strong></div>
                            <div v-if="totalImplantes" class="lin"><span>Implantes</span><strong>{{ fmt(totalImplantes) }}</strong></div>
                            <div v-if="totalInternacao" class="lin"><span>Internação</span><strong>{{ fmt(totalInternacao) }}</strong></div>
                            <div v-if="totalAnestesia" class="lin"><span>Anestesia</span><strong>{{ fmt(totalAnestesia) }}</strong></div>
                            <div class="lin total"><span>Total</span><strong>{{ fmt(totalGeral) }}</strong></div>
                            <div class="divisor"></div>
                            <div class="lin"><span>Soma formas</span><strong>{{ fmt(somaFormas) }}</strong></div>
                            <div class="integridade" :class="integridadeOk ? 'ok' : 'erro'">
                                <i :class="integridadeOk ? 'fa-solid fa-circle-check' : 'fa-solid fa-circle-exclamation'"></i>
                                {{ integridadeOk ? "Soma confere." : `Falta ${fmt(diferenca)}.` }}
                            </div>
                        </div>
                        <template #footer>
                            <AppButton block icon="fa-solid fa-save" :loading="salvando" :disabled="!integridadeOk" @click="salvar">
                                Salvar
                            </AppButton>
                        </template>
                    </AppCard>
                </aside>
            </div>
        </template>
    </main>
</template>

<style scoped>
.estado { display: flex; align-items: center; gap: 0.5rem; color: var(--text-muted); padding: 2rem 0; }
.erro-banner {
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    margin-bottom: 1rem;
}
.grid-form {
    display: grid;
    grid-template-columns: 1fr 280px;
    gap: 1.25rem;
    align-items: start;
}
@media (max-width: 1024px) {
    .grid-form { grid-template-columns: 1fr; }
    .col-resumo { position: static; }
}
.col-principal { display: flex; flex-direction: column; gap: 0.85rem; }
.col-resumo { position: sticky; top: 80px; }

.form-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 0.75rem;
}
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
.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88em;
    margin-bottom: 0.5rem;
}
.tabela th, .tabela td {
    padding: 0.5rem 0.7rem;
    text-align: left;
    border-bottom: 1px solid var(--border);
}
.tabela th {
    font-weight: 600;
    font-size: 0.78em;
    text-transform: uppercase;
    color: var(--text-muted);
}
.tabela tr:last-child td { border-bottom: none; }

.texto-aux { color: var(--text-muted); font-size: 0.88em; margin: 0.5rem 0; }

.resumo { display: flex; flex-direction: column; gap: 0.4rem; }
.lin { display: flex; justify-content: space-between; font-size: 0.88em; }
.lin.total {
    border-top: 1px solid var(--border);
    padding-top: 0.4rem;
    margin-top: 0.25rem;
    font-size: 1em;
    font-weight: 700;
}
.divisor { height: 1px; background: var(--border); margin: 0.4rem 0; }
.integridade {
    display: flex; align-items: center; gap: 0.4rem;
    font-size: 0.82em; font-weight: 600; padding: 0.45rem;
    border-radius: var(--radius-sm);
}
.integridade.ok { color: hsl(var(--success)); background: hsl(var(--success) / 0.1); }
.integridade.erro { color: hsl(var(--warning)); background: hsl(var(--warning) / 0.1); }

.mb-3 { margin-bottom: 0.75rem; }
.mt-2 { margin-top: 0.5rem; }
.mt-3 { margin-top: 0.75rem; }
</style>
