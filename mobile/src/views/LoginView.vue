<script setup lang="ts">
import { onMounted, ref } from "vue"
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

const email = ref("")
const senha = ref("")
const mostrarSenha = ref(false)
const erro = ref(false)
const carregando = ref(false)
// Botão de biometria visível quando: biometria disponível no device E (há creds salvas OU pref habilitada)
const mostrarBotaoBiometria = ref(false)

onMounted(async () => {
  const [dispRes, temCreds, habPref] = await Promise.all([
    biometric.disponivel(),
    biometric.temCredenciais(),
    biometric.habilitadaPeloUsuario(),
  ])
  mostrarBotaoBiometria.value = dispRes && (temCreds || habPref)
})

async function entrar() {
  if (carregando.value) return
  erro.value = false
  carregando.value = true
  try {
    await auth.login(email.value.trim(), senha.value)
    // Após login bem-sucedido: salva credenciais no Keychain se biometria disponível e habilitada.
    // Condição: nativo + pref habilitada → salva; assim na próxima vez o login biométrico já funciona.
    const [dispRes, habPref] = await Promise.all([biometric.disponivel(), biometric.habilitadaPeloUsuario()])
    if (dispRes && habPref) {
      await biometric.salvarCredenciais(email.value.trim(), senha.value)
    }
    irProximaTela()
  } catch {
    // Mensagem genérica — não revela se o e-mail existe (anti-enumeração).
    erro.value = true
  } finally {
    carregando.value = false
  }
}

async function entrarComBiometria() {
  // Exige biometria disponível no device
  const dispRes = await biometric.disponivel()
  if (!dispRes) {
    ui.toast("Biometria não disponível neste dispositivo", "error")
    return
  }
  // Verifica se há credenciais salvas — sem elas não há como logar
  const temCreds = await biometric.temCredenciais()
  if (!temCreds) {
    ui.toast("Faça login com e-mail e senha uma vez para ativar a biometria", "error")
    return
  }
  // Pede confirmação biométrica (FaceID / digital)
  const ok = await biometric.confirmar("Entrar no Imedto")
  if (!ok) return
  // Recupera credenciais e faz login real
  const creds = await biometric.obterCredenciais()
  if (!creds) {
    ui.toast("Não foi possível recuperar as credenciais. Faça login com e-mail e senha.", "error")
    return
  }
  carregando.value = true
  try {
    await auth.login(creds.username, creds.password)
    irProximaTela()
  } catch {
    // Senha mudou no servidor: apaga credenciais obsoletas e orienta login normal
    await biometric.apagarCredenciais()
    ui.toast("Credenciais desatualizadas. Faça login com e-mail e senha para reativar a biometria.", "error")
  } finally {
    carregando.value = false
  }
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
          <input v-model="email" class="linput" type="email" placeholder="exemplo@exemplo.com" autocomplete="username" />
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

      <template v-if="mostrarBotaoBiometria">
        <div class="ldiv">ou</div>
        <button class="bio-btn" @click="entrarComBiometria">
          <i class="fa-solid fa-fingerprint"></i> Entrar com biometria
        </button>
      </template>
    </div>
  </div>
</template>
