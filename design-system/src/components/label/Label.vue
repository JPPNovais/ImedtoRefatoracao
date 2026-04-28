<script setup lang="ts">
import type { LabelProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { Label, useForwardProps } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<LabelProps & { class?: HTMLAttributes["class"] }>()

const delegatedProps = useForwardProps(props)
</script>

<template>
  <Label
    v-bind="delegatedProps"
    :class="cn('text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70', props.class)"
  >
    <slot />
  </Label>
</template>
