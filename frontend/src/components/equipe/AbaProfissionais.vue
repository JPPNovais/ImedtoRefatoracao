<script setup lang="ts">
import { computed, ref, watch } from "vue"
import {
    AppButton, AppEmptyState, AppFilterPills, AppRolePill, AppSearchInput, AppSelect, AppStatusPill,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { useAuthStore } from "@/stores/authStore"
import type { ProfissionalVinculado } from "@/services/vinculoService"
import type { ModeloPermissao } from "@/services/permissaoService"

/**
 * Aba "Profissionais": lista de profissionais ativos/inativos (não pendentes),
 * com busca debounced, filtro de status e por papel/modelo.
 *
 * Bulk actions e edição abrem o modal de detalhes (controlado pela view-pai).
 */
const props = defineProps<{
    profissionais: ProfissionalVinculado[]
    modelos: ModeloPermissao[]
}>()

const emit = defineEmits<{
    (e: "abrir-detalhes", p: ProfissionalVinculado): void
    (e: "abrir-convite"): void
    (e: "acao-massa", payload: { acao: "ativar" | "suspender" | "remover", ids: number[] }): void
}>()

const auth = useAuthStore()

// ─── Filtros locais ────────────────────────────────────────────────────────
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 200) // local-only, mas mantém a UI fluida em listas grandes
const filtroStatus = ref<"todos" | "ativos" | "inativos">("todos")
const filtroModelo = ref<"todos" | number>("todos")

// "Convidado" = vinculo com convite enviado, ainda nao aceito — esses ficam soh
// na aba "Convites pendentes", nao aparecem na lista de profissionais ativos.
const visiveis = computed(() => props.profissionais.filter(p => p.status !== "Convidado"))

const ativos    = computed(() => visiveis.value.filter(p => p.status === "Ativo" || p.status === "Dono"))
const inativos  = computed(() => visiveis.value.filter(p => p.status !== "Ativo" && p.status !== "Dono"))

const filtrados = computed(() => {
    const termo = busca.value.trim().toLowerCase()
    return visiveis.value.filter(p => {
        if (filtroStatus.value === "ativos"   && !(p.status === "Ativo" || p.status === "Dono")) return false
        if (filtroStatus.value === "inativos" && (p.status === "Ativo" || p.status === "Dono")) return false
        if (filtroModelo.value !== "todos" && p.modeloPermissaoId !== filtroModelo.value) return false
        if (termo) {
            const hay = `${p.nomeCompleto ?? ""} ${p.email ?? ""} ${p.especialidade ?? ""} ${p.conselho ?? ""}`.toLowerCase()
            if (!hay.includes(termo)) return false
        }
        return true
    })
})

watch([busca, filtroStatus, filtroModelo], () => {
    // resetar seleção em massa quando os filtros mudam
    selecionados.value = new Set()
})

// ─── Bulk select ───────────────────────────────────────────────────────────
const selecionados = ref<Set<number>>(new Set())

const todosSelecionados = computed(() =>
    filtrados.value.length > 0 && filtrados.value.every(p => selecionados.value.has(p.vinculoId)),
)

function toggleTodos() {
    if (todosSelecionados.value) selecionados.value = new Set()
    else selecionados.value = new Set(filtrados.value.map(p => p.vinculoId))
}

function toggleUm(id: number) {
    const next = new Set(selecionados.value)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    selecionados.value = next
}

// ─── Helpers visuais ───────────────────────────────────────────────────────
function iniciais(p: ProfissionalVinculado): string {
    const base = (p.nomeCompleto && p.nomeCompleto.trim()) || p.email || "?"
    return base
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map(s => s[0]?.toUpperCase())
        .join("")
        || base.charAt(0).toUpperCase()
}

function corAvatar(p: ProfissionalVinculado): string {
    // Determinismo simples — hash do vinculoId em uma paleta amigável.
    const paleta = [
        "hsl(254 56% 38%)", "hsl(190 60% 45%)", "hsl(280 55% 50%)",
        "hsl(140 45% 45%)", "hsl(40 70% 50%)", "hsl(340 55% 55%)",
        "hsl(220 55% 50%)", "hsl(170 50% 40%)",
    ]
    return paleta[p.vinculoId % paleta.length]
}

function modeloDe(p: ProfissionalVinculado): ModeloPermissao | undefined {
    return props.modelos.find(m => m.id === p.modeloPermissaoId)
}

function statusVariante(s: string): "success" | "warning" | "error" | "muted" {
    if (s === "Ativo" || s === "Dono")  return "success"
    if (s === "Convidado")              return "warning"
    if (s === "Bloqueado")              return "error"
    return "muted"
}

function statusLabel(s: string): string {
    if (s === "Dono") return "Dono"
    return s
}

function ehVinculoProprio(p: ProfissionalVinculado): boolean {
    return p.usuarioId === auth.usuario?.id
}

