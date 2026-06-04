# Seletor de troca de estabelecimento + último acessado persistido

**ID**: 2026-06-04_004
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento (resolução de tenant no boot), autenticação (bootstrap), nenhuma alteração em orçamento/prontuário/estoque

## 1. Contexto e motivação

Usuário com multi-vínculo (Dono e/ou Profissional em N estabelecimentos) hoje sofre três problemas concretos:

**Problema 1 — A troca de estabelecimento não tem seletor. É uma roleta.**
`frontend/src/layouts/AppLayout.vue:119-123` — a ação "Trocar estabelecimento" do menu de perfil simplesmente faz:
```
function trocarEstabelecimento() {
    tenant.limpar()
    window.location.href = "/home"
}
```
Ela limpa o tenant e recarrega. Como o boot re-seleciona pela mesma regra (primeiro da lista), o usuário **volta para o mesmo estabelecimento** — não há como escolher para qual ir. O nome "Trocar estabelecimento" é uma promessa que o código não cumpre.

**Problema 2 — A seleção inicial é arbitrária (`lista[0]`), não a intenção do usuário.**
`frontend/src/stores/tenantStore.ts:103-172` — `popularEstabelecimentos` auto-seleciona sempre `lista[0]` (linhas 148-155 e 164-171). O comentário no código (`tenantStore.ts:99-100`) já admite a limitação: *"2+: auto-seleciona o primeiro (troca via seletor no header)"* — só que o seletor nunca existiu. Quem trabalha o dia inteiro no estabelecimento B, mas B não é o primeiro da lista, recomeça toda sessão no A.

**Problema 3 — Nada lembra onde o usuário estava.**
A escolha de tenant vive só em `sessionStorage` (`tenantStore.ts:9`, chave `imedto.estabelecimentoAtivo`), que morre ao fechar o navegador. Não há persistência server-side da última preferência. `MeUsuarioDto` (`backend/.../Auth/Queries/Results/MeUsuarioDto.cs`) e `BootstrapMeDto` (`.../Auth/Queries/Results/BootstrapMeDto.cs:11-14`) não carregam essa informação.

Decisões fechadas com o usuário em 2026-06-04: "último estabelecimento" = **último acessado** (o que estava em uso na sessão anterior); persistência no **backend** (nova coluna `usuarios.ultimo_estabelecimento_id`); troca **com reload completo**; item do menu **só aparece se houver 2+ estabelecimentos acessíveis**; grava o último acessado **ao trocar manualmente E na primeira resolução do boot** (se ainda não houver registro).

## 2. Persona-alvo

Profissional/Dono com multi-vínculo (atua em 2+ estabelecimentos). Momento da jornada: início de sessão (login/boot) e durante o expediente quando precisa pular de uma unidade para outra. Frequência: diária para quem é multi-unidade; irrelevante para quem tem 1 só (por isso o item some).

## 3. Escopo

**Inclui**:
- Coluna nova `usuarios.ultimo_estabelecimento_id` (nullable, FK lógica para estabelecimento).
- Exposição do `ultimoEstabelecimentoId` no `BootstrapMeDto` (via `MeUsuarioDto`).
- Resolução do tenant no boot priorizando `ultimoEstabelecimentoId`, com fallback ao comportamento atual (`lista[0]`).
- Novo comando CQRS + endpoint leve para gravar o último estabelecimento acessado, com validação multi-tenant falha-fechada.
- Gravação do último acessado em dois gatilhos: troca manual e primeira resolução do boot (quando ainda não há registro).
- Nova modal de seleção de estabelecimento (design system) acionada pelo item "Trocar estabelecimento".
- Visibilidade condicional do item (oculto quando há 1 só estabelecimento acessível).

**Não inclui**:
- Trocar tenant sem reload (SPA "hot-swap" de estabelecimento) — decisão C1 manteve `window.location.href`.
- Busca/filtro/paginação dentro da modal (lista de estabelecimentos de um usuário é pequena; não há requisito de volume).
- Histórico de estabelecimentos acessados (só o último).
- Audit trail dedicado da troca de tenant (não é acesso a prontuário/PII; é preferência operacional do próprio usuário sobre si mesmo).

## 4. Regras de negócio

