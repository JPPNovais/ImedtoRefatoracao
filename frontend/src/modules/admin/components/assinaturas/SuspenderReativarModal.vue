<script setup lang="ts">
/**
 * Modal reutilizável para Suspender ou Reativar assinatura.
 * O modo é determinado pela prop `acao`.
 */
import { ref } from "vue"
import { AppModal, AppField, AppTextarea, AppButton } from "@/components/ui"
import { useAssinaturasStore } from "../../stores/assinaturasStore"

const props = defineProps<{
    estabelecimentoId: number
    /** "suspender" ou "reativar" */
    acao: "suspender" | "reativar"
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

const isSuspender = props.acao === "suspender"

async function confirmar() {
    if (motivo.value.trim().length < 5) {
        erro.value = "Motivo deve ter ao menos 5 caracteres."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        if (isSuspender) {
            await store.suspender(props.estabelecimentoId, motivo.value.trim())
        } else {
            await store.reativar(props.estabelecimentoId, motivo.value.trim())
        }
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? `Não foi possível ${isSuspender ? "suspender" : "reativar"} a assinatura.`
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="true"
        :titulo="isSuspender ? 'Suspender assinatura' : 'Reativar assinatura'"
        @fechar="emit('fechar')"
    >
        <p class="modal-desc">
            <template v-if="isSuspender">
                Suspende o acesso imediatamente. A vigência é mantida — para reverter use Reativar.
            </template>
            <template v-else>
                Remove a suspensão e restaura o acesso conforme a vigência atual.
            </template>
        </p>

        <AppField label="Motivo" required>
            <AppTextarea
                v-model="motivo"
                :rows="2"
                :placeholder="isSuspender ? 'Ex: inadimplência, violação de uso...' : 'Ex: pagamento regularizado...'"
                :disabled="salvando"
            />
        </AppField>

        <p v-if="erro" class="campo-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                :variant="isSuspender ? 'danger' : 'primary'"
                :loading="salvando"
                :disabled="salvando || motivo.trim().length < 5"
                @click="confirmar"
            >
                {{ isSuspender ? "Suspender" : "Reativar" }}
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-desc {
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
    margin-bottom: 1rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: var(--text-sm);
    margin-top: 0.5rem;
}
</style>
