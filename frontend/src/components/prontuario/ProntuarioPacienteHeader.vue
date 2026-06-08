<!--
    Header sticky do prontuário (visual do design Imedto care):
      - Esquerda: voltar + avatar + nome/idade/sexo/CPF/contato + alertas
      - Direita: timer (quando há atendimento ativo) + ação finalizar
    Não toca o backend — `finalizar` apenas emite evento (a view chama
    `agendaService.concluir`).
-->
<script setup lang="ts">
import { computed } from "vue"
import type { Paciente } from "@/services/pacienteService"
import type { Agendamento } from "@/services/agendaService"
import { useAtendimentoAtivo } from "@/composables/useAtendimentoAtivo"
import { useClockTick, formatarDuracao } from "@/composables/useClockTick"

const props = defineProps<{
    paciente: Paciente | null
    agendamento?: Agendamento | null
    estabelecimento?: string | null
    /** Permite ocultar a coluna de ações (em telas read-only / sem agendamento) */
    semAcoes?: boolean
}>()

const emit = defineEmits<{
    "voltar": []
    "finalizar": []
}>()

const { atual: atendimentoAtivo, ehEstePaciente } = useAtendimentoAtivo()
const { agora } = useClockTick()

const ativoAqui = computed(() =>
    !!props.paciente && ehEstePaciente(props.paciente.id),
)

const decorridoLabel = computed(() => {
    if (!ativoAqui.value || !atendimentoAtivo.value) return null
    const ms = agora.value - new Date(atendimentoAtivo.value.iniciadoEm).getTime()
    return formatarDuracao(ms)
})

const iniciais = computed(() => {
    const nome = props.paciente?.nomeCompleto?.trim() ?? ""
    if (!nome) return "?"
    const partes = nome.split(/\s+/).filter(Boolean)
    if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
})

function calcularIdade(iso: string | null | undefined) {
    if (!iso) return null
    const nasc = new Date(iso)
    const hoje = new Date()
    let idade = hoje.getFullYear() - nasc.getFullYear()
    const m = hoje.getMonth() - nasc.getMonth()
    if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) idade--
    return idade
}
const idade = computed(() => calcularIdade(props.paciente?.dataNascimento))

const meta = computed(() => {
    const p = props.paciente
    if (!p) return [] as string[]
    const linhas: string[] = []
    if (idade.value !== null) linhas.push(`${idade.value} anos`)
    if (p.genero) linhas.push(p.genero)
    if (p.cpf) linhas.push(`CPF ${p.cpf}`)
    if (p.telefone) linhas.push(p.telefone)
    return linhas
})

// Alertas do paciente: tudo via campo livre `alertas: string[]`. O design
// distingue "alergia" (vermelho) de outros alertas (amarelo). Heurística:
// se o texto começa com "alergia" → vermelho.
type Alerta = { texto: string, tipo: "err" | "warn" }
const alertas = computed<Alerta[]>(() => {
    return (props.paciente?.alertas ?? []).map(t => ({
        texto: t,
        tipo: t.toLowerCase().startsWith("alergia") ? "err" : "warn",
    }))
})

const dataHora = computed(() => {
    const iso = props.agendamento?.inicioPrevisto
    if (!iso) return null
    return new Date(iso).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
})

void emit
</script>

<template>
    <header class="pront-header">
        <div class="ph-left">
            <button type="button" class="ph-back" aria-label="Voltar" @click="emit('voltar')">
                <i class="fa-solid fa-arrow-left"></i>
            </button>

            <div class="ph-avatar" :title="paciente?.nomeCompleto ?? ''">{{ iniciais }}</div>

            <div class="ph-info">
                <h1>{{ paciente?.nomeCompleto ?? "—" }}</h1>
                <div class="ph-meta">
                    <span v-for="(t, i) in meta" :key="i">
                        {{ t }}
                        <span v-if="i < meta.length - 1" class="ph-meta-sep">·</span>
                    </span>
                    <span v-if="estabelecimento && meta.length > 0" class="ph-meta-sep">·</span>
                    <span v-if="estabelecimento">{{ estabelecimento }}</span>
                </div>

                <div v-if="alertas.length > 0" class="ph-alerts">
                    <span
                        v-for="(a, i) in alertas"
                        :key="i"
                        class="ph-alert"
                        :class="a.tipo"
                    >
                        <i class="fa-solid" :class="a.tipo === 'err' ? 'fa-ban' : 'fa-circle-info'"></i>
                        {{ a.texto }}
                    </span>
                </div>

                <div v-if="dataHora && agendamento" class="ph-encounter">
                    <i class="fa-solid fa-calendar-check"></i>
                    {{ dataHora }} · {{ agendamento.tipoServico || "Consulta" }}
                    <span v-if="agendamento.profissionalNome">· {{ agendamento.profissionalNome }}</span>
                </div>
            </div>
        </div>

        <div v-if="!semAcoes" class="ph-right">
            <div v-if="decorridoLabel" class="ph-timer" aria-label="Tempo de atendimento">
                <i class="fa-solid fa-stopwatch"></i>
                <div class="ph-timer-info">
                    <span class="ph-timer-val">{{ decorridoLabel }}</span>
                    <span class="ph-timer-lbl">em atendimento</span>
                </div>
            </div>

            <button
                v-if="ativoAqui"
                type="button"
                class="ph-btn ph-btn-success"
                @click="emit('finalizar')"
            >
                <i class="fa-solid fa-circle-check"></i>
                Finalizar e assinar
            </button>
        </div>
    </header>
