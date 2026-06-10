<script setup lang="ts">
import { computed } from "vue"
import { STATUS, type EstadoSistema } from "@/content/status"
import AppStatusPill from "@/components/ui/AppStatusPill.vue"

// R4: mapeamento completo do conjunto fechado de estados
const ESTADO_CONFIG: Record<
    EstadoSistema,
    { label: string; descricao: string; icone: string; variante: "success" | "warning" | "error" }
> = {
    operacional: {
        label: "Todos os sistemas operacionais",
        descricao: "A plataforma está funcionando normalmente.",
        icone: "fa-solid fa-circle-check",
        variante: "success",
    },
    instabilidade: {
        label: "Estamos com instabilidade",
        descricao: "Identificamos um problema e estamos trabalhando para resolver.",
        icone: "fa-solid fa-triangle-exclamation",
        variante: "warning",
    },
    manutenção: {
        label: "Em manutenção programada",
        descricao: "O sistema está temporariamente indisponível para manutenção.",
        icone: "fa-solid fa-screwdriver-wrench",
        variante: "error",
    },
}

const config = computed(() => ESTADO_CONFIG[STATUS.estado])

function formatarData(iso: string): string {
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
            <h1>Status do sistema</h1>

            <!-- Cartão de estado principal (CA7) -->
            <div class="estado-card" :class="`estado-${STATUS.estado}`" role="status" :aria-label="config.label">
                <i :class="[config.icone, 'estado-icone']" aria-hidden="true"></i>
                <div class="estado-texto">
                    <AppStatusPill :label="config.label" :variante="config.variante" />
                    <p v-if="STATUS.texto" class="estado-detalhe">{{ STATUS.texto }}</p>
                    <p class="estado-descricao">{{ config.descricao }}</p>
                </div>
            </div>

            <!-- Última atualização -->
            <p class="ultima-atualizacao">
                Última atualização:
                <time :datetime="STATUS.atualizadoEm">{{ formatarData(STATUS.atualizadoEm) }}</time>
            </p>

            <!-- Nota de evolução futura (alinha expectativa, não promete SLA) -->
            <p class="nota-futura">
                Em breve: monitoramento de disponibilidade em tempo real.
            </p>
        </article>

        <footer class="legal-footer">
            <router-link :to="{ name: 'Landing' }">← Voltar ao início</router-link>
            <router-link :to="{ name: 'Novidades' }">Novidades →</router-link>
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
    margin: 0 0 1.5rem;
    letter-spacing: -0.4px;
    color: hsl(var(--foreground));
}

/* Cartão de estado */
.estado-card {
    display: flex;
    align-items: flex-start;
    gap: 1.25rem;
    padding: 1.5rem;
    border-radius: var(--radius-lg, 12px);
    border: 1.5px solid;
    margin-bottom: 1.25rem;
}

.estado-operacional {
    background: hsl(var(--success) / 0.07);
    border-color: hsl(var(--success) / 0.35);
    color: hsl(160 79% 28%);
}

.estado-instabilidade {
    background: hsl(var(--warning) / 0.1);
    border-color: hsl(var(--warning) / 0.45);
    color: hsl(40 95% 30%);
}

.estado-manutenção {
    background: hsl(var(--info) / 0.08);
    border-color: hsl(var(--info) / 0.4);
    color: hsl(199 89% 30%);
}

.estado-icone {
    font-size: var(--text-3xl);
    flex-shrink: 0;
    margin-top: 0.1rem;
}

.estado-operacional .estado-icone { color: hsl(var(--success)); }
.estado-instabilidade .estado-icone { color: hsl(var(--warning)); }
.estado-manutenção .estado-icone { color: hsl(var(--info)); }

.estado-texto {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.estado-detalhe {
    font-size: var(--text-base);
    font-weight: var(--font-weight-semibold);
    margin: 0;
}

.estado-descricao {
    font-size: var(--text-sm);
    margin: 0;
    opacity: 0.85;
}

/* Última atualização */
.ultima-atualizacao {
    font-size: var(--text-sm);
    color: var(--text-muted);
    margin: 0 0 2rem;
}

/* Nota futura */
.nota-futura {
    font-size: var(--text-sm);
    color: var(--text-faint);
    margin: 0;
    font-style: italic;
    border-top: 1px solid var(--border);
    padding-top: 1.5rem;
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
@media (max-width: 480px) {
    .estado-card {
        flex-direction: column;
        gap: 1rem;
    }
}
</style>
