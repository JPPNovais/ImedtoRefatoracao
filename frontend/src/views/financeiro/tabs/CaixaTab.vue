<script setup lang="ts">
import { ref, onMounted } from "vue"
import { financeiroService, type CaixaDiario } from "@/services/financeiroService"
import { AppButton, AppModal, AppField, AppInput, AppToast } from "@/components/ui"
import { usePermissoesStore } from "@/stores/permissoesStore"

const props = defineProps<{ ehDono: boolean }>()

const permissoesStore = usePermissoesStore()
const podeFecharCaixa = () => permissoesStore.pode("financeiro.fechar")

const caixa = ref<CaixaDiario | null>(null)
const carregando = ref(false)
const erroGlobal = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erroGlobal.value = null
    try {
        caixa.value = await financeiroService.obterCaixa()
    } catch {
        erroGlobal.value = "Erro ao carregar caixa."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)

// ─── Abrir ────────────────────────────────────────────────────────────────────
const abrindo = ref(false)
const toast = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)

async function abrirCaixa() {
    abrindo.value = true
    try {
        await financeiroService.abrirCaixa({})
        toast.value = { mensagem: "Caixa aberto.", variante: "success" }
        await carregar()
    } catch (e: any) {
        toast.value = { mensagem: e?.response?.data?.mensagem ?? "Erro ao abrir caixa.", variante: "error" }
    } finally {
        abrindo.value = false
    }
}

// ─── Fechar ───────────────────────────────────────────────────────────────────
const modalFechar = ref(false)
const formFechar = ref({ observacao: "" })
const erroFechar = ref<string | null>(null)
const fechando = ref(false)

async function fecharCaixa() {
    fechando.value = true; erroFechar.value = null
    try {
        await financeiroService.fecharCaixa({ observacao: formFechar.value.observacao || undefined })
        modalFechar.value = false
        toast.value = { mensagem: "Caixa fechado com sucesso.", variante: "success" }
        await carregar()
    } catch (e: any) {
        erroFechar.value = e?.response?.data?.mensagem ?? "Erro ao fechar caixa."
    } finally {
        fechando.value = false
    }
}

// ─── Reabrir ──────────────────────────────────────────────────────────────────
const reabrindo = ref(false)

