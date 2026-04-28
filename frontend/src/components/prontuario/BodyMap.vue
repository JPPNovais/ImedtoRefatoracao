<!--
  Body Map interativo do exame físico.
  SVG simplificado com regiões clicáveis (vista anterior + posterior).
  v-model = { regioesMarcadas: string[], notas: Record<regiao, string> }
-->
<script setup lang="ts">
import { computed } from "vue"

interface Data {
    regioesMarcadas?: string[]
    notas?: Record<string, string>
}

const props = defineProps<{ modelValue: Data; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: Data] }>()

function atualizar(patch: Partial<Data>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

const marcadas = computed(() => new Set(props.modelValue.regioesMarcadas ?? []))
const notas    = computed(() => props.modelValue.notas ?? {})

function toggleRegiao(chave: string) {
    if (props.readOnly) return
    const set = new Set(marcadas.value)
    if (set.has(chave)) set.delete(chave)
    else set.add(chave)
    atualizar({ regioesMarcadas: Array.from(set) })
}

function setNota(chave: string, valor: string) {
    atualizar({ notas: { ...notas.value, [chave]: valor } })
}

// Catálogo de regiões exibidas no body map.
// Layout simplificado: coordenadas em um SVG 360×560 (anterior) + outro posterior.
const REGIOES_ANTERIORES = [
    { chave: "cabeca",          label: "Cabeça",          cx: 180, cy:  50, r: 30 },
    { chave: "pescoco",         label: "Pescoço",         cx: 180, cy:  95, r: 12 },
    { chave: "ombro_d",         label: "Ombro D",         cx: 230, cy: 120, r: 18 },
    { chave: "ombro_e",         label: "Ombro E",         cx: 130, cy: 120, r: 18 },
    { chave: "torax",           label: "Tórax",           cx: 180, cy: 165, r: 38 },
    { chave: "abdome",          label: "Abdome",          cx: 180, cy: 230, r: 35 },
    { chave: "pelve",           label: "Pelve",           cx: 180, cy: 290, r: 30 },
    { chave: "braco_d",         label: "Braço D",         cx: 245, cy: 180, r: 15 },
    { chave: "braco_e",         label: "Braço E",         cx: 115, cy: 180, r: 15 },
    { chave: "antebraco_d",     label: "Antebraço D",     cx: 265, cy: 230, r: 13 },
    { chave: "antebraco_e",     label: "Antebraço E",     cx:  95, cy: 230, r: 13 },
    { chave: "mao_d",           label: "Mão D",           cx: 280, cy: 275, r: 12 },
    { chave: "mao_e",           label: "Mão E",           cx:  80, cy: 275, r: 12 },
    { chave: "coxa_d",          label: "Coxa D",          cx: 205, cy: 360, r: 20 },
    { chave: "coxa_e",          label: "Coxa E",          cx: 155, cy: 360, r: 20 },
    { chave: "joelho_d",        label: "Joelho D",        cx: 205, cy: 420, r: 13 },
    { chave: "joelho_e",        label: "Joelho E",        cx: 155, cy: 420, r: 13 },
    { chave: "perna_d",         label: "Perna D",         cx: 205, cy: 470, r: 15 },
    { chave: "perna_e",         label: "Perna E",         cx: 155, cy: 470, r: 15 },
    { chave: "pe_d",            label: "Pé D",            cx: 205, cy: 525, r: 13 },
    { chave: "pe_e",            label: "Pé E",            cx: 155, cy: 525, r: 13 },
]
const REGIOES_POSTERIORES = [
    { chave: "nuca",            label: "Nuca",            cx: 180, cy:  60, r: 20 },
    { chave: "dorso_sup",       label: "Dorso superior",  cx: 180, cy: 140, r: 35 },
    { chave: "lombar",          label: "Lombar",          cx: 180, cy: 220, r: 32 },
    { chave: "gluteo_d",        label: "Glúteo D",        cx: 205, cy: 295, r: 22 },
    { chave: "gluteo_e",        label: "Glúteo E",        cx: 155, cy: 295, r: 22 },
    { chave: "posterior_coxa_d",label: "Post. coxa D",    cx: 205, cy: 370, r: 18 },
    { chave: "posterior_coxa_e",label: "Post. coxa E",    cx: 155, cy: 370, r: 18 },
    { chave: "panturrilha_d",   label: "Panturrilha D",   cx: 205, cy: 465, r: 18 },
    { chave: "panturrilha_e",   label: "Panturrilha E",   cx: 155, cy: 465, r: 18 },
    { chave: "calcanhar_d",     label: "Calcanhar D",     cx: 205, cy: 530, r: 11 },
    { chave: "calcanhar_e",     label: "Calcanhar E",     cx: 155, cy: 530, r: 11 },
]

const listaMarcadas = computed(() =>
    [...REGIOES_ANTERIORES, ...REGIOES_POSTERIORES]
        .filter(r => marcadas.value.has(r.chave)),
)
</script>

<template>
    <div class="body-map">
        <div class="body-map-header">
            <h4 class="bm-titulo">Mapa corporal</h4>
            <span class="bm-hint">Clique em uma região para marcá-la como examinada.</span>
        </div>

        <div class="bm-vistas">
            <!-- ── Vista anterior ── -->
            <div class="bm-vista">
                <svg viewBox="0 0 360 560" class="bm-svg" role="img" aria-label="Mapa corporal — vista anterior">
                    <!-- Silhueta -->
                    <path class="silhueta"
                        d="M180 20 a32 32 0 0 1 32 32 c0 14 -5 24 -10 32 l6 20 c30 6 44 30 46 60 l-6 60 -6 30 h12
                           l6 40 -4 30 -8 70 -4 50 h-12 l-2 -50 -6 -40 -8 -50 -2 -50
                           h-32 l-2 50 -8 50 -6 40 -2 50 h-12 l-4 -50 -8 -70 -4 -30 6 -40 h12
                           l-6 -30 -6 -60 c2 -30 16 -54 46 -60 l6 -20 c-5 -8 -10 -18 -10 -32 a32 32 0 0 1 32 -32 z"
                    />
                    <!-- Regiões clicáveis -->
                    <g class="regioes">
                        <circle
                            v-for="r in REGIOES_ANTERIORES" :key="'a_' + r.chave"
                            :cx="r.cx" :cy="r.cy" :r="r.r"
                            :class="['regiao', { marcada: marcadas.has(r.chave) }]"
                            @click="toggleRegiao(r.chave)"
                            role="button" :aria-label="r.label"
                        ><title>{{ r.label }}</title></circle>
                    </g>
                </svg>
                <span class="bm-legenda">Anterior</span>
            </div>

            <!-- ── Vista posterior ── -->
            <div class="bm-vista">
                <svg viewBox="0 0 360 560" class="bm-svg" role="img" aria-label="Mapa corporal — vista posterior">
                    <path class="silhueta"
                        d="M180 20 a32 32 0 0 1 32 32 c0 14 -5 24 -10 32 l6 20 c30 6 44 30 46 60 l-6 60 -6 30 h12
                           l6 40 -4 30 -8 70 -4 50 h-12 l-2 -50 -6 -40 -8 -50 -2 -50
                           h-32 l-2 50 -8 50 -6 40 -2 50 h-12 l-4 -50 -8 -70 -4 -30 6 -40 h12
                           l-6 -30 -6 -60 c2 -30 16 -54 46 -60 l6 -20 c-5 -8 -10 -18 -10 -32 a32 32 0 0 1 32 -32 z"
                    />
                    <g class="regioes">
                        <circle
                            v-for="r in REGIOES_POSTERIORES" :key="'p_' + r.chave"
                            :cx="r.cx" :cy="r.cy" :r="r.r"
                            :class="['regiao', { marcada: marcadas.has(r.chave) }]"
                            @click="toggleRegiao(r.chave)"
                            role="button" :aria-label="r.label"
                        ><title>{{ r.label }}</title></circle>
                    </g>
                </svg>
                <span class="bm-legenda">Posterior</span>
            </div>

            <!-- ── Lista lateral de marcadas ── -->
            <div class="bm-lista">
                <h5 class="bm-lista-titulo">
                    Regiões examinadas
                    <span class="bm-contador">{{ listaMarcadas.length }}</span>
                </h5>

                <p v-if="listaMarcadas.length === 0" class="bm-vazio">
                    Nenhuma região marcada.
                </p>

                <ul v-else class="bm-regioes-lista">
                    <li v-for="r in listaMarcadas" :key="r.chave" class="bm-regiao-item">
                        <div class="bm-regiao-topo">
                            <strong>{{ r.label }}</strong>
                            <button
                                class="btn-icon-x" title="Desmarcar"
                                :disabled="readOnly"
                                @click="toggleRegiao(r.chave)"
                            >✕</button>
                        </div>
                        <textarea
                            :value="notas[r.chave] ?? ''" rows="2"
                            class="bm-nota"
                            :placeholder="`Achados em ${r.label.toLowerCase()}...`"
                            :disabled="readOnly"
                            @input="(e) => setNota(r.chave, (e.target as HTMLTextAreaElement).value)"
                        ></textarea>
                    </li>
                </ul>
            </div>
        </div>
    </div>
</template>

<style scoped>
.body-map {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 0.5rem;
    padding: 1rem 1.25rem;
    display: flex; flex-direction: column; gap: 0.75rem;
}
.body-map-header {
    display: flex; justify-content: space-between; align-items: baseline;
    flex-wrap: wrap; gap: 0.5rem;
}
.bm-titulo { font-size: 0.95em; font-weight: 700; margin: 0; color: hsl(var(--primary)); }
.bm-hint   { font-size: 0.78em; color: hsl(var(--muted-foreground)); }

.bm-vistas {
    display: grid;
    grid-template-columns: 220px 220px 1fr;
    gap: 1rem; align-items: flex-start;
}

.bm-vista {
    display: flex; flex-direction: column; align-items: center; gap: 0.35rem;
}
.bm-svg {
    width: 100%; max-width: 220px; height: auto;
    background: hsl(var(--muted) / 0.3);
    border-radius: 0.5rem;
}
.silhueta {
    fill: hsl(var(--muted));
    stroke: hsl(var(--border));
    stroke-width: 1.5;
}
.regioes .regiao {
    fill: hsl(var(--primary) / 0.15);
    stroke: hsl(var(--primary) / 0.45);
    stroke-width: 1;
    cursor: pointer;
    transition: all 0.14s;
}
.regioes .regiao:hover {
    fill: hsl(var(--primary) / 0.45);
    stroke: hsl(var(--primary));
    stroke-width: 2;
}
.regioes .regiao.marcada {
    fill: hsl(var(--primary));
    stroke: hsl(var(--primary-dark));
    stroke-width: 2;
}
.bm-legenda {
    font-size: 0.78em; font-weight: 600;
    color: hsl(var(--muted-foreground));
    text-transform: uppercase; letter-spacing: 0.04em;
}

.bm-lista {
    display: flex; flex-direction: column; gap: 0.5rem; min-width: 0;
}
.bm-lista-titulo {
    font-size: 0.82em; font-weight: 700; margin: 0;
    display: flex; align-items: center; gap: 0.5rem;
}
.bm-contador {
    background: hsl(var(--primary-dark)); color: hsl(var(--neutral));
    font-size: 0.7em; padding: 1px 7px; border-radius: 999px; font-weight: 700;
}
.bm-vazio {
    font-size: 0.82em; color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.4); padding: 0.6rem;
    border-radius: 0.375rem; text-align: center; margin: 0;
}
.bm-regioes-lista {
    list-style: none; padding: 0; margin: 0;
    display: flex; flex-direction: column; gap: 0.5rem;
    max-height: 540px; overflow-y: auto;
}
.bm-regiao-item {
    background: hsl(var(--accent) / 0.4);
    border: 1px solid hsl(var(--border));
    border-radius: 0.375rem;
    padding: 0.5rem 0.6rem;
    display: flex; flex-direction: column; gap: 0.3rem;
}
.bm-regiao-topo { display: flex; align-items: center; justify-content: space-between; }
.bm-regiao-topo strong { font-size: 0.85em; }
.btn-icon-x {
    border: none; background: transparent; cursor: pointer;
    width: 22px; height: 22px; border-radius: 50%;
    color: hsl(var(--muted-foreground)); font: inherit;
    transition: all 0.12s;
}
.btn-icon-x:hover:not(:disabled) {
    background: hsl(var(--destructive) / 0.12);
    color: hsl(var(--destructive));
}
.bm-nota {
    width: 100%; box-sizing: border-box;
    padding: 0.35rem 0.5rem;
    border: 1px solid hsl(var(--border)); border-radius: 0.375rem;
    font: inherit; font-size: 0.8em; background: hsl(var(--neutral));
    color: hsl(var(--foreground)); resize: vertical;
}
.bm-nota:focus { outline: none; border-color: hsl(var(--primary)); }

@media (max-width: 1100px) {
    .bm-vistas { grid-template-columns: 1fr 1fr; }
    .bm-lista { grid-column: 1 / -1; }
}
@media (max-width: 640px) {
    .bm-vistas { grid-template-columns: 1fr; }
}
</style>
