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

// ─── Upload local ─────────────────────────────────────────────────────────────
const inputFileRef = ref<HTMLInputElement | null>(null)
const nomeArquivoSelecionado = ref<string | null>(null)

function abrirSeletorArquivo() {
    inputFileRef.value?.click()
}

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

// ─── Filtro por ano ───────────────────────────────────────────────────────────
const anos = computed(() => {
    const set = new Set<number>()
    props.evolucoes.forEach(e => {
        const d = new Date(e.criadaEm)
        if (Number.isFinite(d.getTime())) set.add(d.getFullYear())
    })
    return Array.from(set).sort((a, b) => b - a)
})

const anoSelecionado = ref<number | null>(null)

watch(anos, (vals) => {
    if (!vals.length) { anoSelecionado.value = null; return }
    if (!anoSelecionado.value || !vals.includes(anoSelecionado.value)) {
        anoSelecionado.value = vals[0]
    }
}, { immediate: true })

// ─── Agrupamento ─────────────────────────────────────────────────────────────
type Grupo = { ano: number; itens: Evolucao[] }

const grupos = computed<Grupo[]>(() => {
    if (!props.evolucoes.length) return []
    const map = new Map<number, Evolucao[]>()
    props.evolucoes.forEach(e => {
        const d = new Date(e.criadaEm)
        if (!Number.isFinite(d.getTime())) return
        const ano = d.getFullYear()
        if (anoSelecionado.value && ano !== anoSelecionado.value) return
        if (!map.has(ano)) map.set(ano, [])
        map.get(ano)!.push(e)
    })
    return Array.from(map.entries())
        .sort((a, b) => b[0] - a[0])
        .map(([ano, itens]) => ({ ano, itens }))
})

// ─── Helpers ─────────────────────────────────────────────────────────────────
function formatarData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
}

