<!--
    AgendamentoFormFields — campos compartilhados entre o drawer de "Novo agendamento"
    e o drawer de "Editar agendamento" (mantém paridade visual).

    Em modo "editar", apenas o paciente fica bloqueado (para trocar paciente, cancele
    e crie um novo agendamento). Profissional, especialidade, data/hora, tipo,
    duração, contato e observações são editáveis.
-->
<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { vMaska } from "maska/vue"
import SlotPicker from "@/components/agenda/SlotPicker.vue"
import { AppButton } from "@/components/ui"
import type { Agendamento } from "@/services/agendaService"
import type { ProfissionalVinculado } from "@/services/vinculoService"
import type { PacienteListaItem } from "@/services/pacienteService"

export interface AgendamentoFormModel {
    pacienteId: number
    profissionalUsuarioId: string
    data: string
    hora: string
    duracaoMin: number
    tipoServico: string
    especialidade: string
    contato: string
    observacoes: string | null
}

const props = defineProps<{
    modelValue: AgendamentoFormModel
    profissionais: ProfissionalVinculado[]
    pacientes: PacienteListaItem[]
    agendamentos: Agendamento[]
    /** Quando "editar", paciente/profissional/contato ficam disabled. */
    modo: "criar" | "editar"
    /** Lista filtrada (pacientes) para autocomplete; obrigatório em modo "criar". */
    pacientesFiltrados?: PacienteListaItem[]
    /** Texto digitado no autocomplete de paciente (somente "criar"). */
    pacienteQuery?: string
    pacienteDropdownAberto?: boolean
}>()

const emit = defineEmits<{
    "update:modelValue":          [AgendamentoFormModel]
    "update:pacienteQuery":       [string]
    "update:pacienteDropdownAberto": [boolean]
    "selecionarPaciente":         [PacienteListaItem]
    "abrirCadastroPaciente":      []
}>()

const m = computed(() => props.modelValue)
const slotPickerAberto = ref(false)

function onPacienteBlur() {
    setTimeout(() => emit("update:pacienteDropdownAberto", false), 180)
}

// ─── Constantes do domínio (mesmas do legado) ────────────────────────────────
const DURACOES = [15, 30, 45, 60, 90, 120]
const TIPOS_CONSULTA = [
    "Consulta",
    "Retorno",
    "Primeira consulta",
    "Exame",
    "Procedimento",
    "Avaliação",
    "Teleconsulta",
    "Emergência",
    "Encaixe",
]

function labelDuracao(min: number) {
    if (min < 60) return `${min} minutos`
    const h = Math.floor(min / 60), mm = min % 60
    return mm ? `${h}h ${mm}min` : `${h} hora${h > 1 ? "s" : ""}`
}

function setCampo<K extends keyof AgendamentoFormModel>(k: K, v: AgendamentoFormModel[K]) {
    emit("update:modelValue", { ...m.value, [k]: v })
}

// ─── Auto-fill ───────────────────────────────────────────────────────────────
// Auto-preenche especialidade ao trocar de profissional.
watch(() => m.value.profissionalUsuarioId, (id) => {
    const p = props.profissionais.find(x => x.usuarioId === id)
    if (p?.especialidade && !m.value.especialidade) setCampo("especialidade", p.especialidade)
})

// Auto-preenche contato ao trocar de paciente.
watch(() => m.value.pacienteId, (id) => {
    const p = props.pacientes.find(pac => pac.id === id)
    if (p?.telefone && !m.value.contato) setCampo("contato", p.telefone)
})

// ─── Computeds visuais ───────────────────────────────────────────────────────
const horaFim = computed(() => {
    if (!m.value.data || !m.value.hora) return ""
    const ini = new Date(`${m.value.data}T${m.value.hora}`)
    const fim = new Date(ini.getTime() + m.value.duracaoMin * 60_000)
    return fim.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
})

const dataCardLabel = computed(() => {
    if (!m.value.data) return ""
    const [y, mes, d] = m.value.data.split("-")
    return new Date(+y, +mes - 1, +d).toLocaleDateString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
    })
})

const especialidadesDisponiveis = computed<string[]>(() => {
    const set = new Set<string>()
    for (const p of props.profissionais) {
        if (p.especialidade && p.especialidade.trim()) set.add(p.especialidade.trim())
    }
    return Array.from(set).sort((a, b) => a.localeCompare(b, "pt-BR"))
})

const profissionalSelecionadoNome = computed(() =>
    props.profissionais.find(p => p.usuarioId === m.value.profissionalUsuarioId)?.nomeCompleto ?? ""
)

