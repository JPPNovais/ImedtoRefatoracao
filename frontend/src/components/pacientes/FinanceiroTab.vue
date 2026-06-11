<script setup lang="ts">
/**
 * Aba Financeiro do detalhe do paciente (F2).
 *
 * Exibe:
 *   - KPIs: total cobrado / total pago líquido / saldo em aberto
 *   - Lista expansível de cobranças com pagamentos e estornos inline
 *   - Botão "Registrar pagamento" reutiliza PaymentModal (F1)
 *   - Botão "Estornar" abre EstornoModal (F2)
 *   - Gate de acesso restrito quando sem permissão `financeiro_paciente.ver`
 *
 * Lazy: a consulta HTTP é acionada pelo pai via prop `ativa`.
 * A aba apenas renderiza o que recebeu — gerenciamento de estado no pai.
 */
import { ref, watch } from "vue"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import AppButton from "@/components/ui/AppButton.vue"
import AppModal from "@/components/ui/AppModal.vue"
import AppField from "@/components/ui/AppField.vue"
import PaymentModal from "@/components/ui/PaymentModal.vue"
import EstornoModal from "@/components/ui/EstornoModal.vue"
import { cobrancaService, type FinanceiroAba, type CobrancaAba, type PagamentoAba, type RegistrarPagamentosRequest } from "@/services/cobrancaService"
import type { CobrancaDetalhe } from "@/services/cobrancaService"
import { formaPagamentoService } from "@/services/categoriaFinanceiraService"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { convenioService } from "@/services/convenioService"

// ── Props ──────────────────────────────────────────────────────────────────

const props = defineProps<{
    pacienteId: number
    ativa: boolean
}>()

const emit = defineEmits<{
    notificar: [mensagem: string, variante?: "info" | "success" | "error"]
}>()

// ── Permissões ─────────────────────────────────────────────────────────────

const permissoes = usePermissoesStore()
const temAcesso = () => permissoes.pode("financeiro_paciente.ver") || permissoes.ehDono
const podeRegistrar = () => permissoes.pode("financeiro_paciente.registrar") || permissoes.ehDono

// ── Estado ─────────────────────────────────────────────────────────────────

const dados = ref<FinanceiroAba | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)

const carregada = ref(false)

// ── Lazy-load: dispara só quando a aba é ativada pela primeira vez ──────────

watch(() => props.ativa, (ativa) => {
    if (ativa && !carregada.value && temAcesso()) {
        void carregar()
    }
}, { immediate: true })

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await cobrancaService.obterFinanceiroAba(props.pacienteId)
        carregada.value = true
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar financeiro do paciente."
    } finally {
        carregando.value = false
    }
}

// ── Expansion (accordion) ──────────────────────────────────────────────────

const expandidoId = ref<number | null>(null)

function toggleExpandido(id: number) {
    expandidoId.value = expandidoId.value === id ? null : id
}

// ── PaymentModal (reutiliza F1) ─────────────────────────────────────────────

const cobrancaParaPagar = ref<CobrancaAba | null>(null)
const formasPagamento = ref<{ value: number; label: string; taxaPercentual?: number }[]>([])
const formasCarregadas = ref(false)

async function garantirFormasPagamento(): Promise<void> {
    if (formasCarregadas.value) return
    try {
        const [formas, configsTaxa] = await Promise.all([
            formaPagamentoService.listar(),
            cobrancaService.listarConfigTaxa(),
        ])
        const taxaMap = new Map<number, number>()
        for (const c of configsTaxa) {
            if (c.ativo && c.taxaPercentual > 0) taxaMap.set(c.formaPagamentoId, c.taxaPercentual)
        }
        formasPagamento.value = formas.map(f => ({
            value: f.id,
            label: f.nome,
            taxaPercentual: taxaMap.get(f.id),
        }))
        formasCarregadas.value = true
    } catch {
        // silencioso — PaymentModal funciona sem formas
    }
}

async function abrirPagamento(cobranca: CobrancaAba) {
    await garantirFormasPagamento()
    cobrancaParaPagar.value = cobranca
}

