<script setup lang="ts">
/**
 * BadgeStatusEstabelecimento — usa AppStatusPill do DS.
 * W3-CA9: zero CSS custom, componente DS direto.
 */
import { computed } from "vue"
import { AppStatusPill } from "@/components/ui"

const props = defineProps<{ status: string }>()

const variante = computed((): "success" | "warning" | "error" | "muted" => {
    if (props.status === "Ativo") return "success"
    if (props.status === "Suspenso") return "warning"
    return "muted"
})
</script>

<template>
    <AppStatusPill :label="status" :variante="variante" />
</template>
