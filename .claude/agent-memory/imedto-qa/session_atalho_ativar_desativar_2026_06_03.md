---
name: session-atalho-ativar-desativar-2026-06-03
description: QA do briefing 2026-06-03_003 (atalho pause/play na linha de profissionais) — devolvido Tipo A por dead code em ReativarVinculoCommandHandler
metadata:
  type: project
---

Briefing `planejamentos/2026-06-03_003_atalho-ativar-desativar-profissional-na-linha.md`.

Build backend: verde (0 erros, 7 warnings pré-existentes CA1001/CA1869).
Testes unitários: 1221 passaram, 77 skipped (integração Postgres). 11 testes novos (InativarVinculoCommandHandlerTests + ReativarVinculoCommandHandlerTests).
Build frontend: verde. Typecheck: verde. Lint: erro pré-existente de config ESLint (@typescript-eslint/recommended). Vitest: 384 testes passaram.

**Bug Tipo A devolvido**: `ReativarVinculoCommandHandler.cs` injeta e armazena `IEstabelecimentoRepository _estabelecimentoRepo` no construtor mas nunca chama o campo. A mudança do dev substituiu `estab.DonoUsuarioId` por `_permissoes.UsuarioTemPermissaoExtra` (que trata Dono como pass-through), mas não removeu o campo/_param do construtor. Violação de CLAUDE.md Surgical Changes.

Fix esperado: remover `_estabelecimentoRepo` field/param/using do handler e remover `_estabRepo` mock do `ReativarVinculoCommandHandlerTests.cs` onde não é usado.

**Why:** É código morto introduzido pela própria mudança — violação clara de Surgical Changes do CLAUDE.md.
**How to apply:** Após fix do dev, reiniciar pipeline da Etapa 1.
