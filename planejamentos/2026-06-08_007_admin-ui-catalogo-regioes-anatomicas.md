# Admin UI — catálogo de regiões/sub-regiões anatômicas (B3 da epic de vista no exame físico)

**ID**: 2026-06-08_007
**Status**: Aprovado por usuário em 2026-06-08
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P (polimento/correção de UI existente — não é greenfield)
**Áreas regressivas tocadas**: prontuário (catálogo de regiões consumido pelo exame físico), área admin global (nenhum impacto em tenant)

---

## 0. Achado decisivo da investigação (leia antes de tudo)

**A infraestrutura de UI admin EXISTE e é madura. A tela de regiões já está 90% construída.** Este B3 **não cria nada do zero** — ele **corrige e completa** o que já está em `frontend/src/modules/admin/`. Dimensionamento honesto na seção 8.

Estado atual confirmado por leitura de código:

| Camada | Arquivo | Estado |
|---|---|---|
| Backend CRUD completo | `AdminRegioesAnatomicasGlobaisController.cs` + 6 handlers em `Application/Admin/Regioes/` | **Criar/editar/inativar/excluir completos.** Pré-execução 2026-06-09: adicionar **`ReativarRegiaoAdminCommandHandler`** (+ rota) e **trava de circunferencial no `CriarRegiaoAdminCommandHandler`** — ambos triviais, padrão já existente. |
| Auth/layout/router admin | `AdminLogin.vue`, `AdminLayout.vue`, `adminAuthStore.ts`, `router/index.ts` (guard `requiresAdminAuth`) | **Completo.** Não é dependência pendente. |
| Rotas de regiões | `/admin/catalogos/regioes`, `/novo`, `/:id` | **Já registradas.** |
| Service + store | `catalogosService.ts` (`regioesGlobaisService`) + `regioesGlobaisStore.ts` | **Completos** (inclui `inativar`, que hoje não é chamado pela UI). |
| Árvore | `components/regioes/RegiaoTreeView.vue` | **Existe**, mas com bugs (seção 4). |
| Lista | `RegioesGlobaisListView.vue` | **Existe**, mas expõe hard-delete como ação primária e **não expõe inativar** (viola Q4.4). |
| Form criar/editar | `RegioesGlobaisFormView.vue` | **Existe**, mas com `OPCOES_VISTA` errado (seção 4). |

**Conclusão de dimensionamento: NÃO precisa fatiar. NÃO precisa de `imedto-database`.** É uma entrega P majoritariamente de **frontend** (corrigir vocabulário de vista, religar a ação de inativar/reativar, alinhar guard de UX com as regras já travadas no backend) **+ uma pequena mudança de backend** (2 endpoints/regras triviais, seguindo padrões já existentes — ver decisões fechadas na seção 8) **+ atualização da doc** que descreve a árvore.

> **CORREÇÃO PRÉ-EXECUÇÃO (2026-06-09)**: as duas decisões abertas da seção 8 foram **APROVADAS pelo orquestrador com espelho no backend**. O B3 agora cravado em escopo: (1) endpoint/handler de **reativar** região no backend + religar na UI; (2) **trava de "filho de circunferencial" espelhada no backend** (422/BusinessException). Ambas pequenas, seguindo padrões existentes. **`imedto-developer` atua em FRONTEND + BACKEND leve. `imedto-database` continua fora** (sem schema/migration). CAs e regras atualizados abaixo.

---

## 1. Contexto e motivação

A epic de "vista" no exame físico tem 3 blocos. B1 (briefing `2026-06-08_005`) e B2 (`2026-06-08_006`) entregaram a parte clínica: vista `circunferencial`, os 9 nós agregadores nível-1, fusão de polígonos e coloração. B3 é a contraparte **administrativa e ortogonal**: o super-admin Imedto precisa de uma UI confiável para manter o catálogo global de regiões anatômicas que alimenta o exame físico de todos os tenants.

