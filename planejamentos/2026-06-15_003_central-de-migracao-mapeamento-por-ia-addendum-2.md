# Central de Migração — Gate de aprovação manual do admin antes da inferência por IA (addendum 2)

**ID**: 2026-06-15_003
**Refere-se a**: 2026-06-15_001_central-de-migracao-mapeamento-por-ia.md (e ao addendum 2026-06-15_002)
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: nenhuma de domínio (paciente/estoque/orçamento/prontuário intactos) — toca **só** o bounded context de migração (máquina de estados do job + UI admin + UI cliente). Migration de schema necessária (1 const de status nova; sem coluna nova).

> O épico Central de Migração (2026-06-15_001, CA1–CA24) e o addendum 002 (falha visível + motivos no relatório, CA25–CA39) estão entregues em `main`. Este addendum **fecha uma lacuna de produto/custo**: hoje, ao subir o ZIP, o job vai direto para `aguardando_mapa` e o recorrente `inferir-mapa-migracao` (poll 30s) **dispara a IA automaticamente, sem aprovação do admin Imedto**. O briefing original permanece intocado e seus CAs continuam válidos. Os CAs deste addendum começam em **CA40** (o addendum 002 terminou em CA39).

---

## 1. Contexto e motivação

Problema real de custo e governança levantado pelo usuário (decisão firme dele):

**Hoje, a inferência por IA roda sem aprovação humana.** Quando o cliente sobe o ZIP, `RegistrarArquivoRecebido` transiciona o job de `aguardando_arquivo` direto para `aguardando_mapa` ([MigracaoJob.cs](../backend/src/Services/Imedto.Backend.Domain/Migracao/MigracaoJob.cs):127-139). O job recorrente `inferir-mapa-migracao` seleciona o mais antigo em `aguardando_mapa` ([MigracaoJobRepository.cs](../backend/src/Services/Imedto.Backend.Infrastructure/Database/Repositories/MigracaoJobRepository.cs):49-52) a cada 30s e **chama a IA imediatamente**, sem nenhum operador no loop antes da chamada.

**Por que isso é um problema:** um único cliente pode subir vários arquivos/ZIPs de uma vez, e cada um dispara chamadas de IA **em massa, fora do controle do operador Imedto** — custo de IA descontrolado + governança zero sobre quando o gasto acontece. O addendum 002 já mitigou o *loop* de re-tentativa em caso de falha (estado `falhou`), mas **não** atacou a causa: o primeiro disparo da IA já acontece sem aval.

**O que o usuário quer (requisito firme):** a inferência por IA só pode rodar **após aprovação manual do admin Imedto, por job**. Ao subir, o job fica **represado** num estado de espera de aprovação; **nenhuma chamada de IA roda** até o admin aprovar aquele job específico. Vários uploads ficam todos parados, cada um aguardando aprovação individual.

**Benefício de negócio:** controle de custo de IA (o gasto só ocorre por decisão explícita do operador) + governança (o operador vê o que vai analisar antes de gastar) + alinhamento com o "humano no loop" que já é premissa do épico (R7) — que hoje só existe **depois** da IA (revisão do mapa), e agora passa a existir **antes** dela também.

## 2. Persona-alvo

- **Operador Imedto (admin da plataforma)**, no painel `modules/admin`. É quem ganha o controle: passa a **aprovar a análise** de cada job antes de qualquer chamada de IA. Frequência: recorrente (toda nova migração).
- **Cliente (dono/admin do estabelecimento)**, na Configuração do Estabelecimento. Após o upload, vê um status honesto de "aguardando aprovação do Imedto" em vez de "processando" (transparência — D-A2). Frequência: 1 a poucas vezes na vida do cliente.

## 3. Escopo

**Inclui:**

