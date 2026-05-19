---
name: qa-revalidacao-73c7fe3-2026-05-18
description: Revalidação pós-fix Repartições (73c7fe3) — campo Sala em EditarAgendamentoModal confirmado em produção; aprovado.
metadata:
  type: project
---

Pipeline run 26031129449 (commit 73c7fe3) entregou todos 6 stages verde (~7 min). EditarAgendamentoModal passou a expor o campo "Sala (opcional)" na seção "Detalhes do atendimento" — bug P0 do smoke anterior corrigido.

**Persistência E2E validada em produção** (https://app.imedto.com):
- Criar sala em Repartições → OK
- NovoAgendamento Step 2: campo Sala aparece com dropdown quando há sala cadastrada (v-if intencional; com 0 salas o campo é ocultado).
- EditarAgendamento: selecionar sala + Salvar alterações → drawer fecha, card do agendamento mostra chip da sala; reabrir drawer → sala vem pré-selecionada (hidratação OK).
- Desfazer (— Sem sala — + Salvar) → chip some do card.
- Meus Atendimentos: card mostra "Alocar sala" para item sem sala; modal abre com dropdown listando opções.
- Cleanup: sala excluída sem erro; tela volta a "Nenhuma repartição cadastrada ainda.".

Zero erros/warnings no console durante toda a sessão.

**Why:** validação fecha o ciclo aberto pelo smoke test do dia 17, confirma persistência multi-camada (lista, drawer, card, Meus Atendimentos) e elimina dúvida sobre v-if do Step 2.

**How to apply:** ao revalidar features de agendamento, reusar este roteiro de 5 cliques (criar sala → novo agendamento step 2 → editar agendamento + salvar + reabrir → meus atendimentos → cleanup). Linka com [[project_qa_smoke_reparticoes_2026_05_18]].
