<script setup lang="ts">
import type { AccordionContentProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { AccordionContent } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<AccordionContentProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <AccordionContent
    v-bind="props"
    :class="cn(
      'overflow-hidden text-sm data-[state=closed]:animate-accordion-up data-[state=open]:animate-accordion-down',
      props.class,
    )"
  >
    <div class="pb-4 pt-0">
      <slot />
    </div>
  </AccordionContent>
</template>
