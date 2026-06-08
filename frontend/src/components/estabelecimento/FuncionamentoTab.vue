<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue"
import { AppButton, AppCard, AppDatePicker, AppField, AppInput } from "@/components/ui"
import type { Estabelecimento, HorarioBloqueado, DataBloqueada } from "@/services/estabelecimentoService"
import { estabelecimentoService } from "@/services/estabelecimentoService"

const props = defineProps<{
    estabelecimento: Estabelecimento
    podeEditar: boolean
}>()

const emit = defineEmits<{
    (e: "atualizado"): void
}>()

const DIAS_SEMANA = [
    { id: 0, label: "Dom" },
    { id: 1, label: "Seg" },
    { id: 2, label: "Ter" },
    { id: 3, label: "Qua" },
    { id: 4, label: "Qui" },
    { id: 5, label: "Sex" },
    { id: 6, label: "Sáb" },
]

// "HH:mm:ss" → "HH:mm" (compatível com input type=time).
function paraInputTime(s: string): string {
    if (!s) return ""
    return s.length >= 5 ? s.slice(0, 5) : s
}

const form = reactive({
    horarioInicio: paraInputTime(props.estabelecimento.horarioInicio),
    horarioFim:    paraInputTime(props.estabelecimento.horarioFim),
    duracaoConsultaMin:  props.estabelecimento.duracaoConsultaPadraoMinutos ?? 30,
    intervaloEntreConsultasMin: props.estabelecimento.intervaloEntreConsultasMinutos ?? 0,
    diasSemana:    [...props.estabelecimento.diasSemanaFuncionamento].sort((a, b) => a - b),
    horariosBloqueados: props.estabelecimento.horariosBloqueados.map(h => ({
        id: h.id,
        inicio: paraInputTime(h.inicio),
        fim:    paraInputTime(h.fim),
        descricao: h.descricao ?? "",
    })) as HorarioBloqueado[],
    datasBloqueadas: props.estabelecimento.datasBloqueadas.map(d => ({
        id: d.id,
        data: d.data,
        descricao: d.descricao ?? "",
    })) as DataBloqueada[],
})

watch(() => props.estabelecimento, (e) => {
    form.horarioInicio = paraInputTime(e.horarioInicio)
    form.horarioFim = paraInputTime(e.horarioFim)
    form.duracaoConsultaMin = e.duracaoConsultaPadraoMinutos ?? 30
    form.intervaloEntreConsultasMin = e.intervaloEntreConsultasMinutos ?? 0
    form.diasSemana = [...e.diasSemanaFuncionamento].sort((a, b) => a - b)
    form.horariosBloqueados = e.horariosBloqueados.map(h => ({
        id: h.id, inicio: paraInputTime(h.inicio), fim: paraInputTime(h.fim), descricao: h.descricao ?? "",
    }))
    form.datasBloqueadas = e.datasBloqueadas.map(d => ({ id: d.id, data: d.data, descricao: d.descricao ?? "" }))
})

const erroHorario = computed<string | null>(() => {
    if (!form.horarioInicio || !form.horarioFim) return null
    return form.horarioFim <= form.horarioInicio
        ? "O término deve ser maior que o início."
        : null
})

const erroDuracao = computed<string | null>(() => {
    const d = Number(form.duracaoConsultaMin)
    if (!Number.isFinite(d) || d < 5 || d > 480)
        return "Duração deve estar entre 5 e 480 minutos."
    return null
})

const erroIntervalo = computed<string | null>(() => {
    const i = Number(form.intervaloEntreConsultasMin)
    if (!Number.isFinite(i) || i < 0 || i > 240)
        return "Intervalo deve estar entre 0 e 240 minutos."
    return null
})

function toggleDiaSemana(id: number) {
    if (!props.podeEditar) return
    const idx = form.diasSemana.indexOf(id)
    if (idx >= 0) form.diasSemana.splice(idx, 1)
    else { form.diasSemana.push(id); form.diasSemana.sort((a, b) => a - b) }
}

// ─── Adicionar horário bloqueado ──────────────────────────────────────────────
const novoHorario = reactive({ inicio: "", fim: "", descricao: "" })
const erroNovoHorario = ref<string | null>(null)

function adicionarHorarioBloqueado() {
    erroNovoHorario.value = null
    if (!novoHorario.inicio || !novoHorario.fim) {
        erroNovoHorario.value = "Informe o início e o término do bloqueio."
        return
    }
    if (novoHorario.fim <= novoHorario.inicio) {
        erroNovoHorario.value = "O término deve ser maior que o início."
        return
    }
    if (form.horarioInicio && form.horarioFim) {
        if (novoHorario.inicio < form.horarioInicio || novoHorario.fim > form.horarioFim) {
            erroNovoHorario.value = "O bloqueio deve estar dentro do horário de funcionamento."
            return
        }
    }

    form.horariosBloqueados.push({
        id: crypto.randomUUID(),
        inicio: novoHorario.inicio,
        fim: novoHorario.fim,
        descricao: novoHorario.descricao.trim(),
    })
    novoHorario.inicio = ""
    novoHorario.fim = ""
    novoHorario.descricao = ""
}

