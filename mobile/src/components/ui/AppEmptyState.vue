<script setup lang="ts">
defineProps<{ icon: string; titulo: string; texto?: string }>()
</script>

<template>
  <div class="empty">
    <i class="fa-regular" :class="icon"></i>
    <b>{{ titulo }}</b>
    <p v-if="texto">{{ texto }}</p>
  </div>
</template>
