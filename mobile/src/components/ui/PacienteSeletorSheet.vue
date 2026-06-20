<script setup lang="ts">
/**
 * PacienteSeletorSheet — BottomSheet reutilizável para selecionar, criar ou editar
 * dados básicos de um paciente. Usado no FAB (receita/atestado/exame) e no
 * NovoAgendamentoView.
 *
 * Modos:
 *  - "selecionar": busca-rápida + opção de criar novo
 *  - "editar": formulário pré-preenchido para atualizar dados básicos
 *
 * Emite @selecionado com { id, nomeCompleto } ao escolher ou criar.
 * Emite @atualizado com { id, nomeCompleto } ao salvar edição.
 *
 * RBAC: botão "Novo paciente" e "Editar dados" só aparecem se permissoes.pode("pacientes").
 * Validação real no backend (422 → mensagemDeErro).
 * Nunca loga PII (LGPD).
 */
import { ref, watch, computed } from "vue"
import { usePermissoesStore } from "@/stores/permissoes"
import { useUiStore } from "@/stores/ui"
import { pacienteService } from "@/services/paciente.service"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { mensagemDeErro } from "@/lib/erros"
import { iniciais } from "@/lib/format"
import BottomSheet from "@/components/ui/BottomSheet.vue"
import AppSearchInput from "@/components/ui/AppSearchInput.vue"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import type { PacientePayloadRapido } from "@/types"

interface PacienteRef {
  id: number
  nomeCompleto: string
}

const props = defineProps<{
  open: boolean
  /** Quando fornecido, o sheet abre diretamente no modo editar para este paciente. */
  pacienteParaEditar?: PacienteRef | null
}>()

const emit = defineEmits<{
  "update:open": [v: boolean]
  /** Emitido ao selecionar um paciente existente ou criar um novo. */
  selecionado: [p: PacienteRef]
  /** Emitido ao salvar uma edição de dados básicos. */
  atualizado: [p: PacienteRef]
}>()

const permissoes = usePermissoesStore()
const ui = useUiStore()

// ── Modo: "lista" (busca + criar) ou "form" (criar/editar) ──────────────────
type Modo = "lista" | "form"
const modo = ref<Modo>("lista")
// true = editando paciente existente; false = criando novo
const editando = ref(false)
const editandoId = ref<number | null>(null)

// ── Busca ───────────────────────────────────────────────────────────────────
const buscaRaw = ref("")
const busca = useDebouncedRef("", 350)
watch(buscaRaw, (v) => { busca.value = v })

const lista = ref<PacienteRef[]>([])
const carregando = ref(false)

async function buscarPacientes() {
  carregando.value = true
  try {
    lista.value = await pacienteService.buscaRapida(busca.value || undefined, 20)
  } catch {
    // falha silenciosa na busca — lista fica vazia (AppEmptyState cobre)
    lista.value = []
  } finally {
    carregando.value = false
  }
}

// Recarrega quando o valor debounced muda
watch(busca, buscarPacientes)

// Carrega lista inicial ao abrir o sheet (modo lista)
watch(() => props.open, (v) => {
  if (!v) return
  if (props.pacienteParaEditar) {
    // Abriu com paciente pré-definido → vai direto ao formulário de edição
    abrirEdicao(props.pacienteParaEditar)
  } else {
    modo.value = "lista"
    buscaRaw.value = ""
    busca.value = ""
    buscarPacientes()
  }
}, { immediate: false })

// ── Formulário rápido ───────────────────────────────────────────────────────
const form = ref<PacientePayloadRapido>({ nomeCompleto: "" })
const salvando = ref(false)

function abrirNovo() {
  editando.value = false
  editandoId.value = null
  form.value = { nomeCompleto: "" }
  modo.value = "form"
}

function abrirEdicao(p: PacienteRef) {
  editando.value = true
  editandoId.value = p.id
  // Pré-preenche só o nome (não expõe PII de telefone/CPF na abertura — LGPD)
  form.value = { nomeCompleto: p.nomeCompleto }
  modo.value = "form"
}

