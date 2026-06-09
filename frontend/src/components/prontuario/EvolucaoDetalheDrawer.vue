<!--
    Drawer lateral de leitura de uma evolução do prontuário.
    Exibe somente as seções preenchidas do modeloSnapshot + conteúdo correspondente.
    Seções vazias (null / "" / whitespace / [] / {}) são omitidas.
    Somente leitura — sem botão Editar.
    Sem audit LGPD ao abrir (decisão consciente — briefing 2026-05-25_001, seção 3, item 4).
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton, AppDrawer, AppEmptyState } from "@/components/ui"
import { formatarSecaoLegivel } from "@/composables/useEvolucaoResumo"
import type { Evolucao } from "@/services/prontuarioService"

const props = defineProps<{
    evolucao: Evolucao | null
    aberto: boolean
}>()

const emit = defineEmits<{ fechar: [] }>()

function fmtData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", { dateStyle: "long", timeStyle: "short" })
}

/**
 * Seções do snapshot que produzem texto legível, na ordem original.
 * A decisão de exibir e o texto exibido derivam de `formatarSecaoLegivel`
 * (fonte única, mesma do PDF — briefing 2026-06-09_008, R7/R9): string vazia
 * = seção sem conteúdo legível, omitida.
 */
const secoesPreenchidas = computed(() => {
    if (!props.evolucao) return []
    return props.evolucao.modeloSnapshot
        .map(secao => ({
            chave: secao.chave,
            titulo: secao.titulo,
            texto: formatarSecaoLegivel(secao.chave, props.evolucao!.conteudo[secao.chave]),
        }))
        .filter(s => s.texto.length > 0)
})

const titulo = computed(() => {
    if (!props.evolucao) return "Evolução"
    return `Evolução de ${fmtData(props.evolucao.criadaEm)}`
})
</script>

<template>
    <AppDrawer
        :aberto="aberto"
        :titulo="titulo"
        :largura="560"
        @fechar="emit('fechar')"
    >
        <template v-if="evolucao">
            <!-- Cabeçalho da evolução -->
            <div class="edd-header">
                <div class="edd-meta-row">
                    <i class="fa-solid fa-user-doctor edd-icon"></i>
                    <span class="edd-prof">{{ evolucao.autorNome || "—" }}</span>
                </div>
                <div class="edd-meta-row">
                    <i class="fa-solid fa-file-medical edd-icon"></i>
                    <span class="edd-modelo">{{ evolucao.modeloNome || "Evolução" }}</span>
                </div>
            </div>

            <hr class="edd-divider" />

            <!-- Seções preenchidas -->
            <template v-if="secoesPreenchidas.length > 0">
                <section
                    v-for="secao in secoesPreenchidas"
                    :key="secao.chave"
                    class="edd-secao"
                    :data-test="`secao-${secao.chave}`"
                >
                    <h3 class="ds-section-title">{{ secao.titulo }}</h3>
                    <p class="edd-secao-conteudo">{{ secao.texto }}</p>
                </section>
            </template>

            <!-- Estado vazio: todas as seções estão em branco -->
            <AppEmptyState
                v-else
                icone="fa-solid fa-file-circle-xmark"
                titulo="Nenhuma seção preenchida"
                descricao="Esta evolução não tem seções preenchidas."
                :compacto="true"
                data-test="empty-state"
            />
        </template>

        <template #rodape>
            <AppButton
                variant="ghost"
                data-test="btn-fechar"
                @click="emit('fechar')"
            >
                Fechar
            </AppButton>
        </template>
    </AppDrawer>
</template>

<style scoped>
.edd-header {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding-bottom: 4px;
}

.edd-meta-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.edd-icon {
    font-size: 12px;
    color: hsl(var(--primary));
    width: 16px;
    flex-shrink: 0;
}

.edd-prof {
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
}

.edd-modelo {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.7);
}

.edd-divider {
    border: none;
    border-top: 1px solid hsl(var(--secondary) / 0.1);
    margin: 0;
}

.edd-secao {
    display: flex;
    flex-direction: column;
    gap: 6px;
}


.edd-secao-conteudo {
    margin: 0;
    font-size: 14px;
    line-height: 1.65;
    color: hsl(var(--secondary) / 0.9);
    white-space: pre-wrap;
}
</style>
