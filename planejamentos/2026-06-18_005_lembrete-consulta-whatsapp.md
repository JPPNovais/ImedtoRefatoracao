# Lembrete de consulta via WhatsApp (MVP — espelha o lembrete por e-mail)

**ID**: 2026-06-18_005
**Status**: Aprovado por usuário em 2026-06-18 (todas as decisões nos defaults)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: agendamento (job de lembrete por e-mail — não pode regredir), permissionamento (`automacao_config`), prontuário/paciente (novo campo de opt-in + audit), nenhuma no financeiro/orçamento/estoque

## 1. Contexto e motivação

O lembrete de consulta por e-mail já existe e roda em produção: o `AutomacaoJob` (BackgroundService, `PeriodicTimer` de 1h) chama `EnviarLembretesAgendamentosCommandHandler`, que lê agendamentos numa janela configurável por estabelecimento (`configuracoes_automacao.horas_antecedencia_lembrete`), envia o e-mail e marca `agendamentos.lembrete_por_email_enviado = true`. O canal de e-mail tem baixa taxa de abertura no público de clínica; o WhatsApp é o canal onde o paciente realmente lê o lembrete e reduz no-show.

Esta demanda adiciona o **WhatsApp como canal complementar do MESMO lembrete** — não cria mecanismo paralelo, não substitui o e-mail. É o primeiro (e único) evento WhatsApp do MVP. Confirmação de agendamento, cancelamento e mensagem avulsa ficam **fora** deste briefing (eventos futuros).

**Fronteira de fase (premissa do MVP):** o dono ainda não tem CNPJ. O provedor escolhido é a **Meta WhatsApp Cloud API** (oficial), consumida por **porta + adapter** (ports & adapters), espelhando o provider de e-mail. No modo teste/sandbox da Meta, o envio real só chega aos **até 5 números cadastrados como teste** no app da Meta. A virada para produção (Business Verification, número verificado, template aprovado em produção, volume ilimitado) é destravada **somente por configuração/credenciais**, sem reescrita de código — ver §3 "Não inclui" e CA de fronteira (CA17/CA18).

## 2. Persona-alvo

- **Dono / Admin do estabelecimento** (quem tem `automacao_config`): liga o canal WhatsApp na tela de Automação, no mesmo lugar do toggle de e-mail. Frequência: configuração pontual.
- **Recepção / quem cadastra/edita paciente**: marca o opt-in "autoriza receber lembretes por WhatsApp" no cadastro do paciente. Frequência: por paciente.
- **Paciente** (titular): recebe o lembrete no WhatsApp no momento da janela de antecedência configurada. Não opera o sistema — é o destinatário.
- **Admin global do Imedto**: configura as credenciais da Meta (token, Phone Number ID, WABA ID) via appsettings/SSM. Não é fluxo de tela; é infra/config.

## 3. Escopo

**Inclui**:
- Porta de domínio `IWhatsappService` (Application/Domain), com adapter `MetaWhatsappService` (Infrastructure, Meta Cloud API) e `NoOpWhatsappService` (loga, não envia), selecionados por config `Whatsapp:Provider` via factory condicional no `Container.cs` — espelho exato do padrão de `IEmailService` (`Email:Provider`).
- Toggle "Enviar lembretes também por WhatsApp" na MESMA tela de Automação (`AutomacoesView.vue`), no mesmo bloco "Lembretes de consulta", sob a MESMA permissão `automacao_config` (back: `[RequiresPermissaoExtra(PermissoesExtras.AutomacaoConfig)]`; front: route extra `automacao_config`). Persistido em `configuracoes_automacao` (nova coluna `lembretes_whatsapp_habilitados`).
- Campo de opt-in no paciente: checkbox "Autoriza receber lembretes por WhatsApp", com gravação de consentimento (data/hora + quem registrou) e audit trail.
- Extensão do job de lembrete existente (`EnviarLembretesAgendamentosCommandHandler`): para cada lembrete devido, além do e-mail (comportamento atual intocado), enviar **também** WhatsApp **quando** o estabelecimento habilitou o canal **E** o paciente tem opt-in marcado **E** telefone em formato válido (E.164, com DDD). Controle de envio independente por canal (nova coluna `agendamentos.lembrete_por_whatsapp_enviado`, espelhando `lembrete_por_email_enviado`).
- Template Meta categoria **Utility** (texto aprovado em §6), com variáveis preenchidas pelo handler. Mensagem **sempre identifica o estabelecimento no corpo** (multi-tenant correto).
- Acionamento das migrations pelo `imedto-database` (ver §5).

