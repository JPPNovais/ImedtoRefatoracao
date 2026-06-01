<script setup lang="ts">
import { computed } from "vue"
import AppBadge from "@/components/ui/AppBadge.vue"
import type { StatusAssinaturaDigital } from "@/services/assinaturaDigitalService"

const props = defineProps<{
    status: StatusAssinaturaDigital
}>()

type Variant = "default" | "success" | "warning" | "error" | "info" | "muted"

const config = computed<{ label: string; variant: Variant }>(() => {
    switch (props.status) {
        case "NaoAssinada":      return { label: "Não assinada",  variant: "muted" }
        case "AssinaturaPendente": return { label: "Pendente",    variant: "warning" }
        case "AssinadaIcp":      return { label: "Assinada",      variant: "success" }
        case "FalhaAssinatura":  return { label: "Falha",         variant: "error" }
        case "AssinaturaExpirada": return { label: "Expirada",    variant: "muted" }
        case "AssinadaMemed":    return { label: "Assinada (Memed)", variant: "success" }
        default:                 return { label: props.status,    variant: "muted" }
    }
})
</script>

<template>
    <AppBadge :variant="config.variant" :label="config.label" />
</template>
