# Confidencialidade da evolução e do prontuário — leitura autor-ou-dono (sigilo entre profissionais)

**ID**: 2026-06-27_001
**Status**: Aprovado por usuário em 2026-06-27
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: prontuário, documentos clínicos (receitas/atestados/pedidos de exame), anexos, relatório/timeline, mobile, audit/LGPD

> **Premissa primordial (CLAUDE.md §"Confidencialidade clínica entre profissionais")**: o conteúdo de uma evolução de prontuário e **tudo vinculado a ela** (anexos, fotos, receitas, atestados, pedidos de exame, termos) só pode ser **lido/baixado** por **(a)** o profissional **autor** daquela evolução (`ProntuarioEvolucao.AutorUsuarioId`) e **(b)** o **Dono** do estabelecimento. Médicos do mesmo estabelecimento são autônomos e **não compartilham prontuário entre si**. Este briefing **implementa** essa premissa no backend (fonte da verdade), com espelho de UX em web e mobile. **Exceção:** alertas clínicos seguem regra própria já existente (não tocar — ver §Escopo/Não inclui).

---

## 1. Contexto e motivação

Hoje, qualquer usuário do estabelecimento que abre o prontuário de um paciente vê **todas** as evoluções, documentos clínicos e anexos — independentemente de quem os criou. Isso viola o sigilo médico entre profissionais autônomos que dividem a mesma clínica: o Dr. B não deveria enxergar a evolução, a receita ou a foto clínica que o Dr. A registrou em seu atendimento.

A investigação do código confirmou que **nenhuma query de leitura filtra por autor hoje** — todas filtram só por `estabelecimento_id` + `paciente_id` (multi-tenant), mas não por `autor_usuario_id`/`profissional_usuario_id`. As colunas de autoria já existem e já são selecionadas (para exibir o nome), mas não restringem a visibilidade.

Esta é a **prioridade/primordial** do épico. O Briefing 2026-06-27_002 (upload de anexos e fotos) **consome** a regra de acesso definida aqui.

## 2. Persona-alvo

- **Profissional (médico autônomo)**: atende seus pacientes, registra evoluções/documentos/fotos. Deve ver **apenas o que ele mesmo criou** naquele paciente. As evoluções dos colegas são invisíveis para ele.
- **Dono do estabelecimento**: tem visão completa de todo o prontuário (governança/responsabilidade clínica da clínica). Vê tudo de todos os profissionais.
- **Recepção / demais papéis**: **nunca** veem evolução/documento/anexo clínico (já é a postura de minimização; reforçada aqui via falha-fechada).

Momento da jornada: atendimento e pós-consulta (abrir prontuário, ler timeline de evoluções, baixar documentos/anexos).

## 3. Escopo

**Inclui** (gating de leitura autor-ou-dono em TODAS as superfícies de leitura/download):

- **Evoluções do prontuário**: timeline completa (`ObterDoPacienteGated`), listagem paginada (`ListarEvolucoesPaginadas`) e contagem (`ContarEvolucoes`).
- **Documentos clínicos**: lista unificada de documentos (`DocumentoQueryRepository.ListarDoPaciente` — UNION de receitas/atestados/pedidos de exame), lista de receitas (`ReceitaQueryRepository.ListarDoPaciente`) e a **leitura/download individual** de cada documento (`ObterReceitaQuery`, `ObterAtestadoQuery`, `ObterPedidoExameQuery`).
- **Anexos** (entidade `ProntuarioAnexo`): listagem (`ListarAnexosDoProntuarioQueryHandlers`), emissão de URL individual (`ObterUrlAnexoQueryHandlers`) e em lote (`ObterUrlsAnexosQueryHandler`) — gating pelo autor da **evolução à qual o anexo está vinculado**.
- **Exposição de `AutorUsuarioId`** nos DTOs de evolução/documento/anexo para o front e o mobile espelharem a UX (já existe em `EvolucaoDto`; avaliar nos demais).
- **Espelho de UX em web e mobile**: ocultar itens de outros autores na timeline, nas abas de documentos e nas listas de anexos. Falha-fechada nos dois clientes.
- **Índices de banco** para o novo filtro por autor não degradar performance (via `imedto-database`).
- **Atualização de `Docs/LGPD.md`** com o checklist de leitura autor-ou-dono.

**Não inclui**:

