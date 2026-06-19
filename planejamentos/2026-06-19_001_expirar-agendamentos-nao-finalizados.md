# Expirar agendamentos não finalizados — job recorrente de madrugada (D-1)

**ID**: 2026-06-19_001
**Status**: Aprovado por usuário em 2026-06-19
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: agenda/calendário (novo estado de status), filtros/contagens por status, relatórios que exibem status, jobs recorrentes (single-leader/advisory lock)

## 1. Contexto e motivação

Hoje a agenda acumula agendamentos "presos" no passado em status `Agendado` ou `Confirmado` — o atendimento nunca foi marcado como `Concluido` nem `Cancelado` (recepção esqueceu de dar baixa, paciente faltou e ninguém registrou, etc.). Esses registros poluem contagens, relatórios e a visão de agenda: um horário de ontem que continua aparecendo como "Confirmado" sugere falsamente que há um atendimento pendente, e infla métricas de ocupação/realização.

Marcar manualmente esses agendamentos como `Cancelado` seria errado em dois sentidos: (a) operacionalmente é trabalho repetitivo e esquecível; (b) semanticamente, `Cancelado` implica uma ação deliberada (com motivo, possível repasse de horário, notificação) — e usar `Cancelado` para "ninguém deu baixa" mistura faltas/esquecimentos com cancelamentos reais, corrompendo qualquer relatório de cancelamento.

A demanda cria um **estado novo e neutro — `Expirado`** — aplicado **automaticamente** por um job recorrente que roda **1x de madrugada (03:00 BRT)** e varre apenas os agendamentos do **dia anterior (D-1)** que ficaram em aberto. A premissa é **custo zero**: reusar a infraestrutura de jobs já existente (`JobScheduler`, single-leader via advisory lock) e não publicar eventos de notificação (nada de spam para paciente/profissional por uma higienização em massa).

## 2. Persona-alvo

Indireta — ninguém aciona a feature manualmente. Os beneficiários são:
- **Recepção / dono / gerente**: veem a agenda e os relatórios limpos, sem registros velhos travados em "Confirmado".
- **Sistema** (job automático): roda sozinho, global, todas as madrugadas, para todos os estabelecimentos.

Não há tela, configuração nem opt-in. É comportamento de plataforma (global, padrão), não uma preferência por estabelecimento.

## 3. Escopo

**Inclui**:
- Novo valor `Expirado` no enum de domínio `AgendamentoStatus` (hoje: `Agendado`, `Confirmado`, `Cancelado`, `Concluido`).
- Novo método de domínio `Agendamento.ExpirarPorFimDoDia(motivo)` que move o agendamento para `Expirado` **sem publicar evento** (à prova de futuro contra spam de notificação em massa).
- Novo job recorrente implementando `IJobHandler`, registrado em DI (`Container.cs`) e em `JobsRegistrados.cs` com intervalo de 86400s (1x/dia), seed de `proximo_run_em` para **06:00 UTC (= 03:00 BRT)**.
- Varredura **cross-tenant** (job global) que seleciona, por janela de D-1 em fuso Brasília, agendamentos em `Agendado` **ou** `Confirmado` e os expira **em lote** chamando o método de domínio por item.
- Gravação do **motivo** na coluna existente `motivo_cancelamento` com o texto: `"Expirado automaticamente — não finalizado até o fim do dia"`.
- Log **agregado por execução** `{ estabelecimento_id, quantidade, timestamp }`, **sem PII** (sem nome/id de paciente individual).
- **Frontend**: renderização do novo estado `Expirado` em agenda/calendário, filtros de status, badges e relatórios — com **label PT-BR** e **cor própria**; nenhuma tela que filtra/conta por status pode quebrar ao encontrar o valor `Expirado`.
- **DB** (`imedto-database`): índice parcial para a query de varredura + seed do job em `jobs_agendados`.

**Não inclui**:
- Qualquer tela de configuração, opt-in ou toggle por estabelecimento (a feature é **global** por decisão de produto).
- Varredura de "todo o passado em aberto" — a janela é estritamente **D-1** (ver risco assumido em §8).
- Publicação de `AgendamentoCanceladoEvent` ou qualquer evento de notificação no caminho de expiração.
- Notificar paciente/profissional sobre a expiração (e-mail, WhatsApp, sino).
- Mover `Concluido` ou `Cancelado` para `Expirado` (apenas `Agendado` e `Confirmado` são elegíveis).
- Reabrir/"desexpirar" um agendamento expirado (não há fluxo de undo nesta entrega).
- Qualquer mudança no `AutomacaoJob` (legado, sem advisory lock — **não** usar).

