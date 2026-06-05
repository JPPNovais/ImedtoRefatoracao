---
name: session-briefing-008-valores-profissional-2026-06-04
description: Briefing 2026-06-04_008 — aba valores profissional na configuração de orçamento; pipeline fechada, commit 8267b7f pushed
metadata:
  type: project
---

Pipeline fechada em 2026-06-04. Frontend-only (backend já existia).

Commit: 8267b7f — feat(orcamento): aba de valores profissional na configuração de orçamento

**Arquivos alterados:**
- `frontend/src/components/orcamento/config/ValoresProfissionalTab.vue` (novo, 429 linhas)
- `frontend/src/components/orcamento/config/ValoresProfissionalTab.test.ts` (novo, 9 testes)
- `frontend/src/views/orcamentos/OrcamentoSettingsView.vue` (modificado — +5/-2 linhas efetivas)
- `planejamentos/2026-06-04_008_aba-valores-profissional-na-configuracao-de-orcamento.md` (briefing)

**Totais pós-commit:** 445 frontend testes (9 novos). Build vue-tsc 0 erros.

**Decisões notáveis:**
- Dois drawers separados por v-if/v-else em `idEditando`: criação inclui `profissionalUsuarioIdStr` (str vazia → null no payload); edição usa `AtualizarValorProfissionalPayload` sem esse campo — fiel ao contrato do backend.
- `listarProfissionaisPublico()` exclusivo no seletor de profissional (LGPD). `listarProfissionais` (lista completa) não foi usada.
- CA9: teste cobre apenas função vazia; cenário de campo negativo tem lógica correta mas sem teste dedicado — não bloqueante.
- Lint `@typescript-eslint/recommended` é falha pré-existente (confirmado por git stash), não introduzida nesta entrega.
- `OrcamentoFormView.vue` não foi alterado; já consumia `listarValoresProfissional()` — CA12 passou por análise de código.

**Why:** Fechou lacuna que deixava o select "Tabela de valor" sempre vazio no formulário de orçamento. Pré-requisito operacional para honorário por tempo.

**How to apply:** Referência de padrão para próximas abas de configuração de orçamento (AnestesistasTab, PacotesTab já existem como modelos similares).
