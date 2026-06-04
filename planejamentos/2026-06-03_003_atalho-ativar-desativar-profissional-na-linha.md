# Atalho de 1 clique para ativar/desativar profissional na linha da lista

**ID**: 2026-06-03_003
**Status**: Aprovado por usuário em 2026-06-03
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento (RBAC do reativar muda), equipe/vínculo

## 1. Contexto e motivação

Hoje, para ativar ou desativar um profissional na tela **Equipe e permissões → aba Profissionais**, o gestor precisa:
- selecionar o profissional pelo checkbox e usar a **bulk-bar** (`AbaProfissionais.vue`), OU
- abrir o **modal de detalhes** (`ProfissionalDetalhesModal.vue`).

São 2-3 cliques + leitura de bulk-bar para uma ação que, no dia a dia da recepção/gestão, é frequente e pontual ("desativar este aqui agora"). A dor é fricção operacional: a ação mais comum exige o caminho mais longo.

**Importante — o que JÁ existe e será reusado (não duplicar):**
- O fluxo de inativar/reativar já existe ponta-a-ponta: endpoints `/vinculo/{id}/inativar` e `/vinculo/{id}/reativar`, handlers `InativarVinculoCommandHandler` / `ReativarVinculoCommandHandler`, métodos de serviço `vinculoService.inativarVinculo` / `reativarVinculo`, e o orquestrador `onAcaoMassa` na `EquipeView.vue`.
- A regra "Dono nunca desativável" já está no domínio e no front (`podeSelecionar`).
- A regra "reativar exige vínculo já aceito" já está no domínio (`Reativar()` lança 422 quando `AceitoEm is null`).

A lacuna é **somente a entrada de UX**: um botão de ação por linha, ao lado do lápis, que alterna ícone (pause/play) conforme o status e dispara a ação em 1 clique (com confirmação).

**Ponto de atenção crítico de RBAC (divergência decisão × backend atual):**
A decisão de produto é "Dono + perfis com permissão `gerir_profissionais`". Hoje:
- `InativarVinculoCommandHandler` permite **Dono OU o próprio profissional** — NÃO permite um perfil com `gerir_profissionais` que não seja o Dono nem o próprio. Diverge da decisão.
- `ReativarVinculoCommandHandler` permite **apenas o Dono** — diverge da decisão.

Logo, esta demanda **inclui ajustar a autorização dos dois handlers** para que o botão de linha (que respeita a mesma autorização do endpoint) não prometa algo que o backend recusaria. Sem esse ajuste, um gestor com `gerir_profissionais` (não-Dono) veria o botão e tomaria 422.

## 2. Persona-alvo

**Gestor da clínica** — Dono ou perfil com permissão `gerir_profissionais` (ex.: recepção sênior, gerente). Acessa a aba Profissionais com frequência alta para administrar a equipe (afastamento, desligamento, retorno de profissional). Momento da jornada: gestão de equipe/vínculos, fora do atendimento.

## 3. Escopo

**Inclui**:
- Botão de ação por linha em `AbaProfissionais.vue`, ao lado do lápis (Editar), que alterna ícone conforme status:
  - status **Ativo** → ícone **pause** (`fa-solid fa-circle-pause`), ação = **Desativar**.
  - status **Inativo** → ícone **play** (`fa-solid fa-circle-play`), ação = **Reativar**.
- Confirmação (modal `AppConfirmDialog`) antes de aplicar.
- Estados de loading (botão da linha em processamento) e erro (toast).
- Reuso dos métodos `vinculoService.inativarVinculo` / `reativarVinculo` e do refetch/notify já existentes na `EquipeView.vue`.
- Tratamento do caso "vínculo nunca aceito" (status Inativo com `AceitoEm == null`): o botão play deve ficar **desabilitado** com tooltip orientando reenvio de convite, espelhando o 422 do domínio.
- Ajuste de autorização nos handlers `InativarVinculoCommandHandler` e `ReativarVinculoCommandHandler` para **Dono OU usuário com `gerir_profissionais` no estabelecimento do vínculo** (mantendo no inativar a permissão pré-existente do próprio profissional encerrar o próprio vínculo).