## 4. Regras de negócio

- **R1 — Elegibilidade por status.** Apenas agendamentos em `Agendado` **ou** `Confirmado` são expirados. `Concluido` e `Cancelado` são **terminais** e nunca são tocados (evita marcar atendimento real/cancelamento real como expirado). Mora em: Query de seleção (filtro `status IN ('Agendado','Confirmado')`) + Domínio (`ExpirarPorFimDoDia` só transiciona a partir desses dois estados; chamar em estado terminal é no-op seguro/guard). Validada em: backend (fonte da verdade).

- **R2 — Janela = somente D-1 em fuso Brasília.** A coluna `inicio_previsto` é `timestamptz` (UTC). "Ontem" é definido em **BRT**: do **início de ontem 00:00 BRT** (inclusive) até o **início de hoje 00:00 BRT** (exclusivo), convertido para UTC na query. Agendamentos de **hoje** e **futuros** nunca são tocados; agendamentos de **anteontem ou antes** **não** são varridos nesta execução (ver R-risco em §8). Mora em: Query/Handler do job (cálculo da fronteira em `America/Sao_Paulo`). Validada em: backend.

- **R3 — Expiração é silenciosa (sem evento).** O método `Agendamento.ExpirarPorFimDoDia(motivo)` **não** publica `AgendamentoCanceladoEvent` nem enfileira evento de notificação. É deliberadamente distinto de `Cancelar(motivo)` (que dispara evento). Razão: uma varredura noturna pode expirar centenas de registros de uma vez — publicar eventos geraria spam massivo de notificação. Mora em: Domínio (`Agendamento.cs` — novo método sem `AddDomainEvent`). Validada em: backend (teste verifica que nenhum evento é coletado após a expiração).

- **R4 — Motivo gravado na coluna existente.** O texto `"Expirado automaticamente — não finalizado até o fim do dia"` é gravado na coluna **existente** `motivo_cancelamento` (decisão explícita de reuso — não se cria coluna nova para isso). Isso dá rastreabilidade por agendamento sem schema extra. Mora em: Domínio (`ExpirarPorFimDoDia` seta `motivo_cancelamento` e `status`). Validada em: backend.

- **R5 — Job é global e cross-tenant, mas o efeito é por estabelecimento.** O job **não** recebe nem filtra por um único `estabelecimento_id` (é uma rotina de plataforma que varre todos os tenants). Ainda assim, o efeito é registrado **por estabelecimento** no log agregado e cada agendamento permanece vinculado ao seu `estabelecimento_id` original — nenhuma linha é reatribuída entre tenants, e nenhuma query do job devolve dado de um tenant para outro consumidor (o job não tem "consumidor" multi-tenant; ele só escreve). Mora em: Handler do job (varredura cross-tenant intencional, documentada). Validada em: backend (a varredura não embaralha `estabelecimento_id`; cada agendamento mantém o seu).

- **R6 — Processamento em lote via Domínio (nunca UPDATE em massa).** A varredura processa em **batches (~200 por vez)** e, para cada item, chama o **método de domínio** `ExpirarPorFimDoDia` (respeitando guard de status, audit de `atualizado_em`, motivo). **Proibido** `UPDATE agendamentos SET status='Expirado'...` em massa que burle o Domain. Mora em: Handler do job. Validada em: backend.

- **R7 — Idempotência e segurança de re-execução.** Rodar o job duas vezes na mesma madrugada (ou re-disparar manualmente) **não** causa erro nem dupla transição: agendamentos já em `Expirado` não estão no conjunto elegível (`status IN ('Agendado','Confirmado')`), então são ignorados. O job é seguro para re-rodar. O single-leader (advisory lock do `JobScheduler`) garante que apenas uma instância processa por vez. Mora em: Query (filtro de elegibilidade) + `JobScheduler` (lock). Validada em: backend.

- **R8 — Novo estado é terminal para esta entrega.** `Expirado` não tem fluxo de saída automático nesta entrega (não vira `Agendado`/`Confirmado` de novo). É tratado nas telas como um estado final neutro, lado a lado com `Cancelado`/`Concluido`. Mora em: Domínio + Front (enum/label/cor). Validada em: front (renderização) + backend (sem transição de saída).