// Adapta CobrancaAba → CobrancaDetalhe para o PaymentModal (F1 API)
function cobrancaDetalheParaModal(c: CobrancaAba): CobrancaDetalhe {
    return {
        id: c.id,
        pacienteId: props.pacienteId,
        agendamentoId: null,
        tipoAtendimento: c.tipoAtendimento,
        valorCobrado: c.valorCobrado,
        desconto: c.desconto,
        status: c.status,
        descricao: c.descricao,
        pagamentos: c.pagamentos.map(p => ({
            id: p.id,
            formaPagamentoId: 0,
            formaPagamentoNome: p.formaPagamentoNome,
            valor: p.valor,
            parcelas: p.parcelas,
            juros: 0,
            taxa: p.taxa,
            dataPagamento: p.dataPagamento,
            registradoPorNome: "",
        })),
        totalLiquido: c.totalLiquido,
        totalPago: c.totalPagoLiquido,
        saldoDevedor: c.saldo,
    }
}

const registrandoPagamento = ref(false)

async function onPago(payload: RegistrarPagamentosRequest) {
    if (!cobrancaParaPagar.value) return
    registrandoPagamento.value = true
    try {
        await cobrancaService.registrarPagamentos(cobrancaParaPagar.value.id, payload)
        cobrancaParaPagar.value = null
        emit("notificar", "Pagamento registrado com sucesso.")
        await carregar() // Reload para refletir novo status
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao registrar pagamento.", "error")
    } finally {
        registrandoPagamento.value = false
    }
}

// ── Emitir recibo (F8/CA118/CA122) ─────────────────────────────────────────

const loadingRecibo = ref<Record<number, boolean>>({})

async function emitirRecibo(pagamentoId: number) {
    loadingRecibo.value = { ...loadingRecibo.value, [pagamentoId]: true }
    try {
        const blob = await cobrancaService.emitirRecibo(pagamentoId)
        const url = URL.createObjectURL(blob)
        const a = document.createElement("a")
        a.href = url
        a.download = `recibo-${pagamentoId}.pdf`
        document.body.appendChild(a)
        a.click()
        document.body.removeChild(a)
        URL.revokeObjectURL(url)
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao emitir o recibo.", "error")
    } finally {
        loadingRecibo.value = { ...loadingRecibo.value, [pagamentoId]: false }
    }
}

// ── EstornoModal ────────────────────────────────────────────────────────────

interface EstornoAlvo {
    cobranca: CobrancaAba
    pagamento: PagamentoAba
}

const estornoAlvo = ref<EstornoAlvo | null>(null)
const enviandoEstorno = ref(false)

function abrirEstorno(cobranca: CobrancaAba, pagamento: PagamentoAba) {
    estornoAlvo.value = { cobranca, pagamento }
}

async function onConfirmarEstorno(motivo: string) {
    if (!estornoAlvo.value) return
    enviandoEstorno.value = true
    try {
        await cobrancaService.estornarPagamento(
            estornoAlvo.value.cobranca.id,
            estornoAlvo.value.pagamento.id,
            motivo,
        )
        estornoAlvo.value = null
        emit("notificar", "Estorno registrado com sucesso.")
        await carregar()
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao registrar estorno.", "error")
    } finally {
        enviandoEstorno.value = false
    }
}

// ── Modal de guia/autorização (F6/R10/R13) ─────────────────────────────────

const cobrancaGuia = ref<CobrancaAba | null>(null)
const modalGuiaAberto = ref(false)
const formGuia = ref({ guiaNumero: "", guiaSenha: "", guiaAutorizadaEm: "" })
const salvandoGuia = ref(false)
const erroGuia = ref<string | null>(null)

function abrirModalGuia(c: CobrancaAba) {
    cobrancaGuia.value = c
    formGuia.value = {
        guiaNumero: c.guiaNumero ?? "",
        guiaSenha: c.guiaSenha ?? "",
        guiaAutorizadaEm: c.guiaAutorizadaEm ?? "",
    }
    erroGuia.value = null
    modalGuiaAberto.value = true
}

