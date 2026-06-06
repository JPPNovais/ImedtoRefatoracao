<script setup lang="ts">
import { computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useNotificacoesStore } from "@/stores/notificacoes"
import { useUiStore } from "@/stores/ui"
import type { Notificacao } from "@/types"
import { categoriaNotif, tempoRelativo, grupoNotif } from "@/lib/format"

const router = useRouter()
const store = useNotificacoesStore()
const ui = useUiStore()

const GRUPOS = ["Hoje", "Ontem", "Anteriores"] as const

const agrupadas = computed(() =>
  GRUPOS.map((g) => ({
    grupo: g,
    itens: store.notificacoes.filter((n) => grupoNotif(n.criadaEm) === g),
  })).filter((x) => x.itens.length),
)

onMounted(() => {
  if (!store.notificacoes.length) void store.carregar()
})

async function abrir(n: Notificacao) {
  await store.marcarLida(n.id)
  if (n.linkAcao) router.push(n.linkAcao).catch(() => {})
  else ui.toast(`Abrindo: ${n.titulo}`)
}
</script>

<template>
  <section class="view">
    <div class="av-head">
      <div class="ttl">
        <template v-if="store.naoLidas > 0"><b>{{ store.naoLidas }}</b> não lidos</template>
        <template v-else>Tudo lido</template>
      </div>
      <button class="mark-read" :disabled="store.naoLidas === 0" @click="store.marcarTodasLidas()">
        <i class="fa-solid fa-check-double"></i> Marcar todas lidas
      </button>
    </div>

    <template v-if="agrupadas.length">
      <template v-for="g in agrupadas" :key="g.grupo">
        <div class="av-group">{{ g.grupo }}</div>
        <div class="av-list">
          <div
            v-for="n in g.itens"
            :key="n.id"
            class="notif"
            :class="{ unread: !n.lida }"
            @click="abrir(n)"
          >
            <span class="udot"></span>
            <span class="nic" :class="categoriaNotif(n.categoria).cls">
              <i class="fa-solid" :class="categoriaNotif(n.categoria).icon"></i>
            </span>
            <span class="ntx">
              <b>{{ n.titulo }}</b>
              <span class="ns">{{ n.mensagem }}</span>
              <span class="nt">{{ tempoRelativo(n.criadaEm) }}</span>
            </span>
          </div>
        </div>
      </template>
    </template>

    <div v-else class="empty">
      <i class="fa-regular fa-bell-slash"></i>
      <b>Tudo em dia</b>
      <p>Você não tem avisos no momento.</p>
    </div>
  </section>
</template>
