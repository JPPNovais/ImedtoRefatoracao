<script setup lang="ts">
/**
 * RegiaoTreeView — renderiza recursivamente a árvore de regiões anatômicas.
 * Cada nó pode ser expandido/colapsado.
 * Ações: editar, inativar (primária, ativo), reativar (inativo), excluir (secundária).
 * B3 (2026-06-08_007): inativar é ação primária; reativar exposto para inativos; excluir secundário.
 */
import { ref } from "vue"
import { AppBadge } from "@/components/ui"
import type { RegiaoAnatomicaNoDto } from "../../services/catalogosService"

const props = defineProps<{
    nos: RegiaoAnatomicaNoDto[]
    nivel?: number
}>()

const emit = defineEmits<{
    editar: [id: number]
    inativar: [no: RegiaoAnatomicaNoDto]
    reativar: [no: RegiaoAnatomicaNoDto]
    excluir: [no: RegiaoAnatomicaNoDto]
}>()

const expandidos = ref<Set<number>>(new Set())

function alternarExpansao(id: number) {
    if (expandidos.value.has(id)) {
        expandidos.value.delete(id)
    } else {
        expandidos.value.add(id)
    }
}

const nivelAtual = props.nivel ?? 0
</script>

<template>
    <ul class="arvore-lista" :class="`arvore-nivel-${nivelAtual}`">
        <li v-for="no in nos" :key="no.id" class="arvore-item">
            <div class="arvore-no" :class="{ 'arvore-no--inativo': !no.ativo }">
                <!-- Toggle expand -->
                <button
                    v-if="no.filhos.length > 0"
                    type="button"
                    class="btn-toggle"
                    :aria-label="expandidos.has(no.id) ? 'Colapsar' : 'Expandir'"
                    @click="alternarExpansao(no.id)"
                >
                    <i :class="expandidos.has(no.id) ? 'fa-solid fa-chevron-down' : 'fa-solid fa-chevron-right'"></i>
                </button>
                <span v-else class="btn-toggle btn-toggle--vazio"></span>

                <!-- Código + Nome -->
                <span class="no-codigo">{{ no.codigo }}</span>
                <span class="no-nome">{{ no.nome }}</span>

                <!-- Info badges -->
                <span v-if="no.vista" class="badge-vista">{{ no.vista }}</span>
                <span v-if="no.lateralidade" class="badge-lat">Bilateral</span>
                <AppBadge v-if="!no.ativo" variant="muted" label="Inativo" />

                <!-- Contagem de filhos -->
                <span v-if="no.filhos.length > 0" class="badge-filhos">{{ no.filhos.length }} sub</span>

                <!-- Ações -->
                <div class="no-acoes">
                    <!-- Editar (sempre disponível) -->
                    <button
                        class="btn-icon btn-icon-editar"
                        type="button"
                        title="Editar"
                        @click="emit('editar', no.id)"
                    >
                        <i class="fa-solid fa-pen"></i>
                    </button>

                    <!-- Inativar (ação primária de remoção) — só para nós ativos -->
                    <button
                        v-if="no.ativo"
                        class="btn-icon btn-icon-inativar"
                        type="button"
                        title="Inativar"
                        @click="emit('inativar', no)"
                    >
                        <i class="fa-solid fa-ban"></i>
                    </button>

                    <!-- Reativar — só para nós inativos -->
                    <button
                        v-else
                        class="btn-icon btn-icon-reativar"
                        type="button"
                        title="Reativar"
                        @click="emit('reativar', no)"
                    >
                        <i class="fa-solid fa-rotate-left"></i>
                    </button>

                    <!-- Excluir permanentemente (secundário) — desabilitado quando tem filhos -->
                    <button
                        class="btn-icon btn-icon-excluir btn-icon-secundario"
                        type="button"
                        :title="no.filhos.length > 0 ? 'Possui sub-regiões — inative ou remova-as primeiro' : 'Excluir permanentemente'"
                        :disabled="no.filhos.length > 0"
                        @click="emit('excluir', no)"
                    >
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>

            <!-- Filhos recursivos -->
            <RegiaoTreeView
                v-if="no.filhos.length > 0 && expandidos.has(no.id)"
                :nos="no.filhos"
                :nivel="nivelAtual + 1"
                @editar="emit('editar', $event)"
                @inativar="emit('inativar', $event)"
                @reativar="emit('reativar', $event)"
                @excluir="emit('excluir', $event)"
            />
        </li>
    </ul>
</template>

<style scoped>
.arvore-lista {
    list-style: none;
    padding: 0;
    margin: 0;
}

.arvore-nivel-0 { margin-left: 0; }
.arvore-nivel-1 { margin-left: 1.5rem; border-left: 2px solid hsl(var(--border)); padding-left: 0.75rem; }
.arvore-nivel-2 { margin-left: 1.5rem; border-left: 2px solid hsl(var(--border) / 0.5); padding-left: 0.75rem; }

.arvore-item { margin: 0; }

.arvore-no {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.4375rem 0.625rem;
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    transition: background 0.1s;
}
.arvore-no:hover { background: hsl(var(--muted) / 0.5); }
.arvore-no--inativo { opacity: 0.55; }

.btn-toggle {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1.25rem;
    height: 1.25rem;
    background: none;
    border: none;
    cursor: pointer;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-xs);
    flex-shrink: 0;
    border-radius: 3px;
}
.btn-toggle:hover { background: hsl(var(--muted)); color: hsl(var(--foreground)); }
.btn-toggle--vazio { cursor: default; }
.btn-toggle--vazio:hover { background: none; }

.no-codigo {
    font-family: monospace;
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.6);
    padding: 0.0625rem 0.3rem;
    border-radius: 3px;
    flex-shrink: 0;
}

.no-nome {
    font-weight: var(--font-weight-medium);
    color: hsl(var(--foreground));
    flex: 1;
    min-width: 0;
}

.badge-vista {
    font-size: var(--text-xs);
    padding: 0.0625rem 0.375rem;
    border-radius: 9999px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    text-transform: capitalize;
    flex-shrink: 0;
}

.badge-lat {
    font-size: var(--text-xs);
    padding: 0.0625rem 0.375rem;
    border-radius: 9999px;
    background: hsl(var(--warning, 40 95% 55%) / 0.15);
    color: hsl(var(--warning, 30 90% 40%));
    flex-shrink: 0;
}

.badge-filhos {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    flex-shrink: 0;
}

.no-acoes {
    display: flex;
    gap: 0.125rem;
    margin-left: auto;
    flex-shrink: 0;
}

/* Inativar — ação primária de remoção (warning/laranja) */
.btn-icon-inativar {
    color: hsl(var(--warning, 30 90% 40%));
}
.btn-icon-inativar:hover {
    background: hsl(var(--warning, 40 95% 55%) / 0.15);
    color: hsl(var(--warning, 25 85% 35%));
}

/* Reativar — cor de sucesso */
.btn-icon-reativar {
    color: hsl(var(--success, 140 60% 35%));
}
.btn-icon-reativar:hover {
    background: hsl(var(--success, 140 60% 35%) / 0.1);
}

/* Excluir permanente — secundário (mais discreto via opacidade) */
.btn-icon-secundario {
    opacity: 0.55;
}
.btn-icon-secundario:not(:disabled):hover {
    opacity: 1;
}

.btn-icon:disabled {
    opacity: 0.3;
    cursor: not-allowed;
}
</style>
