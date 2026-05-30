# Módulo Admin Global

Área administrativa do Imedto. Autocontida — preparada para extração futura como projeto separado.

## Regras de isolamento

Este módulo NÃO importa de outros módulos do app principal:

- `@/views/*` — views do app cliente
- `@/stores/*` — stores de tenant/profissional/auth do usuário comum
- `@/services/*` — usa `adminApi.ts` interno (axios isolado)

**Permitido importar de fora:**
- `@/components/ui/*` — design system compartilhado
- `@/composables/useDebouncedRef` — utilitário neutro
- `@/utils/*` — formatadores neutros (sem PII de paciente)

## Estrutura

```
modules/admin/
  index.ts              # ponto de entrada; documenta regras de isolamento
  router/
    index.ts            # rotas /admin/* + guard
  stores/
    adminAuthStore.ts   # auth do admin global (BFF pattern)
  services/
    adminApi.ts         # cliente HTTP isolado (baseURL /api/admin)
  views/
    AdminLogin.vue      # tela de login
    AdminLayout.vue     # shell com banner + sidebar + header
    AdminDashboard.vue  # placeholder — devs de feature preenchem
    AdminChangePassword.vue  # troca de senha obrigatória
  composables/
    (futuros composables admin)
  components/
    (futuros componentes admin)
```

## Roadmap de extração (checklist)

- [ ] Confirmar zero import cruzado (grep por `from '@/views\|@/stores\|@/services'`)
- [ ] Mover `adminApi.ts` para usar baseURL absoluta (remover dependência do proxy Vite)
- [ ] Criar `vite.config.admin.ts` separado
- [ ] Mover para repositório separado com próprio deploy

## Referência

Briefing: `planejamentos/2026-05-30_001_admin-global-mvp.md`
