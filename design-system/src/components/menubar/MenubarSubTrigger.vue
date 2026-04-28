<script setup lang="ts">
import type { MenubarSubTriggerProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { MenubarSubTrigger } from "reka-ui"
import { cn } from "@/utils/cn"
import { ChevronRight } from "lucide-vue-next"
const props = defineProps<MenubarSubTriggerProps & { class?: HTMLAttributes["class"]; inset?: boolean }>()
</script>
<template>
  <MenubarSubTrigger v-bind="props" :class="cn('flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none focus:bg-accent focus:text-accent-foreground data-[state=open]:bg-accent data-[state=open]:text-accent-foreground', inset && 'pl-8', props.class)">
    <slot />
    <ChevronRight class="ml-auto h-4 w-4" />
  </MenubarSubTrigger>
</template>
