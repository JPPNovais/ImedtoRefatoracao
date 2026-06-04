<script setup lang="ts">
import { computed } from "vue"
import { PERMISSION_AREAS, type AreaPermissao } from "@/constants/permissions"

/**
 * Matriz de permissões action-level. Recebe um array de chaves no formato
 * `area.acao` e renderiza cada área (Agenda, Prontuário, ...) com suas
 * ações como checkboxes.
 *
 * Modos:
 *  - `readOnly`: somente leitura (visualizar permissões de um papel do sistema).
 *  - `compact`: somente leitura condensada — mostra apenas as áreas/ações
 *    concedidas, em formato de chips informativos (sem cara de checkbox). Útil
 *    quando os acessos são herdados de um modelo e não são editáveis ali (ex:
 *    aba de permissão do profissional). Implica `readOnly`.
 *  - editável: emite `update:modelValue` com o array novo a cada toggle.
 *
 * Compatibilidade: aceita chaves legadas sem ponto (ex: `"agenda"`) — quando
 * todas as ações de uma área estão presentes, a chave umbrella é considerada
 * concedida (apenas para detecção; ao salvar, sempre persiste no formato granular).
 */
const props = defineProps<{
    modelValue: string[]
    readOnly?: boolean
    compact?: boolean
}>()

const emit = defineEmits<{
    (e: "update:modelValue", v: string[]): void
}>()

interface AreaResumo {
    area: AreaPermissao
    granted: number
    total: number
    fully: boolean
    none: boolean
}

const resumos = computed<AreaResumo[]>(() =>
    PERMISSION_AREAS.map(area => {
        const total = area.acoes.length
        const grantedAcoes = area.acoes.filter(ac =>
            props.modelValue.includes(`${area.chave}.${ac.chave}`)
            // Legado: chave umbrella concede tudo.
            || props.modelValue.includes(area.chave),
        ).length
        return {
            area,
            granted: grantedAcoes,
            total,
            fully: grantedAcoes === total,
            none: grantedAcoes === 0,
        }
    }),
)

// Apenas áreas com ao menos uma ação concedida (visão compacta).
const resumosConcedidos = computed(() => resumos.value.filter(r => !r.none))

function acoesConcedidas(area: AreaPermissao) {
    return area.acoes.filter(ac => temPerm(area.chave, ac.chave))
}

function temPerm(area: string, acao: string): boolean {
    const chave = `${area}.${acao}`
    return props.modelValue.includes(chave) || props.modelValue.includes(area)
}

function togglePerm(area: string, acao: string) {
    if (props.readOnly) return
    const chave = `${area}.${acao}`
    const next = temPerm(area, acao)
        ? props.modelValue.filter(p => p !== chave && p !== area).concat(
            // Se a chave umbrella estava presente, expande para as outras ações que continuam.
            props.modelValue.includes(area)
                ? PERMISSION_AREAS.find(a => a.chave === area)!.acoes
                    .filter(ac => ac.chave !== acao)
                    .map(ac => `${area}.${ac.chave}`)
                : [],
        )
        : [...props.modelValue, chave]
    emit("update:modelValue", dedupe(next))
}

function toggleArea(area: AreaPermissao) {
    if (props.readOnly) return
    const allPerms = area.acoes.map(ac => `${area.chave}.${ac.chave}`)
    const allHave = allPerms.every(p => props.modelValue.includes(p) || props.modelValue.includes(area.chave))
    const sem = props.modelValue.filter(p => p !== area.chave && !allPerms.includes(p))
    const next = allHave ? sem : [...sem, ...allPerms]
    emit("update:modelValue", dedupe(next))
}

function dedupe(arr: string[]) {
    return [...new Set(arr)]
}
</script>

