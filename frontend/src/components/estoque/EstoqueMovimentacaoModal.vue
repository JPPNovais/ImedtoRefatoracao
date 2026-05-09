<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { AppModal, AppField, AppInput, AppSelect, AppButton, AppTextarea } from "@/components/ui"
import type { ItemInventario } from "@/services/inventarioService"
import { formatarMoedaBrl } from "@/utils/format"

const props = defineProps<{
    aberto: boolean
    itemPreSelecionado?: ItemInventario | null
    tipoInicial?: "Entrada" | "Saida"
}>()

const emit = defineEmits<{
    fechar: []
    confirmar: [payload: {
        itemInventarioId: number
        tipo: "Entrada" | "Saida"
        quantidade: number
        custoUnitario?: number
        observacao?: string | null
    }]
}>()

const tipo = ref<"Entrada" | "Saida">(props.tipoInicial ?? "Entrada")
const quantidade = ref<number>(1)
const custoUnitario = ref<number>(0)
const observacao = ref("")
const erro = ref<string | null>(null)
const salvando = ref(false)

// Sincroniza tipo quando prop muda
watch(() => props.tipoInicial, (v) => { if (v) tipo.value = v })
watch(() => props.aberto, (v) => {
    if (v) {
        tipo.value = props.tipoInicial ?? "Entrada"
        quantidade.value = 1
        custoUnitario.value = 0
        observacao.value = ""
        erro.value = null
    }
})

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}

async function confirmar() {
    erro.value = null
    if (!props.itemPreSelecionado) {
        erro.value = "Selecione um item para a movimentação."
        return
    }
    if (quantidade.value <= 0) {
        erro.value = "Quantidade deve ser maior que zero."
        return
    }
    if (tipo.value === "Entrada" && custoUnitario.value <= 0) {
        erro.value = "Custo unitário deve ser maior que zero para entradas."
        return
    }
    salvando.value = true
    try {
        emit("confirmar", {
            itemInventarioId: props.itemPreSelecionado.id,
            tipo: tipo.value,
            quantidade: quantidade.value,
            custoUnitario: tipo.value === "Entrada" ? custoUnitario.value : undefined,
            observacao: observacao.value || null,
        })
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Registrar movimentação"
        @fechar="emit('fechar')"
    >
        <!-- Seletor de tipo -->
        <div class="tipo-selector">
            <button
                v-for="opt in [{ valor: 'Entrada', label: 'Entrada', icone: 'fa-solid fa-arrow-down-to-bracket', cor: 'hsl(160 79% 39%)' }, { valor: 'Saida', label: 'Saída', icone: 'fa-solid fa-arrow-up-from-bracket', cor: 'hsl(0 70% 45%)' }]"
                :key="opt.valor"
                type="button"
                class="tipo-btn"
                :class="{ ativo: tipo === opt.valor }"
                :style="tipo === opt.valor ? { background: opt.cor, borderColor: opt.cor } : {}"
                @click="tipo = opt.valor as 'Entrada' | 'Saida'"
            >
                <i :class="opt.icone"></i>
                {{ opt.label }}
            </button>
        </div>

        <!-- Info do item selecionado -->
        <div v-if="itemPreSelecionado" class="item-info">
            <i class="fa-solid fa-pills item-icone"></i>
            <div>
                <b>{{ itemPreSelecionado.nome }}</b>
                <span>
                    {{ itemPreSelecionado.codigo }} · estoque atual:
                    <strong>{{ formatarQtd(itemPreSelecionado.quantidadeAtual) }} {{ itemPreSelecionado.unidadeMedida }}</strong>
                    · custo médio: <strong>{{ formatarMoedaBrl(itemPreSelecionado.custoMedio) }}</strong>
                </span>
            </div>
        </div>

        <!-- Campos -->
        <div class="form-grid">
            <AppField label="Quantidade" required>
                <AppInput
                    v-model="quantidade"
                    type="number"
                    :min="0.001"
                    :step="0.001"
                    placeholder="0"
                />
            </AppField>

            <AppField
                v-if="tipo === 'Entrada'"
                label="Custo unitário (R$)"
                required
                :erro="erro?.includes('Custo') ? erro : null"
            >
                <AppInput
                    v-model="custoUnitario"
                    type="number"
                    :min="0.01"
                    :step="0.01"
                    placeholder="0,00"
                />
            </AppField>

            <p v-else class="hint">
                O custo unitário da saída será registrado como o custo médio atual do item.
            </p>

            <AppField label="Observação" class="full">
                <AppTextarea
                    v-model="observacao"
                    placeholder="Motivo, fornecedor, nota fiscal..."
                    :rows="2"
                />
            </AppField>
        </div>

        <p v-if="erro && !erro.includes('Custo')" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                :loading="salvando"
                :disabled="salvando"
                :icon="tipo === 'Entrada' ? 'fa-solid fa-arrow-down-to-bracket' : 'fa-solid fa-arrow-up-from-bracket'"
                @click="confirmar"
            >
                Confirmar {{ tipo === "Entrada" ? "entrada" : "saída" }}
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.tipo-selector {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 8px;
    margin-bottom: 4px;
}

.tipo-btn {
    background: var(--bg-card);
    border: 2px solid hsl(var(--secondary) / 0.1);
    border-radius: var(--radius-lg);
    padding: 14px 10px;
    cursor: pointer;
    transition: all 150ms;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    font-weight: 700;
    color: hsl(var(--secondary) / 0.7);
    font-family: inherit;
}
.tipo-btn i { font-size: 18px; }
.tipo-btn:hover { border-color: hsl(var(--primary) / 0.3); }
.tipo-btn.ativo { color: white; border-color: transparent; }

.item-info {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    padding: 12px 14px;
    background: hsl(var(--secondary) / 0.04);
    border-radius: var(--radius-lg);
    border: 1px solid hsl(var(--secondary) / 0.08);
}
.item-icone {
    color: hsl(var(--primary));
    font-size: 16px;
    margin-top: 2px;
    flex-shrink: 0;
}
.item-info b { display: block; font-weight: 700; color: hsl(var(--primary-dark)); font-size: 14px; }
.item-info span { display: block; font-size: 12px; color: hsl(var(--secondary) / 0.65); margin-top: 2px; }

.form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}
.form-grid .full { grid-column: 1 / -1; }

.hint {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.65);
    padding: 8px 0;
    grid-column: 2;
    align-self: center;
}

.msg-erro {
    color: hsl(var(--error));
    font-size: 13px;
}
</style>