A UI já existe (Wave 4 — `2026-05-30_004`), porém foi escrita **antes** de a vista `circunferencial` existir e **antes** de a decisão de soft-delete-first (Q4.4) ser tomada. Resultado: a tela hoje (a) não permite criar/editar nós circunferenciais corretamente, (b) oferece valores de vista que não existem no catálogo (`lateral_direita`/`lateral_esquerda`), e (c) expõe exclusão permanente como ação primária, sem a opção de inativar — exatamente o oposto da política segura para dado que lastreia prontuário.

Evidência: o seed canônico (`SeedsRegioesAnatomicas.cs`) usa **exatamente três** valores de vista — `anterior` (108x), `posterior` (38x), `circunferencial` (9x). Nenhum nó usa `lateral_*`.

## 2. Persona-alvo

Super-admin da plataforma Imedto (tabela `imedto_admins`, autenticação admin separada via `/admin/login`). **Não** é usuário de estabelecimento. Frequência de uso: baixa/eventual (manutenção de catálogo clínico), volume de dados baixo (~144 registros). Nunca um usuário de tenant comum acessa esta área.

## 3. Escopo

**Inclui — Frontend** (em `frontend/src/modules/admin/`):
- Corrigir `OPCOES_VISTA` em `RegioesGlobaisFormView.vue` para o vocabulário real: `anterior`, `posterior`, `circunferencial` (+ opção "sem vista" para nós neutros). Remover `lateral_direita`/`lateral_esquerda`.
- Religar a ação **Inativar** (soft-delete) na lista/árvore como **ação primária** de remoção, consumindo o `inativar` que já existe no store/service. Exclusão permanente (hard-delete) passa a ser ação **secundária/escondida**, restrita conforme R5.
- Expor **Reativar** para nós inativos, consumindo o novo endpoint de backend (ver "Inclui — Backend" e R6).
- Alinhar a árvore (`RegiaoTreeView.vue`) ao contrato documentado em `Docs/DESIGN.md` (eventos `editar`, `inativar`, `reativar`, `excluir`) — hoje a lista ignora `inativar`/`reativar`.
- Guards de UX que espelham as regras já travadas no backend (R1–R4 e R7): bloquear no front, com mensagem clara, antes de bater no 422.
- Estados de árvore vazia, carregando, erro de rede (a maioria já existe — validar).

**Inclui — Backend** (2 mudanças triviais, padrões já existentes; aprovadas em 2026-06-09):
- **Endpoint/handler de reativar região** (R6): `POST /api/admin/catalogos/regioes-anatomicas/{id}/reativar` + `ReativarRegiaoAdminCommandHandler`, **espelhando exatamente o padrão já existente** de `modelos`/`variaveis` (`ReativarModeloAdminCommandHandler` / `ReativarVariavelAdminCommandHandler`): exige `motivo` (≥10 chars), chama `regiao.Reativar()` (`ativo=true`), grava audit (`ReativarRegiaoAnatomica` em `AcoesAuditAdmin`). Adicionar `reativar` ao `regioesGlobaisService`/`store` espelhando os services de modelos/variáveis.
- **Trava "filho de circunferencial" no backend** (R7): no `CriarRegiaoAdminCommandHandler`, após resolver o pai, se o pai tem `vista == 'circunferencial'`, lançar `BusinessException` (422) "Nós circunferenciais são agregadores e não aceitam sub-regiões." — regra de negócio no backend, front é UX antecipada.

**Não inclui**:
- Qualquer outra alteração de backend de regiões além das 2 acima (CRUD de criar/editar/inativar/excluir, validações R1–R5/R8/R9 e audit já completos).
- Drag-and-drop de reordenação (reordenação continua via campo `ordem`, conforme Wave 4).
- Edição de `codigo`/`vista`/`pai`/`nivel` após criação (permanecem imutáveis — comportamento atual correto).
- Qualquer mudança na parte clínica (B1/B2 já entregues e validados).
- Virtualização da árvore (volume baixo).

## 4. Regras de negócio

