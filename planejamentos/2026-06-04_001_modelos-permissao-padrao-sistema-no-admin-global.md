# Modelos de permissão padrão do sistema no Admin Global (CRUD + propagação retroativa)

**ID**: 2026-06-04_001
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: permissionamento (RBAC de tenant + criação de estabelecimento), Admin Global (catálogos), audit

## 1. Contexto e motivação

Hoje os 3 modelos de permissão "padrão do sistema" (Admin, Médico, Recepção) só existem **hardcoded** em `ModeloPermissaoEstabelecimento.CriarPadroes()` (`Domain/ModelosPermissao/ModeloPermissaoEstabelecimento.cs`). O `CriarModeloPadraoAoCriarEstabelecimentoHandler` reage a `EstabelecimentoCriadoEvent` e materializa cópias `EhPadrao=true` em cada novo estabelecimento, lendo de `CatalogoPermissoes.AdminPadrao/MedicoPadrao/RecepcaoPadrao`.

Consequência da dor: para ajustar uma permissão padrão (ex.: dar `relatorios.exportar` ao Médico), hoje é preciso **mexer direto no banco** (UPDATE em N linhas `eh_padrao=true` de N estabelecimentos) ou alterar código e redeployar — sem audit, sem segurança, sem reversibilidade controlada. O dono quer poder ajustar o padrão pela área Admin Global, reusando o mesmo editor visual de permissões já existente.

Evidência de domínio: o sistema já tem precedente de "registro global gerenciável pelo admin" — os Catálogos Globais (Modelo de prontuário, Variável pool, Região anatômica) em `Docs/ARQUITETURA.md §Catálogos Globais`. Esta demanda estende esse conceito para permissões, com uma diferença arquitetural crítica (ver §4 R1).

## 2. Persona-alvo

**Dono / operador do produto (super-admin)** logado no Admin Global (`imedto_admin=true`). Frequência: baixa (ajuste de política de permissão é evento raro, mas de alto impacto — toca todos os tenants). Momento: manutenção/configuração de produto, fora da jornada clínica.

Persona indiretamente afetada: **todo profissional/recepção de todas as clínicas**, cujas permissões efetivas mudam quando o padrão é editado.

## 3. Escopo

**Inclui**:
- Nova área no Admin Global: "Modelos de permissão (padrão do sistema)" — listar, criar, editar, excluir os modelos padrão do sistema.
- Reuso do editor visual existente (`PapelEditorModal` + `AppPermissionMatrix`) no contexto admin.
- Persistência dos modelos padrão como **registro global de referência** na MESMA tabela `modelo_permissao_estabelecimento` (representação definida em §5).
- Migration mínima: relaxar a constraint que hoje exige `estabelecimento_id NOT NULL` + ajustar a unique de nome; **semear** os 3 registros globais a partir dos valores hoje hardcoded.
- Refatorar `CriarModeloPadraoAoCriarEstabelecimentoHandler` para semear cópias `EhPadrao=true` a partir dos **registros globais** (em vez do hardcode), mantendo o comportamento de clínicas novas.
- **Propagação retroativa total**: criar/editar/excluir um modelo padrão sincroniza as cópias `EhPadrao=true` de TODOS os estabelecimentos existentes, imediatamente.
- Audit de toda mutação (autor admin, antes/depois, nº de instâncias propagadas).

**Não inclui**:
- Edição de modelos **não-padrão** que cada estabelecimento criou (`EhPadrao=false`) — intocáveis por esta feature.
- Edição do catálogo de áreas/ações (`CatalogoPermissoes`) ou de permissões extras (`PermissoesExtras`) — continuam hardcoded.
- Edição de **permissões extras** (`permissoes_extras`: `gerir_permissoes`, `assistente_clinico_ia`, etc.) via este editor admin — o `PapelEditorModal`/`AppPermissionMatrix` atual NÃO editam extras (ver §4 R8). Os valores de `permissoes_extras` dos padrões permanecem como hoje (hardcoded no seed inicial).
- Alterar a forma como o tenant atribui modelo a vínculo (`AlterarModeloPermissaoDoVinculoCommand` permanece igual).
- Tornar o padrão editável pelo tenant (continua bloqueado — ver §4 R7).

## 4. Regras de negócio

