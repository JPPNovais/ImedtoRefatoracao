<script setup lang="ts">
/**
 * FinanceiroConfigTab — Configurações financeiras embutidas no painel /financeiro.
 *
 * Layout: grid 2-col.
 *  - Card comissão (full-width / cfg-span): lista de profissionais com percentuais + modal de edição.
 *    Apenas Dono (CA178 / briefing 2026-06-11_002).
 *  - Blocos Tabela de preços e Taxa por forma de pagamento absorvidos de FinanceiroConfigView.vue
 *    (briefing 2026-06-13_003). Gate: configuracoes.gerenciar (R1/R2).
 */
import { ref, computed, onMounted, watch } from "vue"
import { AppButton, AppField, AppInputDecimal, AppToast, AppModal, AppBadge, AppSelect, AppEmptyState, AppSearchInput } from "@/components/ui"
import { financeiroService, type ConfigComissao } from "@/services/financeiroService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"
import { useCobrancaConfigStore } from "@/stores/cobrancaStore"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

const props = defineProps<{ ehDono: boolean }>()

// ── Gate de permissão (R2) ────────────────────────────────────────────────────
const permissoesStore = usePermissoesStore()
const podeGerenciarConfig = computed(() => permissoesStore.pode("configuracoes.gerenciar"))

// ── Store de cobranças (tabela de preços + taxa) ──────────────────────────────
const cobrancaStore = useCobrancaConfigStore()

// ─── Tipo para linha da lista ────────────────────────────────────────────────
interface LinhaComissao {
    profissional: ProfissionalPublico
    config: ConfigComissao
}

// ─── Estado da lista ─────────────────────────────────────────────────────────
const linhas = ref<LinhaComissao[]>([])
const percentualPadrao = ref<number>(30)
const carregando = ref(false)
const erroCarregar = ref<string | null>(null)

// ─── Estado do modal de edição ───────────────────────────────────────────────
const modalAberto = ref(false)
const profEditando = ref<ProfissionalPublico | null>(null)
// AppInputDecimal emite string; convertemos para Number só no payload (null permanece null)
const formEdicao = ref<{ percentualConsulta: string | null; percentualProcedimento: string | null }>({
    percentualConsulta: null,
    percentualProcedimento: null,
})
const salvando = ref(false)
const erroSalvar = ref<string | null>(null)

// ─── Toast ───────────────────────────────────────────────────────────────────
const toast = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)

// ─── Helpers ─────────────────────────────────────────────────────────────────
function ePadrao(linha: LinhaComissao): boolean {
    // R1: badge PADRÃO quando ambos percentuais iguais ao padrão do sistema
    const padrao = linha.config.percentualPadrao
    const consulta = linha.config.percentualConsulta ?? padrao
    const procedimento = linha.config.percentualProcedimento ?? padrao
    return consulta === padrao && procedimento === padrao
}

function formatarPct(valor: number | null, padrao: number): string {
    return String(valor ?? padrao)
}

// ─── Carregamento ────────────────────────────────────────────────────────────
async function carregarTodos() {
    if (!props.ehDono) return
    carregando.value = true
    erroCarregar.value = null
    try {
        const profissionais = await vinculoService.listarProfissionaisPublico()
        // Opção A: N requisições em paralelo (seguro — paralelismo no front, não na mesma conexão Npgsql)
        const configs = await Promise.all(
            profissionais.map(p => financeiroService.obterConfigComissao(p.usuarioId))
        )
        linhas.value = profissionais.map((p, i) => ({ profissional: p, config: configs[i] }))
        if (configs.length > 0) {
            percentualPadrao.value = configs[0].percentualPadrao
        }
    } catch {
        erroCarregar.value = "Erro ao carregar comissões."
    } finally {
        carregando.value = false
    }
}

// ─── Modal de edição ─────────────────────────────────────────────────────────
function abrirModal(linha: LinhaComissao) {
    profEditando.value = linha.profissional
    const padrao = linha.config.percentualPadrao
    const c = linha.config.percentualConsulta ?? padrao
    const p = linha.config.percentualProcedimento ?? padrao
    formEdicao.value = {
        percentualConsulta: c != null ? String(c) : null,
        percentualProcedimento: p != null ? String(p) : null,
    }
    erroSalvar.value = null
    modalAberto.value = true
}