- **Alertas clínicos** (`pacientes.alertas`) — **mantêm a regra própria já existente** (Dono + profissional com vínculo de atendimento, inclusive primeira consulta), porque segurança do paciente prevalece sobre o sigilo entre médicos. **Não tocar** em `ObterDoPacienteGated` no que se refere ao gating de alertas (`VerificarVinculoAtendimento`, flag `PodeGerirAlertas`). Detalhado em `Docs/LGPD.md §Alertas clínicos` e briefing 2026-06-22_002.
- **Escrita** de evolução/documento/anexo — a "consulta atual" do próprio médico continua igual (ele é o autor; vê o que está montando). Sem mudança de regra de escrita.
- **Termos de consentimento**: já têm fluxo próprio. A premissa cobre termos vinculados à evolução, mas a aplicação prática do gating de termos **fica fora deste briefing** (não há, hoje, query de leitura de termo por terceiro que vaze conteúdo de colega de forma equivalente; se o QA identificar superfície de leitura de termo que vaze, vira spec gap/addendum). Foco aqui: evoluções + documentos + anexos.
- **Painel admin global**: já tem isolamento próprio (ver `Docs/LGPD.md §Acesso de admin global`). Não é alvo.

## 4. Regras de negócio

- **R1 — Predicado de visibilidade (fonte única conceitual)**: um registro clínico é visível ao solicitante se, e somente se, `solicitante.papel == Dono` **OU** `autor_do_registro == solicitante.usuarioId`. "Autor do registro" = `autor_usuario_id` (evolução) / `profissional_usuario_id` (receita/atestado/pedido de exame) / autor da evolução vinculada (anexo com evolução) / `criado_por_usuario_id` do próprio anexo (anexo órfão — ver R7). Mora em: **Query Repository (WHERE) + Handler (gate)**. Validada em: **back (fonte da verdade) + front/mobile (UX espelho)**.

- **R2 — Falha-fechada**: se a query/handler não tem o `usuarioId` e o `papel` do solicitante (claim ausente, papel não-reconhecido como Dono nem como o autor), o retorno é **vazio** (lista) ou **"não encontrado" genérico** (registro individual). Nunca "abre" por falta de informação. Mora em: **Handler + Repository**. Validada em: **back**.

- **R3 — Papel transita do controller até a query**: o `ICurrentTenantAccessor.Papel` (string "Dono"/"Profissional") + `UsuarioId` (Guid) são propagados pelos endpoints até as queries que hoje não os recebem. Onde a query nova precisa de papel, o contract ganha `SolicitantePapel` (enum `TenantPapel`) + `SolicitanteUsuarioId` (Guid). Espelha o que `ObterProntuarioDoPacienteQuery` já faz. Mora em: **Controller → Query (contract) → Handler → Repository**.

- **R4 — Dono vê tudo**: quando `papel == Dono`, o predicado de autor é **bypassado** (vê todos os registros do paciente naquele tenant). Mora em: **Repository (WHERE condicional) / Handler**. Validada em: **back**.

- **R5 — Mensagem genérica em acesso negado**: profissional que tenta ler/baixar registro de colega recebe **"não encontrado"** (não "sem permissão") — não vaza a existência do registro nem o nome do colega autor. Espelha o estilo defense-in-depth de `ObterUrlAnexoQueryHandlers`/`ObterReferenciaAnexo` (retorna null → 404/422 genérico). Mora em: **Handler/Repository**. Validada em: **back**. (Sem PII na mensagem — ver R8.)

- **R6 — Contagem coerente com a lista**: `ContarEvolucoes` aplica o **mesmo** predicado de R1, senão a paginação/contador exibe "12 evoluções" enquanto a lista mostra 3 (vaza a existência das 9 do colega). Mora em: **Repository + Handler + Controller (passa papel)**.

- **R7 — Visibilidade do anexo (COALESCE de autoria)**: a visibilidade do anexo é decidida pela **autoria efetiva**, escolhida conforme haja ou não evolução vinculada:
  - **Anexo COM evolução** (`evolucao_id` preenchido) → visível ao **autor da evolução** (`prontuario_evolucoes.autor_usuario_id`) **OU** ao **Dono**.
  - **Anexo ÓRFÃO** (`evolucao_id IS NULL` — legado ou pré-evolução) → visível a **quem fez o upload** (`prontuario_anexos.criado_por_usuario_id`) **OU** ao **Dono**.
  - Conceitualmente: `autoria_efetiva = COALESCE(autor_da_evolução_vinculada, criado_por_do_anexo)`; visível se `autoria_efetiva == solicitante OR papel == Dono`. Implementação via JOIN condicional em `prontuario_evolucoes` (quando há `evolucao_id`) com fallback para `criado_por_usuario_id` quando órfão.
  Mora em: **ProntuarioAnexoQueryRepository (JOIN + WHERE) + handlers de anexo**. Validada em: **back**.

