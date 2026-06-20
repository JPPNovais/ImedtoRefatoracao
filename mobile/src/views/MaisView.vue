<script setup lang="ts">
import { computed, ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/auth"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore, type ThemeMode } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import { usePreferenciasPushStore } from "@/stores/preferenciasPush"
import { iniciais } from "@/lib/format"

const router = useRouter()
const auth = useAuthStore()
const tenant = useTenantStore()
const ui = useUiStore()
const permissoes = usePermissoesStore()
const pushPrefs = usePreferenciasPushStore()

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

const podeVerEstoque = computed(() => permissoes.pode("estoque"))

onMounted(() => pushPrefs.carregar())

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
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.avisos }"
          @click="pushPrefs.alternar('avisos')"
        ></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-fingerprint"></i></span>
        <span class="st">Biometria<small>Face ID / digital ao abrir</small></span>
        <button class="switch" :class="{ on: bioOn }" @click="bioOn = !bioOn"></button>
      </div>
    </div>

    <!-- Toggles de push por categoria (Bloco 9) -->
    <div class="f-label">Push por categoria</div>
    <div class="set-list">
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-cash-register"></i></span>
        <span class="st">Caixa<small>Recebimentos e movimentações</small></span>
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.caixa }"
          @click="pushPrefs.alternar('caixa')"
        ></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-boxes-stacked"></i></span>
        <span class="st">Estoque<small>Alertas de baixo estoque</small></span>
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.estoque }"
          @click="pushPrefs.alternar('estoque')"
        ></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-camera"></i></span>
        <span class="st">Fotos clínicas<small>Novas fotos adicionadas</small></span>
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.fotos }"
          @click="pushPrefs.alternar('fotos')"
        ></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-hand-holding-dollar"></i></span>
        <span class="st">Pagamento<small>Novos recebimentos registrados</small></span>
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.pagamento }"
          @click="pushPrefs.alternar('pagamento')"
        ></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-solid fa-bolt"></i></span>
        <span class="st">Automação<small>Lembretes enviados e confirmações</small></span>
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.automacao }"
          @click="pushPrefs.alternar('automacao')"
        ></button>
      </div>
      <div class="set-row">
        <span class="si"><i class="fa-regular fa-bell"></i></span>
        <span class="st">Avisos<small>Notificações gerais do sistema</small></span>
        <button
          class="switch"
          :class="{ on: pushPrefs.prefs.avisos }"
          @click="pushPrefs.alternar('avisos')"
        ></button>
      </div>
    </div>

    <div class="f-label">Tema</div>
    <div class="seg">
      <button :class="{ on: ui.themeMode === 'light' }" @click="setTema('light')">Claro</button>
      <button :class="{ on: ui.themeMode === 'dark' }" @click="setTema('dark')">Escuro</button>
      <button :class="{ on: ui.themeMode === 'auto' }" @click="setTema('auto')">Automático</button>
    </div>

    <div class="f-label">Gestão</div>
    <div class="set-list">
      <button class="set-row" @click="abrirNoNavegador('Equipe e permissões')">
        <span class="si"><i class="fa-solid fa-users-gear"></i></span>
        <span class="st">Equipe e permissões</span>
        <i class="fa-solid fa-arrow-up-right-from-square rt"></i>
      </button>

      <!-- Estoque: só aparece com permissão "estoque" (RBAC G2) -->
      <button
        v-if="podeVerEstoque"
        class="set-row"
        @click="router.push({ name: 'estoque' })"
      >
        <span class="si"><i class="fa-solid fa-boxes-stacked"></i></span>
        <span class="st">Estoque<small>Materiais e inventário</small></span>
        <i class="fa-solid fa-chevron-right rt"></i>
      </button>

      <!-- Automações: navega para tela interna -->
      <button class="set-row" @click="router.push({ name: 'automacao' })">
        <span class="si"><i class="fa-solid fa-bolt"></i></span>
        <span class="st">Automações<small>Lembretes e confirmações</small></span>
        <i class="fa-solid fa-chevron-right rt"></i>
      </button>

      <button class="set-row" @click="abrirNoNavegador('Assinatura e plano')">
        <span class="si"><i class="fa-solid fa-crown"></i></span>
        <span class="st">Assinatura e plano</span>
        <i class="fa-solid fa-arrow-up-right-from-square rt"></i>
      </button>
    </div>

    <div class="set-list">
      <button class="set-row" @click="abrirNoNavegador('Ajuda e suporte')">
        <span class="si"><i class="fa-regular fa-circle-question"></i></span>
        <span class="st">Ajuda e suporte</span>
        <i class="fa-solid fa-chevron-right rt"></i>
      </button>
      <button class="set-row danger" @click="sair">
        <span class="si"><i class="fa-solid fa-right-from-bracket"></i></span>
        <span class="st">Sair</span>
      </button>
    </div>
    <div class="app-version">Imedto Mobile · versão 1.0.0</div>
  </section>
</template>
