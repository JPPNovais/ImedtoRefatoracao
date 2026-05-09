<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { AppButton, AppPermissionMatrix } from "@/components/ui"
import type { ModeloPermissao } from "@/services/permissaoService"
import type { ProfissionalVinculado } from "@/services/vinculoService"

/**
 * Aba "Papéis e permissões". Layout em grid de 2 colunas:
 *  - Coluna esquerda: lista de papéis (do sistema + customizados da clínica).
 *  - Coluna direita: detalhe do papel selecionado, com matriz de permissões.
 */
const props = defineProps<{
    modelos: ModeloPermissao[]
    profissionais: ProfissionalVinculado[]
}>()

const emit = defineEmits<{
    (e: "criar-papel"): void
    (e: "editar-papel", m: ModeloPermissao): void
}>()

const selecionadoId = ref<number | null>(null)

watch(() => props.modelos, (lista) => {
    if (selecionadoId.value && !lista.find(m => m.id === selecionadoId.value)) {
        selecionadoId.value = lista[0]?.id ?? null
    } else if (!selecionadoId.value) {
        selecionadoId.value = lista[0]?.id ?? null
    }
}, { immediate: true })

const selecionado = computed(() => props.modelos.find(m => m.id === selecionadoId.value) ?? null)

const padroes = computed(() => props.modelos.filter(m => m.ehPadrao))
const customizados = computed(() => props.modelos.filter(m => !m.ehPadrao))

function quantosUsam(m: ModeloPermissao): number {
    return props.profissionais.filter(p => p.modeloPermissaoId === m.id && p.status !== "Removido").length
}

function bgIcone(cor?: string | null): string {
    const c = cor ?? "hsl(0 0% 45%)"
    return `color-mix(in srgb, ${c} 14%, white)`
}

function corIcone(cor?: string | null): string {
    return cor ?? "hsl(0 0% 45%)"
}
</script>

<template>
    <div class="aba-papeis">
        <div class="roles-grid">
            <!-- Coluna esquerda: lista -->
            <div class="roles-list">
                <div class="rl-section">
                    <div class="rl-head">
                        <div>
                            <h4>Permissões do sistema</h4>
                            <span>Disponíveis em todas as clínicas Imedto</span>
                        </div>
                    </div>
                    <button
                        v-for="r in padroes" :key="r.id"
                        type="button"
                        class="role-item"
                        :class="{ active: selecionadoId === r.id }"
                        @click="selecionadoId = r.id"
                    >
                        <div class="ri-icon" :style="{ background: bgIcone(r.cor), color: corIcone(r.cor) }">
                            <i class="fa-solid" :class="r.icone || 'fa-shield-halved'"></i>
                        </div>
                        <div class="ri-info">
                            <b>{{ r.nome }}</b>
                            <span>{{ quantosUsam(r) }} profissio{{ quantosUsam(r) === 1 ? 'nal' : 'nais' }}</span>
                        </div>
                        <i class="fa-solid fa-chevron-right ri-arrow"></i>
                    </button>
                </div>

                <div class="rl-section">
                    <div class="rl-head">
                        <div>
                            <h4>Permissões personalizadas</h4>
                            <span>Customizadas — só esta clínica vê</span>
                        </div>
                        <button type="button" class="btn-icon-sm" title="Criar nova permissão" @click="emit('criar-papel')">
                            <i class="fa-solid fa-plus"></i>
                        </button>
                    </div>
                    <div v-if="customizados.length === 0" class="rl-empty">
                        <i class="fa-solid fa-shield-halved"></i>
                        <span>Nenhuma permissão customizada ainda</span>
                        <button type="button" class="btn-text-sm" @click="emit('criar-papel')">+ Criar primeira permissão</button>
                    </div>
                    <button
                        v-for="r in customizados" :key="r.id"
                        type="button"
                        class="role-item"
                        :class="{ active: selecionadoId === r.id }"
                        @click="selecionadoId = r.id"
                    >
                        <div class="ri-icon" :style="{ background: bgIcone(r.cor), color: corIcone(r.cor) }">
                            <i class="fa-solid" :class="r.icone || 'fa-user-tag'"></i>
                        </div>
                        <div class="ri-info">
                            <b>{{ r.nome }}</b>
                            <span>{{ quantosUsam(r) }} profissio{{ quantosUsam(r) === 1 ? 'nal' : 'nais' }}</span>
                        </div>
                        <i class="fa-solid fa-chevron-right ri-arrow"></i>
                    </button>
                    <button v-if="customizados.length > 0" type="button" class="rl-add-btn" @click="emit('criar-papel')">
                        <i class="fa-solid fa-plus"></i> Criar nova permissão
                    </button>
                </div>
            </div>

            <!-- Coluna direita: detalhe -->
            <div v-if="selecionado" class="role-detail">
                <div class="rd-head">
                    <div class="rd-title">
                        <div class="rd-icon" :style="{ background: bgIcone(selecionado.cor), color: corIcone(selecionado.cor) }">
                            <i class="fa-solid" :class="selecionado.icone || 'fa-shield-halved'"></i>
                        </div>
                        <div>
                            <h2>{{ selecionado.nome }}</h2>
                            <p>{{ selecionado.descricao || "Sem descrição." }}</p>
                        </div>
                    </div>
                    <div class="rd-actions">
                        <span class="rd-badge">
                            <i class="fa-solid fa-users"></i>
                            {{ quantosUsam(selecionado) }} profissio{{ quantosUsam(selecionado) === 1 ? 'nal' : 'nais' }}
                        </span>
                        <AppButton
                            v-if="!selecionado.ehPadrao"
                            variant="secondary"
                            icon="fa-solid fa-pen-to-square"
                            @click="emit('editar-papel', selecionado!)"
                        >
                            Editar permissão
                        </AppButton>
                        <span v-else class="rd-system">
                            <i class="fa-solid fa-lock"></i> Permissão do sistema — não editável
                        </span>
                    </div>
                </div>

                <AppPermissionMatrix :model-value="selecionado.permissoes" read-only />
            </div>
        </div>
    </div>
