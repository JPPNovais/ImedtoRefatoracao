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
                <button class="admin-btn-gratuidade" :disabled="salvando || !gratuidadeMotivoValido || motivo.trim().length < 10" @click="salvar">
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
    max-width: 480px;
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

.admin-campo {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.admin-label {
    color: hsl(var(--muted-foreground));
    font-size: 0.8125rem;
    font-weight: 600;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.admin-contador {
    font-size: 0.75rem;
    color: hsl(var(--destructive));
    font-weight: 400;
}

.admin-contador--ok {
    color: hsl(var(--success));
}

.admin-input,
.admin-textarea {
    background: hsl(var(--background));
    border: 1px solid hsl(var(--border));
    color: hsl(var(--foreground));
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
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0;
}

.admin-campo-hint {
    color: hsl(var(--muted-foreground));
    font-size: 0.75rem;
    margin: 0;
}

.admin-modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

.admin-btn-gratuidade {
    background: hsl(var(--success) / 0.15);
    color: hsl(var(--success));
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-gratuidade:hover:not(:disabled) {
    background: hsl(var(--success));
}

.admin-btn-gratuidade:disabled {
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
