<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import { agendaService } from "@/services/agenda.service"
import { salaService } from "@/services/sala.service"
import type { SalaDto } from "@/services/sala.service"
import { useAuthStore } from "@/stores/auth"
import { useUiStore } from "@/stores/ui"
import { iniciais, toISODate } from "@/lib/format"

/** Data de hoje em ISO local (sem shift UTC). */
const hojeISO = toISODate(new Date())

/**
 * Retorna true se o horário (string "HH:MM") está no passado considerando a data
 * selecionada. Usa hora LOCAL do dispositivo — espelho da regra do web (motivo: 'passado').
 * Só bloqueia se a data selecionada for HOJE; datas futuras liberam todos os slots.
 */
function horarioNoPassado(h: string): boolean {
  if (data.value !== hojeISO) return false
  const [hh, mm] = h.split(":").map(Number)
  const agora = new Date()
  return hh < agora.getHours() || (hh === agora.getHours() && mm <= agora.getMinutes())
}
import { mensagemDeErro } from "@/lib/erros"
import PacienteSeletorSheet from "@/components/ui/PacienteSeletorSheet.vue"

const router = useRouter()
const auth = useAuthStore()
const ui = useUiStore()

const paciente = ref<{ id: number; nomeCompleto: string } | null>(null)
const data = ref(toISODate(new Date()))
const horario = ref("")
const tipo = ref("Consulta")
const salas = ref<SalaDto[]>([])
const salaId = ref<number | null>(null)
const obs = ref("")
const salvando = ref(false)

const HORARIOS = ["08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "14:00", "14:30", "15:00"]

// Sheet de seleção de paciente (busca + criar + editar)
const seletorOpen = ref(false)

// Quando o sheet abre no modo editar, passa o paciente selecionado
const pacienteParaEditar = ref<{ id: number; nomeCompleto: string } | null>(null)

onMounted(async () => {
  const salasResp = await salaService.listar().catch(() => [])
  salas.value = salasResp
  if (salasResp.length) salaId.value = salasResp[0].id
})

function abrirSeletor() {
  // Abre no modo busca (não edição)
  pacienteParaEditar.value = null
  seletorOpen.value = true
}

function abrirEdicaoPaciente() {
  if (!paciente.value) return
  // Passa o paciente atual para edição inline, sem sair do fluxo
  pacienteParaEditar.value = paciente.value
  seletorOpen.value = true
}

function onPacienteSelecionado(p: { id: number; nomeCompleto: string }) {
  paciente.value = p
  pacienteParaEditar.value = null
}

function onPacienteAtualizado(p: { id: number; nomeCompleto: string }) {
  // Atualiza o nome exibido no card após edição rápida
  paciente.value = { ...paciente.value, ...p }
  pacienteParaEditar.value = null
}

async function salvar() {
  if (!paciente.value) return ui.toast("Selecione um paciente", "error")
  if (!horario.value) return ui.toast("Escolha um horário", "error")
  salvando.value = true
  try {
    const inicio = `${data.value}T${horario.value}:00`
    // Monta fim com componentes locais para não shift de UTC (+3h BRT).
    const [hStr, mStr] = horario.value.split(":")
    const fimTotalMin = Number(hStr) * 60 + Number(mStr) + 30
    const fimH = String(Math.floor(fimTotalMin / 60) % 24).padStart(2, "0")
    const fimM = String(fimTotalMin % 60).padStart(2, "0")
    const fimPrevisto = `${data.value}T${fimH}:${fimM}:00`
    await agendaService.criar({
      pacienteId: paciente.value.id,
      profissionalUsuarioId: auth.usuario?.id || "",
      inicioPrevisto: inicio,
      fimPrevisto,
      tipoServico: tipo.value,
      observacoes: obs.value || undefined,
      salaId: salaId.value ?? undefined,
    })
    ui.toast("Consulta agendada")
    router.back()
  } catch (err) {
    ui.toast(mensagemDeErro(err, "Não foi possível agendar"), "error")
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
      <button class="rc-patient" @click="abrirSeletor">
        <span class="av" :style="!paciente ? 'background:var(--app-card-2);color:var(--app-text-faint)' : ''">
          <template v-if="paciente">{{ iniciais(paciente.nomeCompleto) }}</template>
          <i v-else class="fa-solid fa-user"></i>
        </span>
        <span class="rx">
          <b>{{ paciente?.nomeCompleto || "Selecionar paciente" }}</b>
          <span>{{ paciente ? "Toque para trocar" : "Toque para escolher ou criar" }}</span>
        </span>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <!-- Botão editar dados rápidos do paciente selecionado -->
      <button
        v-if="paciente"
        class="rc-editar-pac"
        @click="abrirEdicaoPaciente"
      >
        <i class="fa-regular fa-pen-to-square"></i>
        Editar dados do paciente
      </button>

      <div class="f-label">Data</div>
      <div class="tap-field">
        <i class="fa-regular fa-calendar lead"></i>
        <input v-model="data" type="date" style="border: 0; background: transparent; font: inherit; color: var(--app-text); flex: 1; outline: none" />
      </div>

      <div class="f-label">Horário</div>
      <div class="fav-chips">
        <button
          v-for="h in HORARIOS"
          :key="h"
          class="fav-chip"
          :class="{ on: horario === h, passado: horarioNoPassado(h) }"
          :disabled="horarioNoPassado(h)"
          :aria-label="horarioNoPassado(h) ? `${h} — horário no passado` : h"
          @click="horario = h"
        >{{ h }}</button>
      </div>

      <div class="f-label">Tipo de atendimento</div>
      <div class="sel-wrap">
        <select v-model="tipo" class="msel"><option>Consulta</option><option>Retorno</option><option>Avaliação</option><option>Primeira consulta</option></select>
        <i class="fa-solid fa-chevron-down"></i>
      </div>

      <div v-if="salas.length" class="f-label">Sala</div>
      <div v-if="salas.length" class="sel-wrap">
        <select v-model="salaId" class="msel">
          <option :value="null">— Sem sala —</option>
          <option v-for="s in salas" :key="s.id" :value="s.id">{{ s.nome }}</option>
        </select>
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

    <!-- Sheet de seleção/criação/edição de paciente -->
    <PacienteSeletorSheet
      v-model:open="seletorOpen"
      :paciente-para-editar="pacienteParaEditar"
      @selecionado="onPacienteSelecionado"
      @atualizado="onPacienteAtualizado"
    />
  </div>
</template>

<style scoped>
/* Chip de horário no passado — desabilitado visualmente (cinza, sem cursor). */
.fav-chip.passado,
.fav-chip:disabled {
  opacity: 0.38;
  cursor: not-allowed;
  pointer-events: none;
}

/* Botão de editar dados rápidos do paciente selecionado */
.rc-editar-pac {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  border: 0;
  background: transparent;
  font: inherit;
  font-size: var(--fs-xs);
  font-weight: var(--fw-bold);
  color: var(--brand);
  cursor: pointer;
  padding: var(--space-1) var(--space-2);
  margin: -4px 0 var(--space-5);
  min-height: 44px;
}
.rc-editar-pac:active {
  opacity: 0.7;
}
</style>