- **R8 — Audit e log sem PII**: cada leitura/emissão de URL continua registrando em `prontuario_acesso_log` (`Leitura`/`Exportacao`) com `{usuario_id, paciente_id, estabelecimento_id, timestamp}` — **inclusive quando o acesso é negado** o log não pode conter nome do colega autor nem conteúdo clínico. Mora em: **Handler (IProntuarioAcessoLogService)**. Validada em: **back**. (Best-effort, falha não bloqueia — padrão atual.)

- **R9 — UX espelha o back (não substitui)**: web e mobile ocultam itens de outros autores **porque o back já não os retorna**. O front não implementa o predicado por conta própria como única defesa — ele apenas reflete o que veio. Se um item de colega vazar no payload, é bug de back (Tipo A), não se "esconde no front". Mora em: **Front/Mobile (render) + Back (fonte)**.

## 5. Modelo de dados

**Sem tabela nova. Sem coluna nova.** As colunas de autoria já existem (confirmado):

| Tabela | Coluna de autor | Tipo |
|---|---|---|
| `prontuario_evolucoes` | `autor_usuario_id` | `uuid` (Guid, não-nullable) |
| `receitas` | `profissional_usuario_id` | `uuid` (Guid) |
| `atestados` | `profissional_usuario_id` | `uuid` (Guid) |
| `pedidos_exame` | `profissional_usuario_id` | `uuid` (Guid) |
| `prontuario_anexos` | (herda via `evolucao_id` → `prontuario_evolucoes.autor_usuario_id`) | — |

**Índices (necessita `imedto-database`)** — hoje há índices por `(prontuario_id, criada_em)` / `(paciente_id, ...)` mas **não** por autor; o novo filtro por autor faria seq scan/filtro pós-índice em pacientes com muitas evoluções/documentos. Avaliar e criar (via `CREATE INDEX CONCURRENTLY`, migration idempotente):

- `prontuario_evolucoes (prontuario_id, autor_usuario_id)` — cobre timeline/lista/contagem gated do profissional.
- `prontuario_anexos (evolucao_id)` — cobre o JOIN do gating de anexo COM evolução (confirmar se já existe; a investigação não localizou índice de anexos).
- `prontuario_anexos (criado_por_usuario_id)` — cobre o caminho de anexo ÓRFÃO (`evolucao_id IS NULL`, gating por uploader). O DB agent decide via `EXPLAIN` se vale índice dedicado ou um composto.
- `atestados (paciente_id, profissional_usuario_id)` e `pedidos_exame (paciente_id, profissional_usuario_id)` — filtro por autor na lista do paciente.
- `receitas`: já existe `ix_receitas_estab_prof_emitida (estabelecimento_id, profissional_usuario_id, emitida_em)`; o DB agent avalia se cobre o predicado da lista de paciente ou se precisa de `(paciente_id, profissional_usuario_id)`.

> **Decisão de índice é do `imedto-database`** após `EXPLAIN ANALYZE` no estado real (não cravar no briefing qual índice exato — só a necessidade e os padrões de acesso).

**LGPD**: dado de saúde (Art. 11). Audit já existe; minimização mantida (DTO só com campos da tela). A regra **aumenta** a confidencialidade — nunca expõe mais que hoje.

## 6. UX e fluxo

Sem tela nova. A mudança é **subtrativa** (some o que era de colega):

- **Prontuário web** (`ProntuarioView.vue`, `EvolucaoTimelineItem.vue`, `ConsultasAnterioresTab.vue`): a timeline e a aba de consultas anteriores listam só as evoluções do solicitante (ou todas, se Dono). Contador coerente.
- **Abas de documentos web** (Receitas / Atestados / Pedidos de Exame): listam só os documentos do solicitante (ou todos, se Dono). Botão de visualizar/baixar de documento de colega não aparece (e o back nega se forçado).
- **Mobile** (`mobile/src/views/ProntuarioView.vue` + tipos): idem, falha-fechada.
- **Estados**:
  - **Vazio** (profissional sem evoluções suas neste paciente, mesmo havendo de colegas): `AppEmptyState` com texto neutro — ex.: "Nenhuma evolução registrada por você para este paciente." **Não** revelar que há evoluções de colegas.
  - **Dono**: vê tudo, sem aviso especial.