async function salvarGuia() {
    if (!cobrancaGuia.value) return
    if (!formGuia.value.guiaNumero.trim()) {
        erroGuia.value = "Número da guia é obrigatório."
        return
    }
    salvandoGuia.value = true
    erroGuia.value = null
    try {
        await convenioService.registrarGuia(cobrancaGuia.value.id, {
            guiaNumero: formGuia.value.guiaNumero.trim(),
            guiaSenha: formGuia.value.guiaSenha.trim() || null,
            guiaAutorizadaEm: formGuia.value.guiaAutorizadaEm || null,
        })
        modalGuiaAberto.value = false
        emit("notificar", "Guia registrada com sucesso.")
        await carregar()
    } catch (e: any) {
        erroGuia.value = e?.response?.data?.detail ?? "Erro ao registrar guia."
    } finally {
        salvandoGuia.value = false
    }
}

// ── Helpers visuais ─────────────────────────────────────────────────────────

function fmtMoeda(n: number): string {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function fmtData(iso: string | null | undefined): string {
    if (!iso) return "—"
    try {
        return new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "short", year: "numeric" })
    } catch { return iso }
}

type StatusCobranca = "Aberta" | "ParcialmentePaga" | "Paga" | "Cancelada"

const STATUS_META: Record<StatusCobranca, { label: string; cls: string }> = {
    Aberta:            { label: "Em aberto",         cls: "pill--warning" },
    ParcialmentePaga:  { label: "Pago parcialmente",  cls: "pill--partial" },
    Paga:              { label: "Quitada",            cls: "pill--ok" },
    Cancelada:         { label: "Cancelada",          cls: "pill--muted" },
}

const ORIGEM_META: Record<string, { label: string; icone: string; cor: string }> = {
    Consulta:  { label: "Consulta",  icone: "fa-stethoscope",       cor: "254 56% 38%" },
    Cirurgia:  { label: "Cirurgia",  icone: "fa-scalpel",           cor: "280 55% 50%" },
    Exame:     { label: "Exame",     icone: "fa-vial",              cor: "190 60% 45%" },
    Orcamento: { label: "Orçamento", icone: "fa-file-invoice-dollar", cor: "40 90% 38%" },
    Manual:    { label: "Manual",    icone: "fa-pen",               cor: "var(--secondary)" },
}

function origemMeta(origem: string) {
    return ORIGEM_META[origem] ?? { label: origem, icone: "fa-circle-dollar-to-slot", cor: "var(--secondary)" }
}
</script>

