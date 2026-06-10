<script setup lang="ts">
import { computed } from "vue"
import { CHANGELOG, type EntradaChangelog, type TagChangelog } from "@/content/changelog"
import AppEmptyState from "@/components/ui/AppEmptyState.vue"
import AppBadge from "@/components/ui/AppBadge.vue"

// R7: ordenação mais recente primeiro (data ISO, comparação lexicográfica é suficiente)
const entradas = computed<EntradaChangelog[]>(() =>
    [...CHANGELOG].sort((a, b) => (a.data < b.data ? 1 : a.data > b.data ? -1 : 0))
)

// R5: conjunto fechado de tags — mapeado para variante e rótulo visual
const TAG_CONFIG: Record<TagChangelog, { label: string; variante: "success" | "info" | "error" }> = {
    novidade: { label: "Novidade",  variante: "success" },
    melhoria: { label: "Melhoria",  variante: "info"    },
    correção: { label: "Correção",  variante: "error"   },
}

function formatarData(iso: string): string {
    // "2026-06-09" → "9 de junho de 2026"
    const [ano, mes, dia] = iso.split("-").map(Number)
    return new Date(ano, mes - 1, dia).toLocaleDateString("pt-BR", {
        day: "numeric",
        month: "long",
        year: "numeric",
    })
}
</script>

<template>
    <div class="legal">
        <header class="legal-header">
            <router-link :to="{ name: 'Landing' }" class="voltar">← Voltar</router-link>
            <span class="logo">Imedto</span>
        </header>

        <article class="conteudo">
            <h1>Novidades</h1>
            <p class="subtitulo">Tudo o que melhoramos no Imedto, em ordem cronológica.</p>

            <AppEmptyState
                v-if="entradas.length === 0"
                icone="fa-solid fa-sparkles"
                titulo="Em breve, novidades por aqui."
                descricao="Ainda não há entradas publicadas."
            />

            <ul v-else class="lista-changelog" role="list">
                <li v-for="entrada in entradas" :key="entrada.data + entrada.titulo" class="entrada">
                    <div class="entrada-meta">
                        <AppBadge :variant="TAG_CONFIG[entrada.tag].variante" :label="TAG_CONFIG[entrada.tag].label" />
                        <time :datetime="entrada.data" class="data">{{ formatarData(entrada.data) }}</time>
                    </div>
                    <h3 class="ds-card-title entrada-titulo">{{ entrada.titulo }}</h3>
                    <p class="entrada-desc">{{ entrada.descricao }}</p>
                </li>
            </ul>
        </article>

        <footer class="legal-footer">
            <router-link :to="{ name: 'Landing' }">← Voltar ao início</router-link>
            <router-link :to="{ name: 'Status' }">Status do sistema →</router-link>
        </footer>
    </div>
</template>

<style scoped>
.legal {
    max-width: 780px;
    margin: 0 auto;
    padding: 0 1rem 4rem;
    background: hsl(var(--card));
    min-height: 100vh;
}

.legal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 0;
    border-bottom: 1px solid var(--border);
    margin-bottom: 2.5rem;
}

.voltar {
    font-size: var(--text-sm);
    color: var(--text-muted);
    text-decoration: none;
}

.voltar:hover {
    color: hsl(var(--primary));
}

.logo {
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--primary));
}

.conteudo h1 {
    font-size: var(--text-3xl);
    font-weight: var(--font-weight-extrabold);
    margin: 0 0 0.4rem;
    letter-spacing: -0.4px;
    color: hsl(var(--foreground));
}

.subtitulo {
    font-size: var(--text-base);
    color: var(--text-faint);
    margin: 0 0 2.5rem;
}

/* Lista de entradas */
.lista-changelog {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

.entrada {
    border-left: 3px solid hsl(var(--border));
    padding-left: 1.25rem;
}

.entrada-meta {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.4rem;
    flex-wrap: wrap;
}

.data {
    font-size: var(--text-sm);
    color: var(--text-muted);
}

.entrada-titulo {
    margin: 0 0 0.35rem;
    color: hsl(var(--foreground));
}

.entrada-desc {
    font-size: var(--text-base);
    line-height: 1.65;
    color: var(--text-muted);
    margin: 0;
}

/* Footer */
.legal-footer {
    display: flex;
    justify-content: space-between;
    padding-top: 2rem;
    margin-top: 3rem;
    border-top: 1px solid var(--border);
    font-size: var(--text-sm);
}

.legal-footer a {
    color: var(--text-muted);
    text-decoration: none;
}

.legal-footer a:hover {
    color: hsl(var(--primary));
}

/* Mobile */
@media (max-width: 600px) {
    .entrada-meta {
        flex-direction: column;
        align-items: flex-start;
    }
}
</style>
