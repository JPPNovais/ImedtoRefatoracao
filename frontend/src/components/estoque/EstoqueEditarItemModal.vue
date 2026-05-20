<script setup lang="ts">
import { ref, watch } from "vue"
import { AppModal, AppField, AppInput, AppSelect, AppButton } from "@/components/ui"
import type { ItemInventario } from "@/services/inventarioService"
import type { CategoriaEstoque } from "@/services/estoqueCadastrosService"

const props = defineProps<{
    aberto: boolean
    item: ItemInventario | null
    categorias: CategoriaEstoque[]
}>()

const emit = defineEmits<{
    fechar: []
    confirmar: [payload: { nome: string; categoriaId: number; unidadeMedida: string; quantidadeMinima: number }]
}>()

const form = ref({ nome: "", categoriaId: 0, unidadeMedida: "", quantidadeMinima: 0 })
const erro = ref<string | null>(null)
const salvando = ref(false)

watch(() => props.item, (item) => {
    if (item) {
        form.value = {
            nome: item.nome,
            categoriaId: item.categoriaId ?? 0,
            unidadeMedida: item.unidadeMedida,
            quantidadeMinima: item.quantidadeMinima,
        }
        erro.value = null
    }
}, { immediate: true })

async function confirmar() {
    erro.value = null
    if (!form.value.nome.trim()) { erro.value = "Nome é obrigatório."; return }
    if (!form.value.categoriaId) { erro.value = "Selecione uma categoria."; return }
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
                <AppSelect v-model="form.categoriaId">
                    <option :value="0" disabled>Selecione</option>
                    <option
                        v-for="c in categorias.filter(c => c.ativo || c.id === form.categoriaId)"
                        :key="c.id"
                        :value="c.id"
                    >
                        {{ c.nome }}
                    </option>
                </AppSelect>
            </AppField>
            <AppField label="Unidade de medida" required>
                <AppInput v-model="form.unidadeMedida" />
            </AppField>
            <AppField label="Quantidade mínima" required>
                <AppInput v-model="form.quantidadeMinima" type="number" :min="0" :step="1" />
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
