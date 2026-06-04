---
name: project-visibilidade-profissionais-papel
description: Briefing 2026-06-04_007 implementado — AppPopover no design system + contagem do Dono no Admin + contadores clicáveis em AbaPapeis.vue
metadata:
  type: project
---

Briefing 2026-06-04_007 "Visibilidade dos profissionais por papel" implementado em 2026-06-04.

Mudança 100% frontend (CA13): nenhum toque em backend/migrations/contratos.

Arquivos tocados:
- `frontend/src/components/ui/AppPopover.vue` — novo componente do design system
- `frontend/src/components/ui/index.ts` — exportação do AppPopover
- `frontend/src/components/equipe/AbaPapeis.vue` — reescrito com novas features
- `frontend/src/components/equipe/AbaPapeis.test.ts` — 19 testes novos (CA1-CA10 + invariantes)
- `Docs/DESIGN.md` — seção "Componentes de exibição contextual" com AppPopover

Decisões técnicas registradas:
- Matching do modelo Admin: `ehPadrao === true && nome === 'Admin'` (não existe campo slug/chave no tipo `ModeloPermissao`)
- Dedup (R3): por `usuarioId: string` (campo estável no tipo `ProfissionalVinculado`)
- AppPopover: Teleport para body + posicionamento via getBoundingClientRect + clamp viewport 8px

Status: build verde, 434 testes verdes (19 novos), aguardando QA.

**Why:** AppPopover é genérico (slot gatilho + slot conteúdo) e pode ser reutilizado em outras telas que precisem de painéis flutuantes ancorados.
**How to apply:** Antes de criar qualquer popover/tooltip inline, verificar se AppPopover atende. API: posicao + offset + slots #gatilho e #conteudo.
