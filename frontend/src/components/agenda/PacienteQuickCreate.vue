<script setup lang="ts">
import { ref, watch } from "vue"
import { vMaska } from "maska/vue"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"
import { AppButton, AppField, AppInput, AppModal } from "@/components/ui"

const props = defineProps<{
    aberto: boolean
    nomeInicial?: string
}>()

const emit = defineEmits<{
    fechar: []
    criado: [paciente: PacienteListaItem]
}>()

const nome     = ref("")
const telefone = ref("")
const cpf      = ref("")
const salvando = ref(false)
const erro     = ref<string | null>(null)

watch(() => props.aberto, (a) => {
    if (a) {
        nome.value     = props.nomeInicial ?? ""
        telefone.value = ""
        cpf.value      = ""
        erro.value     = null
    }
})

async function salvar() {
    if (!nome.value.trim()) return
    salvando.value = true
    erro.value = null
    try {
        await pacienteService.criar({
            nomeCompleto: nome.value.trim(),
            telefone: telefone.value || undefined,
            cpf: cpf.value || undefined,
        })

        const pg = await pacienteService.listar(nome.value.trim(), 1, 5)
        const criado = pg.itens.find(p => p.nomeCompleto === nome.value.trim())
            ?? pg.itens[0]

        if (criado) emit("criado", criado)
        emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao cadastrar paciente."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Cadastro rápido de paciente"
        largura="sm"
        @fechar="$emit('fechar')"
    >
        <AppField label="Nome completo" required>
            <AppInput v-model="nome" placeholder="Nome do paciente" :disabled="salvando" autofocus />
        </AppField>

        <AppField label="Telefone">
            <AppInput
                v-model="telefone"
                v-maska="'(##) #####-####'"
                type="tel"
                placeholder="(00) 00000-0000"
                :disabled="salvando"
            />
        </AppField>

        <AppField label="CPF">
            <AppInput
                v-model="cpf"
                v-maska="'###.###.###-##'"
                placeholder="000.000.000-00"
                :disabled="salvando"
            />
        </AppField>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p class="dica">Você pode completar os demais dados em <strong>Pacientes</strong> depois.</p>

        <template #rodape>
            <AppButton variant="secondary" :disabled="salvando" @click="$emit('fechar')">Cancelar</AppButton>
            <AppButton :disabled="salvando || !nome.trim()" :loading="salvando" @click="salvar">
                Cadastrar e selecionar
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.msg-erro { color: hsl(var(--error)); font-size: 0.875em; margin: 0; }
.dica     { font-size: 0.78em; color: var(--text-muted); margin: 0; }
</style>