**Não inclui**:
- Cancelar, realocar ou notificar agendamentos futuros do profissional. Desativar **apenas corta acesso/vínculo**; agendamentos futuros permanecem intactos; reativar restaura o acesso sem mexer em agenda.
- Mudança no fluxo de **remover** (lixeira/bulk) — fora de escopo, permanece como está.
- Mudança na bulk-bar de seleção múltipla — permanece; o botão de linha é adicional, não substituto.
- Botão de linha para status **Dono** (nunca aparece) ou **Convidado** (não aparece nesta aba; vínculos pendentes ficam na aba Convites).

## 4. Regras de negócio

- **R1 — Quem pode ativar/desativar**: Dono do estabelecimento OU usuário com permissão `gerir_profissionais` naquele estabelecimento. No **inativar**, mantém-se também a regra pré-existente de o próprio profissional poder encerrar o próprio vínculo. Mora em: Domain (regra Dono-não-desativável) + Handler (`InativarVinculoCommandHandler`, `ReativarVinculoCommandHandler` — checagem de autorização). Validada em: back (422/autorização) + front (botão oculto se sem permissão).
- **R2 — Dono nunca desativável**: já garantido em `InativarVinculoCommandHandler` (defesa em profundidade) e no front (`podeSelecionar`). O botão de linha **não é renderizado** para linha de status `Dono`. Mora em: Domain + Handler + Front. Validada em: back + front.
- **R3 — Reativar exige vínculo já aceito**: vínculo Inativo com `AceitoEm == null` não pode ser reativado — o domínio lança "Este vínculo nunca foi aceito — reenvie o convite". O botão play da linha fica **desabilitado** nesse caso, com tooltip "Vínculo nunca aceito — reenvie o convite na aba Convites". Mora em: Domain (`Reativar()`) + Front (estado desabilitado). Validada em: back (422) + front (disabled + tooltip).
- **R4 — Confirmação obrigatória**: toda ação dispara `AppConfirmDialog` antes de chamar o endpoint, com texto específico ("Desativar {nome}?" / "Reativar {nome}?"). Mora em: Front. (Sem espelho no back — é UX; o back não confirma, apenas executa.)
- **R5 — Ícone reflete o status atual**: pause quando Ativo, play quando Inativo. A ação derivada do ícone é a inversa do status. Mora em: Front.
- **R6 — Multi-tenant**: a checagem de `gerir_profissionais` e a operação resolvem o estabelecimento a partir do **vínculo** (`vinculo.EstabelecimentoId`), nunca de um tenant arbitrário. Usuário sem claim/tenant válido para o estabelecimento do vínculo recebe erro genérico. Mora em: Handler + repositório falha-fechada. Validada em: back.

## 5. Modelo de dados

**Sem migration. Sem mudança de schema.** Reusa `VinculoProfissionalEstabelecimento` (campos `Status`, `AceitoEm`, `EstabelecimentoId`, `ProfissionalUsuarioId`) e o modelo de permissão existente (`gerir_profissionais`).

- Multi-tenant: `estabelecimento_id` já é coluna do vínculo; a autorização resolve o tenant a partir dele.
- Audit/LGPD: vínculo profissional×estabelecimento **não é dado de saúde** (não é paciente/prontuário) — não exige audit trail de acesso. Mensagens de erro permanecem genéricas, sem PII.

## 6. UX e fluxo

**Wireframe textual (linha da tabela em `AbaProfissionais.vue`, coluna de ações à direita):**

```
[ avatar ] Nome / especialidade   [Permissão]   [contato]   [StatusPill]    [ ✏ Editar ] [ ⏸/▶ ]
```

