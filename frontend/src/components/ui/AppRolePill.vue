<script setup lang="ts">
/**
 * Pill colorido representando um papel/função (modelo de permissão). Recebe
 * cor e ícone do próprio modelo (`ModeloPermissao.cor` / `.icone`) ou do
 * fallback definido pela tela. Usado em tabelas, modais e role-selector.
 */
const props = defineProps<{
    nome: string
    icone?: string | null
    cor?: string | null
    /** Tamanho do pill — `sm` é o padrão, `md` aumenta padding e fonte. */
    tamanho?: "sm" | "md"
}>()

const corBase = "hsl(0 0% 45%)"
const cor = props.cor ?? corBase
// `color-mix` é nativo no CSS moderno — fallback hsla é seguro caso engine antiga.
const bg = `color-mix(in srgb, ${cor} 12%, white)`
</script>

<template>
    <span class="role-pill" :class="`role-pill--${tamanho ?? 'sm'}`" :style="{ background: bg, color: cor }">
        <i v-if="icone" :class="['fa-solid', icone]"></i>
        {{ nome }}
    </span>
</template>

<style scoped>
.role-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 4px 10px;
    border-radius: 999px;
    font-weight: 600;
    white-space: nowrap;
    line-height: 1.2;
}
.role-pill--sm { font-size: 12px; }
.role-pill--md { font-size: 13px; padding: 6px 12px; }
.role-pill i { font-size: 10px; }
</style>
