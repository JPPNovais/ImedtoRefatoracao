<script setup lang="ts">
import { ref, computed } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import logoColorido from "@/assets/imedto-logo.png"
import googleLogo from "@/assets/google-logo.png"
import httpClient from "@/services/httpClient"
import { AppButton } from "@/components/ui"

const router = useRouter()
const auth = useAuthStore()

type Modo = "login" | "cadastro" | "recuperar"

const modo = ref<Modo>("login")
const email = ref("")
const senha = ref("")
const senhaConfirm = ref("")
const aceitouTermos = ref(false)
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
    if (s <= 2) return { label: "Senha fraca", cor: "#ef4444", pct: "33%" }
    if (s <= 4) return { label: "Senha média", cor: "#d97706", pct: "66%" }
    return { label: "Senha forte", cor: "#059669", pct: "100%" }
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
    <main class="login-page">
        <!-- Coluna esquerda: formulário -->
        <section class="form-col">
            <div class="form-inner">
                <img :src="logoColorido" alt="Imedto" class="logo" />

                <h1 class="titulo">
                    <template v-if="modo === 'login'">Acessar minha conta</template>
                    <template v-else-if="modo === 'cadastro'">Criar conta</template>
                    <template v-else>Recuperar senha</template>
                </h1>
                <p class="subtitulo">
                    <template v-if="modo === 'login'">Preencha os campos abaixo para acessar sua conta:</template>
                    <template v-else-if="modo === 'cadastro'">Preencha os campos abaixo para criar sua conta:</template>
                    <template v-else>Digite seu e-mail e enviaremos instruções para redefinir sua senha.</template>
                </p>

                <div v-if="sucesso" class="alerta alerta--sucesso">{{ sucesso }}</div>
                <div v-if="erro" class="alerta alerta--erro">{{ erro }}</div>

                <form @submit.prevent="enviar" class="form">
                    <!-- Recuperar senha -->
                    <template v-if="modo === 'recuperar'">
                        <div class="campo">
                            <label>E-mail</label>
                            <input v-model="resetEmail" type="email" placeholder="Digite seu e-mail" required />
                        </div>
                    </template>

                    <!-- Login e cadastro -->
                    <template v-else>
                        <div class="campo">
                            <label>E-mail</label>
                            <input v-model="email" type="email" placeholder="Digite seu e-mail" autocomplete="email" required />
                        </div>

                        <div class="campo">
                            <label>Senha</label>
                            <div class="input-senha">
                                <input
                                    v-model="senha"
                                    :type="mostrarSenha ? 'text' : 'password'"
                                    placeholder="Digite sua senha"
                                    :autocomplete="modo === 'cadastro' ? 'new-password' : 'current-password'"
                                    required
                                />
                                <button type="button" class="btn-olho" @click="mostrarSenha = !mostrarSenha" tabindex="-1">
                                    <i :class="mostrarSenha ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye'"></i>
                                </button>
                            </div>
                            <!-- Força da senha (cadastro) -->
                            <div v-if="senhaForca" class="senha-forca">
                                <div class="senha-forca-bar">
                                    <div class="senha-forca-fill" :style="{ width: senhaForca.pct, background: senhaForca.cor }"></div>
                                </div>
                                <span :style="{ color: senhaForca.cor }">{{ senhaForca.label }}</span>
                            </div>
                        </div>

                        <div v-if="modo === 'cadastro'" class="campo">
                            <label>Confirmar senha</label>
                            <div class="input-senha">
                                <input
                                    v-model="senhaConfirm"
                                    :type="mostrarSenhaConfirm ? 'text' : 'password'"
                                    placeholder="Repita a senha"
                                    autocomplete="new-password"
                                    required
                                />
                                <button type="button" class="btn-olho" @click="mostrarSenhaConfirm = !mostrarSenhaConfirm" tabindex="-1">
                                    <i :class="mostrarSenhaConfirm ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye'"></i>
                                </button>
                            </div>
                        </div>

                        <div v-if="modo === 'cadastro'" class="campo-checkbox">
                            <input id="termos" v-model="aceitouTermos" type="checkbox" />
                            <label for="termos">
                                Li e aceito os
                                <router-link to="/termos" target="_blank" class="link">termos de uso</router-link>
                                e a
                                <router-link to="/privacidade" target="_blank" class="link">política de privacidade</router-link>
                            </label>
                        </div>

                        <div v-if="modo === 'login'" class="login-extras">
                            <span></span>
                            <button type="button" class="link-btn" @click="irPara('recuperar')">
                                Esqueci minha senha
                            </button>
                        </div>
                    </template>

                    <AppButton
                        type="submit"
                        block
                        :loading="carregando"
                        :disabled="!formularioValido"
                    >
                        <template v-if="carregando">
                            <template v-if="modo === 'login'">Entrando...</template>
                            <template v-else-if="modo === 'cadastro'">Criando conta...</template>
                            <template v-else>Enviando...</template>
                        </template>
                        <template v-else>
                            <template v-if="modo === 'login'">Entrar</template>
                            <template v-else-if="modo === 'cadastro'">Criar conta</template>
                            <template v-else>Enviar e-mail</template>
                        </template>
                    </AppButton>
                </form>

                <div class="divisor">
                    <span>ou</span>
                </div>

                <p class="troca-modo">
                    <template v-if="modo === 'login'">
                        Não tem uma conta?
                        <button type="button" class="link-btn" @click="irPara('cadastro')">Cadastre-se</button>
                    </template>
                    <template v-else-if="modo === 'cadastro'">
                        Já tem uma conta?
                        <button type="button" class="link-btn" @click="irPara('login')">Acesse</button>
                    </template>
                    <template v-else>
                        Lembrou sua senha?
                        <button type="button" class="link-btn" @click="irPara('login')">Voltar ao login</button>
                    </template>
                </p>
            </div>
        </section>

        <!-- Coluna direita: hero -->
        <section class="hero-col" :class="modo === 'cadastro' ? 'hero-col--cadastro' : 'hero-col--login'"></section>
    </main>