<template>
    <!-- Gate de acesso restrito (CA33/CA34) -->
    <div v-if="!temAcesso()" class="fin-restricted">
        <div class="fr-icon">
            <i class="fa-solid fa-lock"></i>
        </div>
        <b>Acesso restrito</b>
        <p>
            O módulo financeiro do paciente contém dados sensíveis e exige permissão específica.<br />
            Solicite acesso ao administrador da clínica.
        </p>
        <span class="fr-audit">
            <i class="fa-solid fa-shield-halved"></i>
            Acessos a esta aba são auditados (LGPD)
        </span>
    </div>

    <template v-else>
        <div class="prontuario-head">
            <div>
                <h2>Financeiro</h2>
                <p>
                    Cobranças, pagamentos e recibos do paciente ·
                    <i class="fa-solid fa-shield-halved" style="font-size: 11px;"></i>
                    acesso auditado
                </p>
            </div>
        </div>

        <!-- Loading -->
        <p v-if="carregando" class="msg-info">Carregando…</p>
        <p v-else-if="erro" class="msg-erro">{{ erro }}</p>

        <template v-else-if="dados">
            <!-- KPIs (CA25) -->
            <div class="fin-kpis">
                <div class="fin-kpi">
                    <span>Total cobrado</span>
                    <b>{{ fmtMoeda(dados.totalCobrado) }}</b>
                </div>
                <div class="fin-kpi fin-kpi--ok">
                    <span>Total pago</span>
                    <b>{{ fmtMoeda(dados.totalPagoLiquido) }}</b>
                </div>
                <div class="fin-kpi" :class="dados.saldo > 0 ? 'fin-kpi--danger' : 'fin-kpi--zero'">
                    <span>Saldo em aberto</span>
                    <b>{{ fmtMoeda(dados.saldo) }}</b>
                </div>
            </div>

            <!-- Empty state -->
            <AppEmptyState
                v-if="dados.cobrancas.length === 0"
                icone="💳"
                titulo="Nenhuma movimentação financeira"
                descricao="Cobranças aparecem aqui quando o paciente faz check-in particular ou aprova um orçamento."
            />

            <!-- Lista de cobranças (CA26/CA27) -->
            <div v-else class="charge-list">
                <div
                    v-for="cobranca in dados.cobrancas"
                    :key="cobranca.id"
                    class="charge-card"
                    :class="{ open: expandidoId === cobranca.id }"
                >
                    <!-- Cabeçalho do card (sempre visível) -->
                    <div class="cc-main" @click="toggleExpandido(cobranca.id)">
                        <!-- Tag de origem -->
                        <div
                            class="cc-origin"
                            :style="{ '--ot': `hsl(${origemMeta(cobranca.origem).cor})` }"
                        >
                            <i class="fa-solid" :class="origemMeta(cobranca.origem).icone"></i>
                            <span>{{ origemMeta(cobranca.origem).label }}</span>
                        </div>

                        <!-- Descrição -->
                        <div class="cc-desc">
                            <b>{{ cobranca.descricao || origemMeta(cobranca.origem).label }}</b>
                        </div>

                        <!-- Status pill -->
                        <div class="cc-status">
                            <span
                                class="cc-pill"
                                :class="STATUS_META[cobranca.status]?.cls ?? 'pill--muted'"
                            >
                                {{ STATUS_META[cobranca.status]?.label ?? cobranca.status }}
                            </span>
                        </div>

                        <!-- Valor + ações -->
                        <div class="cc-amount">
                            <b>{{ fmtMoeda(cobranca.totalLiquido) }}</b>
                            <span v-if="cobranca.saldo > 0 && cobranca.totalPagoLiquido > 0" class="cc-saldo">
                                saldo {{ fmtMoeda(cobranca.saldo) }}
                            </span>
                            <span v-else-if="cobranca.status === 'Paga'" class="cc-ok">
                                <i class="fa-solid fa-check"></i> quitada
                            </span>
                        </div>

                        <div class="cc-actions" @click.stop>
                            <!-- Botão registrar pagamento — só para não-convênio com saldo em aberto -->
                            <AppButton
                                v-if="cobranca.tipoAtendimento !== 'Convenio' && cobranca.saldo > 0 && cobranca.status !== 'Cancelada' && podeRegistrar()"
                                variant="primary"
                                icon="fa-solid fa-circle-dollar-to-slot"
                                size="sm"
                                @click="abrirPagamento(cobranca)"
                            >
                                Registrar pagamento
                            </AppButton>
                            <button class="cc-chev">
                                <i class="fa-solid" :class="expandidoId === cobranca.id ? 'fa-chevron-up' : 'fa-chevron-down'"></i>
                            </button>
                        </div>
                    </div>

                    <!-- Detalhe expandido (CA27) -->
                    <div v-if="expandidoId === cobranca.id" class="cc-detail">

                        <!-- Histórico de valor (F5 — exibido se existir, CA28) -->
                        <div v-if="cobranca.historicoValor.length > 0" class="cc-block">
                            <div class="ccb-title">
                                <i class="fa-solid fa-clock-rotate-left"></i>
                                Histórico de valor
                            </div>
                            <div v-for="(h, i) in cobranca.historicoValor" :key="i" class="value-change">
                                <span class="vc-from">{{ fmtMoeda(h.valorAnterior) }}</span>
                                <i class="fa-solid fa-arrow-right"></i>
                                <span class="vc-to">{{ fmtMoeda(h.valorNovo) }}</span>
                                <span class="vc-meta">{{ h.alteradoPorNome }} · {{ fmtData(h.alteradoEm) }}</span>
                            </div>
                        </div>

                        <!-- Convênio: guia de autorização (F6/R10) -->
                        <div v-if="cobranca.tipoAtendimento === 'Convenio'" class="cc-block">
                            <div class="cc-convenio-note">
                                <i class="fa-solid fa-shield-halved"></i>
                                <span>
                                    Faturada ao convênio{{ cobranca.convenioNome ? ` — ${cobranca.convenioNome}` : '' }}.
                                </span>
                            </div>
                            <!-- Estado da guia -->
                            <div v-if="cobranca.guiaNumero" class="cc-guia-preenchida">
                                <i class="fa-solid fa-check-circle"></i>
                                Guia: <strong>{{ cobranca.guiaNumero }}</strong>
                                <template v-if="cobranca.guiaAutorizadaEm">
                                    · Autorizada em {{ cobranca.guiaAutorizadaEm }}
                                </template>
                            </div>
                            <div v-else class="cc-guia-pendente">
                                <i class="fa-solid fa-clock"></i>
                                Guia pendente — preencha após a autorização do convênio.
                            </div>
                            <AppButton
                                v-if="podeRegistrar()"
                                variante="secundario"
                                tamanho="sm"
                                icone="fa-solid fa-file-medical"
                                class="btn-guia"
                                @click="abrirModalGuia(cobranca)"
                            >
                                {{ cobranca.guiaNumero ? 'Atualizar guia' : 'Registrar guia' }}
                            </AppButton>
                        </div>

                        <!-- Pagamentos e estornos (CA27) -->
                        <div v-else class="cc-block">
                            <div class="ccb-title">
                                <i class="fa-solid fa-receipt"></i>
                                Pagamentos e estornos
                            </div>

                            <div v-if="cobranca.pagamentos.length === 0" class="cc-empty-pay">
                                Nenhum pagamento registrado ainda.
                            </div>

                            <div class="pay-ledger">
                                <template v-for="pagamento in cobranca.pagamentos" :key="pagamento.id">
                                    <!-- Linha do pagamento -->
                                    <div class="ledger-row pay" :class="{ voided: pagamento.estornado }">
                                        <div class="lr-icon">
                                            <i class="fa-solid fa-circle-dollar-to-slot"></i>
                                        </div>
                                        <div class="lr-info">
                                            <b>
                                                {{ pagamento.formaPagamentoNome }}{{ pagamento.parcelas > 1 ? ` · ${pagamento.parcelas}x` : "" }}
                                            </b>
                                            <span>{{ fmtData(pagamento.dataPagamento) }}</span>
                                        </div>
                                        <div class="lr-amount">{{ fmtMoeda(pagamento.valor) }}</div>
                                        <div class="lr-acts">
                                            <!-- Ações para pagamento não estornado -->
                                            <template v-if="!pagamento.estornado">
                                                <!-- F8/CA118: emitir recibo (CA120: oculto se estornado) -->
                                                <button
                                                    class="btn-icon btn-icon-ver"
                                                    title="Emitir recibo"
                                                    :disabled="!!loadingRecibo[pagamento.id]"
                                                    @click="emitirRecibo(pagamento.id)"
                                                >
                                                    <i :class="loadingRecibo[pagamento.id] ? 'fa-solid fa-spinner fa-spin' : 'fa-solid fa-file-pdf'"></i>
                                                </button>
                                                <!-- Pode estornar apenas se tem permissão -->
                                                <button
                                                    v-if="podeRegistrar()"
                                                    class="btn-icon btn-icon-excluir"
                                                    title="Estornar pagamento"
                                                    @click="abrirEstorno(cobranca, pagamento)"
                                                >
                                                    <i class="fa-solid fa-rotate-left"></i>
                                                </button>
                                            </template>
                                            <span v-else-if="pagamento.estornado" class="lr-voided-tag">estornado</span>
                                        </div>
                                    </div>

                                    <!-- Linha do estorno (quando existir) -->
                                    <div v-if="pagamento.estorno" class="ledger-row refund">
                                        <div class="lr-icon">
                                            <i class="fa-solid fa-rotate-left"></i>
                                        </div>
                                        <div class="lr-info">
                                            <b>Estorno</b>
                                            <span>
                                                {{ fmtData(pagamento.estorno.dataEstorno) }} ·
                                                {{ pagamento.estorno.estornadoPorNome }} ·
                                                "{{ pagamento.estorno.motivo }}"
                                            </span>
                                        </div>
                                        <div class="lr-amount">– {{ fmtMoeda(pagamento.estorno.valor) }}</div>
                                        <div class="lr-acts"></div>
                                    </div>
                                </template>
                            </div>

                            <!-- Saldo da cobrança -->
                            <div class="cc-saldo-line">
                                <span>Saldo da cobrança</span>
                                <b :class="{ zero: cobranca.saldo <= 0 }">{{ fmtMoeda(Math.max(0, cobranca.saldo)) }}</b>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </template>
    </template>

    <!-- PaymentModal reutilizado do F1 (CA26) -->
    <PaymentModal
        :aberto="cobrancaParaPagar !== null"
        :cobranca="cobrancaParaPagar ? cobrancaDetalheParaModal(cobrancaParaPagar) : null"
        :formas-pagamento="formasPagamento"
        :pode-desconto="podeRegistrar()"
        :carregando="registrandoPagamento"
        @fechar="cobrancaParaPagar = null"
        @pago="onPago"
    />

    <!-- Modal de estorno (CA29/CA31) -->
    <EstornoModal
        :aberto="estornoAlvo !== null"
        :pagamento="estornoAlvo?.pagamento ?? null"
        :carregando="enviandoEstorno"
        @fechar="estornoAlvo = null"
        @confirmar="onConfirmarEstorno"
    />

    <!-- Modal de guia/autorização (F6/R10/R13) -->
    <AppModal
        :aberto="modalGuiaAberto"
        largura="sm"
        @fechar="modalGuiaAberto = false"
    >
        <template #titulo>
            <h2>{{ cobrancaGuia?.guiaNumero ? 'Atualizar guia' : 'Registrar guia' }}</h2>
        </template>
        <div class="guia-form">
            <AppField label="Número da guia" required>
                <input
                    v-model="formGuia.guiaNumero"
                    class="form-input"
                    placeholder="Nº da guia emitida pelo convênio"
                    maxlength="100"
                />
            </AppField>
            <AppField label="Senha de autorização (opcional)">
                <input
                    v-model="formGuia.guiaSenha"
                    class="form-input"
                    placeholder="Senha do convênio"
                    maxlength="100"
                />
            </AppField>
            <AppField label="Data de autorização (opcional)">
                <input
                    v-model="formGuia.guiaAutorizadaEm"
                    class="form-input"
                    type="date"
                />
            </AppField>
            <p v-if="erroGuia" class="msg-erro-inline">{{ erroGuia }}</p>
        </div>
        <template #rodape>
            <AppButton variante="ghost" @click="modalGuiaAberto = false">Cancelar</AppButton>
            <AppButton variante="primario" :executando="salvandoGuia" @click="salvarGuia">
                Salvar guia
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
/* Gate restrito */
.fin-restricted {
    display: flex; flex-direction: column; align-items: center;
    text-align: center;
    padding: 56px 24px;
    gap: 10px;
}
.fr-icon {
    width: 64px; height: 64px;
    background: hsl(var(--secondary) / 0.06);
    border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    font-size: 24px; color: hsl(var(--secondary) / 0.4);
    margin-bottom: 4px;
}
.fin-restricted b {
    font-size: var(--text-lg);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--primary-dark));
}
.fin-restricted p {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.7);
    line-height: 1.6;
    max-width: 380px;
    margin: 0;
}
.fr-audit {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.5);
    display: flex; align-items: center; gap: 5px;
    margin-top: 4px;
}