// ─── Ações em massa (delegadas — emite eventos para a view-pai) ───────────
function bulk(acao: "ativar" | "suspender" | "remover") {
    if (!selecionados.value.size) return
    emit("acao-massa", { acao, ids: [...selecionados.value] })
    selecionados.value = new Set()
}
</script>

<template>
    <div class="aba-profissionais">
        <!-- Filtros -->
        <div class="filters-bar">
            <AppSearchInput
                v-model="buscaInput"
                placeholder="Buscar por nome, e-mail, especialidade ou conselho..."
            />
            <AppFilterPills
                v-model="filtroStatus"
                :opcoes="[
                    { valor: 'todos', label: 'Todos', count: visiveis.length },
                    { valor: 'ativos', label: 'Ativos', count: ativos.length, dot: 'success' },
                    { valor: 'inativos', label: 'Inativos', count: inativos.length, dot: 'muted' },
                ]"
            />
            <AppSelect v-model="filtroModelo" class="role-filter">
                <option value="todos">Todas as permissões</option>
                <option v-for="m in modelos" :key="m.id" :value="m.id">{{ m.nome }}</option>
            </AppSelect>
        </div>

        <!-- Bulk bar -->
        <div v-if="selecionados.size > 0" class="bulk-bar">
            <span><b>{{ selecionados.size }}</b> selecionado{{ selecionados.size > 1 ? 's' : '' }}</span>
            <button type="button" class="btn-ghost-sm" @click="bulk('ativar')">
                <i class="fa-solid fa-circle-check"></i> Ativar
            </button>
            <button type="button" class="btn-ghost-sm" @click="bulk('suspender')">
                <i class="fa-solid fa-circle-pause"></i> Suspender
            </button>
            <button type="button" class="btn-ghost-sm danger" @click="bulk('remover')">
                <i class="fa-solid fa-trash"></i> Remover
            </button>
            <button type="button" class="btn-ghost-sm" @click="selecionados = new Set()">
                <i class="fa-solid fa-xmark"></i> Limpar seleção
            </button>
        </div>

        <!-- Lista -->
        <AppEmptyState
            v-if="filtrados.length === 0 && busca.trim()"
            icone="🔎"
            titulo="Nenhum profissional encontrado"
            descricao="Tente outra busca ou ajuste os filtros."
        />
        <AppEmptyState
            v-else-if="filtrados.length === 0"
            icone="👥"
            titulo="Nenhum profissional vinculado"
            descricao="Convide o primeiro profissional para começar."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-user-plus" @click="emit('abrir-convite')">
                    Convidar profissional
                </AppButton>
            </template>
        </AppEmptyState>

        <div v-else class="pros-table">
            <div class="pros-thead">
                <label class="pt-checkbox">
                    <input type="checkbox" :checked="todosSelecionados" @change="toggleTodos" />
                    <span class="cb-box"></span>
                </label>
                <div>Profissional</div>
                <div>Permissão</div>
                <div>Contato</div>
                <div>Status</div>
                <div></div>
            </div>
            <div
                v-for="p in filtrados" :key="p.vinculoId"
                class="pros-row"
                :class="{ selected: selecionados.has(p.vinculoId) }"
                @click="emit('abrir-detalhes', p)"
            >
                <label class="pt-checkbox" @click.stop>
                    <input type="checkbox" :checked="selecionados.has(p.vinculoId)" @change="toggleUm(p.vinculoId)" />
                    <span class="cb-box"></span>
                </label>

                <div class="pr-name">
                    <div class="pr-avatar" :style="{ background: corAvatar(p) }">
                        {{ iniciais(p) }}
                        <span v-if="p.status === 'Dono'" class="owner-crown" title="Dono da clínica">
                            <i class="fa-solid fa-crown"></i>
                        </span>
                    </div>
                    <div class="pr-name-info">
                        <b>{{ p.nomeCompleto || p.email }}</b>
                        <span v-if="p.especialidade || p.conselho" class="pr-spec">
                            {{ p.especialidade }}{{ p.especialidade && p.conselho ? ' · ' : '' }}{{ p.conselho }}
                        </span>
                    </div>
                </div>

                <div class="pr-role">
                    <AppRolePill
                        v-if="modeloDe(p)"
                        :nome="modeloDe(p)!.nome"
                        :icone="modeloDe(p)!.icone"
                        :cor="modeloDe(p)!.cor"
                    />
                    <span v-else class="muted">—</span>
                </div>

                <div class="pr-contact">
                    <span class="ellip">{{ p.email }}</span>
                </div>

                <div>
                    <AppStatusPill :label="statusLabel(p.status)" :variante="statusVariante(p.status)" />
                </div>

                <div class="pr-actions" @click.stop>
                    <button type="button" class="btn-icon-sm" title="Editar perfil e permissões" @click="emit('abrir-detalhes', p)">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </button>
                </div>
                <span v-if="ehVinculoProprio(p)" class="self-tag">Você</span>
            </div>
        </div>
    </div>
</template>

<style scoped>
.aba-profissionais { display: flex; flex-direction: column; gap: 14px; }

