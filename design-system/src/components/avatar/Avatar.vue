<script setup lang="ts">
import type { AvatarRootProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { AvatarRoot } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<AvatarRootProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <AvatarRoot
    v-bind="props"
    :class="cn('relative flex h-10 w-10 shrink-0 overflow-hidden rounded-full', props.class)"
  >
    <slot />
  </AvatarRoot>
</template>
