<script setup lang="ts">
/**
 * LoginView — split layout (formulário à esquerda, painel da marca à direita)
 * fiel ao design Anthropic/Login.html.
 *
 * Modos: login | cadastro | recuperar (mantidos do fluxo anterior).
 * Em desktop: 2 colunas; em mobile (≤ 960px): só formulário, painel some.
 */
import { computed, ref } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import httpClient from "@/services/httpClient"
import imedtoLogo from "@/assets/imedto-logo.png"

const router = useRouter()
const auth = useAuthStore()

type Modo = "login" | "cadastro" | "recuperar"

const modo = ref<Modo>("login")
const email = ref("")
const senha = ref("")
const senhaConfirm = ref("")
const aceitouTermos = ref(false)
const lembrarMe = ref(true)
const resetEmail = ref("")
const mostrarSenha = ref(false)
const mostrarSenhaConfirm = ref(false)
const carregando = ref(false)
const erro = ref<string | null>(null)
const sucesso = ref<string | null>(null)

const senhaForca = computed(() => {
    if (modo.value !== "cadastro" || !senha.value) return null
    let s = 0
    if (senha.value.length >= 8) s++
    if (/[a-z]/.test(senha.value)) s++
    if (/[A-Z]/.test(senha.value)) s++
    if (/[0-9]/.test(senha.value)) s++
    if (/[^a-zA-Z0-9]/.test(senha.value)) s++
    if (s <= 2) return { label: "Senha fraca", cor: "hsl(var(--error))", pct: "33%" }
    if (s <= 4) return { label: "Senha média", cor: "hsl(var(--warning))", pct: "66%" }
    return { label: "Senha forte", cor: "hsl(var(--success))", pct: "100%" }
})

const formularioValido = computed(() => {
    if (modo.value === "recuperar") return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(resetEmail.value)
    if (!email.value || !senha.value) return false
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) return false
    if (modo.value === "cadastro") {
        return senha.value === senhaConfirm.value && senhaConfirm.value.length > 0 && aceitouTermos.value
    }
    return true
})

const titulo = computed(() => {
    switch (modo.value) {
        case "cadastro":  return "Crie sua conta"
        case "recuperar": return "Recuperar senha"
        default:          return "Bem-vindo de volta"
    }
})
const subtitulo = computed(() => {
    switch (modo.value) {
        case "cadastro":  return "Cadastre-se para começar a organizar a agenda, prontuários e financeiro da sua clínica."
        case "recuperar": return "Digite seu e-mail e enviaremos instruções para redefinir sua senha."
        default:          return "Entre na sua conta para acessar a agenda, prontuários e financeiro da sua clínica."
    }
})

