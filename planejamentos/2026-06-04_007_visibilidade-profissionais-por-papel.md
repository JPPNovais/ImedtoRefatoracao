# Visibilidade dos profissionais por papel — contagem do Dono no Admin e popover de equipe

**ID**: 2026-06-04_007
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento (aba Papéis e permissões / Equipe)

## 1. Contexto e motivação

Na aba **"Papéis e permissões"** (`AbaPapeis.vue`), cada papel — tanto os modelos do sistema (ex.: "Admin") quanto os customizados — exibe um contador "N profissionais". Hoje esse contador tem dois problemas de leitura para o gestor:

1. **O Dono não é contado em lugar nenhum.** O dono do estabelecimento chega no array `profissionais` com `status === 'Dono'` e `modeloPermissaoId === null`. Como o contador filtra por `modeloPermissaoId === m.id`, o dono nunca cai em nenhum card — mesmo tendo, na prática, o acesso máximo (equivalente a Admin). O gestor olha o card "Admin" e vê "0 profissionais" mesmo sabendo que ele próprio tem acesso total. Gera desconfiança sobre o permissionamento.
2. **O contador é um número morto.** Ele diz "3 profissionais" mas não diz *quem*. Para saber quem usa um papel, o gestor precisa ir até a aba Equipe e cruzar manualmente. Atrito operacional real em clínicas com equipe média/grande.

Esta demanda resolve os dois: conta o Dono dentro do card "Admin" (com selo visual distinguindo que é acesso imutável de dono, não o template) e torna o contador clicável, abrindo um popover só-leitura com avatar + nome + status de cada profissional daquele papel.

## 2. Persona-alvo

**Dono ou Administrador** do estabelecimento, na tela de configuração de equipe, aba "Papéis e permissões". Frequência: baixa/recorrente (quando audita quem tem qual acesso, ao convidar/remover gente ou revisar permissões). Momento da jornada: gestão de equipe e governança de acesso — não é fluxo de atendimento.

## 3. Escopo

**Inclui**:
- Contar o Dono dentro do card do modelo padrão "Admin" (e no badge do detalhe do mesmo papel quando selecionado).
- Selo/badge "Dono" diferenciando visualmente o dono dos demais profissionais, dentro do popover do papel Admin.
- Tornar **ambos** os contadores clicáveis: (a) o "N profissionais" no card da lista lateral e (b) o badge "N profissionais" no topo do detalhe à direita.
- Popover só-leitura listando os profissionais daquele papel: `AppAvatar` + nome + status (Ativo / Inativo / Convidado / Dono). Altura máxima com scroll (~6 itens visíveis).
- Criação de um componente `AppPopover` reutilizável no design system (não existe equivalente hoje — ver R7 e seção 9).

**Não inclui**:
- Qualquer alteração de backend, contrato, query, DTO ou migration. **Mudança 100% frontend.** Os dados de Dono (`status === 'Dono'`, `modeloPermissaoId === null`), nome, foto e status já chegam nos arrays `modelos` e `profissionais` recebidos via props por `AbaPapeis.vue`.
- Navegação do popover para o perfil do profissional (é só-leitura).
- Edição de papel/permissões a partir do popover.
- Mudar a regra de contagem dos papéis customizados (continua `modeloPermissaoId === m.id`).
- Reordenar ou paginar a lista de profissionais dentro do popover (scroll basta).

## 4. Regras de negócio

> Observação cross-cutting: como esta demanda **não altera backend**, o "espelho back+front" das regras multi-tenant/LGPD já está garantido pelas queries existentes que populam `profissionais` (todas filtram `estabelecimento_id` no servidor). O front aqui apenas **renderiza** o que o back já minimizou. Nenhuma regra nova de autorização ou exposição é criada no front.

- **R1 — Contagem do Dono no Admin.** O profissional com `status === 'Dono'` é contado dentro do card cujo modelo é o **padrão "Admin"**. Critério de matching: `m.ehPadrao === true && m.nome === 'Admin'`. Mora em: Front (`AbaPapeis.vue`, função de contagem). Validada em: front (é regra de apresentação; o back não tem conceito de "Dono pertence ao Admin").