- **Novo estado inicial pós-upload `aguardando_aprovacao`** na máquina de estados do `MigracaoJob`, inserido **entre** `aguardando_arquivo` e `aguardando_mapa`.
- `RegistrarArquivoRecebido` passa a transicionar `aguardando_arquivo → aguardando_aprovacao` (não mais direto para `aguardando_mapa`).
- **Novo método de domínio `AprovarAnalise(Guid adminId)`** válido **apenas** a partir de `aguardando_aprovacao`, que transiciona para `aguardando_mapa` (o estado que o recorrente de inferência consome). Registra o admin que aprovou.
- **Novo endpoint admin `POST /{jobId}/aprovar-analise`** (RBAC `ImedtoAdmin`, reuso da policy já aplicada em todo o `AdminMigracaoController`), espelhando o padrão do endpoint `reprocessar` existente.
- **UI admin:** badge do novo estado `aguardando_aprovacao` ("Aguardando aprovação") na fila (`MigracoesListView`) e no detalhe (`MigracaoRevisaoView`); botão **"Aprovar análise"** no detalhe do job quando em `aguardando_aprovacao`.
- **UI cliente:** o status exibido após o upload reflete `aguardando_aprovacao` com um texto honesto ("Recebemos seus arquivos. Aguardando aprovação da equipe Imedto para iniciar a análise.") — sem prometer que já está processando (D-A2).
- **Anti-regressão (não-negociável):** garantir que **nenhum** job em `aguardando_aprovacao` seja jamais selecionado pelo recorrente de inferência — a IA só vê jobs já aprovados (em `aguardando_mapa`). CA explícito (CA44).

**Não inclui (registrar como backlog / fora deste addendum):**

- **Rejeitar/descartar job pelo admin** como ação dedicada deste fluxo. Decisão D-A4: **não** se cria um caminho novo de "rejeitar análise" agora — o estado `aguardando_aprovacao` se enquadra na regra do `Rejeitar()` já existente (que aceita `aguardando_arquivo` e `aguardando_mapa`), e **passa a aceitar `aguardando_aprovacao` também**, reusando o estado terminal `rejeitado` já implementado. Não há UI nova de rejeição neste addendum além de garantir que o `Rejeitar()` cubra o novo estado (a UI de rejeitar, se necessária, é backlog). Ver D-A4.
- **Aprovação por arquivo/entidade.** A aprovação é **por job inteiro** (D-A3), não granular por arquivo. Granularidade por arquivo é backlog se algum dia o operador pedir.
- **Aprovação em lote** (aprovar N jobs de uma vez na fila). MVP é 1 clique por job no detalhe. Botão de aprovar direto na fila é opcional/backlog (ver §7 — pode entrar se trivial, mas não é CA obrigatório).
- **Notificação ativa ao admin** quando um job entra em `aguardando_aprovacao`. O operador vê na fila ao abrir o painel (mesma decisão do addendum 002 para falhas). Backlog.
- **Auto-aprovação condicional** (ex.: aprovar automático abaixo de X MB). Fora de escopo — o requisito é aprovação **sempre** manual.

## 4. Decisões de produto (fechadas neste addendum)

> Os 5 pontos que o usuário pediu para o BA fechar. Padrão conservador: reuso, LGPD-safe, mínimo viável.

- **D-A1 — Nome do estado e label.** Estado de domínio: `aguardando_aprovacao` (const `StatusAguardandoAprovacao = "aguardando_aprovacao"`). Label exibido (admin e cliente): **"Aguardando aprovação"**. Badge variant: `warning` (mesmo do `aguardando_mapa`, é um estado de espera benigno). **Por quê:** segue a convenção snake_case dos demais status e o label espelha o vocabulário do usuário; reaproveita o padrão de mapa `STATUS_LABELS`/`STATUS_VARIANT` já existente na fila.

- **D-A2 — O cliente vê o status de "aguardando aprovação do Imedto"? Sim.** Após o upload, o cliente vê um status honesto na seção "Migrar meus dados" da Config do Estabelecimento: "Recebemos seus arquivos. Aguardando aprovação da equipe Imedto para iniciar a análise." **Por quê:** transparência — hoje o cliente veria "processando" enquanto na verdade nada roda até a aprovação; mostrar a verdade evita a sensação de travamento e alinha com o tom honesto do produto. Não expõe nenhum dado de admin nem PII — é só o status do próprio job do cliente.

- **D-A3 — A aprovação é por job inteiro.** Um clique "Aprovar análise" libera o **job todo** (todos os arquivos/entidades daquele upload) para inferência. **Não** há aprovação por arquivo ou por entidade. **Por quê:** o job já é a unidade de upload (um ZIP = um job); a inferência roda 1 chamada por arquivo *dentro* do job (CA23), mas o gate de custo/governança que o usuário quer é "este upload pode gastar IA?" — granularidade de job é o nível certo e mantém a UI simples (1 clique).

