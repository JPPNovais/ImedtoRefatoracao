<script setup lang="ts">
import type { PaginationEllipsisProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationEllipsis } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<PaginationEllipsisProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <PaginationEllipsis
    v-bind="props"
    :class="cn('flex h-9 w-9 items-center justify-center', props.class)"
  >
    <slot>
      <svg
        xmlns="http://www.w3.org/2000/svg"
        width="16"
        height="16"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round"
      >
        <circle cx="12" cy="12" r="1" />
        <circle cx="19" cy="12" r="1" />
        <circle cx="5" cy="12" r="1" />
      </svg>
    </slot>
    <span class="sr-only">Mais páginas</span>
  </PaginationEllipsis>
</template>