async function enviar() {
    erro.value = null
    sucesso.value = null
    carregando.value = true
    try {
        if (modo.value === "login") {
            await auth.login(email.value, senha.value)
            router.push({ name: "Home" })
        } else if (modo.value === "cadastro") {
            try {
                await auth.signup(email.value, senha.value)
                router.push({ name: "Home" })
            } catch (err: any) {
                if (err?.requerConfirmacaoEmail) {
                    modo.value = "login"
                    sucesso.value = "Conta criada! Confirme o e-mail antes de entrar."
                } else {
                    throw err
                }
            }
        } else {
            await httpClient.post("/auth/forgot-password", { email: resetEmail.value })
            sucesso.value = "Se o e-mail existir, você receberá as instruções em breve."
            setTimeout(() => irPara("login"), 3000)
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Ocorreu um erro. Tente novamente."
    } finally {
        carregando.value = false
    }
}

function irPara(m: Modo) {
    modo.value = m
    erro.value = null
    sucesso.value = null
    resetEmail.value = ""
}
</script>

<template>
    <main class="auth-shell">
        <!-- ── Form side (esquerda) ── -->
        <section class="auth-form-side">
            <div class="auth-form-wrap">
                <img :src="imedtoLogo" alt="Imedto" class="auth-logo-img" />

                <form class="auth-form" @submit.prevent="enviar">
                <h1>{{ titulo }}</h1>
                <p class="auth-sub">{{ subtitulo }}</p>

                <div v-if="sucesso" class="alerta alerta--sucesso">{{ sucesso }}</div>
                <div v-if="erro" class="alerta alerta--erro">{{ erro }}</div>

                <!-- Modo recuperar -->
                <template v-if="modo === 'recuperar'">
                    <div class="field">
                        <label for="reset-email">E-mail</label>
                        <div class="input-wrap">
                            <i class="fa-regular fa-envelope" aria-hidden="true"></i>
                            <input
                                id="reset-email"
                                v-model="resetEmail"
                                type="email"
                                placeholder="seu@email.com"
                                autocomplete="email"
                                required
                            />
                        </div>
                    </div>
                </template>

                <!-- Modo login / cadastro -->
                <template v-else>
                    <div class="field">
                        <label for="email">E-mail</label>
                        <div class="input-wrap">
                            <i class="fa-regular fa-envelope" aria-hidden="true"></i>
                            <input
                                id="email"
                                v-model="email"
                                type="email"
                                placeholder="seu@email.com"
                                autocomplete="email"
                                required
                            />
                        </div>
                    </div>

                    <div class="field">
                        <label for="password">
                            Senha
                            <button
                                v-if="modo === 'login'"
                                type="button"
                                class="hint hint--link"
                                @click="irPara('recuperar')"
                            >Esqueci minha senha</button>
                        </label>
                        <div class="input-wrap">
                            <i class="fa-solid fa-lock" aria-hidden="true"></i>
                            <input
                                id="password"
                                v-model="senha"
                                :type="mostrarSenha ? 'text' : 'password'"
                                placeholder="••••••••"
                                :autocomplete="modo === 'cadastro' ? 'new-password' : 'current-password'"
                                required
                            />
                            <button
                                type="button"
                                class="toggle-pwd"
                                :aria-label="mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'"
                                tabindex="-1"
                                @click="mostrarSenha = !mostrarSenha"
                            >
                                <i :class="mostrarSenha ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye'" aria-hidden="true"></i>
                            </button>
                        </div>
                        <div v-if="senhaForca" class="senha-forca">
                            <div class="senha-forca-bar">
                                <div class="senha-forca-fill" :style="{ width: senhaForca.pct, background: senhaForca.cor }"></div>
                            </div>
                            <span :style="{ color: senhaForca.cor }">{{ senhaForca.label }}</span>
                        </div>
                    </div>

                    <div v-if="modo === 'cadastro'" class="field">
                        <label for="password-confirm">Confirmar senha</label>
                        <div class="input-wrap">
                            <i class="fa-solid fa-lock" aria-hidden="true"></i>
                            <input
                                id="password-confirm"
                                v-model="senhaConfirm"
                                :type="mostrarSenhaConfirm ? 'text' : 'password'"
                                placeholder="Repita a senha"
                                autocomplete="new-password"
                                required
                            />
                            <button
                                type="button"
                                class="toggle-pwd"
                                tabindex="-1"
                                @click="mostrarSenhaConfirm = !mostrarSenhaConfirm"
                            >
                                <i :class="mostrarSenhaConfirm ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye'" aria-hidden="true"></i>
                            </button>
                        </div>
                    </div>

                    <div v-if="modo === 'cadastro'" class="field-row">
                        <label class="checkbox">
                            <input v-model="aceitouTermos" type="checkbox" />
                            <span class="box"><i class="fa-solid fa-check" aria-hidden="true"></i></span>
                            <span>
                                Aceito os
                                <router-link to="/termos" target="_blank" class="link">termos de uso</router-link>
                                e a
                                <router-link to="/privacidade" target="_blank" class="link">política de privacidade</router-link>
                            </span>
                        </label>
                    </div>

                    <div v-if="modo === 'login'" class="field-row">
                        <label class="checkbox">
                            <input v-model="lembrarMe" type="checkbox" />
                            <span class="box"><i class="fa-solid fa-check" aria-hidden="true"></i></span>
                            Manter-me conectado
                        </label>
                    </div>
                </template>

                <button type="submit" class="btn-primary" :disabled="!formularioValido || carregando">
                    <template v-if="carregando">
                        <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                        <template v-if="modo === 'login'">Entrando...</template>
                        <template v-else-if="modo === 'cadastro'">Criando conta...</template>
                        <template v-else>Enviando...</template>
                    </template>
                    <template v-else>
                        <template v-if="modo === 'login'">Entrar</template>
                        <template v-else-if="modo === 'cadastro'">Criar conta</template>
                        <template v-else>Enviar e-mail</template>
                        <i class="fa-solid fa-arrow-right" aria-hidden="true"></i>
                    </template>
                </button>

                <div class="switch-cta">
                    <template v-if="modo === 'login'">
                        Ainda não tem conta?
                        <button type="button" class="link-btn" @click="irPara('cadastro')">Comece grátis</button>
                    </template>
                    <template v-else-if="modo === 'cadastro'">
                        Já tem uma conta?
                        <button type="button" class="link-btn" @click="irPara('login')">Acesse</button>
                    </template>
                    <template v-else>
                        Lembrou sua senha?
                        <button type="button" class="link-btn" @click="irPara('login')">Voltar ao login</button>
                    </template>
                </div>
                </form>

                <div class="auth-footer">
                    <div class="secure">
                        <i class="fa-solid fa-shield-halved" aria-hidden="true"></i>
                        Conexão segura e em conformidade com a LGPD
                    </div>
                </div>
            </div>
        </section>

        <!-- ── Brand side (direita) ── -->
        <aside class="auth-brand-side">
            <div class="brand-content">
                <div class="brand-eyebrow">
                    <span class="dot"></span>
                    Plataforma para clínicas modernas
                </div>
                <h2 class="brand-headline">Sua clínica, <em>organizada</em> num só lugar.</h2>
                <p class="brand-sub">
                    Agenda, prontuário eletrônico, prescrição digital e financeiro — tudo conectado
                    para você focar no que importa: cuidar bem dos seus pacientes.
                </p>

                <div class="preview-stack">
                    <div class="preview-card c1">
                        <div class="pc-av">MS</div>
                        <div class="pc-info">
                            <b>Maria Silva</b>
                            <span>Consulta · 09:00</span>
                        </div>
                        <span class="pc-pill success">Confirmado</span>
                    </div>
                    <div class="preview-card c2">
                        <div class="pc-av pc-av--info">JR</div>
                        <div class="pc-info">
                            <b>João Ribeiro</b>
                            <span>Retorno · 10:30</span>
                        </div>
                        <span class="pc-pill warning">Sala de espera</span>
                    </div>
                    <div class="preview-card c3">
                        <div class="pc-av pc-av--success">AC</div>
                        <div class="pc-info">
                            <b>Ana Costa</b>
                            <span>Avaliação · 14:00</span>
                        </div>
                        <span class="pc-pill info">Telemedicina</span>
                    </div>
                </div>
            </div>

            <div class="brand-footer">
                <span>© 2026 Imedto</span>
                <a href="#">Termos</a>
                <a href="#">Privacidade</a>
            </div>
        </aside>
    </main>
</template>

<style scoped>
:root {
    --auth-radius: 14px;
    --dur-fast: 160ms;
}

.auth-shell {
    min-height: 100vh;
    display: grid;
    grid-template-columns: minmax(420px, 1fr) 1fr;
}

/* ── Form side ── */
.auth-form-side {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 40px 24px;
    background: white;
    position: relative;
    overflow-y: auto;
}

.auth-form-wrap {
    width: 100%;
    max-width: 400px;
    display: flex;
    flex-direction: column;
}

.auth-logo-img {
    width: 140px;
    height: auto;
    margin-bottom: 40px;
    align-self: flex-start;
}

.auth-form {
    width: 100%;
}
.auth-form h1 {
    margin: 0 0 8px;
    font-size: 28px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    letter-spacing: -0.02em;
}
.auth-form .auth-sub {
    margin: 0 0 32px;
    font-size: 14px;
    color: hsl(var(--secondary) / 0.65);
    line-height: 1.5;
}

/* ── Alertas ── */
.alerta {
    padding: 10px 14px;
    border-radius: 10px;
    font-size: 13px;
    margin-bottom: 16px;
    line-height: 1.4;
}
.alerta--sucesso {
    background: hsl(var(--success) / 0.1);
    color: hsl(var(--success));
    border: 1px solid hsl(var(--success) / 0.2);
}
.alerta--erro {
    background: hsl(var(--error) / 0.08);
    color: hsl(var(--error));
    border: 1px solid hsl(var(--error) / 0.2);
}

/* ── Field ── */
.field {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-bottom: 16px;
}
.field label {
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.field label .hint {
    font-size: 11px;
    font-weight: 400;
    color: hsl(var(--secondary) / 0.55);
}
.field label .hint--link {
    background: none;
    border: none;
    padding: 0;
    color: hsl(var(--primary));
    font-size: 11px;
    font-weight: 600;
    cursor: pointer;
    font-family: inherit;
}
.field label .hint--link:hover { text-decoration: underline; }

.field .input-wrap { position: relative; }
.field .input-wrap > i {
    position: absolute;
    left: 14px;
    top: 50%;
    transform: translateY(-50%);
    color: hsl(var(--secondary) / 0.4);
    font-size: 13px;
    pointer-events: none;
    transition: color var(--dur-fast);
}
.field input[type="text"],
.field input[type="email"],
.field input[type="password"],
.field input[type="tel"] {
    width: 100%;
    padding: 12px 14px 12px 40px;
    font-family: inherit;
    font-size: 14px;
    color: hsl(var(--primary-dark));
    background: white;
    border: 1.5px solid hsl(var(--secondary) / 0.12);
    border-radius: 10px;
    outline: none;
    transition: all var(--dur-fast);
}
.field input:focus {
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 4px hsl(var(--primary) / 0.1);
}
.field .input-wrap:focus-within > i {
    color: hsl(var(--primary));
}

.field .toggle-pwd {
    position: absolute;
    right: 12px;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    color: hsl(var(--secondary) / 0.5);
    cursor: pointer;
    font-size: 13px;
    padding: 4px;
}
.field .toggle-pwd:hover { color: hsl(var(--primary-dark)); }

.senha-forca {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-top: 4px;
}
.senha-forca-bar {
    height: 4px;
    background: hsl(var(--secondary) / 0.1);
    border-radius: 99px;
    overflow: hidden;
}
.senha-forca-fill {
    height: 100%;
    border-radius: 99px;
    transition: width 0.3s, background 0.3s;
}
.senha-forca span {
    font-size: 11px;
    font-weight: 600;
}

/* ── Field row + checkbox ── */
.field-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin: 4px 0 24px;
}
.checkbox {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    color: hsl(var(--secondary) / 0.75);
    cursor: pointer;
    user-select: none;
}
.checkbox input { display: none; }
.checkbox .box {
    width: 18px;
    height: 18px;
    border: 1.5px solid hsl(var(--secondary) / 0.25);
    border-radius: 5px;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all var(--dur-fast);
    flex-shrink: 0;
}
.checkbox .box i { color: white; font-size: 10px; opacity: 0; }
.checkbox input:checked + .box {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
}
.checkbox input:checked + .box i { opacity: 1; }
.link {
    color: hsl(var(--primary));
    font-weight: 600;
    text-decoration: none;
}
.link:hover { text-decoration: underline; }

/* ── Botões ── */
.btn-primary {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    padding: 13px 18px;
    font-family: inherit;
    font-size: 14px;
    font-weight: 600;
    border-radius: 10px;
    cursor: pointer;
    transition: all var(--dur-fast);
    width: 100%;
    background: hsl(var(--primary));
    color: white;
    border: none;
    box-shadow: 0 1px 2px hsl(var(--primary-dark) / 0.2);
}
.btn-primary:hover:not(:disabled) {
    background: hsl(var(--primary-dark));
    transform: translateY(-1px);
    box-shadow: 0 4px 12px hsl(var(--primary) / 0.3);
}
.btn-primary:active:not(:disabled) { transform: translateY(0); }
.btn-primary:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    transform: none;
}