- **R1** — "Último estabelecimento" significa o **último acessado** (o que estava ativo na sessão anterior), não o mais antigo nem o alfabético. Mora em: Domain/Handler (gravação) + Front (boot resolve por esse id). Validada em: back (persiste o id) + front (prioriza no boot).
- **R2** — A gravação de `ultimo_estabelecimento_id` só é aceita se o usuário tem **vínculo Ativo** com o estabelecimento alvo **ou é Dono** dele. Caso contrário: falha-fechada, `BusinessException`/404 com mensagem genérica, e **nada é persistido**. Mora em: Handler (comando) + Domain. Validada em: back (fonte da verdade) + front (UX — a modal só lista estabelecimentos acessíveis, então o caminho normal nunca tenta gravar inválido).
- **R3** — Visibilidade do item "Trocar estabelecimento": aparece apenas quando o total de estabelecimentos acessíveis (Dono + vínculos Ativos, ou seja `estabelecimentos.length` do bootstrap) for **> 1**. Oculto quando `== 1` (ou `0`). Mora em: Front (AppLayout). Sem espelho de back (é só visibilidade de UI; a regra de acesso de R2 protege o endpoint).
- **R4** — Resolução do tenant no boot: se `ultimoEstabelecimentoId` está presente **e existe na lista de acessíveis**, seleciona-o. Se nulo, órfão (id não está mais na lista) ou ausente, cai no comportamento atual (`lista[0]`) **e grava o resolvido** (E1) — para que a próxima sessão já tenha registro. Mora em: Front (tenantStore) + Handler (gravação do resolvido). Validada em: front (lógica de seleção) + back (persiste).
- **R5** — Ao trocar manualmente pela modal: persiste o novo `ultimo_estabelecimento_id` no backend **e** executa reload completo (`window.location.href`). A persistência precede ou acompanha o reload; falha na persistência **não bloqueia** a troca (R7). Mora em: Front (modal + tenantStore) + Handler. Validada em: back + front.
- **R6** — Multi-tenant ao trocar: ao mudar de estabelecimento, permissões e assinatura do tenant anterior **não vazam**. O fluxo atual já trata isso — `tenantStore.selecionar` redefine `permissoesStore` (`tenantStore.ts:63-67`) e limpa `assinaturaStore` quando troca (`tenantStore.ts:68-70`); o reload completo re-hidrata tudo do zero via `/auth/bootstrap`. Mora em: Front (tenantStore + boot). Validada em: front (verificar que nenhum dado do tenant anterior persiste após troca).
- **R7** — Degradação graciosa: se o POST de gravação do último acessado falhar (rede/500), a troca **acontece mesmo assim** (reload prossegue). A consequência é apenas que a próxima sessão pode não lembrar a escolha — aceitável. Não exibir erro bloqueante ao usuário. Mora em: Front (catch silencioso, log de diagnóstico sem PII). Validada em: front.

## 5. Modelo de dados

- **Tabela `usuarios`** — adicionar coluna `ultimo_estabelecimento_id BIGINT NULL` (tipo coerente com a PK de `estabelecimentos` no schema atual — o `imedto-database` confirma o tipo real da PK ao gerar a migration). Nullable por padrão para todos os usuários existentes.
  - **FK**: referência lógica para `estabelecimentos(id)`. Decisão sobre `ON DELETE SET NULL` vs FK explícita fica a critério do `imedto-database` — o requisito de produto é: **id órfão (estabelecimento removido) é tolerado** e tratado no boot como fallback (R4). Se FK com `ON DELETE SET NULL` simplificar, ótimo; se não, o front já trata órfão.
  - **Índice**: não há requisito de busca por essa coluna (sempre lida por `usuario_id`, que já é PK). Índice provavelmente desnecessário — decisão do `imedto-database`.
- **PII / LGPD**: `ultimo_estabelecimento_id` é preferência operacional do próprio usuário sobre si mesmo, não é dado de saúde nem PII de terceiro. Não exige audit trail dedicado. Já é minimizado (um único bigint).
- **`MeUsuarioDto`** — adicionar campo `long? UltimoEstabelecimentoId` (serializado como `ultimoEstabelecimentoId`). Manter a minimização: nenhum outro campo novo.

## 6. UX e fluxo

**Item de menu (AppLayout, perfil popover)** — `AppLayout.vue:203-205`:
- "Trocar estabelecimento" só renderiza quando `estabelecimentos.length > 1` (R3). Quando há 1, o item some (não fica desabilitado — some).

