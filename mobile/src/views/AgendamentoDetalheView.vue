<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { agendaService } from "@/services/agenda.service"
import type { Agendamento } from "@/types"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import { useShare } from "@/native/useShare"
import { horaDe, iniciais } from "@/lib/format"
import AppStatusPill from "@/components/ui/AppStatusPill.vue"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const permissoes = usePermissoesStore()
const share = useShare()

// RBAC (G2): edição da agenda e abertura do prontuário respeitam o vínculo.
const podeEditar = computed(() => permissoes.pode("agenda.editar"))
const podeProntuario = computed(() => permissoes.pode("prontuario.ver"))

const id = Number(route.params.id)
const ag = ref<Agendamento | null>(null)
const carregando = ref(true)

onMounted(async () => {
  try {
    ag.value = await agendaService.obter(id)
  } catch {
    ui.toast("Agendamento não encontrado", "error")
    router.back()
  } finally {
    carregando.value = false
  }
})

function voltar() {
  router.back()
}
function irFicha() {
  if (ag.value) router.push(`/paciente/${ag.value.pacienteId}`)
}
function iniciarAtendimento() {
  if (ag.value) router.push({ path: `/paciente/${ag.value.pacienteId}/prontuario`, query: { nova: "1" } })
}
async function marcarAtendido() {
  if (!ag.value) return
  try {
    await agendaService.concluir(ag.value.id)
    ui.toast("Marcado como atendido")
    voltar()
  } catch {
    ui.toast("Não foi possível concluir", "error")
  }
}
function marcarFaltou() {
  ui.openConfirm({
    title: "Marcar como faltou?",
    msg: "O paciente será registrado como ausente. A ação fica no histórico do agendamento.",
    onConfirm: async () => {
      if (!ag.value) return
      try {
        await agendaService.cancelar(ag.value.id, "Faltou")
        ui.toast("Marcado como faltou")
        voltar()
      } catch {
        ui.toast("Não foi possível registrar", "error")
      }
    },
  })
}
async function enviarConfirmacao() {
  const ok = await share.compartilhar({
    title: "Confirmar presença",
    text: "Enviar para o paciente confirmar presença.",
    url: `https://app.imedto.com/confirmar/${id}`,
  })
  if (ok) ui.toast("Compartilhado")
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="voltar"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Agendamento</div>
      <button class="iconbtn"><i class="fa-solid fa-ellipsis"></i></button>
    </div>

    <div v-if="ag" class="push-body">
      <div class="ag-time">
        <span>{{ horaDe(ag.inicioPrevisto) }} – {{ horaDe(ag.fimPrevisto) }}</span>
        <span class="sep">·</span>
        <span>{{ ag.salaNome || "Sem sala" }}</span>
      </div>
      <AppStatusPill :status="ag.status" />

      <button class="pat-card" style="margin-top: 16px" @click="irFicha">
        <span class="av">{{ iniciais(ag.pacienteNome) }}</span>
        <span class="pc-tx">
          <b>{{ ag.pacienteNome }}</b>
          <span>{{ ag.tipoServico }}</span>
          <span v-if="ag.temAlertaClinico" class="alert-mini"><i class="fa-solid fa-triangle-exclamation"></i> Alerta clínico</span>
        </span>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <div v-if="ag.observacoes">
        <div class="f-label">Observações</div>
        <div class="ag-obs">{{ ag.observacoes }}</div>
      </div>

      <div class="f-label">Ações</div>
      <button v-if="podeProntuario" class="btn-primary-lg" @click="iniciarAtendimento"><i class="fa-solid fa-play"></i> Iniciar atendimento</button>
      <div v-if="podeEditar" class="btn-row">
        <button class="btn-soft ok" @click="marcarAtendido"><i class="fa-solid fa-check"></i> Atendido</button>
        <button class="btn-soft danger" @click="marcarFaltou"><i class="fa-solid fa-xmark"></i> Faltou</button>
      </div>
      <button v-if="podeEditar" class="btn-outline" @click="ui.toast('Abrindo reagendamento')"><i class="fa-solid fa-arrows-rotate"></i> Reagendar</button>
      <button class="btn-outline" @click="enviarConfirmacao"><i class="fa-brands fa-whatsapp"></i> Enviar confirmação</button>
    </div>
  </div>
</template>
