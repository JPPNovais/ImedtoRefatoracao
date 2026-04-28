<script setup lang="ts">
import { Field } from "@imedto/ui"

defineProps<{
    label?:        string
    hint?:         string
    erro?:         string | null
    required?:     boolean
    for?:          string
    labelVariant?: "default" | "compact" | "dense"
}>()
</script>

<template>
    <Field
        :label="label"
        :description="hint"
        :error="erro ?? undefined"
        :required="required"
        :html-for="for"
    >
        <slot />
    </Field>
</template>