**Modal de seleção** (novo componente, ex. `EstabelecimentoSeletorModal.vue`):
- Usa o design system: `AppModal` (`frontend/src/components/ui/AppModal.vue`).
- Título: "Trocar estabelecimento".
- Lista vertical de estabelecimentos acessíveis (a mesma lista do bootstrap / tenantStore). Cada item exibe:
  - Nome fantasia.
  - Papel do usuário naquele estabelecimento via **`AppRolePill`** (Dono / Profissional) — reuso do componente existente no design system.
  - Indicador visual do **estabelecimento atualmente ativo** (destacado e desabilitado/não-clicável — clicar nele não faz nada, já está ativo).
- Ação **Selecionar** (clicar num item != ativo): dispara persistência (R5) + reload.
- Ação **Cancelar / fechar**: fecha a modal sem efeito colateral (padrão `AppModal`).
- Estados:
  - **loading**: a lista já vem do tenantStore/bootstrap (em memória) — abertura é instantânea, não há fetch. Se o POST de gravação estiver em voo ao selecionar, o item pode mostrar estado de carregando até o reload disparar.
  - **erro**: ver R7 — falha na gravação não bloqueia; a troca prossegue.
  - **vazio**: não aplicável (modal só abre com 2+; nunca vazia).
  - **sucesso**: reload completo leva o usuário ao novo tenant.
- **Mobile-ready**: lista vertical responsiva; `AppModal` já é responsivo.

## 7. Critérios de aceite (testáveis)

- **CA1** (visibilidade — 1 estab): Dado um usuário com exatamente 1 estabelecimento acessível, Quando abre o menu de perfil, Então o item "Trocar estabelecimento" **não é renderizado** no DOM.
- **CA2** (visibilidade — 2+ estab): Dado um usuário com 2 ou mais estabelecimentos acessíveis, Quando abre o menu de perfil, Então o item "Trocar estabelecimento" aparece e, ao clicar, abre a modal de seleção.
- **CA3** (modal — conteúdo e design system): Dado um usuário multi-vínculo, Quando abre a modal, Então ela usa `AppModal`, lista cada estabelecimento com nome fantasia + `AppRolePill` do papel, e o item correspondente ao estabelecimento ativo aparece destacado e desabilitado (não clicável).
- **CA4** (modal — selecionar): Dado o usuário na modal, Quando clica num estabelecimento diferente do ativo, Então o front grava o último acessado no backend e dispara reload completo (`window.location.href`), reabrindo a SPA já no estabelecimento escolhido.
- **CA5** (modal — cancelar): Dado o usuário na modal, Quando clica em Cancelar/fechar, Então a modal fecha, nenhum estabelecimento é trocado e nenhuma chamada de gravação é feita.
- **CA6** (persistência ao trocar): Dado um usuário no estabelecimento A, Quando seleciona o B na modal, Então `usuarios.ultimo_estabelecimento_id` passa a valer o id de B (verificável no banco) e, na próxima sessão/boot, o tenant resolvido é B.
- **CA7** (multi-tenant na gravação — falha-fechada): Dado um usuário **sem vínculo Ativo e não-Dono** do estabelecimento X, Quando o endpoint de gravação é chamado com o id de X (request forjada fora do fluxo da modal), Então recebe 404/422 com mensagem genérica, `ultimo_estabelecimento_id` **não é alterado** e nada com PII é logado.
- **CA8** (boot — usa último acessado): Dado um usuário cujo `ultimoEstabelecimentoId` aponta para um estabelecimento que ainda existe na sua lista de acessíveis, Quando a SPA faz boot, Então o tenant ativo resolvido é esse estabelecimento (não `lista[0]`).
- **CA9** (boot — fallback nulo + grava resolvido): Dado um usuário com `ultimoEstabelecimentoId` nulo, Quando faz boot, Então o tenant resolvido cai em `lista[0]` (comportamento atual) **e** o backend passa a registrar esse id como último acessado (verificável: após o boot, a coluna deixa de ser nula).
- **CA10** (boot — fallback órfão): Dado um usuário cujo `ultimoEstabelecimentoId` aponta para um estabelecimento que **não está mais** na sua lista (vínculo removido), Quando faz boot, Então o tenant resolvido cai em `lista[0]`, não há erro/crash, e o id órfão é substituído pelo resolvido na próxima gravação.
- **CA11** (multi-tenant ao trocar — sem vazamento): Dado um usuário no estabelecimento A com permissões/assinatura de A carregadas, Quando troca para B, Então após o reload o `permissoesStore` e o `assinaturaStore` refletem **somente** B — nenhuma permissão ou dado de assinatura de A permanece.
- **CA12** (degradação graciosa): Dado que o POST de gravação do último acessado falha (rede/500), Quando o usuário troca de estabelecimento, Então a troca acontece mesmo assim (reload prossegue para o estabelecimento escolhido) e nenhum erro bloqueante é exibido ao usuário.

