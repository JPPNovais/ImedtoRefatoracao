<script setup lang="ts">
import type { PaginationListItemProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { PaginationListItem } from "reka-ui"
import { cn } from "@/utils/cn"
import { buttonVariants } from "@/components/button"

const props = withDefaults(
  defineProps<PaginationListItemProps & {
    class?: HTMLAttributes["class"]
    isActive?: boolean
  }>(),
  {
    isActive: false,
  },
)
</script>

<template>
  <PaginationListItem
    :value="props.value"
    :class="cn(
      buttonVariants({
        variant: isActive ? 'default' : 'outline',
        size: 'icon',
      }),
      props.class,
    )"
  >
    <slot />
  </PaginationListItem>
</template>
