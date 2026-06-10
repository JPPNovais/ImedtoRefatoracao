# Brief 03 — Aba Financeiro do paciente (+ estorno e recibo)

Use o design system já configurado neste projeto.

## Contexto
Sistema de gestão de clínicas (Imedto). A página de detalhe do paciente tem abas
(Resumo, Prontuário, Orçamentos, Financeiro…). A aba **Financeiro** hoje é um empty
state "em breve" — vamos desenhá-la de verdade. Ela é a **segunda porta para a mesma
cobrança** do agendamento: registrar pagamento aqui repercute lá (e vice-versa).
Cirurgia só é paga por aqui. Acesso a esta aba é restrito por permissão e auditado
(dado financeiro de paciente identificado é sensível).

## A tela
1. **Resumo no topo** (3 números): Total cobrado · Total pago · Saldo em aberto.
2. **Lista de cobranças** do paciente, mais recente primeiro. Cada linha:
   - Origem com identidade visual própria: `Consulta` · `Procedimento` · `Cirurgia`
   - Descrição (ex.: "Consulta — Dr. Ricardo Tavares · 10/06"), valor cobrado,
     desconto (se houver), status (`Aberta` / `Parcialmente paga` / `Paga` /
     `Cancelada`), tag `Convênio` quando aplicável.
   - Ação "Registrar pagamento" (mesmo modal do brief 02) quando há saldo.
3. **Expandir a linha** mostra o histórico: pagamentos (data, forma, parcelas,
   valor, quem registrou), **estornos** (em vermelho, com motivo — o pagamento
   original permanece listado, riscado/atenuado, nunca some) e, para cirurgia, o
   **histórico de alteração de valor** do orçamento (de → para, quem, quando).
4. **Ações por pagamento**: "Emitir recibo" (PDF) e "Estornar" (abre confirmação
   com campo obrigatório "Motivo do estorno").

## Estados obrigatórios
- Vazio: paciente sem cobranças ("Nenhuma movimentação financeira").
- Lista com os 3 tipos ao mesmo tempo (consulta paga, procedimento parcial,
  cirurgia aberta com histórico de valor).
- Pagamento estornado (original riscado + linha de estorno com motivo).
- Sem permissão: a aba inteira mostra estado de acesso restrito.

## Dados de exemplo
- Consulta · 10/06 · R$ 350,00 · **Paga** (PIX R$ 150,00 + Crédito 2x R$ 200,00).
- Procedimento · Infiltração articular · 28/05 · R$ 800,00 · **Parcialmente paga**
  (R$ 400,00 PIX) · saldo R$ 400,00.
- Cirurgia · Orçamento #1042 — Artroscopia de joelho · R$ 12.500,00 · **Aberta** ·
  histórico: R$ 11.800,00 → R$ 12.500,00 (Dr. Ricardo, 05/06, "inclusão de implante").
- Estorno: R$ 100,00 · 02/06 · motivo "cobrança duplicada" · por Ana.

## Não desenhar
Visão financeira da clínica inteira (outra tela) · nota fiscal · edição/exclusão de
pagamento (não existe — só estorno com histórico).
