<script setup lang="ts">
defineProps<{
  carregando: boolean
  /** Se false, o botão não é renderizado (lista já chegou ao fim). */
  visivel: boolean
}>()

defineEmits<{ carregar: [] }>()
</script>

<template>
  <div v-if="visivel" class="load-more-wrap">
    <button
      class="load-more-btn"
      :disabled="carregando"
      aria-label="Carregar mais itens"
      @click="$emit('carregar')"
    >
      <span v-if="carregando" class="lm-spin">
        <i class="fa-solid fa-circle-notch"></i>
      </span>
      <span v-else>Carregar mais</span>
    </button>
  </div>
</template>

<style scoped>
.load-more-wrap {
  display: flex;
  justify-content: center;
  padding: 16px 0 8px;
}

.load-more-btn {
  min-height: 44px;
  min-width: 140px;
  padding: 0 20px;
  border-radius: var(--radius-full);
  border: 1.5px solid var(--app-border);
  background: var(--app-card);
  color: var(--brand);
  font: inherit;
  font-size: var(--fs-sm);
  font-weight: var(--fw-bold);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: background 0.15s, border-color 0.15s;
}

.load-more-btn:active:not(:disabled) {
  background: var(--brand-soft);
  border-color: var(--brand);
}

.load-more-btn:disabled {
  opacity: 0.6;
  cursor: default;
}

.lm-spin i {
  animation: spin 0.8s linear infinite;
  color: var(--brand);
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
