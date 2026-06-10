# Brief 05 — Financeiro da clínica (redesign) + caixa diário + configurações

Use o design system já configurado neste projeto.

## Contexto
Sistema de gestão de clínicas (Imedto). Esta é a página `/financeiro` da **clínica**
(não do paciente) — a atual é provisória e será redesenhada do zero. Quem usa:
dono/gestor e financeiro. Tudo é do **estabelecimento ativo** (dados de uma unidade
jamais aparecem em outra).

## Tela 1 — Visão geral (extrato)
1. **KPIs do período** (filtro: hoje · semana · mês · personalizado): Recebido ·
   A receber · Despesas · Saldo. Secundários: descontos concedidos, taxas de cartão,
   estornos.
2. **Extrato de lançamentos**: data, descrição (com link para o paciente/cobrança
   quando origem é um pagamento), categoria, forma de pagamento, valor (entrada
   verde / saída vermelha / estorno destacado), status.
3. Filtros: tipo (receita/despesa), categoria, forma, origem (Consulta ·
   Procedimento · Cirurgia · Avulso).

## Tela 2 — Caixa diário
- Estado do dia: **Caixa aberto** (hora de abertura, operador) ou **Caixa fechado**
  (resumo do fechamento).
- Resumo do dia por forma de pagamento (Dinheiro R$ X · PIX R$ Y · Cartão R$ Z) +
  total de estornos.
- Ação "Fechar caixa" → confirmação com o resumo e campo de observação.
  Dia fechado fica read-only com selo "Fechado por • às".

## Tela 3 — Comissões (consolidado por profissional)
Tabela por período: profissional · atendimentos · faturamento gerado · % comissão ·
valor a repassar. Linha expandível mostra o detalhamento por atendimento.
(Cirurgia usa o valor definido no orçamento; consulta/procedimento usa o % da config.)

## Tela 4 — Configurações do financeiro (aba/área de config)
- **Taxa de cartão por forma de pagamento**: lista editável (forma · taxa % ·
  ativo). Ex.: Crédito à vista 3,5% · Crédito parcelado 4,2% · Débito 1,9%.
- **Tabela de preços de consulta**: valor padrão do estabelecimento + exceções por
  profissional (ex.: padrão R$ 300,00 · Dr. Ricardo R$ 350,00).
- **Comissão por profissional**: % por profissional com default do sistema (30%)
  destacado como "padrão", editável.

## Estados obrigatórios
- Período sem movimento (extrato vazio) · caixa ainda não aberto hoje · caixa
  fechado (read-only) · taxa não configurada para uma forma (hint de alerta).

## Dados de exemplo
Junho/2026: Recebido R$ 48.350,00 · A receber R$ 23.900,00 · Despesas R$ 12.480,00 ·
Descontos R$ 1.250,00 · Taxas R$ 1.680,45. Comissões: Dr. Ricardo (30% → R$ 8.940,00),
Dra. Paula (35% → R$ 6.230,00).

## Não desenhar
Nota fiscal · conciliação bancária · DRE/contábil · multi-moeda · gateway de
pagamento.