<template>
    <!-- Visão compacta, somente leitura: só áreas/ações concedidas, em chips. -->
    <div v-if="compact" class="perm-compact">
        <div v-for="r in resumosConcedidos" :key="r.area.chave" class="pc-area">
            <div class="pc-head">
                <div class="pc-icon">
                    <i class="fa-solid" :class="r.area.icone"></i>
                </div>
                <b class="pc-area-label">{{ r.area.label }}</b>
                <span v-if="r.fully" class="pc-tag pc-tag-full">Acesso total</span>
                <span v-else class="pc-tag pc-tag-partial">{{ r.granted }}/{{ r.total }}</span>
            </div>
            <ul class="pc-perms">
                <li v-for="acao in acoesConcedidas(r.area)" :key="acao.chave" class="pc-perm">
                    <i class="fa-solid fa-check"></i>{{ acao.label }}
                </li>
            </ul>
        </div>
        <p v-if="!resumosConcedidos.length" class="pc-empty">
            <i class="fa-solid fa-ban"></i> Esta permissão não concede nenhum acesso.
        </p>
    </div>

    <div v-else class="perm-matrix">
        <div
            v-for="r in resumos" :key="r.area.chave"
            class="perm-area"
            :class="{ full: r.fully, partial: !r.fully && !r.none, none: r.none }"
        >
            <div class="pa-head">
                <div class="pa-info">
                    <div class="pa-icon" :class="{ full: r.fully, partial: !r.fully && !r.none, none: r.none }">
                        <i class="fa-solid" :class="r.area.icone"></i>
                    </div>
                    <div>
                        <b>{{ r.area.label }}</b>
                        <span>{{ r.area.descricao }}</span>
                    </div>
                </div>
                <div class="pa-summary">
                    <span v-if="r.none" class="pa-tag pa-tag-none">Sem acesso</span>
                    <span v-else-if="r.fully" class="pa-tag pa-tag-full">Acesso total</span>
                    <span v-else class="pa-tag pa-tag-partial">{{ r.granted }}/{{ r.total }} permissões</span>
                    <button
                        v-if="!readOnly"
                        type="button"
                        class="pa-toggle-all"
                        @click="toggleArea(r.area)"
                    >
                        {{ r.fully ? "Remover tudo" : "Conceder tudo" }}
                    </button>
                </div>
            </div>
            <div class="pa-actions">
                <label
                    v-for="acao in r.area.acoes" :key="acao.chave"
                    class="pa-action"
                    :class="{ granted: temPerm(r.area.chave, acao.chave), readonly: readOnly }"
                >
                    <input
                        type="checkbox"
                        :checked="temPerm(r.area.chave, acao.chave)"
                        :disabled="readOnly"
                        @change="togglePerm(r.area.chave, acao.chave)"
                    />
                    <span class="pa-check">
                        <i v-if="temPerm(r.area.chave, acao.chave)" class="fa-solid fa-check"></i>
                    </span>
                    <span class="pa-label">{{ acao.label }}</span>
                </label>
            </div>
        </div>
    </div>
</template>

<style scoped>
/* ---- Visão compacta (somente leitura, herdada de um modelo) ---- */
.perm-compact { display: flex; flex-direction: column; gap: 6px; }
.pc-area {
    display: grid;
    grid-template-columns: auto 1fr;
    align-items: baseline;
    gap: 4px 12px;
    padding: 10px 4px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.07);
}
.pc-area:last-child { border-bottom: none; }
.pc-head { display: flex; align-items: center; gap: 8px; min-width: 150px; }
.pc-icon {
    width: 26px; height: 26px; border-radius: 6px;
    display: inline-flex; align-items: center; justify-content: center;
    background: hsl(var(--primary) / 0.08); color: hsl(var(--primary));
    font-size: 12px; flex-shrink: 0;
}
.pc-area-label { font-size: 13px; font-weight: 700; color: hsl(var(--primary-dark)); }
.pc-tag { font-size: 10px; font-weight: 700; padding: 2px 7px; border-radius: 999px; white-space: nowrap; }
.pc-tag-full    { background: hsl(var(--success) / 0.14); color: hsl(160 79% 30%); }
.pc-tag-partial { background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.6); }

