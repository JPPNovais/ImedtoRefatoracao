<script setup lang="ts">
import { useRouter } from "vue-router"
import { useTenantStore } from "@/stores/tenant"
import { useAuthStore } from "@/stores/auth"
import type { Estabelecimento } from "@/types"

const router = useRouter()
const tenant = useTenantStore()
const auth = useAuthStore()

async function escolher(e: Estabelecimento) {
  await tenant.selecionar(e)
  void auth // mantém referência (logout disponível pela aba Mais)
  router.replace({ name: "inicio" })
}
</script>

<template>
  <div class="fs-layer show">
    <div class="sel-wrap2">
      <template v-if="!tenant.semEstabelecimento">
        <h1>Onde você vai atender?</h1>
        <p class="sub">Escolha o estabelecimento para esta sessão.</p>
        <button v-for="e in tenant.estabelecimentos" :key="e.id" class="sel-card" @click="escolher(e)">
          <span class="badge" style="background: linear-gradient(150deg, hsl(var(--primary)), hsl(var(--primary-dark)))">
            <i class="fa-solid fa-hospital"></i>
          </span>
          <span class="tx">
            <b>{{ e.nomeFantasia }}</b>
            <span class="role"><span class="role-pill">{{ e.papelDoUsuario === "Dono" ? "Dono" : "Médico" }}</span></span>
          </span>
          <i class="fa-solid fa-chevron-right chev"></i>
        </button>
      </template>

      <!-- Estado vazio: sem vínculo ativo -->
      <div v-else class="empty" style="padding-top: 60px">
        <i class="fa-regular fa-building"></i>
        <b>Você ainda não tem vínculo</b>
        <p>Quando você for adicionado a um estabelecimento, ele aparece aqui.</p>
      </div>
    </div>
  </div>
</template>
