---
name: feedback-zero-hex-admin
description: Todo CSS scoped do módulo admin deve usar tokens HSL — zero hex codes literais
metadata:
  type: feedback
---

Zero hex codes em CSS scoped no módulo admin (`frontend/src/modules/admin/`).

**Why:** R15 do briefing W2 — paridade visual com o app principal; tokens HSL garantem suporte a dark/light mode via CSS custom properties do design system.

**How to apply:** Sempre usar `hsl(var(--token))` em vez de `#rrggbb`. Tokens disponíveis: `--foreground`, `--background`, `--card`, `--border`, `--muted`, `--muted-foreground`, `--primary`, `--primary-foreground`, `--destructive`, `--destructive-foreground`, `--success`, `--warning`. Antes de fazer commit, rodar: `grep -rn "#[0-9a-fA-F]{3,6}" frontend/src/modules/admin/` deve retornar vazio.
