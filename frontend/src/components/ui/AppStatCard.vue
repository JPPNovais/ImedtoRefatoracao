<script setup lang="ts">
/**
 * AppStatCard — card de estatística clicável, com barra lateral colorida e
 * estado "ativo" para uso como filtro (estilo design Anthropic).
 *
 * Uso:
 *   <AppStatCard label="Confirmados" :valor="12" cor="success" :ativo="..." @click="..." />
 *
 * Cor pode ser uma das tags de tema (`primary`, `success`, `warning`, `info`,
 * `error`, `muted`) ou um HSL custom (ex: "280 60% 50%").
 */
import { computed } from "vue"

type CorPreset = "primary" | "success" | "warning" | "info" | "error" | "muted"

const props = defineProps<{
    label: string
    valor: number | string
    icone?: string
    cor?: CorPreset | string
    legenda?: string
    ativo?: boolean
}>()

defineEmits<{
    (e: "click"): void
}>()

const corHsl = computed(() => {
    const c = props.cor ?? "primary"
    const map: Record<CorPreset, string> = {
        primary: "var(--primary, 254 56% 38%)",
        success: "var(--success, 160 79% 39%)",
        warning: "var(--warning, 45 96% 47%)",
        info:    "var(--info, 199 89% 48%)",
        error:   "var(--error, 0 84% 60%)",
        muted:   "0 0% 60%",
    }
    return `hsl(${map[c as CorPreset] ?? c})`
})
</script>

<template>
    <button
        type="button"
        class="stat-card"
        :class="{ ativo }"
        :style="{ '--accent-color': corHsl }"
        @click="$emit('click')"
    >
        <div class="top">
            <span class="lbl">{{ label }}</span>
            <i v-if="icone" :class="icone" aria-hidden="true"></i>
        </div>
        <div class="num">{{ valor }}</div>
        <div v-if="legenda" class="foot">{{ legenda }}</div>
    </button>
</template>

<style scoped>
.stat-card {
    padding: 12px 14px;
    border-radius: 12px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.07);
    cursor: pointer;
    transition: all 0.15s;
    position: relative;
    overflow: hidden;
    text-align: left;
    font-family: inherit;
    width: 100%;
}
.stat-card::before {
    content: '';
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 3px;
    background: var(--accent-color, hsl(var(--foreground) / 0.2));
    opacity: 0.85;
}
.stat-card:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 14px hsl(0 0% 0% / 0.06);
}
.stat-card.ativo {
    border-color: var(--accent-color);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--accent-color) 18%, transparent);
    background: color-mix(in srgb, var(--accent-color) 4%, hsl(var(--card)));
}

.top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
}
.top .lbl {
    font-size: 10px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.7);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    line-height: 1.2;
}
.top i {
    font-size: 12px;
    color: var(--accent-color);
}

.num {
    font-size: 24px;
    font-weight: 800;
    color: hsl(var(--primary-dark, 254 56% 21%));
    line-height: 1;
    margin-top: 8px;
    letter-spacing: -0.02em;
    font-variant-numeric: tabular-nums;
}

.foot {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.6);
    margin-top: 4px;
    font-weight: 500;
}
</style>
