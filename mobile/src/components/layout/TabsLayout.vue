<script setup lang="ts">
import { computed } from "vue"
import { useRouter } from "vue-router"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore } from "@/stores/ui"
import { useNotificacoesStore } from "@/stores/notificacoes"
import BottomTabBar from "./BottomTabBar.vue"
import ActionSheet from "./ActionSheet.vue"
import EstabelecimentoSwitcher from "./EstabelecimentoSwitcher.vue"

const tenant = useTenantStore()
const ui = useUiStore()
const notificacoes = useNotificacoesStore()
const router = useRouter()

const nomeEstab = computed(() => tenant.ativo?.nomeFantasia ?? "—")
const papel = computed(() => (tenant.papel === "Dono" ? "Dono" : "Médico"))

function abrirAvisos() {
  router.push("/avisos")
}
</script>

<template>
  <div class="screen-inner" style="display: flex; flex-direction: column; height: 100%">
    <div class="safe-top"></div>

    <!-- Top bar contextual: switcher de estabelecimento + ferramentas da aba + sino -->
    <div class="topbar">
      <button class="switcher" @click="ui.openSheet('switcher')">
        <span class="badge"><i class="fa-solid fa-hospital"></i></span>
        <span class="meta">
          <b>{{ nomeEstab }} <i class="fa-solid fa-chevron-down"></i></b>
          <span>{{ papel }}</span>
        </span>
      </button>
      <div class="tools" id="topbar-tools">
        <!-- Alvo de teleport: cada aba injeta aqui suas ferramentas (busca/filtro) -->
        <!-- Sino de avisos: sempre visível, badge = não-lidas -->
        <button class="iconbtn" aria-label="Avisos" @click="abrirAvisos">
          <i class="fa-regular fa-bell"></i>
          <span v-if="notificacoes.temNaoLidas" class="nb">{{ notificacoes.naoLidas }}</span>
        </button>
      </div>
    </div>

    <!-- G3 — offline -->
    <div v-if="ui.offline" class="offline-bar">
      <i class="fa-solid fa-wifi"></i> Sem conexão — mostrando dados salvos
      <template v-if="ui.lastSyncLabel"> de {{ ui.lastSyncLabel }}</template>
    </div>

    <div class="body">
      <RouterView v-slot="{ Component }">
        <KeepAlive>
          <component :is="Component" />
        </KeepAlive>
      </RouterView>
    </div>

    <BottomTabBar />

    <!-- Sheets globais das abas -->
    <ActionSheet />
    <EstabelecimentoSwitcher />
  </div>
</template>
