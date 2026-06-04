---
name: apppilltoggle-design-system
description: AppPillToggle é o toggle segmentado do design system (corridos/úteis, tipos de variável), mas NÃO está documentado em Docs/DESIGN.md — pendência de backlog.
metadata:
  type: project
---

`AppPillToggle` (`frontend/src/components/ui/AppPillToggle.vue`) é o componente de
botões segmentados / toggle group do design system. API:
`<AppPillToggle v-model="x" :opcoes="[{ valor, label, icone? }]" />`. Já usado em
`ListasVariaveisTab.vue` (tipos de variável de prontuário) e no briefing
[[2026-06-04_005]] (tipo de prazo de fornecedor corridos|úteis). É o componente correto
para qualquer escolha binária/segmentada — não usar AppSelect nem par de AppButton para isso.

**Why:** evita o anti-padrão de criar componente novo ou improvisar com AppButton quando
já existe o segmentado canônico.

**How to apply:** ao especificar UI com escolha entre 2-4 opções mutuamente exclusivas,
indicar reuso de `AppPillToggle` direto no briefing.

**Pendência de backlog (não-óbvia):** `AppPillToggle` NÃO consta em `Docs/DESIGN.md` na
seção de componentes (verificado 2026-06-04). Documentá-lo num ciclo futuro — mas não
inflar uma demanda de feature com essa doc (Surgical Changes). Quando surgir demanda que
toque o design system de verdade, aproveitar para registrar o componente.
