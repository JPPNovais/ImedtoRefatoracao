<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore } from "@/stores/ui"
import { useBiometric } from "@/native/useBiometric"
import logo from "@/assets/imedto-logo.png"
import logoWhite from "@/assets/imedto-logo-white.png"

const router = useRouter()
const auth = useAuthStore()
const tenant = useTenantStore()
const ui = useUiStore()
const biometric = useBiometric()

const email = ref("marina.castro@imedto.com")
const senha = ref("")
const mostrarSenha = ref(false)
const erro = ref(false)
const carregando = ref(false)

async function entrar() {
  if (carregando.value) return
  erro.value = false
  carregando.value = true
  try {
    await auth.login(email.value.trim(), senha.value)
    irProximaTela()
  } catch {
    // Mensagem genérica — não revela se o e-mail existe (anti-enumeração).
    erro.value = true
  } finally {
    carregando.value = false
  }
}

async function entrarComBiometria() {
  const ok = await biometric.confirmar("Entrar no Imedto")
  if (!ok) return
  // Em produção: usa credenciais guardadas no Keychain/Keystore. Aqui revalida a sessão.
  await auth.bootstrap()
  if (auth.isAuthenticated) irProximaTela()
  else ui.toast("Faça login com e-mail e senha na primeira vez", "error")
}

function irProximaTela() {
  if (tenant.semEstabelecimento || !tenant.temTenantSelecionado) router.replace({ name: "seletor" })
  else router.replace({ name: "inicio" })
}
</script>

<template>
  <div class="fs-layer show">
    <div class="login-wrap">
      <img class="login-logo light-only" :src="logo" alt="Imedto" />
      <img class="login-logo dark-only" :src="logoWhite" alt="Imedto" />
      <h1>Bem-vindo de volta</h1>
      <p class="sub">Preencha os campos abaixo para acessar sua conta.</p>

      <div class="lfield">
        <label class="f-label">E-mail</label>
        <div class="linput-wrap">
          <input v-model="email" class="linput" type="email" placeholder="seu@email.com" autocomplete="username" />
        </div>
      </div>
      <div class="lfield">
        <label class="f-label">Senha</label>
        <div class="linput-wrap">
          <input
            v-model="senha"
            class="linput"
            :type="mostrarSenha ? 'text' : 'password'"
            placeholder="••••••••"
            autocomplete="current-password"
            @keyup.enter="entrar"
          />
          <button class="eye" @click="mostrarSenha = !mostrarSenha">
            <i class="fa-regular" :class="mostrarSenha ? 'fa-eye-slash' : 'fa-eye'"></i>
          </button>
        </div>
      </div>

      <div class="lerror" :class="{ on: erro }">
        <i class="fa-solid fa-circle-exclamation"></i> Verifique seu e-mail e senha e tente novamente.
      </div>

      <span class="forgot">Esqueci minha senha</span>

      <button class="btn-primary-lg" style="margin: 0" :disabled="carregando" @click="entrar">
        <i v-if="carregando" class="fa-solid fa-spinner fa-spin"></i>
        <i v-else class="fa-solid fa-arrow-right-to-bracket"></i>
        {{ carregando ? "Entrando…" : "Entrar" }}
      </button>

      <div class="ldiv">ou</div>
      <button class="bio-btn" @click="entrarComBiometria">
        <i class="fa-solid fa-fingerprint"></i> Entrar com biometria
      </button>
    </div>
  </div>
</template>
