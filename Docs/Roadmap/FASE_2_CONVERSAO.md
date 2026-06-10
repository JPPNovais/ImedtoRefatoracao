# FASE 2 — Conversão (o combo que fecha assinatura)

> Parte do roadmap [`README.md`](README.md). **Objetivo**: construir as 3 capacidades que mais pesam na decisão de compra (pesquisa de mercado §3) + a infraestrutura comercial (medidores, planos). Ao final, o produto tem resposta para as primeiras perguntas de toda clínica: *"confirma por WhatsApp? paciente agenda online? integra com Memed?"*
>
> **Duração estimada**: ~1 trimestre. **Pré-requisitos**: F1 completa; F0-E1 executada **antes de vender** (não antes de construir). A central de migração tem arquivo próprio: [`FASE_2B_CENTRAL_DE_MIGRACAO.md`](FASE_2B_CENTRAL_DE_MIGRACAO.md).

## Itens

### 2.1 WhatsApp — confirmação e lembrete (dor #1 do mercado) — esforço M
- **Por quê**: no-show consome 20-32% da agenda; confirmação automatizada reduz 50-70%; é a primeira pergunta de compra. ROI direto para o cliente.
- **Antes do briefing**: retomar o discovery [`whatsapp-envio/`](../Discoverys/whatsapp-envio/) e cravar: Meta Cloud API direto vs BSP (custo/aprovação/manutenção), modelo de cobrança vigente da Meta (por template utility vs janela de conversa) e custo unitário real — isso calibra as franquias do [pricing §4](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md).
- **Base existente**: motor de automações (gatilhos/regras), link público de confirmação por token (o flow de 2 vias reaproveita), templates de e-mail como referência de conteúdo.
- **Escopo MVP**: lembrete T-24h e T-2h com botões Sim/Não/Remarcar; "Sim" confirma (mesmo efeito do link público); "Não" libera o slot e notifica recepção; "Remarcar" oferece 3 slots reais da disponibilidade. Opt-in/opt-out do paciente registrado (LGPD).
- **CAs-chave**: idempotência de envio (não duplicar lembrete); webhook de resposta valida tenant pelo token, não pelo telefone; medidor de consumo por estabelecimento; falha de WhatsApp degrada para e-mail.
- **Evolução (F3)**: agente 2 vias com texto livre (diferencial B1 completo).

### 2.2 Agendamento online pelo paciente — esforço M
- **Por quê**: aquisição é prioridade de 33% das clínicas; agenda 24/7; segunda pergunta de compra.
- **Base existente**: `GET /api/agendamentos/disponibilidade`, endpoint público de profissionais (`/profissionais/publico`), páginas públicas por token (padrão de segurança já estabelecido).
- **Escopo MVP**: página pública por estabelecimento (`/agendar/{slug}`): escolhe profissional → vê slots reais → preenche nome/telefone/e-mail → agendamento nasce como "pré-agendamento" para aprovação da recepção (configurável: aprovação automática). Anti-abuso: rate limit por IP, captcha leve, verificação do telefone via código (carona no canal WhatsApp do 2.1).
- **CAs-chave**: slot ofertado respeita agenda/conflitos do domínio (EXCLUDE GiST já protege); dados mínimos (LGPD); pré-agendamento não vaza dados de outros pacientes; flag `agendamento_online` por plano.
- **Decisão de produto a destravar no briefing**: dedupe de paciente (telefone já cadastrado → vincula ou cria novo?).

### 2.3 Prescrição digital integrada — Memed — esforço P-M
- **Por quê**: expectativa básica (2 de cada 8 médicos usam); integração é commodity mas a ausência elimina.
- **Base existente**: receitas com rascunho/favoritos; o fluxo Memed (SDK front + retorno do documento) convive com a receita nativa.
- **Escopo MVP**: botão "Prescrever com Memed" na consulta → SDK embed → receita Memed salva como documento do paciente (aba Documentos) com metadados; receita nativa continua para quem preferir.
- **CAs-chave**: token Memed por profissional (cadastro/vínculo seguro); documento entra no audit; sem dependência dura (Memed fora do ar não bloqueia receita nativa).
- **Risco**: contrato/credenciamento Memed para integradores — iniciar o contato comercial no começo da fase.

### 2.4 Medidores de consumo (infraestrutura comercial) — esforço M
- **Por quê**: WhatsApp e IA têm custo variável; o [pricing](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md) depende de franquia + medidor transparente (princípio anti-dor #7).
- **Base existente**: `ai_rate_limits` é o precedente do padrão.
- **Escopo**: generalizar para `consumo_recursos` (tenant, recurso, período, usado, franquia); widget de consumo nas configurações; alerta a 80%; estouro pausa o recurso e oferece pacote/upgrade (opt-in — nunca cobrança automática).
- **CAs-chave**: contagem atômica (sem corrida); reset por ciclo de cobrança; admin global enxerga consumo agregado.

### 2.5 Planos comerciais no ar — esforço P-M
- **Escopo**: configurar os 3 tiers do [pricing §3](../Discoverys/roadmap-melhorias-2026/05_planos_e_pricing.md) no admin global (infra de planos/flags já existe), criar flags novas (`whatsapp`, `agendamento_online`, ...), página de preços pública na landing, fluxo de upgrade/downgrade self-service e cancelamento self-service com exportação (diferencial D1).
- **Pendência externa**: gateway de pagamento (cartão recorrente/Pix) — **exige mini-discovery** (Stripe vs Pagar.me vs Asaas; Asaas/Pagar.me têm Pix recorrente nativo). Até lá, cobrança manual/Pix com controle no admin (já suportado: conceder gratuidade/trocar plano).
- **CAs-chave**: troca de plano aplica flags imediatamente; downgrade nunca apaga dados (read-only do excedente); cancelamento exporta e agenda exclusão conforme LGPD.

### 2.6 Onboarding orientado a valor (UX) — esforço M
- **Por quê**: onboarding/migração é a dor #2 do mercado; trial que não ativa não converte.
- **Escopo**: meta "1º agendamento em <10 min": checklist de primeiros passos, seed de dados de exemplo (paciente/agenda fictícios marcados como demo), modelos de prontuário por especialidade aplicados na escolha da especialidade (diferencial A5 — primeiras 5 especialidades), import de pacientes como passo sugerido (ponte com F2B).
- **CAs-chave**: dados demo não contaminam relatórios e são removíveis em 1 clique; tempo-até-1º-agendamento instrumentado.

## Critérios de saída da fase

- [ ] Clínica piloto com lembrete WhatsApp ativo e taxa de no-show medida (antes/depois).
- [ ] Página de agendamento online publicada e recebendo agendamentos reais.
- [ ] Receita Memed emitida em produção por usuário real.
- [ ] Página de preços pública no ar com os 3 tiers e contratação self-service (mesmo que pagamento ainda semi-manual).
- [ ] Medidor de consumo visível e bloqueio suave de franquia funcionando.
- [ ] Tempo mediano até o 1º agendamento de trial < 10 min (instrumentado).

## Execução

Tudo pela pipeline com briefing. 2.1, 2.2, 2.4 e 2.5 mudam schema → `imedto-database`. Discoveries prévios: WhatsApp (retomar), gateway de pagamento (criar). Instrumentar as métricas da fase desde o primeiro item (não no final).
