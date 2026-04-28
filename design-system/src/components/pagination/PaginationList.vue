<script setup lang="ts">
import type { PaginationListProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationList } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<PaginationListProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <PaginationList
    v-bind="props"
    :class="cn('flex flex-row items-center gap-1', props.class)"
  >
    <slot />
  </PaginationList>
</template>
