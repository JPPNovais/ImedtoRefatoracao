# Proteção do nível 1 e padronização dos códigos de membros (catálogo de regiões anatômicas)

**ID**: 2026-06-23_001
**Status**: Aprovado por usuário em 2026-06-23 (decisões fechadas antes do briefing)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: catálogo admin global (CRUD de regiões), mapa corporal / exame físico (não-regressão crítica), seletor de pai no cadastro (briefing 2026-06-22_004)

## 1. Contexto e motivação

O catálogo **global** de regiões anatômicas (tabela `regioes_anatomicas_catalogo`, gerido só pelo admin global) é o "esqueleto" do boneco do mapa corporal e do exame físico. As regiões **nível 1 (raiz)** são insubstituíveis:

- O front desenha os hotspots por **nome** (`bodyMapPaths.ts`).
- O mapeamento circunferencial/tronco é resolvido por **código** (`regioesCircunferenciais.ts`).

O banco foi limpo deixando só os 27 nível 1 (o usuário recriará nível 2/3 manualmente). Daí duas dores concretas:

1. **Risco operacional grave**: hoje a tela de admin permite excluir/inativar qualquer região, inclusive nível 1. Se um nível 1 some, o mapa corporal perde o hotspot e o exame físico quebra. O usuário foi explícito: *"esses principais do polígono não podem ser deletados na tela de admin, senão fico sem mapeamento; o primeiro nível precisa existir."*
2. **Dívida de legibilidade**: os 12 códigos de membros usam abreviações (`msd-*`, `mse-*`, `mid-*`, `mie-*`) que ninguém entende ao gerir o catálogo, enquanto os **nomes de exibição já estão por extenso**. Padronizar os códigos por extenso reduz erro humano na gestão manual das sub-regiões.

## 2. Persona-alvo

**Admin global** (papel `imedto_admin`, sem `estabelecimento_id`), no momento de manutenção do catálogo de referência — atividade rara, alto impacto. Nenhum usuário de estabelecimento toca este catálogo; os profissionais apenas o consomem (leitura) via mapa corporal/exame físico.

## 3. Escopo

A entrega tem **duas partes independentes** no mesmo deploy.

**Inclui — Parte A (proteção do nível 1)**:
- Backend rejeita excluir/inativar região `nivel == 1` (fonte da verdade).
- Front esconde/desabilita os botões de excluir e inativar para nós `nivel == 1`, com tooltip explicando.

**Inclui — Parte B (padronização dos códigos de membros)**:
- Migration idempotente que renomeia os códigos de membros nível 1 que **existirem** na tabela, e atualiza `pai_codigo` dos filhos em cascata.
- Atualização coordenada do front (`regioesCircunferenciais.ts` + comentários) e dos testes, no mesmo deploy.
- Decisão registrada sobre o seed `.cs`.

**Não inclui**:
- Renomear nomes de exibição (já estão por extenso — não mudam).
- Renomear códigos de regiões não-membros (`cabeca-*`, `pescoco-*`, `torax-*`, `abdome-*`, `pelve-*`, `lombossacra-posterior`) — ficam como estão.
- Permitir edição de `codigo` pela tela (segue imutável pela UI — regra do catálogo; rename só via migration).
- Bloquear edição/reativação/criação de nível 1, ou a gestão de nível 2/3 — tudo isso segue normal.
- Qualquer mudança em `bodyMapPaths.ts` (usa nomes, não códigos).
- Editar a migration `20260526000001` (imutável, já aplicada).

## 4. Regras de negócio

### Parte A — proteção do nível 1

