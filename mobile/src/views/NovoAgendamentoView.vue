<script setup lang="ts">
import { onMounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { agendaService } from "@/services/agenda.service"
import { salaService } from "@/services/sala.service"
import type { SalaDto } from "@/services/sala.service"
import { useAuthStore } from "@/stores/auth"
import { useUiStore } from "@/stores/ui"
import { iniciais, toISODate } from "@/lib/format"
import { mensagemDeErro } from "@/lib/erros"
import type { DisponibilidadeDia, DisponibilidadeSlot } from "@/types"
import PacienteSeletorSheet from "@/components/ui/PacienteSeletorSheet.vue"

/** Data de hoje em ISO local (sem shift UTC). */
const hojeISO = toISODate(new Date())

/**
 * Retorna true se o slot está no passado LOCAL (só relevante quando a data é hoje,
 * pois o backend pode não excluir slots passados da resposta).
 */
function slotNoPassado(hora: string): boolean {
  if (data.value !== hojeISO) return false
  const [hh, mm] = hora.split(":").map(Number)
  const agora = new Date()
  return hh < agora.getHours() || (hh === agora.getHours() && mm <= agora.getMinutes())
}

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

// Disponibilidade de agenda — substituem o HORARIOS fixo
const diaDisponibilidade = ref<DisponibilidadeDia | null>(null)
const carregandoSlots = ref(false)

/** Retorna os slots do dia, com checagem de passado sobreposta à resposta do backend. */
const slotsDodia = () => diaDisponibilidade.value?.slots ?? []

async function carregarDisponibilidade() {
  const profId = auth.usuario?.id
  if (!profId) return
  carregandoSlots.value = true
  horario.value = "" // limpa seleção ao trocar data
  try {
    const resp = await agendaService.disponibilidade({
      profissionalUsuarioId: profId,
      dataInicio: data.value,
      dataFim: data.value,
      duracaoMinutos: 30,
    })
    diaDisponibilidade.value = resp.dias[0] ?? null
  } catch {
    ui.toast("Não foi possível carregar os horários disponíveis", "error")
    diaDisponibilidade.value = null
  } finally {
    carregandoSlots.value = false
  }
}

/** Slot habilitado: disponível pela API E não no passado. */
function slotHabilitado(slot: DisponibilidadeSlot): boolean {
  return slot.disponivel && !slotNoPassado(slot.hora)
}

/** Dica de acessibilidade para slots indisponíveis. */
function ariaLabelSlot(slot: DisponibilidadeSlot): string {
  if (slotNoPassado(slot.hora)) return `${slot.hora} — horário no passado`
  if (!slot.disponivel) {
    if (slot.motivo === "agendado") return `${slot.hora} — já agendado`
    if (slot.motivo === "bloqueado") return `${slot.hora} — bloqueado`
    return `${slot.hora} — indisponível`
  }
  return slot.hora
}

// Sheet de seleção de paciente (busca + criar + editar)
const seletorOpen = ref(false)

// Quando o sheet abre no modo editar, passa o paciente selecionado
const pacienteParaEditar = ref<{ id: number; nomeCompleto: string } | null>(null)

onMounted(async () => {
  const salasResp = await salaService.listar().catch(() => [])
  salas.value = salasResp
  if (salasResp.length) salaId.value = salasResp[0].id
  await carregarDisponibilidade()
})

// Ao trocar data: recarrega disponibilidade
watch(data, carregarDisponibilidade)

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

      <!-- Dia fechado (fim de semana / sem expediente) -->
      <div v-if="!carregandoSlots && diaDisponibilidade?.status === 'fechado'" class="slots-fechado">
        <i class="fa-regular fa-calendar-xmark"></i>
        Sem atendimento neste dia
      </div>

      <!-- Skeleton enquanto carrega -->
      <div v-else-if="carregandoSlots" class="fav-chips">
        <div v-for="i in 8" :key="i" class="fav-chip slot-skeleton"></div>
      </div>

      <!-- Slots reais do backend -->
      <div v-else class="fav-chips">
        <button
          v-for="slot in slotsDodia()"
          :key="slot.hora"
          class="fav-chip"
          :class="{
            on: horario === slot.hora,
            indisponivel: !slotHabilitado(slot),
          }"
          :disabled="!slotHabilitado(slot)"
          :aria-label="ariaLabelSlot(slot)"
          :title="!slotHabilitado(slot) && slot.motivo ? (slot.motivo === 'agendado' ? 'Já agendado' : 'Bloqueado') : undefined"
          @click="horario = slot.hora"
        >{{ slot.hora }}</button>

        <!-- Nenhum slot disponível mas dia não fechado -->
        <p v-if="slotsDodia().length === 0" class="slots-vazio">
          Nenhum horário disponível
        </p>
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
/* Chips indisponíveis (agendado, bloqueado, passado) — cinza, sem cursor. */
.fav-chip.indisponivel,
.fav-chip:disabled {
  opacity: 0.38;
  cursor: not-allowed;
  pointer-events: none;
}

/* Skeleton de slots durante loading */
.slot-skeleton {
  background: var(--app-card-2);
  color: transparent;
  border-color: transparent;
  min-width: 56px;
  animation: pulse 1.2s ease-in-out infinite;
}
@keyframes pulse {
  0%, 100% { opacity: 0.5; }
  50% { opacity: 1; }
}

/* Mensagem de dia fechado */
.slots-fechado {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--app-text-faint);
  font-size: var(--fs-sm);
  padding: var(--space-3) 0 var(--space-5);
}
.slots-fechado i {
  font-size: var(--fs-lg);
}

/* Mensagem de sem slots */
.slots-vazio {
  color: var(--app-text-faint);
  font-size: var(--fs-sm);
  margin: var(--space-2) 0 var(--space-5);
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
