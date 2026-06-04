---
name: project_padrao_termos_clonado
description: Padrão Termos (token/controller/audit/rate-limit) foi corretamente clonado na Fase 2 de agendamentos
metadata:
  type: project
---

A Fase 2 do briefing 2026-06-02_001 usou o padrão maduro de Termos como base:
- Token url-safe 256 bits: `RandomNumberGenerator.GetBytes(32)` → base64url sem padding (idêntico a TermoEmitido).
- Controller público `[AllowAnonymous] + [EnableRateLimiting("agendamentos-publico")]` — clonado de TermoPublicoController.
- 410 genérico idêntico para todos os casos de erro (anti-enumeração).
- `AgendamentoConfirmacaoAcessoLog` clonado de `TermoEmitidoAcessoLog` (sem paciente_id).
- Rate limit "agendamentos-publico" 10 req/min registrado junto ao "termos-publico" no Program.cs.

**Why:** CLAUDE.md exige reuso antes de criar mecânica nova. O padrão Termos estava maduro e testado.

**How to apply:** Em futuras features com "endpoint público por token", usar o padrão Termos/Agendamentos
como referência. Verificar se os 4 elementos acima estão presentes: token, rate limit, 410 genérico, audit sem PII.
