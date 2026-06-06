<script setup lang="ts">
import { computed } from "vue"

const props = defineProps<{ modelValue: string; placeholder?: string }>()
const emit = defineEmits<{ "update:modelValue": [v: string] }>()

const hasVal = computed(() => props.modelValue.length > 0)
</script>

<template>
  <div class="psearch" :class="{ 'has-val': hasVal }">
    <i class="fa-solid fa-magnifying-glass"></i>
    <input
      :value="modelValue"
      :placeholder="placeholder || 'Buscar…'"
      type="text"
      autocomplete="off"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <button v-show="hasVal" class="clr" aria-label="Limpar" @click="emit('update:modelValue', '')">
      <i class="fa-solid fa-circle-xmark"></i>
    </button>
  </div>
</template>
