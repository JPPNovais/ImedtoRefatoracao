<script setup lang="ts">
interface Props {
  title: string
  subtitle?: string
}

defineProps<Props>()
</script>

<template>
  <div class="flex items-start justify-between gap-4 mb-6">
    <div>
      <h1 class="page-header-title">{{ title }}</h1>
      <p v-if="subtitle" class="page-header-sub">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.actions" class="flex items-center gap-2 flex-shrink-0">
      <slot name="actions" />
    </div>
  </div>
</template>

<style scoped>
/* Título de página canônico — 30px/800/-0.015em via tokens (briefing 2026-06-08_003) */
.page-header-title {
  font-size: var(--text-3xl);
  font-weight: var(--font-weight-extrabold);
  letter-spacing: var(--tracking-title);
  line-height: var(--line-height-tight);
  color: var(--c-primary-dark);
  margin: 0;
}

.page-header-sub {
  font-size: var(--text-base);
  color: hsl(var(--muted-foreground));
  margin-top: 0.125rem;
}
</style>
