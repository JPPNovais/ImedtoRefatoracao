<script setup lang="ts">
/**
 * Input decimal com preenchimento estilo-moeda: os dígitos entram pela direita
 * e a vírgula se posiciona sozinha conforme o número de casas (`decimals`).
 * Ex.: decimals=1, digita 3→6→5  ⇒  0,3 → 3,6 → 36,5
 *
 * Exibe em pt-BR (vírgula); emite o valor com ponto decimal ("36.5") para
 * permanecer parseável no backend e nos cálculos (IMC etc.).
 */
import { ref, watch } from "vue"
import { Input } from "@imedto/ui"

const props = withDefaults(
    defineProps<{
        modelValue?:  string | number | null
        decimals?:    number
        placeholder?: string
        disabled?:    boolean
        class?:       string
    }>(),
    { decimals: 2 },
)

const emit = defineEmits<{ "update:modelValue": [string] }>()

const display = ref("")

function digitsParaDisplay(digits: string): string {
    const n = Number(digits || "0") / 10 ** props.decimals
    return n.toLocaleString("pt-BR", {
        minimumFractionDigits: props.decimals,
        maximumFractionDigits: props.decimals,
    })
}

function sincronizarDoModel() {
    const bruto = String(props.modelValue ?? "").replace(",", ".").trim()
    const n = parseFloat(bruto)
    if (bruto === "" || Number.isNaN(n)) {
        display.value = ""
        return
    }
    display.value = digitsParaDisplay(String(Math.round(n * 10 ** props.decimals)))
}

watch(() => props.modelValue, sincronizarDoModel, { immediate: true })

function aoDigitar(raw: string | number) {
    const digits = String(raw).replace(/\D/g, "").replace(/^0+(?=\d)/, "")
    if (digits === "") {
        display.value = ""
        emit("update:modelValue", "")
        return
    }
    display.value = digitsParaDisplay(digits)
    emit("update:modelValue", (Number(digits) / 10 ** props.decimals).toFixed(props.decimals))
}
</script>

<template>
    <Input
        :model-value="display"
        :placeholder="placeholder"
        :disabled="disabled"
        :class="props.class"
        inputmode="decimal"
        @update:model-value="aoDigitar"
    />
</template>