- **D-A4 — Caminho para job reprovado/descartado: reusar `rejeitado`, sem fluxo novo.** Se o operador **não** quer analisar um job (cliente subiu lixo, arquivo errado, teste), ele pode **rejeitá-lo** — e a regra de `Rejeitar()` existente **passa a aceitar `aguardando_aprovacao`** como estado de origem (além de `aguardando_arquivo` e `aguardando_mapa` que já aceita), levando ao estado terminal `rejeitado` já implementado. **Não** se cria um estado novo "reprovado" nem um método `ReprovarAnalise` separado — `rejeitado` já é o terminal semântico de "este job não vai adiante". **Por quê:** mínimo viável, reuso do estado terminal existente, enum enxuto. **Nota de escopo:** este addendum só garante que o domínio aceite rejeitar a partir do novo estado (1 linha na guard de `Rejeitar()`); a **UI de botão "Rejeitar"** no detalhe é **opcional** — entra se o dev achar trivial junto do botão "Aprovar análise", senão fica como backlog. O CA45 cobre a regra de domínio (rejeitar a partir de `aguardando_aprovacao` funciona e não dispara IA); a UI de rejeição não é CA obrigatório.

- **D-A5 — Gate anti-IA é a essência da entrega (não-negociável).** Nenhum job em `aguardando_aprovacao` pode ser consumido pelo recorrente de inferência. O gate se materializa **naturalmente** porque `ObterMaisAntigoAguardandoMapaOuNulo` seleciona **só** `StatusAguardandoMapa` — e o job só chega a `aguardando_mapa` via `AprovarAnalise`. Mas isso vira **CA explícito anti-regressão** (CA44): subir um job e deixar o recorrente rodar várias vezes **nunca** dispara IA até a aprovação. **Por quê:** é o requisito central; tem que ser verificável e blindado contra regressão futura (ex.: alguém alterar a transição do upload de volta para `aguardando_mapa`).

## 5. Regras de negócio

- **R-A1 (estado inicial pós-upload é `aguardando_aprovacao`)**: `RegistrarArquivoRecebido` transiciona `aguardando_arquivo → aguardando_aprovacao` (não mais `→ aguardando_mapa`). O termo de responsabilidade e a expiração do S3 (R12/CA24) continuam sendo registrados nesse momento, exatamente como hoje — só o status de destino muda. Mora em: `MigracaoJob.RegistrarArquivoRecebido` (Domain). Validada em: back.

- **R-A2 (aprovação libera a IA)**: Novo método `AprovarAnalise(Guid adminId)` válido **apenas** a partir de `aguardando_aprovacao` (BusinessException/422 em qualquer outro estado). Transiciona `aguardando_aprovacao → aguardando_mapa`, registra o `adminId` aprovador (reusar `DisparadoPorUsuarioId` **não** se aplica aqui — é o disparo da carga; usar um registro de aprovação próprio só se o dev julgar necessário para audit, senão a trilha de transição CA20 já captura o admin). Mora em: `MigracaoJob.AprovarAnalise` (Domain) + `AprovarAnaliseCommandHandler` (Application). Validada em: back.

- **R-A3 (gate anti-IA — nenhum job não-aprovado é inferido)**: O recorrente de inferência (`ObterMaisAntigoAguardandoMapaOuNulo` → `InferirMapaMigracaoJobHandler`) seleciona **exclusivamente** jobs em `aguardando_mapa`. Como o único caminho de `aguardando_aprovacao` para `aguardando_mapa` é `AprovarAnalise` (acionado por admin via endpoint `ImedtoAdmin`), nenhum job recém-upado é inferido antes da aprovação. **Não** alterar a query de seleção para incluir o novo estado. Mora em: query de seleção do recorrente (Infrastructure) — *inalterada por design*; a regra é "não regredir". Validada em: back + QA (CA44).

