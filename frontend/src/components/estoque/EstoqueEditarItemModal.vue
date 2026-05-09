<script setup lang="ts">
import { ref, watch } from "vue"
import { AppModal, AppField, AppInput, AppButton } from "@/components/ui"
import type { ItemInventario } from "@/services/inventarioService"

const props = defineProps<{
    aberto: boolean
    item: ItemInventario | null
    categorias: string[]
}>()

const emit = defineEmits<{
    fechar: []
    confirmar: [payload: { nome: string; categoria: string; unidadeMedida: string; quantidadeMinima: number }]
}>()

const form = ref({ nome: "", categoria: "", unidadeMedida: "", quantidadeMinima: 0 })
const erro = ref<string | null>(null)
const salvando = ref(false)

watch(() => props.item, (item) => {
    if (item) {
        form.value = { nome: item.nome, categoria: item.categoria, unidadeMedida: item.unidadeMedida, quantidadeMinima: item.quantidadeMinima }
        erro.value = null
    }
}, { immediate: true })

async function confirmar() {
    erro.value = null
    if (!form.value.nome.trim()) { erro.value = "Nome é obrigatório."; return }
    salvando.value = true
    try {
        emit("confirmar", { ...form.value })
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        :titulo="`Editar — ${item?.nome ?? ''}`"
        @fechar="emit('fechar')"
    >
        <div class="form-grid">
            <AppField label="Nome" required class="full">
                <AppInput v-model="form.nome" />
            </AppField>
            <AppField label="Categoria" required>
                <AppInput v-model="form.categoria" list="cats-editar" />
                <datalist id="cats-editar">
                    <option v-for="c in categorias" :key="c" :value="c" />
                </datalist>
            </AppField>
            <AppField label="Unidade de medida" required>
                <AppInput v-model="form.unidadeMedida" />
            </AppField>
            <AppField label="Quantidade mínima" required>
                <AppInput v-model="form.quantidadeMinima" type="number" :min="0" :step="0.001" />
            </AppField>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton icon="fa-solid fa-check" :loading="salvando" :disabled="salvando" @click="confirmar">
                Salvar
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}
.form-grid .full { grid-column: 1 / -1; }
.msg-erro { color: hsl(var(--error)); font-size: 13px; }
</style>
