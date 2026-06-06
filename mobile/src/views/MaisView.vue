<script setup lang="ts">
import { computed, ref } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore, type ThemeMode } from "@/stores/ui"
import { iniciais } from "@/lib/format"

const router = useRouter()
const auth = useAuthStore()
const tenant = useTenantStore()
const ui = useUiStore()

const pushOn = ref(true)
const bioOn = ref(true)

const nome = computed(() => auth.usuario?.nomeCompleto || "Profissional")
const crm = computed(() => {
  const p = auth.profissional
  if (!p) return ""
  return `${p.conselho || "CRM"} ${p.numeroRegistro || ""}${p.uf ? "-" + p.uf : ""} · ${p.especialidade || ""}`
})
const estabLabel = computed(() =>
  tenant.ativo ? `${tenant.ativo.nomeFantasia} · ${tenant.papel === "Dono" ? "Dono" : "Médico"}` : "—",
)

function setTema(m: ThemeMode) {
  ui.applyTheme(m)
}
function abrirNoNavegador(label: string) {
  ui.toast(`Abrir no navegador: ${label}`)
}
function sair() {
  ui.openConfirm({
    title: "Sair da conta?",
    msg: "Você precisará entrar novamente na próxima vez.",
    confirmLabel: "Sair",
    onConfirm: async () => {
      await auth.logout()
      router.replace({ name: "login" })
    },
  })
}
</script>

<template>
  <section class="view">
    <button class="mais-profile">
      <span class="av">{{ iniciais(nome) }}</span>
      <span class="mx"><b>{{ nome }}</b><span class="crm">{{ crm }}</span></span>
      <i class="fa-solid fa-chevron-right chev"></i>
    </button>

    <div class="f-label">Estabelecimento</div>
    <div class="set-list">
      <button class="set-row" @click="ui.openSheet('switcher')">
        <span class="si"><i class="fa-solid fa-hospital"></i></span>
        <span class="st">Trocar estabelecimento<small>{{ estabLabel }}</small></span>
        <i class="fa-solid fa-chevron-right rt"></i>
      </button>
    </div>

    <div class="f-label">Preferências</div>
    <div class="set-list">
      <div class="set-row">
        <span class="si"><i class="fa-regular fa-bell"></i></span>
        <span class="st">Notificações push<small>Lembretes, cancelamentos, receitas</small></span>
        <button class="switch" :class="{ on: pushOn }" @click="pushOn = !pushOn"></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-fingerprint"></i></span>
        <span class="st">Biometria<small>Face ID / digital ao abrir</small></span>
        <button class="switch" :class="{ on: bioOn }" @click="bioOn = !bioOn"></button>
      </div>
    </div>

    <div class="f-label">Tema</div>
    <div class="seg">
      <button :class="{ on: ui.themeMode === 'light' }" @click="setTema('light')">Claro</button>
      <button :class="{ on: ui.themeMode === 'dark' }" @click="setTema('dark')">Escuro</button>
      <button :class="{ on: ui.themeMode === 'auto' }" @click="setTema('auto')">Automático</button>
    </div>

    <!-- Anti-escopo (§10): configurações pesadas só abrem no navegador -->
    <div class="f-label">Gestão</div>
    <div class="set-list">
      <button class="set-row" @click="abrirNoNavegador('Equipe e permissões')"><span class="si"><i class="fa-solid fa-users-gear"></i></span><span class="st">Equipe e permissões</span><i class="fa-solid fa-arrow-up-right-from-square rt"></i></button>
      <button class="set-row" @click="abrirNoNavegador('Automações')"><span class="si"><i class="fa-solid fa-bolt"></i></span><span class="st">Automações</span><i class="fa-solid fa-arrow-up-right-from-square rt"></i></button>
      <button class="set-row" @click="abrirNoNavegador('Assinatura e plano')"><span class="si"><i class="fa-solid fa-crown"></i></span><span class="st">Assinatura e plano</span><i class="fa-solid fa-arrow-up-right-from-square rt"></i></button>
    </div>

    <div class="set-list">
      <button class="set-row" @click="abrirNoNavegador('Ajuda e suporte')"><span class="si"><i class="fa-regular fa-circle-question"></i></span><span class="st">Ajuda e suporte</span><i class="fa-solid fa-chevron-right rt"></i></button>
      <button class="set-row danger" @click="sair"><span class="si"><i class="fa-solid fa-right-from-bracket"></i></span><span class="st">Sair</span></button>
    </div>
    <div class="app-version">Imedto Mobile · versão 1.0.0</div>
  </section>
</template>
