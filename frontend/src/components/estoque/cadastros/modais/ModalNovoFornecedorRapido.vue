<script setup lang="ts">
/**
 * Cadastro rápido de fornecedor — atalho "+ Novo" do drawer "Novo produto".
 * Captura razão social + nome fantasia + CNPJ + prazo de entrega. Demais
 * campos (contato nome/telefone/email) ficam para edição completa pela aba
 * Fornecedores. Default de prazo = 5 dias (mesmo da listagem padrão).
 *
 * CNPJ é opcional; quando preenchido, valida via `validateCnpj` (front
 * espelha o `CnpjValidator` do domínio — backend é fonte da verdade).
 */
import { ref, watch, computed } from "vue"
import { AppModal, AppButton, AppField, AppInput } from "@/components/ui"
import {
    estoqueCadastrosService,
    type CadastroOpcao,
    type FornecedorPayload,
} from "@/services/estoqueCadastrosService"
import { apenasDigitos, validateCnpj } from "@/utils/validateCnpj"

const props = defineProps<{ aberto: boolean }>()
const emit  = defineEmits<{ criada: [opcao: CadastroOpcao]; fechar: [] }>()

const form = ref<FornecedorPayload>({
    razaoSocial: "",
    nomeFantasia: "",
    cnpj: "",
    prazoEntregaDias: 5,
})
const erro     = ref<string | null>(null)
const salvando = ref(false)

const cnpjValido = computed(() => {
    if (!form.value.cnpj) return true
    return validateCnpj(form.value.cnpj)
})

watch(() => props.aberto, (aberta) => {
    if (aberta) {
        form.value = { razaoSocial: "", nomeFantasia: "", cnpj: "", prazoEntregaDias: 5 }
        erro.value = null
    }
})

async function salvar() {
    erro.value = null
    const razaoSocial = form.value.razaoSocial.trim()
    if (!razaoSocial) { erro.value = "Razão social é obrigatória."; return }
    if (!cnpjValido.value) { erro.value = "CNPJ inválido."; return }
    if (form.value.prazoEntregaDias < 0) { erro.value = "Prazo não pode ser negativo."; return }

    salvando.value = true
    try {
        const cnpj = apenasDigitos(form.value.cnpj) || null
        const nomeFantasia = form.value.nomeFantasia?.trim() || null
        const { id } = await estoqueCadastrosService.fornecedores.criar({
            razaoSocial,
            nomeFantasia,
            cnpj,
            prazoEntregaDias: Number(form.value.prazoEntregaDias) || 0,
        })
        // Pré-seleção mostra a razão social (mesmo nome que o endpoint /opcoes devolve).
        emit("criada", { id, nome: razaoSocial })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar fornecedor."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Novo fornecedor"
        largura="md"
        :acima-de-drawer="true"
        @fechar="emit('fechar')"
    >
        <AppField label="Razão social" required>
            <AppInput v-model="form.razaoSocial" placeholder="Ex: Distribuidora Saúde Ltda" />
        </AppField>

        <AppField label="Nome fantasia">
            <AppInput v-model="form.nomeFantasia" placeholder="Como é conhecido (opcional)" />
        </AppField>

        <div class="grid-2">
            <AppField label="CNPJ" :hint="form.cnpj && !cnpjValido ? 'CNPJ inválido' : undefined">
                <AppInput v-model="form.cnpj" placeholder="00.000.000/0000-00" />
            </AppField>

            <AppField label="Prazo de entrega (dias)" required>
                <AppInput v-model="form.prazoEntregaDias" type="number" :min="0" :step="1" />
            </AppField>
        </div>

        <p class="ajuda">
            Outros campos (contato, telefone, e-mail) podem ser preenchidos depois pela aba
            <b>Fornecedores</b>.
        </p>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="emit('fechar')">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-plus"
                :loading="salvando"
                :disabled="salvando"
                @click="salvar"
            >Criar fornecedor</AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.grid-2 { display: grid; grid-template-columns: 1.4fr 1fr; gap: 12px; }
.ajuda {
    margin: 0;
    font-size: 12px;
    color: hsl(var(--secondary) / 0.6);
    line-height: 1.4;
}
.msg-erro { color: hsl(var(--error)); font-size: 13px; margin: 0; }
</style>
