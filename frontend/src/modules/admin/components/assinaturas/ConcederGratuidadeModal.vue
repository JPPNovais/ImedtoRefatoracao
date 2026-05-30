<script setup lang="ts">
import { ref, computed } from "vue"
import { AppModal, AppField, AppInput, AppTextarea, AppButton } from "@/components/ui"
import { useAssinaturasStore } from "../../stores/assinaturasStore"

const props = defineProps<{
    estabelecimentoId: number
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const gratuidadeMotivo = ref("")
const fimEm = ref("")
const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

// Validação frontend: ≥ 20 chars (espelho da regra de backend).
const gratuidadeMotivoValido = computed(() => gratuidadeMotivo.value.trim().length >= 20)

async function salvar() {
    if (!gratuidadeMotivoValido.value) {
        erro.value = "Motivo da gratuidade deve ter pelo menos 20 caracteres."
        return
    }
    if (motivo.value.trim().length < 10) {
        erro.value = "Motivo administrativo é obrigatório (mín. 10 caracteres)."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.concederGratuidade(props.estabelecimentoId, {
            gratuidadeMotivo: gratuidadeMotivo.value.trim(),
            fimEm: fimEm.value ? new Date(fimEm.value).toISOString() : null,
            motivo: motivo.value.trim(),
        })
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível conceder gratuidade."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal :aberto="true" titulo="Conceder Gratuidade Vitalícia" @fechar="emit('fechar')">
        <div class="form-campos">
            <AppField
                label="Motivo da gratuidade"
                required
                :hint="gratuidadeMotivo.trim().length > 0 && !gratuidadeMotivoValido
                    ? 'Mínimo de 20 caracteres.'
                    : `${gratuidadeMotivo.trim().length}/20 mín.`"
            >
                <AppTextarea
                    v-model="gratuidadeMotivo"
                    :rows="3"
                    placeholder="Ex: Parceiro estratégico beta tester (mínimo 20 caracteres)"
                    :disabled="salvando"
                />
            </AppField>

            <AppField label="Data de fim da gratuidade (opcional)" hint="Deixe vazio para gratuidade vitalícia sem expiração.">
                <AppInput v-model="fimEm" type="date" :disabled="salvando" />
            </AppField>

            <AppField label="Motivo administrativo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivo"
                    :rows="2"
                    placeholder="Motivo para registro de auditoria..."
                    :disabled="salvando"
                />
            </AppField>

            <p v-if="erro" class="campo-erro">{{ erro }}</p>
        </div>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                variant="success"
                :loading="salvando"
                :disabled="salvando || !gratuidadeMotivoValido || motivo.trim().length < 10"
                @click="salvar"
            >
                Conceder gratuidade
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0;
}
</style>