- **R1 — Por que cópias materializadas, não live-link (decisão arquitetural central)**: diferente dos Catálogos Globais (modelo de prontuário/variável/região), que usam **live-link** (`WHERE eh_padrao_sistema=true OR estabelecimento_id=@X` — o tenant lê o registro global direto), o modelo de permissão **não pode** usar live-link. Motivo: `vinculo_profissional_estabelecimento.modelo_permissao_id` é FK para a cópia `EhPadrao=true` **daquele estabelecimento**, e as queries de autorização (`ModeloPermissaoRepository.UsuarioTemAcao`/`UsuarioTemPermissaoExtra`) fazem JOIN `v.modelo_permissao_id = mp.id` filtrando `v.estabelecimento_id`. Um registro global compartilhado quebraria esse vínculo por tenant. **Logo: o registro global é apenas a FONTE/TEMPLATE; cada estabelecimento continua tendo sua cópia `EhPadrao=true`, e a edição do global PROPAGA (UPDATE) para as cópias.** Mora em: Domain + Handler (Application/Admin). Validada em: back.

- **R2 — Representação do registro global**: o modelo padrão do sistema vive na tabela `modelo_permissao_estabelecimento` com `estabelecimento_id = NULL` (seguindo o precedente `ModeloDeProntuario`/`ProntuarioVariavelPool`, que usam `estabelecimento_id NULL` para o registro padrão-sistema). Esse registro NÃO é referenciável por nenhum vínculo (vínculos só apontam cópias com `estabelecimento_id NOT NULL`). Distingue-se a cópia do estabelecimento pelo par (`estabelecimento_id IS NOT NULL` + `eh_padrao=true`). Mora em: Domain + migration. Validada em: back.

- **R3 — Vinculação global↔cópia para propagação**: cada cópia `EhPadrao=true` de um estabelecimento precisa saber de QUAL registro global descende, para a propagação saber quais linhas atualizar. Como hoje os 3 padrões se distinguem por `Nome` ("Admin"/"Médico"/"Recepção") e a unique `(estabelecimento_id, nome)` garante 1 cópia por nome por tenant, a propagação casa global↔cópia **por `Nome`** (cópia `eh_padrao=true` com mesmo nome do registro global). Premissa: nomes dos padrões são únicos no escopo global e estáveis como chave de correlação. Mora em: Handler. Validada em: back. **Renomear** um padrão global (R5) deve renomear todas as cópias na mesma transação para não quebrar a correlação.

- **R4 — Criar padrão do sistema**: ao criar um novo modelo padrão global (ex.: "Financeiro"), o handler (a) insere o registro global (`estabelecimento_id NULL`, `eh_padrao=true`), e (b) **materializa uma cópia `eh_padrao=true` em cada estabelecimento existente** (mesmo Nome/TipoAcesso/Permissoes/metadados visuais), coerente com a decisão de propagação retroativa total. Valida unicidade de Nome no escopo global e não pode colidir com cópia de mesmo nome já existente em algum tenant (ver R9). Mora em: Handler (Application/Admin/ModelosPermissaoPadraoSistema). Validada em: back.

- **R5 — Editar padrão do sistema**: ao editar o registro global (nome, tipo de acesso, permissões, ícone, cor, descrição), o handler atualiza (a) o registro global e (b) **todas as cópias `eh_padrao=true` correlacionadas** (por R3) em TODOS os estabelecimentos, dentro da MESMA transação. Permissões efetivas dos profissionais vinculados mudam imediatamente. Mora em: Handler. Validada em: back.

- **R6 — Excluir padrão do sistema (regra segura escolhida)**: exclusão é permitida **somente se nenhuma cópia correlacionada estiver em uso por vínculo ativo em nenhum estabelecimento** (espelha a regra de tenant `EstaEmUsoPorVinculoAtivo`, agora cross-tenant). Se houver ao menos 1 vínculo ativo apontando para uma cópia desse padrão em qualquer clínica, a exclusão é **bloqueada** com 422 e mensagem orientando reatribuir os profissionais antes. Quando permitido, o handler exclui (a) o registro global e (b) todas as cópias correlacionadas. **Justificativa**: é a regra mais simples e segura — nunca deixa profissional órfão de permissão (LGPD/segurança: profissional sem modelo perde gate de autorização ou fica em estado indefinido). Reatribuição em massa automática foi descartada por ser complexa, arriscada e exigir decisão de "para qual modelo migrar". Mora em: Handler. Validada em: back. (Alternativa de soft-delete via `Ativo` NÃO se aplica: `modelo_permissao_estabelecimento` não tem coluna `ativo` e não vamos adicioná-la — restrição "não criar estrutura nova". Exclusão é física, igual ao fluxo de tenant.)

