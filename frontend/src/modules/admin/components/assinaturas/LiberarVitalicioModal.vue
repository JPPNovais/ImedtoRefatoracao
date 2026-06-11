<script setup lang="ts">
import { ref } from "vue"
import { AppModal, AppField, AppTextarea, AppButton } from "@/components/ui"
import type { PlanoAdminDto } from "../../services/planosService"
import { useAssinaturasStore } from "../../stores/assinaturasStore"

const props = defineProps<{
    estabelecimentoId: number
    planos: PlanoAdminDto[]
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const planoId = ref("")
const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

async function confirmar() {
    if (!planoId.value) {
        erro.value = "Selecione um plano."
        return
    }
    if (motivo.value.trim().length < 5) {
        erro.value = "Motivo deve ter ao menos 5 caracteres."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.liberarVitalicio(props.estabelecimentoId, {
            planoId: planoId.value,
            motivo: motivo.value.trim(),
        })
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível liberar acesso vitalício."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal :aberto="true" titulo="Liberar acesso vitalício" @fechar="emit('fechar')">
        <p class="modal-desc">Cria nova vigência sem data de expiração. Ideal para parceiros ou acordos permanentes.</p>

        <AppField label="Plano" required>
            <select v-model="planoId" class="form-select" :disabled="salvando">
                <option value="">— selecione —</option>
                <option v-for="p in planos" :key="p.id" :value="p.id">{{ p.nome }}</option>
            </select>
        </AppField>

        <AppField label="Motivo" required>
            <AppTextarea
                v-model="motivo"
                :rows="2"
                placeholder="Ex: parceiro estratégico beta..."
                :disabled="salvando"
            />
        </AppField>

        <p v-if="erro" class="campo-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                :loading="salvando"
                :disabled="salvando || !planoId || motivo.trim().length < 5"
                @click="confirmar"
            >
                Liberar vitalício
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

.form-select {
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    font-size: var(--text-sm);
    font-family: inherit;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: var(--text-sm);
    margin-top: 0.5rem;
}
</style>