function fecharModal() {
    modalAberto.value = false
    profEditando.value = null
    erroSalvar.value = null
}

async function salvarComissao() {
    if (!profEditando.value) return
    salvando.value = true
    erroSalvar.value = null
    try {
        const toNum = (v: string | null): number | null =>
            v === null || v === "" ? null : Number(v)
        await financeiroService.salvarConfigComissao({
            profissionalUsuarioId: profEditando.value.usuarioId,
            percentualConsulta: toNum(formEdicao.value.percentualConsulta),
            percentualProcedimento: toNum(formEdicao.value.percentualProcedimento),
        })
        // Atualiza linha localmente re-buscando config do profissional editado
        const novaConfig = await financeiroService.obterConfigComissao(profEditando.value.usuarioId)
        const idx = linhas.value.findIndex(l => l.profissional.usuarioId === profEditando.value!.usuarioId)
        if (idx !== -1) {
            linhas.value[idx] = { ...linhas.value[idx], config: novaConfig }
        }
        toast.value = { mensagem: "Comissão salva.", variante: "success" }
        fecharModal()
    } catch (e: any) {
        erroSalvar.value = "Erro ao salvar comissão."
    } finally {
        salvando.value = false
    }
}

onMounted(carregarTodos)

// ── Tabela de preços (absorvido de FinanceiroConfigView) ──────────────────────

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 300)
const profissionaisPreco = ref<ProfissionalPublico[]>([])

async function carregarProfissionaisPreco() {
    try {
        profissionaisPreco.value = await vinculoService.listarProfissionaisPublico()
    } catch { /* silencioso */ }
}

// Observa debounce para recarregar (CA10)
watch(busca, (b) => cobrancaStore.carregarTabelaPreco(b || undefined))

// ── Modal de tabela de preços ─────────────────────────────────────────────────

interface FormPreco {
    id?: number
    profissionalId: string | null
    valorSugerido: string
    modoEdicao: boolean
}

const modalPreco = ref(false)
const formPreco = ref<FormPreco>({ profissionalId: null, valorSugerido: "", modoEdicao: false })
const salvandoPreco = ref(false)
const erroPreco = ref<string | null>(null)

function abrirNovoPreco() {
    formPreco.value = { profissionalId: null, valorSugerido: "", modoEdicao: false }
    erroPreco.value = null
    modalPreco.value = true
}

function abrirEditarPreco(item: typeof cobrancaStore.tabelaPreco[0]) {
    formPreco.value = {
        id: item.id,
        profissionalId: item.profissionalId ?? null,
        valorSugerido: item.valorSugerido.toFixed(2),
        modoEdicao: true,
    }
    erroPreco.value = null
    modalPreco.value = true
}

async function salvarPreco() {
    const valor = parseFloat(formPreco.value.valorSugerido) || 0
    if (valor <= 0) {
        erroPreco.value = "Informe um valor maior que zero."
        return
    }
    salvandoPreco.value = true
    erroPreco.value = null
    try {
        await cobrancaStore.salvarTabelaPreco({
            id: formPreco.value.id,
            profissionalId: formPreco.value.profissionalId || null,
            valorSugerido: valor,
        })
        modalPreco.value = false
    } catch (e: any) {
        erroPreco.value = e?.response?.data?.mensagem ?? "Erro ao salvar preço."
    } finally {
        salvandoPreco.value = false
    }
}

async function inativarPreco(id: number) {
    try {
        await cobrancaStore.inativarTabelaPreco(id)
    } catch { /* silencioso — lista recarrega mesmo assim */ }
}

// ── Taxa por forma de pagamento ───────────────────────────────────────────────

// Mantém strings para edição inline (AppInputDecimal emite string)
const taxaStrings = ref<Record<number, string>>({})

watch(() => cobrancaStore.configTaxa, (lista) => {
    for (const item of lista) {
        if (!(item.formaPagamentoId in taxaStrings.value)) {
            taxaStrings.value[item.formaPagamentoId] = item.taxaPercentual.toFixed(3)
        }
    }
}, { immediate: true })

