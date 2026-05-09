<script setup lang="ts">
/**
 * RelatorioPeriodoFiltro — seletor de período com presets + comparação.
 * Emite { dataInicio, dataFim } ao aplicar.
 */
import { ref, computed, watch } from 'vue'

export type PresetPeriodo = 'hoje' | '7d' | '30d' | 'mes' | 'trim'

const props = withDefaults(defineProps<{
    modelValue: PresetPeriodo | 'custom'
    comparar?: boolean
}>(), {
    comparar: false,
})

const emit = defineEmits<{
    'update:modelValue': [value: PresetPeriodo | 'custom']
    'update:comparar': [value: boolean]
    aplicar: [{ dataInicio: string; dataFim: string }]
}>()

const presets: { id: PresetPeriodo | 'custom'; label: string }[] = [
    { id: 'hoje',   label: 'Hoje' },
    { id: '7d',     label: '7 dias' },
    { id: '30d',    label: '30 dias' },
    { id: 'mes',    label: 'Este mês' },
    { id: 'trim',   label: 'Trimestre' },
    { id: 'custom', label: 'Customizado' },
]

const dataInicioCustom = ref('')
const dataFimCustom    = ref('')

function calcularDatas(preset: PresetPeriodo | 'custom'): { inicio: string; fim: string } {
    const hoje = new Date()
    const fmt = (d: Date) => d.toISOString().slice(0, 10)

    switch (preset) {
        case 'hoje': {
            const s = fmt(hoje)
            return { inicio: s, fim: s }
        }
        case '7d': {
            const ini = new Date(hoje); ini.setDate(hoje.getDate() - 6)
            return { inicio: fmt(ini), fim: fmt(hoje) }
        }
        case '30d': {
            const ini = new Date(hoje); ini.setDate(hoje.getDate() - 29)
            return { inicio: fmt(ini), fim: fmt(hoje) }
        }
        case 'mes': {
            const ini = new Date(hoje.getFullYear(), hoje.getMonth(), 1)
            return { inicio: fmt(ini), fim: fmt(hoje) }
        }
        case 'trim': {
            const ini = new Date(hoje); ini.setMonth(hoje.getMonth() - 2); ini.setDate(1)
            return { inicio: fmt(ini), fim: fmt(hoje) }
        }
        case 'custom':
            return { inicio: dataInicioCustom.value, fim: dataFimCustom.value }
    }
}

function selecionar(preset: PresetPeriodo | 'custom') {
    emit('update:modelValue', preset)
    if (preset !== 'custom') {
        const { inicio, fim } = calcularDatas(preset)
        emit('aplicar', { dataInicio: inicio, dataFim: fim })
    }
}

function aplicarCustom() {
    if (!dataInicioCustom.value || !dataFimCustom.value) return
    emit('aplicar', { dataInicio: dataInicioCustom.value, dataFim: dataFimCustom.value })
}
</script>

<template>
    <div class="rp-periodo-filtro">
        <div class="rp-presets" role="group" aria-label="Período">
            <button
                v-for="p in presets"
                :key="p.id"
                type="button"
                class="rp-preset-chip"
                :class="{ ativo: modelValue === p.id }"
                @click="selecionar(p.id)"
            >{{ p.label }}</button>
        </div>

        <!-- Range customizado -->
        <div v-if="modelValue === 'custom'" class="rp-custom-range">
            <label class="rp-custom-label" for="rp-data-inicio">De</label>
            <input
                id="rp-data-inicio"
                v-model="dataInicioCustom"
                type="date"
                class="rp-custom-input"
            />
            <label class="rp-custom-label" for="rp-data-fim">Até</label>
            <input
                id="rp-data-fim"
                v-model="dataFimCustom"
                type="date"
                class="rp-custom-input"
            />
            <button type="button" class="rp-custom-btn" @click="aplicarCustom">Aplicar</button>
        </div>

        <!-- Toggle comparar -->
        <label class="rp-comparar">
            <input
                type="checkbox"
                :checked="comparar"
                @change="emit('update:comparar', ($event.target as HTMLInputElement).checked)"
                class="sr-only"
            />
            <span class="rp-toggle" :class="{ ativo: comparar }" aria-hidden="true"></span>
            <span>Comparar com período anterior</span>
        </label>
    </div>
</template>

<style scoped>
.rp-periodo-filtro {
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    align-items: center;
}
.rp-presets {
    display: flex;
    gap: 4px;
    background: hsl(var(--muted));
    padding: 4px;
    border-radius: 10px;
}
.rp-preset-chip {
    border: 0;
    background: transparent;
    padding: 6px 12px;
    border-radius: 7px;
    font-size: 12.5px;
    font-weight: 500;
    color: hsl(var(--muted-foreground));
    cursor: pointer;
    transition: all 0.15s;
    font-family: inherit;
}
.rp-preset-chip:hover { color: hsl(var(--foreground)); }
.rp-preset-chip.ativo {
    background: hsl(var(--card));
    color: hsl(var(--primary));
    box-shadow: 0 1px 2px hsl(var(--foreground) / 0.08);
    font-weight: 600;
}

.rp-custom-range {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
}
.rp-custom-label {
    font-size: 12px;
    font-weight: 500;
    color: hsl(var(--muted-foreground));
}
.rp-custom-input {
    padding: 5px 8px;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm);
    font-family: inherit;
    font-size: 13px;
    background: hsl(var(--card));
    color: hsl(var(--foreground));
}
.rp-custom-input:focus { outline: none; border-color: hsl(var(--primary)); }
.rp-custom-btn {
    padding: 5px 14px;
    border: 0;
    border-radius: var(--radius-sm);
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    font-family: inherit;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
}
.rp-custom-btn:hover { background: hsl(var(--primary-dark)); }

.rp-comparar {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    font-size: 13px;
    color: hsl(var(--muted-foreground));
    user-select: none;
}
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
.rp-toggle {
    width: 34px;
    height: 18px;
    background: hsl(var(--border));
    border-radius: 999px;
    position: relative;
    transition: background 0.2s;
    flex-shrink: 0;
}
.rp-toggle::before {
    content: '';
    position: absolute;
    top: 2px;
    left: 2px;
    width: 14px;
    height: 14px;
    background: hsl(var(--card));
    border-radius: 50%;
    transition: left 0.2s;
}
.rp-toggle.ativo { background: hsl(var(--primary)); }
.rp-toggle.ativo::before { left: 18px; }
</style>