- **R-A4 (RBAC — só `ImedtoAdmin` aprova)**: O endpoint `POST /{jobId}/aprovar-analise` é protegido pela policy `ImedtoAdmin` (reuso — o `AdminMigracaoController` inteiro já é `[Authorize(Policy = "ImedtoAdmin")]`). Usuário sem a policy → 403; botão "Aprovar análise" oculto no front. O **cliente** (dono/admin do tenant) **nunca** aprova a própria análise — só o operador da plataforma. Mora em: controller admin + front. Validada em: back + front.

- **R-A5 (rejeitar a partir de `aguardando_aprovacao` — D-A4)**: A guard de `MigracaoJob.Rejeitar()` passa a aceitar `aguardando_aprovacao` como estado de origem válido (além de `aguardando_arquivo` e `aguardando_mapa`), levando ao terminal `rejeitado`. Um job rejeitado **nunca** dispara IA (não está em `aguardando_mapa`). Mora em: `MigracaoJob.Rejeitar` (Domain). Validada em: back.

- **R-A6 (audit da transição de aprovação)**: As transições `aguardando_arquivo → aguardando_aprovacao` (no upload) e `aguardando_aprovacao → aguardando_mapa` (na aprovação) são auditadas como qualquer transição do job (CA20 do original — {usuario_id, estabelecimento_id, job_id, status_anterior, status_novo, timestamp}, sem PII). Na aprovação, `usuario_id` é o admin; no upload, é o usuário do cliente que subiu. Mora em: trilha de transição existente. Validada em: back.

- **R-A7 (multi-tenant — herdada)**: O endpoint de aprovação e o detalhe do job resolvem o job pelo repositório admin existente; job inexistente → mensagem genérica "não encontrado", sem vazar tenant alheio nem PII. O cliente só vê o status do próprio job do seu tenant. Mora em: repositório admin + query do cliente. Validada em: back (CA46).

## 6. Modelo de dados

**Toca apenas o enum de status de `migracao_jobs`** (staging multi-tenant existente):

- **Novo valor de status** `aguardando_aprovacao`. O `status` é `text` livre no banco (o addendum 002 já confirmou que não há CHECK enum a alterar — a const nova entra no domínio e no front). **Confirmar com `imedto-database`** se há alguma constraint/CHECK a ajustar; se não houver (esperado), **não há migration de schema** — é só a const nova no domínio. Caso o `imedto-database` opte por um CHECK/lookup de status, a migration adiciona o valor de forma idempotente.
- **Sem coluna nova.** Diferente do addendum 002 (que adicionou `motivo_falha` + `status_antes_falha`), este addendum **não** precisa de coluna nova — só um valor de status novo. O admin aprovador é capturável pela trilha de audit de transição (CA20/R-A6); não há requisito de produto para uma coluna `aprovado_por_usuario_id` dedicada neste MVP (se o dev quiser registrar para relatório futuro, é liberdade técnica, mas não é exigência).

**Backfill / jobs em voo:** se houver jobs hoje em `aguardando_mapa` que **ainda não foram inferidos**, eles permanecem válidos (já passaram a fase que agora exige aprovação — não regredir; o gate vale para uploads **novos**). Não há backfill obrigatório; o estado novo só afeta o caminho de upload daqui para frente. Confirmar com `imedto-database` se há jobs em `aguardando_mapa` parados que mereçam atenção operacional — não bloqueia esta entrega.

**Audit / LGPD**: a transição de/para `aguardando_aprovacao` entra na trilha de audit existente (CA20), sem PII. Nenhum dado novo de paciente é tocado.

## 7. UX e fluxo

**Fluxo-alvo (substitui o trecho de §6/§7 do original apenas no ponto do disparo da IA):**

1. Cliente sobe ZIP → job em **`aguardando_aprovacao`** (não mais `aguardando_mapa`). Nenhuma IA roda.
2. Admin vê o job na fila com badge "Aguardando aprovação" → abre o detalhe → clica **"Aprovar análise"** → job vai para `aguardando_mapa`.
3. O recorrente `inferir-mapa-migracao` seleciona o job (agora em `aguardando_mapa`) e roda a IA. Resto do fluxo **inalterado**: `mapa_em_revisao` → preview → "Migrar" → carga → relatório → desfazer.