## 5. Modelo de dados

- **Tabela afetada**: `agendamentos`. **Nenhuma coluna nova.**
  - `status` é `varchar(20)` — o valor `'Expirado'` (8 chars) cabe sem migration de tipo e **não** há CHECK/enum no banco a alterar (o enum é de aplicação). **Confirmar via `imedto-database`** que não existe constraint CHECK em `status` que precise incluir `'Expirado'`; se existir, a migration deve estendê-la idempotentemente.
  - `motivo_cancelamento` (existente) recebe o texto de expiração (R4).
  - `atualizado_em` (existente) é tocado na transição — é a trilha de auditoria por agendamento (efeito auditável sem audit table dedicada).
  - `inicio_previsto` (`timestamptz`, UTC) é a base da janela D-1 (R2).

- **Índice (acionar `imedto-database`)**: os índices atuais lideram por `estabelecimento_id`, ineficientes para uma varredura **cross-tenant** por `inicio_previsto` + status. Pedir **índice parcial**:
  `CREATE INDEX ... ON agendamentos (inicio_previsto) WHERE status IN ('Agendado','Confirmado')` (via `CREATE INDEX CONCURRENTLY`, padrão do `imedto-database`). Isso mantém o índice pequeno (só linhas em aberto) e cobre exatamente o predicado do job. CA de performance depende disso.

- **Seed do job (acionar `imedto-database`)**: inserir/garantir o registro do job em `jobs_agendados` com intervalo 86400 e `proximo_run_em` semeado para a próxima ocorrência de **06:00 UTC**. Seguir o padrão dos jobs já registrados (idempotente — `ON CONFLICT DO NOTHING` ou equivalente já usado na suíte de seed de jobs).

- **PII/LGPD**: o log do job é **agregado** `{ estabelecimento_id, quantidade, timestamp }` — **sem** nome, id de paciente ou qualquer PII individual. O motivo gravado é um texto fixo, sem PII. Nenhum novo tipo de dado pessoal é exposto.

- **Multi-tenant**: cada agendamento mantém seu `estabelecimento_id`; o job não reatribui tenant nem expõe dado de um tenant a um consumidor de outro (R5).

## 6. UX e fluxo

Não há tela de acionamento (job de madrugada). O impacto de UX é a **renderização do novo estado `Expirado`** onde quer que o status apareça:

- **Agenda / calendário**: eventos `Expirado` renderizam com **cor própria** (token neutro/acinzentado, distinto de `Cancelado` em vermelho e `Concluido` em verde — escolha do dev/DS, registrar o token) e **label PT-BR "Expirado"**.
- **Filtros de status**: a lista de filtros passa a incluir `Expirado` como opção; selecioná-lo lista apenas expirados.
- **Badges de status**: o mapeamento status → {label, cor, ícone} ganha a entrada `Expirado`. **Não pode haver `default`/fallback que quebre** (tela em branco, "undefined", erro de console) ao encontrar o valor.
- **Relatórios que exibem status**: qualquer relatório/contagem que agrupe ou exiba por status deve reconhecer `Expirado` (contar na sua própria categoria — **não** somar em `Cancelado` nem em `Concluido`).

**Estados de UX a cobrir**:
- **Existente sem expirados**: nenhuma mudança visível.
- **Com expirados**: badge/cor/label corretos, filtro funcional.
- **Tela legada que itera status conhecidos**: deve degradar com elegância (mostrar "Expirado" como label, nunca crashar).

**Mobile-ready**: reusar os componentes de badge/filtro existentes da agenda (responsivos por padrão).

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — expira Agendado e Confirmado de D-1)**: Dado dois agendamentos de **ontem** (em fuso BRT), um em `Agendado` e outro em `Confirmado`, Quando o job de expiração roda, Então ambos passam a `Expirado`, recebem `motivo_cancelamento = "Expirado automaticamente — não finalizado até o fim do dia"` e têm `atualizado_em` atualizado.

- **CA2 (não toca terminais)**: Dado agendamentos de **ontem** em `Concluido` e em `Cancelado`, Quando o job roda, Então **nenhum** dos dois é alterado (status, motivo e `atualizado_em` inalterados).

