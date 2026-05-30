<script setup lang="ts">
/**
 * RegiaoTreeView — renderiza recursivamente a árvore de regiões anatômicas.
 * Cada nó pode ser expandido/colapsado. Expõe ações de editar, inativar e excluir.
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
                <span v-if="no.lateralidade" class="badge-lat">bilateral</span>
                <AppBadge v-if="!no.ativo" variant="muted" label="Inativo" />

                <!-- Contagem de filhos -->
                <span v-if="no.filhos.length > 0" class="badge-filhos">{{ no.filhos.length }} sub</span>

                <!-- Ações -->
                <div class="no-acoes">
                    <button class="btn-icon btn-icon-editar" type="button" title="Editar" @click="emit('editar', no.id)">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button
                        class="btn-icon btn-icon-excluir"
                        type="button"
                        :title="no.filhos.length > 0 ? 'Possui sub-regiões — remova-as primeiro' : 'Excluir'"
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
    font-size: 0.875rem;
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
    font-size: 0.6875rem;
    flex-shrink: 0;
    border-radius: 3px;
}
.btn-toggle:hover { background: hsl(var(--muted)); color: hsl(var(--foreground)); }
.btn-toggle--vazio { cursor: default; }
.btn-toggle--vazio:hover { background: none; }

.no-codigo {
    font-family: monospace;
    font-size: 0.75rem;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.6);
    padding: 0.0625rem 0.3rem;
    border-radius: 3px;
    flex-shrink: 0;
}

.no-nome {
    font-weight: 500;
    color: hsl(var(--foreground));
    flex: 1;
    min-width: 0;
}

.badge-vista {
    font-size: 0.6875rem;
    padding: 0.0625rem 0.375rem;
    border-radius: 9999px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    text-transform: capitalize;
    flex-shrink: 0;
}

.badge-lat {
    font-size: 0.6875rem;
    padding: 0.0625rem 0.375rem;
    border-radius: 9999px;
    background: hsl(var(--warning, 40 95% 55%) / 0.15);
    color: hsl(var(--warning, 30 90% 40%));
    flex-shrink: 0;
}

.badge-filhos {
    font-size: 0.6875rem;
    color: hsl(var(--muted-foreground));
    flex-shrink: 0;
}

.no-acoes {
    display: flex;
    gap: 0.125rem;
    margin-left: auto;
    flex-shrink: 0;
}

.btn-icon:disabled {
    opacity: 0.35;
    cursor: not-allowed;
}
</style>
