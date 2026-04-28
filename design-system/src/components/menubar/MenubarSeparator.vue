<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { MenubarSeparator } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<{ class?: HTMLAttributes["class"] }>()
</script>
<template><MenubarSeparator :class="cn('-mx-1 my-1 h-px bg-muted', props.class)" /></template>