/* KPIs */
.fin-kpis {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
    margin-bottom: 20px;
}
.fin-kpi {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 10px;
    padding: 14px 18px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
    display: flex; flex-direction: column; gap: 4px;
}
.fin-kpi span {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.6);
    text-transform: uppercase; letter-spacing: 0.04em;
}
.fin-kpi b {
    font-size: var(--text-xl);
    font-weight: var(--font-weight-extrabold);
    color: hsl(var(--primary-dark));
}
.fin-kpi--ok    { border-top: 3px solid hsl(var(--success)); }
.fin-kpi--ok b  { color: hsl(160 79% 30%); }
.fin-kpi--danger    { border-top: 3px solid hsl(var(--error)); }
.fin-kpi--danger b  { color: hsl(var(--error)); }
.fin-kpi--zero b    { color: hsl(var(--secondary) / 0.5); }

/* Lista de cobranças */
.charge-list {
    display: flex; flex-direction: column; gap: 8px;
}
.charge-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 10px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
    overflow: hidden;
    transition: box-shadow 150ms;
}
.charge-card:hover { box-shadow: 0 2px 6px -2px rgb(0 0 0 / 0.07); }
.charge-card.open { border-color: hsl(var(--primary) / 0.2); }

.cc-main {
    display: flex; align-items: center; gap: 12px;
    padding: 14px 16px;
    cursor: pointer;
    flex-wrap: wrap;
}

