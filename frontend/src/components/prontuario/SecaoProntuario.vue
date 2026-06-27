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
import SecaoCondutaChecklist from "./secoes/SecaoCondutaChecklist.vue"
import SecaoEvolucaoPosOperatoria from "./secoes/SecaoEvolucaoPosOperatoria.vue"
import SecaoDescricaoCirurgica from "./secoes/SecaoDescricaoCirurgica.vue"
import SecaoAnexos from "./secoes/SecaoAnexos.vue"
import SecaoFotosPaciente from "./secoes/SecaoFotosPaciente.vue"

const props = defineProps<{
    chave: string
    titulo: string
    tipo: string            // "texto" | "texto_longo" | "estruturado" | ...
    modelValue: any         // string OU objeto estruturado
    readOnly?: boolean
    /** Sexo do paciente — usado pelo SecaoExameFisico para escolher a silhueta do mapa corporal. */
    pacienteSexo?: string | null
    /** Erro de validação do campo "cirurgião" — propagado para SecaoDescricaoCirurgica (CA20–CA22). */
    erroCirurgiao?: string | null
    /**
     * ID do paciente — necessário para SecaoAnexos e SecaoFotosPaciente fazerem
     * chamadas de upload/listagem/remoção. Opcional; sem ele, upload desabilitado.
     */
    pacienteId?: number | null
    /**
     * ID da evolução atual — presente quando há evolução já salva (modo imediato).
     * Ausente na consulta atual antes de salvar (modo pendente — upload diferido).
     */
    evolucaoId?: number | null
}>()
const emit = defineEmits<{
    "update:modelValue": [valor: any]
    /** Repassa emit das seções de arquivo (addendum upload diferido). */
    pendentes: [arquivos: File[]]
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

// Retrocompat: conduta legada (string não-vazia salva antes da F3B) — read-only sempre (CA73).
// Avaliado ANTES de ehCondutaChecklist para que evolução antiga não caia no checklist.
const ehCondutaLegado = computed(() =>
    props.chave === "conduta" && typeof props.modelValue === "string" && props.modelValue.length > 0
)

// CA73: qualquer seção com chave === "conduta" que não seja legado string é checklist.
// Cobre: modelo antigo persistido com tipo "texto_longo" + evolução nova (modelValue vazio
// ou objeto) — o tipo vindo do banco não determina mais o comportamento; a chave sim.
const ehCondutaChecklist = computed(() =>
    props.chave === "conduta" || props.tipo === "conduta_checklist"
)

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

    <SecaoEvolucaoPosOperatoria
        v-else-if="chave === 'evolucao-pos-op'"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <SecaoDescricaoCirurgica
        v-else-if="chave === 'desc-cirurgica'"
        v-model="valorEstrutura"
        :read-only="readOnly"
        :erro-cirurgiao="erroCirurgiao"
    />

    <!--
      Conduta legada (string não-vazia) — read-only sempre (CA73).
      DEVE vir ANTES do checklist: ehCondutaChecklist cobre toda chave='conduta',
      então avaliamos legado primeiro para preservar o texto salvo antes da F3B.
    -->
    <AppTextarea
        v-else-if="ehCondutaLegado"
        :model-value="valorTexto"
        :rows="4"
        :disabled="true"
        @update:model-value="() => {}"
    />

    <!-- Conduta checklist — qualquer caso restante com chave='conduta' (modelo antigo
         ou novo, evolução nova/vazia/objeto). Também cobre tipo conduta_checklist explícito. -->
    <SecaoCondutaChecklist
        v-else-if="ehCondutaChecklist"
        v-model="valorEstrutura"
        :read-only="readOnly"
    />

    <!-- Seção de anexos clínicos (PDF, imagens, Office) — briefing 2026-06-27_002 + addendum -->
    <SecaoAnexos
        v-else-if="chave === 'anexos'"
        v-model="valorEstrutura"
        :read-only="readOnly"
        :paciente-id="pacienteId"
        :evolucao-id="evolucaoId"
        @pendentes="(arqs) => emit('pendentes', arqs)"
    />

    <!-- Seção de fotos do paciente — briefing 2026-06-27_002 + addendum -->
    <SecaoFotosPaciente
        v-else-if="chave === 'fotos-paciente'"
        v-model="valorEstrutura"
        :read-only="readOnly"
        :paciente-id="pacienteId"
        :evolucao-id="evolucaoId"
        @pendentes="(arqs) => emit('pendentes', arqs)"
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