**Não inclui (fase pós-CNPJ — destravada só por config/credenciais, sem reescrita de código)**:
- Business Verification e número de produção verificado da Meta.
- Aprovação do template em produção (no MVP usa o número/template de teste do sandbox Meta).
- Volume/destinatários ilimitados (no MVP, envio real só para os até 5 números de teste cadastrados na Meta).
- Número de envio por estabelecimento (no MVP é número único do Imedto; o corpo identifica a clínica).

**Não inclui (eventos/briefings futuros)**:
- Confirmação de agendamento, cancelamento e mensagem avulsa por WhatsApp.
- Recebimento de respostas do paciente (inbound), status de entrega/leitura (webhooks de delivery), opt-out por resposta.
- Lembrete in-app (sininho `Notificacao`) — **não é o caminho**; o lembrete segue pelo job, igual ao e-mail (ver §9).

## 4. Regras de negócio

- **R1 — Canal complementa, não substitui.** O envio de e-mail permanece exatamente como hoje (não pode regredir). O WhatsApp é um envio adicional, independente, dentro do mesmo loop do job. Falha de um canal não impede o outro. Mora em: Handler (`EnviarLembretesAgendamentosCommandHandler`). Validada em: back (lógica do handler) + teste de integração.
- **R2 — Gating do canal WhatsApp.** Só envia WhatsApp se **todas** forem verdadeiras: (a) `configuracoes_automacao.lembretes_habilitados = true` (mesmo gate que já liga o lembrete); (b) `configuracoes_automacao.lembretes_whatsapp_habilitados = true`; (c) paciente com opt-in WhatsApp marcado; (d) telefone do paciente em formato E.164 válido (com DDD). Faltando qualquer uma → o canal WhatsApp é **pulado em silêncio** para esse paciente (sem PII em log), e o e-mail segue normalmente. Mora em: Handler (predicado no SQL/no loop). Validada em: back + teste.
- **R3 — Opt-in é pré-condição absoluta (LGPD).** Sem opt-in marcado, o sistema **nunca** envia WhatsApp, mesmo com telefone válido e canal habilitado. O opt-in é consentimento explícito do titular para tratamento (envio a terceiro — Meta). Mora em: Domain (campo no `Paciente`) + Handler (gate). Validada em: back + front (checkbox + estado).
- **R4 — Consentimento auditado.** Marcar/desmarcar o opt-in grava data/hora do consentimento + quem registrou (`usuario_id`) em audit trail, reusando o padrão de audit de paciente existente (`paciente_acesso_log` / `IPacienteAcessoLogService`, `TipoAcessoPaciente.Escrita`). Mora em: Handler de salvar paciente. Validada em: back + teste.
- **R5 — Mensagem identifica o estabelecimento (multi-tenant).** O corpo do template SEMPRE contém o nome do estabelecimento de origem do agendamento (`{{nome_estabelecimento}}`). O paciente sabe de qual clínica veio o lembrete. Nenhum dado de outro tenant transita. Mora em: Handler (montagem das variáveis a partir do agendamento, já filtrado por `estabelecimento_id`). Validada em: back + teste.
- **R6 — Idempotência por canal.** Após envio WhatsApp bem-sucedido, marca `agendamentos.lembrete_por_whatsapp_enviado = true`. O reagendamento já reseta `lembrete_por_email_enviado = false` em `Agendamento.Atualizar()` (R1/R6 do briefing 2026-06-02_001) — o novo flag de WhatsApp deve ser **resetado no mesmo ponto**, para que o reagendamento volte a notificar nos dois canais. Mora em: Domain (`Agendamento.Atualizar`) + Handler. Validada em: back + teste de regressão de reagendamento.
- **R7 — Falha de envio não derruba o job.** Falha do adapter Meta (erro de rede, 4xx/5xx, template indisponível) → `LogWarning` **sem PII** (apenas hash do destinatário, espelhando `ResendEmailService`), **não** marca `lembrete_por_whatsapp_enviado = true` (permite retry na próxima rodada), **não** relança, e **não** impede o e-mail nem os demais pacientes do lote. Mora em: Adapter + Handler (try/catch por paciente). Validada em: back + teste.
- **R8 — Telefone validado e normalizado a E.164.** O telefone do paciente é armazenado digits-only hoje (`Paciente.Telefone`, sanitizado). Para envio WhatsApp, o handler/adapter normaliza para E.164 (DDI Brasil +55 quando ausente, com DDD). Telefone que não compõe um E.164 válido → paciente pulado no canal WhatsApp (R2.d). Mora em: Handler/Domain (helper de normalização). Validada em: back + teste com telefones inválidos.
- **R9 — Provider atrás de porta (ports & adapters).** O Handler conhece apenas `IWhatsappService`; nunca o SDK/URL/payload da Meta. Trocar de provedor = trocar só o adapter, sem tocar Handler nem Domain. DI registra a implementação ativa por `Whatsapp:Provider`. Mora em: Application (porta) + Infrastructure (adapter). Validada em: back (revisão de diff — nenhum tipo da Meta fora de Infrastructure).

