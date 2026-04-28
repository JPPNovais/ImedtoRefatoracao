<script setup lang="ts">
import type { NavigationMenuListProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { NavigationMenuList } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<NavigationMenuListProps & { class?: HTMLAttributes["class"] }>()
</script>
<template>
  <NavigationMenuList v-bind="props" :class="cn('group flex flex-1 list-none items-center justify-center gap-1', props.class)">
    <slot />
  </NavigationMenuList>
</template>
