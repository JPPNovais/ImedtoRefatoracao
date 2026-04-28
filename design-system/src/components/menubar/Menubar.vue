<script setup lang="ts">
import type { MenubarRootProps, MenubarRootEmits } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { MenubarRoot, useForwardPropsEmits } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<MenubarRootProps & { class?: HTMLAttributes["class"] }>()
const emits = defineEmits<MenubarRootEmits>()
const forwarded = useForwardPropsEmits(props, emits)
</script>
<template>
  <MenubarRoot v-bind="forwarded" :class="cn('flex h-9 items-center gap-1 rounded-md border bg-background p-1 shadow-sm', props.class)">
    <slot />
  </MenubarRoot>
</template>
