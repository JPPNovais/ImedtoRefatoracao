<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { AppAvatar, AppButton, AppPermissionMatrix, AppPopover, AppStatusPill } from "@/components/ui"
import type { ModeloPermissao } from "@/services/permissaoService"
import type { ProfissionalVinculado } from "@/services/vinculoService"

/**
 * Aba "Papéis e permissões". Layout em grid de 2 colunas:
 *  - Coluna esquerda: lista de papéis (do sistema + customizados da clínica).
 *  - Coluna direita: detalhe do papel selecionado, com matriz de permissões.
 *
 * Regras de contagem:
 *  - R1: profissional com status "Dono" (modeloPermissaoId null) é contado no
 *    card do modelo padrão "Admin" (ehPadrao === true && nome === 'Admin').
 *    Não existe campo slug/chave estável no tipo ModeloPermissao — matching por
 *    nome literal com fallback seguro (R2: sem card Admin → dono não vaza).
 *  - R3: deduplicação por usuarioId — se o dono também tiver vínculo apontando
 *    para o modelo Admin, é contado uma vez só, com precedência do selo "Dono".
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

const padroes    = computed(() => props.modelos.filter(m => m.ehPadrao))
const customizados = computed(() => props.modelos.filter(m => !m.ehPadrao))

/**
 * Modelo padrão "Admin" — critério: ehPadrao === true && nome === 'Admin'.
 * Não existe campo de slug/chave no tipo, portanto usamos nome literal.
 * Se não houver, retorna null (R2: fallback seguro).
 */
const modeloAdmin = computed(() =>
    props.modelos.find(m => m.ehPadrao && m.nome === "Admin") ?? null,
)

/**
 * Profissionais de um modelo, deduplizados por usuarioId.
 * Para o modelo Admin: inclui o Dono (R1) e desemparelha duplicata de
 * registro de vínculo do mesmo usuário (R3).
 */
function profissionaisDoModelo(m: ModeloPermissao): ProfissionalVinculado[] {
    const vistosUserId = new Set<string>()
    const resultado: ProfissionalVinculado[] = []

    const ehModeloAdmin = modeloAdmin.value !== null && m.id === modeloAdmin.value.id

    if (ehModeloAdmin) {
        // Dono primeiro (precedência visual do selo — R3)
        const dono = props.profissionais.find(
            p => p.status === "Dono" && p.modeloPermissaoId === null,
        )
        if (dono) {
            vistosUserId.add(dono.usuarioId)
            resultado.push(dono)
        }
    }

    // Demais profissionais vinculados ao modelo (exceto Removidos)
    for (const p of props.profissionais) {
        if (p.modeloPermissaoId !== m.id) continue
        if (p.status === "Removido") continue
        if (vistosUserId.has(p.usuarioId)) continue  // dedup R3
        vistosUserId.add(p.usuarioId)
        resultado.push(p)
    }

    return resultado
}

function quantosUsam(m: ModeloPermissao): number {
    return profissionaisDoModelo(m).length
}

// --- Chips de status (mesma lógica de AbaProfissionais para consistência visual) ---

function statusVariante(s: string): "success" | "warning" | "error" | "muted" {
    if (s === "Ativo" || s === "Dono") return "success"
    if (s === "Convidado")             return "warning"
    return "muted"
}

function statusLabel(s: string): string {
    if (s === "Dono")      return "Dono"
    if (s === "Ativo")     return "Ativo"
    if (s === "Inativo")   return "Inativo"
    if (s === "Convidado") return "Convidado"
    return s
}

// --- Utilitários visuais ---

function bgIcone(cor?: string | null): string {
    const c = cor ?? "hsl(0 0% 45%)"
    return `color-mix(in srgb, ${c} 14%, white)`
}

function corIcone(cor?: string | null): string {
    return cor ?? "hsl(0 0% 45%)"
}