async function reabrirCaixa() {
    reabrindo.value = true
    try {
        await financeiroService.reabrirCaixa({})
        toast.value = { mensagem: "Caixa reaberto.", variante: "success" }
        await carregar()
    } catch (e: any) {
        toast.value = { mensagem: e?.response?.data?.mensagem ?? "Erro ao reabrir caixa.", variante: "error" }
    } finally {
        reabrindo.value = false
    }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
function formatarData(s: string) {
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}
function formatarHora(s: string) {
    return new Date(s).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
}
const ICONES_FORMA: Record<string, string> = {
    "Dinheiro": "fa-solid fa-money-bill-wave",
    "PIX":      "fa-brands fa-pix",
    "Crédito":  "fa-solid fa-credit-card",
    "Débito":   "fa-regular fa-credit-card",
    "Boleto":   "fa-solid fa-barcode",
}
function iconeForma(forma: string): string {
    return ICONES_FORMA[forma] ?? "fa-solid fa-money-bill"
}
</script>

<template>
    <div class="caixa-tab">
        <div v-if="carregando" class="info">Carregando...</div>
        <div v-if="erroGlobal" class="msg-erro">{{ erroGlobal }}</div>

        <!-- Estado: não aberto (caixa null) -->
        <div v-if="!carregando && !caixa" class="cx-state-card">
            <div class="cx-state-ic"><i class="fa-solid fa-cash-register" aria-hidden="true" /></div>
            <b>Caixa ainda não aberto hoje</b>
            <p>Abra o caixa para começar a registrar os recebimentos do dia.</p>
            <AppButton
                v-if="podeFecharCaixa()"
                icon="fa-solid fa-unlock"
                :loading="abrindo"
                @click="abrirCaixa"
            >
                Abrir caixa
            </AppButton>
        </div>

        <!-- Estado: aberto -->
        <template v-if="!carregando && caixa && caixa.status === 'Aberto'">
            <div class="cx-status open">
                <div class="cx-status-l">
                    <span class="cx-badge open">
                        <span class="cx-dot" aria-hidden="true" />
                        Caixa aberto
                    </span>
                    <div class="cx-status-info">
                        <b>{{ formatarData(caixa.data) }}</b>
                        <span>Aberto às {{ formatarHora(caixa.abertoEm) }} por {{ caixa.abertoPorNome }}</span>
                    </div>
                </div>
                <AppButton
                    v-if="podeFecharCaixa()"
                    icon="fa-solid fa-lock"
                    @click="modalFechar = true; formFechar.observacao = ''"
                >
                    Fechar caixa
                </AppButton>
            </div>

            <div class="cf-card">
                <div class="cf-card-h">
                    <div><i class="fa-solid fa-coins" aria-hidden="true" /> Resumo do dia por forma de pagamento</div>
                </div>
                <div class="cx-methods-wrap">
                    <div class="cx-methods">
                        <div
                            v-for="r in caixa.resumoPorForma"
                            :key="r.formaPagamento"
                            class="cx-method"
                        >
                            <span><i :class="iconeForma(r.formaPagamento)" aria-hidden="true" /> {{ r.formaPagamento }}</span>
                            <b>{{ moeda(r.total) }}</b>
                        </div>
                        <div v-if="caixa.totalEstornos !== 0" class="cx-method refund">
                            <span><i class="fa-solid fa-rotate-left" aria-hidden="true" /> Estornos</span>
                            <b>– {{ moeda(Math.abs(caixa.totalEstornos)) }}</b>
                        </div>
                        <div class="cx-method total">
                            <span>Total do dia</span>
                            <b>{{ moeda(caixa.totalDia) }}</b>
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <!-- Estado: fechado -->
        <template v-if="!carregando && caixa && caixa.status === 'Fechado'">
            <div class="cx-status closed">
                <div class="cx-status-l">
                    <span class="cx-badge closed">
                        <i class="fa-solid fa-lock" aria-hidden="true" />
                        Caixa fechado
                    </span>
                    <div class="cx-status-info">
                        <b>{{ formatarData(caixa.data) }}</b>
                        <span>
                            Fechado por {{ caixa.fechadoPorNome ?? '—' }}
                            {{ caixa.fechadoEm ? ' · às ' + formatarHora(caixa.fechadoEm) : '' }}
                        </span>
                    </div>
                </div>
                <span class="cx-readonly"><i class="fa-solid fa-eye" aria-hidden="true" /> Somente leitura</span>
            </div>

            <div class="cf-card">
                <div class="cf-card-h">
                    <div><i class="fa-solid fa-receipt" aria-hidden="true" /> Resumo do fechamento</div>
                    <AppButton
                        v-if="ehDono"
                        variant="secondary"
                        size="sm"
                        icon="fa-solid fa-lock-open"
                        :loading="reabrindo"
                        @click="reabrirCaixa"
                    >
                        Reabrir
                    </AppButton>
                </div>
                <div class="cx-methods-wrap">
                    <div class="cx-methods">
                        <div
                            v-for="r in caixa.resumoPorForma"
                            :key="r.formaPagamento"
                            class="cx-method"
                        >
                            <span><i :class="iconeForma(r.formaPagamento)" aria-hidden="true" /> {{ r.formaPagamento }}</span>
                            <b>{{ moeda(r.total) }}</b>
                        </div>
                        <div v-if="caixa.totalEstornos !== 0" class="cx-method refund">
                            <span><i class="fa-solid fa-rotate-left" aria-hidden="true" /> Estornos</span>
                            <b>– {{ moeda(Math.abs(caixa.totalEstornos)) }}</b>
                        </div>
                        <div class="cx-method total">
                            <span>Total do dia</span>
                            <b>{{ moeda(caixa.totalDia) }}</b>
                        </div>
                    </div>
                </div>
                <div v-if="caixa.observacao" class="cx-obs">
                    <i class="fa-solid fa-comment" aria-hidden="true" />
                    {{ caixa.observacao }}
                </div>
            </div>
        </template>
    </div>

    <!-- Modal fechar caixa -->
    <AppModal
        :aberto="modalFechar"
        titulo="Fechar caixa do dia"
        @fechar="modalFechar = false"
    >
        <!-- Resumo no modal -->
        <div v-if="caixa" class="fc-summary">
            <div
                v-for="r in caixa.resumoPorForma"
                :key="r.formaPagamento"
                class="fc-row"
            >
                <span><i :class="iconeForma(r.formaPagamento)" aria-hidden="true" /> {{ r.formaPagamento }}</span>
                <b>{{ moeda(r.total) }}</b>
            </div>
            <div v-if="caixa.totalEstornos !== 0" class="fc-row refund">
                <span><i class="fa-solid fa-rotate-left" aria-hidden="true" /> Estornos</span>
                <b>– {{ moeda(Math.abs(caixa.totalEstornos)) }}</b>
            </div>
            <div class="fc-row total">
                <span>Total do dia</span>
                <b>{{ moeda(caixa.totalDia) }}</b>
            </div>
        </div>

        <AppField label="Observação do fechamento" :required="false">
            <AppInput v-model="formFechar.observacao" placeholder="Diferença de caixa, sangria, observações..." />
        </AppField>
        <p v-if="erroFechar" class="msg-erro">{{ erroFechar }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="modalFechar = false">Cancelar</AppButton>
            <AppButton icon="fa-solid fa-lock" :loading="fechando" @click="fecharCaixa">
                Confirmar fechamento
            </AppButton>
        </template>
    </AppModal>

    <AppToast v-if="toast" :mensagem="toast.mensagem" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
.caixa-tab { display: flex; flex-direction: column; gap: 1rem; }

/* Estado não aberto */
.cx-state-card {
    text-align: center;
    padding: 48px 24px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
}
.cx-state-ic {
    width: 60px;
    height: 60px;
    border-radius: 16px;
    margin-bottom: 4px;
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-3xl);
}
.cx-state-card b { font-size: var(--text-md); color: var(--c-primary-dark); }
.cx-state-card p { font-size: var(--text-sm); color: hsl(var(--secondary) / 0.6); margin: 0 0 6px; max-width: 380px; }

/* Banner de status */
.cx-status {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 20px;
    border-radius: 12px;
}
.cx-status.open   { background: hsl(var(--success) / 0.06); border: 1px solid hsl(var(--success) / 0.22); }
.cx-status.closed { background: hsl(var(--secondary) / 0.04); border: 1px solid hsl(var(--secondary) / 0.12); }

.cx-status-l { display: flex; align-items: center; gap: 16px; }

.cx-badge {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 6px 13px;
    border-radius: 999px;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-bold);
}
.cx-badge.open   { background: hsl(var(--success) / 0.14); color: hsl(160 79% 28%); }
.cx-badge.closed { background: hsl(var(--secondary) / 0.1); color: hsl(var(--secondary) / 0.6); }