- O botão novo fica **à direita do lápis** já existente, na `<div class="pr-actions">`.
- Linha **Ativo** → botão com ícone `fa-circle-pause`, title "Desativar profissional".
- Linha **Inativo** (aceito) → botão com ícone `fa-circle-play`, title "Reativar profissional".
- Linha **Inativo** (`AceitoEm == null`) → botão `fa-circle-play` **desabilitado**, title "Vínculo nunca aceito — reenvie o convite na aba Convites".
- Linha **Dono** → botão **não renderizado** (só o lápis, conforme hoje).
- Clique no botão **para a propagação** (`@click.stop`) — não deve abrir o modal de detalhes.

**Componente do design system**: usar `AppConfirmDialog` (já existe, props `titulo`, `mensagem`, `confirmarRotulo`, `variante`, `executando`, eventos `confirmar`/`fechar`). Desativar usa `variante="danger"`; reativar usa `variante="primary"`.

**Padrão do botão de ação na linha**: ver `Docs/DESIGN.md` — a doc instrui usar as classes globais `.btn-icon` + variante (`.btn-icon-editar` etc.) de `main.css`, e **não criar variantes scoped**. Hoje `AbaProfissionais.vue` usa `.btn-icon-sm` scoped local. O dev deve manter consistência visual com o lápis vizinho. Como o pause/play é um par novo de variantes (toggle de status), há decisão de UX a registrar na doc — ver seção 10.

**Estados:**
- **loading**: enquanto a request da linha está em voo, o botão da linha fica `disabled` com spinner/estado de processamento, evitando duplo clique. Demais linhas seguem clicáveis.
- **erro**: toast `AppToast` com a mensagem do back (`e?.response?.data?.mensagem`) ou genérica.
- **sucesso**: refetch (`carregar()`) + toast de sucesso, reusando o padrão de `onAcaoMassa`.
- **vazio**: sem mudança — `AppEmptyState` já cobre lista vazia.

**Mobile-ready**: o grid responsivo já existe (`@media (max-width: 1100px)`); o botão adicional vive na mesma célula de ações, sem nova coluna.

## 7. Critérios de aceite (testáveis)

- **CA1** (caminho feliz — desativar): Dado um gestor (Dono ou com `gerir_profissionais`) na aba Profissionais, e um profissional com status **Ativo**, Quando clica no ícone **pause** da linha e confirma no diálogo, Então `vinculoService.inativarVinculo(vinculoId)` é chamado, a lista é recarregada, o status vira **Inativo** e um toast de sucesso é exibido.

- **CA2** (caminho feliz — reativar): Dado um gestor autorizado e um profissional **Inativo** que **já foi aceito** (`AceitoEm != null`), Quando clica no ícone **play** e confirma, Então `vinculoService.reativarVinculo(vinculoId)` é chamado, a lista recarrega, o status volta a **Ativo** e um toast de sucesso aparece.

- **CA3** (ícone alterna conforme status): Dado uma linha com status **Ativo**, Então o botão de ação exibe `fa-circle-pause` (title "Desativar profissional"); Dado uma linha com status **Inativo**, Então exibe `fa-circle-play` (title "Reativar profissional").

- **CA4** (confirmação antes): Dado o gestor clica no botão de ação da linha, Quando o `AppConfirmDialog` abre, Então **nenhuma** request é disparada até o usuário confirmar; ao cancelar, o diálogo fecha e nada muda.

- **CA5** (Dono não desativável — front): Dado uma linha com status **Dono**, Então o botão de ativar/desativar **não é renderizado** (somente o lápis aparece, como hoje).

- **CA6** (Dono não desativável — back, defesa em profundidade): Dado uma chamada direta a `/vinculo/{id}/inativar` cujo `ProfissionalUsuarioId == DonoUsuarioId`, Quando o handler executa, Então retorna 422 "O dono do estabelecimento não pode ser desativado." e nada é alterado.

