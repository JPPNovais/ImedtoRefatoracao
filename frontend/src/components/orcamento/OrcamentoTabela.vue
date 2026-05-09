<script setup lang="ts">
import { computed } from "vue"
import type { OrcamentoResumo } from "@/services/orcamentoService"
import OrcamentoStatusPill from "./OrcamentoStatusPill.vue"
import { AppEmptyState } from "@/components/ui"

const props = defineProps<{
    orcamentos: OrcamentoResumo[]
    carregando?: boolean
}>()

const emit = defineEmits<{
    abrir: [o: OrcamentoResumo]
}>()

const hoje = new Date().toISOString().slice(0, 10)

function diasEntre(a: string, b: string) {
    return Math.round((new Date(b + "T00:00:00").getTime() - new Date(a + "T00:00:00").getTime()) / 86400000)
}

function fmtBRL(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }

function fmtDataCurta(s: string) {
    const dt = new Date(s + "T00:00:00")
    return dt.toLocaleDateString("pt-BR", { day: "2-digit", month: "short" })
}

interface OrcamentoEnriquecido extends OrcamentoResumo {
    expDias: number | null
    expirando: boolean
    expirado: boolean
}

const linhas = computed<OrcamentoEnriquecido[]>(() =>
    props.orcamentos.map(o => {
        const expDias = o.validade ? diasEntre(hoje, o.validade) : null
        const expirando = expDias !== null && expDias >= 0 && expDias <= 7 && o.status === "Enviado"
        const expirado  = expDias !== null && expDias < 0
        return { ...o, expDias, expirando, expirado }
    })
)
</script>

<template>
    <div class="orc-table-wrap">
        <!-- Header -->
        <div class="orc-thead">
            <div>Orçamento</div>
            <div>Paciente</div>
            <div>Status</div>
            <div class="r">Valor</div>
            <div class="r">Validade</div>
            <div></div>
        </div>

        <!-- Empty -->
        <div v-if="!carregando && linhas.length === 0" class="orc-empty-row">
            <AppEmptyState
                icone="fa-regular fa-file"
                titulo="Nenhum orçamento neste filtro"
                descricao="Tente ajustar os filtros ou crie um novo orçamento."
            />
        </div>

        <!-- Loading skeleton -->
        <div v-else-if="carregando" class="orc-loading">
            <div v-for="i in 5" :key="i" class="orc-skel-row">
                <div class="skel skel-text"></div>
                <div class="skel skel-text"></div>
                <div class="skel skel-pill"></div>
                <div class="skel skel-num r"></div>
                <div class="skel skel-num r"></div>
                <div></div>
            </div>
        </div>

        <!-- Rows -->
        <template v-else>
            <button
                v-for="o in linhas"
                :key="o.id"
                class="orc-row"
                type="button"
                @click="emit('abrir', o)"
            >
                <div>
                    <div class="row-num">{{ o.numero || `#${o.id}` }}</div>
                    <div class="row-title">{{ o.pacienteNome }}</div>
                </div>
                <div>
                    <div class="row-strong">{{ o.criadoPorNome }}</div>
                    <div class="row-dim">{{ fmtDataCurta(o.criadoEm) }}</div>
                </div>
                <div>
                    <OrcamentoStatusPill :status="o.status" />
                </div>
                <div class="r">
                    <div class="row-strong">{{ fmtBRL(o.total) }}</div>
                </div>
                <div class="r">
                    <div
                        class="row-strong"
                        :class="{ 'val-warn': o.expirando, 'val-err': o.expirado }"
                    >
                        {{ fmtDataCurta(o.validade) }}
                    </div>
                    <div class="row-dim">
                        <template v-if="o.expirado">Expirado</template>
                        <template v-else-if="o.expirando">em {{ o.expDias }}d</template>
                        <template v-else>válido</template>
                    </div>
                </div>
                <div class="row-actions">
                    <span class="btn-icon btn-icon-ver" aria-hidden="true">
                        <i class="fa-solid fa-eye"></i>
                    </span>
                </div>
            </button>
        </template>
    </div>
</template>

<style scoped>
.orc-table-wrap {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    overflow: hidden;
}

.orc-thead {
    display: grid;
    grid-template-columns: 1.6fr 1.2fr 1fr 0.9fr 0.9fr 44px;
    gap: 12px;
    align-items: center;
    padding: 12px 18px;
    background: hsl(var(--secondary) / 0.03);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.5);
}
.orc-thead .r { text-align: right; }

.orc-row {
    display: grid;
    grid-template-columns: 1.6fr 1.2fr 1fr 0.9fr 0.9fr 44px;
    gap: 12px;
    align-items: center;
    padding: 14px 18px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
    text-decoration: none;
    color: hsl(var(--secondary));
    background: transparent;
    border-left: none;
    border-right: none;
    border-top: none;
    font-family: inherit;
    cursor: pointer;
    text-align: left;
    width: 100%;
    transition: background 0.12s;
}
.orc-row:last-child { border-bottom: 0; }
.orc-row:hover { background: hsl(var(--primary) / 0.025); }
.orc-row .r { text-align: right; }

.row-num    { font-size: 10.5px; color: hsl(var(--secondary) / 0.5); font-weight: 700; margin-bottom: 2px; }
.row-title  { font-size: 13.5px; font-weight: 600; }
.row-strong { font-size: 13px; font-weight: 600; }
.row-dim    { font-size: 11.5px; color: hsl(var(--secondary) / 0.55); margin-top: 2px; }

.val-warn { color: hsl(40 90% 33%); }
.val-err  { color: hsl(var(--error)); }

.row-actions { display: flex; justify-content: flex-end; }

/* Loading skeleton */
.orc-skel-row {
    display: grid;
    grid-template-columns: 1.6fr 1.2fr 1fr 0.9fr 0.9fr 44px;
    gap: 12px;
    align-items: center;
    padding: 14px 18px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.skel {
    border-radius: 4px;
    background: hsl(var(--secondary) / 0.06);
    animation: shimmer 1.4s infinite;
}
.skel-text  { height: 32px; }
.skel-pill  { height: 22px; width: 80px; border-radius: 999px; }
.skel-num   { height: 24px; width: 80px; }
.skel-num.r { margin-left: auto; }

.orc-empty-row { padding: 24px; }

@keyframes shimmer {
    0%, 100% { opacity: 0.6; }
    50%       { opacity: 1; }
}
</style>
