<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { Network } from "@capacitor/network"
import { useUiStore } from "@/stores/ui"
import { useNotificacoesStore } from "@/stores/notificacoes"
import { usePush } from "@/native/usePush"
import AppToast from "@/components/ui/AppToast.vue"
import PushBanner from "@/components/ui/PushBanner.vue"
import AppConfirmDialog from "@/components/ui/AppConfirmDialog.vue"
import logoWhite from "@/assets/imedto-logo-white.png"

const ui = useUiStore()
const route = useRoute()
const router = useRouter()
const notificacoes = useNotificacoesStore()

const showSplash = ref(true)
const transitionName = computed(() => (route.meta.layout === "push" ? "slide" : ""))

onMounted(async () => {
  // Splash de marca por um instante (§ "tela inicial de carregamento").
  setTimeout(() => (showSplash.value = false), 1300)

  // Estado offline (G3): banner discreto + dados salvos.
  const status = await Network.getStatus().catch(() => ({ connected: true }))
  ui.setOffline(!status.connected)
  Network.addListener("networkStatusChange", (s) => {
    const wasOffline = ui.offline
    ui.setOffline(!s.connected)
    if (wasOffline && s.connected) {
      ui.toast("Conexão restabelecida")
      void notificacoes.atualizarContador()
    }
  })

  // Push nativo (APNs/FCM) + deep-link.
  void usePush(router).registrar()
})
</script>

<template>
  <div class="app" :class="{ dark: ui.isDark }">
    <div class="screen">
      <RouterView v-slot="{ Component, route: r }">
        <Transition :name="transitionName">
          <component
            :is="Component"
            :key="r.meta.layout === 'push' ? r.fullPath : 'tabs'"
          />
        </Transition>
      </RouterView>

      <!-- Globais -->
      <AppToast />
      <PushBanner />
      <AppConfirmDialog />

      <!-- Splash -->
      <div v-if="showSplash" class="splash" :class="{ hide: !showSplash }">
        <img :src="logoWhite" alt="Imedto" />
        <div class="ring"></div>
        <div class="tagline">Gestão clínica simplificada</div>
      </div>
    </div>
  </div>
</template>
