<script setup lang="ts">
defineProps<{ status: string }>()
</script>

<template>
    <span class="badge-status" :class="`badge-status--${status.toLowerCase()}`">
        {{ status }}
    </span>
</template>

<style scoped>
.badge-status {
    display: inline-block;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.badge-status--ativo { background: hsl(var(--success) / 0.15); color: hsl(var(--success) / 0.15); }
.badge-status--inativo { background: hsl(var(--muted) / 0.4); color: hsl(var(--muted-foreground)); }
.badge-status--suspenso { background: hsl(var(--warning) / 0.15); color: hsl(var(--warning-foreground, var(--foreground))); }
</style>
