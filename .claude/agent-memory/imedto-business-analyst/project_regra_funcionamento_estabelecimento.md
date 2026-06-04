---
name: regra-funcionamento-estabelecimento
description: Onde vive a regra de funcionamento (dia/horário/intervalo/feriado) e o endpoint de disponibilidade reutilizável — referência para briefings de agenda/encaixe.
metadata:
  type: project
---

A validação de "o estabelecimento funciona neste dia/horário" é centralizada e reutilizável; briefings de agenda devem reusar, não duplicar.

**Fonte da verdade (Domain):** `Estabelecimento.ValidarPodeAgendar(inicio, fim, agora)` valida, nesta ordem: passado; dia da semana (`DiasSemanaFuncionamento`); data bloqueada (`DatasBloqueadas` = feriados/exceções); faixa de expediente (`HorarioInicio`/`HorarioFim`); intervalos bloqueados (`HorariosBloqueados`, ex: almoço). Chamada por `CriarAgendamentoCommandHandler` em TODA criação — `TipoServico="Encaixe"` NÃO tem bypass; encaixe fechado retorna 422.

**Endpoint de leitura reutilizável:** `GET /agendamentos/disponibilidade` → `agendaService.consultarDisponibilidade(profissional, dataIni, dataFim, duracao?)`. Já calcula server-side e multi-tenant. Retorna `DisponibilidadeDia { status: "fechado"|"disponivel"|"indisponivel", slots[] }`; cada slot tem `disponivel` + `motivo: "passado"|"bloqueado"|"agendado"`. `status="fechado"` = dia não-funcional OU data bloqueada. Defaults seguros quando estab. sem config: 08:00–18:00, seg–sex, 30min.

**Tempo:** backend usa `BrasiliaTime.Now` (independe do TZ do container). Front soma `+60s` ao criar encaixe ("agora") para evitar rejeição "no passado" por skew front/back.

**Config de funcionamento também exposta no front** via `estabelecimentoService.listarMeus()`/bootstrap: `horarioInicio`, `horarioFim`, `diasSemanaFuncionamento` (0=Dom..6=Sáb), `horariosBloqueados`, `datasBloqueadas`. O `tenantStore` NÃO guarda funcionamento (só id/papel/permissões).

**Why:** múltiplos briefings de agenda tocam essas mesmas regras; saber onde elas já vivem evita propor endpoint/lógica nova e mantém o backend como fonte única da verdade (front é espelho/UX).
**How to apply:** em qualquer demanda de agenda/encaixe/bloqueio, reusar `consultarDisponibilidade` para UX preventiva e `ValidarPodeAgendar` como autoridade; nunca duplicar a lógica de horário no front sem necessidade. Primeiro briefing que usou isso: `planejamentos/2026-06-03_001_encaixe-desabilitado-estabelecimento-fechado.md`.
