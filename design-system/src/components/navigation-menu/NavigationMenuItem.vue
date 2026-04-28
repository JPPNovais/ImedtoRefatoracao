<script setup lang="ts">
import type { NavigationMenuItemProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { NavigationMenuItem } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<NavigationMenuItemProps & { class?: HTMLAttributes["class"] }>()
</script>
<template>
  <NavigationMenuItem v-bind="props" :class="cn('relative', props.class)"><slot /></NavigationMenuItem>
</template>