## 5. Modelo de dados

> Acionar `imedto-database` (o dev solicita a migration). Toda alteração é multi-tenant-aware e idempotente em DDL.

- **`configuracoes_automacao`** (por estabelecimento — já existe, é a tabela do toggle de e-mail):
  - Nova coluna `lembretes_whatsapp_habilitados boolean NOT NULL DEFAULT false`.
- **`pacientes`** (já tem `estabelecimento_id`, `telefone`, soft-delete e audit):
  - Nova coluna `whatsapp_lembrete_opt_in boolean NOT NULL DEFAULT false`.
  - Nova coluna `whatsapp_lembrete_opt_in_em timestamptz NULL` (data/hora do consentimento).
  - Nova coluna `whatsapp_lembrete_opt_in_por_usuario_id uuid NULL` (quem registrou). Sem FK rígida obrigatória — segue o padrão de rastreabilidade já usado em criação de cobrança/pagamento.
  - **Atenção LGPD/anonimização:** o job `anonimizar-pacientes-inativos` zera `Telefone`. Avaliar zerar/resetar o opt-in junto (consentimento sem telefone é inerte) — registrar a decisão no PR; sem schema novo, só comportamento.
- **`agendamentos`** (já tem `lembrete_por_email_enviado`, `estabelecimento_id`):
  - Nova coluna `lembrete_por_whatsapp_enviado boolean NOT NULL DEFAULT false` (espelho exato do flag de e-mail).
- **Índice:** a query do job já filtra por janela/estado; avaliar com `imedto-database` se o predicado adicional (`lembrete_por_whatsapp_enviado = false`) exige ajuste de índice. Provavelmente o índice existente da query de lembrete cobre — confirmar via EXPLAIN.
- **Sem nova tabela de log de envio WhatsApp** no MVP (delivery/leitura é fase futura). O audit do MVP é só o do **consentimento** (opt-in), em `paciente_acesso_log` (R4).

## 6. UX e fluxo

**Tela de Automação (`AutomacoesView.vue`) — bloco "Lembretes de consulta"** (onde já vive o toggle de e-mail):
- Abaixo do toggle "Lembretes de consulta" e dos campos de antecedência/e-mail remetente, adicionar um toggle "Enviar lembretes também por WhatsApp", visível e habilitado **somente quando** `lembretesHabilitados = true` (espelha o padrão de `bloco-campos` que só aparece com o lembrete ligado).
- Texto auxiliar (`<small>`): "Pacientes recebem por e-mail e, se autorizarem o WhatsApp e tiverem telefone válido, também por WhatsApp." Reusar a classe `.desc`/`.campo` já existentes na view. Sem novo componente de DS necessário (toggle nativo já é o padrão da view).
- Estados: salvar com sucesso → reusa `msg-sucesso`; erro → `msg-erro`; persistência via `automacaoService.salvarConfiguracao` (DTO estendido).