const pacienteSelecionado = computed(() =>
    props.pacientes.find(p => p.id === m.value.pacienteId) ?? null
)

// ─── Slot picker ─────────────────────────────────────────────────────────────
function onSlotSelecionado(payload: { data: string; hora: string }) {
    emit("update:modelValue", { ...m.value, data: payload.data, hora: payload.hora })
    slotPickerAberto.value = false
}

const agendamentosParaSlot = computed(() => {
    // Em edição, exclui o próprio agendamento (passado pelo parent já filtrado se necessário).
    return props.agendamentos
})

</script>

<template>
    <!-- Profissional (deve vir antes de data para que o SlotPicker tenha o ID disponível) -->
    <div class="campo">
        <label class="campo-label">Profissional <span class="obrig">*</span></label>
        <select
            :value="m.profissionalUsuarioId"
            class="input-field"
            @change="setCampo('profissionalUsuarioId', ($event.target as HTMLSelectElement).value)"
        >
            <option value="" disabled>Selecione...</option>
            <option v-for="p in profissionais" :key="p.usuarioId" :value="p.usuarioId">
                {{ p.nomeCompleto || p.email }}
            </option>
        </select>
    </div>

    <!-- Card de data e horário (clicável → SlotPicker) -->
    <div class="campo">
        <label class="campo-label">Data e horário <span class="obrig">*</span></label>
        <div
            class="data-card"
            :class="m.profissionalUsuarioId ? 'data-card--clicavel' : 'data-card--desabilitado'"
            @click="m.profissionalUsuarioId && (slotPickerAberto = true)"
        >
            <span class="data-card-icon">📅</span>
            <div class="data-card-info">
                <span class="data-card-data">{{ dataCardLabel || (m.profissionalUsuarioId ? "Selecione a data" : "Selecione um profissional primeiro") }}</span>
                <span class="data-card-hora" v-if="m.hora">{{ m.hora }} — {{ horaFim }}</span>
            </div>
            <AppButton
                v-if="m.profissionalUsuarioId"
                variant="secondary" size="sm" type="button"
                @click.stop="slotPickerAberto = true"
            >
                ✏️ Alterar
            </AppButton>
        </div>
    </div>

    <!-- Duração -->
    <div class="campo">
        <label class="campo-label">Duração da consulta</label>
        <select :value="m.duracaoMin" class="input-field" @change="setCampo('duracaoMin', +($event.target as HTMLSelectElement).value)">
            <option v-for="d in DURACOES" :key="d" :value="d">{{ labelDuracao(d) }}</option>
        </select>
    </div>

    <!-- Tipo da consulta -->
    <div class="campo">
        <label class="campo-label">Tipo da consulta <span class="obrig">*</span></label>
        <select :value="m.tipoServico" class="input-field" @change="setCampo('tipoServico', ($event.target as HTMLSelectElement).value)">
            <option value="" disabled>Selecione...</option>
            <option v-for="t in TIPOS_CONSULTA" :key="t" :value="t">{{ t }}</option>
        </select>
    </div>

    <!-- Especialidade -->
    <div class="campo">
        <label class="campo-label">Especialidade</label>
        <select
            :value="m.especialidade"
            class="input-field"
            :disabled="especialidadesDisponiveis.length === 0"
            @change="setCampo('especialidade', ($event.target as HTMLSelectElement).value)"
        >
            <option value="">
                {{ especialidadesDisponiveis.length === 0
                    ? "Nenhum profissional com especialidade cadastrada"
                    : "Selecione..." }}
            </option>
            <option v-for="e in especialidadesDisponiveis" :key="e" :value="e">{{ e }}</option>
        </select>
        <p v-if="especialidadesDisponiveis.length === 0 && modo === 'criar'" class="campo-hint">
            Cadastre a especialidade em <strong>Configurações</strong> ou convide profissionais com especialidades definidas.
        </p>
    </div>

    <!-- Paciente: autocomplete em "criar" / display em "editar" -->
    <div v-if="modo === 'criar'" class="campo combobox">
        <label class="campo-label">Nome do paciente <span class="obrig">*</span></label>
        <input
            :value="pacienteQuery"
            class="input-field"
            placeholder="Digite para buscar ou registrar..."
            autocomplete="off"
            @input="emit('update:pacienteQuery', ($event.target as HTMLInputElement).value)"
            @focus="emit('update:pacienteDropdownAberto', true)"
            @blur="onPacienteBlur"
        />
        <div v-if="pacienteDropdownAberto" class="ac-dropdown">
            <button
                v-for="p in (pacientesFiltrados ?? [])" :key="p.id"
                type="button"
                class="ac-item"
                @mousedown.prevent="emit('selecionarPaciente', p)"
            >
                <span class="ac-nome">{{ p.nomeCompleto }}</span>
                <span v-if="p.telefone || p.cpf" class="ac-meta">
                    {{ p.telefone ?? p.cpf }}
                </span>
            </button>
            <button
                type="button"
                class="ac-item ac-novo"
                @mousedown.prevent="emit('abrirCadastroPaciente')"
            >
                <span class="ac-nome">
                    ＋ Cadastrar novo paciente<span v-if="pacienteQuery"> "{{ pacienteQuery }}"</span>
                </span>
            </button>
        </div>
    </div>

    <div v-else class="campo">
        <label class="campo-label">Paciente</label>
        <input
            :value="pacienteSelecionado?.nomeCompleto ?? ''"
            class="input-field"
            disabled
            readonly
        />
    </div>

    <!-- Contato -->
    <div class="campo">
        <label class="campo-label">Contato</label>
        <input
            :value="m.contato"
            v-maska="'(##) #####-####'"
            class="input-field"
            placeholder="(00) 00000-0000"
            type="tel"
            inputmode="numeric"
            @input="setCampo('contato', ($event.target as HTMLInputElement).value)"
        />
    </div>

    <!-- Observações -->
    <div class="campo">
        <label class="campo-label">Observações</label>
        <textarea
            :value="m.observacoes ?? ''"
            class="input-field"
            rows="3"
            placeholder="Opcional..."
            @input="setCampo('observacoes', ($event.target as HTMLTextAreaElement).value || null)"
        ></textarea>
    </div>

    <!-- Modal de seleção de horário -->
    <SlotPicker
        :aberto="slotPickerAberto"
        :titulo="profissionalSelecionadoNome"
        :profissionalId="m.profissionalUsuarioId"
        :dataInicial="m.data"
        @fechar="slotPickerAberto = false"
        @selecionar="onSlotSelecionado"
    />
