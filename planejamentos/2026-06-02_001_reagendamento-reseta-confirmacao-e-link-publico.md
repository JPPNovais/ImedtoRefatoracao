# Reagendamento reseta confirmação + re-confirmação por e-mail (Fase 1) e link público de confirmação (Fase 2)

**ID**: 2026-06-02_001
**Status**: Aprovado por usuário em 2026-06-02
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (Fase 1 = M; Fase 2 = M-G)
**Áreas regressivas tocadas**: agenda (máquina de estados de Agendamento), automações (lembrete por e-mail), permissionamento (RBAC `agenda`), LGPD (e-mail + token público)

---

## 1. Contexto e motivação

Hoje, um agendamento com `Status = Confirmado` que é **reagendado** (mudança de horário ou de profissional) **permanece Confirmado** — porque `Agendamento.Atualizar()` (`Agendamento.cs:136-164`) não toca no `Status`. Resultado operacional: a clínica acredita que a paciente confirmou presença em um horário que **ela nunca viu**. A "confirmação" passa a ser uma mentira de dado: foi feita para o horário antigo.

Isso gera no-show, retrabalho da recepção e, no limite, fricção com a paciente que aparece no horário errado ou não aparece.

A demanda tem **duas fases**, num único briefing imutável:

- **Fase 1** — corrigir a máquina de estados (reagendar Confirmado → volta a Agendado/pendente) e **avisar a paciente por e-mail** que houve mudança e que a confirmação será refeita. A recepção reconfirma manualmente (fluxo `ConfirmarAgendamentoCommand` já existente). Sem link público.
- **Fase 2** — link público de confirmação (paciente clica "Confirmar presença" e o status volta a Confirmado sem intervenção da recepção), clonando o padrão maduro de **Termos** (token 256 bits, controller `[AllowAnonymous]` + rate limit, 410 genérico, zero PII no payload, audit de acesso, idempotência).

WhatsApp **não tem integração de envio** no sistema (só checkbox visual). Fica como dependência futura documentada em discovery; o checkbox enganoso é desabilitado nesta entrega.

## 2. Persona-alvo

- **Recepcionista / Administrador** (papel com permissão de ação `agenda`): reagenda no `EditarAgendamentoModal`, dezenas de vezes por dia. É quem hoje sofre com o status mentiroso.
- **Paciente** (sem login): recebe o e-mail informativo (Fase 1) e, na Fase 2, clica no link público e reconfirma sozinha.
- **Dono/Profissional**: consome o dado de status como verdade para planejar a agenda.

## 3. Escopo

**Inclui (Fase 1)**:
- Reset de `Confirmado → Agendado` no domínio quando reagendamento muda `InicioPrevisto`, `FimPrevisto` **ou** `ProfissionalUsuarioId`.
- Zerar `LembretePorEmailEnviado` ao resetar (para o lembrete de 24h poder redisparar no novo horário).
- E-mail informativo automático à paciente avisando da mudança e de que a confirmação será refeita (reusa `IEmailService` / SES).
- Espelho/UX no front (`EditarAgendamentoModal` / `AgendaView`): badge/aviso de que reagendar um Confirmado o tornará pendente novamente.
- Desabilitar o checkbox "WhatsApp" (`detalhes.lembreteWA`) no `NovoAgendamentoModal` com rótulo "em breve".

**Inclui (Fase 2)**:
- Token público de confirmação (256 bits, RFC 4648 url-safe) + expiração, persistidos no agendamento.
- Endpoint público `[AllowAnonymous]` + rate limit: GET (visualizar resumo mínimo) e POST (confirmar presença).
- Página pública no frontend (rota anônima) com botão "Confirmar presença".
- E-mail com o link de confirmação (canal: e-mail / SES).
- Audit de acesso (IP/UserAgent), 410 genérico, idempotência, zero PII no payload.

**Não inclui**:
- Envio por WhatsApp (sem integração — vai para discovery).
- Recusa/cancelamento via link público (Fase 2 só confirma presença; cancelar continua sendo ação interna da recepção).
- Reagendamento via link público pela própria paciente.
- Alterar o comportamento de Cancelar/Concluir/CheckIn/AlocarSala.
- Lembrete por WhatsApp.

## 4. Regras de negócio

### Detecção de mudança e reset (Fase 1)

