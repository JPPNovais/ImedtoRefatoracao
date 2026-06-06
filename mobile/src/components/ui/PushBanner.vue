<script setup lang="ts">
import { useRouter } from "vue-router"
import { useUiStore } from "@/stores/ui"

const ui = useUiStore()
const router = useRouter()

function abrir() {
  const link = ui.pushBanner?.link
  ui.dismissPush()
  if (link) router.push(link).catch(() => {})
}
</script>

<template>
  <div class="push-banner" :class="{ show: !!ui.pushBanner }" @click="abrir">
    <div class="pic"><i class="fa-solid fa-bell"></i></div>
    <div class="pbtx">
      <div class="pbk">Imedto</div>
      <b>{{ ui.pushBanner?.titulo }}</b>
      <span>{{ ui.pushBanner?.corpo }}</span>
    </div>
    <div class="pbtime">agora</div>
  </div>
</template>
