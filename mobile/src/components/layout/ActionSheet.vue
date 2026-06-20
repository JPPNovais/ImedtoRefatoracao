<script setup lang="ts">
import { computed, ref } from "vue"
import { useRouter } from "vue-router"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import PacienteSeletorSheet from "@/components/ui/PacienteSeletorSheet.vue"

const ui = useUiStore()
const router = useRouter()
const permissoes = usePermissoesStore()

const open = computed({
  get: () => ui.activeSheet === "actions",
  set: (v: boolean) => (v ? ui.openSheet("actions") : ui.closeSheet()),
})

// Rota aguardando seleção de paciente (receita/atestado/exame vêm do FAB sem contexto)
const rotaPendente = ref<string | null>(null)
const seletorOpen = ref(false)

interface Acao {
  key: string
  rota: string
  titulo: string
  sub: string
  icon: string
  cor: string
  pode: boolean
  /** Exige seleção de paciente antes de navegar */
  precisaPaciente?: boolean
}
// As ações disponíveis respeitam o RBAC (G2): some o que o vínculo não permite.
const acoes = computed<Acao[]>(() =>
  [
    {
      key: "receita",
      rota: "/receita",
      titulo: "Nova receita",
      sub: "Favoritos · assinar · enviar",
      icon: "fa-prescription",
      cor: "ic-violet",
      pode: permissoes.pode("prescricao"),
      precisaPaciente: true,
    },
    {
      key: "atestado",
      rota: "/atestado",
      titulo: "Atestado",
      sub: "CID · dias · assinar",
      icon: "fa-file-medical",
      cor: "ic-green",
      pode: permissoes.pode("prescricao"),
      precisaPaciente: true,
    },
    {
      key: "exame",
      rota: "/exame",
      titulo: "Pedido de exame",
      sub: "Selecionar exames · assinar",
      icon: "fa-flask",
      cor: "ic-blue",
      pode: permissoes.pode("prescricao"),
      precisaPaciente: true,
    },
    {
      key: "agendamento",
      rota: "/novo-agendamento",
      titulo: "Novo agendamento",
      sub: "Marcar uma consulta",
      icon: "fa-calendar-plus",
      cor: "ic-amber",
      pode: permissoes.pode("agenda.criar"),
      precisaPaciente: false,
    },
  ].filter((a) => a.pode),
)

function abrir(acao: Acao) {
  ui.closeSheet()
  if (acao.precisaPaciente) {
    // Abre o seletor de paciente antes de navegar para receita/atestado/exame
    rotaPendente.value = acao.rota
    seletorOpen.value = true
  } else {
    router.push(acao.rota).catch(() => {})
  }
}

function onPacienteSelecionado(p: { id: number; nomeCompleto: string }) {
  seletorOpen.value = false
  if (rotaPendente.value) {
    router.push({ path: rotaPendente.value, query: { pacienteId: String(p.id) } }).catch(() => {})
    rotaPendente.value = null
  }
}
</script>

<template>
  <BottomSheet
    v-model:open="open"
    titulo="Ações rápidas"
    sub="As ações disponíveis seguem suas permissões."
  >
    <div v-for="a in acoes" :key="a.key" class="act-row" @click="abrir(a)">
      <div class="ic" :class="a.cor"><i class="fa-solid" :class="a.icon"></i></div>
      <div class="tx"><b>{{ a.titulo }}</b><span>{{ a.sub }}</span></div>
      <i class="fa-solid fa-chevron-right chev"></i>
    </div>
  </BottomSheet>

  <!-- Seletor de paciente — abre após escolher receita/atestado/exame no FAB -->
  <PacienteSeletorSheet
    v-model:open="seletorOpen"
    @selecionado="onPacienteSelecionado"
  />
</template>
