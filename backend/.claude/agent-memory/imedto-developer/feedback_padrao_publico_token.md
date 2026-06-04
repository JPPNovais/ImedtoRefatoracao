---
name: feedback_padrao_publico_token
description: Endpoints anônimos por token (link público) devem clonar TermoPublicoController; query handlers que gravam log de acesso devem ser scoped, não singleton.
metadata:
  type: feedback
---

Ao implementar endpoints públicos anônimos via token (padrão confirmação/aceite), clonar sempre:
- `TermoPublicoController` (AllowAnonymous, EnableRateLimiting, 410 genérico, ResolverIp/UserAgent)
- `termoAceitePublicoService.ts` no frontend (publicClient sem withCredentials, sem interceptor de auth)
- `AceiteTermoPublicoView.vue` (estados: carregando/válido/confirmado/expirado/erro)

**Why:** Query handler `ConsultarConfirmacaoPublicaQueryHandler` grava `AgendamentoConfirmacaoAcessoLog` via `IAgendamentoRepository.SalvarAcessoLog()` que é scoped. Registrar como singleton causaria captive dependency — sempre registrar como `AddScoped`.

**How to apply:** Checar se o handler de query usa algum serviço scoped (repositório EF, UoW) antes de decidir singleton vs scoped. Em casos de endpoint público que grava log, sempre scoped.
