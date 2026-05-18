<!--
  Dispatcher de seções do prontuário. Renderiza o componente adequado
  baseado na `chave` da seção, caindo para textarea genérico se não houver
  componente estruturado correspondente. Alinhado com o legado.
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppInput, AppTextarea } from "@/components/ui"
import SecaoHistoriaPregressa    from "./secoes/SecaoHistoriaPregressa.vue"
import SecaoHistoriaFamiliar     from "./secoes/SecaoHistoriaFamiliar.vue"
import SecaoHistoriaSocial       from "./secoes/SecaoHistoriaSocial.vue"
import SecaoExameFisico          from "./secoes/SecaoExameFisico.vue"
import SecaoExamesRealizados     from "./secoes/SecaoExamesRealizados.vue"
import SecaoProcedimentosIndicados from "./secoes/SecaoProcedimentosIndicados.vue"

const props = defineProps<{
    chave: string
    titulo: string
    tipo: string            // "texto" | "texto_longo" | "estruturado" | ...
    modelValue: any         // string OU objeto estruturado
    readOnly?: boolean
    /** Sexo do paciente — usado pelo SecaoExameFisico para escolher a silhueta do mapa corporal. */
    pacienteSexo?: string | null
}>()
const emit = defineEmits<{
    "update:modelValue": [valor: any]
}>()

function atualizar(v: any) { emit("update:modelValue", v) }

// Para seções estruturadas, gerenciamos um objeto; se ainda for string/vazio,
// inicializamos como objeto vazio para os componentes filhos não quebrarem.
const valorEstrutura = computed({
    get() {
        const v = props.modelValue
        if (v && typeof v === "object") return v
        return {}
    },
    set: atualizar,
})

const valorTexto = computed({
    get() {
        return typeof props.modelValue === "string" ? props.modelValue : ""
    },
    set: atualizar,
})
</script>

<template>
    <!-- Seções estruturadas — cada componente tem layout e campos próprios -->
    <SecaoHistoriaPregressa
        v-if="chave === 'hpp'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoHistoriaFamiliar
        v-else-if="chave === 'h-familiar'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoHistoriaSocial
        v-else-if="chave === 'h-social'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoExameFisico
        v-else-if="chave === 'exame-fisico'"
        v-model="valorEstrutura"
        :read-only="readOnly"
        :paciente-sexo="pacienteSexo"
    />

    <SecaoExamesRealizados
        v-else-if="chave === 'exames-realizados'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoProcedimentosIndicados
        v-else-if="chave === 'procedimentos-indicados'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <!-- Fallback: texto_longo → textarea, texto → input -->
    <AppTextarea
        v-else-if="tipo === 'texto_longo'"
        v-model="valorTexto"
        :rows="4"
        :placeholder="`Descreva ${titulo.toLowerCase()}...`"
        :disabled="readOnly"
    />
    <AppInput
        v-else
        v-model="valorTexto"
        :placeholder="titulo"
        :disabled="readOnly"
    />
</template>
