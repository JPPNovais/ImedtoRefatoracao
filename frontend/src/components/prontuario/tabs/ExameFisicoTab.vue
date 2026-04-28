<script setup lang="ts">
import { AppButton } from "@/components/ui"
import SecaoProntuario from "@/components/prontuario/SecaoProntuario.vue"

const props = defineProps<{
    novaEvolucao: Record<string, any>
    salvando: boolean
}>()

const emit = defineEmits<{
    salvar: []
}>()

function scrollTo(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" })
}
</script>

<template>
    <div class="grid-layout">
        <!-- Sidebar de navegação -->
        <div class="sidebar-wrap">
            <div class="nav-card">
                <div class="nav-header">Navegação</div>
                <div class="nav-links">
                    <button type="button" class="nav-link" @click="scrollTo('exame-topo')">
                        <i class="fa-solid fa-arrow-up nav-icon" /> Voltar ao topo
                    </button>
                    <button type="button" class="nav-link" @click="scrollTo('exame-sinais')">
                        <i class="fa-solid fa-heart-pulse nav-icon" /> Sinais vitais
                    </button>
                    <button type="button" class="nav-link" @click="scrollTo('exame-antropometria')">
                        <i class="fa-solid fa-weight-scale nav-icon" /> Antropometria
                    </button>
                    <button type="button" class="nav-link" @click="scrollTo('exame-mapa')">
                        <i class="fa-solid fa-person nav-icon" /> Mapa corporal
                    </button>
                </div>
            </div>
        </div>

        <!-- Conteúdo principal -->
        <div class="conteudo-principal">
            <!-- Cabeçalho -->
            <div id="exame-topo" class="exame-header scroll-mt">
                <h3 class="exame-titulo">
                    <i class="fa-solid fa-stethoscope" />
                    Exame físico completo
                </h3>
                <AppButton
                    size="sm"
                    icon="fa-solid fa-save"
                    :loading="salvando"
                    :disabled="salvando"
                    @click="emit('salvar')"
                >
                    {{ salvando ? "Salvando..." : "Salvar exame" }}
                </AppButton>
            </div>

            <p class="exame-sub">
                Sinais vitais, antropometria, ectoscopia e mapa corporal interativo.
                Estes dados são salvos junto com a evolução.
            </p>

            <!-- Componente de exame físico estruturado -->
            <div class="exame-card">
                <SecaoProntuario
                    v-model="novaEvolucao['exame-fisico']"
                    chave="exame-fisico"
                    titulo="Exame físico"
                    tipo="estruturado"
                />
            </div>

            <!-- Rodapé -->
            <div class="acoes-rodape">
                <AppButton
                    type="button"
                    size="lg"
                    icon="fa-solid fa-save"
                    :loading="salvando"
                    :disabled="salvando"
                    @click="emit('salvar')"
                >
                    {{ salvando ? "Salvando..." : "Salvar exame físico" }}
                </AppButton>
            </div>
        </div>
    </div>
</template>

<style scoped>
.grid-layout {
    display: grid;
    grid-template-columns: 200px 1fr;
    gap: 1.25rem;
    align-items: start;
}
@media (max-width: 900px) {
    .grid-layout { grid-template-columns: 1fr; }
    .sidebar-wrap { display: none; }
}

/* Sidebar */
.sidebar-wrap {
    position: sticky;
    top: 1rem;
}
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
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: none;
    border-bottom: 1px solid var(--border);
    background: none;
    cursor: pointer;
    font-family: inherit;
    font-size: 0.78em;
    color: var(--text-muted);
    text-align: left;
    transition: background 0.12s, color 0.12s;
}
.nav-link:last-child { border-bottom: none; }
.nav-link:hover { background: var(--bg-hover); color: var(--text); }
.nav-icon {
    font-size: 0.65em;
    color: hsl(var(--primary) / 0.6);
    flex-shrink: 0;
    width: 0.75rem;
}

/* Conteúdo */
.conteudo-principal { display: flex; flex-direction: column; gap: 0.9rem; }

.exame-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
    scroll-margin-top: 1rem;
}
.exame-titulo {
    font-size: 0.95em;
    font-weight: 700;
    margin: 0;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text);
}
.exame-titulo i { color: hsl(var(--primary)); font-size: 0.85em; }

.exame-sub {
    font-size: 0.82em;
    color: var(--text-muted);
    margin: -0.5rem 0 0;
    line-height: 1.4;
    background: #fffbeb;
    border-left: 3px solid #fbbf24;
    padding: 0.45rem 0.75rem;
    border-radius: 4px;
}

.exame-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem 1.25rem;
}

.acoes-rodape {
    display: flex;
    justify-content: flex-end;
}
</style>