function voltarParaLista() {
  modo.value = "lista"
}

async function salvar() {
  if (!form.value.nomeCompleto.trim()) {
    ui.toast("Nome completo é obrigatório", "error")
    return
  }

  // Monta payload sem campos vazios (LGPD: minimização)
  const payload: PacientePayloadRapido = {
    nomeCompleto: form.value.nomeCompleto.trim(),
  }
  if (form.value.telefone?.trim()) payload.telefone = form.value.telefone.trim()
  if (form.value.email?.trim()) payload.email = form.value.email.trim()
  if (form.value.dataNascimento) payload.dataNascimento = form.value.dataNascimento
  if (form.value.cpf?.trim()) payload.cpf = form.value.cpf.trim()

  salvando.value = true
  try {
    if (editando.value && editandoId.value !== null) {
      await pacienteService.atualizar(editandoId.value, payload)
      ui.toast("Dados atualizados")
      emit("atualizado", { id: editandoId.value, nomeCompleto: payload.nomeCompleto })
      emit("update:open", false)
    } else {
      await pacienteService.criar(payload)
      ui.toast("Paciente cadastrado")
      // Busca o paciente recém-criado pelo nome para obter o id
      const recemCriados = await pacienteService.buscaRapida(payload.nomeCompleto, 5).catch(() => [])
      const criado = recemCriados[0] ?? { id: 0, nomeCompleto: payload.nomeCompleto }
      emit("selecionado", criado)
      emit("update:open", false)
    }
  } catch (err) {
    ui.toast(mensagemDeErro(err, "Não foi possível salvar"), "error")
  } finally {
    salvando.value = false
  }
}

function escolher(p: PacienteRef) {
  emit("selecionado", p)
  emit("update:open", false)
}

const podeCriarOuEditar = computed(() => permissoes.pode("pacientes"))

const tituloSheet = computed(() => {
  if (modo.value === "form") {
    return editando.value ? "Editar dados" : "Novo paciente"
  }
  return "Selecionar paciente"
})
</script>

<template>
  <BottomSheet
    :open="open"
    :titulo="tituloSheet"
    tall
    @update:open="emit('update:open', $event)"
  >
    <!-- MODO LISTA: busca + resultados -->
    <template v-if="modo === 'lista'">
      <AppSearchInput
        v-model="buscaRaw"
        placeholder="Buscar por nome…"
      />

      <!-- Botão criar novo (RBAC: só quem pode criar pacientes) -->
      <button
        v-if="podeCriarOuEditar"
        class="sps-novo"
        @click="abrirNovo"
      >
        <div class="ic ic-violet"><i class="fa-solid fa-user-plus"></i></div>
        <div class="tx"><b>Novo paciente</b><span>Cadastrar agora</span></div>
        <i class="fa-solid fa-chevron-right chev"></i>
      </button>

      <!-- Skeleton enquanto carrega -->
      <div v-if="carregando" class="plist">
        <div v-for="i in 4" :key="i" class="skrow">
          <div class="sk sk-av"></div>
          <div style="flex:1; display:flex; flex-direction:column; gap:8px">
            <div class="sk sk-l" style="width:65%"></div>
            <div class="sk sk-l" style="width:40%"></div>
          </div>
        </div>
      </div>

      <!-- Lista de resultados -->
      <div v-else-if="lista.length" class="plist">
        <div
          v-for="p in lista"
          :key="p.id"
          class="prow"
          @click="escolher(p)"
        >
          <div class="av">{{ iniciais(p.nomeCompleto) }}</div>
          <div class="pinfo">
            <div class="pn"><b>{{ p.nomeCompleto }}</b></div>
          </div>
          <i class="fa-solid fa-chevron-right chev"></i>
        </div>
      </div>

      <!-- Estado vazio -->
      <AppEmptyState
        v-else-if="!carregando"
        icon="fa-user-slash"
        titulo="Nenhum paciente encontrado"
        :descricao="buscaRaw ? 'Tente outro nome ou cadastre um novo.' : 'Nenhum paciente cadastrado ainda.'"
      />
    </template>

    <!-- MODO FORMULÁRIO: criar ou editar -->
    <template v-else>
      <button class="sps-back" @click="voltarParaLista" v-if="!pacienteParaEditar">
        <i class="fa-solid fa-arrow-left"></i> Voltar
      </button>

      <div class="f-label">Nome completo *</div>
      <input
        v-model="form.nomeCompleto"
        class="sps-input"
        placeholder="Nome do paciente"
        autocomplete="off"
        aria-label="Nome completo"
      />

      <div class="f-label" style="margin-top: var(--space-4)">Telefone</div>
      <input
        v-model="form.telefone"
        class="sps-input"
        placeholder="(11) 99999-9999"
        type="tel"
        autocomplete="off"
        inputmode="tel"
        aria-label="Telefone"
      />

      <div class="f-label" style="margin-top: var(--space-4)">E-mail</div>
      <input
        v-model="form.email"
        class="sps-input"
        placeholder="email@exemplo.com"
        type="email"
        autocomplete="off"
        inputmode="email"
        aria-label="E-mail"
      />

      <div class="f-label" style="margin-top: var(--space-4)">Data de nascimento</div>
      <input
        v-model="form.dataNascimento"
        class="sps-input"
        type="date"
        aria-label="Data de nascimento"
      />

      <div class="f-label" style="margin-top: var(--space-4)">CPF</div>
      <input
        v-model="form.cpf"
        class="sps-input"
        placeholder="000.000.000-00"
        type="text"
        inputmode="numeric"
        autocomplete="off"
        aria-label="CPF"
      />

      <button
        class="btn-primary-lg"
        style="margin-top: var(--space-6)"
        :disabled="salvando"
        @click="salvar"
      >
        <i v-if="salvando" class="fa-solid fa-spinner fa-spin"></i>
        <i v-else class="fa-solid fa-check"></i>
        {{ salvando ? "Salvando…" : (editando ? "Salvar alterações" : "Cadastrar paciente") }}
      </button>
    </template>

    <template v-if="modo === 'lista'" #footer></template>
  </BottomSheet>