Todas as regras de mutação **já estão implementadas e validadas no backend** (handlers em `Application/Admin/Regioes/`). O front apenas **espelha** para UX — o 422 do `BusinessException` é a fonte da verdade.

- **R1 — Código único**: ao criar, código duplicado é rejeitado. Mora em: Backend (`CriarRegiaoAdminCommandHandler` → `ExisteCodigoAsync`). Validado em: back (existe) + front (UX: mensagem inline ao receber 422 "Já existe região anatômica com esse código.").
- **R2 — Vista do filho = vista do pai**: ao criar com `paiCodigo`, a vista deve ser idêntica à do pai (comparação `Ordinal`). Mora em: Backend (`CriarRegiaoAdminCommandHandler`). Validado em: back (existe) + front (UX: ao selecionar um pai, pré-preencher/forçar a vista igual à do pai, exibindo-a como derivada e não livremente editável; se divergir, mostrar a mensagem "Vista deve ser igual à do pai.").
- **R3 — Nível = nível do pai + 1**: filho tem `nivel = pai.nivel + 1`. Mora em: Backend (mesmo handler). Validado em: back (existe) + front (UX: ao informar pai, derivar nível automaticamente em vez de deixar livre).
- **R4 — Pai deve existir**: `paiCodigo` informado precisa apontar para região existente. Mora em: Backend (`ObterPorCodigoOuNulo`). Validado em: back (existe) + front (UX: mensagem ao 422 "Código do pai não encontrado.").
- **R5 — Hard-delete só sem filhos; soft-delete é a ação padrão**: `DELETE` é bloqueado pelo backend se a região tem filhos ("Esta região tem subgrupos. Inative em vez de excluir..."). Decisão de produto (Q4.4): na **UI**, **Inativar é a ação primária** de remoção; **Excluir permanentemente** é ação secundária, visualmente discreta e desabilitada quando há filhos. Mora em: Backend (`ExcluirRegiaoAdminCommandHandler` → `TemFilhosAsync`) + Front (hierarquia/visibilidade das ações). Validado em: back (existe) + front (UX da hierarquia de ações).
- **R6 — Inativar preserva, não apaga; Reativar restaura**: inativar é soft-delete (`regiao.Inativar()` seta `ativo=false`); o registro permanece e exames físicos já salvos que referenciam esse código continuam íntegros. Nós inativos somem das **novas** seleções no exame físico (B1/B2 filtram `ativo`) mas o histórico de prontuário é imutável. **Reativar** restaura (`regiao.Reativar()` seta `ativo=true`), tornando o nó novamente selecionável em novos exames. Mora em: Backend (`InativarRegiaoAdminCommandHandler` — existe — e **novo** `ReativarRegiaoAdminCommandHandler`, espelhando `ReativarModeloAdminCommandHandler`/`ReativarVariavelAdminCommandHandler`) + Front (badge "Inativo" na árvore + ação de Reativar para nós inativos). Validado em: back (inativar existe; reativar novo) + front (badge + ações). **Decisão fechada em 2026-06-09**: reativar entra com espelho de backend (ver seção 8).
- **R7 — Não criar filho de nó circunferencial**: os 9 nós `*-circunferencial` (nível 1) são **agregadores sintéticos** — não têm filhos próprios; suas sub-regiões clínicas vivem nos ramos `anterior`/`posterior` (mapeados em `regioesCircunferenciais.ts`/`RAMOS_CIRCUNFERENCIAL`). **Decisão fechada em 2026-06-09**: a regra é **espelhada no backend** (premissa não-negociável: regra de negócio no backend). Mora em: Backend (`CriarRegiaoAdminCommandHandler` — ao resolver o pai, se `pai.vista == 'circunferencial'` lança `BusinessException`/422 "Nós circunferenciais são agregadores e não aceitam sub-regiões.") + Front (guard de UX: não oferecer nó circunferencial como pai selecionável; se digitado, mostra a mesma mensagem e bloqueia o submit antes de bater no 422). Validado em: back (novo guard no handler) + front (guard de UX espelhando a mensagem).
- **R8 — Editar não toca estrutura nem reescreve histórico**: `PUT` altera apenas `nome` e `templateTexto`. Mudar `templateTexto` afeta só sugestões futuras — textos já gravados em exames físicos permanecem como foram salvos (template é sugestão no momento do registro, não vínculo vivo). Mora em: Backend (`AtualizarRegiaoAdminCommandHandler`, só nome+template) + Front (campos código/vista/pai/nível somente-leitura no modo edição — já implementado). Validado em: back (existe) + front (existe).
- **R9 — Motivo obrigatório (≥10 caracteres) em toda mutação**: criar/editar/inativar/reativar/excluir exigem `motivo` para o audit trail. Mora em: Backend (todos os handlers) + Front (validação já existe). Validado em: back (existe) + front (existe).

