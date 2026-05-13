<!--
    Aba "Consultas anteriores" — visual timeline do design Imedto care:
      - Filtro por ano (chips no topo).
      - Linha do tempo vertical com cards (httf-card) por evolução: data
        destacada, modelo, profissional, resumo, badge "Em andamento" se for
        a evolução mais recente.
      - Card de Anexos (lista + upload) abaixo.

    Mantém todas as ações que existiam (gerar PDF, upload, download).
-->
<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { AppButton, AppEmptyState } from "@/components/ui"
import type { Evolucao, Anexo } from "@/services/prontuarioService"

const props = defineProps<{
    evolucoes: Evolucao[]
    anexos: Anexo[]
    uploadando: boolean
    gerarPdf: () => void
}>()

const emit = defineEmits<{
    downloadAnexo: [anexo: Anexo]
    selecionarArquivo: [event: Event]
    enviarAnexo: []
}>()

// ─── Upload local ────────────────────────────────────────────────────────────
const inputFileRef = ref<HTMLInputElement | null>(null)
const nomeArquivoSelecionado = ref<string | null>(null)

function abrirSeletorArquivo() { inputFileRef.value?.click() }
function onArquivoSelecionado(event: Event) {
    const input = event.target as HTMLInputElement
    nomeArquivoSelecionado.value = input.files?.[0]?.name ?? null
    emit("selecionarArquivo", event)
}
function onEnviarAnexo() {
    emit("enviarAnexo")
    nomeArquivoSelecionado.value = null
    if (inputFileRef.value) inputFileRef.value.value = ""
}

// ─── Filtro por ano (chips) ─────────────────────────────────────────────────
const anos = computed(() => {
    const set = new Set<number>()
    props.evolucoes.forEach(e => {
        const d = new Date(e.criadaEm)
        if (Number.isFinite(d.getTime())) set.add(d.getFullYear())
    })
    return Array.from(set).sort((a, b) => b - a)
})
const anoSelecionado = ref<number | null>(null)
watch(anos, vals => {
    if (!vals.length) { anoSelecionado.value = null; return }
    if (!anoSelecionado.value || !vals.includes(anoSelecionado.value)) {
        anoSelecionado.value = vals[0]
    }
}, { immediate: true })

// ─── Filtragem + ordenação cronológica decrescente ──────────────────────────
const evolucoesFiltradas = computed(() => {
    const lista = [...props.evolucoes].sort((a, b) =>
        new Date(b.criadaEm).getTime() - new Date(a.criadaEm).getTime(),
    )
    if (!anoSelecionado.value) return lista
    return lista.filter(e => {
        const d = new Date(e.criadaEm)
        return Number.isFinite(d.getTime()) && d.getFullYear() === anoSelecionado.value
    })
})

const idMaisRecente = computed(() => evolucoesFiltradas.value[0]?.id ?? null)