function removerHorarioBloqueado(id: string) {
    form.horariosBloqueados = form.horariosBloqueados.filter(h => h.id !== id)
}

// ─── Adicionar data bloqueada ─────────────────────────────────────────────────
const novaData = reactive({ data: "", descricao: "" })
const erroNovaData = ref<string | null>(null)

function adicionarDataBloqueada() {
    erroNovaData.value = null
    if (!novaData.data) {
        erroNovaData.value = "Informe a data a ser bloqueada."
        return
    }
    if (form.datasBloqueadas.some(d => d.data === novaData.data)) {
        erroNovaData.value = "Esta data já está na lista."
        return
    }

    form.datasBloqueadas.push({
        id: crypto.randomUUID(),
        data: novaData.data,
        descricao: novaData.descricao.trim(),
    })
    form.datasBloqueadas.sort((a, b) => a.data.localeCompare(b.data))
    novaData.data = ""
    novaData.descricao = ""
}

function removerDataBloqueada(id: string) {
    form.datasBloqueadas = form.datasBloqueadas.filter(d => d.id !== id)
}

function fmtData(s: string) {
    const [y, m, d] = s.split("-")
    return `${d}/${m}/${y}`
}

// ─── Salvar ───────────────────────────────────────────────────────────────────
const salvando = ref(false)
const erroSalvar = ref<string | null>(null)
const msgOk = ref<string | null>(null)

const podeSalvar = computed(() =>
    !!form.horarioInicio
    && !!form.horarioFim
    && !erroHorario.value
    && !erroDuracao.value
    && !erroIntervalo.value
    && form.diasSemana.length > 0,
)