- **R7 — Tenant nunca edita padrão**: a regra atual permanece — `ModeloPermissaoEstabelecimento.Atualizar()`, `AdicionarPermissaoExtra()`, `RemoverPermissaoExtra()` e `GarantirPodeExcluir()` lançam `BusinessException` quando `EhPadrao=true`. Confirmado por leitura do domínio (`ModeloPermissaoEstabelecimento.cs` linhas 202-203, 220-223, 270-271, 285-286). **Premissa**: como o tenant nunca customizou um padrão, a propagação sobrescreve as cópias sem conflito de customização do tenant. Mora em: Domain (já existe). Validada em: back.

- **R8 — Editor não toca permissões extras**: o `PapelEditorModal` atual edita apenas `permissoes` (granulares area.acao) via `AppPermissionMatrix`, NÃO `permissoes_extras`. Logo o CRUD admin segue a mesma capacidade: edita `permissoes`, `nome`, `tipoAcesso`, `icone`, `cor`, `descricao`. As `permissoes_extras` dos padrões (ex.: Admin tem `gerir_permissoes`, `config_estabelecimento`) são preservadas como estão (semeadas a partir do hardcode atual) e NÃO são alteradas pela edição. Mora em: Front + Handler. Validada em: back (handler preserva `permissoes_extras` no UPDATE da edição) + front.

- **R9 — Unicidade de nome**: (a) no escopo global, não pode haver 2 registros globais com mesmo Nome; (b) ao criar um padrão novo, se algum estabelecimento já tiver um modelo (padrão OU não-padrão) com o mesmo Nome, a unique `(estabelecimento_id, nome)` quebraria no INSERT da cópia. O handler deve checar antes e retornar 422 com mensagem clara ("Já existe um modelo com o nome X em uma ou mais clínicas; escolha outro nome."), evitando `DbUpdateException` → 500. Mora em: Handler + migration (constraint). Validada em: back.

- **R10 — TipoAcesso**: mantém a semântica do editor atual — enum `TipoAcessoModelo` (`Profissional` / `Recepcionista`). Sem mudança. Mora em: Domain (já existe). Validada em: back + front.

- **R11 — Atomicidade da propagação**: criar/editar/excluir + propagação rodam em UMA transação (tudo ou nada). Se falhar a meio, nenhuma clínica fica em estado inconsistente. Mora em: Handler. Validada em: back.

## 5. Modelo de dados

**Tabela reutilizada**: `modelo_permissao_estabelecimento` (sem tabela nova — restrição não-negociável do usuário).

**Migration mínima (autor: `imedto-database`)**:
1. Tornar `estabelecimento_id` **nullable** na coluna `modelo_permissao_estabelecimento.estabelecimento_id` (hoje `NOT NULL`, ver `20260509032304_InitialCreate.cs` linha 314). Mapear no EF (`ModeloPermissaoEstabelecimento.EstabelecimentoId` → `long?` ou manter `long` com convenção; **decisão de modelagem fica com o BA/dev**: preferir `long?` para alinhar ao precedente `ModeloDeProntuario.EstabelecimentoId` que é `long?`). Onde `EstabelecimentoId` é usado em queries de tenant, o filtro `=@X` já exclui naturalmente o registro global (`NULL` nunca casa `=`).
2. Ajustar a unique `uq_modelo_permissao_nome_por_estabelecimento` (`estabelecimento_id, nome`): com `estabelecimento_id NULL`, Postgres trata NULLs como distintos numa unique padrão — isso permitiria 2 registros globais de mesmo nome. Criar **unique parcial** adicional `WHERE estabelecimento_id IS NULL` sobre `nome` para garantir unicidade do escopo global (R9a). A unique existente `(estabelecimento_id, nome)` continua valendo para as cópias por tenant.
3. Índice para a propagação/leitura do escopo global: índice parcial em `nome WHERE estabelecimento_id IS NULL` (atende R9a e a query de listagem admin).
4. **Seed idempotente** (SQL em `db/migrations/`): inserir os 3 registros globais (`estabelecimento_id NULL`, `eh_padrao=true`) com os exatos valores hoje em `CriarPadroes()` (Admin/Médico/Recepção — incluindo `permissoes`, `permissoes_extras`, `icone`, `cor`, `descricao`, `tipo_acesso`). `INSERT ... WHERE NOT EXISTS` para idempotência.

