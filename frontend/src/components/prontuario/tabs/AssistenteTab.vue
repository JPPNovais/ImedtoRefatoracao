<script setup lang="ts">
import { AppButton } from "@/components/ui"

const props = defineProps<{
    resumoIa: string
    gerandoResumo: boolean
    novaEvolucao: Record<string, any>
}>()

const emit = defineEmits<{
    gerarResumo: []
}>()

function temContexto(): boolean {
    return Object.values(props.novaEvolucao).some(v => {
        if (typeof v === "string") return v.trim().length > 0
        if (typeof v === "object" && v !== null) return Object.values(v).some(Boolean)
        return false
    })
}
</script>

<template>
    <div class="assistente-wrap">
        <!-- Painel IA -->
        <div class="assistente-panel">
            <!-- Cabeçalho -->
            <div class="panel-header">
                <div class="panel-titulo">
                    <i class="fa-solid fa-robot panel-icon" />
                    <span>Assistente clínico</span>
                    <span class="badge-ia">IA</span>
                </div>
            </div>

            <!-- Aviso IA -->
            <div class="aviso-ia">
                <i class="fa-solid fa-triangle-exclamation aviso-icon" />
                <p class="aviso-texto">
                    Conteúdo gerado por IA. <strong>Revise antes de salvar.</strong>
                </p>
            </div>

            <!-- Corpo -->
            <div class="panel-corpo">
                <p class="descricao">
                    Gera um resumo clínico das seções preenchidas na Consulta atual
                    e aponta possíveis insights para auxiliar na conduta médica.
                </p>

                <div v-if="!temContexto()" class="aviso-sem-contexto">
                    <i class="fa-solid fa-circle-info" />
                    Preencha ao menos uma seção na aba <strong>Consulta atual</strong>
                    para que o assistente possa gerar um resumo.
                </div>

                <AppButton
                    icon="fa-solid fa-wand-magic-sparkles"
                    :loading="gerandoResumo"
                    :disabled="gerandoResumo || !temContexto()"
                    @click="emit('gerarResumo')"
                >
                    {{ gerandoResumo ? "Gerando resumo..." : "Gerar resumo clínico" }}
                </AppButton>

                <!-- Resultado -->
                <div
                    v-if="resumoIa"
                    class="resumo-ia"
                    :class="{ gerando: gerandoResumo }"
                >
                    {{ resumoIa }}
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.assistente-wrap { max-width: 760px; }

.assistente-panel {
    background: hsl(var(--primary-light));
    border: 1px solid hsl(var(--primary) / 0.15);
    border-radius: var(--radius);
    overflow: hidden;
}

/* Cabeçalho */
.panel-header {
    padding: 0.75rem 1.25rem;
    border-bottom: 1px solid hsl(var(--primary) / 0.12);
}
.panel-titulo {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 700;
    font-size: 0.9em;
    color: hsl(var(--primary-dark));
}
.panel-icon { color: hsl(var(--primary)); }
.badge-ia {
    font-size: 0.7em;
    font-weight: 700;
    padding: 0.15rem 0.5rem;
    border-radius: 99px;
    background: hsl(220 90% 56% / 0.12);
    color: hsl(220 90% 56%);
}

/* Aviso */
.aviso-ia {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    padding: 0.5rem 1.25rem;
    background: hsl(45 90% 60% / 0.1);
    border-bottom: 1px solid hsl(45 90% 60% / 0.2);
}
.aviso-icon { color: hsl(45 90% 40%); font-size: 0.8em; margin-top: 0.1rem; }
.aviso-texto { font-size: 0.8em; color: hsl(45 60% 30%); margin: 0; line-height: 1.4; }

/* Corpo */
.panel-corpo {
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.descricao {
    font-size: 0.85em;
    color: var(--text-muted);
    margin: 0;
    line-height: 1.5;
}

.aviso-sem-contexto {
    font-size: 0.82em;
    color: var(--text-muted);
    display: flex;
    align-items: flex-start;
    gap: 0.4rem;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.65rem 0.9rem;
}
.aviso-sem-contexto i { color: hsl(var(--primary) / 0.6); margin-top: 0.1rem; flex-shrink: 0; }

/* Resultado */
.resumo-ia {
    padding: 1rem 1.25rem;
    background: var(--bg-card);
    border: 1px solid hsl(var(--primary) / 0.2);
    border-radius: var(--radius);
    white-space: pre-wrap;
    font-size: 0.88em;
    line-height: 1.6;
    color: hsl(var(--primary-dark));
}
.resumo-ia.gerando { opacity: 0.65; }
</style>
