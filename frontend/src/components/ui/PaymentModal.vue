<script setup lang="ts">
/**
 * Modal de registro de pagamentos de cobrança do paciente (F1 — Financeiro).
 *
 * Props:
 *   - aberto:        controla visibilidade
 *   - cobranca:      detalhes completos (inclui pagamentos já registrados)
 *   - formasPagamento: lista de formas cadastradas no estabelecimento
 *   - podeDesconto:  true se o usuário tem permissão `financeiro_paciente.registrar` ou superior
 *
 * Eventos:
 *   - fechar: fechar sem pagamento
 *   - pago:   cobrança quitada/atualizada (re-renderiza a badge na agenda)
 */
import { ref, computed, watch } from "vue"
import AppModal from "./AppModal.vue"
import AppField from "./AppField.vue"
import AppButton from "./AppButton.vue"
import AppInputDecimal from "./AppInputDecimal.vue"
import AppSelect from "./AppSelect.vue"
import type { CobrancaDetalhe, FormaPagamentoItemRequest, RegistrarPagamentosRequest } from "@/services/cobrancaService"

// ── Prop types ────────────────────────────────────────────────────────────────

interface FormaPagamentoOpcao {
    value: number
    label: string
    taxaPercentual?: number
}

const props = defineProps<{
    aberto:           boolean
    cobranca:         CobrancaDetalhe | null
    formasPagamento:  FormaPagamentoOpcao[]
    podeDesconto?:    boolean
    carregando?:      boolean
}>()

const emit = defineEmits<{
    fechar: []
    pago: [payload: RegistrarPagamentosRequest]
}>()

// ── Estado do formulário ──────────────────────────────────────────────────────

interface LinhaForma {
    formaPagamentoId: number | null
    valor: string
    parcelas: number
}

const desconto = ref("0")
const dataPagamento = ref(new Date().toISOString().slice(0, 10))
const formas = ref<LinhaForma[]>([{ formaPagamentoId: null, valor: "", parcelas: 1 }])
const enviando = ref(false)
const erroEnvio = ref<string | null>(null)

// Reset ao abrir
watch(() => props.aberto, (aberto) => {
    if (aberto) {
        desconto.value = "0"
        dataPagamento.value = new Date().toISOString().slice(0, 10)
        formas.value = [{ formaPagamentoId: props.formasPagamento[0]?.value ?? null, valor: "", parcelas: 1 }]
        erroEnvio.value = null
    }
})

// ── Derived ───────────────────────────────────────────────────────────────────

const saldoDevedor = computed(() => props.cobranca?.saldoDevedor ?? 0)
const totalPago = computed(() => props.cobranca?.totalPago ?? 0)
const valorCobrado = computed(() => props.cobranca?.valorCobrado ?? 0)

const descontoNum = computed(() => parseFloat(desconto.value) || 0)
const liquidoComDesconto = computed(() =>
    Math.max(0, Number((valorCobrado.value - descontoNum.value).toFixed(2)))
)
const saldoAposDesconto = computed(() =>
    Math.max(0, Number((liquidoComDesconto.value - totalPago.value).toFixed(2)))
)

const totalFormas = computed(() =>
    formas.value.reduce((acc, f) => acc + (parseFloat(f.valor) || 0), 0)
)

const excedeSaldo = computed(() => totalFormas.value > saldoAposDesconto.value + 0.001)

/** Retorna o valor líquido após taxa para uma linha de forma, ou null se não há taxa/valor. */
function valorLiquidoForma(forma: LinhaForma): number | null {
    const valor = parseFloat(forma.valor) || 0
    if (valor <= 0) return null
    const opcao = props.formasPagamento.find(fp => fp.value === forma.formaPagamentoId)
    if (!opcao?.taxaPercentual || opcao.taxaPercentual <= 0) return null
    return Number((valor - valor * opcao.taxaPercentual / 100).toFixed(2))
}

const podePagar = computed(() =>
    !enviando.value &&
    !excedeSaldo.value &&
    formas.value.length > 0 &&
    formas.value.every(f => f.formaPagamentoId && parseFloat(f.valor) > 0)
)

// ── Helpers de exibição ───────────────────────────────────────────────────────

