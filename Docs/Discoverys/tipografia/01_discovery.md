# Discovery de tipografia — auditoria e escala canônica

> Briefing: `planejamentos/2026-06-08_003_padronizacao-tipografica-design-system.md`
> Data: 2026-06-08

## Problema diagnosticado

Antes desta entrega, a base de código tinha fragmentação tipográfica severa:

- **163 ocorrências** de `<h1>/<h2>/<h3>` espalhadas por ~64 arquivos — cada uma declarando `font-size` e `font-weight` em CSS scoped com valores diferentes.
- **8 arquivos** com a classe legada `.campo-label` (label de campo com font-size/weight inconsistente).
- **24+ arquivos** com classes de título scoped (`.secao-titulo`, `.card-titulo`, `.painel-titulo`, `.lista-titulo`, `.page-titulo`) com tamanhos diferentes por arquivo.
- **Sem fonte única de verdade** para a escala — cada dev escolhia um valor arbitrário.
- `PageHeader` do design system usava classes Tailwind hardcoded (`text-2xl font-bold`) que divergiam do mockup (deveria ser 30px/800).
- `Label.vue` e `Field.vue` usavam `text-sm font-medium` (14px/500) em vez de 12px/600.
- Inputs e botões em `.form-input`/`.btn-*` usavam 14px (`0.875rem`) em vez de 13px do mockup.

### Inventário de fragmentação (estado pré-entrega)

| Área | Tamanhos encontrados | Peso encontrado |
|------|---------------------|-----------------|
| Títulos de página (h1) | 18px / 20px / 24px / 26px / 28px / 1.4rem / 1.6rem / 2rem | 700 / 800 |
| Títulos de seção (h2/h3) | 13px / 14px / 15px / 16px / 17px / 18px / 20px / 22px / 0.9rem / 0.95rem / 1.05rem | 600 / 700 / 800 |
| Labels | 12px / 13px / 14px | 500 / 600 / 700 |
| Inputs/botões | 13px / 14px / 0.875rem | — |

## Escala canônica adotada

Decisões de produto tomadas e travadas com o usuário em 2026-06-08:

| Nível | Tamanho | Peso | Letter-spacing | Uso |
|-------|---------|------|----------------|-----|
| Página (Q4) | 30px (`--text-3xl`) | 800 | -0.015em | Título da página via `AppPageHeader` |
| Seção (Q1) | 21px (`--text-xl`) | 800 | -0.01em | `.ds-section-title` em h2/h3 |
| Card (Q1) | 15px (`--text-md`) | 700 | — | `.ds-card-title` em h2/h3 |
| Label (Q2) | 12px (`--text-xs`) | 600 | — | `AppField`/`AppLabel` do DS |
| Input/Botão (Q3) | 13px (`--text-sm`) | — | — | `.form-input`, `.btn-*` em `main.css` |
| Corpo | 14px (`--text-base`) | 400 | — | herda do `body` |

### Por que esses valores

- **30px título** (Q4): mockup de referência. Uniformiza telas que hoje têm 18–26px — delta intencional visível.
- **21px seção** (Q1): separa claramente "seção" de "card" sem chegar ao peso do título de página.
- **15px card** (Q1): menor que seção, ainda mais pesado que corpo — hierarquia clara.
- **12px/600 label** (Q2): mockup de referência. Labels mais compactos e com peso maior (semibold) leem melhor em layouts densos de formulário.
- **13px input/botão** (Q3): mockup de referência. Compacta formulários e botões sem perda de legibilidade.

## Estratégia de migração executada

1. **Tokens em `main.css` `:root`**: escala completa 10px → 30px + pesos + line-heights + letter-spacing.
2. **Tokens em `tokens.css` (DS)**: espelho para consumo dentro do design system.
3. **DS atualizado**: `PageHeader` → `var(--text-3xl)` + `var(--font-weight-extrabold)`; `Label.vue`/`Field.vue` → `.ds-label` com `var(--text-xs)/var(--font-weight-semibold)`.
4. **Utilitários globais**: `.ds-section-title`, `.ds-card-title`, `.ds-sub` em `main.css`.
5. **Migração CA5** (`.campo-label`): 8 arquivos migrados para `AppField`/`AppLabel` ou classe-token.
6. **Migração CA4** (`.secao-titulo`, `.card-titulo`, `.painel-titulo`, `.lista-titulo`, `.page-titulo`): 24+ arquivos migrados para `.ds-section-title`/`.ds-card-title`/`AppPageHeader`.
7. **Migração CA9** (h1/h2/h3 com CSS scoped): ~50+ selectors convertidos para `var(--text-*)` e `var(--font-weight-*)`.
8. **CA8** (inputs/botões 13px): aplicado centralmente em `.form-input` e todos `.btn-*` em `main.css`.
9. **Tailwind preset**: `fontSize` extension alinhada aos tokens (CA10).

## Riscos aceitos

- **Mudança visual intencional** (CA14): título de página 24px→30px, label 14px/500→12px/600, input/botão 14px→13px. Deltas uniformes — validação visual em prod é do usuário.
- **LandingView**: headings usam `clamp()` e `em` para responsividade de marketing — exceção justificada, não migrado para tokens fixos.
- **PDF inline styles**: `ReceitasPacienteTab.vue` tem template string de HTML para impressão com `h1{font-size:1.4rem}` — CSS vars não funcionam em contexto de documento PDF; deixado como está.
- **ProseMirror editor content**: h2/h3 dentro do editor TipTap usam `em` relativos — apropriado para rich text de conteúdo.

## Referência futura

Para adicionar novos níveis tipográficos ou alterar a escala, editar **exclusivamente**:
- `frontend/src/assets/main.css` (tokens + utilitários)
- `design-system/src/styles/tokens.css` (espelho DS)
- `design-system/src/tailwind/preset.js` (preset Tailwind)

Nunca adicionar `font-size` literal em CSS scoped de view ou componente. Ver `Docs/DESIGN.md §Escala tipográfica`.
