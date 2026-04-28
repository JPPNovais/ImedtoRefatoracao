<!--
  Dispatcher de seções do prontuário. Renderiza o componente adequado
  baseado na `chave` da seção, caindo para textarea genérico se não houver
  componente estruturado correspondente. Alinhado com o legado.
-->
<script setup lang="ts">
import { computed } from "vue"
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
        v-if="chave === 'historia_pregressa'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoHistoriaFamiliar
        v-else-if="chave === 'historia_familiar'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoHistoriaSocial
        v-else-if="chave === 'historia_social'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoExameFisico
        v-else-if="chave === 'exame_fisico'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoExamesRealizados
        v-else-if="chave === 'exames_realizados'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoProcedimentosIndicados
        v-else-if="chave === 'procedimentos_indicados'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <!-- Fallback: texto_longo → textarea, texto → input -->
    <textarea
        v-else-if="tipo === 'texto_longo'"
        v-model="valorTexto"
        rows="4"
        class="input-field"
        :placeholder="`Descreva ${titulo.toLowerCase()}...`"
        :disabled="readOnly"
    ></textarea>
    <input
        v-else
        v-model="valorTexto"
        type="text"
        class="input-field"
        :placeholder="titulo"
        :disabled="readOnly"
    />
</template>

<style scoped>
.input-field {
    padding: 0.55rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text);
    width: 100%; box-sizing: border-box; resize: vertical;
}
.input-field:focus { outline: none; border-color: var(--primary); }
</style>
