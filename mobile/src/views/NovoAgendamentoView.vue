<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import { agendaService } from "@/services/agenda.service"
import { pacienteService } from "@/services/paciente.service"
import { useAuthStore } from "@/stores/auth"
import { useUiStore } from "@/stores/ui"
import { iniciais } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppSearchInput from "@/components/ui/AppSearchInput.vue"

const router = useRouter()
const auth = useAuthStore()
const ui = useUiStore()

const paciente = ref<{ id: number; nomeCompleto: string } | null>(null)
const data = ref(new Date("2026-06-05").toISOString().slice(0, 10))
const horario = ref("")
const tipo = ref("Consulta")
const sala = ref("Sala 1")
const obs = ref("")
const salvando = ref(false)

const HORARIOS = ["08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "14:00", "14:30", "15:00"]

const pickerOpen = ref(false)
const busca = ref("")
const lista = ref<{ id: number; nomeCompleto: string }[]>([])

onMounted(async () => {
  lista.value = await pacienteService.buscaRapida("", 20).catch(() => [])
})
async function buscar() {
  lista.value = await pacienteService.buscaRapida(busca.value, 20).catch(() => [])
}
function escolher(p: { id: number; nomeCompleto: string }) {
  paciente.value = p
  pickerOpen.value = false
}

async function salvar() {
  if (!paciente.value) return ui.toast("Selecione um paciente", "error")
  if (!horario.value) return ui.toast("Escolha um horário", "error")
  salvando.value = true
  try {
    const inicio = `${data.value}T${horario.value}:00`
    const fimDate = new Date(inicio)
    fimDate.setMinutes(fimDate.getMinutes() + 30)
    await agendaService.criar({
      pacienteId: paciente.value.id,
      profissionalUsuarioId: auth.usuario?.id || "",
      inicioPrevisto: inicio,
      fimPrevisto: fimDate.toISOString().slice(0, 19),
      tipoServico: tipo.value,
      observacoes: obs.value || undefined,
    })
    ui.toast("Consulta agendada")
    router.back()
  } catch {
    ui.toast("Não foi possível agendar", "error")
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" @click="router.back()"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Novo agendamento</div>
      <span style="width: 40px"></span>
    </div>

    <div class="push-body">
      <div class="f-label">Paciente</div>
      <button class="rc-patient" @click="pickerOpen = true">
        <span class="av" :style="!paciente ? 'background:var(--app-card-2);color:var(--app-text-faint)' : ''">
          <template v-if="paciente">{{ iniciais(paciente.nomeCompleto) }}</template>
          <i v-else class="fa-solid fa-user"></i>
        </span>
        <span class="rx">
          <b>{{ paciente?.nomeCompleto || "Selecionar paciente" }}</b>
          <span>{{ paciente ? "Toque para trocar" : "Toque para escolher" }}</span>
        </span>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <div class="f-label">Data</div>
      <div class="tap-field">
        <i class="fa-regular fa-calendar lead"></i>
        <input v-model="data" type="date" style="border: 0; background: transparent; font: inherit; color: var(--app-text); flex: 1; outline: none" />
      </div>

      <div class="f-label">Horário</div>
      <div class="fav-chips">
        <button v-for="h in HORARIOS" :key="h" class="fav-chip" :class="{ on: horario === h }" @click="horario = h">{{ h }}</button>
      </div>

      <div class="f-label">Tipo de atendimento</div>
      <div class="sel-wrap">
        <select v-model="tipo" class="msel"><option>Consulta</option><option>Retorno</option><option>Avaliação</option><option>Primeira consulta</option></select>
        <i class="fa-solid fa-chevron-down"></i>
      </div>

      <div class="f-label">Sala</div>
      <div class="sel-wrap">
        <select v-model="sala" class="msel"><option>Sala 1</option><option>Sala 2</option><option>Sala 3</option></select>
        <i class="fa-solid fa-chevron-down"></i>
      </div>

      <div class="f-label">Observações (opcional)</div>
      <textarea v-model="obs" class="doc-ta" placeholder="Motivo, preparo, encaminhamento…"></textarea>
    </div>

    <div class="push-foot">
      <button class="btn-primary-lg" style="margin: 0" :disabled="salvando" @click="salvar">
        <i v-if="salvando" class="fa-solid fa-spinner fa-spin"></i>
        <i v-else class="fa-regular fa-calendar-check"></i>
        {{ salvando ? "Agendando…" : "Agendar consulta" }}
      </button>
    </div>

    <BottomSheet v-model:open="pickerOpen" titulo="Selecionar paciente" tall>
      <AppSearchInput v-model="busca" placeholder="Buscar paciente…" @update:model-value="buscar" />
      <div class="med-row" v-for="p in lista" :key="p.id" @click="escolher(p)">
        <div class="mi">{{ iniciais(p.nomeCompleto) }}</div>
        <b>{{ p.nomeCompleto }}</b>
        <i class="fa-solid fa-chevron-right add-i"></i>
      </div>
    </BottomSheet>
  </div>
</template>
