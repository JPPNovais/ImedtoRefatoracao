<script setup lang="ts">
import { ref } from "vue"
import { useAssinaturasStore } from "../../stores/assinaturasStore"
import type { PlanoAdminDto } from "../../services/planosService"

const props = defineProps<{
    estabelecimentoId: number
    planos: PlanoAdminDto[]
}>()

const emit = defineEmits<{
    fechar: []
    sucesso: []
}>()

const store = useAssinaturasStore()

const planoId = ref("")
const inicio = ref(new Date().toISOString().slice(0, 10))
const fimEm = ref("")
const motivo = ref("")
const erro = ref("")
const salvando = ref(false)

async function salvar() {
    if (!planoId.value) {
        erro.value = "Selecione um plano."
        return
    }
    if (!motivo.value.trim()) {
        erro.value = "Motivo é obrigatório."
        return
    }

    salvando.value = true
    erro.value = ""
    try {
        await store.trocarPlano(props.estabelecimentoId, {
            planoId: planoId.value,
            inicio: new Date(inicio.value).toISOString(),
            fimEm: fimEm.value ? new Date(fimEm.value).toISOString() : null,
            motivo: motivo.value.trim(),
        })
        emit("sucesso")
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erro.value = msg ?? "Não foi possível trocar o plano."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <div class="admin-modal-overlay" @click.self="emit('fechar')">
        <div class="admin-modal">
            <h2 class="admin-modal-title">Trocar plano</h2>

            <div class="admin-campo">
                <label class="admin-label">Plano *</label>
                <select v-model="planoId" class="admin-select">
                    <option value="">Selecione...</option>
                    <option v-for="p in planos" :key="p.id" :value="p.id">
                        {{ p.nome }}
                    </option>
                </select>
            </div>

            <div class="admin-campo">
                <label class="admin-label">Data de início *</label>
                <input v-model="inicio" type="date" class="admin-input" />
            </div>

            <div class="admin-campo">
                <label class="admin-label">Data de fim (opcional)</label>
                <input v-model="fimEm" type="date" class="admin-input" />
            </div>

            <div class="admin-campo">
                <label class="admin-label">Motivo *</label>
                <textarea v-model="motivo" class="admin-textarea" rows="2" placeholder="Motivo da troca..." />
            </div>

            <p v-if="erro" class="admin-campo-erro">{{ erro }}</p>

            <div class="admin-modal-actions">
                <button class="admin-btn-secondary" @click="emit('fechar')">Cancelar</button>
                <button class="admin-btn-primary" :disabled="salvando || motivo.trim().length < 10" @click="salvar">
                    {{ salvando ? "Salvando..." : "Confirmar" }}
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
}

.admin-input,
.admin-select,
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

.admin-modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

.admin-btn-primary {
    background: hsl(var(--primary));
    color: hsl(var(--card));
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-primary:disabled {
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
