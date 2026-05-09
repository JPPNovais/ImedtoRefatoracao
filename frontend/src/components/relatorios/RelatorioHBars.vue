<script setup lang="ts">
/**
 * RelatorioHBars — barras horizontais de ranking.
 */
export interface HBarItem {
    label: string
    valor: number
    cor?: string
}

const props = withDefaults(defineProps<{
    itens: HBarItem[]
    cor?: string
    formatarValor?: (v: number) => string
}>(), {
    cor: 'hsl(var(--primary))',
    formatarValor: (v: number) => v.toLocaleString('pt-BR'),
})

function maxValor() {
    return Math.max(...props.itens.map(d => d.valor)) || 1
}
</script>

<template>
    <div class="rp-hbars" role="list">
        <div
            v-for="(item, i) in itens"
            :key="i"
            class="rp-hbar"
            role="listitem"
        >
            <div class="rp-hbar-label" :title="item.label">{{ item.label }}</div>
            <div class="rp-hbar-track" aria-hidden="true">
                <div
                    class="rp-hbar-fill"
                    :style="{
                        width: `${(item.valor / maxValor()) * 100}%`,
                        background: item.cor || cor
                    }"
                ></div>
            </div>
            <div class="rp-hbar-valor">{{ formatarValor(item.valor) }}</div>
        </div>
    </div>
</template>

<style scoped>
.rp-hbars { display: flex; flex-direction: column; gap: 10px; }
.rp-hbar {
    display: grid;
    grid-template-columns: 1fr 1fr 100px;
    gap: 12px;
    align-items: center;
}
.rp-hbar-label {
    font-size: 12.5px;
    color: hsl(var(--foreground));
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.rp-hbar-track {
    background: hsl(var(--muted));
    border-radius: 6px;
    height: 20px;
    overflow: hidden;
}
.rp-hbar-fill {
    height: 100%;
    border-radius: 6px;
    transition: width 0.4s ease;
}
.rp-hbar-valor {
    font-size: 13px;
    font-weight: 600;
    color: hsl(var(--foreground));
    text-align: right;
}
</style>