- **Tipografia**: usar tokens (premissa CLAUDE.md §5). Nenhum literal de `font-size`/`font-weight` novo. Gate `npm run check:typography -- --ci` verde.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — autor vê o seu)**: Dado o Dr. A autenticado como Profissional, que registrou 2 evoluções no paciente P, Quando abre o prontuário de P, Então vê as 2 evoluções dele, os documentos/anexos por ele criados, e a contagem exibe "2".

- **CA2 (isolamento entre profissionais — evolução)**: Dado o Dr. B (Profissional) no mesmo estabelecimento, sem nenhuma evolução própria em P, mas com 3 evoluções do Dr. A em P, Quando o Dr. B abre o prontuário de P, Então a timeline vem **vazia** (0 evoluções), a contagem retorna **0**, e o payload do back **não contém** nenhuma evolução do Dr. A (verificável via network/DTO).

- **CA3 (isolamento — documento individual)**: Dado o Dr. B, Quando tenta `GET` da receita/atestado/pedido de exame cujo `profissional_usuario_id` é do Dr. A (ID conhecido), Então recebe **"não encontrado" genérico** (404/422), sem revelar existência nem autor, e o acesso negado é auditado **sem PII** do Dr. A.

- **CA4 (isolamento — anexo via URL)**: Dado o Dr. B, Quando chama `ObterUrlAnexoQuery` para um anexo vinculado a evolução do Dr. A, Então recebe **"não encontrado" genérico** e **nenhuma presigned URL** é emitida; em lote (`ObterUrlsAnexos`), os anexos do Dr. A são silenciosamente omitidos do resultado.

- **CA5 (Dono vê tudo)**: Dado o Dono autenticado, Quando abre o prontuário de P (com evoluções de A e B), Então vê **todas** as evoluções, todos os documentos e todos os anexos, contagem coerente com o total, e consegue baixar qualquer documento/anexo.

- **CA6 (anexo órfão — Dono ou uploader)**: Dado um anexo com `evolucao_id IS NULL` (legado/pré-evolução), Quando o Dr. A que **fez o upload** (`criado_por_usuario_id == Dr. A`) tenta listar/obter URL, Então **vê** o anexo órfão; Quando o Dr. B (que **não** subiu e não é Dono) tenta, Então **não vê** (negação genérica); Quando o Dono lista/obtém URL, Então **vê** o anexo órfão.

- **CA7 (recepção/outros papéis — falha-fechada)**: Dado um usuário com papel Recepção (ou papel não-Dono/não-autor), Quando acessa qualquer superfície de leitura de evolução/documento/anexo, Então recebe **vazio**/"não encontrado", sem conteúdo clínico no payload. (Onde o endpoint já exige `[RequiresPapel(Profissional, Dono)]`, recepção nem chega; onde não exige, o predicado falha-fechada garante.)

- **CA8 (contagem coerente — multi-tenant + autor)**: Dado o Dr. B com 0 evoluções próprias em P, Quando o front pede `ContarEvolucoes`, Então retorna **0** (não o total real do paciente), evitando vazar a existência das evoluções do Dr. A pela paginação.

- **CA9 (multi-tenant preservado)**: Dado um usuário do estabelecimento Y, Quando tenta acessar evolução/documento/anexo de paciente do estabelecimento X (mesmo sendo "Dono" em Y), Então recebe "não encontrado" genérico — o filtro `estabelecimento_id` **precede** o gating por autor e nada é logado com PII do tenant alheio.

- **CA10 (consulta atual do próprio médico — regressão)**: Dado o Dr. A conduzindo a consulta atual de P, Quando registra a evolução, Então ele a vê normalmente (ele é o autor) e nada na nova regra quebra o fluxo de escrita/registro.

- **CA11 (alertas clínicos — NÃO regredir)**: Dado o Dr. B sem evolução própria em P, mas atendendo P agora (agendamento com check-in), Quando abre o cabeçalho do prontuário, Então **continua vendo os alertas clínicos** de P (regra própria de alertas, inalterada) — ainda que **não veja** as evoluções/documentos/anexos do Dr. A. (Confirma que o gating de evolução não vazou para os alertas.)