- **R1 — Gatilho do reset**: em `Agendamento.Atualizar(...)`, **se** `Status == Confirmado` **e** mudou (`InicioPrevisto` **ou** `FimPrevisto` **ou** `ProfissionalUsuarioId`) em relação ao valor atual → `Status` volta para `Agendado`. Comparação **antes** de sobrescrever os campos. Mora em: **Domain** (`Agendamento.Atualizar`). Espelho/UX no **Front**.
- **R2 — Editar sem reset**: se `Status == Confirmado` e mudou **apenas** `TipoServico` e/ou `Observacoes` (horário e profissional idênticos) → `Status` permanece `Confirmado`, **sem** e-mail e **sem** zerar lembrete. Mora em: **Domain**. Espelho no **Front** (não exibir aviso de re-confirmação nesse caso).
- **R3 — Zerar lembrete ao resetar**: quando R1 dispara o reset, setar `LembretePorEmailEnviado = false` no mesmo `Atualizar()`, para que `EnviarLembretesAgendamentosCommandHandler` possa redisparar o lembrete de 24h no novo horário. Mora em: **Domain**.
- **R4 — Sinalização para evento de e-mail**: `Atualizar()` deve sinalizar que houve reset (ex.: `AddDomainEvent(new AgendamentoReagendadoEvent(...))` anexado apenas quando R1 dispara) para que um event handler dispare o e-mail informativo de forma desacoplada — mesmo padrão de `AgendamentoCriadoEvent` / `TermoEmitidoEvent`. Mora em: **Domain** (anexa evento) + **Application/Events** (handler que envia e-mail). O evento **não** carrega PII (só `AgendamentoId`, `EstabelecimentoId`, `PacienteId`, novo `InicioPrevisto`, `ProfissionalUsuarioId`).

### Caso "já era Agendado/pendente" (coerência da máquina de estados — DECIDIDO)

- **R5 — Reagendar um Agendado permanece Agendado, sem reset, mas reenvia informativo**: se `Status == Agendado` e mudou horário/profissional, **não há reset** (já está pendente) e **não se zera** lembrete diferentemente (segue R6). Para manter a paciente informada de forma coerente com a Fase 1, o **mesmo e-mail informativo** de mudança é disparado (a paciente precisa saber do novo horário tanto quanto no caso Confirmado). Decisão fixada: o gatilho do **e-mail informativo** é "mudou horário ou profissional" independente do status de origem ser Agendado ou Confirmado; o gatilho do **reset de status** é exclusivo de origem Confirmado.
- **R6 — Zerar lembrete também quando Agendado muda horário**: se `Status == Agendado` e mudou `InicioPrevisto`/`FimPrevisto`/`ProfissionalUsuarioId`, zerar `LembretePorEmailEnviado` (o lembrete antigo, se já enviado, apontava para horário/profissional desatualizado). Editar só observações/tipoServico **não** zera. Mora em: **Domain**.
- **R7 — Cancelado/Concluído seguem bloqueados**: `Atualizar()` continua lançando `BusinessException` para `Cancelado`/`Concluído` e para agendamento que já ocorreu (comportamento atual preservado — não regredir).

### E-mail informativo (Fase 1)

- **R8 — Degradação graciosa sem e-mail**: se a paciente não tem e-mail (`Paciente.Email` nulo/vazio), o reagendamento **conclui com sucesso** (status resetado, lembrete zerado, transação commitada) e o envio é **apenas pulado** com log `Information` (sem PII). Mesmo padrão de `EnviarEmailTermoLinkEventHandler:59-63`. Mora em: **Application/Events**.
- **R9 — Falha de envio não bloqueia**: exceção no envio de e-mail é capturada com `LogWarning` (sem destinatário/assunto/corpo, conforme política SES) e **não** relança — o reagendamento já está persistido. Mesmo padrão de `EnviarEmailTermoLinkEventHandler:75-79`. Mora em: **Application/Events**.
- **R10 — Conteúdo do e-mail Fase 1 (mínimo)**: novo método em `EmailTemplates` com: nome fantasia do estabelecimento, tipo de serviço, novo profissional, nova data/hora (Brasília), e a frase "seu agendamento foi remarcado; em breve você poderá reconfirmar sua presença". Sem CPF, sem dados clínicos. Mora em: **Infrastructure/Email**.

### Confirmação por link público (Fase 2)

