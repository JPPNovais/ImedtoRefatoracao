# Central de Migração — Observabilidade de falha + motivos de rejeição no relatório (addendum)

**ID**: 2026-06-15_002
**Refere-se a**: 2026-06-15_001_central-de-migracao-mapeamento-por-ia.md
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: nenhuma de domínio (paciente/estoque/orçamento/prontuário intactos) — toca **só** o bounded context de migração (estados do job + relatório). Migration de schema necessária (1 coluna nova + 1 const de status).

> O épico Central de Migração (2026-06-15_001, CA1–CA24) está entregue em `main`. Este addendum corrige **duas lacunas observadas em produção** após a entrega, ambas incrementais. O briefing original permanece intocado e seus CAs continuam válidos. Os CAs deste addendum começam em **CA25** (o original terminou em CA24).

---

## 1. Contexto e motivação

Dois problemas reais, observados em produção hoje (2026-06-15):

**Problema 1 — job preso e mudo (motiva a Melhoria B).** Um job de migração (#6) subiu e ficou **preso em `aguardando_mapa`** sem nenhum feedback ao operador. Causa raiz: o job recorrente `inferir-mapa-migracao` ([InferirMapaMigracaoJobHandler.cs](../backend/src/Services/Imedto.Backend.Application/Migracao/Jobs/InferirMapaMigracaoJobHandler.cs):81-88) **captura a exceção, loga e NÃO marca falha** — a justificativa no código ("operador verá o job parado e poderá acionar manualmente") não se sustenta: **não há onde ver o motivo nem como acionar**. Pior: como o job é recorrente e re-seleciona o mesmo job a cada 30s, a inferência fica em **loop silencioso** (e queima chamadas de IA se a falha for intermitente). A falha provável foi `Ia:AnthropicApiKey` ausente no SSM (infra), mas **qualquer** falha (download S3, parser de arquivo, API de IA) deixa o job preso e mudo.

O mesmo padrão-vício existe na carga: `CarregarOnda1JobHandler` e `CarregarOnda2JobHandler` **re-lançam** a exceção inesperada ([CarregarOnda1JobHandler.cs](../backend/src/Services/Imedto.Backend.Application/Migracao/Jobs/CarregarOnda1JobHandler.cs):87-91) → o job fica preso em `migrando` e o recorrente re-seleciona o mesmo job indefinidamente. (Exceção legítima a preservar: a Onda 2 faz um `return` **sem mudar status** quando está bloqueada esperando a Onda 1 — isso **não é falha**, é espera, e não pode virar `falhou`.)

**Problema 2 — relatório não diz POR QUE registros não entraram (motiva a Melhoria C).** O relatório do admin ([MigracaoRevisaoView.vue](../frontend/src/modules/admin/views/MigracaoRevisaoView.vue):~370) mostra **só contagens** por entidade (criados/atualizados/rejeitados/pulados). O operador vê "37 rejeitados" e não tem como saber o motivo sem ir ao banco. Os motivos **já estão gravados** em `migracao_registros.motivo_rejeicao` (string genérica sem PII, por design — R3/CA4 do original) e o backend até já carrega uma lista `MotivosRejeicao` por entidade — mas é uma lista `Distinct()` **sem contagem** e **não é exposta na tela**. Falta agregar (motivo → quantidade) e mostrar.

**Benefício de negócio:** migração é o primeiro contato do cliente novo com o produto (onboarding). Job preso e mudo = operador travado e cliente sem migração; relatório sem motivo = operador não sabe o que pedir ao cliente para reenviar. Ambos atacam o atrito de portabilidade que é o diferencial da FASE 2B.

## 2. Persona-alvo

- **Operador Imedto (admin da plataforma)**, no painel `modules/admin`, durante uma migração. É quem hoje fica travado: vê o job parado sem motivo e não consegue agir, e vê o relatório sem entender as rejeições. Frequência: recorrente (toda nova migração que falha ou rejeita registros).

## 3. Escopo

**Inclui:**

**Melhoria B — falha visível e acionável do job:**
- Novo estado de job `falhou` + campo `motivo_falha` (genérico, sem PII) no aggregate `MigracaoJob`.
- Transição para `falhou` quando a **inferência de mapa** falha (qualquer etapa: download S3, parser, IA) e quando a **carga (Onda 1 ou Onda 2)** falha por exceção inesperada.
- O motivo de falha é uma **categoria legível mapeada da exceção** ("IA não configurada", "falha ao baixar arquivo", "arquivo corrompido/ilegível", "falha inesperada na carga") — **nunca** a mensagem técnica crua nem PII.
- Exibição do estado `falhou` + motivo na **fila** (`MigracoesListView`) e no **detalhe/revisão** (`MigracaoRevisaoView`).
- **Botão "Reprocessar"** que recoloca o job na fila do scheduler a partir da falha (ver D-B3 abaixo para a regra de transição). RBAC: reusa a policy `ImedtoAdmin` (toda a área já é admin-only).

**Melhoria C — motivos das rejeições agregados no relatório:**
- Agregar `migracao_registros.motivo_rejeicao` em **motivo → quantidade**, por entidade, dentro do tenant.
- Expor a quebra no `RelatorioMigracaoResult` / `RelatorioEntidadeResult` (back), no service e interface do front, e na tabela do relatório (`MigracaoRevisaoView`).
- Exibição em formato "CPF ausente: 12 · categoria não encontrada: 3 · paciente não identificado: 5" por entidade.

**Não inclui (registrar como backlog / fora deste addendum):**
- Retry automático com backoff do job que falhou. O reprocessar é **manual** (operador decide quando reprocessar — geralmente após corrigir a causa, ex. a chave da IA no SSM). Retry automático fica para um item futuro.
- Notificação ativa (sino/e-mail) ao operador quando um job falha. O operador vê na fila ao abrir o painel. Notificação proativa é backlog.
- Detalhamento por linha das rejeições (lista de cada registro rejeitado com seu motivo). Hoje o original já tem o conceito de motivo por registro no preview; este addendum agrega **contagem por motivo**, não a lista linha-a-linha no relatório final. Lista detalhada é backlog se o operador pedir.
- Configurar a chave da IA no SSM de produção — **dependência de INFRA** (fora do código). Ver §8.

## 4. Decisões de produto (fechadas neste addendum)

> Estas decisões foram tomadas pelo BA como padrão conservador (reuso, LGPD-safe, mínimo viável) e devem ser **confirmadas pelo usuário** na validação. São a base dos CAs.

- **D-B1 — Um único estado de falha (`falhou`), não um por etapa.** Em vez de `falha_inferencia` + `falha_carga`, um só `falhou`. O motivo legível (`motivo_falha`) carrega a etapa ("IA não configurada", "falha ao baixar arquivo", "falha inesperada na carga"). **Por quê:** mantém o enum enxuto, espelha o padrão do `rejeitado` que já existe (um estado, motivo no campo), e o operador entende a etapa pelo motivo. O reprocessar sabe de onde retomar pelo **status anterior** (ver D-B3).

- **D-B2 — Motivo de falha é categoria, nunca mensagem crua.** O handler mapeia o **tipo de exceção** para uma categoria legível fixa (tabela em R-B2). A mensagem técnica vai só para o log estruturado (já sem PII). **Por quê:** LGPD (mensagem genérica, sem PII) + não vazar stack/detalhe técnico de infra na UI do operador. Espelha CA4 do original.

- **D-B3 — Reprocessar retoma da etapa que falhou, via status anterior.** O job guarda de onde caiu (`status_antes_falha`). "Reprocessar" transiciona: se falhou na inferência → volta para `aguardando_mapa` (o recorrente `inferir-mapa-migracao` re-seleciona); se falhou na carga → volta para `migrando` (o recorrente `carregar-onda*` re-seleciona). **Por quê:** os jobs recorrentes já selecionam por status; basta recolocar o status correto e a máquina existente reprocessa — **zero infra nova**. Como a carga já é idempotente por registro (registros já `importado_*` não são reprocessados — só os `pendente`), reprocessar a carga é seguro.

- **D-B4 — `falhou` para o loop silencioso.** Ao marcar `falhou`, o job sai dos estados que os recorrentes selecionam (`aguardando_mapa` / `migrando`), então o loop de re-seleção a cada 30s **cessa** até o operador reprocessar. Isso é parte do valor: hoje o job preso é re-tentado mudamente para sempre.

- **D-C1 — Agregação é motivo → quantidade, por entidade, sem PII.** O relatório passa a expor, por entidade, um mapa `{ motivo: quantidade }`. Como `motivo_rejeicao` já é genérico por design (R3/CA4), a agregação nunca revela PII — só categorias. Substitui a `List<string> MotivosRejeicao` atual (lista sem contagem) por uma estrutura com contagem.

- **D-C2 — Pulados também ganham motivo agregado? Sim, no mesmo formato.** Hoje "pulados" (ex.: "identificador ausente", "agendamento já existe") também só aparecem como número. Pela mesma lógica e custo marginal, agregar motivo dos **pulados** junto. **Por quê:** o operador precisa saber por que N pacientes foram pulados tanto quanto por que foram rejeitados — é o mesmo tipo de informação acionável. (Se o usuário preferir manter só rejeitados nesta entrega, é corte trivial.)

## 5. Regras de negócio

**Melhoria B:**

- **R-B1 (transição de falha)**: O aggregate `MigracaoJob` ganha o estado `falhou` (const `StatusFalhou = "falhou"`), o campo `MotivoFalha` (string nullable, genérico) e o campo `StatusAntesFalha` (string nullable, para o reprocessar). Método `MarcarFalhou(string motivo)` válido **apenas** a partir de `aguardando_mapa` ou `migrando` (espelha onde as falhas ocorrem); guarda o status atual em `StatusAntesFalha` antes de transicionar. Mora em: `MigracaoJob` (Domain). Validada em: back.

- **R-B2 (mapeamento exceção → categoria legível, sem PII)**: Cada job handler (`InferirMapa`, `CarregarOnda1`, `CarregarOnda2`), no `catch` da exceção inesperada, mapeia para uma categoria fixa e chama `job.MarcarFalhou(categoria)`:
  | Sintoma | Categoria exibida |
  |---|---|
  | Chave/credencial de IA ausente ou inválida (ex.: config IA faltando) | `"IA não configurada"` |
  | Falha ao baixar o arquivo do S3 | `"falha ao baixar o arquivo"` |
  | Falha ao descompactar/parsear o arquivo | `"arquivo corrompido ou ilegível"` |
  | Falha inesperada na inferência de mapa | `"falha ao gerar o mapa"` |
  | Falha inesperada na carga | `"falha inesperada na carga"` |
  A mensagem técnica/stack vai **só** para o log estruturado (que já é sem PII). Nenhuma categoria contém nome de arquivo do cliente, valor de coluna, nem PII. Mora em: job handlers (Application). Validada em: back.

- **R-B3 (não confundir espera com falha)**: O `return` legítimo do `CarregarOnda2JobHandler` quando a Onda 1 ainda não concluiu (CA13) **não** marca `falhou` — continua sendo espera silenciosa que o scheduler re-tenta. Só exceção **inesperada** marca `falhou`. Mora em: `CarregarOnda2JobHandler`. Validada em: back.

- **R-B4 (reprocessar)**: Comando admin `ReprocessarMigracao(jobId)` válido **apenas** quando `status == falhou`. Transiciona o job de volta para `StatusAntesFalha` (`aguardando_mapa` ou `migrando`), limpa `MotivoFalha`, e o recorrente correspondente reprocessa. A carga é idempotente: só registros `pendente` são reprocessados (registros já `importado_criado`/`importado_atualizado` permanecem). RBAC: policy `ImedtoAdmin` (reuso). Mora em: command de reprocessar (Application) + método `Reprocessar()` no aggregate. Validada em: back + front (botão só aparece em `falhou`).

- **R-B5 (audit da transição de falha e do reprocessar)**: As transições `→ falhou` e `falhou → (status anterior)` são auditadas como qualquer transição do job (CA20 do original — {usuario_id quando houver, estabelecimento_id, job_id, status_anterior, status_novo, timestamp}, sem PII). A falha é disparada por job recorrente (sem usuário humano) → usuario_id pode ser nulo/sistema; o reprocessar carrega o admin_id. Mora em: trilha de transição existente. Validada em: back.

**Melhoria C:**

- **R-C1 (agregação motivo → quantidade por entidade, no tenant)**: A query de relatório (`ObterRelatorio` no `MigracaoRegistroRepository`) passa a agrupar, por entidade, `motivo_rejeicao` em `{ motivo: quantidade }` para os registros `rejeitado`, e `{ motivo: quantidade }` para os `pulado` (D-C2). A agregação roda **só sobre os registros do job** (que já carregam `estabelecimento_id` do tenant — multi-tenant herdado). Substitui o `List<string> MotivosRejeicao` por `Dictionary<string,int> MotivosRejeicao` (+ `Dictionary<string,int> MotivosPulo`). Mora em: repositório de leitura do relatório (Infrastructure) + Result DTO (Contracts). Validada em: back.

- **R-C2 (sem PII na agregação — herdada)**: A agregação nunca expõe PII porque `motivo_rejeicao`/motivo de pulo já são genéricos por design (R3/CA4 do original). Nenhum motivo agregado pode conter CPF, nome, telefone ou valor de coluna — é só a categoria do erro. Validada em: back + QA confere os valores possíveis.

## 6. Modelo de dados

**Toca apenas `migracao_jobs`** (staging multi-tenant existente):
- **Nova coluna** `motivo_falha` (`text`, nullable) — categoria genérica da falha, sem PII.
- **Nova coluna** `status_antes_falha` (`text`, nullable) — de onde o job caiu, para o reprocessar saber para onde voltar.
- **Novo valor de status** `falhou` (o `status` já é `text` livre no banco — não há CHECK enum a alterar; a const nova entra no domínio e no front). Confirmar com `imedto-database` se há constraint a ajustar.
- Sem alteração em `migracao_registros` — a Melhoria C **reusa** `motivo_rejeicao` já existente; é só agregação em query, **sem migration**.

**Migration**: 1 migration idempotente (`ADD COLUMN IF NOT EXISTS motivo_falha`, `ADD COLUMN IF NOT EXISTS status_antes_falha`). **Atenção ao gotcha conhecido**: `AddColumn` cru de EF quebra deploy se o schema for aplicado fora da pipeline — usar `Sql` com `IF NOT EXISTS` (padrão já adotado no projeto). Acionar `imedto-database`.

**Audit / LGPD**: `motivo_falha` é categoria genérica (R-B2), nunca PII. As transições de/para `falhou` entram na trilha de audit de transição existente (CA20), sem PII.

## 7. UX e fluxo

**Fila de migrações (`MigracoesListView`):**
- O mapa `STATUS_LABELS` ganha `falhou: "Falhou"`; `STATUS_VARIANT` ganha `falhou: "error"`. O badge de falha aparece na coluna de status (reuso de `AppBadge`, padrão já existente na view).

**Detalhe / revisão (`MigracaoRevisaoView`):**
- Novo card (ou ramo de `v-if`) para `statusJob === 'falhou'`: título "Migração falhou", `AppBadge variant="error"`, parágrafo com o **motivo legível** (`job.motivoFalha`), e botão **"Reprocessar"** (`AppButton variant="secondary"`, ícone `fa-rotate`). Estado de loading no botão durante o reprocessar; mensagem de erro genérica se o reprocessar falhar. Após sucesso, recarrega o job (volta a `aguardando_mapa`/`migrando` com o painel correspondente).
- **Relatório da carga** (card `concluido`/`concluido_com_erros`): a tabela por entidade ganha exibição dos motivos agregados abaixo (ou em coluna) de cada entidade — formato "motivo: N · motivo: N". Rejeições e pulos com suas quebras. Reusar a estética da tabela existente; mobile: o painel admin é desktop-first (aceitável, herdado do original).

**Estados a cobrir:** falha (novo), loading do reprocessar, sucesso do reprocessar (volta ao fluxo), relatório com motivos (com dados) e relatório sem rejeições (lista de motivos vazia → não renderiza a quebra).

**Service/DTO front:** `migracaoAdminService` ganha `reprocessar(jobId)`; `MigracaoJobAdminDto` ganha `motivoFalha: string | null`; `RelatorioEntidadeResult` ganha `motivosRejeicao: Record<string,number>` e `motivosPulo: Record<string,number>`.

## 8. Critérios de aceite (testáveis) — começam em CA25

**Melhoria B — falha visível e acionável:**

- **CA25 (inferência falha → `falhou` com motivo, sem loop)**: Dado um job em `aguardando_mapa` cuja inferência de mapa lança exceção (ex.: IA não configurada), Quando o job recorrente `inferir-mapa-migracao` processa, Então o job transiciona para `falhou` com `motivo_falha = "IA não configurada"` (categoria genérica, sem PII), o `status_antes_falha` guarda `aguardando_mapa`, e nas rodadas seguintes o recorrente **não re-seleciona** o job (loop cessa).

- **CA26 (carga falha → `falhou`)**: Dado um job em `migrando` cuja carga lança exceção inesperada, Quando `carregar-onda1-migracao` (ou onda2) processa, Então o job transiciona para `falhou` com `motivo_falha = "falha inesperada na carga"` e `status_antes_falha = migrando`; nenhuma exceção é re-lançada para o scheduler de forma a travar o job mudo.

- **CA27 (espera da Onda 2 NÃO é falha — R-B3)**: Dado um job de Onda 2 (prontuário) cujo tenant ainda tem Onda 1 ativa, Quando `carregar-onda2-migracao` processa, Então o job **permanece** no status atual (espera), **não** vai para `falhou`, e é reprocessado normalmente na rodada seguinte quando a Onda 1 concluir.

- **CA28 (motivo de falha sem PII — LGPD)**: Dado um job que falhou por qualquer causa, Quando o admin vê o detalhe, Então o `motivo_falha` é uma das categorias fixas (R-B2) e **não contém** nome de arquivo do cliente, valor de coluna, CPF, nome de paciente, stack trace nem mensagem técnica crua; a mensagem técnica aparece apenas no log estruturado (sem PII).

- **CA29 (fila e detalhe mostram falha)**: Dado um job em `falhou`, Quando o operador abre a fila de migrações, Então vê o badge "Falhou" (variant error); e quando abre o detalhe, Então vê o card de falha com o motivo legível e o botão "Reprocessar".

- **CA30 (reprocessar retoma da etapa correta — R-B4)**: Dado um job em `falhou` cujo `status_antes_falha = aguardando_mapa`, Quando o operador clica "Reprocessar", Então o job volta para `aguardando_mapa`, `motivo_falha` é limpo, e o recorrente de inferência o reprocessa. E dado `status_antes_falha = migrando`, Quando reprocessa, Então volta para `migrando` e a carga o reprocessa — processando **apenas** os registros `pendente` (os já `importado_criado`/`importado_atualizado` permanecem; nenhuma duplicação).

- **CA31 (reprocessar só em `falhou` — RBAC + estado)**: Dado um job que **não** está em `falhou`, Quando se chama o endpoint de reprocessar, Então recebe 422 (estado inválido) e nada muda. E dado um usuário sem a policy `ImedtoAdmin`, Quando chama o endpoint, Então recebe 403 e o botão "Reprocessar" não aparece no front.

- **CA32 (audit das transições de falha/reprocessar — R-B5)**: Dado um job que vai para `falhou` e depois é reprocessado, Quando cada transição ocorre, Então há linha de audit com {usuario_id (sistema na falha automática; admin_id no reprocessar), estabelecimento_id, job_id, status_anterior, status_novo, timestamp}, sem PII.

- **CA33 (multi-tenant — detalhe/reprocessar)**: Dado um operador/job, Quando o endpoint de reprocessar ou de detalhe resolve o job, Então usa o repositório admin existente (que já filtra/escopa por job) e nenhum dado de tenant alheio nem PII é exposto; mensagem de "não encontrado" genérica para job inexistente.

**Melhoria C — motivos das rejeições no relatório:**

- **CA34 (agregação motivo → quantidade — R-C1)**: Dado um job concluído com erros em que, na entidade `paciente`, 12 registros foram rejeitados por "CPF ausente" e 3 por "categoria não encontrada", Quando o relatório é gerado, Então `porEntidade["paciente"].motivosRejeicao` retorna `{ "CPF ausente": 12, "categoria não encontrada": 3 }` e a tela exibe "CPF ausente: 12 · categoria não encontrada: 3".

- **CA35 (pulados agregados — D-C2)**: Dado uma entidade com pacientes pulados por "identificador ausente" (N) e agendamentos pulados por "agendamento já existe" (M), Quando o relatório é gerado, Então `motivosPulo` traz `{ "identificador ausente": N, ... }` por entidade e a tela exibe a quebra dos pulados.

- **CA36 (motivos sem PII — R-C2/LGPD)**: Dado qualquer relatório com rejeições/pulos, Quando os motivos agregados são exibidos, Então cada chave é uma categoria genérica (ex.: "CPF ausente", "categoria não encontrada", "paciente não identificado") e **nunca** contém o CPF, nome, telefone ou qualquer valor real do registro.

- **CA37 (multi-tenant — relatório)**: Dado o relatório de um job, Quando a agregação roda, Então conta **apenas** registros daquele job (que carregam o `estabelecimento_id` do tenant) — nenhum registro de outro job/tenant entra na contagem.

- **CA38 (estado vazio — sem rejeições)**: Dado um job concluído sem rejeições nem pulos, Quando o relatório carrega, Então as quebras de motivo ficam vazias e a tela **não** renderiza a seção de motivos (sem "0 motivos" poluindo), mostrando só as contagens.

- **CA39 (regressão — relatório original intacto)**: Dado os CAs do relatório do briefing original (contagens criados/atualizados/rejeitados/pulados por entidade), Quando o relatório carrega após esta mudança, Então as contagens continuam corretas e nada do comportamento original regrediu.

## 9. Riscos e dependências

- **Pré-requisito de INFRA (não é CA de código)**: a chave da IA (`Ia:AnthropicApiKey`) precisa estar provisionada no **SSM Parameter Store de produção**. Sem ela, todo job de inferência cairá em `falhou` com "IA não configurada" — que é exatamente o comportamento correto deste addendum (falha visível em vez de loop mudo), mas a migração não avança até a chave existir. **Registrar como dependência de INFRA/go-live**, fora do escopo de código. Atualizar `Docs/INFRA.md` com o parâmetro esperado (ver §10).
- **Mudança de contrato do relatório**: `MotivosRejeicao` deixa de ser `List<string>` e vira `Dictionary<string,int>`. Não há consumidor externo (é admin interno), mas o front precisa acompanhar a mudança no mesmo PR — senão a tabela quebra. QA valida CA34/CA39 juntos.
- **Loop silencioso era um custo oculto de IA**: até este addendum, um job preso em `aguardando_mapa` re-tentava a inferência a cada 30s — se a falha fosse intermitente, queimava chamadas de IA. `falhou` corta isso (D-B4). Validar no CA25 que o job não é re-selecionado após falhar.
- **Idempotência do reprocessar da carga**: depende de que registros já importados não sejam reprocessados. O handler já filtra por `status == "pendente"` — confirmar que o reprocessar não reseta status de registro. CA30 cobre.
- **Área regressiva**: nenhuma de domínio. O risco está contido no bounded context de migração (estados + relatório). Commands de paciente/estoque/orçamento/prontuário **não** são tocados.

## 10. Observações para execução

- **Reuso obrigatório (não construir do zero)**: o estado `falhou` espelha o padrão de `rejeitado` (estado + motivo no campo) já no aggregate; o reprocessar **reusa os jobs recorrentes existentes** (só recoloca o status que eles selecionam — zero infra/fila nova); a Melhoria C **reusa** `migracao_registros.motivo_rejeicao` (já gravado, já sem PII) — é agregação em query, **sem migration de `migracao_registros`**. Badge de falha reusa `AppBadge`; botão reprocessar reusa `AppButton`.
- **Não-negociável**: motivo de falha e motivos agregados são **categorias genéricas, sem PII** (R-B2, R-C2). Mensagem técnica só no log. Espelha CA4/CA5 do original.
- **Não-negociável**: a espera legítima da Onda 2 (bloqueada pela Onda 1) **não** vira `falhou` (R-B3/CA27).
- **Liberdade técnica**: o nome exato das categorias de falha (desde que genéricas e legíveis em PT-BR), a forma de detectar "IA não configurada" (tipo de exceção vs. checagem de config), e o formato interno do `Dictionary` vs. lista de pares ficam a critério do dev — desde que respeitem os CAs.
- **Acionar `imedto-database`** para a migration de `migracao_jobs` (2 colunas + confirmar ausência de CHECK no status). Migration idempotente (gotcha conhecido `AddColumn`).
- **Fatiamento sugerido**: PR 1 = Melhoria B (estado `falhou` + handlers + reprocessar + UI de falha). PR 2 = Melhoria C (agregação + relatório). Podem ir juntos se o QA preferir um ciclo só — são independentes mas pequenos.

## 11. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — atualizar a descrição da máquina de estados do job de migração (bounded context de migração) para incluir o estado `falhou`, a transição de/para ele e o reprocessar. Incremental: adicionar `falhou` à lista de estados e uma nota sobre o padrão "job recorrente + estado de falha visível + reprocessar manual" (padrão reaproveitável por outros jobs recorrentes do projeto). **Feito nesta entrega pelo BA/dev no marco de Melhoria B.**
- **`Docs/INFRA.md`** — registrar o parâmetro SSM esperado pela IA da migração (`Ia:AnthropicApiKey` ou equivalente já usado pelo `AnthropicIaService`) como **pré-requisito de produção** para a Central de Migração funcionar; sem ele, jobs de inferência falham com "IA não configurada". Incremental: nota na seção de parâmetros SSM / e-mail-IA. **Feito nesta entrega.**
- **`Docs/LGPD.md`** — nota incremental: o `motivo_falha` e os motivos agregados do relatório são categorias genéricas sem PII (mesma regra dos `motivo_rejeicao`). Só atualizar se a seção de migração já listar as garantias de não-PII; caso contrário, é coberto pela regra geral existente. Avaliar no marco.
- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — não alterar (é a fonte do original; este addendum é decisão pós-entrega, não muda D1–D14).