/* Barra de filtros */
.filters-bar {
    display: flex; align-items: center; gap: 12px; flex-wrap: wrap;
}
.role-filter { min-width: 180px; max-width: 220px; }

/* Bulk actions */
.bulk-bar {
    display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
    background: hsl(var(--primary) / 0.06);
    border: 1px solid hsl(var(--primary) / 0.2);
    border-radius: 8px; padding: 10px 16px;
    font-size: 13px; color: hsl(var(--primary-dark));
}
.bulk-bar > span > b { color: hsl(var(--primary)); font-weight: 700; }

.btn-ghost-sm {
    display: inline-flex; align-items: center; gap: 6px;
    background: transparent; color: hsl(var(--primary-dark));
    border: 1px solid transparent;
    padding: 6px 10px; border-radius: 6px;
    font-family: inherit; font-size: 12px; font-weight: 600; cursor: pointer;
    transition: all 150ms;
}
.btn-ghost-sm:hover { background: white; border-color: hsl(var(--secondary) / 0.15); }
.btn-ghost-sm.danger { color: hsl(var(--error)); }
.btn-ghost-sm.danger:hover { background: hsl(var(--error) / 0.08); border-color: hsl(var(--error) / 0.2); }

/* Tabela */
.pros-table {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.pros-thead, .pros-row {
    display: grid;
    grid-template-columns: 36px 1.6fr 1fr 1.4fr 1fr 60px;
    gap: 16px;
    align-items: center;
    padding: 12px 18px;
    position: relative;
}
.pros-thead {
    background: hsl(var(--secondary) / 0.03);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
}
.pros-row {
    border-bottom: 1px solid hsl(var(--secondary) / 0.05);
    transition: background 150ms; cursor: pointer;
}
.pros-row:last-child { border-bottom: none; }
.pros-row:hover { background: hsl(var(--primary) / 0.025); }
.pros-row.selected { background: hsl(var(--primary) / 0.05); }

.pt-checkbox {
    display: inline-flex; align-items: center; cursor: pointer;
    position: relative;
}
.pt-checkbox input { position: absolute; opacity: 0; pointer-events: none; }
.cb-box {
    width: 18px; height: 18px; border-radius: 4px;
    border: 1.5px solid hsl(var(--secondary) / 0.3); background: white;
    display: inline-flex; align-items: center; justify-content: center;
    transition: all 150ms;
}
.pt-checkbox input:checked + .cb-box {
    background: hsl(var(--primary)); border-color: hsl(var(--primary));
}
.pt-checkbox input:checked + .cb-box::after {
    content: ''; width: 5px; height: 9px; border: solid white;
    border-width: 0 2px 2px 0; transform: rotate(45deg) translate(-1px, -1px);
}
.pt-checkbox:hover .cb-box { border-color: hsl(var(--primary)); }

.pr-name { display: flex; align-items: center; gap: 12px; min-width: 0; }
.pr-avatar {
    width: 38px; height: 38px; border-radius: 50%;
    color: white; display: flex; align-items: center; justify-content: center;
    font-weight: 700; font-size: 12px; flex-shrink: 0; position: relative;
}
.owner-crown {
    position: absolute; bottom: -3px; right: -3px;
    width: 16px; height: 16px; border-radius: 50%;
    background: hsl(45 96% 50%); color: white;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 8px; border: 2px solid white;
}
.pr-name-info { min-width: 0; }
.pr-name-info b { display: block; color: hsl(var(--primary-dark)); font-size: 14px; font-weight: 700; line-height: 1.2; }
.pr-spec { display: block; font-size: 11px; color: hsl(var(--secondary) / 0.6); margin-top: 2px; }

.pr-contact { min-width: 0; }
.ellip { font-size: 12px; color: hsl(var(--secondary)); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; display: block; }

.pr-actions { display: flex; gap: 2px; justify-content: flex-end; }
.btn-icon-sm {
    width: 32px; height: 32px; border-radius: 6px;
    background: transparent; border: 1px solid transparent;
    display: inline-flex; align-items: center; justify-content: center;
    cursor: pointer; color: hsl(var(--secondary) / 0.6); font-size: 13px;
    transition: all 150ms;
}
.btn-icon-sm:hover { background: white; border-color: hsl(var(--secondary) / 0.15); color: hsl(var(--primary-dark)); }

.self-tag {
    position: absolute; right: 60px; top: 50%; transform: translateY(-50%);
    font-size: 10px; font-weight: 700; padding: 2px 8px; border-radius: 999px;
    background: hsl(var(--info) / 0.12); color: hsl(199 80% 35%);
}

.muted { color: hsl(var(--secondary) / 0.45); font-size: 12px; }

@media (max-width: 1100px) {
    .pros-thead, .pros-row { grid-template-columns: 32px 1.6fr 1fr 1fr 60px; }
    .pros-thead > div:nth-child(4), .pros-row .pr-contact { display: none; }
}
</style>
