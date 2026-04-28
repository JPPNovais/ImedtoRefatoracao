<script setup lang="ts">
import type { PaginationFirstProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationFirst } from "reka-ui"
import { cn } from "@/utils/cn"
import { buttonVariants } from "@/components/button"

const props = defineProps<PaginationFirstProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <PaginationFirst
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
        <polyline points="11 17 6 12 11 7" />
        <polyline points="18 17 13 12 18 7" />
      </svg>
    </slot>
  </PaginationFirst>
</template>