- **R1**: Região com `nivel == 1` (raiz) **não pode ser excluída** (hard delete) pela tela de admin. Mora em: `ExcluirRegiaoAdminCommandHandler`. Validada em: **back (422 `BusinessException`)** + espelho de UX no front.
- **R2**: Região com `nivel == 1` (raiz) **não pode ser inativada** (soft delete) pela tela de admin. Mora em: `InativarRegiaoAdminCommandHandler`. Validada em: **back (422 `BusinessException`)** + espelho de UX no front.
- **R3 (mensagem genérica, sem PII — N/A aqui pois é ref data)**: a mensagem de 422 deve ser explicativa e genérica, ex.: `"Regiões de nível 1 (raiz) sustentam o mapa corporal e não podem ser excluídas."` (excluir) / `"Regiões de nível 1 (raiz) sustentam o mapa corporal e não podem ser inativadas."` (inativar). Texto livre do dev desde que comunique a razão.
- **R4 (ordem de checagem no handler)**: a checagem de `nivel == 1` ocorre **após** carregar a região via `ObterPorIdOuNulo` (a região precisa existir; região inexistente continua retornando o 422 atual `"Região anatômica não encontrada."`). Decisão de ordem em relação à checagem de filhos do `Excluir`: a proteção de nível 1 pode vir antes ou depois da checagem de filhos — ambas resultam em 422; liberdade do dev. **Não** disparar audit nem alterar estado quando a operação é rejeitada.
- **R5 (nível 2/3 inalterado)**: excluir/inativar/reativar de regiões `nivel >= 2`, e editar/criar/reativar de qualquer nível, seguem com o comportamento atual, sem nova restrição.

### Parte B — padronização dos códigos de membros

- **R6 (rename canônico)**: renomear **apenas** os 12 códigos de membros nível 1, preservando sufixo:
  - `msd-{sufixo}` → `membro-superior-direito-{sufixo}`
  - `mse-{sufixo}` → `membro-superior-esquerdo-{sufixo}`
  - `mid-{sufixo}` → `membro-inferior-direito-{sufixo}`
  - `mie-{sufixo}` → `membro-inferior-esquerdo-{sufixo}`
  - sufixos: `-anterior`, `-posterior`, `-circunferencial`.
  - Tabela de equivalência completa na **seção 5**. Mora em: migration SQL idempotente (`imedto-database`).
- **R7 (cascata de `pai_codigo`)**: ao renomear cada código de membro, atualizar também `pai_codigo` de qualquer filho que aponte para o código antigo (UPDATE manual em cascata), para não deixar órfão em banco novo (onde o seed insere nível 2/3 apontando para os códigos antigos). Mora em: a mesma migration.
- **R8 (idempotência por `WHERE`)**: cada UPDATE filtra por `WHERE codigo = '<antigo>'` (e `WHERE pai_codigo = '<antigo>'`). Rodar a migration duas vezes não altera nada na segunda execução (o código antigo já não existe) e não falha. Códigos antigos ausentes simplesmente afetam 0 linhas — sem erro. Mora em: a mesma migration.
- **R9 (front segue o código)**: `regioesCircunferenciais.ts` (`RAMOS_CIRCUNFERENCIAL`) é a fonte única de verdade do mapeamento por código; deve referenciar os **novos** códigos nas 4 chaves de membro (`*-circunferencial`) e nos 8 valores `anterior`/`posterior` de membro. `PARTE_PARA_TRONCO` **não muda** (não contém membros). Mora em: `regioesCircunferenciais.ts`. Validada em: testes Vitest.
- **R10 (decisão sobre o seed `.cs`)**: **atualizar** os 12 nível 1 de membros e os respectivos `pai_codigo` dos filhos em `SeedsRegioesAnatomicas.cs`, para consistência documental com a migration. Premissa a confirmar pelo dev: esse seed **não roda no startup** (não há seeder/`MigrateAsync` em `Program.cs`; apenas `CatalogoController` lê do banco). Se a premissa se confirmar, o seed `.cs` é apenas documentação/histórico e a atualização é segura e sem efeito em runtime. **Se o dev descobrir que o seed `.cs` roda em runtime**, isso muda o impacto da decisão → parar e reportar como spec gap (Tipo B). Mora em: `SeedsRegioesAnatomicas.cs`.

## 5. Modelo de dados

**Tabela afetada**: `regioes_anatomicas_catalogo` (catálogo global, **sem** `estabelecimento_id` — ref data; multi-tenant N/A).

**Operação**: UPDATE de valores em coluna `codigo` (nível 1 de membros) e `pai_codigo` (filhos nível 2/3 que apontam para esses códigos). **Sem** DDL (nenhuma coluna/índice/tabela nova).

**Tabela de equivalência canônica (R6)** — os 12 códigos:

