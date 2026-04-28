<script setup lang="ts">
import { computed } from "vue"
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "@imedto/ui"

const props = withDefaults(defineProps<{
    title?:    string
    subtitle?: string
    padding?:  "none" | "sm" | "md" | "lg"
    elevated?: boolean
    flat?:     boolean
}>(), {
    padding:  "md",
    elevated: false,
    flat:     false,
})

const cardClass = computed(() => {
    const cls: string[] = []
    if (props.flat)     cls.push("border-0 shadow-none bg-muted/40")
    if (props.elevated) cls.push("shadow-md")
    return cls.join(" ")
})

const contentClass = computed(() => {
    switch (props.padding) {
        case "none": return "p-0"
        case "sm":   return "px-4 py-3"
        case "lg":   return "px-8 py-7"
        default:     return "px-6 py-5"
    }
})
</script>

<template>
    <Card :class="cardClass">
        <CardHeader v-if="title || $slots['header-aside']" class="flex-row items-start justify-between gap-4 space-y-0">
            <div>
                <CardTitle v-if="title">{{ title }}</CardTitle>
                <CardDescription v-if="subtitle">{{ subtitle }}</CardDescription>
            </div>
            <div v-if="$slots['header-aside']">
                <slot name="header-aside" />
            </div>
        </CardHeader>

        <CardContent :class="contentClass">
            <slot />
        </CardContent>

        <CardFooter v-if="$slots.footer" class="justify-end gap-2">
            <slot name="footer" />
        </CardFooter>
    </Card>
</template>
