<script setup lang="ts">
/**
 * Notificação flutuante (bottom-center). Aparece com animação suave e desaparece
 * automaticamente após `duracao` ms (padrão 3500). Caller controla via v-if.
 */
import { onMounted } from "vue"

const props = defineProps<{
    mensagem: string
    variante?: "info" | "success" | "error"
    duracao?: number
}>()

const emit = defineEmits<{ (e: "fechar"): void }>()

onMounted(() => {
    const ms = props.duracao ?? 3500
    setTimeout(() => emit("fechar"), ms)
})
</script>

<template>
    <Teleport to="body">
        <div class="toast" :class="`toast-${variante ?? 'info'}`" role="status" aria-live="polite">
            <i class="fa-solid" :class="[
                variante === 'success' ? 'fa-circle-check' :
                variante === 'error' ? 'fa-circle-exclamation' :
                'fa-circle-info'
            ]"></i>
            {{ mensagem }}
        </div>
    </Teleport>
</template>

<style scoped>
.toast {
    position: fixed; bottom: 24px; left: 50%; transform: translateX(-50%);
    display: inline-flex; align-items: center; gap: 10px;
    background: hsl(var(--primary-dark)); color: white;
    padding: 12px 20px; border-radius: 999px;
    box-shadow: 0 12px 32px -10px rgb(0 0 0 / 0.14);
    font-size: 13px; font-weight: 600;
    z-index: 1000;
    animation: toast-in 240ms cubic-bezier(0.22, 1, 0.36, 1);
}
.toast-success { background: hsl(160 79% 32%); }
.toast-error   { background: hsl(var(--error)); }
.toast i { font-size: 14px; }
@keyframes toast-in {
    from { transform: translate(-50%, 16px); opacity: 0; }
    to   { transform: translateX(-50%); opacity: 1; }
}
</style>