function fmt(valor: number): string {
    return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function labelStatus(status: string): string {
    const map: Record<string, string> = {
        Aberta: "Em aberto",
        ParcialmentePaga: "Parcialmente paga",
        Paga: "Paga",
        Cancelada: "Cancelada",
    }
    return map[status] ?? status
}

// ── Formulário ────────────────────────────────────────────────────────────────

function adicionarForma() {
    formas.value.push({ formaPagamentoId: props.formasPagamento[0]?.value ?? null, valor: "", parcelas: 1 })
}

function removerForma(idx: number) {
    if (formas.value.length > 1) formas.value.splice(idx, 1)
}

async function registrar() {
    if (!props.cobranca) return
    enviando.value = true
    erroEnvio.value = null
    try {
        const payload: RegistrarPagamentosRequest = {
            desconto: descontoNum.value,
            dataPagamento: dataPagamento.value,
            formas: formas.value.map(f => ({
                formaPagamentoId: f.formaPagamentoId!,
                valor: parseFloat(f.valor) || 0,
                parcelas: f.parcelas,
            } satisfies FormaPagamentoItemRequest)),
        }
        emit("pago", payload)
    } finally {
        enviando.value = false
    }
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Registrar pagamento" largura="md" @fechar="$emit('fechar')">
        <!-- Cabeçalho de saldo -->
        <div v-if="cobranca" class="payment-header">
            <div class="payment-header__row">
                <span class="payment-header__label">Valor cobrado</span>
                <span class="payment-header__value">{{ fmt(cobranca.valorCobrado) }}</span>
            </div>
            <div v-if="cobranca.totalPago > 0" class="payment-header__row">
                <span class="payment-header__label">Já pago</span>
                <span class="payment-header__value payment-header__value--pago">{{ fmt(cobranca.totalPago) }}</span>
            </div>
            <div class="payment-header__row payment-header__row--saldo">
                <span class="payment-header__label">Saldo devedor</span>
                <span class="payment-header__value payment-header__value--saldo">{{ fmt(saldoDevedor) }}</span>
            </div>
            <div v-if="cobranca.status !== 'Aberta'" class="payment-header__status">
                {{ labelStatus(cobranca.status) }}
            </div>
        </div>

        <!-- Histórico de pagamentos -->
        <template v-if="cobranca && cobranca.pagamentos.length > 0">
            <h3 class="payment-section-title">Histórico</h3>
            <table class="payment-historico">
                <thead>
                    <tr>
                        <th>Data</th>
                        <th>Forma</th>
                        <th>Valor</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="p in cobranca.pagamentos" :key="p.id">
                        <td>{{ new Date(p.dataPagamento + 'T00:00:00').toLocaleDateString('pt-BR') }}</td>
                        <td>{{ p.formaPagamentoNome }}</td>
                        <td class="payment-historico__valor">{{ fmt(p.valor) }}</td>
                    </tr>
                </tbody>
            </table>
        </template>

        <!-- Estado: cobrança já quitada -->
        <div v-if="cobranca?.status === 'Paga'" class="payment-paga">
            <i class="fa-solid fa-circle-check payment-paga__icon" />
            <span>Esta cobrança já está quitada.</span>
        </div>

        <!-- Formulário de novo pagamento -->
        <template v-else-if="cobranca && cobranca.status !== 'Cancelada'">
            <h3 class="payment-section-title">Novo pagamento</h3>

            <!-- Desconto (apenas se usuário tem permissão) -->
            <AppField v-if="podeDesconto" label="Desconto">
                <div class="payment-field-row">
                    <AppInputDecimal v-model="desconto" placeholder="0,00" />
                    <span v-if="descontoNum > 0" class="payment-field-hint">
                        Líquido: {{ fmt(liquidoComDesconto) }}
                    </span>
                </div>
            </AppField>

            <!-- Data de pagamento -->
            <AppField label="Data do pagamento">
                <input
                    v-model="dataPagamento"
                    type="date"
                    class="form-input"
                />
            </AppField>

            <!-- Formas de pagamento (R11: múltiplas) -->
            <div v-for="(forma, idx) in formas" :key="idx" class="payment-forma">
                <div class="payment-forma__campos">
                    <AppField label="Forma de pagamento" class="payment-forma__select">
                        <AppSelect
                            v-model="forma.formaPagamentoId"
                            :options="formasPagamento"
                        />
                    </AppField>

                    <AppField label="Valor" class="payment-forma__valor">
                        <AppInputDecimal
                            v-model="forma.valor"
                            placeholder="0,00"
                        />
                        <span
                            v-if="valorLiquidoForma(forma) !== null"
                            class="payment-taxa-hint"
                        >
                            Você recebe {{ fmt(valorLiquidoForma(forma)!) }}
                        </span>
                    </AppField>

                    <AppField label="Parcelas" class="payment-forma__parcelas">
                        <input
                            v-model.number="forma.parcelas"
                            type="number"
                            min="1"
                            max="24"
                            class="form-input"
                        />
                    </AppField>

                    <button
                        v-if="formas.length > 1"
                        type="button"
                        class="btn-icon btn-icon-excluir payment-forma__remover"
                        title="Remover forma"
                        @click="removerForma(idx)"
                    >
                        <i class="fa-solid fa-xmark" />
                    </button>
                </div>
            </div>

            <div class="payment-forma__acoes">
                <button
                    type="button"
                    class="payment-add-forma"
                    @click="adicionarForma"
                >
                    <i class="fa-solid fa-plus" /> Adicionar forma
                </button>

                <span v-if="formas.length > 1" class="payment-total-formas">
                    Total: {{ fmt(totalFormas) }}
                </span>
            </div>

            <!-- Erro de excesso -->
            <p v-if="excedeSaldo" class="payment-erro">
                O total das formas ({{ fmt(totalFormas) }}) excede o saldo devedor ({{ fmt(saldoAposDesconto) }}).
            </p>

            <!-- Erro de envio -->
            <p v-if="erroEnvio" class="payment-erro">{{ erroEnvio }}</p>
        </template>

        <!-- Rodapé -->
        <template #rodape>
            <AppButton variant="secondary" @click="$emit('fechar')">Fechar</AppButton>
            <AppButton
                v-if="cobranca && cobranca.status !== 'Paga' && cobranca.status !== 'Cancelada'"
                :disabled="!podePagar"
                :loading="enviando"
                @click="registrar"
            >
                Registrar pagamento
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.payment-header {
    padding: var(--spacing-3);
    background: hsl(var(--muted));
    border-radius: var(--radius-md);
    display: flex;
    flex-direction: column;
    gap: var(--spacing-1);
}

.payment-header__row {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.payment-header__row--saldo {
    border-top: 1px solid hsl(var(--border));
    padding-top: var(--spacing-1);
    margin-top: var(--spacing-1);
}

.payment-header__label {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
}

.payment-header__value {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
}

.payment-header__value--pago {
    color: hsl(var(--success));
}

.payment-header__value--saldo {
    color: hsl(var(--destructive));
    font-size: var(--text-base);
}

.payment-header__status {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    text-align: right;
}

.payment-section-title {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
    margin: 0;
}

.payment-historico {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}

.payment-historico th {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    text-align: left;
    padding: var(--spacing-1) var(--spacing-2);
    border-bottom: 1px solid hsl(var(--border));
}

.payment-historico td {
    padding: var(--spacing-1) var(--spacing-2);
    color: hsl(var(--foreground));
}

.payment-historico__valor {
    text-align: right;
    font-weight: var(--font-weight-semibold);
}

.payment-paga {
    display: flex;
    align-items: center;
    gap: var(--spacing-2);
    color: hsl(var(--success));
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
}

.payment-paga__icon {
    font-size: var(--text-lg);
}

.payment-field-row {
    display: flex;
    align-items: center;
    gap: var(--spacing-2);
}

.payment-field-hint {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
}

.payment-forma {
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm);
    padding: var(--spacing-2);
}

.payment-forma__campos {
    display: grid;
    grid-template-columns: 1fr 120px 80px auto;
    gap: var(--spacing-2);
    align-items: end;
}

.payment-forma__remover {
    margin-bottom: var(--spacing-1);
}

.payment-forma__acoes {
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.payment-add-forma {
    font-size: var(--text-sm);
    color: hsl(var(--primary));
    background: none;
    border: none;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: var(--spacing-1);
    padding: 0;
}

.payment-add-forma:hover {
    text-decoration: underline;
}

.payment-total-formas {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
}

.payment-erro {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    margin: 0;
}

.payment-taxa-hint {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
    margin-top: var(--spacing-1);
    display: block;
}
</style>