const salvandoTaxa = ref<Record<number, boolean>>({})

async function salvarTaxaInline(item: typeof cobrancaStore.configTaxa[0]) {
    if (item.formaPagamentoId == null) return
    const taxa = parseFloat(taxaStrings.value[item.formaPagamentoId] ?? "0") || 0
    salvandoTaxa.value[item.formaPagamentoId] = true
    try {
        await cobrancaStore.salvarConfigTaxa({
            id: item.id ?? null,
            formaPagamentoId: item.formaPagamentoId,
            taxaPercentual: taxa,
            ativo: item.ativo,
        })
    } catch { /* silencioso */ } finally {
        delete salvandoTaxa.value[item.formaPagamentoId]
    }
}

// Opções de profissional para o AppSelect no modal
const opcoesProfissionais = computed(() => [
    { value: "" as string, label: "Padrão do estabelecimento" },
    ...profissionaisPreco.value.map(p => ({ value: p.usuarioId, label: p.nomeCompleto })),
])

function fmt(v: number): string {
    return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

// Carrega dados de cobranças quando o usuário tem permissão (lazy — CA11)
watch(podeGerenciarConfig, async (pode) => {
    if (pode) {
        await Promise.all([
            cobrancaStore.carregarTabelaPreco(),
            cobrancaStore.carregarConfigTaxa(),
            carregarProfissionaisPreco(),
        ])
    }
}, { immediate: true })
</script>

<template>
    <div class="config-tab">

        <!-- Card comissão (apenas Dono) — lista + modal (briefing 2026-06-11_004) -->
        <div v-if="ehDono" class="cfg-card cfg-span">
            <div class="cfg-card-h">
                <div class="cfg-card-title-block">
                    <span class="cfg-ic comissoes"><i class="fa-solid fa-percent" aria-hidden="true" /></span>
                    <div>
                        <b>Comissões por profissional</b>
                        <p>Defina o percentual de consulta e procedimento por profissional.</p>
                    </div>
                </div>
                <span class="cfg-padrao-tag">Padrão do sistema: {{ percentualPadrao }}%</span>
            </div>

            <div class="cfg-comm-lista">
                <!-- Estado: carregando -->
                <p v-if="carregando" class="comm-state-msg">
                    <i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true" />
                    Carregando comissões...
                </p>

                <!-- Estado: erro ao carregar -->
                <p v-else-if="erroCarregar" class="comm-state-msg comm-state-erro">
                    {{ erroCarregar }}
                </p>

                <!-- Estado: vazio -->
                <p v-else-if="linhas.length === 0" class="comm-state-msg">
                    Nenhum profissional cadastrado neste estabelecimento.
                </p>

                <!-- Lista de profissionais -->
                <template v-else>
                    <div
                        v-for="linha in linhas"
                        :key="linha.profissional.usuarioId"
                        class="comm-row"
                    >
                        <span class="comm-name">
                            <i class="fa-solid fa-user-doctor" aria-hidden="true" />
                            {{ linha.profissional.nomeCompleto }}
                        </span>
                        <span class="comm-vals">
                            Consulta {{ formatarPct(linha.config.percentualConsulta, linha.config.percentualPadrao) }}%
                            · Procedimento {{ formatarPct(linha.config.percentualProcedimento, linha.config.percentualPadrao) }}%
                        </span>
                        <AppBadge v-if="ePadrao(linha)" variant="muted" label="PADRÃO" />
                        <span v-else class="comm-badge-placeholder" />
                        <button
                            class="btn-icon btn-icon-editar"
                            title="Editar comissão"
                            @click="abrirModal(linha)"
                        >
                            <i class="fa-solid fa-pen" aria-hidden="true" />
                        </button>
                    </div>
                </template>
            </div>
        </div>

        <!-- Grid 2-col para taxa + tabela de preços (gate: configuracoes.gerenciar) -->

        <!-- Estado sem permissão (CA4) -->
        <div v-if="!podeGerenciarConfig" class="cfg-sem-permissao">
            <i class="fa-solid fa-lock" aria-hidden="true" />
            <p>Você não tem permissão para gerenciar tabela de preços e taxas.</p>
        </div>

        <!-- Blocos reais: apenas com configuracoes.gerenciar (CA3) -->
        <div v-else class="cfg-grid">

            <!-- Card: Taxa por forma de pagamento -->
            <div class="cfg-card">
                <div class="cfg-card-h">
                    <div class="cfg-card-title-block">
                        <span class="cfg-ic taxa"><i class="fa-solid fa-credit-card" aria-hidden="true" /></span>
                        <div>
                            <b>Taxa por forma de pagamento</b>
                            <p>A taxa é informativa ("você recebe R$ X") e não altera o valor cobrado do paciente.</p>
                        </div>
                    </div>
                </div>

                <div class="cfg-card-body">
                    <div v-if="cobrancaStore.configTaxa.length === 0 && !cobrancaStore.carregando" class="cfg-msg-vazio">
                        <i class="fa-solid fa-circle-info" aria-hidden="true" />
                        Nenhuma forma de pagamento ativa. Cadastre formas em Configurações &rsaquo; Financeiro.
                    </div>

                    <div v-if="cobrancaStore.carregando && cobrancaStore.configTaxa.length === 0" class="cfg-carregando">
                        <i class="fa-solid fa-spinner fa-spin" aria-hidden="true" /> Carregando…
                    </div>

                    <table v-else-if="cobrancaStore.configTaxa.length > 0" class="cfg-tabela">
                        <thead>
                            <tr>
                                <th>Forma de pagamento</th>
                                <th class="col-taxa">Taxa (%)</th>
                                <th class="col-ativo">Ativo</th>
                                <th class="col-acoes"></th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in cobrancaStore.configTaxa" :key="item.formaPagamentoId">
                                <td>{{ item.formaPagamentoNome }}</td>
                                <td class="col-taxa">
                                    <AppInputDecimal
                                        v-model="taxaStrings[item.formaPagamentoId]"
                                        :decimals="3"
                                        placeholder="0,000"
                                        class="taxa-input"
                                    />
                                </td>
                                <td class="col-ativo">
                                    <input v-model="item.ativo" type="checkbox" />
                                </td>
                                <td class="col-acoes">
                                    <AppButton
                                        variant="secondary"
                                        size="sm"
                                        :loading="!!salvandoTaxa[item.formaPagamentoId]"
                                        @click="salvarTaxaInline(item)"
                                    >
                                        Salvar
                                    </AppButton>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Card: Tabela de preços de consulta -->
            <div class="cfg-card">
                <div class="cfg-card-h">
                    <div class="cfg-card-title-block">
                        <span class="cfg-ic precos"><i class="fa-solid fa-tag" aria-hidden="true" /></span>
                        <div>
                            <b>Tabela de preços de consulta</b>
                            <p>Valor sugerido ao abrir o check-in. Um preço por profissional + um padrão do estabelecimento.</p>
                        </div>
                    </div>
                    <AppButton icon="fa-solid fa-plus" size="sm" @click="abrirNovoPreco">Novo preço</AppButton>
                </div>

                <div class="cfg-card-body">
                    <div class="cfg-busca-bar">
                        <AppSearchInput v-model="buscaInput" placeholder="Buscar profissional…" />
                    </div>

                    <div v-if="cobrancaStore.carregando" class="cfg-carregando">
                        <i class="fa-solid fa-spinner fa-spin" aria-hidden="true" /> Carregando…
                    </div>

                    <AppEmptyState
                        v-else-if="cobrancaStore.tabelaPreco.length === 0"
                        icone="fa-solid fa-coins"
                        titulo="Nenhum preço cadastrado"
                        descricao="Configure o valor sugerido para consultas particulares."
                    >
                        <AppButton icon="fa-solid fa-plus" @click="abrirNovoPreco">Cadastrar primeiro preço</AppButton>
                    </AppEmptyState>

                    <table v-else class="cfg-tabela">
                        <thead>
                            <tr>
                                <th>Profissional / escopo</th>
                                <th class="col-valor">Valor sugerido</th>
                                <th class="col-ativo">Ativo</th>
                                <th class="col-acoes"></th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in cobrancaStore.tabelaPreco" :key="item.id" :class="{ 'linha-inativa': !item.ativo }">
                                <td>
                                    <span v-if="item.profissionalNome" class="prof-nome">{{ item.profissionalNome }}</span>
                                    <span v-else class="padrao-tag">Padrão do estabelecimento</span>
                                </td>
                                <td class="col-valor">{{ fmt(item.valorSugerido) }}</td>
                                <td class="col-ativo">
                                    <span :class="['status-dot', item.ativo ? 'status-dot--on' : 'status-dot--off']"></span>
                                    {{ item.ativo ? "Sim" : "Não" }}
                                </td>
                                <td class="col-acoes">
                                    <button class="btn-icon btn-icon-editar" title="Editar" @click="abrirEditarPreco(item)">
                                        <i class="fa-solid fa-pen-to-square"></i>
                                    </button>
                                    <button
                                        v-if="item.ativo"
                                        class="btn-icon btn-icon-excluir"
                                        title="Inativar"
                                        @click="inativarPreco(item.id)"
                                    >
                                        <i class="fa-solid fa-ban"></i>
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

        </div>
    </div>

    <!-- Modal: Novo/editar preço -->
    <AppModal
        :aberto="modalPreco"
        :titulo="formPreco.modoEdicao ? 'Editar preço' : 'Novo preço'"
        largura="sm"
        @fechar="modalPreco = false"
    >
        <AppField label="Profissional">
            <AppSelect
                v-model="formPreco.profissionalId"
                :options="opcoesProfissionais"
            />
        </AppField>

        <AppField label="Valor sugerido (R$)">
            <AppInputDecimal v-model="formPreco.valorSugerido" placeholder="0,00" />
        </AppField>

        <p v-if="erroPreco" class="cfg-msg-erro">{{ erroPreco }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="modalPreco = false">Cancelar</AppButton>
            <AppButton :loading="salvandoPreco" @click="salvarPreco">Salvar</AppButton>
        </template>
    </AppModal>

    <!-- Modal de edição de comissão -->
    <AppModal
        :aberto="modalAberto"
        largura="sm"
        :titulo="profEditando ? `Editar comissão — ${profEditando.nomeCompleto}` : 'Editar comissão'"
        @fechar="fecharModal"
    >
        <AppField label="Percentual — Consultas (%)">
            <AppInputDecimal
                v-model="formEdicao.percentualConsulta"
                :decimals="0"
                :placeholder="`Padrão: ${percentualPadrao}%`"
            />
        </AppField>
        <AppField label="Percentual — Procedimentos (%)">
            <AppInputDecimal
                v-model="formEdicao.percentualProcedimento"
                :decimals="0"
                :placeholder="`Padrão: ${percentualPadrao}%`"
            />
        </AppField>
        <p v-if="erroSalvar" class="modal-erro">{{ erroSalvar }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="fecharModal">Cancelar</AppButton>
            <AppButton :loading="salvando" @click="salvarComissao">Salvar</AppButton>
        </template>
    </AppModal>

    <AppToast v-if="toast" :mensagem="toast.mensagem" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
.config-tab { display: flex; flex-direction: column; gap: 16px; }

/* Grid 2-col */
.cfg-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
}
@media (max-width: 800px) {
    .cfg-grid { grid-template-columns: 1fr; }
}

/* Card base */
.cfg-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
}
/* Full-width: span das 2 colunas */
.cfg-span { grid-column: 1 / -1; }

/* Cabeçalho do card */
.cfg-card-h {
    padding: 16px 18px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.07);
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}
.cfg-card-title-block {
    display: flex;
    align-items: flex-start;
    gap: 13px;
}
.cfg-ic {
    flex-shrink: 0;
    width: 36px;
    height: 36px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-base);
}
.cfg-ic.comissoes { background: hsl(var(--primary) / 0.1); color: hsl(var(--primary)); }
.cfg-ic.taxa       { background: hsl(var(--warning) / 0.12); color: hsl(28 90% 45%); }
.cfg-ic.precos     { background: hsl(142 71% 45% / 0.1); color: hsl(142 71% 35%); }

