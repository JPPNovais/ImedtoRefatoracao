---
name: qa-posdeploy-2d94290-2026-05-20
description: QA do fix do mapa corporal (SVG pointer-events + emit atualizar nos cards). Validação estática feita, browser MCP indisponível.
metadata:
  type: project
---

Commit `2d94290` ("fix(exame-fisico): corrigir mapa corporal — pointer-events SVG + propagação de edição nos cards") em produção desde 2026-05-20 20:31:23 UTC.

**Why:** Bug reportado: clique em path SVG do BodyMap não disparava handler (Safari/Firefox em `visiblePainted` deixavam a `<image>` opaca capturar antes) e edições em texto_exame/achados/observacoes não persistiam (RegionExamCard mutava prop diretamente). Fix tem 3 partes:
1. `BodyMap.vue`: `pointer-events="none"` no `<image>` e `pointer-events: all` no `.region-hotspot`.
2. `RegionExamCard.vue`: removeu `v-model` direto na prop, agora emite `atualizar` com `{ index, patch: Partial<RegiaoExaminada> }`.
3. `SecaoExameFisico.vue`: handler `atualizarRegiao` aplica patch imutável (`[...regioes]` + spread) e melhora `:key` do v-for para `${regiao_id}-${lateralidade}-${idx}`.

**How to apply:**
- Validação estática completa: Vitest 8/8 verde (4 BodyMap + 4 RegionExamCard); bundle `BodyMap-d5832593.js` contém `pointer-events":"none"` no JS minificado; `ProntuarioView-fa524707.js` contém `"atualizar"` (2×), `texto_exame` (3×), `achados` (6×). Confirmado por curl ao S3/nginx via Caddy.
- **Cenários funcionais em browser NÃO executados** — sessão sem MCP de Chrome/Playwright. Os 3 cenários (happy path, múltiplas regiões, edit+troca-região) precisam ser rodados manualmente ou em próxima sessão com `chrome-devtools` carregado. Como o próprio fix endereça hit-testing real do navegador, Vitest não substitui.
- Débitos não bloqueantes observados:
  - **Duplicação de tipo**: `RegiaoExaminada` (RegionExamCard) e `RegiaoAnatomicaSelecionada` (SecaoExameFisico) são estruturalmente idênticos. Mover para `frontend/src/types/exameFisico.ts`.
  - **Erro pré-existente** em `ProntuarioPacienteHeader.vue:95` (`Property 'focus' does not exist`) introduzido pelo commit `e263532` de 2026-05-12. Não relacionado, mas `npm run build` falha — investigar se CI tolera ou se há flag diferente.

Aplicar [[feedback-qa-login-via-signup-e-rds-confirm]] na próxima sessão para conseguir testar a UI logada com fluxo de signup + UPDATE no RDS.
