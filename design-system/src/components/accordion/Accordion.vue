<script setup lang="ts">
import type { AccordionRootEmits, AccordionRootProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { AccordionRoot, useForwardPropsEmits } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<AccordionRootProps & { class?: HTMLAttributes["class"] }>()
const emits = defineEmits<AccordionRootEmits>()

const forwarded = useForwardPropsEmits(props, emits)
</script>

<template>
  <AccordionRoot v-bind="forwarded" :class="cn(props.class)">
    <slot />
  </AccordionRoot>
</template>
