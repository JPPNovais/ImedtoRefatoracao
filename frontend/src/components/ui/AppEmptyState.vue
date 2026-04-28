<script setup lang="ts">
import { EmptyState } from "@imedto/ui"

const props = defineProps<{
    icone?:     string
    titulo?:    string
    descricao?: string
    compacto?:  boolean
}>()
</script>

<template>
    <EmptyState
        :icon="icone"
        :title="titulo"
        :description="descricao"
        :compact="compacto"
    >
        <slot name="acao" />
    </EmptyState>
</template>