**Multi-tenant**: registro global tem `estabelecimento_id NULL` e é inacessível por queries de tenant (filtro `=@X`). Cópias mantêm `estabelecimento_id NOT NULL`. Nenhum vínculo aponta para o registro global.

**Audit (LGPD)**: usar `imedto_admin_audit_log` via `ImedtoAdminAuditWriter` (padrão dos catálogos globais). Sem PII (modelo de permissão não contém dado de paciente). `payloadJson` registra antes/depois (nome, permissões) e nº de instâncias propagadas. Novas constantes em `AcoesAuditAdmin` (ver §9).

**Retenção**: audit segue a política existente de `imedto_admin_audit_log` (`Docs/ARQUITETURA.md §Audit`).

## 6. UX e fluxo

**Navegação**: novo item no Admin Global, na seção de catálogos (junto a "Modelos de prontuário", "Variáveis", "Regiões"). Rótulo: **"Modelos de permissão"**. Rotas seguindo o padrão existente:
- `/admin/catalogos/permissoes` → `PermissoesGlobaisListView` (lista).
- Criar/editar via **modal** reusando `PapelEditorModal` (este já é um modal, diferente do padrão Form-View das outras telas admin — preferir reuso do modal a recriar um Form-View, coerente com a decisão de reusar o editor existente).

**Lista** (reusa o padrão de `ModelosGlobaisListView`): `app-page` + `AppPageHeader` ("Modelos de permissão padrão do sistema", subtítulo "Valem para todas as clínicas. Editar aqui altera as permissões em todos os estabelecimentos."), `AppCard`, tabela (Nome com ícone/cor, Tipo de acesso, nº de acessos, Atualizado em, Ações), `AppSearchInput` com debounce (`useDebouncedRef` ~300ms), `AppPagination`, `AppEmptyState`, botão "Novo modelo".

**Editor** (reusa `PapelEditorModal` + `AppPermissionMatrix`): mesmos campos (nome, descrição, ícone, cor, tipo de acesso, matriz de permissões). O modal abre **editável** (não em modo read-only — diferente do uso atual no tenant, onde padrão abre read-only). Para reuso, o modal precisa ganhar um modo "admin" via prop (ex.: `contexto: 'tenant' | 'admin'`) que (a) injeta o `permissaoService` admin (endpoints `/api/admin/...`), e (b) habilita edição/exclusão mesmo com `ehPadrao=true`. Manter o `PapelEditorModal` do tenant com comportamento idêntico ao atual quando `contexto='tenant'` (default).

**Confirmação de impacto (operação de alto risco)**: como editar/excluir afeta N clínicas, o fluxo de salvar/excluir exibe um **passo de confirmação** informando o impacto ("Esta alteração será aplicada a todas as clínicas (N estabelecimentos). Profissionais com este modelo terão suas permissões atualizadas imediatamente."). Para exclusão, reusar/estender o `confirm()` atual com a contagem. **Motivo**: coerente com o padrão admin de exigir consciência da ação cross-tenant.

**Estado "propagando"**: a operação é **síncrona** (uma transação no backend; mesmo com centenas de tenants, é um UPDATE em massa, rápido). O front mostra `loading` no botão Salvar/Excluir até o 200/NoContent. **Decisão**: síncrona com feedback de loading — não justifica fila/assíncrono no volume atual (dezenas/centenas de estabelecimentos). Se no futuro o volume crescer a ponto de a transação estourar timeout, vira addendum (registrar como risco em §8).

**Estados**:
- Loading: spinner na lista / loading no botão.
- Erro: mensagem genérica do `mensagem` do 422/500.
- Vazio: `AppEmptyState` ("Nenhum modelo padrão cadastrado.").
- Sucesso: toast/realimentação da lista.