- **CA7** (vínculo nunca aceito — front): Dado uma linha **Inativo** com `AceitoEm == null`, Então o botão **play** fica `disabled` com tooltip "Vínculo nunca aceito — reenvie o convite na aba Convites", e o clique não dispara request.

- **CA8** (vínculo nunca aceito — back): Dado uma chamada a `/vinculo/{id}/reativar` para vínculo Inativo com `AceitoEm == null`, Quando o handler executa, Então retorna 422 "Este vínculo nunca foi aceito — reenvie o convite em vez de reativar." e o status permanece Inativo.

- **CA9** (RBAC — ajuste do inativar): Dado um usuário **não-Dono** com permissão `gerir_profissionais` no estabelecimento do vínculo, Quando chama `/vinculo/{id}/inativar` de um profissional que não é ele mesmo, Então a operação **é permitida** (204) — comportamento que hoje **falha** com 422 e deve passar a funcionar após o ajuste.

- **CA10** (RBAC — ajuste do reativar): Dado um usuário **não-Dono** com `gerir_profissionais` no estabelecimento do vínculo, Quando chama `/vinculo/{id}/reativar` de um vínculo Inativo já aceito, Então a operação **é permitida** (204) — comportamento que hoje **falha** com "Apenas o dono do estabelecimento pode reativar" e deve passar a funcionar após o ajuste.

- **CA11** (RBAC — negar sem permissão): Dado um usuário **sem** `gerir_profissionais` e que **não** é o Dono nem o próprio profissional do vínculo, Quando chama inativar/reativar, Então recebe 422 com mensagem genérica de autorização; e no front o botão de ação da linha fica **oculto** para esse usuário.

- **CA12** (multi-tenant): Dado um usuário do estabelecimento **B**, Quando tenta inativar/reativar um vínculo cujo `EstabelecimentoId` é do estabelecimento **A**, Então recebe erro genérico (não-encontrado/autorização), nada é alterado e nenhum dado do tenant A é revelado na mensagem.

- **CA13** (loading): Dado o gestor confirma a ação de uma linha, Quando a request está em voo, Então o botão daquela linha fica `disabled` (estado de processamento) impedindo duplo disparo; ao concluir, volta ao normal.

- **CA14** (erro): Dado o back retorna 422, Quando a request falha, Então um toast com a mensagem do back (ou genérica) é exibido, sem PII, e a lista reflete o estado real após o refetch.

## 8. Riscos e dependências

- **Risco RBAC (regressão de permissionamento)**: alterar a autorização do inativar/reativar é mudança em área regressiva. Atenção a não **afrouxar** além do pretendido — o critério é estritamente "Dono OU `gerir_profissionais` no estabelecimento do vínculo" (+ próprio profissional no inativar). Testes existentes a vigiar: `InativarVinculoCommandHandlerTests`, `VinculoProfissionalEstabelecimentoReativarTests`, `IdempotencyAndReativarVinculoIntegrationTests`.
- **Risco de autorização no handler vs. atributo de controller**: os endpoints `/vinculo/{id}/inativar` e `/vinculo/{id}/reativar` recebem **apenas `vinculoId`** na rota — não há `estabelecimentoId` para o atributo `RequiresPermissaoExtra` consumir. Portanto a checagem de `gerir_profissionais` deve ser feita **dentro do handler** (resolvendo o estabelecimento a partir do vínculo) ou via serviço de autorização equivalente, não pelo atributo de rota usado nos endpoints de convite. **Decisão de implementação fica a cargo do dev/DB**, mas o resultado deve satisfazer os CAs de RBAC e multi-tenant.
- **Dependência**: existência da permissão `gerir_profissionais` no modelo (já existe — `PermissoesExtras.GerirProfissionais` no back, `podeExtra("gerir_profissionais")` no front via `permissoesStore`).
- **Não-risco confirmado**: agenda futura não é tocada (fora de escopo por decisão).

