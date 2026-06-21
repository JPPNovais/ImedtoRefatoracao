<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { agendaService } from "@/services/agenda.service"
import { cobrancaService } from "@/services/cobranca.service"
import type { AgendamentoDetalhe } from "@/types"
import { useUiStore } from "@/stores/ui"
import { usePermissoesStore } from "@/stores/permissoes"
import { useShare } from "@/native/useShare"
import { horaDe, iniciais } from "@/lib/format"
import AppStatusPill from "@/components/ui/AppStatusPill.vue"
import BottomSheet from "@/components/ui/BottomSheet.vue"

const route = useRoute()
const router = useRouter()
const ui = useUiStore()
const permissoes = usePermissoesStore()
const share = useShare()

// RBAC (G2): edição da agenda e abertura do prontuário respeitam o vínculo.
const podeEditar = computed(() => permissoes.pode("agenda.editar"))
const podeProntuario = computed(() => permissoes.pode("prontuario.ver"))
const podePagar = computed(
  () =>
    permissoes.pode("financeiro_paciente.registrar") &&
    ["CheckIn", "EmAtendimento", "Concluido"].includes(ag.value?.status ?? ""),
)

const id = Number(route.params.id)
const ag = ref<AgendamentoDetalhe | null>(null)
const carregando = ref(true)
const acaoLoading = ref(false)
const atendSheetOpen = ref(false)
const menuOpen = ref(false)

// Check-in: valor sugerido (busca antes de confirmar)
const checkinSheetOpen = ref(false)
const checkinValorManual = ref("")

// Estados derivados do status
const statusAtual = computed(() => ag.value?.status ?? "")
const podeCheckin = computed(() => ["Agendado", "Confirmado"].includes(statusAtual.value))
const estaNoCheckin = computed(() => statusAtual.value === "CheckIn")
const foiConcluido = computed(() => ["Concluido", "EmAtendimento"].includes(statusAtual.value))
const chegouEm = computed(() => {
  if (!ag.value?.checkInEm) return null
  return horaDe(ag.value.checkInEm)
})

