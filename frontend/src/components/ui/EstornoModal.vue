<script setup lang="ts">
/**
 * Modal de confirmação de estorno de pagamento (F2 — INV-7).
 *
 * Props:
 *   aberto    — controla visibilidade
 *   pagamento — resumo do pagamento a ser estornado (valor, forma, data)
 *   carregando — bloqueia o botão enquanto a request está em flight
 *
 * Eventos:
 *   fechar     — fecha sem confirmar
 *   confirmar  — emite o motivo preenchido pelo usuário
 */
import { ref, watch } from "vue"
import AppModal from "./AppModal.vue"
import AppButton from "./AppButton.vue"
import type { PagamentoAba } from "@/services/cobrancaService"

const props = defineProps<{
    aberto: boolean
    pagamento: PagamentoAba | null
    carregando?: boolean
}>()

const emit = defineEmits<{
    fechar: []
    confirmar: [motivo: string]
}>()

const motivo = ref("")
const erroMotivo = ref(false)

// Limpa o motivo ao abrir (novo estorno)
watch(() => props.aberto, (aberto) => {
    if (aberto) {
        motivo.value = ""
        erroMotivo.value = false
    }
})

function confirmar() {
    if (!motivo.value.trim()) {
        erroMotivo.value = true
        return
    }
    emit("confirmar", motivo.value.trim())
}

function fmtMoeda(n: number): string {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function fmtData(iso: string): string {
    if (!iso) return "—"
    try {
        return new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "short", year: "numeric" })
    } catch { return iso }
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Estornar pagamento" @fechar="$emit('fechar')">
        <template #default>
            <div v-if="pagamento" class="estorno-summary">
                <div class="es-item">
                    <span>Forma</span>
                    <b>{{ pagamento.formaPagamentoNome }}{{ pagamento.parcelas > 1 ? ` · ${pagamento.parcelas}x` : "" }}</b>
                </div>
                <div class="es-item">
                    <span>Valor</span>
                    <b class="es-valor">{{ fmtMoeda(pagamento.valor) }}</b>
                </div>
                <div class="es-item">
                    <span>Data</span>
                    <b>{{ fmtData(pagamento.dataPagamento) }}</b>
                </div>
            </div>

            <div class="estorno-aviso">
                <i class="fa-solid fa-triangle-exclamation"></i>
                <span>Esta ação fica registrada no histórico — o pagamento original não é removido.</span>
            </div>

            <div class="form-field" :class="{ 'form-field--error': erroMotivo }">
                <label class="form-label">
                    Motivo do estorno <em>*</em>
                </label>
                <textarea
                    v-model="motivo"
                    class="form-input estorno-textarea"
                    rows="3"
                    placeholder="Ex.: cobrança duplicada, valor incorreto, desistência do paciente..."
                    autofocus
                    @input="erroMotivo = false"
                ></textarea>
                <span class="form-hint">
                    <i class="fa-solid fa-circle-info"></i>
                    Obrigatório — fica auditado com seu usuário e data/hora.
                </span>
                <span v-if="erroMotivo" class="form-error-msg">Informe o motivo antes de confirmar.</span>
            </div>
        </template>

        <template #rodape>
            <AppButton variant="ghost" @click="$emit('fechar')">Cancelar</AppButton>
            <AppButton
                variant="danger"
                icon="fa-solid fa-rotate-left"
                :loading="carregando"
                :disabled="carregando || !motivo.trim()"
                @click="confirmar"
            >
                Confirmar estorno
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.estorno-summary {
    display: flex; flex-direction: column;
    gap: 0;
    background: hsl(var(--secondary) / 0.04);
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 8px;
    padding: 12px 16px;
    margin-bottom: 14px;
}
.es-item {
    display: flex; justify-content: space-between; align-items: baseline;
    padding: 5px 0;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.es-item:last-child { border-bottom: none; }
.es-item span { font-size: var(--text-sm); color: hsl(var(--secondary) / 0.65); }
.es-item b { font-size: var(--text-sm); color: hsl(var(--primary-dark)); }
.es-valor { color: hsl(var(--primary)); }

.estorno-aviso {
    display: flex; align-items: flex-start; gap: 8px;
    background: hsl(var(--warning) / 0.08);
    border: 1px solid hsl(var(--warning) / 0.2);
    border-radius: 6px;
    padding: 8px 12px;
    margin-bottom: 16px;
    font-size: var(--text-sm);
    color: hsl(40 90% 30%);
}
.estorno-aviso i { color: hsl(40 95% 45%); margin-top: 2px; flex-shrink: 0; }

.form-field { display: flex; flex-direction: column; gap: 4px; }
.form-field--error .form-input { border-color: hsl(var(--error)); }
.form-label {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--primary-dark));
}
.form-label em { color: hsl(var(--error)); font-style: normal; margin-left: 2px; }
.estorno-textarea {
    resize: vertical; min-height: 80px;
    font-family: inherit; line-height: 1.5;
}
.form-hint {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.6);
    display: flex; align-items: center; gap: 4px;
}
.form-error-msg {
    font-size: var(--text-xs);
    color: hsl(var(--error));
}
</style>
