# Brief 01 — Check-in com Particular/Convênio + valor da consulta

Use o design system já configurado neste projeto.

## Contexto
Sistema de gestão de clínicas médicas (Imedto). Quem usa esta tela é a **recepção**,
no balcão, com o paciente na frente — velocidade importa. Já existe um modal de
check-in (resumo do agendamento + dados do paciente + seleção de sala). Vamos
**estender esse modal** com a parte financeira.

## O que adicionar ao modal de check-in
Nova seção "Atendimento" no modal, entre os dados do paciente e a seleção de sala:

1. **Tipo de atendimento** — escolha obrigatória entre `Particular` e `Convênio`
   (segmented control ou radio). Se a clínica não usa convênio, mostrar só Particular
   já selecionado.
2. **Se Particular**: campo "Valor da consulta (R$)" já preenchido com o valor
   sugerido da tabela de preços (ex.: R$ 350,00 — vem da config por profissional, ou
   do padrão do estabelecimento). O valor é **editável**. Indicar discretamente a
   origem: "sugerido pela tabela de preços". Este é o valor **cobrado**, não pago —
   nenhuma forma de pagamento aparece aqui.
3. **Se Convênio**: select "Convênio" (ex.: Unimed, Bradesco Saúde) + campo opcional
   "Nº da guia/autorização". Sem campo de valor — o paciente não paga no balcão.

## Fluxo
1. Recepção clica "Check-in" no agendamento → modal abre.
2. Escolhe Particular → valor sugerido aparece preenchido → ajusta se necessário.
3. Confirma check-in → toast de sucesso mencionando que a cobrança foi registrada
   ("Check-in realizado · Cobrança de R$ 350,00 criada").

## Estados obrigatórios
- Particular com valor sugerido carregado.
- Particular **sem tabela de preços configurada** → campo vazio com hint
  "Nenhum valor sugerido — configure a tabela de preços" (link).
- Convênio selecionado (valor some, select de convênio aparece).
- Erro de validação: valor zerado/negativo em Particular.

## Dados de exemplo
Paciente: Maria Aparecida Souza · Dr. Ricardo Tavares (Cardiologia) ·
Consulta 14:30 · Valor sugerido R$ 350,00 · Convênios: Unimed, Bradesco Saúde, Amil.

## Não desenhar
Formas de pagamento (acontece depois, em outro fluxo) · fluxo de guia do convênio ·
emissão de recibo/nota.
