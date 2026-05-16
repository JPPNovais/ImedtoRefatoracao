<script setup lang="ts">
/**
 * Cadastro rápido de local de estoque — atalho "+ Novo" do drawer "Novo
 * produto". Mantém nome + tipo (obrigatórios) + andar/setor + responsável
 * (opcionais). Default de tipo = "Armario" (mais comum em clínica).
 */
import { ref, watch } from "vue"
import { AppModal, AppButton, AppField, AppInput, AppSelect } from "@/components/ui"
import {
    estoqueCadastrosService,
    type CadastroOpcao,
    type LocalPayload,
    TIPOS_LOCAL_ESTOQUE,
} from "@/services/estoqueCadastrosService"

const props = defineProps<{ aberto: boolean }>()
const emit  = defineEmits<{ criada: [opcao: CadastroOpcao]; fechar: [] }>()

const form = ref<LocalPayload>({
    nome: "",
    tipo: "Armario",
    andarSetor: "",
    responsavel: "",
})
const erro     = ref<string | null>(null)
const salvando = ref(false)

watch(() => props.aberto, (aberta) => {
    if (aberta) {
        form.value = { nome: "", tipo: "Armario", andarSetor: "", responsavel: "" }
        erro.value = null
    }
})

async function salvar() {
    erro.value = null
    const nome = form.value.nome.trim()
    if (!nome) { erro.value = "Nome é obrigatório."; return }
    if (!form.value.tipo) { erro.value = "Tipo é obrigatório."; return }

    salvando.value = true
    try {
        const { id } = await estoqueCadastrosService.locais.criar({
            nome,
            tipo: form.value.tipo,
            andarSetor: form.value.andarSetor?.trim() || null,
            responsavel: form.value.responsavel?.trim() || null,
        })
        emit("criada", { id, nome })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar local."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Novo local"
        largura="sm"
        :acima-de-drawer="true"
        @fechar="emit('fechar')"
    >
        <AppField label="Nome" required>
            <AppInput v-model="form.nome" placeholder="Ex: Armário 1 - Recepção" />
        </AppField>

        <AppField label="Tipo" required>
            <AppSelect v-model="form.tipo">
                <option v-for="t in TIPOS_LOCAL_ESTOQUE" :key="t" :value="t">{{ t }}</option>
            </AppSelect>
        </AppField>

        <AppField label="Andar / setor">
            <AppInput v-model="form.andarSetor" placeholder="Ex: 2º andar, Sala 3" />
        </AppField>

        <AppField label="Responsável">
            <AppInput v-model="form.responsavel" placeholder="Quem cuida do local (opcional)" />
        </AppField>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-plus"
                :loading="salvando"
                :disabled="salvando"
                @click="salvar"
            >Criar local</AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 0; }
</style>