## 5. Modelo de dados

**Nenhuma migration.** Tabela `regioes_anatomicas_catalogo` já existe e suporta `vista='circunferencial'` sem alteração de schema (confirmado nos seeds B1). Audit já persiste em `imedto_admin_audit_log` via `ImedtoAdminAuditWriter` (ações `CriarRegiaoAnatomica`, `AtualizarRegiaoAnatomica`, `InativarRegiaoAnatomica`, `ExcluirRegiaoAnatomica` em `AcoesAuditAdmin`; **adicionar `ReativarRegiaoAnatomica`** junto com o novo handler de reativar — decisão fechada 2026-06-09). Toda mutação grava `{admin_id, recurso_tipo='regiao_anatomica', recurso_id, motivo, timestamp}`.

Catálogo é **global** (sem `estabelecimento_id`) — é compartilhado por todos os tenants. O isolamento aqui é por **papel** (só `ImedtoAdmin`), não por tenant.

## 6. UX e fluxo

Reúso integral do design system admin (já documentado em `Docs/DESIGN.md §Módulo Admin Global`). Componentes em uso: `AppPageHeader`, `AppCard`, `AppEmptyState`, `AppButton`, `AppModal`, `AppField`, `AppInput`, `AppTextarea`, `AppSelect`, `AppBadge`, `RegiaoTreeView`.

**Lista/árvore** (`RegioesGlobaisListView` + `RegiaoTreeView`):
- Cada nó mostra: código (mono), nome, badge de vista, badge "bilateral" (se `lateralidade`), badge "Inativo" (se `!ativo`), contagem de filhos, e ações.
- Ações por nó: **Editar** (lápis); **Inativar** (ação primária de remoção) quando `ativo`; **Reativar** quando `!ativo`; **Excluir permanentemente** (lixeira, discreta/secundária) — desabilitada quando o nó tem filhos, com tooltip explicativo.
- Filtro "Incluir inativas" (já existe) — quando ligado, nós inativos aparecem esmaecidos (já existe `arvore-no--inativo`).
- Cada mutação destrutiva (inativar/excluir) abre `AppModal` exigindo motivo (≥10 chars) — o modal de exclusão já existe; replicar o mesmo padrão para inativar (texto e severidade diferentes: inativar é reversível, excluir não).

**Form** (`RegioesGlobaisFormView`):
- Criação: `codigo`, `nome`, `vista` (apenas anterior/posterior/circunferencial/sem-vista), `paiCodigo`, `nivel` (derivado do pai quando há pai), `ordem`, `lateralidade`, `templateTexto`, `motivo`.
- Ao selecionar/digitar um pai: derivar vista e nível do pai (R2/R3), exibindo-os como derivados; não permitir pai circunferencial (R7).
- Edição: código/vista/pai/nível somente-leitura (já implementado); editáveis: nome, template, motivo.

**Estados**: loading (spinner — existe), erro de rede (`store.erro` — existe), vazio (`AppEmptyState` — existe), sucesso (redirect para lista — existe). Validar todos após as mudanças.

