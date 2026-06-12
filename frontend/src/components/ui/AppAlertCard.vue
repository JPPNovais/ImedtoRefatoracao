<script setup lang="ts">
/**
 * AppAlertCard — card de alerta acionável para o bloco "Precisa da sua atenção" da Home.
 *
 * Características:
 * - Card inteiro é clicável (router-link envolve o conteúdo).
 * - Ícone + título + contagem + linhas de contexto opcionais.
 * - Variante de cor via token do DS (sem literais de cor).
 *
 * Uso:
 *   <AppAlertCard
 *     :to="{ name: 'Financeiro', query: { filtro: 'vencidos' } }"
 *     titulo="Lançamentos vencidos"
 *     icone="fa-solid fa-triangle-exclamation"
 *     :contagem="3"
 *     variante="error"
 *   >
 *     <template #contexto>
 *       <span>R$ 150,00 a receber</span>
 *     </template>
 *   </AppAlertCard>
 *
 * Variantes: "error" | "warning" | "info"
 */
import { RouterLink, type RouteLocationRaw } from "vue-router"

type Variante = "error" | "warning" | "info"

defineProps<{
    to: RouteLocationRaw
    titulo: string
    icone?: string
    contagem: number
    variante?: Variante
}>()
</script>

<template>
    <RouterLink :to="to" class="alert-card" :class="`alert-card--${variante ?? 'warning'}`">
        <div class="alert-card__header">
            <span class="alert-card__chip">
                <i v-if="icone" :class="icone" aria-hidden="true" />
            </span>
            <span class="alert-card__titulo">{{ titulo }}</span>
            <span class="alert-card__badge">{{ contagem }}</span>
        </div>
        <div v-if="$slots.contexto" class="alert-card__contexto">
            <slot name="contexto" />
        </div>
        <span class="alert-card__cta">
            Ver agora
            <i class="fa-solid fa-arrow-right" aria-hidden="true" />
        </span>
    </RouterLink>
</template>

<style scoped>
.alert-card {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 14px 16px;
    border-radius: 12px;
    border: 1.5px solid hsl(var(--secondary) / 0.12);
    background: hsl(var(--card));
    text-decoration: none;
    color: inherit;
    cursor: pointer;
    transition: transform 120ms, box-shadow 120ms, border-color 120ms;
    box-shadow: 0 1px 3px hsl(var(--primary-dark) / 0.04);
}

.alert-card:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px hsl(var(--primary-dark) / 0.08);
}

/* Variantes — borda lateral + chip */
.alert-card--error {
    border-left: 3px solid hsl(var(--destructive));
    background: hsl(var(--destructive) / 0.03);
}
.alert-card--error:hover {
    border-color: hsl(var(--destructive) / 0.5);
    border-left-color: hsl(var(--destructive));
}

.alert-card--warning {
    border-left: 3px solid hsl(var(--warning));
    background: hsl(var(--warning) / 0.03);
}
.alert-card--warning:hover {
    border-color: hsl(var(--warning) / 0.4);
    border-left-color: hsl(var(--warning));
}

.alert-card--info {
    border-left: 3px solid hsl(var(--primary));
    background: hsl(var(--primary) / 0.03);
}
.alert-card--info:hover {
    border-color: hsl(var(--primary) / 0.4);
    border-left-color: hsl(var(--primary));
}

.alert-card__header {
    display: flex;
    align-items: center;
    gap: 8px;
}

.alert-card__chip {
    width: 28px;
    height: 28px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    font-size: var(--text-sm);
}

.alert-card--error .alert-card__chip {
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
}
.alert-card--warning .alert-card__chip {
    background: hsl(var(--warning) / 0.14);
    color: hsl(28 90% 42%);
}
.alert-card--info .alert-card__chip {
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
}

.alert-card__titulo {
    flex: 1;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.85);
}

.alert-card__badge {
    min-width: 22px;
    height: 22px;
    padding: 0 6px;
    border-radius: 100px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-bold);
}

.alert-card--error .alert-card__badge {
    background: hsl(var(--destructive) / 0.12);
    color: hsl(var(--destructive));
}
.alert-card--warning .alert-card__badge {
    background: hsl(var(--warning) / 0.15);
    color: hsl(28 90% 42%);
}
.alert-card--info .alert-card__badge {
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
}

.alert-card__contexto {
    display: flex;
    flex-direction: column;
    gap: 3px;
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.7);
    padding-left: 36px;
}

.alert-card__cta {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    padding-left: 36px;
    margin-top: 2px;
}

.alert-card--error .alert-card__cta { color: hsl(var(--destructive)); }
.alert-card--warning .alert-card__cta { color: hsl(28 90% 42%); }
.alert-card--info .alert-card__cta { color: hsl(var(--primary)); }
</style>
