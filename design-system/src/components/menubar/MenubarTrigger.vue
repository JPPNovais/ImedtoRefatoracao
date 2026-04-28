<script setup lang="ts">
import type { MenubarTriggerProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { MenubarTrigger } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<MenubarTriggerProps & { class?: HTMLAttributes["class"] }>()
</script>
<template>
  <MenubarTrigger v-bind="props" :class="cn('flex cursor-default select-none items-center rounded-sm px-3 py-1 text-sm font-medium outline-none focus:bg-accent focus:text-accent-foreground data-[state=open]:bg-accent data-[state=open]:text-accent-foreground', props.class)">
    <slot />
  </MenubarTrigger>
</template>
