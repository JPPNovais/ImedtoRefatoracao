<script setup lang="ts">
/**
 * Ativar2faModal — fluxo de 3 passos para ativar o 2FA TOTP.
 *
 * Passo 1: exibe URI otpauth:// (segredo manual) para escaneio manual no app.
 *          O frontend não gera QR code — briefing permite exibir só a URI e segredo.
 * Passo 2: confirma com código do app (validação TOTP no backend).
 * Passo 3: exibe os 10 códigos de recuperação — mostrados UMA única vez.
 */
import { ref, watch } from "vue"
import { AppModal, AppButton } from "@/components/ui"
import { auth2faService } from "@/services/auth2faService"

const props = defineProps<{ aberto: boolean }>()
const emit = defineEmits<{
    fechar: []
    ativado: []
}>()

type Passo = 1 | 2 | 3

const passo              = ref<Passo>(1)
const carregando         = ref(false)
const codigoConfirmacao  = ref("")
const erro               = ref<string | null>(null)
const otpauthUri         = ref("")
const segredoBase32      = ref("")
const codigosRecuperacao = ref<string[]>([])
const copiado            = ref(false)

async function iniciar() {
    carregando.value = true
    erro.value = null
    try {
        const res = await auth2faService.iniciarAtivacao()
        otpauthUri.value    = res.otpauthUri
        segredoBase32.value = res.segredoBase32
        passo.value = 1
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível iniciar a ativação."
    } finally {
        carregando.value = false
    }
}

async function confirmar() {
    if (!codigoConfirmacao.value) return
    carregando.value = true
    erro.value = null
    try {
        const res = await auth2faService.confirmarAtivacao(codigoConfirmacao.value)
        codigosRecuperacao.value = res.codigosRecuperacao
        passo.value = 3
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Código inválido. Verifique e tente novamente."
    } finally {
        carregando.value = false
    }
}

function aoCopiarSegredo() {
    navigator.clipboard.writeText(segredoBase32.value).catch(() => {})
}

async function aoCopiarCodigos() {
    const texto = codigosRecuperacao.value.join("\n")
    await navigator.clipboard.writeText(texto).catch(() => {})
    copiado.value = true
    setTimeout(() => { copiado.value = false }, 2000)
}

function fecharAposConclussao() {
    emit("ativado")
    emit("fechar")
    resetarEstado()
}

function aoFechar() {
    emit("fechar")
    resetarEstado()
}

function resetarEstado() {
    passo.value = 1
    carregando.value = false
    codigoConfirmacao.value = ""
    erro.value = null
    otpauthUri.value = ""
    segredoBase32.value = ""
    codigosRecuperacao.value = []
    copiado.value = false
}

// Inicia ao abrir
watch(() => props.aberto, (aberto) => {
    if (aberto) iniciar()
})
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Ativar verificação em duas etapas"
        @fechar="aoFechar"
    >
        <!-- ── Passo 1: Escanear / digitar segredo ─────────────────────────── -->
        <template v-if="passo === 1">
            <div v-if="carregando" class="estado-carregando">
                <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                Preparando...
            </div>
            <template v-else-if="otpauthUri">
                <p class="instrucao">
                    Abra seu aplicativo autenticador (Google Authenticator, Authy, etc.)
                    e adicione uma conta manualmente com os dados abaixo.
                </p>

                <div class="uri-box">
                    <span class="uri-label">URI otpauth://</span>
                    <code class="uri-text">{{ otpauthUri }}</code>
                </div>

                <div class="segredo-box">
                    <div class="segredo-row">
                        <div>
                            <span class="uri-label">Chave secreta (Base32)</span>
                            <code class="segredo-code">{{ segredoBase32 }}</code>
                        </div>
                        <button type="button" class="btn-copiar" @click="aoCopiarSegredo">
                            <i class="fa-regular fa-copy" aria-hidden="true"></i>
                            Copiar
                        </button>
                    </div>
                    <p class="segredo-hint">
                        Configuração: algoritmo SHA1 · 6 dígitos · período 30 segundos
                    </p>
                </div>

                <p v-if="erro" class="msg-erro">{{ erro }}</p>
            </template>
        </template>

        <!-- ── Passo 2: Confirmar código ──────────────────────────────────── -->
        <template v-else-if="passo === 2">
            <p class="instrucao">
                Digite o código de 6 dígitos gerado pelo aplicativo para confirmar
                que está tudo configurado corretamente.
            </p>
            <div class="campo-codigo">
                <label class="field-label">Código de verificação</label>
                <input
                    v-model="codigoConfirmacao"
                    class="input-codigo"
                    type="text"
                    inputmode="numeric"
                    maxlength="6"
                    placeholder="000000"
                    autocomplete="one-time-code"
                    @keydown.enter.prevent="confirmar"
                />
            </div>
            <p v-if="erro" class="msg-erro">{{ erro }}</p>
        </template>

        <!-- ── Passo 3: Códigos de recuperação ────────────────────────────── -->
        <template v-else-if="passo === 3">
            <div class="aviso-recuperacao">
                <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                <strong>Guarde estes códigos em local seguro.</strong>
                <span>Eles são mostrados apenas uma vez. Cada código só pode ser usado uma vez para acessar sua conta se perder o autenticador.</span>
            </div>

            <div class="grid-codigos">
                <code
                    v-for="cod in codigosRecuperacao"
                    :key="cod"
                    class="codigo-recuperacao"
                >{{ cod }}</code>
            </div>

            <button type="button" class="btn-copiar btn-copiar--lg" @click="aoCopiarCodigos">
                <i :class="copiado ? 'fa-solid fa-check' : 'fa-regular fa-copy'" aria-hidden="true"></i>
                {{ copiado ? "Copiado!" : "Copiar todos" }}
            </button>
        </template>

        <template #footer>
            <!-- Passo 1 -->
            <template v-if="passo === 1">
                <AppButton variant="secondary" @click="aoFechar">Cancelar</AppButton>
                <AppButton :disabled="!otpauthUri || carregando" @click="passo = 2">
                    Já configurei — próximo
                    <template #icon-right><i class="fa-solid fa-arrow-right" aria-hidden="true"></i></template>
                </AppButton>
            </template>

            <!-- Passo 2 -->
            <template v-else-if="passo === 2">
                <AppButton variant="secondary" @click="passo = 1">Voltar</AppButton>
                <AppButton
                    :loading="carregando"
                    :disabled="codigoConfirmacao.length !== 6"
                    @click="confirmar"
                >
                    Confirmar ativação
                </AppButton>
            </template>

            <!-- Passo 3 -->
            <template v-else-if="passo === 3">
                <AppButton @click="fecharAposConclussao">
                    <i class="fa-solid fa-check" aria-hidden="true"></i>
                    Salvei os códigos — concluir
                </AppButton>
            </template>
        </template>
    </AppModal>
