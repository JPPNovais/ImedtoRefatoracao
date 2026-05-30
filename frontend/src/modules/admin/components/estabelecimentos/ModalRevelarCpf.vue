<script setup lang="ts">
/**
 * ModalRevelarCpf — modal que pede motivo antes de revelar o CPF do dono.
 * W3-CA12: usa AppModal + AppField + AppTextarea + AppButton do DS.
 * CA17–CA19: motivo obrigatório, audit gerado no backend, resultado exibido inline.
 */
import { ref, computed } from "vue"
import { AppModal, AppField, AppTextarea, AppButton } from "@/components/ui"

const props = defineProps<{
    estabelecimentoId: number
    open: boolean
}>()

const emit = defineEmits<{
    close: []
    revelado: [cpf: string]
}>()

const motivo = ref("")
const carregando = ref(false)
const erro = ref<string | null>(null)

const motivoValido = computed(() => motivo.value.trim().length >= 10)

function fechar() {
    motivo.value = ""
    erro.value = null
    emit("close")
}

async function confirmar() {
    if (!motivoValido.value) {
        erro.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    emit("revelado", motivo.value.trim())
    fechar()
}
</script>

<template>
    <AppModal :aberto="open" titulo="Revelar CPF do dono" @fechar="fechar">
        <p class="modal-desc">
            Esta ação fica registrada em audit. Informe o motivo da consulta.
        </p>

        <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
            <AppTextarea
                v-model="motivo"
                :rows="3"
                placeholder="Ex: Validação de cadastro do parceiro (mín. 10 caracteres)"
                :disabled="carregando"
            />
        </AppField>

        <p v-if="erro" class="campo-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="carregando" @click="fechar">Cancelar</AppButton>
            <AppButton
                :loading="carregando"
                :disabled="!motivoValido"
                @click="confirmar"
            >
                Revelar CPF
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-desc {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
    margin-bottom: 0.25rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin-top: 0.25rem;
}
</style>
