---
name: bug-onboarding-filter-admin
description: OnboardingCompletadoFilter global intercepta JWT admin e retorna 403 OnboardingPendente em TODAS rotas /api/admin/*
metadata:
  type: project
---

`OnboardingCompletadoFilter` está registrado como filtro global em `AddControllers(options.Filters.Add<OnboardingCompletadoFilter>())` em Program.cs linha 204.

Quando admin faz login e o cookie `admin-access-token` HttpOnly é setado, toda request subsequente (incluindo a própria rota `/api/admin/estabelecimentos`, `/api/admin/admins`, etc.) chega com o token autenticado. O filtro obtém o `sub` (UUID do admin), tenta buscar em `usuarios` (não em `imedto_admins`), retorna null → `onboardingCompleto = false` → 403 com `{"tipo":"OnboardingPendente"}`.

**Fix**: adicionar early-return em `OnboardingCompletadoFilter.OnActionExecutionAsync` para claim `imedto_admin = "true"`:
```csharp
var isAdminGlobal = context.HttpContext.User.FindFirst("imedto_admin")?.Value == "true";
if (isAdminGlobal) { await next(); return; }
```

**Por que é Tipo A**: comportamento esperado está claro no briefing (CA8/CA9/CA11); causa raiz é código localizável em `Filters/OnboardingCompletadoFilter.cs:48-54`; não é gap de spec.

**Why:** O filtro foi escrito antes da área admin existir. Nunca foi atualizado para considerar JWT de admin global. O fix não muda regra de negócio — apenas protege a fronteira de identidade correta.

**How to apply:** Toda entrega que toca auth admin ou adiciona novo filtro global deve verificar se o `OnboardingCompletadoFilter` precisa de bypass.