</template>

<style scoped>
.instrucao {
    color: var(--text-muted);
    font-size: var(--text-sm);
    margin: 0 0 1rem;
}

.estado-carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    font-size: var(--text-sm);
    padding: 1rem 0;
}

.uri-box {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    background: var(--bg-accent, hsl(var(--accent)));
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.75rem 1rem;
    margin-bottom: 1rem;
    word-break: break-all;
}

.uri-label {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: var(--text-muted);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.uri-text {
    font-size: var(--text-xs);
    font-family: monospace;
    color: var(--text);
    word-break: break-all;
}

.segredo-box {
    border: 1px solid var(--border-strong);
    border-radius: var(--radius);
    padding: 0.75rem 1rem;
    margin-bottom: 1rem;
}

.segredo-row {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
}

.segredo-code {
    display: block;
    font-family: monospace;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-bold);
    color: var(--text);
    letter-spacing: 0.1em;
    margin-top: 0.25rem;
}

.segredo-hint {
    font-size: var(--text-xs);
    color: var(--text-muted);
    margin: 0.5rem 0 0;
}

.btn-copiar {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.35rem 0.75rem;
    border-radius: var(--radius);
    border: 1px solid var(--border-strong);
    background: var(--bg-card);
    color: var(--text);
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    cursor: pointer;
    white-space: nowrap;
    flex-shrink: 0;
    transition: border-color 0.15s;
}

.btn-copiar:hover {
    border-color: hsl(var(--primary));
    color: hsl(var(--primary));
}

.btn-copiar--lg {
    padding: 0.5rem 1rem;
    font-size: var(--text-sm);
    margin-top: 0.75rem;
}

.campo-codigo {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    margin-bottom: 1rem;
}

.field-label {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: var(--text-muted);
}

.input-codigo {
    padding: 0.6rem 0.9rem;
    border: 1px solid var(--border-strong);
    border-radius: var(--radius);
    font-family: monospace;
    font-size: var(--text-base);
    letter-spacing: 0.25em;
    color: var(--text);
    background: var(--bg-card);
    width: 160px;
    transition: border-color 0.15s;
}

.input-codigo:focus {
    outline: none;
    border-color: hsl(var(--primary));
}

.aviso-recuperacao {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
    background: hsl(var(--warning) / 0.08);
    border: 1px solid hsl(var(--warning) / 0.3);
    border-radius: var(--radius);
    padding: 0.875rem 1rem;
    margin-bottom: 1rem;
    font-size: var(--text-sm);
    color: hsl(28 90% 35%);
}

.aviso-recuperacao i {
    color: hsl(var(--warning));
    font-size: var(--text-base);
    margin-bottom: 0.15rem;
}

.grid-codigos {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 0.4rem;
}

.codigo-recuperacao {
    font-family: monospace;
    font-size: var(--text-sm);
    letter-spacing: 0.08em;
    padding: 0.4rem 0.6rem;
    background: var(--bg-accent, hsl(var(--accent)));
    border: 1px solid var(--border);
    border-radius: var(--radius);
    color: var(--text);
    text-align: center;
}

.msg-erro {
    color: var(--danger);
    font-size: var(--text-sm);
    margin: 0;
}
</style>
