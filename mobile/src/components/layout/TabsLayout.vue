<script setup lang="ts">
import { computed } from "vue"
import { useTenantStore } from "@/stores/tenant"
import { useUiStore } from "@/stores/ui"
import BottomTabBar from "./BottomTabBar.vue"
import ActionSheet from "./ActionSheet.vue"
import EstabelecimentoSwitcher from "./EstabelecimentoSwitcher.vue"

const tenant = useTenantStore()
const ui = useUiStore()

const nomeEstab = computed(() => tenant.ativo?.nomeFantasia ?? "—")
const papel = computed(() => (tenant.papel === "Dono" ? "Dono" : "Médico"))
</script>

<template>
  <div class="screen-inner" style="display: flex; flex-direction: column; height: 100%">
    <div class="safe-top"></div>

    <!-- Top bar contextual: switcher de estabelecimento + ferramentas da aba -->
    <div class="topbar">
      <button class="switcher" @click="ui.openSheet('switcher')">
        <span class="badge"><i class="fa-solid fa-hospital"></i></span>
        <span class="meta">
          <b>{{ nomeEstab }} <i class="fa-solid fa-chevron-down"></i></b>
          <span>{{ papel }}</span>
        </span>
      </button>
      <!-- Alvo de teleport: cada aba injeta aqui suas ferramentas (busca/filtro) -->
      <div class="tools" id="topbar-tools"></div>
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
