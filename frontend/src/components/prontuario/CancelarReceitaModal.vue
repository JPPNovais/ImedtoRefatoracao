<script setup lang="ts">
import { ref, watch } from "vue"
import { AppButton, AppModal } from "@/components/ui"

const props = defineProps<{
    aberto: boolean
    receitaId: number | null
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "confirmar", motivo: string): void
}>()

const motivo = ref("")
const erro = ref<string | null>(null)

watch(() => props.aberto, (aberto) => {
    if (!aberto) {
        motivo.value = ""
        erro.value = null
    }
})

function confirmar() {
    if (!motivo.value.trim()) {
        erro.value = "O motivo é obrigatório."
        return
    }
    emit("confirmar", motivo.value.trim())
}

function fechar() {
    emit("fechar")
}
</script>

<template>
    <AppModal :aberto="aberto" largura="sm" @fechar="fechar">
        <template #titulo>
            <h2 class="modal-titulo">Cancelar receita</h2>
        </template>

        <p class="aviso">
            Esta ação não pode ser desfeita. A receita ficará registrada como cancelada no histórico do paciente.
        </p>

        <label class="campo">
            <span class="campo-label">Motivo do cancelamento <span class="obrigatorio">*</span></span>
            <textarea
                v-model="motivo"
                class="campo-input"
                rows="3"
                maxlength="500"
                placeholder="Descreva o motivo do cancelamento..."
                autofocus
            ></textarea>
            <span class="contador">{{ motivo.length }}/500</span>
        </label>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="ghost" @click="fechar">Voltar</AppButton>
            <AppButton
                variant="danger"
                icon="fa-solid fa-ban"
                :disabled="!motivo.trim()"
                @click="confirmar"
            >
                Cancelar receita
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-titulo {
    font-size: 18px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0;
}

.aviso {
    margin: 0;
    font-size: 13px;
    color: hsl(var(--foreground) / 0.7);
    line-height: 1.45;
}

.campo {
    display: flex;
    flex-direction: column;
    gap: 6px;
    position: relative;
}

.campo-label {
    font-size: 11px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.obrigatorio {
    color: hsl(var(--error));
}

.campo-input {
    width: 100%;
    border: 1px solid hsl(var(--foreground) / 0.15);
    border-radius: 8px;
    padding: 10px 12px;
    font-family: inherit;
    font-size: 13px;
    color: hsl(var(--foreground));
    background: hsl(var(--card));
    resize: vertical;
    min-height: 72px;
    box-sizing: border-box;
}

.campo-input:focus {
    outline: none;
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.12);
}

.contador {
    align-self: flex-end;
    font-size: 11px;
    color: hsl(var(--foreground) / 0.5);
}

.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px;
    padding: 8px 12px;
    font-size: 13px;
    margin: 0;
}
</style>
