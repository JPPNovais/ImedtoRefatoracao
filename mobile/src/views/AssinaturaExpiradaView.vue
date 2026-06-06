<script setup lang="ts">
import { useRouter } from "vue-router"
import { useUiStore } from "@/stores/ui"

const router = useRouter()
const ui = useUiStore()

// G1 — assinatura expirada (backend retorna 402 RequiresAssinaturaAtiva).
function renovar() {
  ui.toast("Abrindo renovação no navegador")
}
function suporte() {
  ui.toast("Abrindo suporte")
}
function voltar() {
  router.replace({ name: "mais" })
}
</script>

<template>
  <div class="fs-layer show">
    <div class="g1-wrap">
      <div class="g1-ic"><i class="fa-regular fa-clock"></i></div>
      <h1>Sua assinatura expirou</h1>
      <p class="sub">
        Renove para voltar a acessar agenda, pacientes e prontuários. Seu login e perfil
        continuam acessíveis.
      </p>
      <button class="btn-primary-lg" style="margin: 0" @click="renovar">
        <i class="fa-solid fa-arrow-up-right-from-square"></i> Renovar assinatura
      </button>
      <button class="btn-outline" style="margin-top: 10px" @click="suporte">
        <i class="fa-regular fa-comments"></i> Falar com suporte
      </button>
      <button class="btn-outline" style="margin-top: 4px; border: 0" @click="voltar">Voltar ao perfil</button>
    </div>
  </div>
</template>
