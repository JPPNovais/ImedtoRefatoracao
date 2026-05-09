<script setup lang="ts">
import { ref, watch } from "vue"
import { AppModal, AppField, AppInput, AppButton } from "@/components/ui"

const props = defineProps<{
    aberto: boolean
    categorias: string[]
}>()

const emit = defineEmits<{
    fechar: []
    confirmar: [payload: {
        codigo: string
        nome: string
        categoria: string
        unidadeMedida: string
        quantidadeInicial: number
        quantidadeMinima: number
        custoUnitarioInicial?: number
    }]
}>()

const form = ref({
    codigo: "",
    nome: "",
    categoria: "",
    unidadeMedida: "",
    quantidadeInicial: 0,
    quantidadeMinima: 0,
    custoUnitarioInicial: 0,
})
const erro = ref<string | null>(null)
const salvando = ref(false)

watch(() => props.aberto, (v) => {
    if (v) {
        form.value = { codigo: "", nome: "", categoria: "", unidadeMedida: "", quantidadeInicial: 0, quantidadeMinima: 0, custoUnitarioInicial: 0 }
        erro.value = null
    }
})

async function confirmar() {
    erro.value = null
    if (!form.value.codigo.trim()) { erro.value = "Código é obrigatório."; return }
    if (!form.value.nome.trim()) { erro.value = "Nome é obrigatório."; return }
    if (!form.value.categoria.trim()) { erro.value = "Categoria é obrigatória."; return }
    if (!form.value.unidadeMedida.trim()) { erro.value = "Unidade de medida é obrigatória."; return }
    if (form.value.quantidadeInicial > 0 && form.value.custoUnitarioInicial <= 0) {
        erro.value = "Custo unitário deve ser maior que zero quando há quantidade inicial."
        return
    }
    salvando.value = true
    try {
        const payload: Parameters<typeof emit>[1] = { ...form.value }
        if (form.value.quantidadeInicial <= 0) delete (payload as any).custoUnitarioInicial
        emit("confirmar", payload)
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Novo item de inventário" @fechar="emit('fechar')">
        <div class="form-grid">
            <AppField label="Código" required>
                <AppInput v-model="form.codigo" placeholder="Ex: MED-001" />
            </AppField>
            <AppField label="Unidade de medida" required>
                <AppInput v-model="form.unidadeMedida" placeholder="un, kg, L, ml..." />
            </AppField>
            <AppField label="Nome" required class="full">
                <AppInput v-model="form.nome" placeholder="Nome completo do produto" />
            </AppField>
            <AppField label="Categoria" required class="full">
                <AppInput v-model="form.categoria" list="cats-criar" placeholder="Ex: Medicamento, Insumo..." />
                <datalist id="cats-criar">
                    <option v-for="c in categorias" :key="c" :value="c" />
                </datalist>
            </AppField>
            <AppField label="Quantidade inicial">
                <AppInput v-model="form.quantidadeInicial" type="number" :min="0" :step="0.001" />
            </AppField>
            <AppField label="Quantidade mínima" required>
                <AppInput v-model="form.quantidadeMinima" type="number" :min="0" :step="0.001" />
            </AppField>
            <AppField
                v-if="form.quantidadeInicial > 0"
                label="Custo unitário inicial (R$)"
                required
                class="full"
                hint="Necessário para calcular o custo médio ponderado inicial."
                :erro="erro?.includes('Custo') ? erro : null"
            >
                <AppInput v-model="form.custoUnitarioInicial" type="number" :min="0.01" :step="0.01" />
            </AppField>
        </div>

        <p v-if="erro && !erro.includes('Custo')" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton icon="fa-solid fa-plus" :loading="salvando" :disabled="salvando" @click="confirmar">
                Criar item
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
