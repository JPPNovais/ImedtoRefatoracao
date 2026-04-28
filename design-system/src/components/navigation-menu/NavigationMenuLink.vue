<script setup lang="ts">
import type { NavigationMenuLinkProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { NavigationMenuLink } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<NavigationMenuLinkProps & { class?: HTMLAttributes["class"] }>()
</script>
<template>
  <NavigationMenuLink v-bind="props" :class="cn('block select-none space-y-1 rounded-md p-3 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground', props.class)">
    <slot />
  </NavigationMenuLink>
</template>
