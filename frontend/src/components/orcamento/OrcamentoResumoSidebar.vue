<script setup lang="ts">
/**
 * Sidebar de resumo financeiro para o formulário de orçamento.
 * Exibe subtotal, desconto inline, total, nota de parcelamento e ações.
 */
import { computed } from "vue"
import { AppButton } from "@/components/ui"

const props = defineProps<{
    subtotal: number
    desconto: number
    tipoDesconto: "valor" | "percentual"
    totalGeral: number
    somaFormas: number
    integridadeOk: boolean
    diferenca: number
    salvando?: boolean
    calculando?: boolean
}>()

const emit = defineEmits<{
    "update:desconto": [v: number]
    "update:tipoDesconto": [v: "valor" | "percentual"]
    salvar: []
    salvarRascunho: []
    enviar: []
}>()

const descontoValor = computed(() => {
    if (props.tipoDesconto === "percentual") return props.subtotal * (props.desconto / 100)
    return props.desconto
})

function fmtBRL(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }
</script>

<template>
    <div class="resumo-sidebar">
        <div class="rs-titulo">{{ calculando ? "Resumo (calculando…)" : "Resumo financeiro" }}</div>

        <div class="rs-linhas">
            <div class="rs-row">
                <span>Subtotal</span>
                <span>{{ fmtBRL(subtotal) }}</span>
            </div>

            <!-- Desconto inline -->
            <div class="rs-discount">
                <label class="rs-disc-label">Desconto</label>
                <div class="rs-disc-input">
                    <input
                        type="number"
                        min="0"
                        :value="desconto"
                        @input="emit('update:desconto', Number(($event.target as HTMLInputElement).value) || 0)"
                        aria-label="Valor do desconto"
                    />
                    <div class="rs-disc-toggle">
                        <button
                            type="button"
                            :class="{ active: tipoDesconto === 'valor' }"
                            @click="emit('update:tipoDesconto', 'valor')"
                        >R$</button>
                        <button
                            type="button"
                            :class="{ active: tipoDesconto === 'percentual' }"
                            @click="emit('update:tipoDesconto', 'percentual')"
                        >%</button>
                    </div>
                </div>
                <div v-if="desconto > 0" class="rs-row rs-row-dim">
                    <span>Desconto aplicado</span>
                    <span>− {{ fmtBRL(descontoValor) }}</span>
                </div>
            </div>

            <div class="rs-divider"></div>

            <div class="rs-total">
                <span>Total</span>
                <strong>{{ fmtBRL(totalGeral) }}</strong>
            </div>

            <!-- Integridade formas de pagamento -->
            <div v-if="somaFormas > 0" class="rs-row">
                <span>Soma das formas</span>
                <span>{{ fmtBRL(somaFormas) }}</span>
            </div>
            <div
                v-if="somaFormas > 0"
                class="rs-integridade"
                :class="integridadeOk ? 'ok' : 'erro'"
            >
                <i :class="integridadeOk ? 'fa-solid fa-circle-check' : 'fa-solid fa-circle-exclamation'"></i>
                {{ integridadeOk ? "Soma confere." : `Falta ${fmtBRL(diferenca)}.` }}
            </div>
        </div>

        <div class="rs-acoes">
            <AppButton
                block
                icon="fa-solid fa-save"
                :loading="salvando"
                :disabled="!integridadeOk"
                :title="integridadeOk ? '' : 'Corrija a integridade das formas de pagamento'"
                @click="emit('salvar')"
            >Salvar</AppButton>
        </div>
    </div>
</template>

<style scoped>
.resumo-sidebar {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 0;
}

.rs-titulo {
    font-size: 13px;
    font-weight: 600;
    color: hsl(var(--secondary));
    padding-bottom: 12px;
    margin-bottom: 12px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}

.rs-linhas { display: flex; flex-direction: column; gap: 10px; }

.rs-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 13px;
    color: hsl(var(--secondary));
}
.rs-row-dim { color: hsl(var(--secondary) / 0.6); font-size: 12px; }

.rs-discount { display: flex; flex-direction: column; gap: 6px; }
.rs-disc-label { font-size: 12px; font-weight: 600; color: hsl(var(--secondary)); }

.rs-disc-input {
    display: flex;
    align-items: center;
    border: 1px solid hsl(var(--secondary) / 0.15);
    border-radius: 8px;
    background: hsl(var(--card));
    padding-right: 4px;
}
.rs-disc-input input {
    border: none;
    padding: 7px 10px;
    font-size: 13px;
    font-family: inherit;
    flex: 1;
    min-width: 0;
    background: transparent;
    color: hsl(var(--secondary));
}
.rs-disc-input input:focus { outline: none; }

.rs-disc-toggle {
    display: flex;
    gap: 2px;
    padding: 2px;
    background: hsl(var(--secondary) / 0.05);
    border-radius: 6px;
}
.rs-disc-toggle button {
    border: none;
    background: transparent;
    padding: 4px 9px;
    font-size: 11.5px;
    font-weight: 600;
    cursor: pointer;
    color: hsl(var(--secondary) / 0.6);
    border-radius: 4px;
    font-family: inherit;
}
.rs-disc-toggle button.active {
    background: hsl(var(--card));
    color: hsl(var(--primary));
}

.rs-divider {
    height: 1px;
    background: hsl(var(--secondary) / 0.08);
    margin: 4px 0;
}

.rs-total {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    padding-top: 4px;
}
.rs-total span { font-size: 13px; color: hsl(var(--secondary) / 0.7); }
.rs-total strong { font-size: 22px; font-weight: 700; color: hsl(var(--primary)); }

.rs-integridade {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 11.5px;
    font-weight: 600;
    padding: 7px 10px;
    border-radius: 6px;
}
.rs-integridade.ok   { background: hsl(var(--success) / 0.1); color: hsl(160 79% 28%); }
.rs-integridade.erro { background: hsl(var(--warning) / 0.1); color: hsl(40 90% 33%); }

.rs-acoes { margin-top: 16px; }
</style>
