<script setup lang="ts">
import { computed } from "vue"
import { useUiStore } from "@/stores/ui"
import { useTenantStore } from "@/stores/tenant"
import type { Estabelecimento } from "@/types"
import BottomSheet from "@/components/ui/BottomSheet.vue"

const ui = useUiStore()
const tenant = useTenantStore()

const open = computed({
  get: () => ui.activeSheet === "switcher",
  set: (v: boolean) => (v ? ui.openSheet("switcher") : ui.closeSheet()),
})

async function escolher(e: Estabelecimento) {
  if (e.id === tenant.estabelecimentoAtivoId) {
    ui.closeSheet()
    return
  }
  await tenant.selecionar(e)
  ui.closeSheet()
  // Troca de tenant recarrega o contexto inteiro (§4).
  ui.toast(`Agora em ${e.nomeFantasia}`)
}
</script>

<template>
  <BottomSheet
    v-model:open="open"
    titulo="Trocar estabelecimento"
    sub="Troca o contexto: agenda, pacientes e permissões."
  >
    <div
      v-for="e in tenant.estabelecimentos"
      :key="e.id"
      class="estab"
      :class="{ cur: e.id === tenant.estabelecimentoAtivoId }"
      @click="escolher(e)"
    >
      <div class="badge" style="background: linear-gradient(150deg, hsl(var(--primary)), hsl(var(--primary-dark)))">
        <i class="fa-solid fa-hospital"></i>
      </div>
      <div class="tx">
        <b>{{ e.nomeFantasia }}</b>
        <div class="role">
          <span class="role-pill">{{ e.papelDoUsuario === "Dono" ? "Dono" : "Médico" }}</span>
        </div>
      </div>
      <div class="check"><i class="fa-solid fa-check"></i></div>
    </div>
  </BottomSheet>
</template>