**Mobile-ready**: tabela com `overflow-x:auto` (padrão existente); modal já responsivo (`PapelEditorModal` tem media query 720px).

## 7. Critérios de aceite (testáveis)

- **CA1 (criar — caminho feliz)**: Dado um admin (`imedto_admin=true`) na tela de modelos de permissão padrão, Quando cria um modelo "Financeiro" (TipoAcesso Recepcionista, permissões `financeiro.ver,financeiro.lancar`), Então é criado 1 registro global (`estabelecimento_id NULL`, `eh_padrao=true`) E uma cópia `eh_padrao=true` "Financeiro" é materializada em cada estabelecimento existente com as mesmas permissões.

- **CA2 (editar — propagação retroativa)**: Dado o padrão "Médico" e 3 estabelecimentos existentes com a cópia "Médico", Quando o admin adiciona `relatorios.exportar` ao registro global "Médico", Então o registro global E as 3 cópias `eh_padrao=true` "Médico" passam a conter `relatorios.exportar`, na mesma transação, imediatamente.

- **CA3 (editar — efeito na autorização)**: Dado um profissional vinculado à cópia "Médico" de um estabelecimento, Quando o admin remove `prescricao.assinar` do padrão "Médico", Então a query `UsuarioTemAcao(usuario, estab, "prescricao", "assinar")` passa a retornar `false` para esse profissional imediatamente.

- **CA4 (excluir — bloqueio por uso)**: Dado o padrão "Recepção" com ao menos 1 vínculo ativo apontando para a cópia "Recepção" em qualquer estabelecimento, Quando o admin tenta excluir o padrão "Recepção", Então recebe 422 com mensagem "Não é possível excluir: há profissionais vinculados a este modelo em uma ou mais clínicas." E nenhum registro (global ou cópia) é removido.

- **CA5 (excluir — permitido)**: Dado um padrão "Financeiro" sem nenhum vínculo ativo apontando para suas cópias em nenhum estabelecimento, Quando o admin exclui "Financeiro", Então o registro global E todas as cópias `eh_padrao=true` "Financeiro" são removidos na mesma transação.

- **CA6 (clínica nova semeia do global)**: Dado que o admin editou o padrão "Médico" (ex.: adicionou `estoque.ver`), Quando um novo estabelecimento é criado (`EstabelecimentoCriadoEvent`), Então sua cópia `eh_padrao=true` "Médico" reflete os valores ATUAIS do registro global (com `estoque.ver`), não o hardcode antigo.

- **CA7 (RBAC admin — acesso)**: Dado um usuário SEM claim `imedto_admin`, Quando chama qualquer endpoint `/api/admin/catalogos/permissoes*`, Então recebe 403 pela policy `ImedtoAdmin` E o item de menu não aparece na navegação admin.

- **CA8 (RBAC admin — blindagem cross-context)**: Dado um JWT admin (`imedto_admin=true`), Quando chama os endpoints de tenant `/api/estabelecimento/modelos-permissao*`, Então recebe 403 pelo `AdminBlindagemFilter` (admin não acessa endpoints de tenant). E vice-versa: JWT de tenant em `/api/admin/...` → 403.

- **CA9 (multi-tenant — tenant não edita global)**: Dado um usuário Dono de um estabelecimento, Quando chama o endpoint de tenant `PUT /api/estabelecimento/modelos-permissao/{id}` para o id de uma cópia `eh_padrao=true`, Então recebe 422 "Modelo padrão do sistema não pode ser editado." (regra R7 inalterada). E o registro global (`estabelecimento_id NULL`) nunca é alcançável por qualquer endpoint de tenant.

- **CA10 (multi-tenant — registro global isolado da leitura de tenant)**: Dado o registro global "Médico" (`estabelecimento_id NULL`), Quando o tenant lista seus modelos (`GET /api/estabelecimento/modelos-permissao`), Então a lista retorna apenas as cópias do próprio `estabelecimento_id` E NÃO inclui o registro global (`NULL` não casa o filtro `= @estabelecimentoId`).

