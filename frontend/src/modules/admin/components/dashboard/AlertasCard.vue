<script setup lang="ts">
/**
 * AlertasCard — trials expirando e estabelecimentos sem assinatura.
 * W6-CA7, CA8, CA9.
 * Links via router-link para /admin/estabelecimentos/{id}.
 * AppEmptyState quando listas vazias.
 */
import { RouterLink } from "vue-router"
import { AppCard, AppEmptyState, AppBadge } from "@/components/ui"
import { useDashboardStore } from "../../stores/dashboardStore"

const store = useDashboardStore()

function formatarData(iso: string): string {
    return new Date(iso).toLocaleDateString("pt-BR")
}

function labelDias(dias: number): string {
    if (dias <= 0) return "Expira hoje"
    return `${dias} dia(s)`
}
</script>

<template>
    <AppCard>
        <h3 class="bloco-titulo">Alertas acionáveis</h3>

        <p v-if="store.carregandoAlertas" class="estado-info" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </p>

        <p v-else-if="store.erroAlertas" class="bloco-erro" role="alert">
            {{ store.erroAlertas }}
        </p>

        <template v-else>
            <!-- Trials expirando -->
            <div class="alerta-secao">
                <h4 class="alerta-subtitulo">
                    Trials expirando nos próximos 7 dias
                    <AppBadge
                        v-if="store.alertas && store.alertas.trialsExpirando.length > 0"
                        :label="String(store.alertas.trialsExpirando.length)"
                        variant="warning"
                    />
                </h4>

                <AppEmptyState
                    v-if="!store.alertas?.trialsExpirando.length"
                    titulo="Nenhum trial expirando nos próximos 7 dias."
                />

                <ul v-else class="alerta-lista">
                    <li
                        v-for="item in store.alertas.trialsExpirando"
                        :key="item.estabelecimentoId"
                        class="alerta-item"
                    >
                        <div class="alerta-item-info">
                            <RouterLink
                                :to="`/admin/estabelecimentos/${item.estabelecimentoId}`"
                                class="alerta-link"
                            >
                                {{ item.nomeFantasia }}
                            </RouterLink>
                            <span class="alerta-dono">{{ item.donoNome }}</span>
                        </div>
                        <div class="alerta-item-meta">
                            <span class="alerta-data">até {{ formatarData(item.fimEm) }}</span>
                            <AppBadge :label="labelDias(item.diasRestantes)" variant="warning" />
                        </div>
                    </li>
                </ul>
            </div>

            <!-- Sem assinatura vigente -->
            <div class="alerta-secao">
                <h4 class="alerta-subtitulo">
                    Sem assinatura vigente
                    <AppBadge
                        v-if="store.alertas && store.alertas.semAssinaturaTotal > 0"
                        :label="String(store.alertas.semAssinaturaTotal)"
                        variant="error"
                    />
                </h4>

                <AppEmptyState
                    v-if="!store.alertas?.semAssinatura.length"
                    titulo="Todos os estabelecimentos ativos têm assinatura vigente."
                />

                <template v-else>
                    <p
                        v-if="store.alertas && store.alertas.semAssinaturaTotal > store.alertas.semAssinatura.length"
                        class="alerta-total"
                    >
                        {{ store.alertas.semAssinaturaTotal }} estabelecimentos sem assinatura — mostrando {{ store.alertas.semAssinatura.length }}
                    </p>
                    <ul class="alerta-lista">
                        <li
                            v-for="item in store.alertas!.semAssinatura"
                            :key="item.estabelecimentoId"
                            class="alerta-item"
                        >
                            <div class="alerta-item-info">
                                <RouterLink
                                    :to="`/admin/estabelecimentos/${item.estabelecimentoId}`"
                                    class="alerta-link"
                                >
                                    {{ item.nomeFantasia }}
                                </RouterLink>
                                <span class="alerta-dono">{{ item.donoNome }}</span>
                            </div>
                            <span class="alerta-data">desde {{ formatarData(item.criadoEm) }}</span>
                        </li>
                    </ul>
                </template>
            </div>
        </template>
    </AppCard>
</template>

<style scoped>
.bloco-titulo {
    font-size: 0.875rem;
    font-weight: 600;
    color: hsl(var(--foreground));
    margin: 0 0 1rem;
}

.estado-info {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.bloco-erro {
    color: hsl(var(--error));
    font-size: 0.875rem;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    background: hsl(var(--error) / 0.08);
}

.alerta-secao {
    margin-bottom: 1.5rem;
}

.alerta-secao:last-child {
    margin-bottom: 0;
}

.alerta-subtitulo {
    font-size: 0.8rem;
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin: 0 0 0.75rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.alerta-lista {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.alerta-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 0.5rem 0.75rem;
    border-radius: 8px;
    background: hsl(var(--muted) / 0.3);
}

.alerta-item-info {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
    min-width: 0;
}

.alerta-link {
    font-size: 0.875rem;
    font-weight: 500;
    color: hsl(var(--primary));
    text-decoration: none;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.alerta-link:hover {
    text-decoration: underline;
}

.alerta-dono {
    font-size: 0.75rem;
    color: hsl(var(--muted-foreground));
}

.alerta-item-meta {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-shrink: 0;
}

.alerta-data {
    font-size: 0.75rem;
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
}

.alerta-total {
    font-size: 0.8rem;
    color: hsl(var(--muted-foreground));
    margin: 0 0 0.5rem;
}
</style>