- **R2 — Caso de borda: não existe modelo Admin padrão.** Se nenhum modelo satisfizer `ehPadrao === true && nome === 'Admin'`, o dono **não é forçado em nenhum card** — não é contado em papel algum, nem aparece em popover de papel algum (fallback seguro: nada quebra, nenhum dono "vaza" para outro template). Mora em: Front.

- **R3 — Caso de borda: dono também possui vínculo com modelo atribuído.** Um mesmo profissional **nunca é contado duas vezes**. Se o registro de dono (`status === 'Dono'`) e um registro de vínculo com `modeloPermissaoId` apontarem para a mesma pessoa, a deduplicação é por identidade do profissional (campo de id do profissional disponível no DTO — ver seção 5). O registro com `status === 'Dono'` tem precedência de exibição no card Admin (mostra o selo "Dono"); o registro de vínculo correspondente não é recontado nesse mesmo card. Mora em: Front.

- **R4 — Contadores clicáveis.** O contador do card lateral e o badge do detalhe abrem, ao clique, o popover do respectivo papel. Mora em: Front.

- **R5 — Contagem 0 não é clicável.** Quando a contagem efetiva de um papel é `0`, o contador/badge daquele papel **não** abre popover (sem cursor de clique, sem handler ativo). Mora em: Front.

- **R6 — Popover só-leitura e minimizado.** O popover lista, por profissional: `AppAvatar` (foto via `fotoUrl`, fallback iniciais) + nome (`nomeCompleto`) + status. Não há link, botão de ação, nem navegação. Só os campos já exibidos na aba Equipe — nenhum dado clínico, nenhum PII além de nome+foto+status. Mora em: Front.

- **R7 — Componente de popover no design system.** Não existe `AppPopover`/`AppDropdown`/`AppMenu` reutilizável em `frontend/src/components/ui/` (só `AppCard`, `AppStatCard`, `AppAvatar`). Pela premissa "design system primeiro", criar `AppPopover` genérico em `frontend/src/components/ui/` e consumi-lo aqui. Mora em: Front (design system).

## 5. Modelo de dados

**Nenhuma alteração de schema, migration ou contrato.** Dados consumidos (já existentes):

- `ProfissionalVinculado` (`frontend/src/services/vinculoService.ts`): `nomeCompleto`, `status` (`'Dono' | 'Ativo' | 'Inativo' | 'Convidado' | 'Removido'`), `modeloPermissaoId: number | null`, `fotoUrl?: string | null`, e o identificador do profissional usado na deduplicação da R3.
  - **Dev**: confirmar no tipo o campo de id estável do profissional (ex.: `profissionalId`/`id`) para a deduplicação da R3. Se não houver id estável de profissional além do id de vínculo, a deduplicação cai em fallback por `nomeCompleto` normalizado — documentar a escolha no PR e cobrir no teste.
- `ModeloPermissao` (`frontend/src/services/permissaoService.ts`): `id`, `nome`, `ehPadrao`, `cor`, `icone`.

**Multi-tenant**: garantido pela origem dos dados — os arrays `profissionais`/`modelos` já vêm filtrados por `estabelecimento_id` no servidor. Front não refiltra nem expande escopo.

**Audit/LGPD**: nenhuma audit table nova. Não há acesso a prontuário/dado clínico — apenas equipe do tenant (nome+foto+status), idêntico ao já exposto na aba Equipe.

## 6. UX e fluxo

**Card lateral (lista de papéis):** mantém layout atual. O texto "{{n}} profissio{nal|nais}" vira alvo clicável quando `n > 0`. Ao clicar, ancora o `AppPopover` no contador.

**Badge do detalhe (coluna direita):** o `.rd-badge` "N profissionais" do papel selecionado também vira alvo clicável quando `n > 0`, ancorando o mesmo `AppPopover`.

**Popover (conteúdo):**
- Cabeçalho curto opcional: nome do papel (ex.: "Admin — 3 profissionais").
- Lista vertical, cada item: `AppAvatar` (tamanho `sm`/`md`) + nome + chip de status.
- Para o item Dono no papel Admin: chip/selo **"Dono"** distinto (cor/ícone diferenciado, ex.: coroa/escudo) sinalizando acesso imutável — não é quem "usa o template", é o dono.
- Altura máxima equivalente a ~6 itens; acima disso, `overflow-y: auto` com scroll interno.
- Só-leitura: sem hover-action, sem cursor de link nos itens.

