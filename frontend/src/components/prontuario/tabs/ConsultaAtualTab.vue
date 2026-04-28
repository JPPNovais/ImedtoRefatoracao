<script setup lang="ts">
import { AppButton, AppSelect } from "@/components/ui"
import SecaoProntuario from "@/components/prontuario/SecaoProntuario.vue"
import type { ModeloProntuario, SecaoModelo } from "@/services/prontuarioService"

const props = defineProps<{
    modeloId: number | null
    modelos: ModeloProntuario[]
    secoes: SecaoModelo[]
    novaEvolucao: Record<string, any>
    salvando: boolean
}>()

const emit = defineEmits<{
    salvar: []
    "update:modeloId": [id: number]
}>()

function scrollToSecao(chave: string) {
    document.getElementById(`secao-${chave}`)?.scrollIntoView({ behavior: "smooth", block: "start" })
}

function scrollToTop() {
    window.scrollTo({ top: 0, behavior: "smooth" })
}
</script>

<template>
    <div class="consulta-wrap">
        <!-- Selector de modelo para esta consulta -->
        <div class="bar-modelo">
            <span class="bar-label">Modelo:</span>
            <AppSelect
                :model-value="modeloId"
                class="select-modelo"
                @update:model-value="emit('update:modeloId', Number($event))"
            >
                <option v-for="m in modelos" :key="m.id" :value="m.id">
                    {{ m.nome }}{{ m.ehPadraoSistema ? " (sistema)" : "" }}
                </option>
            </AppSelect>
        </div>

        <!-- Layout 2 colunas (legado: sidebar + main) -->
        <div class="grid-layout">
            <!-- Coluna lateral — navegação sticky -->
            <div class="sidebar-wrap">
                <div class="nav-card">
                    <div class="nav-header">Navegação</div>
                    <div class="nav-links">
                        <button type="button" class="nav-link" @click="scrollToTop">
                            <i class="fa-solid fa-arrow-up nav-icon" />
                            Voltar ao topo
                        </button>
                        <button
                            v-for="secao in secoes"
                            :key="secao.chave"
                            type="button"
                            class="nav-link"
                            @click="scrollToSecao(secao.chave)"
                        >
                            <i class="fa-solid fa-chevron-right nav-icon" />
                            {{ secao.titulo }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Coluna principal — seções -->
            <div class="secoes-lista">
                <div
                    v-for="secao in secoes"
                    :key="secao.chave"
                    :id="`secao-${secao.chave}`"
                    class="secao-card scroll-mt"
                >
                    <div class="secao-header">
                        <h3 class="secao-titulo">{{ secao.titulo }}</h3>
                    </div>

                    <SecaoProntuario
                        v-model="novaEvolucao[secao.chave]"
                        :chave="secao.chave"
                        :titulo="secao.titulo"
                        :tipo="secao.tipo"
                    />
                </div>

                <!-- Rodapé com botão salvar -->
                <div class="acoes-rodape">
                    <AppButton
                        type="button"
                        size="lg"
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        :disabled="salvando"
                        @click="emit('salvar')"
                    >
                        {{ salvando ? "Salvando..." : "Salvar evolução" }}
                    </AppButton>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.consulta-wrap {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

/* Barra do modelo */
.bar-modelo {
    background: hsl(var(--primary-light));
    color: hsl(var(--primary-dark));
    padding: 0.45rem 0.9rem;
    border-radius: var(--radius);
    font-size: 0.85em;
    display: flex;
    align-items: center;
    gap: 0.6rem;
}
.bar-label {
    color: var(--text-muted);
    font-weight: 500;
    white-space: nowrap;
    flex-shrink: 0;
}
.select-modelo {
    max-width: 260px;
}

/* Layout 2 colunas */
.grid-layout {
    display: grid;
    grid-template-columns: 240px 1fr;
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
}
.nav-links {
    display: flex;
    flex-direction: column;
    border-top: 1px solid var(--border);
}
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

/* Seções principais */
.secoes-lista {
    display: flex;
    flex-direction: column;
    gap: 0.9rem;
}
.secao-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem 1.25rem;
    scroll-margin-top: 1rem;
}
.secao-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.6rem;
}
.secao-titulo {
    font-size: 0.88em;
    font-weight: 700;
    margin: 0;
    color: hsl(var(--primary));
}

/* Rodapé */
.acoes-rodape {
    display: flex;
    justify-content: flex-end;
    padding-top: 0.25rem;
}
</style>