.pc-perms {
    list-style: none; margin: 0; padding: 0;
    display: flex; flex-wrap: wrap; gap: 6px;
}
.pc-perm {
    display: inline-flex; align-items: center; gap: 5px;
    font-size: 12px; color: hsl(var(--secondary) / 0.8);
    background: hsl(var(--secondary) / 0.05);
    padding: 3px 9px; border-radius: 999px;
}
.pc-perm i { font-size: 9px; color: hsl(var(--success)); }
.pc-empty {
    display: flex; align-items: center; gap: 8px;
    font-size: 13px; color: hsl(var(--secondary) / 0.6);
    padding: 12px 4px; margin: 0;
}

.perm-matrix { display: flex; flex-direction: column; gap: 10px; }

.perm-area {
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px;
    background: hsl(var(--secondary) / 0.02);
    overflow: hidden;
}
.perm-area.full    { border-color: hsl(var(--success) / 0.25); background: hsl(var(--success) / 0.025); }
.perm-area.partial { border-color: hsl(var(--warning) / 0.3);  background: hsl(var(--warning) / 0.03); }

.pa-head {
    display: flex; align-items: center; justify-content: space-between; gap: 12px;
    padding: 12px 16px;
}
.pa-info { display: flex; align-items: center; gap: 12px; }
.pa-icon {
    width: 36px; height: 36px; border-radius: 8px;
    display: inline-flex; align-items: center; justify-content: center;
    background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.5);
    font-size: 14px;
}
.pa-icon.full    { background: hsl(var(--success) / 0.15); color: hsl(160 79% 32%); }
.pa-icon.partial { background: hsl(var(--warning) / 0.18); color: hsl(40 90% 35%); }
.pa-icon.none    { background: hsl(var(--secondary) / 0.06); color: hsl(var(--secondary) / 0.35); }
.pa-info b { display: block; font-size: 14px; color: hsl(var(--primary-dark)); font-weight: 700; }
.pa-info span { font-size: 12px; color: hsl(var(--secondary) / 0.65); }

.pa-summary { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.pa-tag { font-size: 11px; font-weight: 700; padding: 4px 10px; border-radius: 999px; }
.pa-tag-full    { background: hsl(var(--success) / 0.15); color: hsl(160 79% 30%); }
.pa-tag-partial { background: hsl(var(--warning) / 0.18); color: hsl(40 90% 32%); }
.pa-tag-none    { background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.55); }

.pa-toggle-all {
    background: none; border: 1px solid hsl(var(--secondary) / 0.15);
    padding: 4px 10px; border-radius: 6px;
    font-family: inherit; font-size: 11px; font-weight: 600;
    color: hsl(var(--secondary) / 0.7); cursor: pointer;
    transition: all 150ms;
}
.pa-toggle-all:hover { border-color: hsl(var(--primary)); color: hsl(var(--primary)); }

.pa-actions {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 6px; padding: 10px 16px 14px;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
    background: white;
}
.pa-action {
    display: flex; align-items: center; gap: 10px;
    padding: 8px 10px; border-radius: 6px;
    cursor: pointer; transition: background 150ms;
    position: relative;
}
.pa-action:not(.readonly):hover { background: hsl(var(--secondary) / 0.04); }
.pa-action.readonly { cursor: default; }
.pa-action input {
    position: absolute; opacity: 0; pointer-events: none;
}
.pa-check {
    width: 18px; height: 18px; border-radius: 4px;
    border: 1.5px solid hsl(var(--secondary) / 0.25); background: white;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 10px; flex-shrink: 0;
    transition: all 150ms;
    color: white;
}
.pa-action.granted .pa-check { background: hsl(var(--success)); border-color: hsl(var(--success)); }
.pa-action.granted .pa-label { color: hsl(var(--primary-dark)); font-weight: 600; }
.pa-label { font-size: 12px; color: hsl(var(--secondary) / 0.7); }
</style>
