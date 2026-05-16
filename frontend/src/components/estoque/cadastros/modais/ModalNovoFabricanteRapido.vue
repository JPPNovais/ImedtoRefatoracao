<script setup lang="ts">
/**
 * Cadastro rápido de fabricante — atalho "+ Novo" do drawer "Novo produto".
 * Mantém só nome + país (default "Brasil"). País é opcional no domínio.
 */
import { ref, watch } from "vue"
import { AppModal, AppButton, AppField, AppInput } from "@/components/ui"
import {
    estoqueCadastrosService,
    type CadastroOpcao,
    type FabricantePayload,
} from "@/services/estoqueCadastrosService"

const props = defineProps<{ aberto: boolean }>()
const emit  = defineEmits<{ criada: [opcao: CadastroOpcao]; fechar: [] }>()

const form = ref<FabricantePayload>({ nome: "", pais: "Brasil" })
const erro     = ref<string | null>(null)
const salvando = ref(false)

watch(() => props.aberto, (aberta) => {
    if (aberta) {
        form.value = { nome: "", pais: "Brasil" }
        erro.value = null
    }
})

async function salvar() {
    erro.value = null
    const nome = form.value.nome.trim()
    if (!nome) { erro.value = "Nome é obrigatório."; return }

    salvando.value = true
    try {
        const pais = form.value.pais?.trim() || null
        const { id } = await estoqueCadastrosService.fabricantes.criar({ nome, pais })
        emit("criada", { id, nome })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar fabricante."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Novo fabricante"
        largura="sm"
        :acima-de-drawer="true"
        @fechar="emit('fechar')"
    >
        <AppField label="Nome" required>
            <AppInput v-model="form.nome" placeholder="Ex: EMS, Pfizer" />
        </AppField>

        <AppField label="País">
            <AppInput v-model="form.pais" placeholder="Brasil" />
        </AppField>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-plus"
                :loading="salvando"
                :disabled="salvando"
                @click="salvar"
            >Criar fabricante</AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 0; }
</style>