## 7. Critérios de aceite (testáveis)

- **CA1 (listar árvore — caminho feliz)**: Dado um super-admin autenticado em `/admin/catalogos/regioes`, Quando a tela carrega, Então a árvore hierárquica (nível 1 → 2 → 3) é renderizada via `GET /api/admin/catalogos/regioes-anatomicas`, com os 9 nós `*-circunferencial` visíveis como nível 1 e expansão/colapso funcional.
- **CA2 (vista correta no form)**: Dado o form de criação, Quando o usuário abre o seletor de vista, Então as únicas opções são "Sem vista", "Anterior", "Posterior" e "Circunferencial" — e `lateral_direita`/`lateral_esquerda` NÃO aparecem.
- **CA3 (criar sub-região — caminho feliz)**: Dado um pai `torax-anterior` (nível 1, vista anterior), Quando o admin cria um filho informando esse pai, Então a vista é derivada para "anterior" e o nível para 2 automaticamente, e o `POST` retorna 201.
- **CA4 (código único)**: Dado um código já existente, Quando o admin tenta criar, Então o backend retorna 422 e a UI exibe inline "Já existe região anatômica com esse código." sem derrubar o formulário.
- **CA5 (vista × pai)**: Dado um pai de vista "posterior", Quando o admin tenta forçar vista diferente no filho, Então a UI impede (vista derivada do pai) e, caso o 422 ocorra, exibe "Vista deve ser igual à do pai.".
- **CA6 (não criar filho de circunferencial — UX/front)**: Dado o nó `torax-circunferencial`, Quando o admin abre o seletor de pai, Então a UI não o oferece como pai selecionável; e Quando o admin digita manualmente um código circunferencial como pai, Então a UI mostra "Nós circunferenciais são agregadores e não aceitam sub-regiões." e bloqueia o submit antes de chamar a API.
- **CA6b (trava de circunferencial espelhada no backend — 422)**: Dado uma chamada direta `POST /api/admin/catalogos/regioes-anatomicas` com `paiCodigo` apontando para um nó de `vista='circunferencial'` (burlando o guard de front), Quando o `CriarRegiaoAdminCommandHandler` resolve o pai, Então retorna **422** com a mensagem "Nós circunferenciais são agregadores e não aceitam sub-regiões." e **nenhuma região filha é persistida**.
- **CA7 (inativar é a ação primária — soft-delete)**: Dado um nó ativo na árvore, Quando o admin abre as ações, Então "Inativar" é a ação de remoção em destaque e "Excluir permanentemente" é secundária/discreta; ao confirmar inativar com motivo ≥10 chars, o `POST /{id}/inativar` é chamado, o nó passa a exibir badge "Inativo" e permanece na base.
- **CA8 (soft-delete preserva prontuário)**: Dado um nó inativado que era referenciado por um exame físico já salvo, Quando se consulta esse prontuário antigo, Então o achado registrado continua íntegro e legível; e Quando um profissional inicia um NOVO exame físico, Então o nó inativo não aparece entre as regiões selecionáveis. (Validação cruzada com B1/B2 — filtro `ativo`.)
- **CA9 (reativar — caminho feliz front+back)**: Dado um nó inativo na árvore, Quando o admin clica em "Reativar" e confirma com motivo ≥10 chars, Então o front chama `POST /api/admin/catalogos/regioes-anatomicas/{id}/reativar`, o `ReativarRegiaoAdminCommandHandler` seta `ativo=true`, o nó perde o badge "Inativo", e ele volta a aparecer nas novas seleções de exame físico (validação cruzada B1/B2 — filtro `ativo`).
- **CA9b (reativar — motivo obrigatório e audit)**: Dado a ação de reativar, Quando o motivo tem menos de 10 caracteres, Então o botão de confirmação fica desabilitado e o backend, se chamado, retorna 422 "Informe o motivo... (mínimo 10 caracteres)."; e Quando a reativação é bem-sucedida, Então uma linha é gravada em `imedto_admin_audit_log` com `{admin_id, recurso_tipo='regiao_anatomica', recurso_id, motivo, timestamp}` e ação `ReativarRegiaoAnatomica`.
- **CA10 (hard-delete bloqueado com filhos)**: Dado um nó com sub-regiões, Quando o admin tenta "Excluir permanentemente", Então o botão está desabilitado com tooltip "Possui sub-regiões — inative ou remova-as primeiro"; e, se forçado, o backend retorna 422 com a mensagem de subgrupos.
- **CA11 (editar não reescreve histórico)**: Dado um nó com `templateTexto` "Sem alterações.", Quando o admin altera o template para outro texto, Então exames físicos já gravados com o texto antigo permanecem inalterados; só novos registros recebem a nova sugestão.
- **CA12 (RBAC / segurança multi-tenant)**: Dado um usuário de estabelecimento comum (sem claim `ImedtoAdmin`), Quando tenta acessar `/admin/catalogos/regioes` ou chamar `GET/POST/PUT/DELETE /api/admin/catalogos/regioes-anatomicas`, Então o guard de rota redireciona para `/admin/login` no front e a API responde 401/403 (policy `ImedtoAdmin`) — nenhum dado do catálogo é exposto a não-admin.
- **CA13 (LGPD / audit)**: Dado qualquer mutação (criar/editar/inativar/reativar/excluir), Quando ela ocorre, Então uma linha é gravada em `imedto_admin_audit_log` com `{admin_id, recurso_tipo='regiao_anatomica', recurso_id, motivo, timestamp}`; e Dado um erro de validação, Quando o 422 retorna, Então a mensagem é genérica/clínica e não vaza dados de paciente (o catálogo não contém PII de paciente, mas a mensagem deve permanecer genérica).
- **CA14 (estado vazio)**: Dado um catálogo sem regiões (cenário de base limpa), Quando a tela carrega, Então `AppEmptyState` é exibido com "Nenhuma região cadastrada." e o botão "Nova região" permanece acessível.
- **CA15 (erro de rede)**: Dado falha na chamada de listagem, Quando a tela carrega, Então a mensagem genérica "Não foi possível carregar as regiões anatômicas." é exibida com `role="alert"` e há ação de "Atualizar" para retry.
- **CA16 (motivo obrigatório)**: Dado qualquer mutação, Quando o motivo tem menos de 10 caracteres, Então o botão de confirmação fica desabilitado e o backend, se chamado, retorna 422 "Informe o motivo... (mínimo 10 caracteres).".