// ─── Helpers de formatação ───────────────────────────────────────────────────
const MESES = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"]
function fmtData(iso: string) { return new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" }) }
function fmtHora(iso: string) { return new Date(iso).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" }) }
function dia(iso: string)  { return String(new Date(iso).getDate()).padStart(2, "0") }
function mes(iso: string)  { return MESES[new Date(iso).getMonth()] }
function ano(iso: string)  { return String(new Date(iso).getFullYear()) }

function fmtTamanho(bytes: number) {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function resumo(e: Evolucao): string {
    // Pega o primeiro campo de texto não vazio (queixa principal, evolução, etc.)
    for (const s of e.modeloSnapshot) {
        const v = e.conteudo[s.chave]
        if (typeof v === "string" && v.trim()) {
            const t = v.trim().replace(/\s+/g, " ")
            return t.length > 220 ? t.slice(0, 220) + "..." : t
        }
    }
    return "Sem resumo textual disponível."
}

function contarSecoesPreenchidas(e: Evolucao): number {
    return e.modeloSnapshot.filter(s => {
        const v = e.conteudo[s.chave]
        if (v === null || v === undefined) return false
        if (typeof v === "string") return v.trim().length > 0
        if (typeof v === "object" && !Array.isArray(v)) return Object.values(v as Record<string, unknown>).some(x => x !== null && x !== undefined && String(x).trim() !== "")
        if (Array.isArray(v)) return (v as unknown[]).length > 0
        return true
    }).length
}
</script>

<template>
    <div class="anteriores-wrap">
        <!-- Header com filtros + ações -->
        <div class="ht-head">
            <div>
                <h2 class="ht-titulo">Linha do tempo de atendimentos</h2>
                <p class="ht-sub">
                    {{ evolucoes.length }} {{ evolucoes.length === 1 ? "evolução registrada" : "evoluções registradas" }}
                </p>
            </div>
            <div class="ht-actions">
                <AppButton
                    v-if="evolucoes.length > 0"
                    variant="secondary"
                    icon="fa-solid fa-file-pdf"
                    @click="gerarPdf"
                >
                    Exportar histórico
                </AppButton>
            </div>
        </div>

        <div v-if="anos.length > 1" class="ht-filtros">
            <button
                type="button"
                class="fchip"
                :class="{ active: anoSelecionado === null }"
                @click="anoSelecionado = null"
            >
                Todos os anos
            </button>
            <button
                v-for="a in anos"
                :key="a"
                type="button"
                class="fchip"
                :class="{ active: anoSelecionado === a }"
                @click="anoSelecionado = a"
            >
                {{ a }}
            </button>
        </div>

        <!-- Empty -->
        <AppEmptyState
            v-if="evolucoes.length === 0"
            mensagem="Nenhuma evolução registrada ainda."
        />

        <!-- Timeline -->
        <div v-else class="ht-timeline-full" role="list">
            <article
                v-for="evo in evolucoesFiltradas"
                :key="evo.id"
                class="httf-item"
                :class="{ current: evo.id === idMaisRecente }"
                role="listitem"
            >
                <div class="httf-dot" aria-hidden="true"></div>
                <div class="httf-card">
                    <div class="httf-top">
                        <div class="httf-date-block">
                            <div class="httf-day">{{ dia(evo.criadaEm) }}</div>
                            <div class="httf-monthyr">
                                {{ mes(evo.criadaEm) }}<span>{{ ano(evo.criadaEm) }}</span>
                            </div>
                        </div>

                        <div class="httf-info">
                            <div class="httf-tpl-row">
                                <span class="httf-tpl">
                                    <i class="fa-solid fa-file-medical"></i>
                                    {{ evo.modeloNome || "Evolução" }}
                                </span>
                                <span class="httf-time">{{ fmtHora(evo.criadaEm) }}</span>
                                <span v-if="evo.id === idMaisRecente" class="httf-now">Mais recente</span>
                            </div>
                            <div class="httf-prof">
                                <i class="fa-solid fa-user-doctor"></i>
                                {{ evo.autorNome || "—" }}
                            </div>
                            <p class="httf-sum">{{ resumo(evo) }}</p>
                            <div class="httf-meta">
                                <span class="httf-meta-pill">
                                    <i class="fa-solid fa-list-check"></i>
                                    {{ contarSecoesPreenchidas(evo) }}/{{ evo.modeloSnapshot.length }} seções
                                </span>
                                <span class="httf-meta-pill">{{ fmtData(evo.criadaEm) }}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </article>
        </div>

        <!-- Card de Anexos -->
        <section class="anexos-card">
            <header class="anexos-head">
                <i class="fa-solid fa-paperclip"></i>
                <h3>Anexos do paciente</h3>
                <span class="anexos-count">{{ anexos.length }}</span>
            </header>

            <ul v-if="anexos.length > 0" class="anexos-lista">
                <li v-for="a in anexos" :key="a.id" class="anexo-item">
                    <button type="button" class="anexo-btn" @click="emit('downloadAnexo', a)">
                        <i class="fa-solid fa-file-arrow-down"></i>
                        <span>{{ a.nomeOriginal }}</span>
                    </button>
                    <span class="anexo-meta">{{ fmtTamanho(a.tamanhoBytes) }} · {{ fmtData(a.criadoEm) }}</span>
                </li>
            </ul>
            <p v-else class="anexo-vazio">Nenhum anexo enviado.</p>

            <input
                ref="inputFileRef"
                type="file"
                class="input-file-hidden"
                @change="onArquivoSelecionado"
            />
            <div class="upload-row">
                <button type="button" class="upload-area" @click="abrirSeletorArquivo">
                    <i class="fa-solid fa-cloud-arrow-up"></i>
                    <span>{{ nomeArquivoSelecionado ?? "Escolher arquivo para enviar..." }}</span>
                </button>
                <AppButton
                    size="sm"
                    :loading="uploadando"
                    :disabled="uploadando || !nomeArquivoSelecionado"
                    @click="onEnviarAnexo"
                >
                    {{ uploadando ? "Enviando..." : "Enviar" }}
                </AppButton>
            </div>
        </section>
    </div>
</template>

<style scoped>
.anteriores-wrap { display: flex; flex-direction: column; gap: 18px; }

/* ──── Header ──── */
.ht-head {
    display: flex; align-items: center; justify-content: space-between;
    gap: 16px; flex-wrap: wrap;
}
.ht-titulo {
    margin: 0 0 4px;
    font-size: 22px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.ht-sub {
    margin: 0;
    font-size: 13px; color: hsl(var(--secondary) / 0.65);
}
.ht-actions { display: flex; gap: 8px; }

/* ──── Filtro por ano (chips) ──── */
.ht-filtros { display: flex; gap: 6px; flex-wrap: wrap; }
.fchip {
    background: white; border: 1px solid hsl(var(--secondary) / 0.12);
    height: 30px; padding: 0 12px; border-radius: 999px;
    font: inherit; font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.8);
    cursor: pointer; display: inline-flex; align-items: center;
    transition: all 150ms;
}
.fchip:hover { color: hsl(var(--primary)); border-color: hsl(var(--primary) / 0.4); }
.fchip.active { background: hsl(var(--primary)); color: white; border-color: hsl(var(--primary)); }

/* ──── Timeline ──── */
.ht-timeline-full {
    display: flex; flex-direction: column; gap: 16px;
    position: relative; padding-left: 50px;
}
.ht-timeline-full::before {
    content: ""; position: absolute; left: 21px; top: 8px; bottom: 8px;
    width: 2px;
    background: linear-gradient(to bottom,
        hsl(var(--primary) / 0.3),
        hsl(var(--secondary) / 0.08));
}

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

/* ──── Card de anexos ──── */
.anexos-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    padding: 16px 18px;
}
.anexos-head {
    display: flex; align-items: center; gap: 10px;
    margin-bottom: 12px;
}
.anexos-head > i {
    width: 32px; height: 32px; border-radius: 8px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
}
.anexos-head h3 {
    margin: 0; flex: 1;
    font-size: 15px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.anexos-count {
    background: hsl(var(--secondary) / 0.06);
    color: hsl(var(--secondary) / 0.7);
    font-size: 11px; font-weight: 700;
    padding: 2px 8px; border-radius: 99px;
}
.anexos-lista { list-style: none; margin: 0 0 8px; padding: 0; display: flex; flex-direction: column; }
.anexo-item {
    display: flex; justify-content: space-between; align-items: center;
    gap: 10px; padding: 8px 0;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.anexo-item:last-child { border-bottom: 0; }
.anexo-btn {
    display: inline-flex; align-items: center; gap: 8px;
    background: none; border: 0; cursor: pointer;
    font: inherit; font-size: 13px;
    color: hsl(var(--primary)); font-weight: 600;
    padding: 4px 0;
}
.anexo-btn:hover { text-decoration: underline; }
.anexo-meta { font-size: 11px; color: hsl(var(--secondary) / 0.55); white-space: nowrap; }
.anexo-vazio { font-size: 13px; color: hsl(var(--secondary) / 0.55); margin: 6px 0 12px; }

.input-file-hidden { display: none; }
.upload-row {
    display: flex; gap: 8px; align-items: center; flex-wrap: wrap;
    margin-top: 10px; padding-top: 12px;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
}
.upload-area {
    flex: 1; min-width: 0;
    display: inline-flex; align-items: center; gap: 8px;
    padding: 8px 12px;
    background: hsl(var(--primary) / 0.04);
    border: 1px dashed hsl(var(--primary) / 0.3);
    border-radius: var(--radius-md);
    cursor: pointer; font: inherit; font-size: 13px;
    color: hsl(var(--secondary) / 0.7);
    transition: border-color 150ms, background 150ms;
}
.upload-area:hover { border-color: hsl(var(--primary) / 0.5); background: hsl(var(--primary) / 0.08); }
.upload-area i { color: hsl(var(--primary)); }
.upload-area span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