**Fila de migrações (`MigracoesListView`):**
- `STATUS_LABELS` ganha `aguardando_aprovacao: "Aguardando aprovação"`; `STATUS_VARIANT` ganha `aguardando_aprovacao: "warning"`. O badge aparece na coluna de status (reuso de `AppBadge`, padrão já existente — espelha como `falhou` foi adicionado no addendum 002).
- (Opcional, não-CA) botão "Aprovar análise" direto na linha da fila para jobs em `aguardando_aprovacao` — entra se trivial; senão a aprovação é feita pelo detalhe.

**Detalhe / revisão (`MigracaoRevisaoView`):**
- Novo ramo de `v-if` para `statusJob === 'aguardando_aprovacao'`: card com título "Aguardando aprovação", `AppBadge variant="warning"`, parágrafo explicando que a análise por IA só roda após a aprovação, e botão **"Aprovar análise"** (`AppButton variant="primary"`). Estado de loading no botão durante a aprovação; mensagem de erro genérica se falhar. Após sucesso, recarrega o job (vai para `aguardando_mapa` e, em até ~30s, o recorrente o move para `mapa_em_revisao`).
- (Opcional, D-A4) botão secundário "Rejeitar" no mesmo card, se o dev julgar trivial reusar o `Rejeitar()` — caso contrário, backlog.

**Lado do cliente — Configuração do Estabelecimento:**
- O status exibido após o upload passa a refletir `aguardando_aprovacao` com o texto de D-A2 ("Recebemos seus arquivos. Aguardando aprovação da equipe Imedto para iniciar a análise."). Reusar o componente/estado de status já existente na seção "Migrar meus dados" — só ajustar o mapa de labels do lado cliente para incluir o novo estado.

**Service/DTO front:** `migracaoAdminService` ganha `aprovarAnalise(jobId)` (espelha o `reprocessar(jobId)` do addendum 002). Nenhum campo novo no DTO é estritamente necessário (o `status` já carrega `aguardando_aprovacao`).

**Estados a cobrir:** novo estado `aguardando_aprovacao` (badge na fila + card no detalhe + status no cliente), loading da aprovação, sucesso da aprovação (vai para `aguardando_mapa`), erro da aprovação (genérico), e o caminho de tentar aprovar um job fora de `aguardando_aprovacao` (422).

## 8. Critérios de aceite (testáveis) — começam em CA40

- **CA40 (upload represa em `aguardando_aprovacao` — R-A1)**: Dado um cliente que sobe um ZIP válido (≤50MB) com termo aceito, Quando `RegistrarArquivoRecebido` roda, Então o job fica em **`aguardando_aprovacao`** (não em `aguardando_mapa`), o `ArquivoExpiraEm`/`TermoAceitoEm` continuam preenchidos (R12/CA24 intactos), e **nenhuma** chamada de IA é disparada.

- **CA41 (aprovar libera a IA — R-A2)**: Dado um job em `aguardando_aprovacao`, Quando o admin chama `POST /{jobId}/aprovar-analise`, Então o job transiciona para `aguardando_mapa`, e na rodada seguinte (≤30s) o recorrente `inferir-mapa-migracao` o seleciona e roda a inferência (job vai para `mapa_em_revisao`).

- **CA42 (aprovar só em `aguardando_aprovacao` — R-A2)**: Dado um job que **não** está em `aguardando_aprovacao` (ex.: já em `aguardando_mapa`, `migrando` ou `concluido`), Quando se chama o endpoint de aprovar-análise, Então recebe **422** (estado inválido), nada muda, e nenhuma IA é disparada por essa chamada.

- **CA43 (RBAC — só `ImedtoAdmin` aprova — R-A4)**: Dado um usuário sem a policy `ImedtoAdmin` (incluindo o dono/admin do próprio tenant), Quando chama `POST /{jobId}/aprovar-analise`, Então recebe **403**, e o botão "Aprovar análise" **não aparece** no front para esse usuário. O cliente nunca aprova a própria análise.

- **CA44 (GATE ANTI-IA — anti-regressão, não-negociável — R-A3/D-A5)**: Dado um job recém-upado em `aguardando_aprovacao`, Quando o recorrente `inferir-mapa-migracao` roda **várias vezes** (múltiplos ciclos de poll) sem que ninguém aprove o job, Então a IA **nunca** é chamada para esse job (zero chamadas ao provider de IA), o job **permanece** em `aguardando_aprovacao`, e ele só passa a ser elegível para inferência **após** uma aprovação bem-sucedida. (Verificável: zero requisições ao provider de IA enquanto o job está em `aguardando_aprovacao`.)