</template>

<style scoped>
.aba-papeis {}

.roles-grid {
    display: grid;
    grid-template-columns: 320px 1fr;
    gap: 22px;
    align-items: flex-start;
}

.roles-list { display: flex; flex-direction: column; gap: 18px; }

.rl-section {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    padding: 14px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.rl-head {
    display: flex; align-items: flex-start; justify-content: space-between; gap: 8px;
    padding: 4px 6px 12px;
}
.rl-head h4 { font-size: 13px; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0 0 2px; }
.rl-head span { font-size: 11px; color: hsl(var(--secondary) / 0.6); }

.rl-empty {
    text-align: center; padding: 24px 16px;
    display: flex; flex-direction: column; align-items: center; gap: 8px;
}
.rl-empty i { font-size: 22px; color: hsl(var(--secondary) / 0.25); }
.rl-empty span { font-size: 12px; color: hsl(var(--secondary) / 0.6); }

.role-item {
    display: flex; align-items: center; gap: 12px; width: 100%;
    background: transparent; border: 1px solid transparent;
    padding: 10px; border-radius: 8px;
    cursor: pointer; text-align: left; transition: all 150ms;
    font-family: inherit;
}
.role-item:hover { background: hsl(var(--secondary) / 0.04); }
.role-item.active { background: hsl(var(--primary) / 0.08); border-color: hsl(var(--primary) / 0.25); }

.ri-icon {
    width: 36px; height: 36px; border-radius: 8px;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 14px; flex-shrink: 0;
}
.ri-info { flex: 1; min-width: 0; }
.ri-info b { display: block; color: hsl(var(--primary-dark)); font-size: 13px; font-weight: 700; }
.ri-info span { font-size: 11px; color: hsl(var(--secondary) / 0.6); }
.ri-arrow { color: hsl(var(--secondary) / 0.3); font-size: 11px; }
.role-item.active .ri-arrow { color: hsl(var(--primary)); }

.rl-add-btn {
    width: 100%; padding: 10px; background: transparent;
    border: 1px dashed hsl(var(--secondary) / 0.2); border-radius: 8px;
    color: hsl(var(--primary)); font-family: inherit; font-size: 12px; font-weight: 600;
    cursor: pointer; margin-top: 4px; transition: all 150ms;
}
.rl-add-btn:hover { border-color: hsl(var(--primary) / 0.4); background: hsl(var(--primary) / 0.04); }

.btn-icon-sm {
    width: 32px; height: 32px; border-radius: 6px;
    background: transparent; border: 1px solid transparent;
    display: inline-flex; align-items: center; justify-content: center;
    cursor: pointer; color: hsl(var(--secondary) / 0.6); font-size: 13px;
    transition: all 150ms;
}
.btn-icon-sm:hover { background: white; border-color: hsl(var(--secondary) / 0.15); color: hsl(var(--primary-dark)); }
.btn-text-sm {
    background: none; border: none; color: hsl(var(--primary));
    font-family: inherit; font-size: 12px; font-weight: 600; cursor: pointer; padding: 4px 0;
}
.btn-text-sm:hover { text-decoration: underline; }

/* Detalhe */
.role-detail {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    padding: 24px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.rd-head {
    display: flex; justify-content: space-between; align-items: flex-start; gap: 16px;
    padding-bottom: 18px; border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    margin-bottom: 18px;
}
.rd-title { display: flex; gap: 14px; align-items: center; flex: 1; min-width: 0; }
.rd-icon {
    width: 52px; height: 52px; border-radius: 12px;
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 22px; flex-shrink: 0;
}
.rd-title h2 { font-size: 20px; color: hsl(var(--primary-dark)); margin: 0 0 2px; font-weight: 700; }
.rd-title p { font-size: 13px; color: hsl(var(--secondary) / 0.7); margin: 0; }

.rd-actions { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.rd-badge {
    display: inline-flex; align-items: center; gap: 6px;
    background: hsl(var(--secondary) / 0.06);
    padding: 6px 12px; border-radius: 999px;
    font-size: 12px; font-weight: 600; color: hsl(var(--secondary) / 0.7);
}
.rd-system {
    display: inline-flex; align-items: center; gap: 6px;
    font-size: 12px; color: hsl(var(--secondary) / 0.55); font-style: italic;
}

@media (max-width: 1100px) {
    .roles-grid { grid-template-columns: 1fr; }
}
</style>
