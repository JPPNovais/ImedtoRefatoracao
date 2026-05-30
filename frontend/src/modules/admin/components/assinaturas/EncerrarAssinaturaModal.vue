<script setup lang="ts">
import { ref } from "vue"
import { useAssinaturasStore } from "../../stores/assinaturasStore"

const props = defineProps<{
    assinaturaId: string
    estabelecimentoId: number
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

async function confirmar() {
    if (!motivo.value.trim()) {
        erro.value = "Motivo é obrigatório."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.encerrar(props.assinaturaId, props.estabelecimentoId, motivo.value.trim())
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível encerrar a assinatura."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <div class="admin-modal-overlay" @click.self="emit('fechar')">
        <div class="admin-modal">
            <h2 class="admin-modal-title">Encerrar assinatura</h2>
            <p class="admin-modal-desc">
                Esta ação encerrará a vigência atual. O histórico é preservado.
            </p>

            <div class="admin-campo">
                <label class="admin-label">Motivo *</label>
                <textarea
                    v-model="motivo"
                    class="admin-textarea"
                    rows="3"
                    placeholder="Motivo do encerramento..."
                />
            </div>

            <p v-if="erro" class="admin-campo-erro">{{ erro }}</p>

            <div class="admin-modal-actions">
                <button class="admin-btn-secondary" @click="emit('fechar')">Cancelar</button>
                <button class="admin-btn-danger" :disabled="salvando || motivo.trim().length < 10" @click="confirmar">
                    {{ salvando ? "Encerrando..." : "Encerrar" }}
                </button>
            </div>
        </div>
    </div>
</template>

<style scoped>
.admin-modal-overlay {
    position: fixed;
    inset: 0;
    background: hsl(var(--foreground) / 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
}

.admin-modal {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 12px;
    padding: 1.5rem;
    width: 100%;
    max-width: 440px;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.admin-modal-title {
    font-size: 1.125rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0;
}

.admin-modal-desc {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
    margin: 0;
}

.admin-campo {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.admin-label {
    color: hsl(var(--muted-foreground));
    font-size: 0.8125rem;
    font-weight: 600;
}

.admin-textarea {
    background: hsl(var(--background));
    border: 1px solid hsl(var(--border));
    color: hsl(var(--foreground));
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    width: 100%;
    box-sizing: border-box;
    resize: vertical;
}

.admin-campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0;
}

.admin-modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

.admin-btn-danger {
    background: hsl(var(--destructive));
    color: hsl(var(--card));
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-danger:hover:not(:disabled) {
    background: hsl(var(--destructive));
}

.admin-btn-danger:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.admin-btn-secondary {
    background: hsl(var(--border));
    color: hsl(var(--foreground));
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    cursor: pointer;
}
</style>
