<script setup lang="ts">
import type { AccordionTriggerProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { AccordionHeader, AccordionTrigger } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<AccordionTriggerProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <AccordionHeader class="flex">
    <AccordionTrigger
      v-bind="props"
      :class="cn(
        'flex flex-1 items-center justify-between py-4 text-sm font-medium transition-all hover:underline [&[data-state=open]>svg]:rotate-180',
        props.class,
      )"
    >
      <slot />
      <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round"
        class="h-4 w-4 shrink-0 text-muted-foreground transition-transform duration-200"
      >
        <path d="m6 9 6 6 6-6" />
      </svg>
    </AccordionTrigger>
  </AccordionHeader>
</template>
