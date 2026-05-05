<script setup lang="ts">
/**
 * DocumentoPacienteField — campo unificado de documento do paciente.
 *
 * Permite alternar entre CPF (default, com mascara + validacao DV) e
 * Documento internacional (passaporte/RNE/etc, texto livre ate 30 chars).
 * Documento eh sempre OPCIONAL — backend aceita null.
 *
 * Emite v-model com `{ tipo: 'cpf' | 'internacional', valor: string }`.
 * O parent traduz para `cpf` ou `documentoInternacional` no payload do service.
 *
 * Reusado em: NovoAgendamentoModal, PacienteFormSidePanel, PacienteEditDrawer.
 */
import { computed, watch } from "vue"
import { vMaska } from "maska/vue"
import { cpfValido, somenteDigitos } from "@/utils/cpf"

export type TipoDocumento = "cpf" | "internacional"

export interface DocumentoPacienteValue {
    tipo: TipoDocumento
    valor: string
}

const props = defineProps<{
    modelValue: DocumentoPacienteValue
    /** Mensagem extra (ex: "Já cadastrado neste estabelecimento"). Quando set, sobrepoe a validacao local. */
    erroExterno?: string | null
    /** Lista de pacientes ja existentes no estabelecimento — usada para flagar duplicidade local. */
    pacientesExistentes?: Array<{ id: number; cpf: string | null; documentoInternacional: string | null }>
}>()

const emit = defineEmits<{
    "update:modelValue": [valor: DocumentoPacienteValue]
}>()

function setTipo(tipo: TipoDocumento) {
    if (props.modelValue.tipo === tipo) return
    emit("update:modelValue", { tipo, valor: "" })
}

function setValor(valor: string) {
    emit("update:modelValue", { tipo: props.modelValue.tipo, valor })
}

const ehCpf = computed(() => props.modelValue.tipo === "cpf")

/** Validacao inline — so dispara quando ha valor preenchido (campo eh opcional). */
const erroLocal = computed<string | null>(() => {
    const valor = props.modelValue.valor.trim()
    if (!valor) return null

    if (ehCpf.value) {
        const digitos = somenteDigitos(valor)
        if (digitos.length < 11) return null // ainda digitando
        if (!cpfValido(valor)) return "CPF inválido."
        const dup = props.pacientesExistentes?.some(p => p.cpf && somenteDigitos(p.cpf) === digitos)
        if (dup) return "Já existe um paciente com este CPF neste estabelecimento."
        return null
    }

    if (valor.length > 30) return "Documento internacional excede 30 caracteres."
    const dup = props.pacientesExistentes?.some(p => (p.documentoInternacional ?? "").trim() === valor)
    if (dup) return "Já existe um paciente com este documento neste estabelecimento."
    return null
})

const erroVisivel = computed(() => props.erroExterno || erroLocal.value)

/** Limpa o valor quando o usuario alterna o tipo, evitando guardar resto invalido. */
watch(() => props.modelValue.tipo, () => { /* o setTipo ja zera; este watch eh defensivo. */ })
</script>

<template>
    <div class="doc-field">
        <div class="doc-head">
            <label class="doc-label">
                Documento <span class="opt">opcional</span>
            </label>
            <div class="doc-tipo-toggle" role="radiogroup" aria-label="Tipo de documento">
                <button
                    type="button"
                    class="doc-tipo"
                    :class="{ ativo: ehCpf }"
                    role="radio"
                    :aria-checked="ehCpf"
                    @click="setTipo('cpf')"
                >CPF</button>
                <button
                    type="button"
                    class="doc-tipo"
                    :class="{ ativo: !ehCpf }"
                    role="radio"
                    :aria-checked="!ehCpf"
                    @click="setTipo('internacional')"
                >Internacional</button>
            </div>
        </div>

        <input
            v-if="ehCpf"
            type="text"
            placeholder="000.000.000-00"
            :value="modelValue.valor"
            v-maska="'###.###.###-##'"
            inputmode="numeric"
            :class="['doc-input', { erro: !!erroVisivel }]"
            @input="setValor(($event.target as HTMLInputElement).value)"
        />
        <input
            v-else
            type="text"
            placeholder="Ex: passaporte, RNE..."
            :value="modelValue.valor"
            maxlength="30"
            :class="['doc-input', { erro: !!erroVisivel }]"
            @input="setValor(($event.target as HTMLInputElement).value)"
        />

        <span v-if="erroVisivel" class="doc-erro">{{ erroVisivel }}</span>
    </div>
</template>

<style scoped>
.doc-field {
    display: flex;
    flex-direction: column;
    gap: 6px;
    min-width: 0;
}

.doc-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
}

.doc-label {
    font-size: 11px;
    font-weight: 600;
    color: hsl(var(--foreground) / 0.7);
}

.doc-label .opt {
    font-size: 10px;
    font-style: italic;
    font-weight: 400;
    color: hsl(var(--foreground) / 0.5);
    margin-left: 4px;
}

.doc-tipo-toggle {
    display: inline-flex;
    background: hsl(var(--foreground) / 0.05);
    border-radius: 8px;
    padding: 2px;
    gap: 2px;
}

.doc-tipo {
    border: 0;
    background: transparent;
    padding: 4px 10px;
    border-radius: 6px;
    font-size: 10px;
    font-weight: 600;
    color: hsl(var(--foreground) / 0.65);
    cursor: pointer;
    font-family: inherit;
    transition: background 0.12s, color 0.12s;
}
.doc-tipo:hover { color: hsl(var(--primary-dark, 254 56% 21%)); }
.doc-tipo.ativo {
    background: hsl(var(--card));
    color: hsl(var(--primary-dark, 254 56% 21%));
    box-shadow: 0 1px 2px hsl(0 0% 0% / 0.08);
}

.doc-input {
    height: 40px;
    padding: 0 12px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 10px;
    font-size: 13px;
    font-family: inherit;
    color: hsl(var(--foreground));
    background: hsl(var(--card));
    outline: none;
    transition: border 0.15s, box-shadow 0.15s;
}
.doc-input:focus {
    border-color: hsl(var(--primary, 254 56% 38%));
    box-shadow: 0 0 0 3px hsl(var(--primary, 254 56% 38%) / 0.12);
}
.doc-input.erro {
    border-color: hsl(0 84% 60%);
    box-shadow: 0 0 0 3px hsl(0 84% 60% / 0.12);
}

.doc-erro {
    font-size: 11px;
    color: hsl(0 84% 50%);
    font-weight: 500;
}
</style>