/* Tag de origem */
.cc-origin {
    display: inline-flex; align-items: center; gap: 5px;
    font-size: var(--text-xs); font-weight: var(--font-weight-semibold);
    padding: 3px 9px; border-radius: 999px;
    background: color-mix(in srgb, var(--ot) 12%, white);
    color: var(--ot);
    flex-shrink: 0;
    white-space: nowrap;
}
.cc-origin i { font-size: 10px; }

.cc-desc { flex: 1; min-width: 140px; }
.cc-desc b { font-size: var(--text-sm); font-weight: var(--font-weight-semibold); color: hsl(var(--primary-dark)); display: block; }
.cc-status { flex-shrink: 0; }

/* Status pills */
.cc-pill {
    display: inline-flex; align-items: center; gap: 4px;
    font-size: var(--text-xs); font-weight: var(--font-weight-semibold);
    padding: 2px 9px; border-radius: 999px;
}
.pill--ok      { background: hsl(var(--success) / 0.12); color: hsl(160 79% 30%); }
.pill--partial { background: hsl(var(--warning) / 0.15); color: hsl(40 95% 35%); }
.pill--warning { background: hsl(var(--warning) / 0.12); color: hsl(40 90% 35%); }
.pill--muted   { background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.55); }

.cc-amount { display: flex; flex-direction: column; align-items: flex-end; min-width: 100px; }
.cc-amount b { font-size: var(--text-base); font-weight: var(--font-weight-extrabold); color: hsl(var(--primary-dark)); }
.cc-saldo { font-size: var(--text-xs); color: hsl(var(--error)); font-weight: var(--font-weight-semibold); }
.cc-ok    { font-size: var(--text-xs); color: hsl(160 79% 30%); font-weight: var(--font-weight-semibold); }

