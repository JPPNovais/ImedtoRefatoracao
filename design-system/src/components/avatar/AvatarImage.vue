<script setup lang="ts">
import type { AvatarImageProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { AvatarImage } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<AvatarImageProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <AvatarImage
    v-bind="props"
    :class="cn('aspect-square h-full w-full', props.class)"
  />
</template>