function formatarTamanho(bytes: number) {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
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
        <div class="grid-layout">
            <!-- Sidebar — filtro por ano -->
            <div class="sidebar-wrap">
                <div class="nav-card">
                    <div class="nav-header">Filtrar por ano</div>
                    <div class="nav-links">
                        <button
                            v-for="ano in anos"
                            :key="ano"
                            type="button"
                            class="nav-link"
                            :class="{ ativo: anoSelecionado === ano }"
                            @click="anoSelecionado = ano"
                        >
                            {{ ano }}
                        </button>
                        <button
                            v-if="anos.length > 1"
                            type="button"
                            class="nav-link nav-link-todos"
                            :class="{ ativo: anoSelecionado === null }"
                            @click="anoSelecionado = null"
                        >
                            Todos os anos
                        </button>
                    </div>
                </div>
            </div>

            <!-- Conteúdo principal -->
            <div class="conteudo-principal">
                <div class="historico-header">
                    <h3 class="historico-titulo">Histórico de evoluções</h3>
                    <AppButton
                        v-if="evolucoes.length > 0"
                        variant="secondary"
                        icon="fa-solid fa-file-pdf"
                        @click="gerarPdf"
                    >
                        Gerar PDF
                    </AppButton>
                </div>

                <AppEmptyState
                    v-if="evolucoes.length === 0"
                    mensagem="Nenhuma evolução registrada ainda."
                />

                <!-- Timeline compacta -->
                <div v-for="grupo in grupos" :key="grupo.ano" class="ano-grupo">
                    <p class="ano-titulo">{{ grupo.ano }}</p>

                    <div
                        v-for="evolucao in grupo.itens"
                        :key="evolucao.id"
                        class="evolucao-linha"
                    >
                        <!-- Marcador timeline -->
                        <div class="timeline-col">
                            <div class="timeline-marker">
                                <i class="fa-regular fa-calendar-check" />
                            </div>
                            <div class="timeline-line" />
                        </div>

                        <!-- Card compacto -->
                        <div class="evolucao-card">
                            <!-- Data em destaque -->
                            <span class="evol-data">{{ formatarData(evolucao.criadaEm) }}</span>

                            <div class="evol-chips">
                                <!-- Profissional -->
                                <span class="chip chip-prof">
                                    <i class="fa-solid fa-user-doctor chip-icon" />
                                    {{ evolucao.autorNome || "—" }}
                                </span>

                                <!-- Modelo usado -->
                                <span v-if="evolucao.modeloNome" class="chip chip-modelo">
                                    <i class="fa-solid fa-file-medical chip-icon" />
                                    {{ evolucao.modeloNome }}
                                </span>

                                <!-- Seções preenchidas -->
                                <span class="chip chip-secoes">
                                    <i class="fa-solid fa-list-check chip-icon" />
                                    {{ contarSecoesPreenchidas(evolucao) }}/{{ evolucao.modeloSnapshot.length }} seções
                                </span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Seção de anexos -->
                <div class="card-secao" style="margin-top: 1.5rem">
                    <div class="secao-header">
                        <h3 class="secao-titulo">Anexos</h3>
                    </div>

                    <ul class="anexos-lista">
                        <li v-for="a in anexos" :key="a.id" class="anexo-item">
                            <button
                                type="button"
                                class="anexo-btn"
                                @click="emit('downloadAnexo', a)"
                            >
                                <i class="fa-solid fa-paperclip" />
                                {{ a.nomeOriginal }}
                            </button>
                            <span class="anexo-meta">
                                {{ formatarTamanho(a.tamanhoBytes) }} · {{ formatarData(a.criadoEm) }}
                            </span>
                        </li>
                        <li v-if="anexos.length === 0" class="anexo-vazio">
                            Nenhum anexo ainda.
                        </li>
                    </ul>

                    <input
                        ref="inputFileRef"
                        type="file"
                        class="input-file-hidden"
                        @change="onArquivoSelecionado"
                    />

                    <div class="upload-row">
                        <div class="upload-area" @click="abrirSeletorArquivo">
                            <i class="fa-solid fa-paperclip upload-icon" />
                            <span class="upload-texto">
                                {{ nomeArquivoSelecionado ?? "Escolher arquivo..." }}
                            </span>
                        </div>
                        <AppButton
                            size="sm"
                            :loading="uploadando"
                            :disabled="uploadando || !nomeArquivoSelecionado"
                            @click="onEnviarAnexo"
                        >
                            {{ uploadando ? "Enviando..." : "Enviar" }}
                        </AppButton>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.anteriores-wrap { display: flex; flex-direction: column; gap: 1rem; }

/* Grid 2 colunas */
.grid-layout {
    display: grid;
    grid-template-columns: 180px 1fr;
    gap: 1.25rem;
    align-items: start;
}
@media (max-width: 900px) {
    .grid-layout { grid-template-columns: 1fr; }
    .sidebar-wrap { display: none; }
}

/* Sidebar */
.sidebar-wrap { position: sticky; top: 1rem; }
.nav-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    overflow: hidden;
}
.nav-header {
    background: var(--bg-hover);
    padding: 0.4rem 0.75rem;
    font-size: 0.7em;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
    border-bottom: 1px solid var(--border);
}
.nav-links { display: flex; flex-direction: column; }
.nav-link {
    display: block;
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: none;
    border-bottom: 1px solid var(--border);
    background: none;
    cursor: pointer;
    font-family: inherit;
    font-size: 0.82em;
    font-weight: 600;
    color: var(--text-muted);
    text-align: left;
    transition: background 0.12s, color 0.12s;
}
.nav-link:last-child { border-bottom: none; }
.nav-link:hover { background: var(--bg-hover); color: var(--text); }
.nav-link.ativo { background: hsl(var(--primary-light)); color: hsl(var(--primary)); }
.nav-link-todos { font-size: 0.75em; font-weight: 500; color: var(--text-muted); }