## 8. Riscos e dependências

- **Dependência de infra admin: NÃO EXISTE PENDÊNCIA.** Login admin, layout, router e guard estão completos e em produção desde a Wave 3/4. B3 não precisa criá-los.
- **DECISÃO 1 — Reativar (R6/CA9/CA9b) — FECHADA EM 2026-06-09: opção (a) APROVADA.** Entra no escopo: endpoint `POST /api/admin/catalogos/regioes-anatomicas/{id}/reativar` + `ReativarRegiaoAdminCommandHandler` espelhando o padrão de `modelos`/`variaveis`, mais o religamento na UI (service/store + ação na árvore). Sem reativar, a UI ofereceria inativar sem caminho de volta. Esforço pequeno, padrão já existente. **Aciona `imedto-developer` (backend leve + front).**
- **DECISÃO 2 — Trava de "filho de circunferencial" (R7/CA6/CA6b) — FECHADA EM 2026-06-09: opção (b) APROVADA.** A regra é espelhada no `CriarRegiaoAdminCommandHandler` (BusinessException/422 quando o pai é circunferencial), mantendo a premissa não-negociável "regra de negócio no backend"; o guard de front permanece como UX antecipada. **Aciona `imedto-developer` (backend leve + front).**
- **Risco regressivo — exame físico clínico (B1/B2)**: o filtro `ativo` que esconde nós inativos das novas seleções é responsabilidade da parte clínica já entregue. CA8/CA9 devem ser validados cruzando com `SecaoExameFisico`/`RegionSelectorPopup` para garantir que inativar/reativar reflete corretamente lá. Sem mudança de código clínico nesta entrega.
- **Drift de documentação**: `Docs/DESIGN.md` afirma que `RegiaoTreeView` emite `select`, `criar-filho`, `editar`, `excluir`, `inativar`, `reativar`; a implementação atual emite só `editar`, `inativar`, `excluir` e a lista ignora `inativar`. A entrega deve **reconciliar** código e doc (seção 10).