## 9. Observações para execução

- **Reuso obrigatório (não duplicar)** — arquivos a estender:
  - Front (botão de linha + confirm + loading): `frontend/src/components/equipe/AbaProfissionais.vue` (adicionar botão na `.pr-actions`, emitir novo evento de ação por linha — ex.: `acao-linha` com `{ acao, vinculoId }`).
  - Front (orquestração): `frontend/src/views/equipe/EquipeView.vue` (novo handler reusando `vinculoService.inativarVinculo`/`reativarVinculo`, `AppConfirmDialog`, `carregar()` e `notificar()` já existentes; pode reaproveitar a lógica de `onAcaoMassa` para single-item).
  - Front (serviço): `frontend/src/services/vinculoService.ts` — **sem novos métodos**, reusar `inativarVinculo`/`reativarVinculo`.
  - Front (permissão): `frontend/src/stores/permissoesStore.ts` — `podeExtra("gerir_profissionais")` para esconder o botão de quem não pode.
  - Front (confirm): `frontend/src/components/ui/AppConfirmDialog.vue`.
  - Back (autorização): `backend/src/Services/Imedto.Backend.Application/Vinculos/Commands/InativarVinculoCommandHandler.cs` e `ReativarVinculoCommandHandler.cs` — ajustar a regra de quem pode (incluir `gerir_profissionais`). Reusar o mecanismo de checagem de permissão extra já existente no domínio/infra (mesma fonte que alimenta `RequiresPermissaoExtra`/`PermissoesExtras.GerirProfissionais`).
  - Back (domínio/contratos): `VinculoProfissionalEstabelecimento.cs` permanece como está (regras já cobrem Dono e nunca-aceito); endpoints em `VinculoController.cs` permanecem com a mesma rota.
- **Não-negociável**: o botão de linha respeita exatamente a mesma autorização do endpoint (back é a fonte da verdade; front é UX). Mensagens genéricas, sem PII. Multi-tenant resolvido pelo vínculo.
- **Liberdade técnica do dev/DB**: como implementar a checagem de `gerir_profissionais` no handler (serviço de autorização, consulta ao modelo de permissão do vínculo, etc.) — desde que satisfaça CA9–CA12.
- **Confirmar com QA** se o botão da linha deve reusar as classes globais `.btn-icon`/`.btn-icon-editar` (padrão documentado em DESIGN.md) ou o `.btn-icon-sm` scoped local já presente em `AbaProfissionais.vue`. Preferência: seguir a doc (global), mas manter coerência visual com o lápis vizinho — se o dev migrar o lápis para a classe global, é uma carona aceitável; se mantiver o `.btn-icon-sm` local, documentar a variante toggle localmente.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar nota sobre o **botão de ação por linha que alterna ícone conforme estado (toggle pause/play)** na seção de "Botões de ação em tabelas". O par pause/play (`fa-circle-pause` quando Ativo → ação Desativar; `fa-circle-play` quando Inativo → ação Reativar) é um padrão de UX novo (ação derivada do status, ícone espelha estado). Registrar: (a) o ícone reflete o estado atual e a ação é a inversa; (b) confirmação via `AppConfirmDialog` antes de aplicar; (c) estado `disabled` + tooltip quando a ação é inválida (ex.: reativar vínculo nunca aceito). Mudança incremental e cirúrgica — só acrescentar o item ao bloco existente de botões de ação em tabela, sem reescrever a seção.
- **`Docs/LGPD.md`** — nenhum ajuste: vínculo profissional×estabelecimento não é dado de saúde; sem novo PII/audit.
- **`Docs/ARQUITETURA.md` / `Docs/INFRA.md` / `Docs/COMANDOS.md`** — nenhum ajuste: sem novo bounded context, sem migration, sem recurso de infra, sem comando novo.
