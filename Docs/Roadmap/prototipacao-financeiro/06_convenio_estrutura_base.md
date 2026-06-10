# Brief 06 — Convênio: estrutura base (cadastro, carteirinha, guia)

Use o design system já configurado neste projeto.

## Contexto
Sistema de gestão de clínicas (Imedto). Convênio (plano de saúde) tem ciclo próprio:
o paciente não paga no balcão — a clínica registra a guia e recebe o repasse da
operadora semanas depois. Nesta primeira versão entregamos a **estrutura base**;
as partes avançadas (coparticipação, conciliação do repasse, glosa) aparecem
**preparadas como "em breve"** — desenhe os espaços delas com esse selo.

## Tela 1 — Cadastro de convênios (configuração)
Lista de convênios do estabelecimento: nome, registro ANS (opcional), planos
(1:N — ex.: Unimed: "Nacional", "Estadual"), ativo/inativo. CRUD simples
(drawer/modal para criar/editar).

## Tela 2 — Carteirinha no paciente (aba Convênios do paciente)
A aba Convênios da página do paciente (hoje "em breve") passa a ter:
convênio + plano, **número da carteirinha**, **validade** (com alerta visual se
vencida), histórico de uso. Um paciente pode ter mais de um convênio.

## Tela 3 — Guia/autorização no atendimento
No contexto do atendimento por convênio (check-in já marcou Convênio): campos
**nº da guia**, **senha de autorização**, data de autorização. Visível na cobrança
de convênio (aba Financeiro do paciente), no lugar do fluxo de pagamento.

## Seções "em breve" (desenhar preparadas, desabilitadas)
- **Coparticipação do paciente** (valor parcial pago no balcão).
- **Conciliação do repasse** (o que a operadora pagou × o que foi faturado).
- **Glosas** (itens recusados pela operadora + motivo).
Cada uma como card/aba presente com selo "Em breve" e uma frase do que fará.

## Estados obrigatórios
- Nenhum convênio cadastrado (empty state com CTA).
- Carteirinha vencida (alerta).
- Cobrança de convênio sem guia preenchida (pendência sinalizada).

## Dados de exemplo
Convênios: Unimed (planos Nacional, Estadual) · Bradesco Saúde (Top, Efetivo) ·
Amil (S450). Paciente Maria Aparecida Souza: Unimed Nacional, carteirinha
0 064 9999 8888 7777, validade 12/2026. Guia nº 48291 · senha AUT-99213 · 10/06.

## Não desenhar
Geração de XML TISS / lote · elegibilidade online · recurso de glosa estruturado ·
qualquer pagamento de balcão para convênio (exceto o espaço "em breve" de
coparticipação).