## 8. Riscos e dependências

- **Schema**: depende do `imedto-database` para a migration (coluna + tipo coerente com a PK de `estabelecimentos`, decisão de FK/`ON DELETE`). Banco roda em container EC2 (ver memória do projeto) — migration em `db/migrations/`, SQL idempotente.
- **Regressão no boot**: `main.ts:42-57` e `tenantStore.popularEstabelecimentos` são caminho crítico de toda sessão. A lógica de "tenant órfão" já existe (`main.ts:50-56`) e precisa coexistir com a nova prioridade do `ultimoEstabelecimentoId` — cuidado para não duplicar/conflitar a checagem de órfão.
- **Reuso**: o endpoint de gravação deve verificar se já existe fluxo de "atualizar preferência do usuário" antes de criar comando novo — se não houver, novo comando CQRS é justificado. A lista de estabelecimentos da modal **reusa** a já presente no tenantStore/bootstrap (não fazer novo fetch).
- **Dependência de componente**: `AppModal` e `AppRolePill` devem existir e cobrir o caso; se `AppRolePill` não aceitar os papéis "Dono"/"Profissional", alinhar com o dev.

## 9. Observações para execução

**Não-negociável**:
- R2 (validação multi-tenant falha-fechada na gravação) e R7 (degradação graciosa) são obrigatórios.
- Boot deve priorizar `ultimoEstabelecimentoId` e fazer fallback seguro (R4) sem quebrar a lógica de órfão já existente.
- Reuso da lista de estabelecimentos do tenantStore na modal — **proibido** novo fetch para popular a modal.
- Item "Trocar estabelecimento" some (não desabilita) quando há 1 estabelecimento.

**Liberdade técnica**:
- Nome do componente da modal, nome do comando/endpoint CQRS, e a decisão de FK/`ON DELETE`/índice ficam a critério do dev/db, respeitando os CAs.
- Forma de gravar o "resolvido no boot" (E1): pode ser chamada disparada pelo front após resolver, ou parte do próprio handler de bootstrap — o dev escolhe o ponto, desde que CA9 passe.

**Arquivos-âncora** (para orientar o dev, não exaustivo):
- `frontend/src/layouts/AppLayout.vue` (item de menu :203-205, função :119-123).
- `frontend/src/stores/tenantStore.ts` (resolução :103-172, `selecionar` :58-71).
- `frontend/src/main.ts` (boot :42-57).
- `backend/.../Auth/Queries/Results/MeUsuarioDto.cs` e `BootstrapMeDto.cs`.
- `backend/.../Auth/Queries/BootstrapMeQueryHandlers.cs` + `UsuarioQueryRepository` / `EstabelecimentoQueryRepository`.
- Novo comando em `backend/.../Application/...` para gravar último acessado (escrita via EF Core).
- `frontend/src/components/ui/AppModal.vue`, `AppRolePill`.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — atualizar a seção de Autenticação/Bootstrap: o `BootstrapMeDto`/`MeUsuarioDto` agora expõe `ultimoEstabelecimentoId`, e a resolução de tenant no boot prioriza o último estabelecimento acessado (persistido server-side) com fallback para `lista[0]`. Documentar o novo comando CQRS de "gravar último estabelecimento acessado" e sua validação multi-tenant. Mudança incremental, cirúrgica — só as subseções de bootstrap/resolução de tenant.
- **`Docs/INFRA.md`** — não se aplica (sem recurso AWS novo). A migration entra pelo fluxo padrão de `db/migrations/`.
- **`Docs/LGPD.md`** — não se aplica (preferência operacional do próprio usuário, sem PII de terceiro, sem audit dedicado).
- **`Docs/COMANDOS.md`** — não se aplica (sem script/comando novo; migration segue o fluxo existente).