/* ── Switch CTA ── */
.switch-cta {
    text-align: center;
    margin-top: 24px;
    font-size: 14px;
    color: hsl(var(--secondary) / 0.7);
}
.link-btn {
    background: none;
    border: none;
    color: hsl(var(--primary));
    font-weight: 700;
    text-decoration: none;
    cursor: pointer;
    font-family: inherit;
    font-size: inherit;
    padding: 0;
}
.link-btn:hover { text-decoration: underline; }

/* ── Footer ── */
.auth-footer {
    margin-top: 40px;
    padding-top: 24px;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
    text-align: center;
}
.auth-footer .secure {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 11px;
    color: hsl(var(--secondary) / 0.5);
}
.auth-footer .secure i { color: hsl(var(--success)); }

/* ── Brand side ── */
.auth-brand-side {
    background: linear-gradient(135deg, hsl(254 56% 24%) 0%, hsl(254 56% 38%) 50%, hsl(264 60% 48%) 100%);
    position: relative;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    padding: 56px;
    color: white;
}
.auth-brand-side::before,
.auth-brand-side::after {
    content: '';
    position: absolute;
    border-radius: 50%;
    pointer-events: none;
    opacity: 0.18;
}
.auth-brand-side::before {
    width: 480px;
    height: 480px;
    top: -160px;
    right: -120px;
    background: radial-gradient(circle, hsl(264 80% 65%) 0%, transparent 70%);
}
.auth-brand-side::after {
    width: 360px;
    height: 360px;
    bottom: -120px;
    left: -80px;
    background: radial-gradient(circle, hsl(199 89% 60%) 0%, transparent 70%);
}

