---
name: salas-alocacao-p1
description: Feature de alocação de salas (Repartições) em agendamentos — entregue 2026-05-18, P0+P1.
metadata:
  type: project
---

Implementado em 2026-05-18 (front; backend já estava pronto e com 942/942 testes verdes).

**Fluxo entregue:**
- Recepcionista aloca sala no `CheckInModal` (carrega salas ativas, pré-sugere última sala usada pelo mesmo profissional no dia, aviso amarelo não-bloqueante de ocupação).
- Médico troca sala via `AlocarSalaModal` (componente novo em `components/agenda/`) usado pela `MeusAtendimentosView` — escutado em `AtendimentoActiveCard`, `AtendimentoNextCard` e `AtendimentoQueueRow` via emit `trocar-sala`.
- Pré-alocação no `NovoAgendamentoModal` (Step Detalhes) — select opcional só aparece se houver salas ativas e não for lista de espera.
- Filtro "Sala" client-side na `AgendaView`.
- Chip de sala (`fa-door-open` + nome) no `AgendamentoRow` (Agenda) e nos cards de atendimentos.
- `ReparticoesTab` refatorada: classes globais `.btn-icon-editar/.btn-icon-excluir`, `AppToast` no lugar de `<p class="msg-erro/ok">`, ícone `fa-building`, botão Desativar/Reativar com filtro "Incluir inativas".

**Aviso de ocupação:**
Outro agendamento do dia com mesma `salaId`, `checkInEm != null`, status ≠ Concluido/Cancelado e id ≠ atual. Apenas informativo — não bloqueia. Lógica espelhada em `AlocarSalaModal` e `CheckInModal` (computed `ocupacao`).

**Why:** Salas eram cadastráveis (CRUD em ReparticoesTab) mas não vinculáveis a agendamento — sem uso operacional. Esta feature fecha o ciclo agendamento → check-in → atendimento com rastreio de sala (LGPD: sala não é PII; é metadado operacional).

**How to apply:** Para qualquer mudança em fluxo de agendamento/check-in/atendimento que envolva sala, manter: (1) selector reutiliza `salaService.listar(estabId, true)`; (2) aviso de ocupação cruza com `outrosAgendamentosDoDia` prop; (3) `agendaService.alocarSala` e `agendaService.registrarCheckIn(id, salaId?)` são as únicas formas de mudar `Agendamento.salaId`. `Agendamento` DTO ganhou `salaId | salaNome | salaTipoNome` (todos nullable).

**Testes:** `salaService.test.ts` (4) e `agendaService.test.ts` (5) cobrem service-layer. Sem testes de componentes para AlocarSalaModal (mounting do AppModal com Teleport torna a verificação visual pouco produtiva — service tests + e2e cobrem).
