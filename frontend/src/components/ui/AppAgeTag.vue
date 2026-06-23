<script setup lang="ts">
/**
 * AppAgeTag — Tag etária automática (briefing 2026-06-23_002).
 * Derivada da data de nascimento; NUNCA persiste no array `tags`.
 * Coexiste com as tags manuais do catálogo pacienteTags.ts.
 *
 * Faixas:
 *  "idoso"  → paciente com 60+ anos (ícone fa-person-cane, tom âmbar)
 *  "menor"  → paciente com < 18 anos (ícone fa-child-reaching, tom violeta)
 *  null     → adulto 18-59 ou sem data → não renderiza nada
 */
import type { FaixaEtaria } from "@/utils/idade"

defineProps<{
    faixa: FaixaEtaria
}>()
</script>

<template>
    <span
        v-if="faixa"
        class="age-tag"
        :class="`age-tag--${faixa}`"
    >
        <i
            class="fa-solid"
            :class="faixa === 'idoso' ? 'fa-person-cane' : 'fa-child-reaching'"
        ></i>
        {{ faixa === "idoso" ? "Idoso" : "Menor de idade" }}
    </span>
</template>

<style scoped>
.age-tag {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 2px 8px;
    border-radius: 999px;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    white-space: nowrap;
    line-height: 1.6;
}

.age-tag i {
    font-size: var(--text-2xs);
}

/* Idoso — tom âmbar */
.age-tag--idoso {
    background: hsl(38 90% 50% / 0.12);
    color: hsl(32 80% 30%);
    border: 1px solid hsl(38 90% 50% / 0.25);
}

/* Menor de idade — tom violeta */
.age-tag--menor {
    background: hsl(260 60% 55% / 0.12);
    color: hsl(260 60% 30%);
    border: 1px solid hsl(260 60% 55% / 0.25);
}
</style>