</template>

<style scoped>
/* Botão de criar novo — espelha .act-row do design system */
.sps-novo {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 13px 8px;
  border-radius: var(--radius-xl);
  cursor: pointer;
  width: 100%;
  border: 0;
  background: transparent;
  font: inherit;
  margin-bottom: var(--space-3);
  min-height: 44px;
}
.sps-novo:active {
  background: var(--app-card-2);
}
.sps-novo .ic {
  width: 44px;
  height: 44px;
  flex: none;
  border-radius: 13px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--fs-lg);
}
.sps-novo .tx {
  flex: 1;
  text-align: left;
}
.sps-novo .tx b {
  display: block;
  font-size: var(--fs-sm);
  font-weight: var(--fw-bold);
  color: var(--app-text);
}
.sps-novo .tx span {
  font-size: var(--fs-xs);
  color: var(--app-text-dim);
  font-weight: var(--fw-semibold);
}
.sps-novo .chev {
  margin-left: auto;
  color: var(--app-text-faint);
  font-size: var(--fs-sm);
}

/* Botão voltar no formulário */
.sps-back {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  border: 0;
  background: transparent;
  font: inherit;
  font-size: var(--fs-sm);
  font-weight: var(--fw-bold);
  color: var(--brand);
  cursor: pointer;
  padding: var(--space-2) 0;
  margin-bottom: var(--space-4);
  min-height: 44px;
}
.sps-back:active {
  opacity: 0.7;
}

/* Campo de texto do formulário */
.sps-input {
  width: 100%;
  padding: var(--space-3) var(--space-4);
  border: 1.5px solid var(--app-border);
  border-radius: var(--radius-lg);
  background: var(--app-card);
  font: inherit;
  font-size: var(--fs-sm);
  color: var(--app-text);
  outline: none;
  min-height: 44px;
}
.sps-input:focus {
  border-color: var(--brand);
  box-shadow: 0 0 0 3px var(--brand-soft);
}
.sps-input::placeholder {
  color: var(--app-text-faint);
}
</style>
