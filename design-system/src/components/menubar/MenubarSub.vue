<script setup lang="ts">
import type { MenubarSubProps, MenubarSubEmits } from "reka-ui"
import { MenubarSub, useForwardPropsEmits } from "reka-ui"
const props = defineProps<MenubarSubProps>()
const emits = defineEmits<MenubarSubEmits>()
const forwarded = useForwardPropsEmits(props, emits)
</script>
<template><MenubarSub v-bind="forwarded"><slot /></MenubarSub></template>
