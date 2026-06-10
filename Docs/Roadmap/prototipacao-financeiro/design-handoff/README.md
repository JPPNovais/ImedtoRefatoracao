# Design handoff — Módulo Financeiro/Cobranças (Claude Design)

Subconjunto **pertinente ao financeiro** extraído do bundle de handoff do Claude Design
(projeto completo do Imedto). O protótipo implementou fielmente os 6 briefs de
[`../`](../README.md), sem divergências de escopo. Origem:
`https://api.anthropic.com/v1/design/h/2aY-tzLEhas1c57LNVd2IQ` (2026-06-10).

## Como usar

- **É referência visual, não código de produção.** Os protótipos são HTML/CSS/JS+React
  standalone; a implementação real é Vue 3 + design system (`frontend/src/components/ui/`).
  Recriar o **resultado visual**, não copiar a estrutura interna.
- **Tokens vencem o protótipo**: onde o CSS do protótipo usa valores literais de
  `font-size`/`font-weight`, a implementação usa os tokens de `main.css` (CLAUDE.md §5).
- As páginas **abrem direto no navegador** (React/Babel via CDN) — duplo clique no .html.
- Dados são fictícios (`*-data.js`) — úteis como fixture de referência para QA.
- Painel de "tweaks" (`tweaks-panel.jsx`) simula permissões/estados:
  `clinicUsesConvenio`, `canDiscount`, `canViewPatientFinance`, `priceTableConfigured`.

## Mapa fase → arquivos → screenshots

| Fase | Tela | Página | Componentes-chave | Screenshots finais |
|---|---|---|---|---|
| **F1** | Check-in Particular/Convênio | `Agenda.html` | `components/CheckInModal.jsx` (+ `config-data.js` p/ tabela de preços) | `checkin-atendimento.png`, `checkin-particular.png`, `02-checkin-financeiro.png` |
| **F1** | Badge + modal de pagamento | `Agenda.html` | `components/AppointmentRow.jsx` (`.payment-badge` em `styles/agenda.css`), `components/PaymentModal.jsx` | `pay-badges.png`, `02-pay-states.png`, `02-pay-modal-parcial.png`, `modal-real.png` |
| **F2/F8** | Aba Financeiro do paciente (estorno, recibo, gate) | `PacienteDetalhe.html` | `components/PatientDetail.jsx` (TabFinanceiro), `components/PatientDetailExtra.jsx` (ChargeCard, EstornoModal) | `fin-tab.png`, `02-fin-cirurgia.png`, `02-fin-estorno.png`, `fin-restricted.png` |
| **F3/F3B** | Procedimentos do catálogo + conduta checklist + pendências | `Prontuario.html` | `components/ProntuarioModules.jsx` (IndicatedProcModule, ConductChecklistModule, NextStepsModal, PendenciesPanel) + `care-data.js` | `02-pront-catalog.png`, `02-pront-noresult.png`, `02-pront-nextsteps.png`, `03-pront-panel-final.png`, `pront-legacy.png` |
| **F7** | Financeiro da clínica (Extrato, Caixa, Comissões, Config) | `Financeiro.html` | `ClinicFinanceApp.jsx`, `components/ClinicFinanceTabs.jsx`, `clinic-finance-data.js`, `styles/clinic-finance.css` | `cf-overview.png`, `cf-extrato.png`, `cf-caixa.png`, `cf-fechado.png`, `02-cf-fechar.png`, `02-cf-comissoes.png`, `cf-config2.png` |
| **F6** | Convênios (cadastro, carteirinha, guia, "em breve") | `Convenios.html` + aba Convênios em `PacienteDetalhe.html` | `ConveniosApp.jsx` (CRUD drawer), `PatientDetailExtra.jsx` (CarteirinhaCard, GuiaForm) | `cv-cadastro.png`, `cv-drawer.png`, `cv-patient.png`, `cv-soon.png`, `02-cv-guia-filled.png`, `cv-guia-modal.png` |

> Screenshots com prefixo numérico maior (`02-`, `03-`) são iterações mais recentes da
> mesma tela — em dúvida, vale a de maior prefixo.

## Decisões de design que o protótipo cravou (insumo para os briefings)

- **Badge de pagamento** na linha da agenda com 4 estados: Aberta (valor a receber),
  Parcial ("R$ X de R$ Y"), Paga (check discreto), Convênio (tag sem valor).
- **PaymentModal único** reutilizado pelas duas portas (agenda e aba do paciente):
  cabeçalho com saldo em destaque, histórico, form com valor pré-preenchido pelo saldo,
  taxa de cartão exibida como informação ("você recebe R$ X"), desconto visível só com permissão.
- **Estorno**: pagamento original riscado/atenuado e mantido na lista; linha de estorno em
  vermelho com motivo obrigatório.
- **Cirurgia** na aba do paciente exibe histórico de alteração de valor (de → para, quem, quando).
- **Cobrança de convênio** troca o fluxo de pagamento por seção Guia/Autorização
  (nº guia, senha, data) com estado "pendente" vs "preenchida".
- **Conduta**: 6 checkboxes fixos + observação livre; modal "Próximos passos" pós-salvar
  com contador ("0 de 3 concluídas") e ação "Fazer depois"; painel persistente de
  pendências com conclusão automática.
- **Navegação**: Convênios e Financeiro entraram no submenu de Configurações, grupo
  **"Faturamento"**.
- Estados de exceção todos desenhados: tabela de preços não configurada, busca de
  procedimento sem resultado, acesso restrito, caixa fechado read-only, carteirinha vencida.
