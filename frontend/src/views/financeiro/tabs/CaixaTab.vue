<script setup lang="ts">
import { ref, onMounted } from "vue"
import { financeiroService, type CaixaDiario } from "@/services/financeiroService"
import { AppButton, AppModal, AppField, AppInput, AppToast } from "@/components/ui"

const props = defineProps<{ ehDono: boolean }>()

const caixa = ref<CaixaDiario | null>(null)
const carregando = ref(false)
const erroGlobal = ref<string | null>(null)

// Estado "não aberto" quando caixa é null.
// CA162/CA163/CA164/CA165/CA167 todos passam por aqui.

async function carregar() {
    carregando.value = true
    erroGlobal.value = null
    try {
        caixa.value = await financeiroService.obterCaixa()
    } catch (e: any) {
        erroGlobal.value = e?.response?.data?.mensagem ?? "Erro ao carregar caixa."
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
function formatarDataHora(s: string) {
    const d = new Date(s)
    return d.toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
}
function formatarData(s: string) {
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}
</script>

<template>
    <div class="caixa-tab">
        <p v-if="carregando" class="info">Carregando...</p>
        <p v-if="erroGlobal" class="msg-erro">{{ erroGlobal }}</p>

        <!-- Estado: não aberto -->
        <div v-if="!carregando && !caixa" class="estado-card nao-aberto">
            <i class="fa-solid fa-cash-register estado-icon" aria-hidden="true" />
            <h2 class="ds-section-title">Caixa não aberto hoje</h2>
            <p class="estado-desc">Abra o caixa do dia para registrar movimentações.</p>
            <AppButton icon="fa-solid fa-lock-open" @click="abrirCaixa" :loading="abrindo">
                Abrir caixa
            </AppButton>
        </div>

        <!-- Estado: aberto ou fechado -->
        <template v-if="!carregando && caixa">
            <!-- Header do caixa -->
            <div class="caixa-header" :class="caixa.status.toLowerCase()">
                <div class="caixa-status-badge" :class="caixa.status.toLowerCase()">
                    <i :class="caixa.status === 'Aberto' ? 'fa-solid fa-lock-open' : 'fa-solid fa-lock'" />
                    {{ caixa.status }}
                </div>

                <div class="caixa-info">
                    <span class="caixa-data">{{ formatarData(caixa.data) }}</span>
                    <span class="caixa-aberto-por">
                        Aberto por <strong>{{ caixa.abertoPorNome }}</strong>
                        às {{ formatarDataHora(caixa.abertoEm) }}
                    </span>
                    <span v-if="caixa.status === 'Fechado' && caixa.fechadoPorNome" class="caixa-fechado-por">
                        Fechado por <strong>{{ caixa.fechadoPorNome }}</strong>
                        às {{ caixa.fechadoEm ? formatarDataHora(caixa.fechadoEm) : "—" }}
                        <template v-if="caixa.observacao">· "{{ caixa.observacao }}"</template>
                    </span>
                </div>

                <div class="caixa-acoes">
                    <AppButton
                        v-if="caixa.status === 'Aberto'"
                        icon="fa-solid fa-lock"
                        variant="secondary"
                        @click="modalFechar = true; formFechar.observacao = ''"
                    >
                        Fechar caixa
                    </AppButton>
                    <AppButton
                        v-if="caixa.status === 'Fechado' && ehDono"
                        icon="fa-solid fa-lock-open"
                        variant="secondary"
                        @click="reabrirCaixa"
                        :loading="reabrindo"
                    >
                        Reabrir caixa
                    </AppButton>
                </div>
            </div>

            <!-- Resumo por forma de pagamento -->
            <section class="resumo-caixa">
                <h2 class="ds-section-title">Resumo do dia</h2>
                <table class="resumo-table">
                    <thead>
                        <tr>
                            <th>Forma de pagamento</th>
                            <th class="col-valor">Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="r in caixa.resumoPorForma" :key="r.formaPagamento">
                            <td>{{ r.formaPagamento }}</td>
                            <td class="col-valor receita">{{ moeda(r.total) }}</td>
                        </tr>
                        <tr v-if="caixa.totalEstornos < 0" class="estorno-row">
                            <td>Estornos</td>
                            <td class="col-valor despesa">{{ moeda(caixa.totalEstornos) }}</td>
                        </tr>
                    </tbody>
                    <tfoot>
                        <tr class="total-row">
                            <td>Total do dia</td>
                            <td class="col-valor" :class="caixa.totalDia >= 0 ? 'receita' : 'despesa'">
                                {{ moeda(caixa.totalDia) }}
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </section>
        </template>
    </div>

    <!-- Modal fechar caixa -->
    <AppModal :aberto="modalFechar" titulo="Fechar caixa" @fechar="modalFechar = false">
        <!-- Resumo no modal -->
        <div v-if="caixa" class="resumo-modal">
            <p class="resumo-modal-label">Resumo antes de fechar:</p>
            <div v-for="r in caixa.resumoPorForma" :key="r.formaPagamento" class="resumo-modal-row">
                <span>{{ r.formaPagamento }}</span>
                <span class="receita">{{ moeda(r.total) }}</span>
            </div>
            <div v-if="caixa.totalEstornos < 0" class="resumo-modal-row">
                <span>Estornos</span>
                <span class="despesa">{{ moeda(caixa.totalEstornos) }}</span>
            </div>
            <div class="resumo-modal-row total">
                <span>Total</span>
                <span :class="caixa.totalDia >= 0 ? 'receita' : 'despesa'">{{ moeda(caixa.totalDia) }}</span>
            </div>
        </div>

        <AppField label="Observação (opcional)">
            <AppInput v-model="formFechar.observacao" placeholder="Anotação de fechamento..." />
        </AppField>
        <p v-if="erroFechar" class="msg-erro">{{ erroFechar }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="modalFechar = false">Cancelar</AppButton>
            <AppButton :loading="fechando" @click="fecharCaixa">Fechar caixa</AppButton>
        </template>
    </AppModal>

    <AppToast v-if="toast" :mensagem="toast.mensagem" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
.caixa-tab { display: flex; flex-direction: column; gap: 1.5rem; }

/* Estado não aberto */
.estado-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    padding: 2.5rem 1rem;
    background: hsl(var(--card));
    border: 1px dashed hsl(var(--border));
    border-radius: 12px;
    text-align: center;
}
.estado-icon {
    font-size: var(--text-3xl);
    color: hsl(var(--muted-foreground));
}
.estado-desc { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); margin: 0; }

/* Header caixa */
.caixa-header {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 12px;
    padding: 1.25rem;
    flex-wrap: wrap;
}
.caixa-header.aberto  { border-left: 4px solid hsl(var(--success)); }
.caixa-header.fechado { border-left: 4px solid hsl(var(--muted-foreground)); }

.caixa-status-badge {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.35rem 0.85rem;
    border-radius: 999px;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    white-space: nowrap;
}
.caixa-status-badge.aberto  { background: hsl(var(--success) / 0.15); color: hsl(var(--success)); }
.caixa-status-badge.fechado { background: hsl(var(--muted)); color: hsl(var(--muted-foreground)); }

.caixa-info {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    flex: 1;
}
.caixa-data { font-size: var(--text-base); font-weight: var(--font-weight-semibold); }
.caixa-aberto-por, .caixa-fechado-por { font-size: var(--text-sm); color: hsl(var(--muted-foreground)); }

.caixa-acoes { margin-left: auto; }

/* Resumo */
.resumo-table { width: 100%; border-collapse: collapse; font-size: var(--text-sm); }
.resumo-table th {
    background: hsl(var(--muted));
    text-align: left;
    padding: 0.45rem 0.75rem;
    border-bottom: 2px solid hsl(var(--border));
    font-weight: var(--font-weight-semibold);
}
.resumo-table td { padding: 0.45rem 0.75rem; border-bottom: 1px solid hsl(var(--border)); }
.col-valor { text-align: right; font-variant-numeric: tabular-nums; }
.total-row td { font-weight: var(--font-weight-semibold); background: hsl(var(--muted) / 0.5); }
.estorno-row td { color: hsl(var(--destructive)); }
.receita { color: hsl(var(--success)); }
.despesa { color: hsl(var(--destructive)); }

/* Modal resumo */
.resumo-modal { margin-bottom: 1rem; font-size: var(--text-sm); }
.resumo-modal-label { font-weight: var(--font-weight-semibold); margin-bottom: 0.5rem; }
.resumo-modal-row {
    display: flex;
    justify-content: space-between;
    padding: 0.25rem 0;
    border-bottom: 1px solid hsl(var(--border));
}
.resumo-modal-row.total {
    font-weight: var(--font-weight-semibold);
    border-top: 2px solid hsl(var(--border));
    border-bottom: none;
    margin-top: 0.25rem;
}

.info { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
