<script setup lang="ts">
/**
 * ModalRevelarCpf — modal que pede motivo (mín. 10 chars) antes de revelar o CPF do dono.
 * CA17–CA19: motivo obrigatório, audit gerado no backend, resultado exibido inline.
 */
import { ref, computed } from "vue"

const props = defineProps<{
    estabelecimentoId: number
    open: boolean
}>()

const emit = defineEmits<{
    close: []
    revelado: [cpf: string]
}>()

const motivo = ref("")
const carregando = ref(false)
const erro = ref<string | null>(null)

const motivoValido = computed(() => motivo.value.trim().length >= 10)

function fechar() {
    motivo.value = ""
    erro.value = null
    emit("close")
}

async function confirmar() {
    if (!motivoValido.value) {
        erro.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    emit("revelado", motivo.value.trim())
    fechar()
}
</script>

<template>
    <Teleport to="body">
        <div v-if="open" class="modal-overlay" @click.self="fechar">
            <div class="modal-box" role="dialog" aria-modal="true" aria-labelledby="modal-revelar-titulo">
                <h2 id="modal-revelar-titulo" class="modal-titulo">Revelar CPF do dono</h2>
                <p class="modal-desc">
                    Esta ação fica registrada em audit. Informe o motivo da consulta.
                </p>

                <div class="modal-campo">
                    <label for="motivo-revelar" class="modal-label">Motivo <span class="req">*</span></label>
                    <textarea
                        id="motivo-revelar"
                        v-model="motivo"
                        class="modal-textarea"
                        rows="3"
                        placeholder="Ex: Validação de cadastro do parceiro (mín. 10 caracteres)"
                        :disabled="carregando"
                    />
                    <p v-if="erro" class="modal-erro">{{ erro }}</p>
                </div>

                <div class="modal-acoes">
                    <button class="btn-cancelar" type="button" @click="fechar" :disabled="carregando">Cancelar</button>
                    <button
                        class="btn-confirmar"
                        type="button"
                        @click="confirmar"
                        :disabled="!motivoValido || carregando"
                    >
                        Revelar CPF
                    </button>
                </div>
            </div>
        </div>
    </Teleport>
</template>

<style scoped>
.modal-overlay {
    position: fixed; inset: 0;
    background: rgba(0,0,0,0.55);
    display: flex; align-items: center; justify-content: center;
    z-index: 1000;
}
.modal-box {
    background: hsl(var(--card)); border-radius: 10px; padding: 28px;
    max-width: 440px; width: 100%; box-shadow: 0 8px 32px rgba(0,0,0,0.18);
}
.modal-titulo { font-size: 18px; font-weight: 700; margin: 0 0 8px; }
.modal-desc { font-size: 13px; color: hsl(var(--muted-foreground)); margin: 0 0 18px; }
.modal-campo { display: flex; flex-direction: column; gap: 6px; margin-bottom: 20px; }
.modal-label { font-size: 13px; font-weight: 600; }
.req { color: hsl(var(--destructive)); }
.modal-textarea {
    padding: 8px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; resize: vertical; font-family: inherit;
}
.modal-textarea:focus { outline: none; border-color: hsl(var(--primary)); box-shadow: 0 0 0 2px hsl(var(--primary))33; }
.modal-erro { color: hsl(var(--destructive)); font-size: 12px; margin: 0; }
.modal-acoes { display: flex; justify-content: flex-end; gap: 10px; }
.btn-cancelar {
    padding: 8px 16px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    background: hsl(var(--card)); font-size: 13px; cursor: pointer;
}
.btn-confirmar {
    padding: 8px 16px; border: none; border-radius: 6px;
    background: hsl(var(--primary)); color: hsl(var(--primary-foreground)); font-size: 13px;
    font-weight: 600; cursor: pointer;
}
.btn-confirmar:disabled { opacity: 0.5; cursor: not-allowed; }
</style>
