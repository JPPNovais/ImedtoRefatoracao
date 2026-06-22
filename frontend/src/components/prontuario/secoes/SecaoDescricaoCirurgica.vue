<!--
  Seção "Descrição cirúrgica" do prontuário — estruturada.

  Shape persistido (chave: "desc-cirurgica"):
    { cirurgiao, data, diaSemana, cirurgiasRealizadas, anestesista, auxiliar,
      instrumentador, outrosMembros[], cirurgiaInicio, cirurgiaFim,
      profilaxia{…}, intercorrencia, intercorrenciaDescricao,
      tecnicaOperatoria, observacoes }

  NÃO inclui (decisão de produto R7/CA6): tipoAnestesia, tipoAnestesiaOutro,
  anestesiaInicio, anestesiaFim.

  Cirurgião é obrigatório no UX (label com *). A trava de salvar é feita no
  ponto de submit da evolução (ProntuarioView) — não aqui.
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppInput, AppTextarea, AppButton, AppCheckbox, AppDatePicker, AppPillToggle } from "@/components/ui"

// ── Tipos ──────────────────────────────────────────────────────────────────────

export interface Profilaxia {
    enoxaparina: boolean
    meiaCompressiva: boolean
    botaPneumatica: boolean
    deambulacaoPrecoce: boolean
    antitrombOutroAtivo: boolean
    antitrombOutro: string
    cefazolina: boolean
    gentamicina: boolean
    antibioOutroAtivo: boolean
    antibioOutro: string
}

export interface OutroMembro {
    funcao: string
    nome: string
}

export interface DescCirurgica {
    cirurgiao: string
    data: string
    diaSemana: string
    cirurgiasRealizadas: string
    anestesista: string
    auxiliar: string
    instrumentador: string
    outrosMembros: OutroMembro[]
    cirurgiaInicio: string
    cirurgiaFim: string
    profilaxia: Profilaxia
    intercorrencia: "sem" | "com" | ""
    intercorrenciaDescricao: string
    tecnicaOperatoria: string
    observacoes: string
}

// ── Props / emits ──────────────────────────────────────────────────────────────

const props = defineProps<{
    modelValue: DescCirurgica
    readOnly?: boolean
    /** Ref de foco para o campo cirurgião — preenchida pelo parent ao exibir erro. */
    erroCirurgiao?: string | null
}>()

const emit = defineEmits<{ "update:modelValue": [v: DescCirurgica] }>()

// ── Constantes ─────────────────────────────────────────────────────────────────

const DIAS_SEMANA = [
    "Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira",
    "Quinta-feira", "Sexta-feira", "Sábado",
]

const OPCOES_INTERCORRENCIA = [
    { valor: "sem", label: "Sem intercorrências" },
    { valor: "com", label: "Com intercorrência"  },
]

// ── Helpers ────────────────────────────────────────────────────────────────────

function calcularDiaSemana(data: string): string {
    if (!data) return ""
    const d = new Date(data + "T12:00:00")
    if (isNaN(d.getTime())) return ""
    return DIAS_SEMANA[d.getDay()]
}

