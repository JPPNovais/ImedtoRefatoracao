# FASE 4 — Paridade e Expansão (commodities e novos canais)

> Parte do roadmap [`README.md`](README.md). **Objetivo**: fechar as commodities cuja ausência elimina o Imedto de parte do mercado (telemedicina) e abrir canais de expansão (portal do paciente, mobile, API). **Esta fase é puxada por tração, não por calendário** — cada item entra quando houver demanda real medida (pedidos de trial perdidos, feedback de piloto), nunca "porque está no plano".
>
> **Pré-requisitos**: F2 rodando com clientes; F3 em andamento.

## Itens

### 4.1 Telemedicina — esforço M
- **Por quê**: commodity (68% das instituições oferecem) — faltar é eliminatório em parte do mercado; ter não diferencia. Por isso está na F4, não antes.
- **⚠️ Decisão antes do briefing (mini-discovery)**: build vs buy. Recomendação inicial: **buy/embed** (Daily.co, Whereby Embedded, ou similar com SDK) — vídeo WebRTC próprio é poço de manutenção; o diferencial do Imedto não é vídeo, é o fluxo em volta (agenda → sala → receita assinada na hora → registro no prontuário).
- **Escopo MVP**: agendamento tipo "teleconsulta" gera sala única + link para paciente (token público — padrão existente); sala abre no app para o profissional; ao encerrar, fluxo de evolução + receita ICP no mesmo lugar; registro de realização para fins CFM 2.314/2022.
- **CAs-chave**: link de sala expira; sem gravação por default (se gravar: consentimento + storage controlado); funciona em mobile browser; flag `telemedicina` por plano.

### 4.2 Portal do paciente — esforço M-G
- **Por quê**: retenção e percepção de modernidade; reduz carga da recepção (2ª via de documento é pedido frequente).
- **Base existente**: aba Documentos consolidada (a fonte), links públicos por token, exportação LGPD.
- **Escopo MVP** (sem login/senha — menos atrito e menos superfície): acesso por link mágico via WhatsApp/e-mail com verificação de código → paciente vê documentos liberados (receitas, atestados, pedidos, termos), confirma/reagenda consultas futuras e atualiza cadastro.
- **Evolução**: conta persistente do paciente, histórico ampliado, pagamento online de consulta (liga com financeiro F3.2).
- **CAs-chave**: liberação de documento é decisão explícita da clínica (default: liberado? — decisão de produto no briefing); todo acesso auditado (mesmo padrão LGPD); rate limit agressivo; nada de prontuário clínico no MVP (só documentos emitidos).

### 4.3 Mobile (Capacitor) 30% → core — esforço G
- **Por quê**: o app existe mas cobre ~30%; profissional quer agenda+prontuário no bolso. Avaliar honestamente: PWA bem feito pode adiar isso (o front Vue já é responsivo em parte).
- **Escopo por valor**: (1) agenda do dia + check-in; (2) prontuário leitura + evolução rápida (com IA scribe por áudio — sinergia F3.1, gravar no celular é o caso de uso natural); (3) notificações push (infra realtime existe; falta canal push).
- **CAs-chave**: paridade de segurança (BFF/cookies → estratégia de token mobile segura); offline somente leitura da agenda do dia (avaliar custo — diferencial HiDoctor).

### 4.4 API pública / integrações — esforço M
- **Por quê**: clínicas maiores pedem integração (contabilidade, BI próprio); abre ecossistema. Também prepara RNDS (interoperabilidade caminhando para requisito regulatório).
- **Escopo MVP**: API keys por estabelecimento (escopo de leitura), endpoints de pacientes/agendamentos/financeiro com paginação, webhooks de eventos (agendamento criado/cancelado, orçamento aprovado), documentação OpenAPI pública.
- **CAs-chave**: key com escopo e rate limit; toda chamada auditada; versionamento de contrato desde o dia 1 (`/v1/`).

### 4.5 Google Calendar sync — esforço P-M
- **Escopo**: espelho unidirecional (Imedto → Google) por profissional com eventos "ocupado" (sem PII no título — LGPD!); bidirecional só se demanda comprovar.
- **CAs-chave**: evento no Google sem nome de paciente; revogação de acesso limpa o espelho.

### 4.6 TISS/convênios — CONDICIONAL, esforço GG
- **Regra de entrada**: só inicia com ≥N clínicas conveniadas pagantes pedindo (definir N na época; sugestão: 10) **ou** deal âncora que pague o desenvolvimento. É a feature mais cara do mercado e o público-alvo atual é majoritariamente particular.
- **Se entrar**: começar por guia de consulta (SP/SADT depois), validação TUSS (catálogo já existe no banco!), geração de XML TISS e controle de glosa — em discovery próprio antes de qualquer briefing.

## Critérios de saída (por item, não por fase)

- Telemedicina: teleconsulta real realizada com receita assinada no fluxo.
- Portal: ≥30% dos documentos entregues via portal (vs e-mail/impresso) nos pilotos.
- Mobile: agenda+evolução usáveis offline-leve; push funcionando.
- API: primeiro integrador externo ativo.

## Execução

Cada item entra na pipeline quando seu gatilho de tração disparar. 4.1 e 4.6 exigem discovery; 4.2 exige decisão de produto sobre liberação default de documentos. Schema novo em 4.2/4.4/4.6 → `imedto-database`.