**Cadastro/edição de paciente:**
- Checkbox "Autoriza receber lembretes por WhatsApp" no formulário do paciente, próximo ao campo de telefone (proximidade semântica: opt-in depende de telefone). Usar `AppField`/`AppLabel` ou o padrão de checkbox já existente no formulário de paciente (o dev confirma o componente).
- Texto auxiliar: "Com a autorização e um telefone válido (com DDD), o paciente recebe os lembretes de consulta também pelo WhatsApp."
- Estado desabilitado/dica: se o telefone estiver vazio/inválido, o opt-in pode ficar marcável mas o sistema simplesmente não enviará (R2) — não bloquear a marcação (decisão: simplicidade; o gate real é no envio). O dev pode exibir aviso suave, opcional.

**Sem tela nova.** Tudo entra em telas existentes. Mobile-ready herda dos formulários atuais.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz, dois canais):** Dado um estabelecimento com lembrete por e-mail E por WhatsApp habilitados, e um paciente com e-mail, telefone E.164 válido e opt-in WhatsApp marcado, e um agendamento `Confirmado` dentro da janela de antecedência, Quando o `AutomacaoJob` roda, Então o paciente recebe o e-mail (como hoje) E o WhatsApp via `IWhatsappService`, e o agendamento fica com `lembrete_por_email_enviado = true` E `lembrete_por_whatsapp_enviado = true`.
- **CA2 (e-mail não regride):** Dado um estabelecimento com lembrete por e-mail habilitado e WhatsApp **desabilitado**, Quando o job roda, Então o paciente recebe o e-mail normalmente e **nenhum** envio WhatsApp ocorre; `lembrete_por_whatsapp_enviado` permanece `false`.
- **CA3 (sem opt-in → nunca envia WhatsApp — LGPD):** Dado um estabelecimento com WhatsApp habilitado e um paciente com telefone válido mas **sem opt-in**, Quando o job roda, Então o paciente recebe só o e-mail; nenhum WhatsApp é enviado; nada com PII é logado.
- **CA4 (sem telefone válido → pulado no WhatsApp):** Dado um paciente com opt-in marcado mas telefone vazio ou fora do padrão E.164, Quando o job roda, Então o canal WhatsApp é pulado em silêncio (sem PII em log) e o e-mail segue normalmente.
- **CA5 (multi-tenant — corpo identifica o estabelecimento):** Dado um agendamento do estabelecimento A, Quando o WhatsApp é enviado, Então o corpo contém o nome do estabelecimento A em `{{nome_estabelecimento}}` e nenhuma variável é preenchida com dado de outro tenant.
- **CA6 (isolamento entre tenants):** Dado um usuário do estabelecimento B operando a tela de Automação, Quando ele salva o toggle de WhatsApp, Então ele afeta apenas `configuracoes_automacao` do tenant B; jamais lê/escreve config do A (filtro `estabelecimento_id` via `ICurrentTenantAccessor`).
- **CA7 (RBAC — quem liga o toggle):** Dado um usuário **sem** a permissão extra `automacao_config`, Quando tenta abrir/salvar a tela de Automação, Então o back retorna 403 (`[RequiresPermissaoExtra(PermissoesExtras.AutomacaoConfig)]`) e o front oculta/bloqueia o acesso à rota (route extra `automacao_config`). Mesmo gate do toggle de e-mail — sem permissão nova.
- **CA8 (consentimento auditado — LGPD):** Dado que a recepção marca/desmarca o opt-in WhatsApp de um paciente, Quando salva, Então `pacientes.whatsapp_lembrete_opt_in`, `..._opt_in_em` e `..._opt_in_por_usuario_id` são gravados, e 1 linha de **Escrita** é registrada em `paciente_acesso_log` via `IPacienteAcessoLogService` (best-effort; falha do audit não bloqueia o salvar).
- **CA9 (sem PII em log de envio):** Dado qualquer envio WhatsApp (sucesso ou falha), Quando o adapter/handler loga, Então o log contém **apenas o hash SHA-256 truncado do destinatário** (espelho de `ResendEmailService`/`NoOpEmailService`) — nunca telefone, nome, e-mail ou corpo da mensagem.
- **CA10 (falha de entrega não derruba o lote):** Dado um lote com 3 pacientes elegíveis e o adapter Meta falhando (rede/4xx/5xx) no 2º, Quando o job roda, Então o 1º e o 3º recebem o WhatsApp, o 2º **não** marca `lembrete_por_whatsapp_enviado` (retry na próxima rodada), e o job conclui sem exceção propagada; os e-mails dos 3 são enviados normalmente.
- **CA11 (template indisponível):** Dado o adapter Meta retornando erro de template inexistente/não aprovado, Quando o job tenta enviar, Então trata como falha de envio (CA10): `LogWarning` sem PII, não marca enviado, não relança.
- **CA12 (idempotência — não reenvia):** Dado um agendamento já com `lembrete_por_whatsapp_enviado = true`, Quando o job roda de novo na mesma janela, Então **não** reenvia WhatsApp para ele.
- **CA13 (reagendamento reabre os dois canais):** Dado um agendamento `Confirmado` com ambos os flags de lembrete `true`, Quando o agendamento é reagendado (muda `InicioPrevisto`/`FimPrevisto`/profissional), Então `Agendamento.Atualizar()` reseta **ambos** `lembrete_por_email_enviado = false` E `lembrete_por_whatsapp_enviado = false`, e o status volta para `Agendado` (regra existente do briefing 2026-06-02_001 preservada).
- **CA14 (provider por config — NoOp em dev):** Dado `Whatsapp:Provider` vazio ou sem credenciais, Quando o DI resolve `IWhatsappService`, Então usa `NoOpWhatsappService` (loga hash, não chama a Meta) — espelho do fallback NoOp do e-mail. Dado `Whatsapp:Provider = "meta"` com credenciais, Então usa `MetaWhatsappService`.
- **CA15 (ports & adapters — sem vazamento de SDK):** Dado o diff da entrega, Quando se inspeciona `EnviarLembretesAgendamentosCommandHandler` e o `Agendamento`/`Paciente`, Então nenhum tipo, URL ou payload da Meta aparece fora de `Infrastructure/` — só a porta `IWhatsappService`.
- **CA16 (estado vazio/sem elegíveis):** Dado um estabelecimento com WhatsApp habilitado mas nenhum paciente elegível (todos sem opt-in ou sem telefone), Quando o job roda, Então nenhum WhatsApp é enviado, sem erro e sem log com PII.
- **CA17 (fronteira teste — envio real só para números de teste):** Dado o modo teste/sandbox da Meta configurado (número e template de teste), Quando o job envia, Então a entrega real chega **apenas** aos até 5 números cadastrados como teste na Meta; números fora da lista de teste retornam erro do provedor, tratado como falha de envio (CA10) — sem quebrar o fluxo. Este comportamento é **esperado** no MVP e deve estar documentado.
- **CA18 (fronteira CNPJ — virada só por config):** Dado que a entrega está em produção no modo teste, Quando o dono obtiver CNPJ e a Meta aprovar número/template de produção, Então a virada para produção exige **apenas** trocar credenciais/config (`Whatsapp:*` no SSM) — **nenhuma** mudança em Domain, Handler, contrato ou migration. (Validável por inspeção: confirmar que volume/número/template são lidos de config, não hardcoded.)
- **CA19 (gate tipográfico — front):** Dado o CSS scoped novo da view de Automação e do form de paciente, Quando `npm run check:typography -- --ci` roda, Então passa (sem `font-size`/`font-weight` literais — usar tokens; CLAUDE.md §5).
- **CA20 (suíte verde + smoke local de job):** Dado a entrega completa, Quando QA roda NUnit + Vitest + lint + typecheck + build E **dispara o job localmente** (botão "Enviar lembretes agora" ou execução direta) com um número de teste da Meta cadastrado, Então a suíte passa **E** o QA confirma no banco/log local que o WhatsApp real chegou ao número de teste e os flags foram marcados — validação local obrigatória antes do push (jobs sem tela exigem conferir o efeito real, não só suíte verde).

