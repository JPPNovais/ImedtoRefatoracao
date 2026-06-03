# Discovery: Envio de WhatsApp — Viabilidade e Requisitos

**Data**: 2026-06-02
**Contexto**: Briefing 2026-06-02_001 — Reagendamento + re-confirmação (Fase 1)
**Status**: NÃO IMPLEMENTADO — registra o que seria necessário para implementar no futuro

---

## Estado atual

O sistema possui um campo `detalhes.lembreteWA` (booleano) no frontend (`NovoAgendamentoModal.vue`) e o payload é enviado ao backend via `PUT /api/agendamentos/{id}`. **Porém não existe nenhuma integração de envio de mensagens WhatsApp** — o checkbox era uma UI placebo.

Como parte do briefing 2026-06-02_001 (Fase 1), o checkbox foi desabilitado com rótulo "em breve" e o resumo de canais deixou de contar WhatsApp como canal ativo. O estado (`lembreteWA`) foi mantido no payload para não quebrar compatibilidade.

---

## O que seria necessário para implementar

### 1. Escolha de provider

Três opções principais:

| Provider | Custo/msg (estimado) | Observação |
|---|---|---|
| **Meta WhatsApp Cloud API** (oficial) | ~USD 0,005–0,015 | Acesso direto à API oficial; exige WABA (WhatsApp Business Account) aprovada |
| **Twilio** | ~USD 0,005–0,10 | Intermediário consolidado; dashboard pronto; suporte técnico |
| **Z-API / WPPConnect** | ~USD 0,01–0,05 | Conectores não-oficiais; risco de banimento da conta WhatsApp |

**Recomendação**: Meta Cloud API (oficial) ou Twilio. Soluções não-oficiais (Z-API) expõem a clínica a banimento.

### 2. Credenciais e configuração

Para Meta Cloud API:
- `WHATSAPP_PHONE_NUMBER_ID` — ID do número registrado como WABA.
- `WHATSAPP_ACCESS_TOKEN` — token de longa duração gerado no Meta Business Manager.
- `WHATSAPP_VERIFY_TOKEN` — token de verificação do webhook.
- Armazenar em **AWS SSM Parameter Store** (`/imedto/{env}/whatsapp-phone-id`, etc.), nunca em código.

### 3. Template HSM (Highly Structured Message)

WhatsApp Business exige que mensagens iniciadas pela empresa usem templates **pré-aprovados** pela Meta. Cada template passa por revisão (1–7 dias úteis). Exemplos para o Imedto:

- **Lembrete de agendamento**: "Olá {{1}}, lembramos que você tem uma consulta amanhã {{2}} às {{3}} em {{4}}. Confirme sua presença respondendo SIM."
- **Remarcação**: "Olá {{1}}, seu agendamento com {{2}} foi remarcado para {{3}} às {{4}}. Em breve você poderá confirmar."

Os nomes dos templates precisam ser registrados no painel Meta e configurados como constantes no backend.

### 4. Opt-in LGPD do titular

A legislação brasileira (LGPD) e a política de uso da Meta exigem **consentimento explícito** do titular para receber mensagens WhatsApp da empresa. Isso implica:

- Campo de aceite explícito no cadastro do paciente ("Aceito receber lembretes por WhatsApp").
- Persistir `whatsapp_opt_in` + `whatsapp_opt_in_em` na tabela `pacientes`.
- Migration necessária: `imedto-database`.
- Nunca enviar para paciente sem opt-in — lança `BusinessException` ou pula silenciosamente.

### 5. Impacto nos fluxos existentes

- `EnviarLembretesAgendamentosCommandHandler` — atualmente envia apenas e-mail. Precisaria ramificar para WhatsApp quando `lembreteWA = true` e paciente com opt-in e telefone válido.
- `EnviarEmailAgendamentoReagendadoEventHandler` — poderia ser estendido para WhatsApp opcionalmente.
- `NovoAgendamentoModal.vue` — habilitar checkbox após implementação.

### 6. Custo operacional estimado

Para 500 agendamentos/mês por estabelecimento:
- 500 lembretes + 200 remarcações = ~700 mensagens/mês/estabelecimento.
- Meta Cloud API: ~USD 3,50–10,50/mês/estabelecimento.
- Impacto no pricing do plano: incorporar no custo do tier ou cobrar à parte.

---

## Decisão

**Não implementar nesta entrega** (briefing 2026-06-02_001). O checkbox é desabilitado com "em breve". A funcionalidade entra em backlog para ser especificada pelo `imedto-business-analyst` quando:

1. Provider for escolhido e contratado.
2. Templates HSM forem aprovados pela Meta.
3. Opt-in LGPD for modelado e implementado.
4. Custo/benefício for validado com o produto.
