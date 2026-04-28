<script setup lang="ts">
import { computed } from "vue"
import { CountBadge } from "@imedto/ui"

type Variant = "default" | "success" | "warning" | "error" | "info" | "muted"
type VariantInput = Variant | "primary"

const props = defineProps<{
    status?:  string
    variant?: VariantInput
    label?:   string
}>()

const mapeamento: Record<string, Variant> = {
    Agendado:   "default",
    Confirmado: "success",
    Cancelado:  "error",
    Concluido:  "muted",
    Pago:       "success",
    Pendente:   "warning",
    Vencido:    "error",
    Rascunho:   "muted",
    DRAFT:      "warning",
    Enviado:    "info",
    Aprovado:   "success",
    FINALIZED:  "success",
    Recusado:   "error",
    CANCELED:   "muted",
    Expirado:   "warning",
    Ativo:      "success",
    Inativo:    "muted",
    Suspenso:   "error",
    SIMPLES:    "info",
    CONTROLADA: "error",
}

const variant = computed<Variant>(() => {
    const v = props.variant
    if (v) return v === "primary" ? "default" : v
    if (props.status && mapeamento[props.status]) return mapeamento[props.status]
    return "muted"
})

const texto = computed(() => props.label ?? props.status ?? "")
</script>

<template>
    <CountBadge :variant="variant">{{ texto }}</CountBadge>
</template>