- **R11 — Geração de token**: ao resetar/reagendar (ou em ação explícita "enviar link de confirmação"), gerar token url-safe de 256 bits (32 bytes, RFC 4648 §5 sem padding) e `TokenConfirmacaoExpiraEm` no agendamento. Mesma técnica de `TermoEmitido.GerarTokenUrlSafe`. TTL configurável (default sugerido: até o horário do agendamento ou X horas — fixar no dev como `min(inicioPrevisto, agora + TTLpadrão)`; **default TTL 7 dias**, nunca além de `InicioPrevisto`). Mora em: **Domain**.
- **R12 — Endpoint público**: controller `[AllowAnonymous]` + `[EnableRateLimiting]` (reusar política existente ou criar "agendamentos-publico", 10 req/min por IP). GET retorna resumo mínimo (nome fantasia do estabelecimento, profissional, tipo de serviço, data/hora) — **zero PII do paciente** (sem nome, CPF, e-mail, `paciente_id`). Mora em: **API/Controllers** + **Application/Queries**.
- **R13 — Confirmação via POST**: paciente clica "Confirmar presença" → `Status` volta a `Confirmado` via método de domínio (ex.: `ConfirmarPorLinkPublico(ip, userAgent)`) **somente** se `Status == Agendado` **e** token válido **e** não expirado. Mora em: **Domain** + **Application/Commands**.
- **R14 — 410 genérico**: token inválido / expirado / agendamento já confirmado por outro meio / cancelado → **410 Gone** com mensagem genérica idêntica em todos os casos (não vazar existência). Mesmo padrão de `TermoPublicoController:64-72`. Mora em: **API/Controllers**.
- **R15 — Idempotência**: POST repetido em agendamento já Confirmado retorna **200** com mensagem "Presença já confirmada. Você pode fechar esta página." — nunca altera estado nem erra. Mesmo padrão de `TermoPublicoController:113-119`. Mora em: **Application/Commands** + **API**.
- **R16 — Audit de acesso**: cada acesso GET/POST ao link público grava log de acesso com `{IP, UserAgent, AgendamentoId, EstabelecimentoId, timestamp}` — **sem PII do paciente**. Reusar padrão de `TermoEmitidoAcessoLog` / audit de Termos. Mora em: **Domain** + **Infrastructure**.
- **R17 — E-mail com link (Fase 2)**: novo método em `EmailTemplates` com link `{AppBaseUrl}/agendamentos/confirmar/{token}` + dados mínimos (estab, profissional, data/hora). Mesma degradação graciosa de R8/R9. Mora em: **Application/Events** + **Infrastructure/Email**.

### RBAC e multi-tenant (ambas as fases)

- **R18 — RBAC reagendar/confirmar**: ações internas (`PUT /agendamentos/{id}`, `POST /agendamentos/{id}/confirmar`) exigem `[Authorize] + [RequiresEstabelecimento] + [RequiresAcao("agenda")]` (já aplicado no `AgendamentoController`). Quem não tem ação `agenda` → 403 e botão oculto no front. Mora em: **API** (atributos) + **Front** (guard de botão).
- **R19 — Multi-tenant interno**: todo command/query filtra `EstabelecimentoId` no repositório (falha-fechada). Já implementado em `AtualizarAgendamentoCommandHandler:30` e `ConfirmarAgendamentoCommandHandler:18` via `ObterPorIdOuNulo(id, estabelecimentoId)` → `BusinessException("Agendamento não encontrado.")` genérica. Preservar.
- **R20 — Multi-tenant no link público**: o endpoint anônimo resolve o agendamento **pelo token**, não por `estabelecimento_id` da sessão (não há sessão). O token é o único segredo. A query pública nunca expõe `paciente_id`/`estabelecimento_id` no payload de resposta. Mora em: **Application/Queries** + **API**.

## 5. Modelo de dados

**Fase 1** — `agendamentos`:
- Nenhuma coluna nova obrigatória. `lembrete_por_email_enviado` (já existe) passa a ser zerada em reset.
- (Opcional, decisão do dev/db) `reagendado_em` se o time quiser timestamp dedicado; **não** é requisito — `atualizado_em` já cobre.

**Fase 2** — `agendamentos` (migration via `imedto-database`):
- `token_confirmacao` (text, nullable, único quando não nulo) — token url-safe 256 bits.
- `token_confirmacao_expira_em` (timestamptz, nullable).
- `confirmado_por_link_em` (timestamptz, nullable) — opcional, para distinguir confirmação por link de confirmação manual; decisão do db.
- Índice em `token_confirmacao` (lookup do endpoint público; parcial `WHERE token_confirmacao IS NOT NULL`).
- **Audit de acesso** — tabela de log de acesso ao link público (clonar `termo_emitido_acesso_log`): `{id, agendamento_id, estabelecimento_id, ip, user_agent, acessado_em, acao}`. Sem `paciente_id`, sem PII. Índice por `agendamento_id`.
- Multi-tenant: `agendamento_id` e `estabelecimento_id` presentes no log para rastreio; payload de resposta nunca os expõe.
- LGPD: token é credencial — não logar em texto claro; expiração obrigatória; retenção do log de acesso conforme política de Termos.