- **CA11 (LGPD/Audit — mutação registra)**: Dado uma edição do padrão "Médico" que afeta 3 estabelecimentos, Quando concluída, Então é gravada 1 linha em `imedto_admin_audit_log` com `{acao: ATUALIZAR_MODELO_PERMISSAO_PADRAO_SISTEMA, admin_id, recurso_tipo:"modelo_permissao_padrao", recurso_id, payload com nº de instâncias propagadas e antes/depois}` E nenhum campo de PII de paciente é gravado.

- **CA12 (LGPD — mensagem genérica)**: Dado um erro de validação (ex.: nome duplicado), Quando o back retorna 422, Então a mensagem é genérica/orientadora e não vaza nome/identificador de tenant alheio.

- **CA13 (unicidade de nome)**: Dado que já existe um modelo (padrão ou não) chamado "Médico" em algum estabelecimento, Quando o admin tenta criar um padrão global "Médico", Então recebe 422 "Já existe um modelo com esse nome em uma ou mais clínicas; escolha outro nome." E nada é inserido.

- **CA14 (atomicidade)**: Dado uma propagação que falha no estabelecimento N (ex.: violação de constraint inesperada), Quando a transação aborta, Então NENHUM registro (global nem cópias) é alterado — estado idêntico ao anterior à operação.

- **CA15 (estados — vazio/loading/erro)**: Dado a tela de modelos padrão, Quando não há registros globais → mostra `AppEmptyState`; Quando carrega → spinner; Quando o back falha → mensagem de erro com `role="alert"`.

- **CA16 (performance — busca)**: Dado a lista de modelos padrão, Quando o admin digita na busca, Então o input usa debounce ~300ms (`useDebouncedRef`) e a request usa paginação (`pagina`/`tamanhoPagina`).

- **CA17 (reuso do editor)**: Dado o `PapelEditorModal`, Quando aberto no contexto admin (`contexto='admin'`), Então permite editar/excluir mesmo com `ehPadrao=true`; Quando aberto no contexto tenant (`contexto='tenant'`, default), Então mantém o comportamento atual (padrão abre read-only, sem botões de salvar/excluir). Regressão: o uso atual no tenant não muda.

- **CA18 (extras preservadas)**: Dado o padrão "Admin" que tem `permissoes_extras=[gerir_permissoes, config_estabelecimento, ...]`, Quando o admin edita as permissões granulares e salva, Então `permissoes_extras` permanece inalterado (o editor não toca extras — R8).

- **CA19 (doc viva)**: Dado o merge desta feature, Quando o QA valida, Então `Docs/ARQUITETURA.md §Catálogos Globais` contém a nova subseção descrevendo o modelo de permissão padrão como exceção ao live-link (cópias materializadas + propagação) — ver §10.

## 8. Riscos e dependências

- **Risco — autorização em massa**: editar um padrão muda permissões efetivas de muitos profissionais de uma vez. Mitigado por: passo de confirmação com contagem (UX) + audit com nº de instâncias (forense). Validar que `UsuarioTemAcao`/`UsuarioTemPermissaoExtra` refletem a mudança sem cache stale.
- **Risco — timeout da transação** em volume muito alto de estabelecimentos. Atual: síncrono (aceitável no volume atual). Se crescer, vira addendum com job assíncrono. Registrado.
- **Risco — correlação por Nome (R3)**: se um tenant tivesse uma cópia `eh_padrao=true` com nome divergente do global, a propagação não a alcançaria. Mitigado porque cópias só nascem do seed/handler (sempre com o mesmo nome) e tenant não pode renomear padrão (R7). Renomear global propaga rename (R3/R5).
- **Dependência**: `imedto-database` para a migration (nullable + unique parcial + seed). `imedto-developer` aciona o DB agent.
- **Áreas regressivas a vigiar**: criação de estabelecimento (handler refatorado — CA6), endpoints de tenant de modelo-permissão (não podem regredir — CA8/CA9/CA10), uso atual do `PapelEditorModal` no tenant (CA17).

## 9. Observações para execução

