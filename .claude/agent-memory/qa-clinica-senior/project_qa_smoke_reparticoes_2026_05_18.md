---
name: qa-smoke-reparticoes-2026-05-18
description: Smoke autenticado de Repartições em prod — 4/5 itens OK; bugs P0 (modal Editar Agendamento) e P1 (Step 2 Novo Agendamento) sem campo Sala.
metadata:
  type: project
---

# Smoke autenticado Repartições — 2026-05-18

Conta usada: `jppnovais@gmail.com` (Dono · novaEra). Sala criada e removida no fim (`QA Sala Teste 2026-05-18`).

## Resultado por item

1. **Login**: OK. Redirect /home limpo. 0 erros de console em todo o teste.
2. **Repartições UI**: OK. Sem emoji 🏢 no markup, sem `<p class="msg-erro">`. Lista vazia mostra empty state textual ("Nenhuma repartição cadastrada ainda."). Toggle "Incluir inativas" **só aparece quando há salas** (CSR esconde controles na lista vazia — escolha aceitável). Criar/Desativar/Reativar/Excluir funcionaram. Badge "Inativa" aparece no título, botão "Excluir" some quando inativa. `AppConfirmDialog` usado na exclusão (OK).
3. **Check-in com Sala**: ⚠ não foi possível abrir modal de check-in (agendamento Confirmado já tinha check-in feito 21:25). **MAS** ao abrir o drawer "Editar agendamento" (botão Editar dentro do detalhe expandido), **NÃO há campo Sala**. Labels presentes: Profissional, Tipo, Duração, Observações. **Bug P0**: ou o campo Sala deveria estar no Editar, ou existe outro fluxo (modal de check-in) que não testei. Como o checkin no agendamento das 10:00 mostra só "Check-in às 21:25" e nenhum link/botão para alocar sala lá, é possível que o roteiro tenha confundido fluxos — a UI real está em "Meus Atendimentos" via "Alocar sala".
4. **Meus Atendimentos**: OK. Card mostra `Alocar sala` como botão visível. Modal `Alocar sala` (AppDialog) abre com dropdown `Sala (opcional)` listando "— Sem sala —" + salas ativas. Salas inativas/excluídas não aparecem. Aviso de ocupação **não foi testado** (só uma sala disponível, sem conflito). Cancelei sem alocar.
5. **Novo Agendamento Step 2**: ❌ **NÃO TEM campo Sala**. Labels presentes: Profissional, Tipo, Data, Duração, Horário, Motivo, Observações, Lembrete. **Bug P1**: o roteiro espera "Sala (opcional)" com dropdown carregado já no Step 2; não existe.

## Bugs reportados

| Prio | Bug | Onde |
|------|-----|------|
| P1 | Step 2 do "Novo agendamento" não tem dropdown "Sala (opcional)" | `frontend/src/components/agenda/AgendamentoFormFields.vue` (provável) ou wizard de criação |
| P0 | Drawer "Editar agendamento" também não tem dropdown Sala | mesmo componente acima — se a feature de alocação só vive em "Meus Atendimentos", isso é OK, mas o roteiro pede no fluxo de check-in/agenda |

## Coisas que funcionaram bem
- AppConfirmDialog na exclusão da repartição.
- Toasts visíveis ("Repartição adicionada/desativada/excluída").
- Filtros (Unidade + "Incluir inativas") aparecem só quando faz sentido.
- Modal Alocar Sala lista apenas salas ativas do tenant correto.
- 0 erros de console no fluxo inteiro.

## Pista para investigação
A feature de alocação de sala parece estar implementada **apenas no fluxo "Meus Atendimentos"** (botão `Alocar sala` no card de fila). Os fluxos de **criação** e **edição** de agendamento, e o suposto **modal de check-in**, não têm o campo. Possível que a story só previa "Meus Atendimentos" — mas se o backlog menciona o campo no Step 2 e no modal de check-in, há lacunas no front.
