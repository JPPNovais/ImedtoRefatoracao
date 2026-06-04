---
name: hotfixes-agenda-2026-06-02
description: 4 hotfixes triviais de agenda commitados sem pipeline formal — commit 5187d04 pushed
metadata:
  type: project
---

Commit 5187d04 (push direto em main, 2026-06-02) fecha 4 hotfixes reportados pelo usuário fora da pipeline formal:

1. Filtro de sala "Todas" na AgendaView: filtroSalaId migrado de ref<number|null> para ref<string>(""), comparação via String(a.salaId). Espelha filtro de Especialidade.
2. Busca rápida por CPF (PacienteQueryRepository.BuscaRapida): WHERE expandido para incluir buscaNumerica + OR cpf LIKE prefixo + OR documento_internacional — mesmo padrão do Listar(). SELECT mantém só id+nome (LGPD ok).
3. Texto do cadastro rápido no NovoAgendamentoModal: mensagem atualizada para orientar recepcionista.
4. Checkbox de lembrete WhatsApp/E-mail: substituído pill toggle custom (.tg) pelo AppCheckbox do design system.

**Why:** Bugs operacionais reportados diretamente, escopo trivial e cirúrgico.
**How to apply:** Arquivo planejamentos/2026-06-02_001_reagendamento-reseta-confirmacao-e-link-publico.md foi incluído neste commit (briefing do BA para feature de reagendamento — não relacionado aos hotfixes).

Armadilha de ambiente: backend não sobe em dev local sem permissão em /var/imedto. Workaround: DataProtection__KeysPath=/tmp/imedto-dp-keys ASPNETCORE_URLS=http://localhost:5050. Banco no EC2 (127.0.0.1:5432 recusado localmente) — validação funcional de browser com banco real não foi possível; validação coberta por análise de diff + suíte automatizada (1169 backend + 359 frontend, todos verdes).

Falha de lint pré-existente (ESLint não acha @typescript-eslint/recommended desde o commit inicial) — não introduzida por esta sessão.