function calcularDuracao(inicio: string, fim: string): string {
    if (!inicio || !fim) return "--:--"
    const [hI, mI] = inicio.split(":").map(Number)
    const [hF, mF] = fim.split(":").map(Number)
    if ([hI, mI, hF, mF].some(isNaN)) return "--:--"
    let total = (hF * 60 + mF) - (hI * 60 + mI)
    if (total < 0) total += 24 * 60
    const h = Math.floor(total / 60)
    const m = total % 60
    return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`
}

// ── Estado derivado ─────────────────────────────────────────────────────────────

const diaSemanaCalculado = computed(() => calcularDiaSemana(props.modelValue.data))

const duracaoCirurgia = computed(() =>
    calcularDuracao(props.modelValue.cirurgiaInicio, props.modelValue.cirurgiaFim),
)

const profilaxia = computed(() => props.modelValue.profilaxia ?? {} as Profilaxia)
const outrosMembros = computed(() => props.modelValue.outrosMembros ?? [])

// ── Emissores ──────────────────────────────────────────────────────────────────

function atualizar(patch: Partial<DescCirurgica>) {
    const merged = { ...props.modelValue, ...patch }
    if (patch.data !== undefined) {
        merged.diaSemana = calcularDiaSemana(patch.data)
    }
    emit("update:modelValue", merged)
}

function atualizarProfilaxia(patch: Partial<Profilaxia>) {
    atualizar({ profilaxia: { ...profilaxia.value, ...patch } })
}

function adicionarMembro() {
    if (props.readOnly) return
    atualizar({ outrosMembros: [...outrosMembros.value, { funcao: "", nome: "" }] })
}

function removerMembro(idx: number) {
    if (props.readOnly) return
    const lista = [...outrosMembros.value]
    lista.splice(idx, 1)
    atualizar({ outrosMembros: lista })
}

function setMembro(idx: number, campo: keyof OutroMembro, valor: string) {
    const lista = outrosMembros.value.map((m, i) =>
        i === idx ? { ...m, [campo]: valor } : m,
    )
    atualizar({ outrosMembros: lista })
}
</script>

<template>
    <div class="secao-desc-cirurgica">

        <!-- ── Identificação ─────────────────────────────────────────────── -->
        <div class="painel">
            <span class="painel-titulo">Identificação</span>
            <div class="grade-3">
                <!-- Cirurgião* -->
                <div class="campo campo--full">
                    <span class="field-label">Cirurgião <em class="obrigatorio">*</em></span>
                    <div :class="{ 'campo-com-erro': !!erroCirurgiao }" id="mod-desc-cirurgica">
                        <AppInput
                            :model-value="modelValue.cirurgiao"
                            placeholder="Nome do cirurgião responsável"
                            :disabled="readOnly"
                            @update:model-value="(v) => atualizar({ cirurgiao: String(v) })"
                        />
                    </div>
                    <span v-if="erroCirurgiao" class="msg-erro" role="alert">{{ erroCirurgiao }}</span>
                </div>
                <!-- Data + dia da semana -->
                <div class="campo">
                    <span class="field-label">Data da cirurgia</span>
                    <AppDatePicker
                        :model-value="modelValue.data"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ data: String(v) })"
                    />
                </div>
                <div class="campo">
                    <span class="field-label">Dia da semana</span>
                    <AppInput
                        :model-value="diaSemanaCalculado"
                        :disabled="true"
                        placeholder="—"
                    />
                </div>
                <!-- Cirurgia(s) realizada(s) -->
                <div class="campo campo--full">
                    <span class="field-label">Cirurgia(s) realizada(s)</span>
                    <AppInput
                        :model-value="modelValue.cirurgiasRealizadas"
                        placeholder="Descreva o(s) procedimento(s) realizado(s)"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ cirurgiasRealizadas: String(v) })"
                    />
                </div>
            </div>
        </div>

        <!-- ── Equipe cirúrgica ──────────────────────────────────────────── -->
        <div class="painel">
            <span class="painel-titulo">Equipe cirúrgica</span>
            <div class="grade-3">
                <div class="campo">
                    <span class="field-label">Anestesista</span>
                    <AppInput
                        :model-value="modelValue.anestesista"
                        placeholder="Nome do anestesista"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ anestesista: String(v) })"
                    />
                </div>
                <div class="campo">
                    <span class="field-label">Auxiliar</span>
                    <AppInput
                        :model-value="modelValue.auxiliar"
                        placeholder="Nome do auxiliar"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ auxiliar: String(v) })"
                    />
                </div>
                <div class="campo">
                    <span class="field-label">Instrumentador(a)</span>
                    <AppInput
                        :model-value="modelValue.instrumentador"
                        placeholder="Nome do(a) instrumentador(a)"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ instrumentador: String(v) })"
                    />
                </div>
            </div>

            <!-- Outros membros (lista repetível) -->
            <div class="subsecao">
                <span class="field-label">Outros membros da equipe</span>
                <div
                    v-for="(m, i) in outrosMembros"
                    :key="i"
                    class="linha-membro"
                >
                    <AppInput
                        :model-value="m.funcao"
                        placeholder="Função"
                        :disabled="readOnly"
                        @update:model-value="(v) => setMembro(i, 'funcao', String(v))"
                    />
                    <AppInput
                        :model-value="m.nome"
                        placeholder="Nome"
                        :disabled="readOnly"
                        @update:model-value="(v) => setMembro(i, 'nome', String(v))"
                    />
                    <button
                        v-if="!readOnly"
                        type="button"
                        class="btn-icon btn-icon-excluir"
                        title="Remover membro"
                        @click="removerMembro(i)"
                    >
                        <i class="fa-solid fa-xmark" aria-hidden="true" />
                    </button>
                </div>
                <AppButton
                    v-if="!readOnly"
                    size="sm"
                    variant="ghost"
                    icon="fa-solid fa-plus"
                    type="button"
                    @click="adicionarMembro"
                >
                    Adicionar membro
                </AppButton>
            </div>
        </div>

        <!-- ── Duração da cirurgia ───────────────────────────────────────── -->
        <div class="painel">
            <span class="painel-titulo">Duração da cirurgia</span>
            <div class="grade-3-sm">
                <div class="campo">
                    <span class="field-label">Início</span>
                    <input
                        type="time"
                        class="input-hora"
                        :value="modelValue.cirurgiaInicio"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ cirurgiaInicio: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <span class="field-label">Fim</span>
                    <input
                        type="time"
                        class="input-hora"
                        :value="modelValue.cirurgiaFim"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ cirurgiaFim: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <span class="field-label">Duração</span>
                    <AppInput
                        :model-value="duracaoCirurgia"
                        :disabled="true"
                        placeholder="--:--"
                    />
                </div>
            </div>
        </div>

        <!-- ── Profilaxia ────────────────────────────────────────────────── -->
        <div class="painel">
            <span class="painel-titulo">Profilaxia</span>
            <div class="grade-profilaxia">
                <!-- Antitrombótica -->
                <div class="coluna-profilaxia">
                    <span class="field-label">Antitrombótica</span>
                    <AppCheckbox
                        :model-value="profilaxia.enoxaparina"
                        label="Enoxaparina 40mg SC"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizarProfilaxia({ enoxaparina: v })"
                    />
                    <AppCheckbox
                        :model-value="profilaxia.meiaCompressiva"
                        label="Meia compressiva"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizarProfilaxia({ meiaCompressiva: v })"
                    />
                    <AppCheckbox
                        :model-value="profilaxia.botaPneumatica"
                        label="Bota pneumática"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizarProfilaxia({ botaPneumatica: v })"
                    />
                    <AppCheckbox
                        :model-value="profilaxia.deambulacaoPrecoce"
                        label="Deambulação precoce"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizarProfilaxia({ deambulacaoPrecoce: v })"
                    />
                    <div class="campo-outro">
                        <AppCheckbox
                            :model-value="profilaxia.antitrombOutroAtivo"
                            label="Outro"
                            :disabled="readOnly"
                            @update:model-value="(v) => atualizarProfilaxia({ antitrombOutroAtivo: v, antitrombOutro: v ? profilaxia.antitrombOutro : '' })"
                        />
                        <AppInput
                            v-if="profilaxia.antitrombOutroAtivo"
                            :model-value="profilaxia.antitrombOutro"
                            placeholder="Especifique..."
                            :disabled="readOnly || !profilaxia.antitrombOutroAtivo"
                            @update:model-value="(v) => atualizarProfilaxia({ antitrombOutro: String(v) })"
                        />
                    </div>
                </div>

                <!-- Antibiótica -->
                <div class="coluna-profilaxia">
                    <span class="field-label">Antibiótica</span>
                    <AppCheckbox
                        :model-value="profilaxia.cefazolina"
                        label="Cefazolina"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizarProfilaxia({ cefazolina: v })"
                    />
                    <AppCheckbox
                        :model-value="profilaxia.gentamicina"
                        label="Gentamicina"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizarProfilaxia({ gentamicina: v })"
                    />
                    <div class="campo-outro">
                        <AppCheckbox
                            :model-value="profilaxia.antibioOutroAtivo"
                            label="Outro"
                            :disabled="readOnly"
                            @update:model-value="(v) => atualizarProfilaxia({ antibioOutroAtivo: v, antibioOutro: v ? profilaxia.antibioOutro : '' })"
                        />
                        <AppInput
                            v-if="profilaxia.antibioOutroAtivo"
                            :model-value="profilaxia.antibioOutro"
                            placeholder="Especifique..."
                            :disabled="readOnly || !profilaxia.antibioOutroAtivo"
                            @update:model-value="(v) => atualizarProfilaxia({ antibioOutro: String(v) })"
                        />
                    </div>
                </div>
            </div>
        </div>

        <!-- ── Intercorrências ───────────────────────────────────────────── -->
        <div class="campo">
            <span class="field-label">Intercorrências</span>
            <AppPillToggle
                :model-value="modelValue.intercorrencia"
                :opcoes="OPCOES_INTERCORRENCIA"
                @update:model-value="(v) => !readOnly && atualizar({
                    intercorrencia: v as DescCirurgica['intercorrencia'],
                    intercorrenciaDescricao: v === 'sem' ? '' : modelValue.intercorrenciaDescricao,
                })"
            />
            <AppTextarea
                v-if="modelValue.intercorrencia === 'com'"
                :model-value="modelValue.intercorrenciaDescricao"
                :rows="3"
                placeholder="Descreva as intercorrências observadas..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ intercorrenciaDescricao: String(v) })"
            />
        </div>

        <!-- ── Técnica operatória ─────────────────────────────────────────── -->
        <div class="campo">
            <span class="field-label">Técnica operatória</span>
            <AppTextarea
                :model-value="modelValue.tecnicaOperatoria"
                :rows="5"
                placeholder="Descreva a técnica operatória utilizada..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ tecnicaOperatoria: String(v) })"
            />
        </div>

        <!-- ── Observações ────────────────────────────────────────────────── -->
        <div class="campo">
            <span class="field-label">Observações</span>
            <AppTextarea
                :model-value="modelValue.observacoes"
                :rows="3"
                placeholder="Observações complementares..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacoes: String(v) })"
            />
        </div>

    </div>
</template>

<style scoped>
.secao-desc-cirurgica {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

/* ── Painéis/cards de subseção ── */
.painel {
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.875rem 1rem;
    background: var(--surface, #fff);
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.painel-titulo {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
}

/* ── Grades ── */
.grade-3 {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.75rem;
}

.grade-3-sm {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.75rem;
    max-width: 480px;
}

/* ── Campos ── */
.campo {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.campo--full {
    grid-column: 1 / -1;
}

/* ── Mensagem de erro inline ── */
.msg-erro {
    font-size: var(--text-xs);
    color: hsl(var(--destructive));
}

.obrigatorio {
    color: hsl(var(--destructive));
    font-style: normal;
}

.input-erro,
.campo-com-erro :deep(.form-input),
.campo-com-erro :deep(input) {
    border-color: hsl(var(--destructive)) !important;
    box-shadow: 0 0 0 1px hsl(var(--destructive) / 0.3) !important;
}

/* ── Outros membros ── */
.subsecao {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding-top: 0.5rem;
    border-top: 1px solid var(--border);
}

.linha-membro {
    display: grid;
    grid-template-columns: 1fr 1.5fr 32px;
    gap: 0.5rem;
    align-items: center;
}

/* ── Input de hora nativo ── */
.input-hora {
    height: 2.25rem;
    width: 100%;
    padding: 0 0.75rem;
    border: 1px solid var(--border, hsl(var(--input)));
    border-radius: var(--radius);
    background: var(--surface, hsl(var(--background)));
    font-size: var(--text-sm);
    color: var(--text, hsl(var(--foreground)));
    outline: none;
    transition: border-color 0.15s;
}

.input-hora:focus {
    border-color: hsl(var(--ring));
    box-shadow: 0 0 0 1px hsl(var(--ring) / 0.3);
}

.input-hora:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    background: var(--muted, hsl(var(--muted)));
}

/* ── Profilaxia ── */
.grade-profilaxia {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
}

.coluna-profilaxia {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.campo-outro {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    padding-left: 0.25rem;
}

/* ── Responsivo ── */
@media (max-width: 768px) {
    .grade-3,
    .grade-3-sm,
    .grade-profilaxia {
        grid-template-columns: 1fr;
    }

    .linha-membro {
        grid-template-columns: 1fr 32px;
    }

    .linha-membro :first-child {
        grid-column: 1 / -1;
    }
}
</style>