## 6. UX e fluxo

### Fase 1 — `EditarAgendamentoModal.vue` / `AgendaView.vue`
- Quando o agendamento aberto está `Confirmado` e o usuário altera horário ou profissional, exibir **aviso inline** (componente de alerta do design system) antes/ao salvar: "Este agendamento está confirmado. Ao remarcar o horário ou o profissional, ele voltará para 'Pendente' e a paciente será avisada por e-mail para reconfirmar."
- Se alterar só observações/tipo de serviço → **sem aviso** (R2).
- Após salvar com reset: badge de status na agenda atualiza para "Pendente/Agendado". Estados: loading no botão Salvar; erro 422 com mensagem genérica do back; sucesso fecha modal e atualiza a célula.
- Mobile-ready (modal já é responsivo).

### Fase 1 — `NovoAgendamentoModal.vue` (ajuste de UI)
- Checkbox "WhatsApp" (`detalhes.lembreteWA`, linha ~787): adicionar atributo `disabled` + rótulo/tooltip "em breve". O resumo de canais (linhas ~874, ~892) deixa de contar WhatsApp como ativo. E-mail continua funcional. Não remover o campo do estado (evita quebrar payload/testes) — apenas travar UI.

### Fase 2 — Página pública de confirmação
- Nova rota anônima `/agendamentos/confirmar/:token` (espelhar `/termos/aceite/:token` → `AceiteTermoPublicoView`).
- Estados: **carregando**, **válido** (mostra resumo mínimo + botão "Confirmar presença"), **confirmado** ("Presença confirmada, pode fechar"), **link inválido/expirado** (410 → tela genérica "Link inválido ou expirado", sem revelar motivo). Reusar layout/estilo da view pública de Termos.
- Sem login, sem menu, sem PII. Mobile-first (paciente abre no celular).

## 7. Critérios de aceite (testáveis)

### Fase 1