| Código antigo | Código novo |
|---|---|
| `msd-anterior` | `membro-superior-direito-anterior` |
| `msd-posterior` | `membro-superior-direito-posterior` |
| `msd-circunferencial` | `membro-superior-direito-circunferencial` |
| `mse-anterior` | `membro-superior-esquerdo-anterior` |
| `mse-posterior` | `membro-superior-esquerdo-posterior` |
| `mse-circunferencial` | `membro-superior-esquerdo-circunferencial` |
| `mid-anterior` | `membro-inferior-direito-anterior` |
| `mid-posterior` | `membro-inferior-direito-posterior` |
| `mid-circunferencial` | `membro-inferior-direito-circunferencial` |
| `mie-anterior` | `membro-inferior-esquerdo-anterior` |
| `mie-posterior` | `membro-inferior-esquerdo-posterior` |
| `mie-circunferencial` | `membro-inferior-esquerdo-circunferencial` |

**Achado verificado (input para `imedto-database`)** — inspecionei a migration de seed `20260526000001_seed_regioes_anatomicas_catalogo.sql`:
- Ela insere **8** nível-1 de membros (`*-anterior` e `*-posterior`); **não** insere os 4 `*-circunferencial` dos membros. Os `*-circunferencial` existem em produção por outra origem (criação manual). Por isso o UPDATE deve renomear os 12, mas, sendo filtrado por `WHERE codigo = '<antigo>'`, naturalmente afeta 0 linhas para um código ausente (idempotência R8) — válido tanto para o banco de produção quanto para banco novo.
- No seed, os `pai_codigo` que apontam para membros referenciam **só** os 8 `*-anterior`/`*-posterior` (nenhum filho aponta para `*-circunferencial`). Logo o cascata de `pai_codigo` (R7) precisa cobrir esses 8 — mas escrever o cascata para os 12 é inofensivo (os 4 circunferenciais afetam 0 linhas).
- **Restrição**: `codigo` tem limite de 60 caracteres (`RegiaoAnatomicaCatalogo.Criar`). O código novo mais longo é `membro-superior-esquerdo-circunferencial` = 40 caracteres → dentro do limite. Sem risco de truncamento.

**Coluna usada na proteção (Parte A)**: `nivel` (`short`/`smallint`). A entidade já expõe `Nivel`; o DTO do front (`RegiaoAnatomicaNoDto`) já expõe `nivel: number`. Nenhuma leitura extra do banco é necessária — os handlers já carregam o aggregate completo via `ObterPorIdOuNulo`.

**Audit/LGPD**: catálogo global é ref data, **sem PII**. O audit admin existente (`ImedtoAdminAuditWriter`) continua sendo gravado apenas nas operações **bem-sucedidas** (excluir/inativar de nível ≥ 2). Operações rejeitadas (nível 1) **não** geram audit. A migration de rename não toca dado de paciente/prontuário.

## 6. UX e fluxo

**Tela**: admin global → catálogo de regiões anatômicas (`RegioesGlobaisListView.vue` → `RegiaoTreeView.vue`).

**Parte A — comportamento na árvore (`RegiaoTreeView.vue`)**:
- Para nós com `nivel == 1` (`nivelAtual === 0` na recursão / `no.nivel === 1`), os botões **Inativar** (ícone `fa-ban`) e **Excluir** (ícone `fa-trash`) devem ser **ocultados ou desabilitados**. Decisão fechada: **desabilitar** (manter o botão visível, `disabled`, com `title`/tooltip explicativo), espelhando o padrão já existente de "excluir desabilitado quando tem filhos" — reuso > duplicação. Tooltip sugerido: `"Regiões de nível 1 sustentam o mapa corporal e não podem ser inativadas/excluídas."`
  - Liberdade do dev entre **ocultar** vs **desabilitar**, desde que o usuário não consiga disparar a ação e entenda o motivo. Preferência registrada: desabilitar com tooltip (consistência visual com o já existente).
- **Editar** (lápis) e, para nível 1 inativo (cenário raro/legado), **Reativar** seguem disponíveis. Nível ≥ 2 mantém todas as ações atuais.
- Tipografia/estilos: reusar as classes e tokens já presentes no componente (`.btn-icon`, `:disabled`, tokens `--text-*`). Nenhum literal de `font-size`/`font-weight` (CLAUDE.md §5).

**Estados**: a lista já existe (loading/erro/vazio tratados em `RegioesGlobaisListView`). A mudança é só de habilitação de botões por nó.

**Parte B — sem mudança visível de UX**: a renomeação de código é interna. Na árvore admin, a coluna de código passará a exibir os códigos por extenso (efeito automático do dado renomeado) — comportamento desejado, sem trabalho de front específico além do `regioesCircunferenciais.ts`.

