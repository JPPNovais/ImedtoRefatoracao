<script setup lang="ts">
import type { NavigationMenuContentProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { NavigationMenuContent } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<NavigationMenuContentProps & { class?: HTMLAttributes["class"] }>()
</script>
<template>
  <NavigationMenuContent v-bind="props" :class="cn('left-0 top-0 w-full data-[motion^=from-]:animate-in data-[motion^=to-]:animate-out data-[motion^=from-]:fade-in data-[motion^=to-]:fade-out md:absolute md:w-auto', props.class)">
    <slot />
  </NavigationMenuContent>
</template>