- **CA1 (caminho feliz — reset por horário)**: Dado um agendamento `Confirmado` às 10h com profissional A, Quando a recepção o reagenda para 14h via `PUT /agendamentos/{id}`, Então o `Status` passa a `Agendado`, `LembretePorEmailEnviado` vira `false`, `atualizado_em` é atualizado e um e-mail informativo é disparado à paciente.
- **CA2 (reset por profissional)**: Dado um agendamento `Confirmado` com profissional A no mesmo horário, Quando é reagendado para o profissional B, Então `Status` passa a `Agendado` e o e-mail informativo é disparado.
- **CA3 (editar sem reset)**: Dado um agendamento `Confirmado`, Quando apenas `Observacoes` e/ou `TipoServico` mudam (horário e profissional idênticos), Então `Status` permanece `Confirmado`, `LembretePorEmailEnviado` permanece inalterado e **nenhum** e-mail é disparado.
- **CA4 (origem Agendado — informativo, sem reset)**: Dado um agendamento `Agendado`, Quando é reagendado para outro horário, Então `Status` permanece `Agendado`, `LembretePorEmailEnviado` vira `false` e o e-mail informativo é disparado.
- **CA5 (reconfirmação manual)**: Dado um agendamento resetado para `Agendado`, Quando a recepção chama `POST /agendamentos/{id}/confirmar`, Então `Status` volta a `Confirmado` (fluxo `ConfirmarAgendamentoCommand` inalterado).
- **CA6 (reagendar várias vezes)**: Dado um agendamento `Confirmado` reagendado (vira `Agendado`) e reconfirmado (vira `Confirmado`), Quando é reagendado novamente, Então volta a `Agendado` e dispara novo e-mail — sem erro de estado em N iterações.
- **CA7 (bloqueio Cancelado/Concluído)**: Dado um agendamento `Cancelado` ou `Concluído`, Quando se tenta `Atualizar`, Então retorna 422 com a mensagem atual e nada muda.
- **CA8 (degradação sem e-mail)**: Dado um agendamento `Confirmado` cuja paciente não tem e-mail cadastrado, Quando é reagendado, Então o reset acontece e a transação commita com sucesso (status `Agendado`), e o envio é apenas pulado com log `Information` **sem PII** — o reagendamento **não falha**.
- **CA9 (falha de envio não bloqueia)**: Dado que o provedor de e-mail (SES) lança exceção, Quando ocorre o reagendamento, Então o `Status` permanece resetado/persistido e o erro é registrado como `LogWarning` **sem destinatário/assunto/corpo**.
- **CA10 (LGPD no e-mail)**: Dado o disparo do e-mail informativo, Quando o log é gravado, Então não contém destinatário, assunto nem corpo (política SES); e o corpo do e-mail não contém CPF nem dados clínicos.
- **CA11 (multi-tenant interno)**: Dado um usuário do estabelecimento B, Quando tenta `PUT`/`confirmar` um agendamento do estabelecimento A, Então recebe `BusinessException` genérica "Agendamento não encontrado" e nada é logado com PII.
- **CA12 (RBAC)**: Dado um usuário sem a ação `agenda`, Quando chama `PUT /agendamentos/{id}` ou `confirmar`, Então recebe 403 e os botões de editar/confirmar ficam ocultos no front.
- **CA13 (UX aviso de reset)**: Dado o `EditarAgendamentoModal` de um agendamento `Confirmado`, Quando o usuário altera horário ou profissional, Então o aviso de re-confirmação aparece; Quando altera só observações/tipo, Então o aviso **não** aparece.
- **CA14 (checkbox WhatsApp desabilitado)**: Dado o `NovoAgendamentoModal`, Quando aberto, Então o checkbox "WhatsApp" está `disabled` com rótulo "em breve" e não conta como canal ativo no resumo; o checkbox "E-mail" continua funcional.
- **CA15 (lembrete redispara no novo horário)**: Dado um agendamento cujo lembrete de 24h já foi enviado (`lembrete_por_email_enviado = true`), Quando é reagendado mudando o horário, Então `lembrete_por_email_enviado` vira `false` e o `EnviarLembretesAgendamentosCommandHandler` volta a elegê-lo dentro da janela do novo horário.

### Fase 2

- **CA16 (geração de token)**: Dado um reagendamento/emissão de link, Quando o token é gerado, Então tem 256 bits de entropia (32 bytes url-safe, sem padding), é único e tem `token_confirmacao_expira_em` definido (nunca além de `InicioPrevisto`).
- **CA17 (visualizar — payload sem PII)**: Dado um token válido, Quando `GET /api/publico/agendamentos/confirmar/{token}`, Então retorna 200 com apenas nome fantasia do estabelecimento, profissional, tipo de serviço e data/hora — **sem** nome do paciente, CPF, e-mail, `paciente_id` ou `estabelecimento_id`.
- **CA18 (confirmar presença)**: Dado um token válido de agendamento `Agendado`, Quando `POST` confirma presença, Então `Status` vira `Confirmado` e a resposta é 200 com mensagem de sucesso.
- **CA19 (410 genérico)**: Dado um token inexistente, expirado, ou de agendamento cancelado, Quando `GET`/`POST`, Então retorna **410 Gone** com a **mesma** mensagem genérica em todos os casos (não revela qual condição falhou).
- **CA20 (idempotência)**: Dado um agendamento já `Confirmado`, Quando o `POST` é repetido com o mesmo token, Então retorna **200** "Presença já confirmada", sem alterar estado.
- **CA21 (rate limit)**: Dado mais de 10 requisições por minuto do mesmo IP ao endpoint público, Quando excede o limite, Então recebe 429 (anti-enumeração).
- **CA22 (audit de acesso)**: Dado um acesso `GET`/`POST` ao link público, Quando ocorre, Então grava log com `{IP, UserAgent, AgendamentoId, EstabelecimentoId, timestamp, acao}` e **sem PII do paciente**.
- **CA23 (multi-tenant via token)**: Dado o endpoint público, Quando resolve o agendamento, Então o faz exclusivamente pelo token (sem sessão/tenant claim) e o payload de resposta nunca expõe `paciente_id`/`estabelecimento_id`.
- **CA24 (e-mail com link — degradação)**: Dado o disparo do e-mail de confirmação (Fase 2), Quando a paciente não tem e-mail, Então o envio é pulado com log `Information` sem PII e o fluxo não falha; Quando o SES lança exceção, Então `LogWarning` sem conteúdo e o agendamento permanece íntegro.
- **CA25 (página pública — estados)**: Dado a rota `/agendamentos/confirmar/:token`, Quando carrega com token válido, Então mostra resumo mínimo + botão "Confirmar presença"; com token inválido/expirado mostra tela genérica de link inválido; após confirmar mostra "Presença confirmada".

