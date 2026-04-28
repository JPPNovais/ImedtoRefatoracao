<script setup lang="ts">
import { PageHeader } from "@imedto/ui"

defineProps<{
    titulo:     string
    subtitulo?: string
}>()
</script>

<template>
    <PageHeader :title="titulo" :subtitle="subtitulo">
        <template v-if="$slots.acoes" #actions>
            <slot name="acoes" />
        </template>
    </PageHeader>
</template>