.cx-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: hsl(var(--success));
    box-shadow: 0 0 0 3px hsl(var(--success) / 0.2);
}

.cx-status-info b { display: block; font-size: var(--text-base); color: var(--c-primary-dark); }
.cx-status-info span { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.65); }

.cx-readonly {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.5);
    display: inline-flex;
    align-items: center;
    gap: 6px;
}

/* Card */
.cf-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
}
.cf-card-h {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 14px 18px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.07);
}
.cf-card-h > div:first-child {
    display: inline-flex;
    align-items: center;
    gap: 9px;
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: var(--c-primary-dark);
}
.cf-card-h i { color: hsl(var(--primary)); }

.cx-methods-wrap { padding: 16px 18px; }
.cx-methods {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 10px;
}
@media (max-width: 1100px) {
    .cx-methods { grid-template-columns: repeat(2, 1fr); }
}
.cx-method {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 14px 16px;
    border-radius: var(--radius-lg);
    background: hsl(var(--secondary) / 0.03);
    border: 1px solid hsl(var(--secondary) / 0.08);
}
.cx-method span {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.65);
    display: inline-flex;
    align-items: center;
    gap: 7px;
}
.cx-method b {
    font-size: var(--text-lg);
    font-weight: var(--font-weight-extrabold);
    color: var(--c-primary-dark);
}
.cx-method.refund b { color: hsl(28 90% 45%); }
.cx-method.total {
    background: hsl(var(--primary) / 0.06);
    border-color: hsl(var(--primary) / 0.18);
}
.cx-method.total b { color: hsl(var(--primary-dark)); }

.cx-obs {
    padding: 12px 18px;
    border-top: 1px solid hsl(var(--secondary) / 0.07);
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.7);
    display: flex;
    align-items: center;
    gap: 9px;
}
.cx-obs i { color: hsl(var(--secondary) / 0.4); }

/* Modal resumo */
.fc-summary {
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: var(--radius-lg);
    overflow: hidden;
    margin-bottom: 16px;
}
.fc-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 14px;
    font-size: var(--text-sm);
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.fc-row span { color: hsl(var(--secondary) / 0.7); display: inline-flex; align-items: center; gap: 8px; }
.fc-row b { color: var(--c-primary-dark); font-weight: var(--font-weight-bold); }
.fc-row.refund b { color: hsl(28 90% 45%); }
.fc-row.total { background: hsl(var(--primary) / 0.05); border-bottom: none; }
.fc-row.total span { font-weight: var(--font-weight-semibold); color: var(--c-primary-dark); }
.fc-row.total b { font-size: var(--text-lg); font-weight: var(--font-weight-extrabold); color: hsl(var(--primary-dark)); }

.info { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