**Dimensionamento final**: esforço **P**. `imedto-developer` é o executor principal, atuando em **FRONTEND** (vocabulário de vista, religar inativar/reativar, guards de UX, estados) **+ BACKEND leve** (2 mudanças triviais já cravadas: endpoint/handler de reativar espelhando `modelos`/`variaveis`; trava de circunferencial no `CriarRegiaoAdminCommandHandler` com 422). **`imedto-database` NÃO é necessário** (sem schema, sem migration, sem índice — `ativo` e a relação pai já existem). **Não há motivo para fatiar o B3.**

## 9. Observações para execução

- **Não-negociável**: o vocabulário de vista no form deve bater exatamente com o catálogo real (`anterior`/`posterior`/`circunferencial`/sem-vista). Remover os valores `lateral_*` inventados.
- **Não-negociável**: inativar (soft-delete) é a ação de remoção primária na UI; hard-delete é secundário e bloqueado com filhos (Q4.4 / R5).
- **Reúso obrigatório**: o store (`regioesGlobaisStore`) e o service (`regioesGlobaisService`) já têm `inativar` — apenas religar na UI. Não duplicar. O `reativar` (front e backend) **deve copiar fielmente** o padrão de `modelosGlobaisService`/`variaveisGlobaisService` e `ReativarModeloAdminCommandHandler`/`ReativarVariavelAdminCommandHandler` — não inventar contrato novo.
- **Liberdade técnica**: forma de derivar vista/nível do pai no form (computed a partir do pai selecionado, ou bloqueio do campo) fica a critério do dev, desde que satisfaça CA3/CA5.
- **Espelho back+front**: toda mensagem de erro exibida deve vir do 422 do backend quando ele for a fonte (R1/R2/R4/R5/R7); guards de front são UX antecipada, nunca substituem a validação do back. A mensagem da trava de circunferencial (R7/CA6b) deve ser idêntica no front e no backend.
- **Backend leve cravado** (decisões 1 e 2 da seção 8, aprovadas em 2026-06-09): registrar a nova ação `ReativarRegiaoAnatomica` em `AcoesAuditAdmin` e auditá-la como as demais mutações. Não acionar `imedto-database` — não há schema novo.

## 10. Atualização de documentação

- **`Docs/DESIGN.md` §Módulo Admin Global** — reconciliar a descrição de `RegiaoTreeView.vue` com a implementação final: lista de eventos realmente emitidos (`editar`, `inativar`, `reativar`, `excluir`) e a regra de UX de que **inativar é a ação primária e excluir é secundária/bloqueada-com-filhos**. Ajuste cirúrgico só nessa entrada (linha ~95). Também corrigir a menção a "Agrupamento por vista (anterior/posterior)" para incluir `circunferencial`.
- **`Docs/LGPD.md`** — adicionar nota curta na seção de audit/admin: mutação do catálogo global de regiões anatômicas (criar/editar/inativar/reativar/excluir) é auditada em `imedto_admin_audit_log` com motivo obrigatório; soft-delete (inativar) é a política padrão para preservar a integridade de prontuários que referenciam o código. O catálogo é global e não contém PII de paciente — mensagens de erro permanecem genéricas. Ajuste incremental, não reescrita.
- **`Docs/ARQUITETURA.md`** — nenhuma mudança (sem novo padrão de DI/bus/store).
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — nenhuma mudança (sem recurso AWS, sem comando novo, sem migration).