onMounted(async () => {
  try {
    ag.value = (await agendaService.obter(id)) as AgendamentoDetalhe
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
  if (ag.value)
    router.push({ path: `/paciente/${ag.value.pacienteId}/prontuario`, query: { nova: "1" } })
}

async function fazerCheckin() {
  if (!ag.value) return
  acaoLoading.value = true
  try {
    // Busca valor sugerido da tabela de preços do profissional deste agendamento.
    const sugerido = await cobrancaService.obterValorSugerido(ag.value.profissionalUsuarioId)
    const valor = sugerido.valorSugerido ?? 0

    if (valor > 0) {
      // Valor configurado: check-in direto sem modal.
      await confirmarCheckin(valor)
    } else {
      // Sem tabela de preços: solicita o valor antes de prosseguir.
      checkinValorManual.value = ""
      checkinSheetOpen.value = true
    }
  } catch {
    ui.toast("Não foi possível fazer o check-in", "error")
  } finally {
    acaoLoading.value = false
  }
}

async function confirmarCheckin(valorCobrado: number) {
  if (!ag.value) return
  await agendaService.checkin(ag.value.id, { tipoAtendimento: "Particular", valorCobrado })
  ag.value = { ...ag.value, status: "CheckIn", checkInEm: new Date().toISOString() }
  ui.toast("Check-in realizado")
}

async function confirmarCheckinManual() {
  if (!ag.value) return
  const valor = Number(checkinValorManual.value.replace(",", "."))
  if (!valor || valor <= 0) {
    ui.toast("Informe o valor da consulta", "error")
    return
  }
  acaoLoading.value = true
  checkinSheetOpen.value = false
  try {
    await confirmarCheckin(valor)
  } catch {
    ui.toast("Não foi possível fazer o check-in", "error")
  } finally {
    acaoLoading.value = false
  }
}

async function marcarAtendido() {
  if (!ag.value) return
  acaoLoading.value = true
  try {
    await agendaService.concluir(ag.value.id)
    ag.value = { ...ag.value, status: "Concluido" }
    // Abre o sheet para oferecer pagamento
    atendSheetOpen.value = true
  } catch {
    ui.toast("Não foi possível concluir", "error")
  } finally {
    acaoLoading.value = false
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

function abrirPagamento() {
  if (!ag.value) return
  atendSheetOpen.value = false
  const query: Record<string, string> = {
    agendamentoId: String(ag.value.id),
    pacienteNome: ag.value.pacienteNome,
  }
  router.push({ path: "/pagamento", query })
}

function concluirSemCobrar() {
  atendSheetOpen.value = false
  ui.toast("Atendimento concluído")
}
</script>

<template>
  <div class="push show">
    <div class="push-head">
      <button class="iconbtn" aria-label="Voltar" @click="voltar"><i class="fa-solid fa-arrow-left"></i></button>
      <div class="ph-title">Agendamento</div>
      <button class="iconbtn" aria-label="Mais ações" @click="menuOpen = true"><i class="fa-solid fa-ellipsis"></i></button>
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
          <span v-if="ag.temAlertaClinico" class="alert-mini">
            <i class="fa-solid fa-triangle-exclamation"></i> Alerta clínico
          </span>
        </span>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <div v-if="ag.observacoes">
        <div class="f-label">Observações</div>
        <div class="ag-obs">{{ ag.observacoes }}</div>
      </div>

      <div class="f-label">Ações</div>

      <!-- Check-in: só para Agendado/Confirmado -->
      <button
        v-if="podeEditar && podeCheckin"
        class="btn-primary-lg"
        :disabled="acaoLoading"
        @click="fazerCheckin"
      >
        <i
          class="fa-solid"
          :class="acaoLoading ? 'fa-circle-notch fa-spin' : 'fa-door-open'"
        ></i>
        Check-in — paciente chegou
      </button>

      <!-- Nota de chegada -->
      <div v-if="chegouEm" class="ag-arrived-note">
        <i class="fa-solid fa-circle-check" style="color: hsl(var(--success));"></i>
        Chegou às {{ chegouEm }}
      </div>

      <!-- Iniciar atendimento: só em CheckIn -->
      <button
        v-if="podeProntuario && estaNoCheckin"
        class="btn-primary-lg"
        @click="iniciarAtendimento"
      >
        <i class="fa-solid fa-play"></i> Iniciar atendimento
      </button>

      <!-- Atendido / Faltou -->
      <div v-if="podeEditar && (podeCheckin || estaNoCheckin)" class="btn-row" style="margin-top: 8px;">
        <button class="btn-soft ok" :disabled="acaoLoading" @click="marcarAtendido">
          <i class="fa-solid fa-check"></i> Atendido
        </button>
        <button class="btn-soft danger" @click="marcarFaltou">
          <i class="fa-solid fa-xmark"></i> Faltou
        </button>
      </div>

      <!-- Nota de conclusão -->
      <div v-if="foiConcluido" class="ag-arrived-note">
        <i class="fa-solid fa-circle-check" style="color: hsl(var(--success));"></i>
        Atendimento concluído
      </div>

      <!-- Nota de falta -->
      <div v-if="statusAtual === 'Faltou'" class="ag-arrived-note ag-faltou">
        <i class="fa-regular fa-circle-xmark"></i>
        Paciente não compareceu
      </div>

      <!-- Registrar pagamento -->
      <button v-if="podePagar" class="btn-outline" style="margin-top: 8px;" @click="abrirPagamento">
        <i class="fa-solid fa-hand-holding-dollar"></i> Registrar pagamento
      </button>

      <button v-if="podeEditar" class="btn-outline" @click="ui.toast('Abrindo reagendamento')">
        <i class="fa-solid fa-arrows-rotate"></i> Reagendar
      </button>
      <button class="btn-outline" @click="enviarConfirmacao">
        <i class="fa-brands fa-whatsapp"></i> Enviar confirmação
      </button>
    </div>

    <!-- Sheet de valor do check-in (quando profissional não tem tabela de preços configurada) -->
    <BottomSheet v-model:open="checkinSheetOpen">
      <div style="text-align: center; padding: 4px 0 2px;">
        <div class="dlg-ic" style="margin: 0 auto 12px;">
          <i class="fa-solid fa-door-open"></i>
        </div>
        <div class="sh-title" style="text-align: center; margin: 0;">Valor da consulta</div>
        <div class="sh-sub" style="text-align: center;">Informe o valor a ser cobrado para registrar o check-in.</div>
      </div>
      <input
        v-model="checkinValorManual"
        class="linput"
        type="number"
        inputmode="decimal"
        placeholder="0,00"
        style="margin-top: 12px; width: 100%;"
        @keyup.enter="confirmarCheckinManual"
      />
      <button class="btn-primary-lg" style="margin-top: 12px;" :disabled="acaoLoading" @click="confirmarCheckinManual">
        <i v-if="acaoLoading" class="fa-solid fa-circle-notch fa-spin"></i>
        <i v-else class="fa-solid fa-door-open"></i>
        Confirmar check-in
      </button>
      <button class="btn-outline" style="margin-bottom: 0;" @click="checkinSheetOpen = false">
        <i class="fa-solid fa-xmark"></i> Cancelar
      </button>
    </BottomSheet>

    <!-- Sheet pós-atendimento -->
    <BottomSheet v-model:open="atendSheetOpen">
      <div style="text-align: center; padding: 4px 0 2px;">
        <div class="dlg-ic" style="margin: 0 auto 12px; background: hsl(var(--success) / 0.12); color: hsl(var(--success));">
          <i class="fa-solid fa-circle-check"></i>
        </div>
        <div class="sh-title" style="text-align: center; margin: 0;">Atendimento concluído</div>
        <div class="sh-sub" style="text-align: center;">
          {{ ag?.pacienteNome }} foi atendido. Deseja registrar o pagamento agora?
        </div>
      </div>
      <button class="btn-primary-lg" style="margin-top: 16px;" @click="abrirPagamento">
        <i class="fa-solid fa-hand-holding-dollar"></i> Registrar pagamento
      </button>
      <button class="btn-outline" style="margin-bottom: 0;" @click="concluirSemCobrar">
        <i class="fa-solid fa-check"></i> Concluir sem cobrar
      </button>
    </BottomSheet>

    <!-- Menu "..." — ações reais do agendamento -->
    <BottomSheet v-model:open="menuOpen" titulo="Ações" closable>
      <div v-if="podeEditar" class="med-row" @click="marcarFaltou(); menuOpen = false">
        <div class="mi"><i class="fa-regular fa-circle-xmark"></i></div>
        <b>Registrar falta</b>
      </div>
      <div class="med-row" @click="enviarConfirmacao(); menuOpen = false">
        <div class="mi"><i class="fa-brands fa-whatsapp"></i></div>
        <b>Enviar confirmação</b>
      </div>
      <div v-if="podeProntuario && ag" class="med-row" @click="irFicha(); menuOpen = false">
        <div class="mi"><i class="fa-solid fa-id-card"></i></div>
        <b>Ver ficha do paciente</b>
      </div>
    </BottomSheet>
  </div>
</template>

<style scoped>
.ag-arrived-note {
  display: flex;
  align-items: center;
  gap: 8px;
  background: hsl(var(--success) / 0.08);
  border: 1px solid hsl(var(--success) / 0.2);
  border-radius: var(--radius-xl);
  padding: 12px 14px;
  font-size: var(--fs-sm);
  font-weight: var(--fw-semibold);
  color: hsl(160 70% 34%);
  margin-bottom: 8px;
}
.ag-faltou {
  background: hsl(var(--error) / 0.07);
  border-color: hsl(var(--error) / 0.18);
  color: hsl(var(--error));
}
</style>