- **CA3 (não toca hoje nem futuro)**: Dado agendamentos de **hoje** e de **amanhã** em `Agendado`/`Confirmado`, Quando o job roda (madrugada), Então **nenhum** é expirado — apenas a janela D-1 é varrida.

- **CA4 (fronteira de fuso BRT)**: Dado um agendamento com `inicio_previsto` em UTC que cai em **ontem às 23:30 BRT** (ou seja, já em UTC do "dia seguinte"), Quando o job roda, Então ele **é** expirado (a janela é calculada em `America/Sao_Paulo`, não em UTC); e dado um agendamento de **hoje 00:30 BRT**, Então ele **não** é expirado.

- **CA5 (silencioso — sem evento)**: Dado a expiração de N agendamentos, Quando o método `ExpirarPorFimDoDia` é chamado, Então **nenhum** `AgendamentoCanceladoEvent` (nem qualquer evento de notificação) é publicado/coletado — verificável inspecionando os domain events do agregado após a transição.

- **CA6 (multi-tenant cross-tenant sem vazamento)**: Dado agendamentos de **dois estabelecimentos distintos** (A e B), ambos em aberto em D-1, Quando o job global roda, Então cada agendamento é expirado mantendo seu `estabelecimento_id` original (nenhuma reatribuição), e o log agregado registra uma linha por estabelecimento com a contagem correta — sem misturar contagens entre tenants e sem expor dado de A para B.

- **CA7 (status `Expirado` renderiza no front — agenda)**: Dado um agendamento `Expirado`, Quando a agenda/calendário carrega, Então ele aparece com label "Expirado" e cor própria (token registrado), distinto de `Cancelado`/`Concluido`, **sem** erro de console nem fallback quebrado.

- **CA8 (filtros/contagens não quebram)**: Dado uma lista/relatório que filtra ou conta por status e que contém ao menos um `Expirado`, Quando renderiza, Então `Expirado` aparece como categoria própria (filtro selecionável; contagem separada de `Cancelado` e `Concluido`) e nenhuma tela que itera status quebra ao encontrar o novo valor.

- **CA9 (log sem PII)**: Dado a execução do job, Quando termina, Então o log contém apenas `{ estabelecimento_id, quantidade, timestamp }` por estabelecimento — **sem** nome, id de paciente ou qualquer PII individual.

- **CA10 (idempotência / re-execução segura)**: Dado que o job já rodou e expirou os elegíveis de D-1, Quando o job roda **de novo** (ou é re-disparado manualmente) na mesma janela, Então nenhum erro ocorre e nenhum agendamento é transicionado duas vezes (os já `Expirado` ficam fora do conjunto elegível); o resultado é o mesmo.

- **CA11 (performance — índice + lote)**: Dado a tabela `agendamentos` com volume relevante de registros (incluindo muitos terminais e poucos em aberto em D-1), Quando o job varre, Então a query de seleção usa o **índice parcial** `(inicio_previsto) WHERE status IN ('Agendado','Confirmado')` (verificável via `EXPLAIN`) e o processamento ocorre em **batches (~200)** chamando o método de domínio por item — sem `UPDATE` em massa e sem varredura sequencial da tabela inteira.

- **CA12 (job registrado e single-leader)**: Dado o `JobScheduler` em execução, Quando chega ~06:00 UTC (03:00 BRT), Então o job de expiração é selecionado a partir de `jobs_agendados` (intervalo 86400, `proximo_run_em` semeado), executa sob o advisory lock (single-leader) e atualiza `proximo_run_em` para o próximo dia — sem execução concorrente em múltiplas instâncias.

## 8. Riscos e dependências

- **Risco assumido — janela só D-1 (decisão de produto, documentar)**: se o job **falhar em rodar** numa madrugada (deploy, instância caída, lock travado), os agendamentos **daquele dia** não serão expirados em execuções futuras — a varredura é estritamente D-1, **não** "todo passado em aberto". Consequência: registros velhos podem permanecer em aberto se um dia for pulado. **Decisão de produto: aceitar este risco** nesta entrega (simplicidade > robustez de catch-up). Mitigação operacional futura (fora de escopo): um modo de catch-up ou varredura de "todo passado em aberto" pode ser um addendum se o problema se materializar. Registrado explicitamente para o QA não classificar como bug.