/* Conteúdo principal */
.conteudo-principal { display: flex; flex-direction: column; gap: 0.75rem; }

.historico-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-bottom: 0.25rem;
}
.historico-titulo {
    font-size: 0.9em;
    font-weight: 700;
    margin: 0;
    color: hsl(var(--primary-dark));
}

/* Grupos de ano */
.ano-grupo { margin-bottom: 0.75rem; }
.ano-titulo {
    font-size: 0.78em;
    font-weight: 700;
    color: hsl(var(--primary));
    letter-spacing: 0.04em;
    margin: 0 0 0.5rem;
    padding-bottom: 0.3rem;
    border-bottom: 2px solid hsl(var(--primary-light));
}

/* Linha de evolução (timeline) */
.evolucao-linha {
    display: flex;
    gap: 0.65rem;
    margin-bottom: 0.5rem;
}
.timeline-col {
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-top: 0.15rem;
    flex-shrink: 0;
}
.timeline-marker {
    width: 1.75rem;
    height: 1.75rem;
    border-radius: 50%;
    background: hsl(var(--primary-light));
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.72em;
    flex-shrink: 0;
}
.timeline-line {
    flex: 1;
    width: 1px;
    background: hsl(var(--primary) / 0.15);
    margin-top: 0.2rem;
    min-height: 0.5rem;
}

/* Card compacto */
.evolucao-card {
    flex: 1;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.55rem 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.evol-data {
    font-size: 0.78em;
    font-weight: 700;
    color: var(--text-muted);
    line-height: 1;
}

/* Chips de informação */
.evol-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
}

.chip {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.2rem 0.55rem;
    border-radius: 999px;
    font-size: 0.76em;
    font-weight: 600;
    white-space: nowrap;
}

.chip-icon { font-size: 0.85em; flex-shrink: 0; }

.chip-prof {
    background: hsl(var(--primary-light));
    color: hsl(var(--primary-dark));
}

.chip-modelo {
    background: hsl(var(--success) / 0.10);
    color: hsl(var(--success) / 0.85);
}

.chip-secoes {
    background: var(--bg-hover);
    color: var(--text-muted);
}

/* Card seção genérico */
.card-secao {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem 1.25rem;
}
.secao-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;
}
.secao-titulo {
    font-size: 0.88em;
    font-weight: 700;
    margin: 0;
    color: hsl(var(--primary));
}

/* Anexos */
.anexos-lista { list-style: none; padding: 0; margin: 0; }
.anexo-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.45rem 0;
    border-bottom: 1px solid var(--border);
    gap: 0.75rem;
}
.anexo-item:last-child { border-bottom: none; }
.anexo-btn {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    background: none;
    border: none;
    cursor: pointer;
    font-family: inherit;
    font-size: 0.85em;
    color: hsl(var(--primary));
    text-decoration: underline;
    padding: 0;
}
.anexo-btn:hover { opacity: 0.8; }
.anexo-meta { font-size: 0.75em; color: var(--text-muted); white-space: nowrap; }
.anexo-vazio { font-style: italic; color: var(--text-muted); font-size: 0.85em; padding: 0.5rem 0; }

.input-file-hidden { display: none; }

.upload-row {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    margin-top: 0.75rem;
    padding-top: 0.75rem;
    border-top: 1px solid var(--border);
    flex-wrap: wrap;
}
.upload-area {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex: 1;
    min-width: 0;
    padding: 0.5rem 0.85rem;
    border: 1px dashed var(--border-strong);
    border-radius: var(--radius);
    background: var(--bg-hover);
    cursor: pointer;
    transition: border-color 0.12s, background 0.12s;
}
.upload-area:hover {
    border-color: hsl(var(--primary) / 0.5);
    background: hsl(var(--primary-light));
}
.upload-icon { color: var(--text-muted); font-size: 0.8em; flex-shrink: 0; }
.upload-texto { font-size: 0.82em; color: var(--text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