## 8. Riscos e dependências

- **Regressão na máquina de estados**: `Atualizar()` é usado tanto por Editar quanto por Reagendar (mesmo endpoint/payload). A lógica de detecção de mudança precisa comparar valores **antes** de sobrescrevê-los — erro comum é zerar o status sempre. CA3 e CA6 cobrem isso.
- **Lembrete duplicado**: zerar `lembrete_por_email_enviado` em reagendamento de última hora pode redisparar lembrete fora de janela — mitigado pela cláusula `inicio_previsto >= NOW()` do handler de lembretes. CA15 valida o caminho normal.
- **Confusão de evento**: o e-mail informativo (Fase 1) e o e-mail com link (Fase 2) são disparos distintos; na Fase 2 o informativo pode ser substituído pelo e-mail com link. Decisão de implementação: Fase 2 **estende** o e-mail da Fase 1 incluindo o link (não cria disparo duplicado).
- **Dependência WhatsApp**: bloqueada por ausência de integração — não é dependência desta entrega, vai para discovery.
- **Dependência Fase 2 → `imedto-database`**: migration de colunas de token + tabela de audit de acesso. Acionar o DB agent.

## 9. Observações para execução

- **Não-negociável**: regra de reset e detecção de mudança **no Domain** (`Agendamento.Atualizar`), com espelho de UX no front. 422 do `BusinessException` é a fonte da verdade.
- **Reuso obrigatório**: clonar o padrão de **Termos** na Fase 2 — `TermoEmitido.GerarTokenUrlSafe`, `TermoPublicoController` (410 genérico, rate limit, `[AllowAnonymous]`, resolução de IP/UserAgent), `TermoEmitidoAcessoLog` (audit), `EnviarEmailTermoLinkEventHandler` (degradação graciosa de e-mail). Não inventar mecânica nova.
- **Reuso de e-mail**: `IEmailService` (SES ativo) + novos métodos em `EmailTemplates`. Seguir política de não logar destinatário/assunto/corpo.
- **Front Fase 2**: clonar `AceiteTermoPublicoView` (rota anônima já existente no router) para a página de confirmação.
- **Liberdade técnica do dev/db**: nome exato do evento de domínio, nomes de colunas/tabela de audit, política de rate limit (reusar "termos-publico" ou criar dedicada), TTL exato do token (respeitando "nunca além de `InicioPrevisto`", default 7 dias).
- **Confirmar com o DB**: índice parcial em `token_confirmacao` e índice em `agendamento_id` da tabela de audit.

## 10. Atualização de documentação

Parte da entrega (atualizar **antes** do hand-off ao dev / no mesmo PR validado pelo QA):

- **`Docs/ARQUITETURA.md`** — adicionar seção "Máquina de estados do Agendamento" (análoga à "Máquina de estados da receita" existente em §194): estados `Agendado / Confirmado / Cancelado / Concluido`, transições válidas, e a regra nova de **reset `Confirmado → Agendado` ao reagendar** (mudança de horário ou profissional), incluindo o evento de domínio e o disparo de e-mail desacoplado. Documentar o link público de confirmação (Fase 2) como transição `Agendado → Confirmado` por canal anônimo via token. Mudança incremental, cirúrgica.
- **`Docs/Discoverys/whatsapp-envio/01_discovery.md`** (nova subpasta) — documentar: hoje **não há integração de envio de WhatsApp** (só checkbox visual `detalhes.lembreteWA`, agora desabilitado); o que seria necessário para habilitar — provider (Meta WhatsApp Cloud API / Twilio / Z-API), credenciais e onde guardá-las (SSM), custo por mensagem, template HSM aprovado, opt-in LGPD do titular, e impacto nos fluxos de lembrete/confirmação. **Não implementar agora** — apenas registrar viabilidade.
- **`Docs/LGPD.md`** (se a Fase 2 introduzir nova tabela de audit de acesso público) — registrar a tabela de log de acesso ao link público de confirmação como novo ponto de coleta de dado operacional (IP/UserAgent, sem PII do paciente) e sua política de retenção, alinhada à de Termos. Avaliar na Fase 2; se idêntico ao já documentado para Termos, basta referência cruzada.