## 8. Riscos e dependências

- **Regressão do e-mail (alto):** a alteração mais sensível é tocar `EnviarLembretesAgendamentosCommandHandler` e `Agendamento.Atualizar()`. O e-mail e o reset de reagendamento **não podem** mudar de comportamento. CA2 e CA13 são os guardrails — exigir teste de regressão de reagendamento (já existe `AgendamentoReagendamentoTests`).
- **Credenciais da Meta no SSM (dependência de infra):** o adapter Meta exige token + Phone Number ID + WABA ID. No MVP (modo teste), são as credenciais de sandbox da Meta. **Pré-requisito de INFRA** antes do smoke local do QA — registrar os parameters no SSM (e em `appsettings.Development.json` gitignored para o dev). Sem isso, o adapter cai em NoOp e o smoke real (CA17/CA20) não acontece.
- **Advisory lock do job em prod (validação local):** o `AutomacaoJob` roda no mesmo banco da EC2. Para o smoke local sem colidir com o backend de prod, isolar conforme CLAUDE.md (parar o backend de prod durante o teste ou usar tenant/job de teste, confirmando nos logs que foi o backend local que processou).
- **Limite de 5 números de teste (fronteira):** no MVP, qualquer número fora da allowlist da Meta falha — esperado (CA17). Não tratar como bug.
- **Dependência de schema:** `imedto-database` precisa entregar as 3 alterações (§5) antes do hand-off ao QA. Migration idempotente em DDL (raw `Sql` + `IF NOT EXISTS`) — vide gotcha de deploy com schema aplicado fora da pipeline.

