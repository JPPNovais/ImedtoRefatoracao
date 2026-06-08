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
 * Retorna true quando o valor de uma seção é considerado preenchido.
 * Espelha a lógica de `contarSecoesPreenchidas` em useEvolucaoResumo.ts.
 */
function preenchido(valor: unknown): boolean {
    if (valor === null || valor === undefined) return false
    if (typeof valor === "string") return valor.trim().length > 0
    if (Array.isArray(valor)) return valor.length > 0
    if (typeof valor === "object") {
        return Object.values(valor as Record<string, unknown>)
            .some(x => x !== null && x !== undefined && String(x).trim() !== "")
    }
    return true
}

/** Seções do snapshot com valor preenchido, na ordem original. */
const secoesPreenchidas = computed(() => {
    if (!props.evolucao) return []
    return props.evolucao.modeloSnapshot.filter(s =>
        preenchido(props.evolucao!.conteudo[s.chave]),
    )
})

/** Valor textual de uma seção para exibição. */
function valorTexto(evolucao: Evolucao, chave: string): string {
    const v = evolucao.conteudo[chave]
    if (typeof v === "string") return v.trim()
    if (Array.isArray(v)) return v.join(", ")
    if (typeof v === "object" && v !== null) return JSON.stringify(v, null, 2)
    return String(v ?? "")
}

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
                    <p class="edd-secao-conteudo">{{ valorTexto(evolucao, secao.chave) }}</p>
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
