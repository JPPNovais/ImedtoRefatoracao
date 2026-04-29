<script setup lang="ts">
import { ref } from "vue"

/**
 * AppCollapsible — seção colapsável do design system.
 * Header clicável expande/recolhe o conteúdo com animação leve.
 */
interface Props {
    titulo: string
    subtitulo?: string
    inicialmenteAberto?: boolean
    icone?: string
}

const props = withDefaults(defineProps<Props>(), {
    inicialmenteAberto: true,
})

const aberto = ref(props.inicialmenteAberto)
</script>

<template>
    <div class="collapsible">
        <button
            type="button"
            class="collapsible-header"
            :aria-expanded="aberto"
            @click="aberto = !aberto"
        >
            <div class="collapsible-titulo-grupo">
                <i v-if="icone" :class="icone" class="collapsible-icone" aria-hidden="true"></i>
                <div class="collapsible-textos">
                    <span class="collapsible-titulo">{{ titulo }}</span>
                    <span v-if="subtitulo" class="collapsible-subtitulo">{{ subtitulo }}</span>
                </div>
            </div>
            <div class="collapsible-header-aside">
                <slot name="header-aside" />
                <i
                    class="fa-solid fa-chevron-down collapsible-seta"
                    :class="{ 'collapsible-seta--aberta': aberto }"
                    aria-hidden="true"
                ></i>
            </div>
        </button>

        <div v-if="aberto" class="collapsible-conteudo">
            <slot />
        </div>
    </div>
</template>

<style scoped>
.collapsible {
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    background: hsl(var(--card));
    overflow: hidden;
}

.collapsible-header {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 1rem 1.25rem;
    background: transparent;
    border: none;
    cursor: pointer;
    text-align: left;
    transition: background 0.12s;
}
.collapsible-header:hover { background: hsl(var(--muted) / 0.4); }

.collapsible-titulo-grupo {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    min-width: 0;
}

.collapsible-icone {
    color: hsl(var(--primary));
    font-size: 0.9em;
    flex-shrink: 0;
}

.collapsible-textos {
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
    min-width: 0;
}

.collapsible-titulo {
    font-size: 0.92em;
    font-weight: 600;
    color: hsl(var(--foreground));
}

.collapsible-subtitulo {
    font-size: 0.78em;
    color: hsl(var(--muted-foreground));
}

.collapsible-header-aside {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-shrink: 0;
}

.collapsible-seta {
    font-size: 0.8em;
    color: hsl(var(--muted-foreground));
    transition: transform 0.2s;
    flex-shrink: 0;
}
.collapsible-seta--aberta { transform: rotate(180deg); }

.collapsible-conteudo {
    padding: 0 1.25rem 1.25rem;
    border-top: 1px solid hsl(var(--border));
}
</style>