</template>

<style scoped>
.pront-header {
    position: sticky;
    top: var(--topbar-h, var(--top-h));
    z-index: 20;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    padding: 14px 22px;
    margin-bottom: 16px;
    display: flex; align-items: center; justify-content: space-between;
    gap: 20px; flex-wrap: wrap;
    box-shadow: var(--shadow-sm);
}
.ph-left { display: flex; align-items: center; gap: 14px; min-width: 0; flex: 1 1 380px; }
.ph-back {
    width: 36px; height: 36px; border-radius: 50%;
    background: hsl(var(--secondary) / 0.06);
    color: hsl(var(--primary-dark));
    display: inline-flex; align-items: center; justify-content: center;
    border: 0; cursor: pointer; flex-shrink: 0;
    transition: background 150ms;
}
.ph-back:hover { background: hsl(var(--primary) / 0.12); color: hsl(var(--primary)); }

.ph-avatar {
    width: 56px; height: 56px; border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary)), hsl(var(--primary-dark)));
    color: white; font-weight: 700; font-size: 18px;
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0;
}
.ph-info { min-width: 0; flex: 1; }
.ph-info h1 {
    font-size: var(--text-2xl); font-weight: var(--font-weight-bold);
    color: hsl(var(--primary-dark));
    margin: 0 0 2px;
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.ph-meta {
    display: flex; flex-wrap: wrap; gap: 6px;
    font-size: 12px; color: hsl(var(--secondary) / 0.7);
}
.ph-meta-sep { opacity: 0.4; margin: 0 2px; }
.ph-alerts { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 6px; }
.ph-encounter {
    margin-top: 6px;
    font-size: 12px; color: hsl(var(--primary));
    display: inline-flex; align-items: center; gap: 6px;
}
.ph-encounter i { font-size: 11px; }

/* ──── Direita: timer + ações ──── */
.ph-right { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }

.ph-timer {
    display: flex; align-items: center; gap: 8px;
    background: hsl(155 60% 50% / 0.1); color: hsl(155 60% 25%);
    padding: 6px 14px; border-radius: var(--radius-md);
}
.ph-timer i { font-size: 18px; }
.ph-timer-info { display: flex; flex-direction: column; line-height: 1; }
.ph-timer-val { font-family: var(--font-mono); font-size: 18px; font-weight: 700; }
.ph-timer-lbl { font-size: 10px; text-transform: uppercase; letter-spacing: 0.05em; opacity: 0.8; margin-top: 2px; }

.ph-btn {
    display: inline-flex; align-items: center; gap: 6px;
    height: 36px; padding: 0 12px;
    border-radius: var(--radius-md);
    font: inherit; font-size: 13px; font-weight: 600;
    cursor: pointer; border: 1px solid transparent;
    transition: background 150ms, border-color 150ms, color 150ms, box-shadow 150ms;
    white-space: nowrap;
}
.ph-btn i { font-size: 12px; }
.ph-btn-secondary {
    background: white; color: hsl(var(--secondary) / 0.85);
    border-color: hsl(var(--secondary) / 0.18);
}
.ph-btn-secondary:hover { color: hsl(var(--primary)); border-color: hsl(var(--primary) / 0.4); }
.ph-btn-success {
    background: hsl(155 60% 50%); color: white; border-color: hsl(155 60% 50%);
}
.ph-btn-success:hover { background: hsl(155 60% 42%); border-color: hsl(155 60% 42%); }

@media (max-width: 760px) {
    .pront-header { padding: 12px 14px; }
    .ph-info h1 { font-size: var(--text-md); }
    .ph-btn { padding: 0 10px; }
    .ph-btn span { display: none; }
}
</style>