**Estados:**
- **Vazio/0**: contador não-clicável (R5); popover nunca abre. Não renderizar `AppEmptyState` dentro do popover porque o popover sequer abre com 0.
- **1 a 6 itens**: lista sem scroll.
- **>6 itens**: scroll interno, altura travada.
- **Loading**: não se aplica — dados já estão em memória (props), sem request ao abrir.
- **Erro**: não se aplica — sem request.

**Fechamento do popover**: clique fora e tecla `Esc` fecham (comportamento padrão do `AppPopover`).

**Acessibilidade/teclado**: o contador clicável é um elemento focável (`button`); abre por `Enter`/`Space`; `Esc` fecha; foco retorna ao gatilho ao fechar.

**Mobile-ready**: em telas estreitas o grid já colapsa para 1 coluna (`@media max-width:1100px`). O `AppPopover` deve se posicionar sem estourar a viewport (reposicionar/clampar dentro da tela).

## 7. Critérios de aceite (testáveis)

- **CA1** (Dono contado no Admin — caminho feliz): Dado um estabelecimento com 1 dono (`status === 'Dono'`, `modeloPermissaoId === null`) e 2 profissionais com `modeloPermissaoId` apontando para o modelo padrão "Admin", Quando a aba "Papéis e permissões" é renderizada, Então o card "Admin" exibe "3 profissionais".

- **CA2** (matching do modelo Admin): Dado um modelo com `ehPadrao === true && nome === 'Admin'` e outro modelo padrão com `nome === 'Recepção'`, Quando a contagem do Dono é calculada, Então o Dono é somado apenas ao card "Admin" e o card "Recepção" não inclui o Dono.

- **CA3** (borda — sem modelo Admin padrão): Dado um estabelecimento sem nenhum modelo que satisfaça `ehPadrao === true && nome === 'Admin'`, Quando a aba é renderizada, Então o Dono não é contado em nenhum card e não aparece em nenhum popover, e nenhum erro é lançado.

- **CA4** (borda — dono com vínculo atribuído, sem dupla contagem): Dado um profissional que aparece duas vezes no array (um registro com `status === 'Dono'` e outro registro de vínculo com `modeloPermissaoId` do "Admin", ambos a mesma pessoa), Quando o card "Admin" conta, Então essa pessoa é contada **uma única vez** e exibida com o selo "Dono" no popover.

- **CA5** (popover — conteúdo só-leitura): Dado o card "Admin" com 3 profissionais, Quando o gestor clica no contador, Então abre um popover listando 3 itens, cada um com `AppAvatar`, nome e status, sem nenhum link/botão de ação e sem navegação para perfil.

- **CA6** (popover — selo Dono): Dado o popover do papel "Admin" aberto, Quando há um item correspondente ao dono, Então esse item exibe o selo "Dono" visualmente distinto dos demais (que mostram Ativo/Inativo/Convidado).

- **CA7** (ambos os contadores clicáveis): Dado o papel "Admin" selecionado na coluna direita, Quando o gestor clica no badge "N profissionais" do detalhe, Então abre o mesmo popover com a mesma lista; e o contador do card lateral do mesmo papel também abre o popover ao ser clicado.

- **CA8** (contagem 0 não-clicável): Dado um papel customizado com 0 profissionais, Quando a aba é renderizada, Então o contador "0 profissionais" não é clicável (sem handler, sem cursor de clique) e nenhum popover abre ao clicar nele.

- **CA9** (scroll do popover): Dado um papel com 10 profissionais, Quando o popover abre, Então mostra ~6 itens com altura travada e `overflow-y: auto`, e os demais ficam acessíveis por scroll interno.

- **CA10** (fechamento e foco): Dado o popover aberto, Quando o gestor pressiona `Esc` ou clica fora, Então o popover fecha e o foco retorna ao contador que o abriu.

- **CA11** (multi-tenant): Dado que o array `profissionais` recebido por props já é filtrado por `estabelecimento_id` no servidor, Quando o popover lista os profissionais, Então só aparecem profissionais do tenant atual e o front não dispara nenhuma request nova nem refiltra escopo (mudança 100% frontend, sem novo endpoint).

- **CA12** (LGPD — minimização): Dado o popover aberto, Quando lista os profissionais, Então expõe somente nome, foto e status (mesmos campos da aba Equipe), sem qualquer dado clínico/PII adicional e sem mensagem contendo PII.

