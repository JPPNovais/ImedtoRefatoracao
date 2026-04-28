<script setup lang="ts">
import type { PaginationNextProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationNext } from "reka-ui"
import { cn } from "@/utils/cn"
import { buttonVariants } from "@/components/button"

const props = defineProps<PaginationNextProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <PaginationNext
    v-bind="props"
    :class="cn(
      buttonVariants({ variant: 'outline', size: 'icon' }),
      props.class,
    )"
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
        <polyline points="9 18 15 12 9 6" />
      </svg>
    </slot>
  </PaginationNext>
</template>