.cc-actions {
    display: flex; align-items: center; gap: 6px; flex-shrink: 0;
}
.cc-chev {
    background: none; border: none; cursor: pointer;
    color: hsl(var(--secondary) / 0.45);
    padding: 4px; display: flex; align-items: center;
    font-size: 12px;
    transition: color 150ms;
}
.cc-chev:hover { color: hsl(var(--primary)); }

/* Detalhe expandido */
.cc-detail { border-top: 1px solid hsl(var(--secondary) / 0.06); }
.cc-block { padding: 12px 16px; border-bottom: 1px solid hsl(var(--secondary) / 0.06); }
.cc-block:last-child { border-bottom: none; }
.ccb-title {
    font-size: var(--text-xs); font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.6);
    text-transform: uppercase; letter-spacing: 0.04em;
    margin-bottom: 10px;
    display: flex; align-items: center; gap: 6px;
}

.cc-convenio-note {
    display: flex; align-items: center; gap: 8px;
    font-size: var(--text-sm); color: hsl(var(--secondary) / 0.7);
    background: hsl(var(--secondary) / 0.04);
    border-radius: 6px; padding: 8px 12px;
}
.cc-convenio-note i { color: hsl(var(--primary) / 0.6); font-size: 13px; }

/* Ledger (pagamentos + estornos) */
.pay-ledger { display: flex; flex-direction: column; gap: 4px; margin-bottom: 10px; }
.ledger-row {
    display: flex; align-items: center; gap: 10px;
    padding: 8px 10px;
    border-radius: 6px;
    background: hsl(var(--secondary) / 0.025);
}
.ledger-row.pay.voided { opacity: 0.55; }
.ledger-row.refund {
    background: hsl(var(--error) / 0.04);
    border: 1px solid hsl(var(--error) / 0.1);
    margin-left: 24px;
}
.lr-icon { color: hsl(var(--secondary) / 0.4); font-size: 14px; flex-shrink: 0; }
.ledger-row.refund .lr-icon { color: hsl(var(--error) / 0.6); }
.lr-info { flex: 1; min-width: 0; }
.lr-info b { display: block; font-size: var(--text-sm); color: hsl(var(--primary-dark)); font-weight: var(--font-weight-semibold); }
.lr-info span { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.6); }
.lr-amount { font-size: var(--text-sm); font-weight: var(--font-weight-semibold); color: hsl(var(--primary-dark)); flex-shrink: 0; min-width: 80px; text-align: right; }
.ledger-row.refund .lr-amount { color: hsl(var(--error)); }
.lr-acts { display: flex; gap: 4px; flex-shrink: 0; }
.lr-voided-tag {
    font-size: var(--text-xs); font-weight: var(--font-weight-semibold);
    color: hsl(var(--error)); background: hsl(var(--error) / 0.1);
    padding: 2px 7px; border-radius: 999px;
}

