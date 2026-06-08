<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from "vue"
import { useRoute } from "vue-router"
import { AppButton, AppInput, AppConfirmDialog, AppToast } from "@/components/ui"
import {
    termoAceitePublicoService,
    type TermoPublicoDto,
} from "@/services/termoAceitePublicoService"
import imedtoLogo from "@/assets/imedto-logo.png"

/**
 * View pública de aceite/recusa de termo (Fase 4).
 *
 * Rota: `/termos/aceite/:token` — anônima, fora do AppLayout.
 *
 * Estados:
 *   carregando: GET inicial em andamento.
 *   pronto:     conteúdo do termo carregado, aguardando paciente decidir.
 *   sucesso:    resposta registrada nesta sessão (aceito ou recusado).
 *   ja_respondido: o backend devolveu idempotência — termo já estava decidido.
 *   expirado:   410 do backend (link inválido/expirado/revogado).
 *   erro:       falha técnica (rede, 5xx, etc).
 *
 * LGPD: o backend retorna apenas título do modelo, nome do estabelecimento, nome
 * do profissional emissor, data de emissão e conteúdo HTML (já sanitizado).
 * Nenhum dado do paciente (CPF, e-mail, nome completo) chega aqui pela rede.
 * O campo "nomeConfirmado" é digitado pelo próprio paciente e validado server-side.
 */
type Estado = "carregando" | "pronto" | "sucesso" | "ja_respondido" | "expirado" | "erro"

const route = useRoute()

const token = computed(() => String(route.params.token || ""))
const estado = ref<Estado>("carregando")
const termo = ref<TermoPublicoDto | null>(null)

// Form de aceite/recusa
const checkLi = ref(false)
const nomeConfirmado = ref("")
const erroNome = ref<string | null>(null)
const enviando = ref(false)

// Mensagem final após resposta
const resultadoMensagem = ref("")
const recusaConfirmando = ref(false)

// Toast (rate-limit, erros transitórios)
const toast = ref<{ msg: string; variante: "info" | "success" | "error" } | null>(null)

const h1Ref = ref<HTMLElement | null>(null)

onMounted(async () => {
    await carregar()
    // Foco inicial no h1 quando o conteúdo estiver pronto — acessibilidade.
    await nextTick()
    h1Ref.value?.focus()
})

async function carregar() {
    if (!token.value) {
        estado.value = "expirado"
        return
    }
    estado.value = "carregando"
    try {
        termo.value = await termoAceitePublicoService.obter(token.value)
        estado.value = "pronto"
    } catch (e: any) {
        const status = e?.response?.status
        if (status === 410) {
            estado.value = "expirado"
        } else if (status === 429) {
            estado.value = "erro"
            toast.value = { msg: "Muitas tentativas. Aguarde alguns instantes e tente novamente.", variante: "error" }
        } else {
            estado.value = "erro"
        }
    }
}

async function enviarResposta(aceito: boolean) {
    if (enviando.value) return
    erroNome.value = null
    enviando.value = true
    try {
        const r = await termoAceitePublicoService.responder(token.value, {
            aceito,
            nomeConfirmado: nomeConfirmado.value.trim() || undefined,
        })
        if (r.resultado === "ja_respondido") {
            estado.value = "ja_respondido"
            resultadoMensagem.value = r.mensagem
        } else {
            estado.value = "sucesso"
            resultadoMensagem.value = r.mensagem
        }
    } catch (e: any) {
        const status = e?.response?.status
        if (status === 410) {
            // Idempotência levou para "já respondido" no servidor (raríssimo —
            // controller já trata isso como 200), mas defensivamente: estado expirado.
            estado.value = "expirado"
        } else if (status === 422) {
            // Único 422 esperado aqui é "nome não confere". Mensagem inline no campo.
            erroNome.value = e?.response?.data?.mensagem ?? "Nome não confere com o cadastro."
        } else if (status === 429) {
            toast.value = { msg: "Muitas tentativas. Aguarde alguns instantes.", variante: "error" }
        } else {
            toast.value = {
                msg: e?.response?.data?.mensagem ?? "Não foi possível registrar sua resposta. Tente novamente.",
                variante: "error",
            }
        }
    } finally {
        enviando.value = false
    }
}

const podeAceitar = computed(() => checkLi.value && !enviando.value)

function aceitar() {
    if (!podeAceitar.value) return
    void enviarResposta(true)
}

function abrirConfirmacaoRecusa() {
    recusaConfirmando.value = true
}

function confirmarRecusa() {
    void enviarResposta(false)
}

function fmtEmitidoEm(iso: string | null | undefined): string {
    if (!iso) return "—"
    const d = new Date(iso)
    return Number.isNaN(d.getTime())
        ? "—"
        : d.toLocaleString("pt-BR", {
            day:   "2-digit",
            month: "2-digit",
            year:  "numeric",
            hour:  "2-digit",
            minute:"2-digit",
        })
}
</script>