## 9. Observações para execução

- **Caminho do lembrete = job, NÃO sininho.** O lembrete WhatsApp segue **exatamente** o caminho do e-mail: o `AutomacaoJob` chama `EnviarLembretesAgendamentosCommandHandler`. O canal in-app (`Notificacao`/`NotificacaoCriadaEvent`/SignalR) é o sininho do app e **não** é usado aqui. Não introduzir evento de domínio novo nem fan-out por permissão para este lembrete.
- **Reuso obrigatório:** porta espelha `IEmailService`; factory espelha o switch `Email:Provider` em `Container.cs`; logging espelha o hash SHA-256 truncado de `ResendEmailService`/`NoOpEmailService`; flag de controle espelha `lembrete_por_email_enviado`; audit de consentimento reusa `IPacienteAcessoLogService` (sem tabela nova). Antes de criar qualquer helper de normalização de telefone, `grep` por equivalente.
- **Não-negociável:** R3 (sem opt-in nunca envia), R5 (corpo identifica o estabelecimento), R7/CA9 (sem PII em log — só hash), CA2 (e-mail não regride), CA13 (reagendamento reseta os dois flags), CA18 (virada de produção só por config).
- **Liberdade técnica do dev:** forma exata da normalização E.164; se o gate do WhatsApp entra no predicado SQL da query do job ou no loop em C# (desde que cubra R2 com performance — preferir filtrar no SQL o que dá, como já faz o e-mail); nome exato do helper; layout fino do checkbox de opt-in.
- **Texto do template (Meta, categoria Utility) — fonte da verdade do corpo:**
  > "Olá, {{nome_paciente}}! Lembrete da sua consulta na {{nome_estabelecimento}}: {{tipo_servico}} com {{nome_profissional}} em {{data_hora}}. Em caso de dúvida, entre em contato com o estabelecimento."
  Variáveis na ordem: nome do paciente, nome do estabelecimento, tipo de serviço, profissional, data/hora (formatada em horário de Brasília via `ToBrasilia()`, como o e-mail já faz).

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — na seção "Bounded Context: Agendamentos", registrar o novo provider WhatsApp (porta `IWhatsappService` + adapter `MetaWhatsappService` + `NoOpWhatsappService`, selecionados por `Whatsapp:Provider`, espelhando `IEmailService`) e o **canal WhatsApp do lembrete** dentro do mesmo job (`EnviarLembretesAgendamentosCommandHandler`), com a fronteira teste-vs-produção (virada só por config). **Atualizado nesta entrega.**
- **`Docs/LGPD.md`** — nova seção registrando o(s) novo(s) dado(s) pessoal(is): consentimento (opt-in) para WhatsApp (campos + audit em `paciente_acesso_log`) e o **uso do telefone do paciente para envio a terceiro (Meta)**, com base no consentimento explícito (opt-in), minimização (sem PII em log — só hash), e a interação com a anonimização LGPD. **Atualizado nesta entrega.**
- `Docs/INFRA.md` — os parameters SSM da Meta (`Whatsapp:*`) são pré-requisito de infra; o `imedto-database`/dev registra o detalhe de SSM/SG em INFRA.md **quando** os parameters forem criados (não como parte deste briefing de produto — é ação de quem provisiona). Sinalizado aqui como dependência (§8).
- `Docs/DESIGN.md` — **nenhum** componente novo de design system; toggle/checkbox reusam padrões existentes da view. Sem atualização.
