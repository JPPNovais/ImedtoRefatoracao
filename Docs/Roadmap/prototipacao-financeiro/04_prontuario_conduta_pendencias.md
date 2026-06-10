# Brief 04 — Prontuário: procedimentos indicados + conduta checklist + pendências

Use o design system já configurado neste projeto.

## Contexto
Sistema de gestão de clínicas (Imedto). Quem usa é o **médico**, durante o
atendimento, preenchendo a evolução do prontuário (página com seções). Duas seções
mudam, e nasce um mecanismo de "pendências do atendimento" que guia o médico após
salvar — para ele não esquecer nada (receita, orçamento etc.).

## Tela 1 — Seção "Procedimentos indicados" (deixa de ser texto livre)
- Seletor com busca que lista **apenas os procedimentos do catálogo do
  estabelecimento ativo** (ex.: "Infiltração articular — R$ 800,00",
  "Artroscopia de joelho — R$ 9.500,00").
- Itens adicionados viram chips/linhas com campo de observação opcional.
- **Atalho "+ Criar procedimento"** no próprio dropdown: abre um mini-form inline
  (nome, valor, duração) sem sair do prontuário — o procedimento nasce no
  estabelecimento ativo e já entra selecionado.

## Tela 2 — Seção "Conduta" (vira checklist fixo)
Checklist com exatamente estas 6 ações (lista fixa do sistema):
☐ Criar receita · ☐ Criar atestado · ☐ Pedir exame · ☐ Criar orçamento ·
☐ Marcar procedimento realizado · ☐ Agendar retorno.
Campo de observação livre abaixo (mantém a anotação clínica).

## Tela 3 — Modal pós-salvar ("Próximos passos do atendimento")
Ao salvar a evolução, abre modal listando **só os itens marcados**, cada um como
link/botão de ação direta (ex.: "Criar receita →"). Ações "Fazer depois" (fecha o
modal — as pendências não se perdem) e contador (0 de 3 concluídas).

## Tela 4 — Painel persistente de pendências
Na página do paciente, um painel/banner "Pendências do atendimento — 10/06" visível
até tudo ser concluído. Cada item: nome da ação, status (pendente/concluída com
hora), link direto. Itens concluem **automaticamente** quando a ação acontece
(receita criada → item concluído). O painel some quando tudo é concluído.

## Estados obrigatórios
- Procedimentos: busca sem resultado → destaque para "+ Criar procedimento".
- Conduta: nada marcado (salvar não gera pendência nem modal).
- Modal com 3 itens marcados; painel com 1 concluída + 2 pendentes; painel
  completo (animação/estado de conclusão antes de sumir).
- Evolução antiga: conduta em texto livre renderizada read-only (retrocompat).

## Dados de exemplo
Paciente: Carlos Eduardo Lima · Dr. Ricardo Tavares · Evolução de 10/06.
Procedimentos indicados: Infiltração articular (R$ 800,00, obs.: "joelho D").
Conduta marcada: Criar receita · Criar orçamento · Agendar retorno.

## Não desenhar
A cobrança/pagamento em si (outros briefs) · checklist configurável (a lista é
fixa) · edição de evolução já salva (é imutável).