## 7. Critérios de aceite (testáveis)

### Parte A — proteção do nível 1 (backend = fonte da verdade)

- **CA1** (excluir nível 1 bloqueado — back): Dado um admin global autenticado e uma região com `nivel == 1` existente, Quando ele chama o endpoint de excluir região passando o id dessa região (com motivo válido ≥ 10 chars), Então a API responde **422** (`BusinessException`) com mensagem explicando que regiões de nível 1 sustentam o mapa corporal, a região **permanece** na tabela e **nenhum** audit de exclusão é gravado.
- **CA2** (inativar nível 1 bloqueado — back): Dado um admin global autenticado e uma região com `nivel == 1` ativa, Quando ele chama o endpoint de inativar região passando o id dessa região (motivo válido), Então a API responde **422** (`BusinessException`) com mensagem explicando que regiões de nível 1 não podem ser inativadas, a região permanece com `ativo = true` e **nenhum** audit de inativação é gravado.
- **CA3** (excluir nível ≥ 2 segue funcionando): Dado um admin global e uma região `nivel == 2` **sem filhos**, Quando ele chama excluir com motivo válido, Então a região é removida (comportamento atual preservado) e o audit `ExcluirRegiaoAnatomica` é gravado.
- **CA4** (inativar nível ≥ 2 segue funcionando): Dado um admin global e uma região `nivel == 2` ativa, Quando ele chama inativar com motivo válido, Então a região fica `ativo = false` e o audit `InativarRegiaoAnatomica` é gravado.
- **CA5** (região inexistente — comportamento atual preservado): Dado um id que não existe, Quando o admin chama excluir ou inativar, Então a API responde **422** com `"Região anatômica não encontrada."` (mensagem atual, inalterada).
- **CA6** (UX espelha o back): Dado o admin na árvore de regiões, Quando ele vê um nó `nivel == 1`, Então os botões Inativar e Excluir aparecem **desabilitados** (ou ocultos) com tooltip explicando que regiões de nível 1 não podem ser inativadas/excluídas; e os nós `nivel >= 2` mantêm os botões funcionais.
- **CA7** (RBAC admin global preservado): Dado um usuário **sem** a claim `imedto_admin` (usuário comum), Quando ele tenta chamar excluir/inativar de qualquer região do catálogo, Então recebe o erro de autorização atual (403/não autorizado), idêntico ao comportamento de hoje — esta entrega não relaxa nem altera o RBAC existente do admin de catálogo.

### Parte B — padronização dos códigos de membros

- **CA8** (rename dos 12 — banco com os 12 presentes): Dado um banco onde os 12 códigos antigos de membros existem, Quando a migration de rename roda, Então os 12 passam a ter os novos códigos da tabela da seção 5 e nenhum código antigo de membro permanece em `codigo`.
- **CA9** (rename condicional — banco de produção pós-limpeza): Dado o banco de produção, onde só existem os nível 1 (incluindo os de membros que existirem) e os nível 2/3 foram apagados, Quando a migration roda, Então os códigos de membros nível 1 presentes são renomeados, os ausentes afetam 0 linhas (sem erro) e nenhum registro fica inconsistente.
- **CA10** (cascata de `pai_codigo` — banco novo): Dado um banco novo onde o seed `20260526000001` inseriu nível 2/3 com `pai_codigo` apontando para os códigos antigos de membros, Quando a migration de rename roda, Então **nenhum** filho fica com `pai_codigo` órfão: todo `pai_codigo` que era código antigo de membro passou a apontar para o código novo correspondente (consulta de verificação: 0 linhas com `pai_codigo` em `('msd-anterior','mse-anterior','mid-anterior','mie-anterior','msd-posterior','mse-posterior','mid-posterior','mie-posterior','msd-circunferencial','mse-circunferencial','mid-circunferencial','mie-circunferencial')`).
- **CA11** (idempotência): Dado um banco já migrado, Quando a migration de rename roda uma **segunda** vez, Então não falha e não altera nenhuma linha (todos os UPDATEs afetam 0 linhas).
- **CA12** (front resolve membros com novos códigos): Dado `regioesCircunferenciais.ts` atualizado, Quando o teste `regioesCircunferenciais.test.ts` roda, Então `RAMOS_CIRCUNFERENCIAL` contém as 4 chaves de membro com os **novos** códigos (`membro-superior-direito-circunferencial`, etc.) mapeando para os **novos** valores `anterior`/`posterior`, e `PARTE_PARA_TRONCO` permanece inalterado.
- **CA13** (não-regressão crítica do exame físico / mapa corporal): Dado o exame físico, Quando o profissional clica em um membro no `BodyMap` e usa o modo circunferencial (`RegionSelectorPopup`), Então o comportamento é idêntico ao atual (hotspot acende, expansão circunferencial resolve as duas vistas do membro), e as suítes `BodyMap.test.ts`, `SecaoExameFisico.test.ts`, `RegionSelectorPopup.test.ts`, `RegionExamCard.test.ts` ficam **verdes**.
- **CA14** (não-regressão do seletor de pai — briefing 2026-06-22_004): Dado o cadastro de região anatômica com o seletor visual de pai (mapa corporal), Quando o admin clica num hotspot de membro nível 1, Então o "Código do pai" preenchido reflete o **novo** código de membro (resolvido via árvore do catálogo já renomeado) e o fluxo do briefing 2026-06-22_004 continua funcionando.
- **CA15** (consistência documental do seed `.cs`): Dado `SeedsRegioesAnatomicas.cs`, Quando atualizado conforme R10, Então os 12 nível 1 de membros e os `pai_codigo` dos filhos usam os novos códigos; e o dev registra no hand-off ao QA a confirmação de que o seed `.cs` **não** roda em runtime (não há seeder/`MigrateAsync` em `Program.cs`). Se rodar em runtime, o dev **para e reporta** (Tipo B).
- **CA16** (build + suíte verdes): Dado o conjunto de mudanças (back + front + migration + testes), Quando rodam `dotnet build`/testes NUnit e `vitest`/typecheck/build do front, Então tudo passa, incluindo os testes atualizados `RegionSelectorPopup.test.ts` e `regioesCircunferenciais.test.ts` e `RegionExamCard.test.ts`.