<template>
    <div class="aceite-page">
        <header class="aceite-topbar">
            <img :src="imedtoLogo" alt="Imedto" class="aceite-logo" />
            <span class="aceite-topbar-titulo">Termo de consentimento</span>
        </header>

        <main class="app-page app-page--narrow aceite-main">
            <!-- Carregando -->
            <section v-if="estado === 'carregando'" class="aceite-card aceite-feedback">
                <div class="aceite-spinner" aria-hidden="true"></div>
                <p>Carregando termo…</p>
            </section>

            <!-- Erro técnico -->
            <section v-else-if="estado === 'erro'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-exclamation aceite-icone aceite-icone--erro" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">Não foi possível carregar este termo</h1>
                <p>Tente novamente em alguns instantes.</p>
                <AppButton variant="secondary" icon="fa-solid fa-rotate" @click="carregar">
                    Tentar novamente
                </AppButton>
            </section>

            <!-- Expirado / inválido / já respondido (do GET) -->
            <section v-else-if="estado === 'expirado'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-exclamation aceite-icone aceite-icone--warn" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">Este link expirou ou já foi respondido</h1>
                <p>Entre em contato com o estabelecimento se precisar de um novo link.</p>
                <a href="https://imedto.com" class="aceite-link-fora">Voltar para o início</a>
            </section>

            <!-- Já respondido (resposta veio do POST com idempotência) -->
            <section v-else-if="estado === 'ja_respondido'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-info aceite-icone aceite-icone--info" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">Termo já respondido</h1>
                <p>{{ resultadoMensagem || "Você pode fechar esta página." }}</p>
            </section>

            <!-- Sucesso -->
            <section v-else-if="estado === 'sucesso'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-check aceite-icone aceite-icone--ok" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">{{ resultadoMensagem.includes("aceito") ? "Termo aceito" : "Recusa registrada" }}</h1>
                <p>{{ resultadoMensagem }}</p>
                <p v-if="resultadoMensagem.includes('aceito')" class="aceite-subtexto">
                    Uma cópia será enviada ao estabelecimento.
                </p>
            </section>

            <!-- Pronto: termo a ser respondido -->
            <template v-else-if="estado === 'pronto' && termo">
                <article class="aceite-card aceite-conteudo">
                    <header class="aceite-cabecalho">
                        <h1 ref="h1Ref" tabindex="-1">{{ termo.tituloModelo }}</h1>
                        <dl class="aceite-meta">
                            <div>
                                <dt>Estabelecimento</dt>
                                <dd>{{ termo.estabelecimentoNome }}</dd>
                            </div>
                            <div>
                                <dt>Profissional responsável</dt>
                                <dd>{{ termo.profissionalEmissor }}</dd>
                            </div>
                            <div>
                                <dt>Data de emissão</dt>
                                <dd>{{ fmtEmitidoEm(termo.emitidoEm) }}</dd>
                            </div>
                        </dl>
                    </header>

                    <div class="aceite-corpo termo-aceite-conteudo" v-html="termo.conteudoSnapshotHtml"></div>
                </article>

                <section class="aceite-card aceite-form" aria-label="Confirmação de aceite">
                    <label class="aceite-check">
                        <input v-model="checkLi" type="checkbox" :disabled="enviando" />
                        <span>Li e estou de acordo com os termos acima.</span>
                    </label>

                    <div class="aceite-nome">
                        <label for="nome-confirmado">Confirme seu nome completo <span class="aceite-opcional">(opcional)</span></label>
                        <AppInput
                            id="nome-confirmado"
                            v-model="nomeConfirmado"
                            placeholder="Como aparece no seu cadastro"
                            :disabled="enviando"
                            autocomplete="name"
                        />
                        <p v-if="erroNome" class="aceite-erro" role="alert">{{ erroNome }}</p>
                    </div>

                    <div class="aceite-acoes">
                        <AppButton
                            variant="secondary"
                            :disabled="enviando"
                            icon="fa-solid fa-xmark"
                            aria-label="Recusar termo"
                            @click="abrirConfirmacaoRecusa"
                        >
                            Não aceito
                        </AppButton>
                        <AppButton
                            icon="fa-solid fa-check"
                            :loading="enviando"
                            :disabled="!podeAceitar"
                            aria-label="Aceitar termo"
                            @click="aceitar"
                        >
                            Aceito
                        </AppButton>
                    </div>
                </section>
            </template>
        </main>

        <!-- Diálogo de confirmação de recusa -->
        <AppConfirmDialog
            v-model:aberto="recusaConfirmando"
            titulo="Confirma a recusa deste termo?"
            mensagem="Esta ação não pode ser desfeita. O estabelecimento será notificado."
            confirmar-rotulo="Recusar termo"
            variante="danger"
            icone="fa-solid fa-xmark"
            :executando="enviando"
            @confirmar="confirmarRecusa"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.msg"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.aceite-page {
    min-height: 100vh;
    background: hsl(var(--muted) / 0.4);
    display: flex;
    flex-direction: column;
}

.aceite-topbar {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 14px 24px;
    background: white;
    border-bottom: 1px solid hsl(var(--secondary) / 0.1);
    box-shadow: 0 1px 0 rgb(0 0 0 / 0.02);
}
.aceite-logo {
    height: 28px;
    width: auto;
}
.aceite-topbar-titulo {
    font-size: 13px;
    font-weight: 600;
    color: hsl(var(--secondary) / 0.7);
    letter-spacing: 0.01em;
}