- **CA12 (UX espelha back — web)**: Dado o Dr. B, Quando abre o prontuário de P (só com conteúdo do Dr. A), Então a timeline mostra `AppEmptyState` com texto neutro, as abas de documentos vêm vazias, e nenhum item/contador de colega aparece na tela.

- **CA13 (UX espelha back — mobile)**: Dado o Dr. B no app mobile, Quando abre o prontuário de P (só com conteúdo do Dr. A), Então as listas vêm vazias com falha-fechada, sem item de colega.

- **CA14 (audit em acesso negado — sem PII)**: Dado o Dr. B tentando ler documento do Dr. A, Quando o acesso é negado, Então (se houver registro de tentativa) o `prontuario_acesso_log` não contém nome do Dr. A, conteúdo clínico nem mensagem que vaze autoria; e o acesso bem-sucedido do autor/Dono continua sendo auditado.

- **CA15 (performance — sem regressão)**: Dado um paciente com 1.000 evoluções (mistura de vários autores), Quando o Dr. A (autor de 50) abre o prontuário/lista paginada, Então a query usa índice por autor (sem seq scan — verificável via `EXPLAIN ANALYZE` pelo DB agent) e a resposta mantém tempo aceitável.

- **CA16 (documentação viva)**: Dado o merge desta feature, Quando se consulta `Docs/LGPD.md`, Então há a seção "Confidencialidade da evolução — leitura autor-ou-dono" com o checklist (quem lê, predicado, falha-fechada, exceção de alertas, audit).

