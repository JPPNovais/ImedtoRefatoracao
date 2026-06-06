<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { pacienteService } from "@/services/paciente.service"
import type { PacienteListaItem } from "@/types"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { iniciais, idade, dataCurta } from "@/lib/format"
import AppSearchInput from "@/components/ui/AppSearchInput.vue"

const router = useRouter()

const buscaImediata = ref("")
const buscaDebounced = useDebouncedRef("", 350)
watch(buscaImediata, (v) => (buscaDebounced.value = v))

const itens = ref<PacienteListaItem[]>([])
const total = ref(0)
const carregando = ref(true)
const filtro = ref<"todos" | "alerta" | "recentes">("todos")

const filtrados = computed(() => {
  if (filtro.value === "alerta") return itens.value.filter((p) => p.qtdAlertas > 0)
  if (filtro.value === "recentes") return [...itens.value].sort((a, b) => (b.ultimaVisita || "").localeCompare(a.ultimaVisita || "")).slice(0, 5)
  return itens.value
})

const contagens = computed(() => ({
  todos: itens.value.length,
  alerta: itens.value.filter((p) => p.qtdAlertas > 0).length,
  recentes: Math.min(5, itens.value.length),
}))

async function carregar() {
  carregando.value = true
  try {
    const pagina = await pacienteService.listar(buscaDebounced.value || undefined, 1, 50)
    itens.value = pagina.itens
    total.value = pagina.total
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)
watch(buscaDebounced, carregar)

function abrir(p: PacienteListaItem) {
  router.push(`/paciente/${p.id}`)
}
function meta(p: PacienteListaItem): string {
  const i = idade(p.dataNascimento)
  const partes: string[] = []
  if (i !== null) partes.push(`${i} anos`)
  if (p.ultimaVisita) partes.push(`últ. ${dataCurta(p.ultimaVisita)}`)
  return partes.join(" · ")
}
</script>

<template>
  <section class="view">
    <AppSearchInput v-model="buscaImediata" placeholder="Buscar paciente..." />

    <div class="fpills">
      <button class="fpill" :class="{ on: filtro === 'todos' }" @click="filtro = 'todos'">Todos <span class="cnt">{{ contagens.todos }}</span></button>
      <button class="fpill" :class="{ on: filtro === 'alerta' }" @click="filtro = 'alerta'">Com alerta <span class="cnt">{{ contagens.alerta }}</span></button>
      <button class="fpill" :class="{ on: filtro === 'recentes' }" @click="filtro = 'recentes'">Recentes <span class="cnt">{{ contagens.recentes }}</span></button>
    </div>

    <!-- skeleton -->
    <div v-if="carregando" class="plist">
      <div v-for="i in 4" :key="i" class="skrow">
        <div class="sk sk-av"></div>
        <div style="flex: 1"><div class="sk sk-l" style="width: 55%"></div><div class="sk sk-l" style="width: 35%; margin-top: 8px"></div></div>
      </div>
    </div>

    <template v-else>
      <div class="pcount">{{ filtrados.length }} {{ filtrados.length === 1 ? "paciente" : "pacientes" }}</div>
      <div v-if="filtrados.length" class="plist">
        <div v-for="p in filtrados" :key="p.id" class="prow" @click="abrir(p)">
          <span class="av">{{ iniciais(p.nomeCompleto) }}</span>
          <div class="pinfo">
            <div class="pn">
              <b>{{ p.nomeCompleto }}</b>
              <!-- LGPD: só o marcador de alerta na lista, nunca o texto -->
              <span v-if="p.qtdAlertas > 0" class="alert-dot"></span>
            </div>
            <div class="psub">{{ meta(p) }}</div>
          </div>
          <i class="fa-solid fa-chevron-right chev"></i>
        </div>
      </div>
      <div v-else class="empty">
        <i class="fa-regular fa-folder-open"></i>
        <b>{{ buscaImediata ? "Nada encontrado" : "Nenhum paciente" }}</b>
        <p>{{ buscaImediata ? "Tente outro nome ou filtro." : "Cadastre o primeiro paciente no sistema." }}</p>
      </div>
    </template>
  </section>
</template>