.aceite-main {
    padding-top: 24px;
    padding-bottom: 40px;
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.aceite-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    padding: 24px;
    box-shadow: 0 4px 16px -8px rgb(0 0 0 / 0.04);
}

.aceite-cabecalho {
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    padding-bottom: 16px;
    margin-bottom: 16px;
}
.aceite-cabecalho h1 {
    margin: 0 0 12px;
    font-size: 20px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    line-height: 1.3;
    outline: none;
}
.aceite-cabecalho h1:focus-visible {
    outline: 2px solid hsl(var(--primary) / 0.45);
    outline-offset: 4px;
    border-radius: 4px;
}

.aceite-meta {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 10px 24px;
    margin: 0;
}
.aceite-meta div { display: flex; flex-direction: column; gap: 2px; }
.aceite-meta dt {
    font-size: 10.5px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    font-weight: 700;
    color: hsl(var(--secondary) / 0.55);
}
.aceite-meta dd {
    margin: 0;
    font-size: 13.5px;
    color: hsl(var(--foreground));
    font-weight: 600;
}

.aceite-corpo {
    font-size: 14px;
    line-height: 1.7;
    color: hsl(var(--foreground));
    word-break: break-word;
}
.aceite-corpo :deep(p)  { margin: 0 0 10px; }
.aceite-corpo :deep(h1) { font-size: var(--text-lg); color: hsl(var(--primary-dark)); margin: 14px 0 8px; }
.aceite-corpo :deep(h2) { font-size: var(--text-md); color: hsl(var(--primary-dark)); margin: 14px 0 8px; }
.aceite-corpo :deep(h3) { font-size: var(--text-base); color: hsl(var(--primary-dark)); margin: 12px 0 6px; }
.aceite-corpo :deep(ul),
.aceite-corpo :deep(ol) { margin: 0 0 10px 22px; }
.aceite-corpo :deep(strong) { font-weight: 700; }
.aceite-corpo :deep(em) { font-style: italic; }

/* Formulário de aceite */
.aceite-form {
    display: flex;
    flex-direction: column;
    gap: 16px;
}
.aceite-check {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    padding: 12px 14px;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 8px;
    cursor: pointer;
    background: hsl(var(--muted) / 0.4);
    transition: border 120ms;
}
.aceite-check:hover { border-color: hsl(var(--primary) / 0.4); }
.aceite-check input {
    margin-top: 3px;
    accent-color: hsl(var(--primary));
}
.aceite-check span {
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--foreground));
}

.aceite-nome {
    display: flex;
    flex-direction: column;
    gap: 6px;
}
.aceite-nome label {
    font-size: 12.5px;
    font-weight: 600;
    color: hsl(var(--secondary));
}
.aceite-opcional {
    color: hsl(var(--secondary) / 0.55);
    font-weight: 500;
}
.aceite-erro {
    margin: 0;
    color: hsl(var(--error));
    font-size: 12.5px;
    font-weight: 600;
}

.aceite-acoes {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    border-top: 1px solid hsl(var(--secondary) / 0.08);
    padding-top: 16px;
}

/* Estados de feedback (carregando/sucesso/expirado/erro) */
.aceite-feedback {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    gap: 12px;
    padding: 40px 24px;
}
.aceite-feedback h1 {
    margin: 0;
    font-size: 18px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    outline: none;
}
.aceite-feedback h1:focus-visible {
    outline: 2px solid hsl(var(--primary) / 0.45);
    outline-offset: 4px;
    border-radius: 4px;
}
.aceite-feedback p {
    margin: 0;
    font-size: 14px;
    color: hsl(var(--secondary) / 0.75);
    line-height: 1.5;
    max-width: 480px;
}
.aceite-subtexto {
    font-size: 12.5px !important;
    color: hsl(var(--secondary) / 0.6) !important;
}

.aceite-icone { font-size: 44px; }
.aceite-icone--ok   { color: hsl(160 79% 32%); }
.aceite-icone--erro { color: hsl(var(--error)); }
.aceite-icone--warn { color: hsl(var(--warning)); }
.aceite-icone--info { color: hsl(var(--info, 220 70% 50%)); }

.aceite-link-fora {
    margin-top: 8px;
    color: hsl(var(--primary));
    font-size: 13px;
    font-weight: 600;
    text-decoration: none;
}
.aceite-link-fora:hover { text-decoration: underline; }

.aceite-spinner {
    width: 36px;
    height: 36px;
    border: 3px solid hsl(var(--secondary) / 0.15);
    border-top-color: hsl(var(--primary));
    border-radius: 50%;
    animation: spin 800ms linear infinite;
}
@keyframes spin {
    to { transform: rotate(360deg); }
}

@media (max-width: 640px) {
    .aceite-meta { grid-template-columns: 1fr; }
    .aceite-card { padding: 18px; }
    .aceite-acoes { flex-direction: column-reverse; }
    .aceite-acoes :deep(button) { width: 100%; justify-content: center; }
}
</style>
