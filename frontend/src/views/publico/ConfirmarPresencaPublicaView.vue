<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from "vue"
import { useRoute } from "vue-router"
import { AppButton, AppToast } from "@/components/ui"
import {
    agendamentoConfirmacaoPublicaService,
    type ConfirmacaoPublicaDto,
} from "@/services/agendamentoConfirmacaoPublicaService"
import imedtoLogo from "@/assets/imedto-logo.png"

/**
 * View pública de confirmação de presença em agendamento (Fase 2).
 *
 * Rota: `/agendamentos/confirmar/:token` — anônima, fora do AppLayout.
 *
 * Estados:
 *   carregando:    GET inicial em andamento.
 *   valido:        resumo carregado, aguardando paciente confirmar.
 *   confirmado:    presença confirmada (POST com sucesso ou idempotência).
 *   expirado:      410 do backend (link inválido/expirado/cancelado) — genérico.
 *   erro:          falha técnica (rede, 5xx).
 *
 * LGPD (CA17/CA23): sem nome do paciente, CPF, e-mail, paciente_id ou estabelecimento_id.
 * Sem login, sem menu — mobile-first.
 */
type Estado = "carregando" | "valido" | "confirmado" | "expirado" | "erro"

const route = useRoute()

const token   = computed(() => String(route.params.token || ""))
const estado  = ref<Estado>("carregando")
const resumo  = ref<ConfirmacaoPublicaDto | null>(null)
const enviando = ref(false)

const toast = ref<{ msg: string; variante: "info" | "success" | "error" } | null>(null)
const h1Ref = ref<HTMLElement | null>(null)

onMounted(async () => {
    await carregar()
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
        resumo.value = await agendamentoConfirmacaoPublicaService.consultar(token.value)
        estado.value = "valido"
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

async function confirmarPresenca() {
    if (enviando.value) return
    enviando.value = true
    try {
        const r = await agendamentoConfirmacaoPublicaService.confirmar(token.value)
        // CA20: "confirmado" ou "ja_confirmado" → ambos levam para estado "confirmado".
        estado.value = "confirmado"
    } catch (e: any) {
        const status = e?.response?.status
        if (status === 410) {
            estado.value = "expirado"
        } else if (status === 429) {
            toast.value = { msg: "Muitas tentativas. Aguarde alguns instantes.", variante: "error" }
        } else {
            toast.value = {
                msg: e?.response?.data?.mensagem ?? "Não foi possível confirmar sua presença. Tente novamente.",
                variante: "error",
            }
        }
    } finally {
        enviando.value = false
    }
}

function fmtData(iso: string | null | undefined): string {
    if (!iso) return "—"
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return "—"
    return d.toLocaleString("pt-BR", {
        day:    "2-digit",
        month:  "2-digit",
        year:   "numeric",
        hour:   "2-digit",
        minute: "2-digit",
        timeZone: "America/Sao_Paulo",
    })
}
</script>

<template>
    <div class="aceite-page">
        <header class="aceite-topbar">
            <img :src="imedtoLogo" alt="Imedto" class="aceite-logo" />
            <span class="aceite-topbar-titulo">Confirmação de presença</span>
        </header>

        <main class="app-page app-page--narrow aceite-main">
            <!-- Carregando -->
            <section v-if="estado === 'carregando'" class="aceite-card aceite-feedback">
                <div class="aceite-spinner" aria-hidden="true"></div>
                <p>Carregando…</p>
            </section>

            <!-- Erro técnico -->
            <section v-else-if="estado === 'erro'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-exclamation aceite-icone aceite-icone--erro" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">Não foi possível carregar este link</h1>
                <p>Tente novamente em alguns instantes.</p>
                <AppButton variant="secondary" icon="fa-solid fa-rotate" @click="carregar">
                    Tentar novamente
                </AppButton>
            </section>

            <!-- Link inválido / expirado / cancelado — CA19: mensagem genérica -->
            <section v-else-if="estado === 'expirado'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-exclamation aceite-icone aceite-icone--warn" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">Link inválido ou expirado</h1>
                <p>Este link não é mais válido. Entre em contato com o estabelecimento.</p>
                <a href="https://imedto.com" class="aceite-link-fora">Voltar para o início</a>
            </section>

            <!-- Presença confirmada — CA18/CA20 -->
            <section v-else-if="estado === 'confirmado'" class="aceite-card aceite-feedback">
                <i class="fa-solid fa-circle-check aceite-icone aceite-icone--ok" aria-hidden="true"></i>
                <h1 ref="h1Ref" tabindex="-1">Presença confirmada</h1>
                <p>Sua presença foi confirmada. Você pode fechar esta página.</p>
            </section>

            <!-- Válido: resumo + botão confirmar — CA17/CA25 -->
            <template v-else-if="estado === 'valido' && resumo">
                <article class="aceite-card aceite-conteudo">
                    <header class="aceite-cabecalho">
                        <h1 ref="h1Ref" tabindex="-1">Confirme sua presença</h1>
                        <dl class="aceite-meta">
                            <div>
                                <dt>Estabelecimento</dt>
                                <dd>{{ resumo.estabelecimentoNome }}</dd>
                            </div>
                            <div>
                                <dt>Profissional</dt>
                                <dd>{{ resumo.profissionalNome }}</dd>
                            </div>
                            <div>
                                <dt>Serviço</dt>
                                <dd>{{ resumo.tipoServico }}</dd>
                            </div>
                            <div>
                                <dt>Data e hora</dt>
                                <dd>{{ fmtData(resumo.inicioPrevisto) }}</dd>
                            </div>
                        </dl>
                    </header>
                </article>

                <section class="aceite-card aceite-form" aria-label="Confirmação de presença">
                    <p class="aceite-instrucao">
                        Confirme sua presença clicando no botão abaixo.
                    </p>
                    <div class="aceite-acoes">
                        <AppButton
                            icon="fa-solid fa-check"
                            :loading="enviando"
                            :disabled="enviando"
                            aria-label="Confirmar presença"
                            @click="confirmarPresenca"
                        >
                            Confirmar presença
                        </AppButton>
                    </div>
                </section>
            </template>
        </main>

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
}
.aceite-cabecalho h1 {
    margin: 0 0 12px;
    font-size: var(--text-2xl);
    font-weight: var(--font-weight-bold);
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
.aceite-meta div  { display: flex; flex-direction: column; gap: 2px; }
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

.aceite-form {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.aceite-instrucao {
    margin: 0;
    font-size: 14px;
    color: hsl(var(--secondary) / 0.8);
    line-height: 1.5;
}

.aceite-acoes {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    border-top: 1px solid hsl(var(--secondary) / 0.08);
    padding-top: 16px;
}

/* Estados de feedback */
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
    font-size: var(--text-lg);
    font-weight: var(--font-weight-bold);
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

.aceite-icone { font-size: 44px; }
.aceite-icone--ok   { color: hsl(160 79% 32%); }
.aceite-icone--erro { color: hsl(var(--error)); }
.aceite-icone--warn { color: hsl(var(--warning)); }

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
    .aceite-acoes { flex-direction: column; }
    .aceite-acoes :deep(button) { width: 100%; justify-content: center; }
}
</style>
