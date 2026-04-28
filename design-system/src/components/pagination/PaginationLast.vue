<script setup lang="ts">
import type { PaginationLastProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationLast } from "reka-ui"
import { cn } from "@/utils/cn"
import { buttonVariants } from "@/components/button"

const props = defineProps<PaginationLastProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <PaginationLast
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
        <polyline points="13 17 18 12 13 7" />
        <polyline points="6 17 11 12 6 7" />
      </svg>
    </slot>
  </PaginationLast>
</template>
