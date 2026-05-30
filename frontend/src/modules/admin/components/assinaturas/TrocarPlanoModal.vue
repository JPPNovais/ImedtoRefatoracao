<script setup lang="ts">
import { ref, computed } from "vue"
import { AppModal, AppField, AppInput, AppTextarea, AppButton } from "@/components/ui"
import { useAssinaturasStore } from "../../stores/assinaturasStore"
import type { PlanoAdminDto } from "../../services/planosService"

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
const inicio = ref(new Date().toISOString().slice(0, 10))
const fimEm = ref("")
const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

const planosOpcoes = computed(() =>
    props.planos.map(p => ({ value: p.id, label: p.nome }))
)

async function salvar() {
    if (!planoId.value) {
        erro.value = "Selecione um plano."
        return
    }
    if (motivo.value.trim().length < 10) {
        erro.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.trocarPlano(props.estabelecimentoId, {
            planoId: planoId.value,
            inicio: new Date(inicio.value).toISOString(),
            fimEm: fimEm.value ? new Date(fimEm.value).toISOString() : null,
            motivo: motivo.value.trim(),
        })
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível trocar o plano."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal :aberto="true" titulo="Trocar plano" @fechar="emit('fechar')">
        <div class="form-campos">
            <AppField label="Plano" required>
                <select v-model="planoId" class="select-campo" :disabled="salvando">
                    <option value="">Selecione...</option>
                    <option v-for="p in planos" :key="p.id" :value="p.id">{{ p.nome }}</option>
                </select>
            </AppField>

            <AppField label="Data de início" required>
                <AppInput v-model="inicio" type="date" :disabled="salvando" />
            </AppField>

            <AppField label="Data de fim (opcional)">
                <AppInput v-model="fimEm" type="date" :disabled="salvando" />
            </AppField>

            <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivo"
                    :rows="2"
                    placeholder="Motivo da troca..."
                    :disabled="salvando"
                />
            </AppField>

            <p v-if="erro" class="campo-erro">{{ erro }}</p>
        </div>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                :loading="salvando"
                :disabled="salvando || motivo.trim().length < 10 || !planoId"
                @click="salvar"
            >
                Confirmar
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

.select-campo {
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    font-size: 0.875rem;
    font-family: inherit;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0;
}
</style>
