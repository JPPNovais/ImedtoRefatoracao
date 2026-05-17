<!--
    Card compacto de uma evolução, usado pela linha do tempo:
      - ConsultasAnterioresTab (dentro do prontuário)
      - PacienteDetalheView (aba Prontuário)

    Mostra apenas os dados principais (data, modelo, profissional, resumo curto,
    quantas seções foram preenchidas) — sem expandir o conteúdo inteiro de cada
    seção. Dois botões de PDF (visualizar ou baixar) emitem o mesmo evento
    `gerar-pdf` com o modo escolhido, e o pai cuida da geração e do audit LGPD.
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton } from "@/components/ui"
import type { Evolucao } from "@/services/prontuarioService"
import type { PdfSaidaModo } from "@/composables/useProntuarioPdf"
import { resumoTextual, contarSecoesPreenchidas } from "@/composables/useEvolucaoResumo"

const props = defineProps<{
    evolucao: Evolucao
    destaque?: boolean
    gerandoPdf?: boolean
}>()

const emit = defineEmits<{
    "gerar-pdf": [payload: { evolucao: Evolucao, modo: PdfSaidaModo }]
}>()

const MESES = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"]
function fmtData(iso: string) { return new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" }) }
function fmtHora(iso: string) { return new Date(iso).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" }) }

const dia    = computed(() => String(new Date(props.evolucao.criadaEm).getDate()).padStart(2, "0"))
const mes    = computed(() => MESES[new Date(props.evolucao.criadaEm).getMonth()])
const ano    = computed(() => String(new Date(props.evolucao.criadaEm).getFullYear()))
const hora   = computed(() => fmtHora(props.evolucao.criadaEm))
const data   = computed(() => fmtData(props.evolucao.criadaEm))
const resumo = computed(() => resumoTextual(props.evolucao))
const secoes = computed(() => contarSecoesPreenchidas(props.evolucao))

function emitirPdf(modo: PdfSaidaModo) {
    if (props.gerandoPdf) return
    emit("gerar-pdf", { evolucao: props.evolucao, modo })
}
</script>

<template>
    <article class="httf-item" :class="{ current: destaque }" role="listitem">
        <div class="httf-dot" aria-hidden="true"></div>
        <div class="httf-card">
            <div class="httf-top">
                <div class="httf-date-block">
                    <div class="httf-day">{{ dia }}</div>
                    <div class="httf-monthyr">
                        {{ mes }}<span>{{ ano }}</span>
                    </div>
                </div>

                <div class="httf-info">
                    <div class="httf-tpl-row">
                        <span class="httf-tpl">
                            <i class="fa-solid fa-file-medical"></i>
                            {{ evolucao.modeloNome || "Evolução" }}
                        </span>
                        <span class="httf-time">{{ hora }}</span>
                        <span v-if="destaque" class="httf-now">Mais recente</span>
                    </div>
                    <div class="httf-prof">
                        <i class="fa-solid fa-user-doctor"></i>
                        {{ evolucao.autorNome || "—" }}
                    </div>
                    <p class="httf-sum">{{ resumo }}</p>
                    <div class="httf-meta">
                        <span class="httf-meta-pill">
                            <i class="fa-solid fa-list-check"></i>
                            {{ secoes.preenchidas }}/{{ secoes.total }} seções
                        </span>
                        <span class="httf-meta-pill">{{ data }}</span>
                    </div>
                </div>

                <div class="httf-acoes acoes-pdf">
                    <AppButton
                        variant="secondary"
                        size="sm"
                        icon="fa-solid fa-eye"
                        :loading="gerandoPdf"
                        :disabled="gerandoPdf"
                        aria-label="Visualizar PDF desta evolução"
                        data-test="btn-pdf-visualizar"
                        @click="emitirPdf('visualizar')"
                    >
                        Ver PDF
                    </AppButton>
                    <AppButton
                        variant="ghost"
                        size="sm"
                        icon="fa-solid fa-download"
                        :loading="gerandoPdf"
                        :disabled="gerandoPdf"
                        aria-label="Baixar PDF desta evolução"
                        data-test="btn-pdf-baixar"
                        @click="emitirPdf('download')"
                    />
                </div>
            </div>
        </div>
    </article>
</template>

<style scoped>
.httf-item { position: relative; }
.httf-dot {
    position: absolute; left: -36px; top: 26px;
    width: 14px; height: 14px; border-radius: 50%;
    background: white;
    border: 3px solid hsl(var(--secondary) / 0.3);
    box-shadow: 0 0 0 4px white;
}
.httf-item.current .httf-dot {
    background: hsl(155 60% 50%);
    border-color: hsl(155 60% 50%);
    box-shadow: 0 0 0 4px white, 0 0 0 8px hsl(155 60% 50% / 0.2);
    animation: pulseDot 2s ease-in-out infinite;
}
@keyframes pulseDot { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.15); } }