- **Risco — regressão de telas que iteram status**: introduzir um valor novo no enum pode quebrar `switch`/mapa de status no front que não tenha `default` seguro. CA7 e CA8 são os gates. Validar com o app rodando (agenda com um `Expirado` real no banco), não só suíte.

- **Risco — confusão semântica `Expirado` × `Cancelado`**: relatórios de cancelamento **não** podem somar expirados. CA8 cobre. Reforçar em qualquer agregação por status.

- **Dependência — `imedto-database`**: índice parcial (`CREATE INDEX CONCURRENTLY`) + seed do job em `jobs_agendados` + confirmação de que não há CHECK em `agendamentos.status` (ou estendê-la). Acionar antes do hand-off ao QA.

- **Dependência — infra de jobs existente**: `JobScheduler` (BackgroundService + `pg_try_advisory_lock` single-leader + retry/backoff) e `JobsRegistrados.cs`/`Container.cs`. **Não** usar `AutomacaoJob`.

- **Áreas regressivas a vigiar**: agenda/calendário (render de status), filtros e contagens por status, relatórios por status, suíte de jobs (seed/scheduler).

## 9. Observações para execução

**Não-negociável**:
- Novo valor `Expirado` no enum `AgendamentoStatus` (backend). Status `varchar(20)` comporta sem migration de tipo — **mas confirmar ausência de CHECK** via `imedto-database`.
- Método de domínio dedicado `Agendamento.ExpirarPorFimDoDia(motivo)` que **não** publica evento (R3) — distinto de `Cancelar(motivo)`.
- Job via `IJobHandler` + DI em `Container.cs` + `JobsRegistrados.cs` (intervalo 86400, seed `proximo_run_em` = 06:00 UTC). **Nunca** `AutomacaoJob`.
- Janela calculada em **`America/Sao_Paulo`** (BRT), não em UTC (R2/CA4).
- Processamento em **lote (~200)** chamando o método de domínio por item — **proibido** `UPDATE` em massa (R6).
- Motivo na coluna **existente** `motivo_cancelamento` com o texto exato (R4).
- Log **agregado, sem PII** (R5/CA9).
- Tipografia: ao tocar badges/labels de status no front, usar tokens (CLAUDE.md §5) — sem literais de `font-size`/`font-weight`.

**Liberdade técnica do dev**:
- Escolha do **token de cor** para `Expirado` (neutro/acinzentado sugerido, distinto de `Cancelado`/`Concluido`) — registrar no DS.
- Forma exata da query de seleção (Dapper de leitura para listar ids elegíveis em batch, depois carregar agregados pelo repositório de escrita para chamar o método de domínio) — desde que respeite R6 e o índice parcial.
- Onde mora o cálculo da fronteira BRT (helper de tempo/`TimeZoneInfo`), seguindo o padrão já usado em outros jobs/handlers.

**Reuso obrigatório (grep antes de criar)**: `JobScheduler`, `IJobHandler`, `JobsRegistrados`, padrão de seed de `jobs_agendados`, `AgendamentoStatus`, `Agendamento.Cancelar`/`Concluir` (referência de transição), componente de badge/filtro de status da agenda no front. Estender o enum e os mapas de status — **não** duplicar telas.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md` §Jobs / BackgroundService** — registrar o novo job recorrente de **expiração de agendamentos não finalizados** (1x/dia, 03:00 BRT / 06:00 UTC, single-leader via `JobScheduler`), citando que ele usa um **método de domínio silencioso** (`ExpirarPorFimDoDia`, sem evento) e processa em lote. Ajuste cirúrgico na lista de jobs/handlers, não reescrita da seção.
- **`Docs/DESIGN.md` §Status de agendamento** (se houver mapa documentado de status → label/cor) — adicionar a entrada `Expirado` (label "Expirado" + token de cor escolhido). Se não houver tabela de status documentada, registrar o token novo na seção de cores/estados.
- **`Docs/LGPD.md`** — **não requer mudança estrutural** (nenhum novo tipo de PII nem endpoint de PII). Se a seção de jobs/logs mencionar quais rotinas escrevem log, adicionar uma linha confirmando que o log da expiração é **agregado e sem PII** `{ estabelecimento_id, quantidade, timestamp }`.

**Quem atualiza**: `imedto-business-analyst` aponta aqui; a edição cirúrgica dos docs acompanha a entrega antes do hand-off ao QA (o QA valida no CA que o doc reflete o novo job/estado).