## 8. Riscos e dependências

- **Risco crítico de regressão**: o mapa corporal/exame físico cruza **nome** (paths) e **código** (circunferencial). A Parte B mexe só em código; `bodyMapPaths.ts` (nome) não muda. O risco mora em qualquer ponto que referencie os códigos antigos. **Mapa de pontos a tocar levantado** (ver seção 9). O QA deve validar o fluxo real do membro no exame físico, não só a suíte.
- **Risco de órfão em banco novo**: mitigado por R7/CA10. A migration **precisa** atualizar `pai_codigo` em cascata; esquecer isso deixa nível 2/3 órfão em qualquer banco recém-provisionado.
- **Ordenação de deploy**: migration (`imedto-database`) + front (`imedto-developer`) + back (Parte A) sobem **no mesmo deploy**. Front e banco precisam estar coerentes ao mesmo tempo (código renomeado no banco ⇄ `regioesCircunferenciais.ts` apontando para os novos códigos). Não fracionar entre deploys.
- **Premissa do seed `.cs` (R10)**: se o seed rodar em runtime, o impacto muda — gatilho de Tipo B.
- **Dependência cross-briefing**: o seletor de pai (2026-06-22_004) resolve o nó pelo catálogo; como ele lê a árvore renomeada, segue coerente — mas é CA de não-regressão obrigatório (CA14).

## 9. Observações para execução

**Mapa de pontos a tocar (levantado no refinamento — não-exaustivo, dev confirma com grep):**

- **Backend (Parte A) — não-negociável (fonte da verdade)**:
  - `backend/src/Services/Imedto.Backend.Application/Admin/Regioes/ExcluirRegiaoAdminCommandHandler.cs` — após `ObterPorIdOuNulo`, rejeitar `nivel == 1` com `BusinessException` (R1).
  - `backend/src/Services/Imedto.Backend.Application/Admin/Regioes/InativarRegiaoAdminCommandHandler.cs` — idem para inativar (R2).
  - Ambos já carregam o aggregate completo (`RegiaoAnatomicaCatalogo` expõe `Nivel`); nenhuma query extra. Decisão de produto registrada: a regra mora no **handler** (não no domain `Inativar()`/`Excluir`), porque é uma regra de **gestão pela tela de admin** — manter o domain genérico evita acoplar a entidade à política de UI. Liberdade do dev de também colocar guarda no domain se preferir, desde que o 422 saia.
  - Testes NUnit dos dois handlers cobrindo CA1–CA5.

