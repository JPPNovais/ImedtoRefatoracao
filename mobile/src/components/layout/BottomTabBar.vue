<script setup lang="ts">
import { computed } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useNotificacoesStore } from "@/stores/notificacoes"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"

const route = useRoute()
const router = useRouter()
const notificacoes = useNotificacoesStore()
const ui = useUiStore()
const permissoes = usePermissoesStore()

// RBAC: abas sem permissão somem (G2).
const verAgenda = computed(() => permissoes.pode("agenda.ver"))
const verPacientes = computed(() => permissoes.pode("pacientes.ver"))

const activeTab = computed(() => (route.meta.tab as string) || "")

function go(tab: string) {
  router.push(`/${tab}`).catch(() => {})
}
function openActions() {
  ui.openSheet("actions")
}
</script>

<template>
  <div class="tabbar">
    <button v-if="verAgenda" class="tab" :class="{ on: activeTab === 'agenda' }" @click="go('agenda')">
      <i class="fa-regular fa-calendar"></i><span>Agenda</span>
    </button>
    <button v-if="verPacientes" class="tab" :class="{ on: activeTab === 'pacientes' }" @click="go('pacientes')">
      <i class="fa-solid fa-user-group"></i><span>Pacientes</span>
    </button>
    <div class="fab-slot">
      <button class="fab" :class="{ rot: ui.activeSheet === 'actions' }" @click="openActions">
        <i class="fa-solid fa-plus"></i>
      </button>
    </div>
    <button
      class="tab has-badge"
      :class="{ on: activeTab === 'avisos' }"
      @click="go('avisos')"
    >
      <i class="fa-regular fa-bell"></i>
      <span v-if="notificacoes.temNaoLidas" class="nb">{{ notificacoes.naoLidas }}</span>
      <span>Avisos</span>
    </button>
    <button class="tab" :class="{ on: activeTab === 'mais' }" @click="go('mais')">
      <i class="fa-solid fa-ellipsis"></i><span>Mais</span>
    </button>
  </div>
</template>
