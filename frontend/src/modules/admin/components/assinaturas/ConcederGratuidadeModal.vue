<script setup lang="ts">
import { ref, computed } from "vue"
import { useAssinaturasStore } from "../../stores/assinaturasStore"

const props = defineProps<{
    estabelecimentoId: number
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const gratuidadeMotivo = ref("")
const fimEm = ref("")
const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

// Validação frontend: ≥ 20 chars (espelho da regra de backend).
const gratuidadeMotivoValido = computed(() => gratuidadeMotivo.value.trim().length >= 20)

async function salvar() {
    if (!gratuidadeMotivoValido.value) {
        erro.value = "Motivo da gratuidade deve ter pelo menos 20 caracteres."
        return
    }
    if (!motivo.value.trim()) {
        erro.value = "Motivo administrativo é obrigatório."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.concederGratuidade(props.estabelecimentoId, {
            gratuidadeMotivo: gratuidadeMotivo.value.trim(),
            fimEm: fimEm.value ? new Date(fimEm.value).toISOString() : null,
            motivo: motivo.value.trim(),
        })
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível conceder gratuidade."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <div class="admin-modal-overlay" @click.self="emit('fechar')">
        <div class="admin-modal">
            <h2 class="admin-modal-title">Conceder Gratuidade Vitalícia</h2>

            <div class="admin-campo">
                <label class="admin-label">
                    Motivo da gratuidade *
                    <span class="admin-contador" :class="{ 'admin-contador--ok': gratuidadeMotivoValido }">
                        {{ gratuidadeMotivo.trim().length }}/20 mín.
                    </span>
                </label>
                <textarea
                    v-model="gratuidadeMotivo"
                    class="admin-textarea"
                    rows="3"
                    placeholder="Ex: Parceiro estratégico beta tester (mínimo 20 caracteres)"
                />
                <p v-if="gratuidadeMotivo.trim().length > 0 && !gratuidadeMotivoValido" class="admin-campo-erro">
                    Mínimo de 20 caracteres.
                </p>
            </div>

            <div class="admin-campo">
                <label class="admin-label">Data de fim da gratuidade (opcional)</label>
                <input v-model="fimEm" type="date" class="admin-input" />
                <p class="admin-campo-hint">Deixe vazio para gratuidade vitalícia sem expiração.</p>
            </div>

            <div class="admin-campo">
                <label class="admin-label">Motivo administrativo *</label>
                <textarea v-model="motivo" class="admin-textarea" rows="2" placeholder="Motivo para registro de auditoria..." />
            </div>

            <p v-if="erro" class="admin-campo-erro">{{ erro }}</p>

            <div class="admin-modal-actions">
                <button class="admin-btn-secondary" @click="emit('fechar')">Cancelar</button>
                <button class="admin-btn-gratuidade" :disabled="salvando" @click="salvar">
                    {{ salvando ? "Salvando..." : "Conceder gratuidade" }}
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
    max-width: 480px;
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

.admin-campo {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.admin-label {
    color: #94a3b8;
    font-size: 0.8125rem;
    font-weight: 600;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.admin-contador {
    font-size: 0.75rem;
    color: #f87171;
    font-weight: 400;
}

.admin-contador--ok {
    color: #4ade80;
}

.admin-input,
.admin-textarea {
    background: #0f172a;
    border: 1px solid #334155;
    color: #f8fafc;
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    width: 100%;
    box-sizing: border-box;
}

.admin-textarea {
    resize: vertical;
}

.admin-campo-erro {
    color: #f87171;
    font-size: 0.8125rem;
    margin: 0;
}

.admin-campo-hint {
    color: #64748b;
    font-size: 0.75rem;
    margin: 0;
}

.admin-modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

.admin-btn-gratuidade {
    background: #065f46;
    color: #6ee7b7;
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-gratuidade:hover:not(:disabled) {
    background: #047857;
}

.admin-btn-gratuidade:disabled {
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
