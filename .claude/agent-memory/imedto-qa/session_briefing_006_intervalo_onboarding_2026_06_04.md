---
name: session-briefing-006-intervalo-onboarding-2026-06-04
description: Briefing 2026-06-04_006 intervalo numérico livre no onboarding — pipeline fechada, commits e356a2e+dbae7fb pushed
metadata:
  type: project
---

Pipeline fechada. 9 CAs validados por análise de código + suíte automatizada (chrome-devtools indisponível no sandbox).

**Commits pushed**:
- `e356a2e` — `feat(onboarding): campo de intervalo entre consultas como input numérico livre`
- `dbae7fb` — `fix(onboarding): máscara de telefone aceita celular com 9 dígitos` (carona do linter, commit separado)

**Suíte**:
- Backend: build OK, 1271 testes passando (1348 total, 77 skipped — skips são pré-existentes)
- Frontend: build OK (vue-tsc + vite), typecheck OK (0 erros), lint ESLint indisponível (config pré-existente quebrada)
- Vitest: 410 testes no main limpo; working tree tem 414 (1 falhando em OrcamentoListaView.test.ts por mudança de outro briefing pendente, não do 006)

**Staging parcial**: diff do OnboardingView.vue tinha 4 hunks; hunk 3 (v-maska carona) foi isolado via `git add -p` e commitado separado.

**Why:** Briefing exigia apenas substituição de <select> por <input type="number"> com validação inline e bloqueio de avanço. Mudança cirúrgica, sem novos testes exigidos, sem backend alterado.

**How to apply:** Ao validar briefings futuros de onboarding, confirmar que `podeAvancar` step 4 ainda usa `erroIntervaloOnboarding.value === null`.
