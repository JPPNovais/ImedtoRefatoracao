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
                <button class="admin-btn-danger" :disabled="salvando" @click="confirmar">
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
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
}

.admin-modal {
    background: #1e293b;
    border: 1px solid #334155;
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
    color: #f8fafc;
    margin: 0;
}

.admin-modal-desc {
    color: #94a3b8;
    font-size: 0.875rem;
    margin: 0;
}

.admin-campo {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.admin-label {
    color: #94a3b8;
    font-size: 0.8125rem;
    font-weight: 600;
}

.admin-textarea {
    background: #0f172a;
    border: 1px solid #334155;
    color: #f8fafc;
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    width: 100%;
    box-sizing: border-box;
    resize: vertical;
}

.admin-campo-erro {
    color: #f87171;
    font-size: 0.8125rem;
    margin: 0;
}

.admin-modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

.admin-btn-danger {
    background: #dc2626;
    color: white;
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-danger:hover:not(:disabled) {
    background: #b91c1c;
}

.admin-btn-danger:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.admin-btn-secondary {
    background: #334155;
    color: #f8fafc;
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    cursor: pointer;
}
</style>