- **CA17 (gate tipográfico)**: Dado o build, Quando roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight`.

## 8. Riscos e dependências

- **Risco — vazar pela contagem/paginação**: se `ContarEvolucoes` não receber o predicado, o total denuncia a existência de evoluções de colega. Mitigado por R6/CA8.
- **Risco — regredir alertas**: o gating de evolução é mais restrito que o de alertas; aplicar o predicado de evolução por engano aos alertas tiraria informação de segurança do paciente. Mitigado por CA11 e pela separação explícita (não tocar `VerificarVinculoAtendimento`/`PodeGerirAlertas`).
- **Risco — performance**: filtro por autor sem índice → seq scan em prontuários grandes. Mitigado por §5 (índices) + CA15. **Dependência: `imedto-database`**.
- **Risco — superfícies esquecidas**: a regra é transversal; uma query de leitura não mapeada vaza. O QA deve varrer todas as superfícies que retornam evolução/documento/anexo (incluindo PDF de prontuário/receita, se listar conteúdo). Áreas regressivas: prontuário, documentos, anexos, relatório/timeline, mobile.
- **Dependência**: o Briefing 2026-06-27_002 (upload anexos/fotos) **consome** esta regra — implementar este primeiro (ordem recomendada).

## 9. Observações para execução

**Não-negociável**:
- Predicado autor-ou-dono no **backend** (WHERE + gate de handler) é a fonte da verdade. Front/mobile só espelham.
- Mensagem genérica "não encontrado" em acesso negado; nunca "sem permissão" nem nome do colega.
- **Não tocar** no gating de alertas clínicos.
- Multi-tenant (`estabelecimento_id`) precede o gating por autor.

**Pontos de implementação confirmados (caminhos reais — para destravar o dev)**:
- `ProntuarioQueryRepository.cs`:
  - `ObterDoPacienteGated` (linhas ~29-162): já recebe `solicitanteUsuarioId` + `papel`. Adicionar `AND (e.autor_usuario_id = @SolicitanteUsuarioId OR @Papel = 'Dono')` no SQL da timeline de evolução (`AutorUsuarioId`/`AutorNome` já são selecionados; `EvolucaoDto` já expõe `AutorUsuarioId`).
  - `ListarEvolucoesPaginadas` (~209-282) e `ContarEvolucoes` (~288-306): **mudar assinatura** para receber `Guid solicitanteUsuarioId` + `TenantPapel papel`; aplicar o predicado no WHERE.
- Contracts/handlers de evolução: `ListarEvolucoesProntuarioPacienteQuery` já tem `SolicitanteUsuarioId` (falta `SolicitantePapel`); `ContarEvolucoesProntuarioPacienteQuery` **não tem nenhum dos dois** — adicionar ambos. Handlers `ListarEvolucoesProntuarioPacienteQueryHandlers` e `ContarEvolucoesProntuarioPacienteQueryHandlers` repassam ao repo.
- `ProntuarioController.cs`: o endpoint de contagem (`contagem-evolucoes`) **não passa papel** hoje — passar `_tenant.UsuarioId` + papel (parse de `_tenant.Papel`). Endpoint de listar evoluções passa `UsuarioId` mas precisa passar o papel também.
- Documentos: `DocumentoQueryRepository.ListarDoPaciente` (~37-183, UNION) + `ReceitaQueryRepository.ListarDoPaciente` (~44-105) — adicionar `AND (profissional_usuario_id = @SolicitanteUsuarioId OR @Papel = 'Dono')` em cada ramo do UNION e na lista de receitas; propagar papel pelos contracts/handlers (`ListarDocumentosDoPacienteQuery` já tem `SolicitanteUsuarioId`, falta papel) e pelo `PacienteController.ListarDocumentos`.
- Leitura/download individual: `ObterReceitaQueryHandlers`, `AtestadoQueryHandlers.ObterAtestado`, `PedidoExameQueryHandlers.ObterPedidoExame` — gate autor-ou-dono antes de retornar/gerar PDF (negação = "não encontrado" genérico).
- Anexos: `ProntuarioAnexoQueryRepository` (`ListarDoProntuario`, `ObterReferenciaAnexo`, `ObterReferenciasAnexos`) — aplicar o **COALESCE de autoria** (R7): LEFT JOIN em `prontuario_evolucoes pe ON pe.id = a.evolucao_id` e predicado `((a.evolucao_id IS NOT NULL AND pe.autor_usuario_id = @SolicitanteUsuarioId) OR (a.evolucao_id IS NULL AND a.criado_por_usuario_id = @SolicitanteUsuarioId) OR @Papel = 'Dono')`. Ou seja: usa `autor_usuario_id` da evolução quando há evolução; usa `criado_por_usuario_id` do anexo quando órfão; Dono sempre passa. Handlers `ListarAnexosDoProntuarioQueryHandlers`, `ObterUrlAnexoQueryHandlers`, `ObterUrlsAnexosQueryHandler` passam a receber/propagar papel. (Estes handlers são também tocados pelo Briefing 2; coordenar para não conflitar — a **regra de acesso** é deste briefing.)
- DTOs: garantir `AutorUsuarioId` exposto onde o front/mobile precisarem espelhar (evolução já tem; avaliar item de documento/anexo — só expor se a UX precisar, senão manter minimização).

**Liberdade técnica**: forma exata de passar o predicado (parâmetro booleano `ehDono` vs comparação de papel no SQL), nomes de parâmetros Dapper, organização dos testes. Reusar o estilo já presente em `ObterDoPacienteGated`.

**Reuso > duplicação**: o predicado é o mesmo conceito em ~9 pontos — considerar um helper compartilhado (ex.: método/const SQL ou um value object de "filtro de visibilidade clínica") para não duplicar a condição literal e evitar divergência. Decisão do dev/senior, mas registrar para o QA verificar consistência.

**Testes**: cobrir CA2/CA3/CA4/CA5/CA6/CA8/CA9 com testes de integração que batem no banco real (não mock) — o gating é SQL; suíte mockada não pega WHERE errado. Cenário canônico: Dr. A, Dr. B e Dono sobre o mesmo paciente.

## 10. Atualização de documentação

- **`Docs/LGPD.md`** — **adicionar** seção "Confidencialidade da evolução e do prontuário — leitura autor-ou-dono (briefing 2026-06-27_001)", logo após/junto da seção "Alertas clínicos — visibilidade restrita". Conteúdo: o predicado (Dono OU autor), as superfícies cobertas (evoluções, documentos, anexos), falha-fechada, mensagem genérica, audit sem PII, e a **distinção explícita** vs. a regra de alertas (alertas são mais amplos por segurança clínica; evolução é mais restrita por sigilo entre médicos). Atualizar também o `Checklist multi-tenant`/requisitos se fizer sentido referenciar o novo gating. Mudança **incremental, cirúrgica**.
- **`CLAUDE.md`** — já contém a premissa (linha ~127, §Confidencialidade clínica entre profissionais). **Não reescrever**; apenas conferir coerência com o que for implementado.
- **`Docs/ARQUITETURA.md`** — só se o dev introduzir um padrão reutilizável novo (ex.: helper de "filtro de visibilidade clínica"). Se for um helper relevante e cross-cutting, registrar 1 parágrafo. Caso contrário, **nenhum**.
