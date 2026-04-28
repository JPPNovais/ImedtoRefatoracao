<script setup lang="ts">
import { computed } from "vue"
import { Button } from "@imedto/ui"

type Variant = "primary" | "secondary" | "danger" | "ghost" | "google" | "success"
type Size    = "sm" | "md" | "lg"
type NativeType = "button" | "submit" | "reset"

const props = withDefaults(defineProps<{
    variant?:   Variant
    size?:      Size
    type?:      NativeType
    loading?:   boolean
    disabled?:  boolean
    icon?:      string
    iconRight?: string
    block?:     boolean
}>(), {
    variant: "primary",
    size:    "md",
    type:    "button",
})

const dsVariant = computed(() => {
    switch (props.variant) {
        case "primary":   return "default"
        case "secondary": return "secondary"
        case "danger":    return "destructive"
        case "ghost":     return "ghost"
        default:          return "default"
    }
})

const dsSize = computed(() => {
    switch (props.size) {
        case "sm": return "sm"
        case "lg": return "lg"
        default:   return "default"
    }
})

const extraClass = computed(() => {
    const cls: string[] = []
    if (props.block) cls.push("w-full")
    if (props.variant === "success")
        cls.push("bg-success text-success-foreground hover:bg-success/90 shadow")
    if (props.variant === "google")
        cls.push("border border-border bg-card text-foreground hover:bg-muted shadow-sm")
    return cls.join(" ")
})
</script>

<template>
    <Button
        :variant="dsVariant"
        :size="dsSize"
        :type="type"
        :disabled="disabled || loading"
        :class="extraClass"
    >
        <svg v-if="loading" class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
        </svg>
        <i v-else-if="icon" :class="icon" aria-hidden="true" />
        <slot />
        <i v-if="iconRight && !loading" :class="iconRight" aria-hidden="true" />
    </Button>
</template>
