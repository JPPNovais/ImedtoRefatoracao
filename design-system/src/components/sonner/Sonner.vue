<script setup lang="ts">
import { computed } from "vue"
import { Toaster as SonnerToaster } from "vue-sonner"

const props = withDefaults(defineProps<{
  position?: "top-left" | "top-right" | "bottom-left" | "bottom-right" | "top-center" | "bottom-center"
  expand?: boolean
  richColors?: boolean
  closeButton?: boolean
  duration?: number
  theme?: "light" | "dark" | "system"
  class?: string
}>(), {
  position: "bottom-right",
  expand: false,
  richColors: true,
  closeButton: false,
  duration: 4000,
  theme: "system",
})

const toasterClass = computed(() =>
  ["toaster group", props.class].filter(Boolean).join(" ")
)
</script>

<template>
  <SonnerToaster
    :class="toasterClass"
    :position="props.position"
    :expand="props.expand"
    :rich-colors="props.richColors"
    :close-button="props.closeButton"
    :duration="props.duration"
    :theme="props.theme"
    :toast-options="{
      classes: {
        toast: 'group toast group-[.toaster]:bg-background group-[.toaster]:text-foreground group-[.toaster]:border-border group-[.toaster]:shadow-lg',
        description: 'group-[.toast]:text-muted-foreground',
        actionButton: 'group-[.toast]:bg-primary group-[.toast]:text-primary-foreground',
        cancelButton: 'group-[.toast]:bg-muted group-[.toast]:text-muted-foreground',
      },
    }"
  />
</template>