.cfg-card-title-block > div > b {
    display: block;
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: var(--c-primary-dark);
    margin-bottom: 2px;
}
.cfg-card-title-block > div > p {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
    line-height: 1.5;
}

/* Tag "Padrão do sistema: X%" no cabeçalho */
.cfg-padrao-tag {
    flex-shrink: 0;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.6);
    background: hsl(var(--secondary) / 0.06);
    padding: 4px 11px;
    border-radius: 999px;
    white-space: nowrap;
}

/* Lista de comissão */
.cfg-comm-lista { padding: 8px; }

.comm-state-msg {
    padding: 16px 12px;
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
    display: flex;
    align-items: center;
    gap: 8px;
}
.comm-state-erro { color: hsl(var(--destructive)); }

/* Linha de profissional */
.comm-row {
    display: grid;
    grid-template-columns: 1fr auto auto auto;
    gap: 14px;
    align-items: center;
    padding: 11px 12px;
    border-radius: 8px;
    transition: background 0.1s;
}
.comm-row:hover { background: hsl(var(--secondary) / 0.03); }

.comm-name {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--c-primary-dark);
    display: inline-flex;
    align-items: center;
    gap: 8px;
}
.comm-name i { color: hsl(var(--secondary) / 0.4); }

.comm-vals {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.75);
    white-space: nowrap;
}