</template>

<style scoped>
.login-page {
    display: grid;
    grid-template-columns: 1fr 1fr;
    min-height: 100vh;
}

/* Coluna esquerda */
.form-col {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2.5rem 1.5rem;
    background: #fff;
    overflow-y: auto;
}

.form-inner {
    width: 100%;
    max-width: 400px;
}

.logo {
    width: 140px;
    margin-bottom: 1.5rem;
}

.titulo {
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text);
    margin: 0 0 0.35rem;
}

.subtitulo {
    font-size: 0.875rem;
    color: var(--text-muted);
    margin: 0 0 1.25rem;
}

/* Alertas */
.alerta {
    padding: 0.65rem 0.9rem;
    border-radius: var(--radius);
    font-size: 0.85em;
    margin-bottom: 1rem;
}
.alerta--sucesso { background: #d1fae5; color: #065f46; }
.alerta--erro    { background: #fee2e2; color: #991b1b; }

/* Form */
.form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    border-bottom: 1px solid var(--border);
    padding-bottom: 1.25rem;
    margin-bottom: 1rem;
}

.campo { display: flex; flex-direction: column; gap: 0.4rem; }
.campo label {
    font-size: 0.85em;
    font-weight: 600;
    color: var(--text);
}
.campo input {
    padding: 0.55rem 0.75rem;
    border: 1px solid var(--border-strong);
    border-radius: var(--radius);
    font-size: 0.9em;
    color: var(--text);
    outline: none;
    transition: border-color 0.15s;
    width: 100%;
}
.campo input:focus { border-color: var(--primary); }

.input-senha {
    position: relative;
}
.input-senha input { padding-right: 2.5rem; }
.btn-olho {
    position: absolute;
    right: 0.65rem;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    cursor: pointer;
    color: var(--text-muted);
    font-size: 0.9em;
    padding: 0;
    line-height: 1;
}
.btn-olho:hover { color: var(--text); }

.senha-forca { display: flex; flex-direction: column; gap: 0.25rem; margin-top: 0.35rem; }
.senha-forca-bar {
    height: 5px;
    background: var(--border);
    border-radius: 99px;
    overflow: hidden;
}
.senha-forca-fill {
    height: 100%;
    border-radius: 99px;
    transition: width 0.3s;
}
.senha-forca span { font-size: 0.78em; font-weight: 600; }

.campo-checkbox {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    font-size: 0.85em;
    color: var(--text);
}
.campo-checkbox input[type="checkbox"] { margin-top: 0.15rem; flex-shrink: 0; }

.login-extras {
    display: flex;
    justify-content: flex-end;
    align-items: center;
    margin-top: -0.25rem;
}

.link { color: var(--primary); font-weight: 600; text-decoration: underline; }
.link-btn {
    background: none;
    border: none;
    color: var(--primary);
    font-weight: 600;
    font-size: 0.875em;
    cursor: pointer;
    padding: 0;
    font-family: inherit;
}
.link-btn:hover { text-decoration: underline; }

.divisor {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    color: var(--text-muted);
    font-size: 0.82em;
    margin-bottom: 1rem;
}
.divisor::before,
.divisor::after {
    content: "";
    flex: 1;
    height: 1px;
    background: var(--border);
}

.troca-modo {
    font-size: 0.875em;
    color: var(--text-muted);
    text-align: center;
    margin: 0;
}

/* Coluna direita: hero */
.hero-col {
    display: none;
}

.hero-col--login {
    background: linear-gradient(135deg,
        rgba(69, 43, 151, 0.92) 0%,
        rgba(36, 21, 84, 0.88) 50%,
        rgba(69, 43, 151, 0.92) 100%
    );
}

.hero-col--cadastro {
    background: linear-gradient(135deg,
        rgba(16, 185, 129, 0.88) 0%,
        rgba(14, 165, 233, 0.85) 50%,
        rgba(69, 43, 151, 0.88) 100%
    );
}

@media (min-width: 768px) {
    .hero-col { display: block; }
}

@media (max-width: 767px) {
    .login-page { grid-template-columns: 1fr; }
}
</style>