.brand-content {
    position: relative;
    z-index: 1;
    max-width: 480px;
}
.brand-eyebrow {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    background: hsl(0 0% 100% / 0.12);
    backdrop-filter: blur(10px);
    padding: 6px 14px;
    border-radius: 99px;
    font-size: 12px;
    font-weight: 600;
    letter-spacing: 0.02em;
    margin-bottom: 24px;
}
.brand-eyebrow .dot {
    width: 6px;
    height: 6px;
    background: hsl(var(--success));
    border-radius: 50%;
    box-shadow: 0 0 0 4px hsl(var(--success) / 0.3);
}
.brand-headline {
    font-size: 38px;
    font-weight: 700;
    line-height: 1.15;
    letter-spacing: -0.025em;
    margin: 0 0 20px;
}
.brand-headline em {
    font-style: normal;
    background: linear-gradient(135deg, hsl(45 95% 70%), hsl(199 89% 70%));
    -webkit-background-clip: text;
    background-clip: text;
    color: transparent;
}
.brand-sub {
    font-size: 16px;
    line-height: 1.55;
    color: hsl(0 0% 100% / 0.8);
    margin: 0 0 36px;
}

/* ── Preview cards flutuantes ── */
.preview-stack {
    position: relative;
    margin: 36px 0 0;
    height: 320px;
}
.preview-card {
    position: absolute;
    background: white;
    border-radius: 14px;
    padding: 14px 16px;
    color: hsl(var(--secondary));
    box-shadow: 0 24px 64px hsl(var(--primary-dark) / 0.4);
    display: flex;
    align-items: center;
    gap: 12px;
    font-size: 13px;
    width: 320px;
}
.preview-card .pc-av {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary)), hsl(var(--primary-dark)));
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 12px;
    flex-shrink: 0;
}
.preview-card .pc-av--info { background: linear-gradient(135deg, hsl(199 89% 48%), hsl(199 89% 32%)); }
.preview-card .pc-av--success { background: linear-gradient(135deg, hsl(160 79% 39%), hsl(160 79% 25%)); }
.preview-card .pc-info b {
    display: block;
    color: hsl(var(--primary-dark));
    font-weight: 600;
}
.preview-card .pc-info span {
    font-size: 11px;
    color: hsl(var(--secondary) / 0.6);
}
.preview-card .pc-pill {
    margin-left: auto;
    padding: 3px 10px;
    border-radius: 99px;
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.preview-card .pc-pill.success {
    background: hsl(var(--success) / 0.15);
    color: hsl(var(--success));
}
.preview-card .pc-pill.warning {
    background: hsl(var(--warning) / 0.18);
    color: hsl(28 90% 40%);
}
.preview-card .pc-pill.info {
    background: hsl(var(--info) / 0.15);
    color: hsl(var(--info));
}
.preview-card.c1 { top: 0; left: 0; transform: rotate(-2deg); animation: float1 6s ease-in-out infinite; }
.preview-card.c2 { top: 88px; left: 56px; transform: rotate(1.5deg); animation: float2 7s ease-in-out infinite 0.5s; }
.preview-card.c3 { top: 176px; left: 24px; transform: rotate(-1deg); animation: float1 8s ease-in-out infinite 1s; }
@keyframes float1 { 0%, 100% { transform: rotate(-2deg) translateY(0); } 50% { transform: rotate(-2deg) translateY(-6px); } }
@keyframes float2 { 0%, 100% { transform: rotate(1.5deg) translateY(0); } 50% { transform: rotate(1.5deg) translateY(-6px); } }

/* ── Brand footer ── */
.brand-footer {
    position: relative;
    z-index: 1;
    display: flex;
    gap: 24px;
    align-items: center;
    font-size: 12px;
    color: hsl(0 0% 100% / 0.6);
}
.brand-footer a {
    color: hsl(0 0% 100% / 0.8);
    text-decoration: none;
}
.brand-footer a:hover { color: white; }

/* ── Responsive ── */
@media (max-width: 960px) {
    .auth-shell { grid-template-columns: 1fr; }
    .auth-brand-side { display: none; }
    .auth-form-side { padding: 32px 24px; }
}
</style>