async function salvar() {
    if (!props.podeEditar) return
    erroSalvar.value = null
    msgOk.value = null
    if (!podeSalvar.value) return

    salvando.value = true
    try {
        await estabelecimentoService.atualizarFuncionamento(props.estabelecimento.id, {
            horarioInicio: form.horarioInicio,
            horarioFim:    form.horarioFim,
            duracaoConsultaPadraoMinutos: Number(form.duracaoConsultaMin),
            intervaloEntreConsultasMinutos: Number(form.intervaloEntreConsultasMin),
            diasSemana:    [...form.diasSemana].sort((a, b) => a - b),
            horariosBloqueados: form.horariosBloqueados.map(h => ({
                id: h.id, inicio: h.inicio, fim: h.fim, descricao: h.descricao,
            })),
            datasBloqueadas: form.datasBloqueadas.map(d => ({
                id: d.id, data: d.data, descricao: d.descricao,
            })),
        })
        msgOk.value = "Configuração de funcionamento atualizada."
        emit("atualizado")
    } catch (e: any) {
        erroSalvar.value = e?.response?.data?.mensagem ?? "Erro ao salvar funcionamento."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <div class="funcionamento">
        <p v-if="!podeEditar" class="aviso-leitura">
            Apenas o dono pode alterar o funcionamento. Você está visualizando em modo leitura.
        </p>

        <!-- ── Horário de funcionamento ── -->
        <AppCard padding="md">
            <h3 class="ds-card-title">Horário de funcionamento</h3>
            <p class="secao-sub">Define o intervalo disponível para agendamento.</p>

            <div class="grade-2">
                <AppField label="Início">
                    <AppInput
                        v-model="form.horarioInicio"
                        type="time"
                        :disabled="!podeEditar"
                    />
                </AppField>
                <AppField
                    label="Término"
                    :erro="erroHorario ?? undefined"
                >
                    <AppInput
                        v-model="form.horarioFim"
                        type="time"
                        :disabled="!podeEditar"
                    />
                </AppField>
            </div>
        </AppCard>

        <!-- ── Duração e intervalo das consultas ── -->
        <AppCard padding="md">
            <h3 class="ds-card-title">Duração e intervalo das consultas</h3>
            <p class="secao-sub">
                A duração padrão define o tamanho de cada slot da agenda.
                O intervalo é o tempo livre obrigatório entre o fim de uma consulta e o início da próxima.
            </p>

            <div class="grade-2">
                <AppField
                    label="Duração padrão (minutos)"
                    :erro="erroDuracao ?? undefined"
                >
                    <AppInput
                        v-model.number="form.duracaoConsultaMin"
                        type="number"
                        min="5"
                        max="480"
                        step="5"
                        :disabled="!podeEditar"
                    />
                </AppField>
                <AppField
                    label="Intervalo entre consultas (minutos)"
                    :erro="erroIntervalo ?? undefined"
                >
                    <AppInput
                        v-model.number="form.intervaloEntreConsultasMin"
                        type="number"
                        min="0"
                        max="240"
                        step="5"
                        :disabled="!podeEditar"
                    />
                </AppField>
            </div>
        </AppCard>

        <!-- ── Dias de funcionamento ── -->
        <AppCard padding="md">
            <h3 class="ds-card-title">Dias de funcionamento</h3>
            <p class="secao-sub">
                Selecione os dias em que o estabelecimento atende.
                Dias não selecionados não terão horários disponíveis.
            </p>

            <div class="dias-row">
                <button
                    v-for="d in DIAS_SEMANA" :key="d.id"
                    type="button"
                    class="dia-btn"
                    :class="{ ativo: form.diasSemana.includes(d.id) }"
                    :disabled="!podeEditar"
                    @click="toggleDiaSemana(d.id)"
                >{{ d.label }}</button>
            </div>

            <p v-if="form.diasSemana.length === 0" class="alerta-aviso">
                Nenhum dia selecionado — nenhum horário ficará disponível para agendamento.
            </p>
        </AppCard>

        <!-- ── Horários bloqueados ── -->
        <AppCard padding="md">
            <h3 class="ds-card-title">Horários bloqueados</h3>
            <p class="secao-sub">
                Intervalos do dia que ficam indisponíveis (ex: almoço, reuniões fixas).
            </p>

            <div v-if="form.horariosBloqueados.length > 0" class="lista-bloqueios">
                <div
                    v-for="b in form.horariosBloqueados" :key="b.id"
                    class="bloqueio-item"
                >
                    <span class="bloqueio-icon" aria-hidden="true">⏰</span>
                    <div class="bloqueio-info">
                        <span class="bloqueio-titulo">{{ b.inicio }} – {{ b.fim }}</span>
                        <span v-if="b.descricao" class="bloqueio-desc">{{ b.descricao }}</span>
                    </div>
                    <button
                        v-if="podeEditar"
                        type="button"
                        class="btn-remover"
                        title="Remover bloqueio"
                        @click="removerHorarioBloqueado(b.id)"
                    >
                        <i class="fa-solid fa-trash" aria-hidden="true"></i>
                    </button>
                </div>
            </div>

            <p v-else class="vazio">Nenhum horário bloqueado cadastrado.</p>

            <div v-if="podeEditar" class="form-novo">
                <p class="form-novo-titulo">Adicionar bloqueio</p>
                <div class="grade-3">
                    <AppField label="Início">
                        <AppInput v-model="novoHorario.inicio" type="time" />
                    </AppField>
                    <AppField label="Término">
                        <AppInput v-model="novoHorario.fim" type="time" />
                    </AppField>
                    <AppField label="Descrição (opcional)">
                        <AppInput
                            v-model="novoHorario.descricao"
                            type="text"
                            placeholder="ex: Horário de almoço"
                        />
                    </AppField>
                </div>
                <p v-if="erroNovoHorario" class="msg-erro">{{ erroNovoHorario }}</p>
                <div class="acoes-direita">
                    <AppButton
                        variant="secondary"
                        icon="fa-solid fa-plus"
                        @click="adicionarHorarioBloqueado"
                    >Adicionar bloqueio</AppButton>
                </div>
            </div>
        </AppCard>

        <!-- ── Datas bloqueadas ── -->
        <AppCard padding="md">
            <h3 class="ds-card-title">Datas bloqueadas</h3>
            <p class="secao-sub">
                Datas específicas em que o estabelecimento não funciona (feriados, recessos).
            </p>

            <div v-if="form.datasBloqueadas.length > 0" class="lista-bloqueios">
                <div
                    v-for="d in form.datasBloqueadas" :key="d.id"
                    class="bloqueio-item"
                >
                    <span class="bloqueio-icon" aria-hidden="true">📅</span>
                    <div class="bloqueio-info">
                        <span class="bloqueio-titulo">{{ fmtData(d.data) }}</span>
                        <span v-if="d.descricao" class="bloqueio-desc">{{ d.descricao }}</span>
                    </div>
                    <button
                        v-if="podeEditar"
                        type="button"
                        class="btn-remover"
                        title="Remover data"
                        @click="removerDataBloqueada(d.id)"
                    >
                        <i class="fa-solid fa-trash" aria-hidden="true"></i>
                    </button>
                </div>
            </div>

            <p v-else class="vazio">Nenhuma data bloqueada cadastrada.</p>

            <div v-if="podeEditar" class="form-novo">
                <p class="form-novo-titulo">Adicionar data</p>
                <div class="grade-2">
                    <AppField label="Data">
                        <AppDatePicker v-model="novaData.data" placeholder="DD/MM/AAAA" />
                    </AppField>
                    <AppField label="Motivo (opcional)">
                        <AppInput
                            v-model="novaData.descricao"
                            type="text"
                            placeholder="ex: Natal, recesso..."
                        />
                    </AppField>
                </div>
                <p v-if="erroNovaData" class="msg-erro">{{ erroNovaData }}</p>
                <div class="acoes-direita">
                    <AppButton
                        variant="secondary"
                        icon="fa-solid fa-plus"
                        @click="adicionarDataBloqueada"
                    >Adicionar data</AppButton>
                </div>
            </div>
        </AppCard>

        <!-- ── Salvar ── -->
        <div class="rodape">
            <p v-if="erroSalvar" class="msg-erro">{{ erroSalvar }}</p>
            <p v-if="msgOk" class="msg-ok">{{ msgOk }}</p>
            <AppButton
                v-if="podeEditar"
                :disabled="salvando || !podeSalvar"
                @click="salvar"
            >{{ salvando ? "Salvando..." : "Salvar configurações" }}</AppButton>
        </div>
    </div>
</template>

<style scoped>
.funcionamento { display: flex; flex-direction: column; gap: 1rem; }

.aviso-leitura {
    background: #fef3c7; color: #92400e;
    padding: 0.65rem 0.9rem; border-radius: var(--radius);
    font-size: 0.82em; margin: 0;
}

.secao-sub    { font-size: 0.82em; color: var(--text-muted); margin: 0 0 1rem; }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.grade-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 0.75rem; }

@media (max-width: 640px) {
    .grade-2, .grade-3 { grid-template-columns: 1fr; }
}

/* Dias da semana */
.dias-row { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.dia-btn {
    width: 48px; height: 48px;
    border-radius: 0.5rem;
    border: 1px solid hsl(var(--secondary) / 0.2);
    background: hsl(var(--secondary) / 0.05);
    color: hsl(var(--secondary) / 0.6);
    font-family: inherit; font-size: 0.78em; font-weight: 700;
    cursor: pointer;
    transition: all 0.12s;
}
.dia-btn:hover:not(:disabled):not(.ativo) {
    border-color: hsl(var(--secondary) / 0.4);
    color: hsl(var(--secondary));
}
.dia-btn.ativo {
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    border-color: hsl(var(--primary));
    box-shadow: 0 1px 2px rgba(0,0,0,0.08);
}
.dia-btn:disabled { opacity: 0.6; cursor: not-allowed; }

.alerta-aviso {
    margin: 0.85rem 0 0;
    color: #b45309;
    font-size: 0.78em;
}

/* Lista de bloqueios */
.lista-bloqueios { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 1rem; }

.bloqueio-item {
    display: flex; align-items: center; gap: 0.75rem;
    padding: 0.65rem 0.85rem;
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    background: hsl(var(--secondary) / 0.03);
}
.bloqueio-icon {
    flex-shrink: 0;
    width: 32px; height: 32px;
    display: flex; align-items: center; justify-content: center;
    background: hsl(var(--primary) / 0.08);
    border-radius: 0.5rem;
    font-size: 0.95rem;
}
.bloqueio-info { flex: 1; min-width: 0; display: flex; align-items: center; gap: 0.6rem; flex-wrap: wrap; }
.bloqueio-titulo { font-size: 0.85em; font-weight: 700; color: var(--text); }
.bloqueio-desc   { font-size: 0.8em; color: var(--text-muted); }

.btn-remover {
    border: none; background: none; cursor: pointer;
    color: hsl(var(--secondary) / 0.45);
    font-size: 0.85rem;
    padding: 0.35rem 0.5rem;
    border-radius: 0.35rem;
    transition: color 0.12s, background 0.12s;
}
.btn-remover:hover {
    color: var(--danger);
    background: hsl(0 90% 60% / 0.08);
}

.vazio {
    margin: 0 0 1rem;
    color: var(--text-muted);
    font-style: italic; font-size: 0.82em;
}

/* Form de adicionar */
.form-novo {
    border: 1px dashed hsl(var(--secondary) / 0.25);
    border-radius: 0.5rem;
    padding: 1rem;
}
.form-novo-titulo {
    font-size: 0.78em; font-weight: 700; color: var(--text-muted);
    margin: 0 0 0.65rem;
}
.acoes-direita {
    display: flex; justify-content: flex-end;
    margin-top: 0.65rem;
}

/* Rodapé */
.rodape {
    display: flex; align-items: center; justify-content: flex-end;
    gap: 1rem; flex-wrap: wrap;
}

.msg-erro { color: var(--danger); font-size: 0.82em; margin: 0; }
.msg-ok   { color: #15803d;       font-size: 0.82em; margin: 0; }
</style>