/* Placeholder para manter alinhamento quando não há badge */
.comm-badge-placeholder { display: inline-block; width: 56px; }

/* Responsivo: em telas estreitas, percentuais vão abaixo do nome */
@media (max-width: 600px) {
    .comm-row {
        grid-template-columns: 1fr auto auto;
        grid-template-rows: auto auto;
    }
    .comm-name { grid-column: 1; grid-row: 1; }
    .comm-vals { grid-column: 1; grid-row: 2; font-size: var(--text-xs); }
    .comm-badge-placeholder,
    :deep(.count-badge) { grid-column: 2; grid-row: 1; }
    .btn-icon { grid-column: 3; grid-row: 1; }
}

/* Erro dentro do modal */
.modal-erro {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    margin: 0;
}

/* Estado sem permissão (CA4) */
.cfg-sem-permissao {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding: 48px 16px;
    color: hsl(var(--secondary) / 0.6);
    font-size: var(--text-sm);
    text-align: center;
}
.cfg-sem-permissao i { font-size: var(--text-xl); opacity: 0.4; }
.cfg-sem-permissao p { margin: 0; }

/* Corpo do card com padding */
.cfg-card-body {
    padding: 16px 18px;
}

/* Barra de busca no card de preços */
.cfg-busca-bar {
    margin-bottom: 12px;
}