- **CA45 (rejeitar a partir de `aguardando_aprovacao` — R-A5/D-A4)**: Dado um job em `aguardando_aprovacao`, Quando o operador o rejeita (via `Rejeitar()`), Então o job vai para `rejeitado` (terminal), e **nenhuma** IA é disparada para ele em nenhum momento. (E dado um job já em `concluido`/`desfeito`, rejeitar continua inválido — a guard só amplia para incluir `aguardando_aprovacao`, sem afrouxar os terminais.)

- **CA46 (multi-tenant — aprovação/detalhe — R-A7)**: Dado o endpoint de aprovar-análise ou de detalhe, Quando resolve um job inexistente ou de outro contexto, Então retorna mensagem genérica "não encontrado", sem vazar tenant alheio nem PII; e o cliente, na Config do Estabelecimento, só enxerga o status do próprio job do seu tenant.

- **CA47 (cliente vê "aguardando aprovação" — D-A2)**: Dado um cliente que acabou de subir o ZIP, Quando abre a seção "Migrar meus dados", Então vê o status honesto "Recebemos seus arquivos. Aguardando aprovação da equipe Imedto para iniciar a análise." (e **não** uma mensagem de "processando/analisando", que seria falsa enquanto o job está represado).

- **CA48 (fila e detalhe mostram o novo estado)**: Dado um job em `aguardando_aprovacao`, Quando o operador abre a fila de migrações, Então vê o badge "Aguardando aprovação" (variant warning); e quando abre o detalhe, Então vê o card de aprovação com o botão "Aprovar análise".

- **CA49 (audit das transições de aprovação — R-A6)**: Dado um job que vai `aguardando_arquivo → aguardando_aprovacao` (upload) e depois `aguardando_aprovacao → aguardando_mapa` (aprovação), Quando cada transição ocorre, Então há linha de audit com {usuario_id (cliente no upload; admin na aprovação), estabelecimento_id, job_id, status_anterior, status_novo, timestamp}, sem PII.

- **CA50 (regressão — fluxo pós-aprovação intacto)**: Dado um job aprovado (em `aguardando_mapa`), Quando o fluxo segue (inferência → `mapa_em_revisao` → preview → `migrando` → conclusão → desfazer) e o caminho de falha do addendum 002 (`falhou` + reprocessar), Então **nada** do comportamento dos briefings 001/002 regrediu — o gate de aprovação só adiciona um passo **antes** da inferência, sem alterar o que vem depois. (Atenção: o `Reprocessar()` do addendum 002, ao retomar de uma falha de inferência, volta para `aguardando_mapa` — e isso continua correto, pois um job que já foi aprovado uma vez não precisa de nova aprovação ao reprocessar; o `status_antes_falha` registrado é `aguardando_mapa`, não `aguardando_aprovacao`.)

## 9. Riscos e dependências

- **Risco de regressão do gate (o mais importante)**: se alguém, no futuro, alterar `RegistrarArquivoRecebido` de volta para `aguardando_mapa`, ou alterar a query de seleção do recorrente para incluir `aguardando_aprovacao`, o gate quebra silenciosamente e a IA volta a rodar sem aprovação. **CA44 é o guardião** — deve virar teste automatizado que falha se o gate cair. Não-negociável.
- **Interação com `falhou`/`reprocessar` (addendum 002)**: coberta no CA50. Um job que falhou na inferência **já foi aprovado** (chegou a `aguardando_mapa`); o `Reprocessar()` o devolve para `aguardando_mapa` (não para `aguardando_aprovacao`) — ou seja, **não se exige re-aprovação ao reprocessar uma falha de inferência**. Isso é o comportamento desejado: a aprovação é o gate de **entrada** da IA, não de cada tentativa. Confirmar que o `status_antes_falha` de uma falha de inferência é `aguardando_mapa` (é — `MarcarFalhou` só roda a partir de `aguardando_mapa`/`migrando`).
- **Jobs em voo no momento do deploy**: jobs hoje em `aguardando_mapa` (já inferidos ou prestes a inferir) **não** regridem para `aguardando_aprovacao` — o gate vale para uploads novos. Sem backlog obrigatório; coordenar com `imedto-database` apenas se houver jobs parados que mereçam ação operacional manual.
- **Sem nova dependência de infra**: ao contrário do addendum 002 (que dependia da chave de IA no SSM), este addendum **não** adiciona dependência de infra — é só máquina de estados + UI. O pré-requisito SSM do addendum 002 continua valendo para quando a IA efetivamente rodar (pós-aprovação).
- **Área regressiva**: nenhuma de domínio. Risco contido no bounded context de migração (estado novo + UI). Commands de paciente/estoque/orçamento/prontuário **não** são tocados.

