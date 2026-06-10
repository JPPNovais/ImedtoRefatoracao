# Brief 02 — Pagamento pelo agendamento (ícone + modal de pagamento)

Use o design system já configurado neste projeto.

## Contexto
Sistema de gestão de clínicas (Imedto). A agenda mostra os agendamentos do dia em
linhas (hora · faixa colorida por status · paciente · profissional · ações
contextuais à direita). Quem usa é a **recepção**. Conceito-chave do domínio:
**cobrado ≠ pago** — a cobrança nasce no check-in; o pagamento é registrado depois,
1 ou N vezes (parcial, múltiplas formas).

## Tela 1 — Linha do agendamento com indicador financeiro
Adicionar à linha do agendamento (apenas consulta e procedimento; cirurgia NÃO):
- **Ícone/badge de pagamento** quando existe cobrança: variações por estado —
  `Aberta` (ex.: R$ 350,00 a receber), `Parcialmente paga` (ex.: R$ 150,00 de
  R$ 350,00), `Paga` (check discreto). Convênio mostra tag "Convênio" sem valor.
- Clicar no ícone abre o modal de pagamento.

## Tela 2 — Modal "Registrar pagamento"
Cabeçalho: paciente, origem (Consulta · 10/06 · Dr. Ricardo), valor cobrado,
desconto (se houver), **saldo restante** em destaque.

Corpo:
1. Lista dos pagamentos já feitos (data, forma, valor, quem registrou).
2. Form de novo pagamento: **Valor (R$)** (pré-preenchido com o saldo) ·
   **Forma de pagamento** (select: PIX, Dinheiro, Cartão de crédito, Cartão de
   débito) · se cartão de crédito: **Parcelas** (1–12) e exibição automática da
   **taxa** configurada (ex.: "taxa 3,5% — você recebe R$ 337,75") — a taxa é
   informativa, nunca editável aqui.
3. Campo **Desconto** (R$) sobre a cobrança — visível só para quem tem permissão;
   para os demais, oculto.
4. Botão "Registrar pagamento". Permite registrar mais de um pagamento em sequência
   (ex.: metade PIX + metade cartão).

Após quitar: estado de sucesso com saldo zerado e ação secundária "Emitir recibo".

## Estados obrigatórios
- Cobrança aberta (nenhum pagamento) · parcialmente paga (1 pagamento listado +
  saldo) · paga (form some, fica histórico + "Emitir recibo") · convênio (modal
  explica que não há pagamento de balcão).
- Erro: valor maior que o saldo → mensagem "O valor excede o saldo da cobrança".

## Dados de exemplo
Cobrança: Consulta · Maria Aparecida Souza · R$ 350,00 · sem desconto.
Pagamento 1: R$ 150,00 · PIX · 10/06 14:55 · por Ana (recepção). Saldo: R$ 200,00.

## Não desenhar
Cirurgia (não tem pagamento pelo agendamento) · estorno (fica na aba do paciente) ·
nota fiscal · gateway/cobrança online.