.cc-empty-pay {
    font-size: var(--text-sm); color: hsl(var(--secondary) / 0.55);
    text-align: center; padding: 12px 0;
}

.cc-saldo-line {
    display: flex; justify-content: space-between; align-items: center;
    padding: 6px 10px;
    font-size: var(--text-sm);
}
.cc-saldo-line span { color: hsl(var(--secondary) / 0.7); }
.cc-saldo-line b { font-weight: var(--font-weight-bold); color: hsl(var(--error)); }
.cc-saldo-line b.zero { color: hsl(160 79% 30%); }

/* Histórico de valor */
.value-change {
    display: flex; align-items: center; gap: 8px;
    font-size: var(--text-sm);
    padding: 4px 0;
}
.vc-from { color: hsl(var(--secondary) / 0.55); text-decoration: line-through; }
.vc-to   { font-weight: var(--font-weight-semibold); color: hsl(var(--primary-dark)); }
.vc-meta { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.55); flex: 1; text-align: right; }

/* Head da seção */
.prontuario-head {
    display: flex; align-items: center; justify-content: space-between;
    gap: 16px; margin-bottom: 16px;
}
.prontuario-head h2 { font-size: var(--text-lg); font-weight: var(--font-weight-bold); color: hsl(var(--primary-dark)); margin: 0; }
.prontuario-head p { font-size: var(--text-sm); color: hsl(var(--secondary) / 0.7); margin: 4px 0 0; }

/* Mensagens */
.msg-info { color: hsl(var(--secondary) / 0.7); margin: 24px 0; }
.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px; padding: 10px 14px;
    font-size: var(--text-sm); margin: 24px 0;
}

/* Guia de autorização (F6) */
.cc-guia-preenchida {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: var(--text-xs);
    color: var(--color-success, hsl(142 71% 45%));
    margin-top: 6px;
}
.cc-guia-pendente {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: var(--text-xs);
    color: var(--color-text-muted);
    margin-top: 6px;
}
.btn-guia { margin-top: 8px; }
.guia-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

@media (max-width: 900px) {
    .fin-kpis { grid-template-columns: 1fr 1fr; }
    .cc-main { gap: 8px; }
    .cc-amount { min-width: unset; }
}
@media (max-width: 600px) {
    .fin-kpis { grid-template-columns: 1fr; }
}
</style>