</template>

<style scoped>
.campo { display: flex; flex-direction: column; gap: 0.3rem; }
.campo-label { font-size: 0.82em; font-weight: 600; color: var(--text-muted); }
.campo-hint  { font-size: 0.75em; color: var(--text-faint); margin: 0.2rem 0 0; }
.obrig { color: var(--danger); }

.input-field {
    padding: 0.5rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text); transition: border-color 0.15s;
}
.input-field:focus { outline: none; border-color: hsl(var(--primary)); }
.input-field:disabled { background: #f9fafb; color: var(--text-muted); cursor: not-allowed; }

.data-card {
    display: flex; align-items: center; gap: 0.75rem;
    border: 2px solid var(--border); border-radius: var(--radius);
    padding: 0.75rem 1rem; background: var(--bg-card);
    transition: border-color 0.15s;
}
.data-card--clicavel { cursor: pointer; }
.data-card--clicavel:hover { border-color: hsl(var(--primary)); }
.data-card--desabilitado { cursor: default; opacity: 0.6; }
.data-card--desabilitado .data-card-data { color: var(--text-muted); font-style: italic; }
.data-card-icon { font-size: 1.3em; flex-shrink: 0; }
.data-card-info { flex: 1; display: flex; flex-direction: column; gap: 0.1rem; min-width: 0; }
.data-card-data { font-weight: 700; font-size: 0.95em; }
.data-card-hora { font-size: 0.82em; color: var(--text-muted); }

/* Autocomplete (combobox) */
.combobox { position: relative; }
.ac-dropdown {
    position: absolute; top: 100%; left: 0; right: 0;
    margin-top: 4px; max-height: 220px; overflow-y: auto;
    background: var(--bg-card); border: 1px solid var(--border-strong);
    border-radius: var(--radius); box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
    z-index: 10;
}
.ac-item {
    display: flex; justify-content: space-between; align-items: center;
    width: 100%; padding: 0.55rem 0.75rem;
    border: none; background: none; cursor: pointer; text-align: left;
    border-bottom: 1px solid var(--border);
    font-family: inherit; font-size: 0.875em;
    transition: background 0.1s;
}
.ac-item:last-child { border-bottom: none; }
.ac-item:hover { background: var(--bg-hover); }
.ac-nome { font-weight: 600; }
.ac-meta { font-size: 0.8em; color: var(--text-muted); }
.ac-item.ac-novo { background: var(--bg-hover); font-weight: 700; }
.ac-item.ac-novo:hover { background: hsl(var(--primary-light)); }
.ac-item.ac-novo .ac-nome { color: hsl(var(--primary)); }
</style>
