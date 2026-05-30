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
.badge-status--ativo { background: #d1fae5; color: #065f46; }
.badge-status--inativo { background: #f3f4f6; color: #6b7280; }
.badge-status--suspenso { background: #fef3c7; color: #92400e; }
</style>
