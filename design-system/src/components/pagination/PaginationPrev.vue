<script setup lang="ts">
import type { PaginationPrevProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationPrev } from "reka-ui"
import { cn } from "@/utils/cn"
import { buttonVariants } from "@/components/button"

const props = defineProps<PaginationPrevProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <PaginationPrev
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
        <polyline points="15 18 9 12 15 6" />
      </svg>
    </slot>
  </PaginationPrev>
</template>
