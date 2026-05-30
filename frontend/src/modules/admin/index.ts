/**
 * Módulo admin global — ponto de entrada.
 *
 * REGRAS DE ISOLAMENTO (CA47):
 * Este módulo NÃO pode importar de outros módulos do app principal:
 *   - ❌ @/views/*
 *   - ❌ @/stores/* (exceto via import dinâmico se absolutamente necessário)
 *   - ❌ @/services/* (usa adminApi interno)
 *   - ❌ @/composables/* (exceto useDebouncedRef e utilitários neutros)
 *
 * PERMITIDO importar:
 *   - ✅ @/components/ui/* (design system compartilhado)
 *   - ✅ @/composables/useDebouncedRef (utilitário neutro)
 *   - ✅ @/utils/* (formatadores neutros sem PII)
 *
 * Razão: preparar extração futura para projeto separado.
 * Ver: planejamentos/2026-05-30_001_admin-global-mvp.md §9
 */

export { adminRoutes } from "./router"