- **CA13** (escopo da entrega — sem backend): Dado o PR desta demanda, Quando o diff é revisado, Então não há alteração em `backend/`, `db/migrations/` nem em contratos/DTOs — apenas em `frontend/` (incluindo o novo `AppPopover` no design system).

- **CA14** (design system primeiro): Dado que não existia `AppPopover` reutilizável, Quando a feature é implementada, Então o popover é um componente em `frontend/src/components/ui/` (`AppPopover`), exportado no índice do design system e consumido por `AbaPapeis.vue` (não um popover ad-hoc inline).

## 8. Riscos e dependências

- **Risco — matching frágil por nome literal "Admin".** R1 casa por `nome === 'Admin'`. Se o seed/renomeação do modelo padrão alterar o rótulo, o Dono deixa de ser contado. Mitigação: documentar a premissa no PR; se o `ModeloPermissao` padrão tiver um identificador estável de "tipo de sistema" (ex.: `chave`/`slug`/código), preferir esse campo ao `nome` literal — **dev verifica** o tipo e usa o campo mais estável disponível, mantendo o fallback por `nome === 'Admin'`. Registrar no PR qual critério foi usado.
- **Risco — deduplicação da R3 sem id estável de profissional.** Se o DTO só tiver id de vínculo, a dedup cai em `nomeCompleto` normalizado (risco de homônimos). Dev confirma o campo e documenta.
- **Dependência leve** do design system: o novo `AppPopover` será reutilizável por futuras telas; manter API genérica (slot de gatilho + slot de conteúdo + posicionamento/clamp na viewport + fechar por Esc/clique-fora).
- **Área regressiva**: permissionamento (aba Papéis). A contagem dos papéis customizados não muda — garantir que o ajuste do Admin não altere o número exibido nos customizados (coberto por CA1/CA2 e pela ausência de mudança em `quantosUsam` para `!ehPadrao`).

## 9. Observações para execução

- **Não-negociável**: zero backend/migration/contrato (CA13). Toda a regra vive em apresentação no front.
- **Não-negociável**: criar `AppPopover` no design system, não popover inline (CA14). Reusar `AppAvatar` existente (props `nome`, `fotoUrl`, `tamanho`).
- **Liberdade técnica do dev**:
  - Implementação interna do `AppPopover` (Teleport/posicionamento) fica a critério do dev, desde que feche por Esc/clique-fora e não estoure a viewport.
  - Forma de extrair a contagem do Dono (computed dedicada vs. ajuste em `quantosUsam`) fica a critério do dev, desde que respeite R1–R3 e não altere a contagem dos customizados.
  - Escolha do campo de matching do modelo Admin (slug estável preferido sobre `nome === 'Admin'`) — decidir após inspecionar o tipo `ModeloPermissao`, registrar no PR.
- **Reuso obrigatório**: `AppAvatar` (`frontend/src/components/ui/AppAvatar.vue`). Para chips de status, reusar padrão de chip/badge já existente na aba Equipe se houver; caso contrário, manter consistência visual com `.rd-badge`/chips de status já usados em `AbaProfissionais.vue`.
- **Arquivo principal**: `frontend/src/components/equipe/AbaPapeis.vue` (contadores nas linhas ~75, ~105 e badge ~130).
- **Testes**: cobrir CA1–CA4 (contagem/dedup/borda) em teste unitário do componente; CA5–CA10 em teste de interação do popover. Há `ProfissionalDetalhesModal.test.ts` como referência de padrão de teste de componente de equipe.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar `AppPopover` à seção de componentes do design system (gatilho + conteúdo via slots, fecha por Esc/clique-fora, clamp na viewport, casos de uso: listas de detalhe só-leitura ancoradas a um gatilho). Atualização incremental, cirúrgica — só a entrada do novo componente. **Dev/BA atualiza no mesmo PR** (o dev pode atualizar ao consolidar a API final do `AppPopover`, já que é ele quem cria o componente; o QA valida nos CAs que a entrada foi adicionada).
- Demais docs (`ARQUITETURA.md`, `INFRA.md`, `COMANDOS.md`, `LGPD.md`): **nenhuma alteração** — sem novo padrão de arquitetura, sem infra, sem novo tipo de PII/endpoint/retenção (a feature renderiza dados já expostos pela aba Equipe).
