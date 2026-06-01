<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue"

const props = defineProps<{
    receitaId: number
}>()

const emit = defineEmits<{
    cancelar: []
}>()

// Contador regressivo de 5 min (300s).
const DURACAO_S = 300
const restantes = ref(DURACAO_S)
let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
    timer = setInterval(() => {
        if (restantes.value > 0) restantes.value--
    }, 1_000)
})

onUnmounted(() => {
    if (timer) clearInterval(timer)
})

const minutosRestantes = computed(() => Math.floor(restantes.value / 60))
const segundosRestantes = computed(() => restantes.value % 60)
const contadorLabel = computed(() =>
    `${minutosRestantes.value}:${String(segundosRestantes.value).padStart(2, "0")}`,
)
</script>

<template>
    <div class="polling-indicator" role="status" aria-live="polite">
        <div class="spinner" aria-hidden="true">
            <svg viewBox="0 0 50 50" class="spinner-svg">
                <circle cx="25" cy="25" r="20" fill="none" stroke-width="4" />
            </svg>
        </div>
        <div class="info">
            <p class="titulo">Aguardando confirmação no app</p>
            <p class="descricao">
                Abra o aplicativo BirdID no seu celular e confirme a assinatura.
            </p>
            <p class="contador">{{ contadorLabel }}</p>
        </div>
        <button class="btn-cancelar" type="button" @click="emit('cancelar')">
            Cancelar
        </button>
    </div>
</template>

<style scoped>
.polling-indicator {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.75rem 1rem;
    background: hsl(var(--accent));
    border: 1px solid hsl(var(--border));
    border-radius: 0.5rem;
    flex-wrap: wrap;
}
.spinner { flex-shrink: 0; }
.spinner-svg {
    width: 36px; height: 36px;
    animation: spin 1.2s linear infinite;
}
.spinner-svg circle {
    stroke: hsl(var(--primary));
    stroke-dasharray: 80;
    stroke-dashoffset: 60;
    stroke-linecap: round;
}
@keyframes spin { to { transform: rotate(360deg); } }
.info { flex: 1; display: flex; flex-direction: column; gap: 0.15rem; min-width: 0; }
.titulo { font-size: 0.9rem; font-weight: 700; margin: 0; color: hsl(var(--foreground)); }
.descricao { font-size: 0.8rem; color: hsl(var(--muted-foreground)); margin: 0; }
.contador {
    font-size: 0.85rem; font-weight: 600; font-variant-numeric: tabular-nums;
    color: hsl(var(--primary)); margin: 0;
}
.btn-cancelar {
    padding: 0.35rem 0.75rem; border-radius: 0.375rem; font-size: 0.8rem; font-weight: 600;
    background: transparent; color: hsl(var(--muted-foreground));
    border: 1px solid hsl(var(--border)); cursor: pointer; flex-shrink: 0;
}
.btn-cancelar:hover { background: hsl(var(--muted)); }
</style>