/* Mensagens de estado */
.cfg-carregando {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.6);
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 0;
}
.cfg-msg-vazio {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.6);
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 0;
}

/* Tabela interna dos cards */
.cfg-tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}
.cfg-tabela th {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    text-align: left;
    padding: 6px 8px;
    border-bottom: 1px solid hsl(var(--border));
}
.cfg-tabela td {
    padding: 10px 8px;
    color: hsl(var(--foreground));
    border-bottom: 1px solid hsl(var(--border));
    vertical-align: middle;
}
.cfg-tabela tr:last-child td { border-bottom: none; }

.col-valor { width: 140px; text-align: right; }
.col-taxa  { width: 120px; }
.col-ativo { width: 80px; }
.col-acoes { width: 80px; text-align: right; }

.linha-inativa td { opacity: 0.5; }

.prof-nome  { font-weight: var(--font-weight-semibold); }
.padrao-tag {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.08);
    padding: 2px 8px;
    border-radius: 999px;
}

.status-dot {
    display: inline-block;
    width: 6px;
    height: 6px;
    border-radius: 50%;
    margin-right: 4px;
    vertical-align: middle;
}
.status-dot--on  { background: hsl(160 79% 39%); }
.status-dot--off { background: hsl(var(--muted-foreground)); }

.taxa-input { max-width: 90px; }

.cfg-msg-erro {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    margin: 0;
}
</style>
