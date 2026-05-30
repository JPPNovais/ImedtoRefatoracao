<script setup lang="ts">
/**
 * ModalResetTenant — confirmação dupla para reset de dados do tenant (CA32–CA36).
 *
 * Etapas:
 * 1. Admin digita exatamente o nome fantasia do estabelecimento.
 * 2. Admin preenche motivo (mín. 10 chars).
 * 3. Admin marca checkbox "Entendo que é irreversível".
 */
import { ref, computed } from "vue"

const props = defineProps<{
    open: boolean
    nomeFantasia: string
    carregando: boolean
    erro: string | null
}>()

const emit = defineEmits<{
    close: []
    confirmar: [motivo: string, confirmarNomeFantasia: string]
}>()

const nomeDigitado = ref("")
const motivo = ref("")
const ciente = ref(false)

const nomeConfere = computed(
    () => nomeDigitado.value.trim().toLowerCase() === props.nomeFantasia.trim().toLowerCase(),
)
const motivoValido = computed(() => motivo.value.trim().length >= 10)
const podeSalvar = computed(() => nomeConfere.value && motivoValido.value && ciente.value && !props.carregando)

function fechar() {
    nomeDigitado.value = ""
    motivo.value = ""
    ciente.value = false
    emit("close")
}

function confirmar() {
    if (!podeSalvar.value) return
    emit("confirmar", motivo.value.trim(), nomeDigitado.value.trim())
}
</script>

<template>
    <Teleport to="body">
        <div v-if="open" class="modal-overlay" @click.self="fechar">
            <div class="modal-box" role="dialog" aria-modal="true" aria-labelledby="modal-reset-titulo">
                <div class="modal-danger-strip">AÇÃO IRREVERSÍVEL</div>

                <h2 id="modal-reset-titulo" class="modal-titulo">Resetar dados do estabelecimento</h2>
                <p class="modal-desc">
                    Esta ação remove <strong>permanentemente</strong> todos os dados operacionais
                    (pacientes, prontuários, agendamentos, financeiro, etc.) do estabelecimento
                    <strong>{{ nomeFantasia }}</strong>. A casca (conta, vínculos, plano) é preservada.
                </p>

                <div class="modal-campo">
                    <label for="reset-nome" class="modal-label">
                        Digite o nome fantasia exato para confirmar <span class="req">*</span>
                    </label>
                    <input
                        id="reset-nome"
                        v-model="nomeDigitado"
                        type="text"
                        class="modal-input"
                        :placeholder="nomeFantasia"
                        :disabled="carregando"
                        autocomplete="off"
                    />
                    <p v-if="nomeDigitado && !nomeConfere" class="modal-erro">Nome não confere.</p>
                </div>

                <div class="modal-campo">
                    <label for="reset-motivo" class="modal-label">
                        Motivo do reset <span class="req">*</span>
                    </label>
                    <textarea
                        id="reset-motivo"
                        v-model="motivo"
                        class="modal-textarea"
                        rows="3"
                        placeholder="Ex: Cliente solicitou reset completo antes do golive (mín. 10 caracteres)"
                        :disabled="carregando"
                    />
                </div>

                <div class="modal-campo modal-campo--check">
                    <input id="reset-ciente" v-model="ciente" type="checkbox" :disabled="carregando" />
                    <label for="reset-ciente" class="modal-label-check">
                        Entendo que esta ação é <strong>irreversível</strong> e fui autorizado a executá-la.
                    </label>
                </div>

                <p v-if="erro" class="modal-erro modal-erro--global">{{ erro }}</p>

                <div class="modal-acoes">
                    <button class="btn-cancelar" type="button" @click="fechar" :disabled="carregando">Cancelar</button>
                    <button
                        class="btn-perigo"
                        type="button"
                        @click="confirmar"
                        :disabled="!podeSalvar"
                    >
                        <span v-if="carregando">Resetando...</span>
                        <span v-else>Confirmar reset</span>
                    </button>
                </div>
            </div>
        </div>
    </Teleport>
</template>

<style scoped>
.modal-overlay {
    position: fixed; inset: 0;
    background: rgba(0,0,0,0.65);
    display: flex; align-items: center; justify-content: center;
    z-index: 1000;
}
.modal-box {
    background: hsl(var(--card)); border-radius: 10px; padding: 0;
    max-width: 480px; width: 100%; box-shadow: 0 8px 32px rgba(0,0,0,0.22);
    overflow: hidden;
}
.modal-danger-strip {
    background: hsl(var(--destructive)); color: hsl(var(--primary-foreground));
    padding: 6px 24px;
    font-size: 11px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase;
}
.modal-titulo { font-size: 18px; font-weight: 700; margin: 20px 24px 8px; }
.modal-desc { font-size: 13px; color: hsl(var(--foreground)); margin: 0 24px 20px; line-height: 1.5; }
.modal-campo { display: flex; flex-direction: column; gap: 6px; margin: 0 24px 16px; }
.modal-campo--check { flex-direction: row; align-items: flex-start; gap: 10px; }
.modal-label { font-size: 13px; font-weight: 600; }
.modal-label-check { font-size: 13px; line-height: 1.4; }
.req { color: hsl(var(--destructive)); }
.modal-input, .modal-textarea {
    padding: 8px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; font-family: inherit;
}
.modal-input:focus, .modal-textarea:focus {
    outline: none; border-color: hsl(var(--destructive)); box-shadow: 0 0 0 2px hsl(var(--destructive))33;
}
.modal-textarea { resize: vertical; }
.modal-erro { color: hsl(var(--destructive)); font-size: 12px; margin: 0; }
.modal-erro--global { margin: 0 24px 12px; font-size: 13px; }
.modal-acoes {
    display: flex; justify-content: flex-end; gap: 10px;
    padding: 16px 24px; border-top: 1px solid hsl(var(--muted) / 0.4); margin-top: 4px;
}
.btn-cancelar {
    padding: 8px 16px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    background: hsl(var(--card)); font-size: 13px; cursor: pointer;
}
.btn-perigo {
    padding: 8px 18px; border: none; border-radius: 6px;
    background: hsl(var(--destructive)); color: hsl(var(--primary-foreground)); font-size: 13px;
    font-weight: 700; cursor: pointer;
}
.btn-perigo:disabled { opacity: 0.45; cursor: not-allowed; }
</style>
