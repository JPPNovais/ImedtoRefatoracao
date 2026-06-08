# Addendum — Padronização tipográfica: regra permanente no CLAUDE.md

**Refere-se a**: `2026-06-08_003_padronizacao-tipografica-design-system.md`
**Status**: Aprovado por usuário em 2026-06-08
**Motivo**: O usuário pediu que o padrão tipográfico vire **premissa permanente** do projeto — qualquer nova implementação deve seguir a escala, para não sair do padrão que estamos estabelecendo. O briefing original não previa a atualização do `CLAUDE.md` (só `Docs/DESIGN.md`). Este addendum acrescenta isso ao escopo. O briefing original permanece imutável.

## Escopo adicional

Acrescentar ao `CLAUDE.md` (arquivo de instruções do projeto, raiz) uma **premissa não-negociável de tipografia**, cirúrgica, na seção de premissas/design, que torne obrigatório para todo código novo:

- Usar a **escala de tokens** (`--text-*`, `--font-weight-*`, `--line-height-*`, `--tracking-*`) — **nunca** `font-size`/`font-weight` hardcoded em CSS scoped ou inline.
- Título de página **sempre** via `PageHeader` (não criar `<h1>` próprio).
- Título de seção/card via `.ds-section-title` / `.ds-card-title`.
- Label de campo via `AppLabel`/`AppField` (canônico 12px/600).
- Inputs/botões herdam o token de texto do design system (13px) — não redefinir.
- Apontar `Docs/DESIGN.md §Escala tipográfica` como **fonte de verdade**; antes de criar qualquer título/label/texto, conferir lá.
- Coerente com a premissa já existente "Design system primeiro".

O texto deve ser conciso (3-6 linhas), no tom das demais premissas não-negociáveis do `CLAUDE.md`, e referenciar este briefing (2026-06-08_003).

## Critério de aceite adicional

- **CA15 — Regra permanente no CLAUDE.md.** Dado o `CLAUDE.md`, Quando revisado após a entrega, Então contém a premissa não-negociável de tipografia (usar tokens + `PageHeader`/`.ds-section-title`/`.ds-card-title`/`AppLabel`; proibido `font-size`/`font-weight` hardcoded em código novo), referenciando `Docs/DESIGN.md §Escala tipográfica` e o briefing 2026-06-08_003. A adição é cirúrgica (não reescreve o `CLAUDE.md`).

Todos os demais CAs e decisões do briefing original (CA1–CA14, Q1–Q4) permanecem válidos e inalterados.