**Não-negociável**:
- NÃO criar tabela nova nem entidade nova de permissão. Reusar `modelo_permissao_estabelecimento` + `ModeloPermissaoEstabelecimento` (restrição forte do usuário).
- Registro global = `estabelecimento_id NULL` + `eh_padrao=true` (alinhar ao precedente `ModeloDeProntuario`).
- Propagação = cópias materializadas (R1) — NÃO live-link. Esta é a diferença essencial em relação aos catálogos globais existentes.
- Toda mutação em transação única (R11) e com audit via `ImedtoAdminAuditWriter` (R/CA11).
- Backend é a fonte de verdade do bloqueio de tenant (R7 já existe no domínio — não duplicar/relaxar).

**Liberdade técnica (dev decide)**:
- Estrutura exata dos command handlers em `Application/Admin/` — seguir o padrão de `ModelosPadraoSistema` (prontuário) para nomenclatura/DI: `Criar/Atualizar/Excluir/Listar/ObterModeloPermissaoPadraoSistema...Handler`.
- Controller admin separado: `AdminModelosPermissaoGlobaisController` com policy `[Authorize(Policy="ImedtoAdmin")]`, rotas `api/admin/catalogos/permissoes`. NÃO reusar o `ModeloPermissaoController` de tenant (RBAC e blindagem distintos — CA8).
- Para o domínio: adicionar fábrica/método `CriarPadraoSistema(...)` (registro global, sem estabelecimento) e um método de domínio que permita atualizar/marcar cópias na propagação — preferir métodos de domínio a manipular `Update` cru no handler, mas dentro da simplicidade (não criar abstração especulativa). O bloqueio `EhPadrao` em `Atualizar()` continua valendo para o caminho de tenant; a propagação usa um caminho de domínio próprio que não passa por esse guard (ex.: método `SincronizarComPadrao(...)` interno ao domínio, ou o handler reescreve campos via método dedicado). **Decisão de onde colocar o método de sincronização fica com o dev**, desde que o guard de tenant (R7) permaneça intacto para o fluxo de tenant.
- Reuso do `PapelEditorModal` via prop `contexto` + injeção do service admin — preferível a duplicar o modal.
- Constantes de audit a adicionar em `AcoesAuditAdmin` (`ImedtoAdminAuditLog.cs`): `CriarModeloPermissaoPadraoSistema`, `AtualizarModeloPermissaoPadraoSistema`, `ExcluirModeloPermissaoPadraoSistema`.

**Reuso obrigatório (grep antes de criar)**: `PapelEditorModal`, `AppPermissionMatrix`, `permissaoService` (criar variante admin do service, não duplicar o componente), padrão de `ModelosGlobaisListView`/`modelosGlobaisStore`/`catalogosService` para a tela admin, `ImedtoAdminAuditWriter` para audit, `useDebouncedRef` para a busca.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md` §Catálogos Globais** — adicionar subseção **"Exceção ao live-link: Modelos de permissão padrão do sistema"** explicando:
  - Por que modelo de permissão NÃO usa live-link (vínculo FK por tenant em `vinculo.modelo_permissao_id` + queries de autorização com JOIN filtrado por `estabelecimento_id`).
  - Representação: registro global = `modelo_permissao_estabelecimento` com `estabelecimento_id NULL` + `eh_padrao=true` (template/fonte), cópias por tenant = `estabelecimento_id NOT NULL` + `eh_padrao=true` (referenciáveis por vínculo).
  - Operação cross-tenant de **propagação** (criar/editar/excluir o global sincroniza as cópias correlacionadas por Nome em todos os tenants, em transação única).
  - Regra de exclusão segura (bloqueio se em uso por vínculo ativo em qualquer tenant).
  - Endpoint admin `api/admin/catalogos/permissoes` (policy `ImedtoAdmin`); refator do `CriarModeloPadraoAoCriarEstabelecimentoHandler` para semear a partir do registro global em vez do hardcode.
  - Atualizar a tabela comparativa de catálogos globais com uma linha de "Modelo de permissão" marcando-o como **cópia materializada + propagação** (distinto de live-link).
- **`Docs/COMANDOS.md` / migrations** — somente se o `imedto-database` introduzir padrão novo de unique parcial; caso contrário, nenhum.
- Demais docs (`DESIGN.md`, `INFRA.md`, `LGPD.md`): nenhum — a feature não introduz componente novo (reusa `PapelEditorModal`/`AppPermissionMatrix`), não muda infra, e o audit segue o padrão LGPD já documentado.