- **Frontend (Parte A) — espelho/UX**:
  - `frontend/src/modules/admin/components/regioes/RegiaoTreeView.vue` — desabilitar/ocultar Inativar e Excluir quando `no.nivel === 1`; tooltip explicativo (reusar padrão `:disabled` + `:title` já presente no botão Excluir). `RegiaoAnatomicaNoDto.nivel` já existe.
  - Verificar `frontend/src/modules/admin/views/RegioesGlobaisListView.vue` (orquestra os modais de confirmação) — basta o gating na tree; os handlers `abrirExcluir`/`abrirInativar` não são chamados se o botão estiver desabilitado.

- **Banco (Parte B) — `imedto-database`**:
  - Migration **nova** idempotente em `db/migrations/` (UPDATE de `codigo` + cascata `pai_codigo`), seguindo o padrão idempotente do projeto (raw SQL, `WHERE` que afeta 0 linhas em re-execução). Cobrir os 12 códigos da seção 5 (mesmo os 4 `*-circunferencial` ausentes no seed — afetam 0 linhas em prod, são inofensivos). **Não** editar `20260526000001` (imutável).

- **Frontend (Parte B)**:
  - `frontend/src/components/exame-fisico/regioesCircunferenciais.ts` — atualizar as 4 chaves de membro e os 8 valores `anterior`/`posterior` de membro em `RAMOS_CIRCUNFERENCIAL` (R9). `PARTE_PARA_TRONCO` **não muda**. `abdome-circunferencial` (exceção clínica) **não é membro** → não muda.
  - Atualizar **só comentários/exemplos** em `SecaoExameFisico.vue` e `RegionSelectorPopup.vue` se algum citar `msd-*`/`mse-*`/etc. (grep antes; pode não haver).
  - `bodyMapPaths.ts` — **não tocar** (usa nomes).

- **Testes a atualizar (Parte B)**:
  - `frontend/src/components/exame-fisico/regioesCircunferenciais.test.ts`
  - `frontend/src/components/exame-fisico/RegionSelectorPopup.test.ts`
  - `RegionExamCard.test.ts` (ajustar fixtures/asserts que referenciem os códigos antigos de membro).

- **Seed `.cs` (Parte B)**:
  - `backend/src/Services/Imedto.Backend.Application/Catalogo/SeedsRegioesAnatomicas.cs` — atualizar os 12 nível 1 de membros + `pai_codigo` dos filhos (R10). Confirmar premissa de não-execução em runtime (grep por `MigrateAsync`/seeder em `Program.cs`; só `CatalogoController` lê do banco). Documentar a confirmação no hand-off (CA15).

**Liberdade técnica vs não-negociável**:
- Não-negociável: o 422 sai do backend (Parte A); a migration é idempotente e atualiza `pai_codigo` em cascata (Parte B); zero mudança no `bodyMapPaths.ts`; suítes do mapa/exame físico verdes (CA13); seletor de pai não quebra (CA14).
- Liberdade do dev: ocultar vs desabilitar botões (preferência: desabilitar com tooltip); ordem das checagens dentro do handler de excluir; guarda adicional no domain (opcional).

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — atualizar a descrição do `RegiaoTreeView.vue` (parágrafo na ~linha 106 que lista os eventos `editar`/`inativar`/`reativar`/`excluir`) acrescentando uma frase incremental: as ações **inativar** e **excluir** ficam **desabilitadas para nós de nível 1 (raiz)**, com tooltip, porque regiões de nível 1 sustentam o mapa corporal (referência a este briefing `2026-06-23_001`). Mudança cirúrgica, só essa subseção.
- **Parte B não exige atualização de doc**: `Docs/DESIGN.md` descreve `RAMOS_CIRCUNFERENCIAL`/`regioesCircunferenciais.ts` conceitualmente (como "fonte única de verdade do mapeamento `{base}-circunferencial`") e os exemplos literais que cita (`torax-anterior`, `abdome-anterior`, `lombossacra-posterior` no `PARTE_PARA_TRONCO`) **não são membros** e não mudam. Nenhum código de membro literal está documentado. Sem mudança necessária.
- **`Docs/INFRA.md`/`COMANDOS.md`/`LGPD.md`/`ARQUITETURA.md`**: nenhum. Sem novo recurso AWS, sem novo padrão de migration (segue o idempotente existente), sem PII nova, sem padrão de arquitetura novo.
