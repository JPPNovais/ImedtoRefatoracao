<script setup lang="ts">
import { ref } from "vue"
import { AppModal, AppField, AppTextarea, AppButton } from "@/components/ui"
import { useAssinaturasStore } from "../../stores/assinaturasStore"

const props = defineProps<{
    assinaturaId: string
    estabelecimentoId: number
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

async function confirmar() {
    if (motivo.value.trim().length < 10) {
        erro.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.encerrar(props.assinaturaId, props.estabelecimentoId, motivo.value.trim())
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível encerrar a assinatura."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal :aberto="true" titulo="Encerrar assinatura" @fechar="emit('fechar')">
        <p class="modal-desc">Esta ação encerrará a vigência atual. O histórico é preservado.</p>

        <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
            <AppTextarea
                v-model="motivo"
                :rows="3"
                placeholder="Motivo do encerramento..."
                :disabled="salvando"
            />
        </AppField>

        <p v-if="erro" class="campo-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                variant="danger"
                :loading="salvando"
                :disabled="salvando || motivo.trim().length < 10"
                @click="confirmar"
            >
                Encerrar
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-desc {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
    margin-bottom: 1rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin-top: 0.5rem;
}
</style>