function labelProfissionais(n: number): string {
    return `${n} profissio${n === 1 ? "nal" : "nais"}`
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
                            <AppPopover v-if="quantosUsam(r) > 0" posicao="bottom-start">
                                <template #gatilho="{ toggle }">
                                    <button
                                        type="button"
                                        class="contador-clicavel"
                                        :aria-label="`Ver profissionais do papel ${r.nome}`"
                                        @click.stop="toggle"
                                    >{{ labelProfissionais(quantosUsam(r)) }}</button>
                                </template>
                                <template #conteudo>
                                    <div class="pop-lista">
                                        <div class="pop-cabecalho">{{ r.nome }} — {{ labelProfissionais(quantosUsam(r)) }}</div>
                                        <div class="pop-itens">
                                            <div
                                                v-for="p in profissionaisDoModelo(r)"
                                                :key="p.usuarioId"
                                                class="pop-item"
                                            >
                                                <AppAvatar :nome="p.nomeCompleto" :foto-url="p.fotoUrl" tamanho="sm" decorativo />
                                                <span class="pop-nome">{{ p.nomeCompleto }}</span>
                                                <span v-if="p.status === 'Dono'" class="pop-selo-dono" title="Dono da clínica">
                                                    <i class="fa-solid fa-crown"></i> Dono
                                                </span>
                                                <AppStatusPill
                                                    v-else
                                                    :label="statusLabel(p.status)"
                                                    :variante="statusVariante(p.status)"
                                                />
                                            </div>
                                        </div>
                                    </div>
                                </template>
                            </AppPopover>
                            <span v-else class="ri-count-zero">{{ labelProfissionais(0) }}</span>
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
                            <AppPopover v-if="quantosUsam(r) > 0" posicao="bottom-start">
                                <template #gatilho="{ toggle }">
                                    <button
                                        type="button"
                                        class="contador-clicavel"
                                        :aria-label="`Ver profissionais do papel ${r.nome}`"
                                        @click.stop="toggle"
                                    >{{ labelProfissionais(quantosUsam(r)) }}</button>
                                </template>
                                <template #conteudo>
                                    <div class="pop-lista">
                                        <div class="pop-cabecalho">{{ r.nome }} — {{ labelProfissionais(quantosUsam(r)) }}</div>
                                        <div class="pop-itens">
                                            <div
                                                v-for="p in profissionaisDoModelo(r)"
                                                :key="p.usuarioId"
                                                class="pop-item"
                                            >
                                                <AppAvatar :nome="p.nomeCompleto" :foto-url="p.fotoUrl" tamanho="sm" decorativo />
                                                <span class="pop-nome">{{ p.nomeCompleto }}</span>
                                                <AppStatusPill
                                                    :label="statusLabel(p.status)"
                                                    :variante="statusVariante(p.status)"
                                                />
                                            </div>
                                        </div>
                                    </div>
                                </template>
                            </AppPopover>
                            <span v-else class="ri-count-zero">{{ labelProfissionais(0) }}</span>
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
                        <AppPopover v-if="quantosUsam(selecionado) > 0" posicao="bottom-end">
                            <template #gatilho="{ toggle }">
                                <button
                                    type="button"
                                    class="rd-badge rd-badge--clicavel"
                                    :aria-label="`Ver profissionais do papel ${selecionado.nome}`"
                                    @click="toggle"
                                >
                                    <i class="fa-solid fa-users"></i>
                                    {{ labelProfissionais(quantosUsam(selecionado)) }}
                                </button>
                            </template>
                            <template #conteudo>
                                <div class="pop-lista">
                                    <div class="pop-cabecalho">{{ selecionado.nome }} — {{ labelProfissionais(quantosUsam(selecionado)) }}</div>
                                    <div class="pop-itens">
                                        <div
                                            v-for="p in profissionaisDoModelo(selecionado)"
                                            :key="p.usuarioId"
                                            class="pop-item"
                                        >
                                            <AppAvatar :nome="p.nomeCompleto" :foto-url="p.fotoUrl" tamanho="sm" decorativo />
                                            <span class="pop-nome">{{ p.nomeCompleto }}</span>
                                            <span v-if="p.status === 'Dono'" class="pop-selo-dono" title="Dono da clínica">
                                                <i class="fa-solid fa-crown"></i> Dono
                                            </span>
                                            <AppStatusPill
                                                v-else
                                                :label="statusLabel(p.status)"
                                                :variante="statusVariante(p.status)"
                                            />
                                        </div>
                                    </div>
                                </div>
                            </template>
                        </AppPopover>
                        <span v-else class="rd-badge">
                            <i class="fa-solid fa-users"></i>
                            {{ labelProfissionais(0) }}
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

/* Contador clicável (quando n > 0) */
.contador-clicavel {
    background: none;
    border: none;
    padding: 0;
    font-family: inherit;
    font-size: 11px;
    color: hsl(var(--primary));
    cursor: pointer;
    text-decoration: underline;
    text-decoration-style: dotted;
    text-underline-offset: 2px;
}
.contador-clicavel:hover { text-decoration-style: solid; }

/* Contador não-clicável (quando n = 0) */
.ri-count-zero {
    font-size: 11px;
    color: hsl(var(--secondary) / 0.6);
}

.ri-arrow { color: hsl(var(--secondary) / 0.3); font-size: 11px; }
.role-item.active .ri-arrow { color: hsl(var(--primary)); }

.rl-add-btn {
    width: 100%; padding: 10px; background: transparent;
    border: 1px dashed hsl(var(--secondary) / 0.2); border-radius: 8px;
    color: hsl(var(--primary)); font-family: inherit; font-size: 12px; font-weight: 600;
    cursor: pointer; margin-top: 4px; transition: all 150ms;
}
.rl-add-btn:hover { border-color: hsl(var(--primary) / 0.4); background: hsl(var(--primary) / 0.04); }

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
.rd-title h2 { font-size: var(--text-2xl); color: hsl(var(--primary-dark)); margin: 0 0 2px; font-weight: var(--font-weight-bold); }
.rd-title p { font-size: 13px; color: hsl(var(--secondary) / 0.7); margin: 0; }

.rd-actions { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.rd-badge {
    display: inline-flex; align-items: center; gap: 6px;
    background: hsl(var(--secondary) / 0.06);
    padding: 6px 12px; border-radius: 999px;
    font-size: 12px; font-weight: 600; color: hsl(var(--secondary) / 0.7);
}
/* Badge clicável no painel de detalhe */
.rd-badge--clicavel {
    border: none;
    font-family: inherit;
    cursor: pointer;
    transition: background 150ms;
}
.rd-badge--clicavel:hover {
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
}
.rd-system {
    display: inline-flex; align-items: center; gap: 6px;
    font-size: 12px; color: hsl(var(--secondary) / 0.55); font-style: italic;
}

/* Popover — conteúdo interno (estilos do painel flutuante ficam no AppPopover) */
.pop-lista {
    padding: 12px;
}
.pop-cabecalho {
    font-size: 11px;
    font-weight: 700;
    color: hsl(var(--secondary) / 0.6);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    padding-bottom: 8px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    margin-bottom: 8px;
}
.pop-itens {
    display: flex;
    flex-direction: column;
    gap: 6px;
    /* ~6 itens de 40px + gaps → max ~276px; acima disso scroll interno */
    max-height: 276px;
    overflow-y: auto;
}
.pop-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 4px 2px;
}
.pop-nome {
    flex: 1;
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--primary-dark));
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
/* Selo "Dono" — visualmente distinto (ouro/coroa) */
.pop-selo-dono {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 3px 8px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 700;
    background: hsl(43 90% 55% / 0.15);
    color: hsl(43 80% 35%);
    white-space: nowrap;
    flex-shrink: 0;
}
.pop-selo-dono i { font-size: 10px; }

@media (max-width: 1100px) {
    .roles-grid { grid-template-columns: 1fr; }
}
</style>