.httf-card {
    background: white;
    border-radius: var(--radius-lg);
    border: 1px solid hsl(var(--secondary) / 0.08);
    padding: 16px 18px;
    transition: box-shadow 150ms, border-color 150ms;
}
.httf-card:hover {
    box-shadow: var(--shadow);
    border-color: hsl(var(--primary) / 0.2);
}
.httf-item.current .httf-card {
    border-color: hsl(155 60% 50% / 0.4);
    background: hsl(155 60% 50% / 0.03);
}

.httf-top { display: flex; gap: 16px; align-items: flex-start; }
.httf-date-block {
    width: 64px; flex-shrink: 0; text-align: center;
    padding: 8px 0;
    background: hsl(var(--primary) / 0.06);
    border-radius: var(--radius-md);
}
.httf-day { font-size: 24px; font-weight: 700; color: hsl(var(--primary-dark)); line-height: 1; }
.httf-monthyr {
    font-size: 11px; text-transform: uppercase; font-weight: 700;
    color: hsl(var(--primary)); letter-spacing: 0.06em; margin-top: 4px;
}
.httf-monthyr span {
    display: block; font-size: 10px;
    color: hsl(var(--secondary) / 0.55);
    font-weight: 600; margin-top: 1px;
}

.httf-info { flex: 1; min-width: 0; }
.httf-tpl-row {
    display: flex; align-items: center; gap: 10px;
    flex-wrap: wrap; margin-bottom: 4px;
}
.httf-tpl {
    display: inline-flex; align-items: center; gap: 6px;
    font-size: 13px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.httf-tpl i { color: hsl(var(--primary)); font-size: 12px; }
.httf-time { font-size: 12px; color: hsl(var(--secondary) / 0.6); }
.httf-now {
    background: hsl(155 60% 50%); color: white;
    font-size: 10px; padding: 2px 8px; border-radius: 99px;
    text-transform: uppercase; letter-spacing: 0.05em; font-weight: 700;
}
.httf-prof {
    font-size: 12px;
    color: hsl(var(--secondary) / 0.7);
    margin-bottom: 8px;
    display: inline-flex; align-items: center; gap: 6px;
}
.httf-prof i { font-size: 10px; opacity: 0.6; }
.httf-sum {
    margin: 0 0 8px;
    font-size: 13px;
    color: hsl(var(--secondary) / 0.9);
    line-height: 1.55;
}
.httf-meta { display: flex; gap: 6px; flex-wrap: wrap; }
.httf-meta-pill {
    display: inline-flex; align-items: center; gap: 5px;
    font-size: 11px;
    background: hsl(var(--secondary) / 0.06);
    color: hsl(var(--secondary) / 0.7);
    padding: 2px 8px; border-radius: 99px;
    font-weight: 600;
}

.httf-acoes { flex-shrink: 0; display: flex; align-items: flex-start; }
.acoes-pdf { gap: 6px; }
@media (max-width: 480px) {
    .acoes-pdf { flex-direction: column; align-items: stretch; }
}
</style>