## 10. Observações para execução

- **Reuso obrigatório (não construir do zero)**: o estado `aguardando_aprovacao` espelha o padrão dos demais status (const no domínio + entrada nos mapas `STATUS_LABELS`/`STATUS_VARIANT` do front, exatamente como `falhou` foi adicionado no addendum 002). O endpoint `aprovar-analise` espelha o `reprocessar` já existente no `AdminMigracaoController` (mesma policy `ImedtoAdmin`, mesma forma de handler). O `aprovarAnalise(jobId)` do service front espelha o `reprocessar(jobId)`. O caminho de rejeição reusa o `Rejeitar()`/estado `rejeitado` existentes (D-A4) — só amplia a guard.
- **Não-negociável**: nenhum job em `aguardando_aprovacao` é consumido pela IA (CA44). **Não** alterar a query de seleção do recorrente para incluir o novo estado — o gate depende exatamente de ela continuar selecionando só `aguardando_mapa`.
- **Não-negociável**: a aprovação é por **job inteiro** (D-A3), só por **`ImedtoAdmin`** (R-A4), e o cliente vê um status **honesto** de espera (D-A2/CA47).
- **Liberdade técnica**: o texto exato do label/mensagem do cliente (desde que honesto e em PT-BR), a decisão de registrar ou não uma coluna `aprovado_por_usuario_id` dedicada (a trilha de audit já basta — coluna é opcional), e se o botão "Aprovar análise" aparece também na linha da fila (além do detalhe) ficam a critério do dev, desde que respeitem os CAs.
- **Acionar `imedto-database`** apenas para confirmar se há CHECK/constraint de status a ajustar (esperado: não há — `status` é `text` livre, como o addendum 002 confirmou). Se não houver, **não há migration**; se houver, migration idempotente para incluir `aguardando_aprovacao` (gotcha conhecido `AddColumn`/CHECK não-idempotente).
- **Fatiamento sugerido**: pode ir num único PR pequeno — (1) estado + `AprovarAnalise` + guard de `Rejeitar` no domínio; (2) endpoint + handler + service front; (3) badges/labels (admin + cliente) + card "Aprovar análise". CA44 como teste automatizado de gate é a peça crítica de QA.

## 11. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — atualizar a descrição da **máquina de estados do job de migração** (bounded context de migração) para incluir o estado `aguardando_aprovacao` **antes** de `aguardando_mapa`, deixando explícito que **a inferência por IA só roda após aprovação manual do admin** (`aguardando_aprovacao --AprovarAnalise(admin)--> aguardando_mapa`), e que o recorrente de inferência seleciona **exclusivamente** `aguardando_mapa` (gate de custo/governança). Incremental: ajustar a lista/diagrama de estados e adicionar uma nota sobre o padrão "gate de aprovação humana antes de chamada de IA cara" (padrão reaproveitável por outros fluxos que disparem IA por job). **Feito nesta entrega pelo BA/dev no PR.**
- **`Docs/LGPD.md`** — sem nova regra de PII; o estado novo não toca dado pessoal. **Não atualizar** (coberto pelas garantias existentes).
- **`Docs/INFRA.md`** — sem mudança de infra. **Não atualizar.**
- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — não alterar (é a fonte do original; este addendum é decisão pós-entrega de produto/custo, não muda D1–D14). Observação de contexto: o discovery §7 descrevia o upload indo direto para `aguardando_mapa` com inferência automática — este addendum **substitui** esse passo pelo gate de aprovação, mas o discovery permanece como registro histórico da decisão original.
