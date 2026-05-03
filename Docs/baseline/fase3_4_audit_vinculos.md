# Auditoria LGPD — Módulo Vínculos + SolicitacaoVinculo (Fase 3.4)

**Data**: 2026-05-02
**Escopo**: Domain/Vinculos, Application/Vinculos (8 commands + 4 queries + 6 event handlers), Contracts/Vinculos (DTOs), Infrastructure (2 Repos EF + 2 Repos Dapper), Controllers (`VinculoController`, `SolicitacaoVinculoController`, `EstabelecimentoProfissionaisController`) + reflexo no front (`vinculoService.ts`, `solicitacaoVinculoService.ts`, 4 views).

> Vínculo é a primitiva do controle de acesso multi-tenant: ele determina quem pode atuar em qual estabelecimento. Vazamento aqui é **vazamento horizontal cross-tenant** — uma Clínica A descobrir profissionais/emails da Clínica B, ou um profissional ver os colegas de outra clínica. Régua das Fases 3.1 / 3.2 / 3.3 vale aqui inteira.

## Sumário

| Categoria | Achados |
|---|---|
| Campos PII vazando em DTOs | **5** (2 críticos LGPD nos 2 DTOs de listagem) |
| Queries Dapper sem filtro de tenant | **0** (queries filtram, mas a **validação de autorização** delas tem lacuna — ver §3.2) |
| Mensagens de erro com PII / vetor de enumeração | **3** (1 alto: enumeração de e-mail no `/convidar`; 2 médios em handlers de inativar/aprovar) |
| Logs com PII | **0** crítico (handlers logam só ids — bom) — porém **1 médio**: `actionLink` do Supabase exposto no body em dev |
| Gaps de autorização / IDOR | **6** (3 críticos: Recepcionista pode chamar `/convidar`/`/aprovar` via permissão extra; `AceitarConvite` e `InativarVinculo` aceitam vínculos cross-tenant; `AlterarModeloPermissao` sem endpoint exposto mas com hole arquitetural) |
| `KeyNotFoundException` → 500 | **2** (`VinculoRepository.ObterPorId`, `SolicitacaoVinculoRepository.ObterPorId`) |
| Defense-in-depth: `ObterPorId(id, tenantId)` ausente | **2 repos** (vinculo + solicitação) — mesmo padrão da Fase 3.2 |
| Endpoints sensíveis sem rate limit | **3** (`/convidar`, `/solicitar`, `/aceitar`) |
| Audit trail (LGPD Art. 37) | **6 lacunas** (convidar, aceitar, inativar, aprovar, recusar, alterarModeloPermissao) |
| Bugs latentes | **4** (`AceitarConvite` cria conta órfã sem `IUsuarioRepo.ObterPorIdOuNulo`; corrida no upsert do `Convidar`; `Aprovar` não valida `EstabelecimentoId` do tenant ativo coincide com o da solicitação antes do erro genérico; `RequiresPapel` ausente nos endpoints) |

---

## 1. DTOs — minimização (LGPD Art. 6º III)

### 1.1 `SolicitacaoVinculoDto` (Contracts/Vinculos/Queries/Results/SolicitacaoVinculoDto.cs:8-21)

DTO compartilhado entre **"minhas solicitações"** (lista do profissional) e **"recebidas"** (lista do dono). Esse "shape comum" é a fonte do vazamento.

| Campo | Front consome? | Severidade | Ação |
|---|---|---|---|
| `Id` | sim | — | manter |
| `ProfissionalUsuarioId` (Guid `sub` do Supabase) | sim em "recebidas" como fallback de display (`SolicitacoesRecebidasView.vue:136`) | **alto** — Guid de auth nunca deveria sair para qualquer cliente. Não é exibido pelo profissional ("minhas") e em "recebidas" só serve de fallback de label. | Remover do DTO. Em "recebidas" troque o fallback por `"Profissional"` literal. Front nunca precisa do `sub` do colega. |
| **`ProfissionalEmail`** (`u.email`) | **NÃO consumido** em lugar nenhum (`solicitacaoVinculoService.ts` nem declara o campo na interface; views não referenciam) | **CRÍTICO LGPD** — e-mail do profissional vai no payload da lista do dono e da própria lista do profissional sem que ninguém o use. Em "recebidas" expõe e-mail de um profissional que **ainda não tem vínculo** com a clínica. Em "minhas" o profissional vê o próprio e-mail (irrelevante mas redundante). | **Remover do DTO e do SQL** (`SolicitacaoVinculoQueryRepository.cs:27,57`). Confirmado pelo bug da Fase 1. |
| `ProfissionalNome` (`u.nome_completo`) | sim em "recebidas" (`SolicitacoesRecebidasView.vue:136,183`) | médio | Manter na resposta de "recebidas". **Remover** na resposta de "minhas" (o profissional sabe o próprio nome). Solução: split em dois DTOs (`SolicitacaoVinculoMinhaDto`, `SolicitacaoVinculoRecebidaDto`) — alinhado com o padrão Fase 3.3. |
| `EstabelecimentoId` | sim ("minhas" como fallback `#42`) | — | manter |
| **`EstabelecimentoNomeFantasia`** | front declara `estabelecimentoNome?` (sem o sufixo "NomeFantasia" → mismatch silencioso) — `SolicitarVinculoView.vue:73,157` lê `s.estabelecimentoNome` que **sempre será undefined** | **CRÍTICO LGPD + bug** — campo existe no payload (e em "recebidas" expõe o nome do PRÓPRIO estabelecimento do dono — tudo bem ali) mas em "minhas" é o nome de uma clínica que recusou/cancelou e ainda assim sai pelo wire. Pior: o front não usa porque o nome da prop bateu errado. Resultado prático: PII passa pelo wire para nada. | **Remover** o campo do SQL de "minhas" (`SolicitacaoVinculoQueryRepository.cs:30,60`). Em "recebidas" também é redundante (o dono já sabe o nome do próprio estabelecimento), pode ir embora. Renomear no DTO para `EstabelecimentoNome` se mantido. |
| `Status` | sim | — | manter |
| `Mensagem` | sim ("recebidas") | — | manter (é texto livre do solicitante) |
| `CriadaEm`, `RespondidaEm` | sim | — | manter |
| `MotivoRecusa` | sim ("minhas": exibido após recusa) | — | manter (o titular tem direito ao motivo) |

### 1.2 `ProfissionalVinculadoDto` (Contracts/Vinculos/Queries/Results/ProfissionalVinculadoDto.cs:3-16)

DTO consumido **apenas pela lista do dono** (`ProfissionaisView.vue`). Já é uma exibição autorizada (dono do estabelecimento) — minimização menos crítica, mas:

| Campo | Front consome? | Severidade | Ação |
|---|---|---|---|
| `VinculoId` | sim | — | manter |
| `UsuarioId` (Guid Supabase) | sim, mas só para `ehVinculoProprio()` (compara com `auth.usuario?.id`) | **médio** — Guid de auth dos colegas no payload. | Trocar pelo id local do `usuarios.id` (que já é o mesmo Guid hoje, mas conceitualmente deveria ser o id local do registro em `public.usuarios`). Alternativa: gerar `bool ehVoce` no backend (mais simples, evita expor o Guid alheio). |
| `Email` | sim (coluna na tabela) | médio LGPD — colegas dentro do mesmo estabelecimento veem o e-mail uns dos outros. | **Aceitável** se considerar que dono autorizou (Art. 7º V). Em refactor futuro: ofuscar (`j***@gmail.com`) na lista, retornar cru só ao clicar em "ver detalhes". Não bloquear nesta fase. |
| `NomeCompleto` | sim | — | manter |
| `Status` | sim | — | manter |
| `ModeloPermissaoId`, `ModeloPermissaoNome` | sim | — | manter |
| `ConvidadoEm`, `AceitoEm` | sim ("ConvidadoEm" usado pelo back; front exibe "Recebido em" no `MeusConvitesView`) | — | manter |
| `Especialidade`, `Conselho` | sim (coluna especialidade) | — | manter (dado profissional, não pessoal sensível) |

### 1.3 `ConviteDto` (Contracts/Vinculos/Queries/Results/ConviteDto.cs:3-17)

Endpoint `/api/vinculo/convites/me` — o profissional vê SEUS convites pendentes. Sem cross-tenant aqui, mas:

| Campo | Front consome? | Severidade | Ação |
|---|---|---|---|
| `VinculoId` | sim | — | manter |
| `EstabelecimentoId` | sim | — | manter |
| `NomeFantasiaEstabelecimento` | sim | — | manter (o convidado precisa saber a quem está aceitando) |
| **`ConvidadoPorEmail`** | **NÃO consumido** (`MeusConvitesView.vue:81-85` usa só `convidadoPorNome`) | **alto LGPD** — e-mail do dono no payload sem uso. | **Remover do DTO + SQL** (`VinculoQueryRepository.cs:115`). |
| `ConvidadoPorNome` | sim ("Convite enviado por X") | — | manter |
| `ConvidadoEm` | sim | — | manter |
| `NomeConvidado`, `TelefoneConvidado`, `EspecialidadeConvidada` | **NÃO consumidos** (front só exibe estabelecimento + quem convidou + data) | **médio** — telefone do convidado (= o próprio usuário logado, que ele já conhece) não tem propósito no payload. Para o caso "convidador deixou o telefone errado" é melhor mostrar só na tela "Aceitar e completar onboarding" (passo seguinte). | Remover desta listagem. Mover para o response do `/api/vinculo/{id}/aceitar` (preview pré-aceite). |
| `ModeloPermissaoId` | **NÃO consumido** | baixo | remover ou trocar por `ModeloPermissaoNome` se algum dia for exibir o tipo de acesso pré-atribuído. |

---

## 2. Mensagens de erro — vetor de enumeração / vazamento de PII

### 2.1 `ConvidarProfissional` no controller (VinculoController.cs:45-47)

```csharp
var convite = await _authService.CriarConviteAsync(request.Email);
var profUserId = Guid.Parse(convite.User.Id);
```

`CriarConviteAsync` chama o Supabase. Se a conta **já existe** com esse e-mail no Supabase, o handler avança e usa o `Id` retornado. Em sequência (linha 75-76):

```csharp
if (existente is { Status: VinculoStatus.Ativo or VinculoStatus.Convidado })
    throw new BusinessException("Este profissional já tem um vínculo ativo ou convite pendente para este estabelecimento.");
```

| Severidade | **alto LGPD — vetor de enumeração de e-mail** |
|---|---|
| Vetor | Atacante autenticado como dono de uma clínica chama `/convidar` com 1000 e-mails. Mensagem 422 distingue: (a) "Este profissional já tem um vínculo ativo ou convite pendente para este estabelecimento" → **e-mail tem conta no Supabase E está vinculado a este estab**; (b) sucesso 201 com `convite.JaExistia=true` no body em dev → **e-mail tem conta no Supabase**; (c) sucesso 201 com `JaExistia=false` → **conta foi criada agora**. Em prod o `JaExistia` não vaza, mas a diferença de mensagem ainda permite cross-tenant fishing: "esse profissional X@Y.com já está em outra clínica?". |
| Recomendação | (1) Mensagem genérica única: `"Não foi possível processar este convite."`; (2) Não retornar `jaExistia` nem em dev — usar log. (3) Rate-limit explícito por `(usuarioId, IP)` em `/convidar` (max 10/min, alinhado a `auth-sensitive`). |
| Arquivos | `VinculoController.cs:60-68`, `ConvidarProfissionalCommandHandler.cs:75-76` |

### 2.2 `InativarVinculo` (InativarVinculoCommandHandler.cs:24-32)

```csharp
var vinculo = await _vinculoRepo.ObterPorId(command.VinculoId);   // 404 se não existe
var estab = await _estabelecimentoRepo.ObterPorId(vinculo.EstabelecimentoId);
// ...
if (!ehDono && !ehProprioProfissional)
    throw new BusinessException("Apenas o dono do estabelecimento ou o próprio profissional podem inativar este vínculo.");
```

| Severidade | **médio LGPD — IDOR + enumeração de IDs** |
|---|---|
| Vetor | Usuário X tenta `POST /api/vinculo/{id}/inativar` para id 1, 2, 3, ... — recebe 422 ("não autorizado") quando o vínculo existe e 404 quando não existe. Permite enumerar quantos vínculos existem no sistema inteiro. |
| Recomendação | Devolver **422 genérico igual** em todos os casos: "Não foi possível inativar este vínculo." (sem distinguir not-found vs unauthorized). Padrão das Fases 3.1/3.2 (`ObterPorIdOuNulo` + same-error pattern). |
| Arquivos | `InativarVinculoCommandHandler.cs:24-32`, `VinculoRepository.cs:18-20` |

### 2.3 `AprovarSolicitacaoVinculo` / `RecusarSolicitacaoVinculo` — `EstabelecimentoId` mismatch (handlers linha 36-37 / 29-30)

```csharp
if (solicitacao.EstabelecimentoId != command.EstabelecimentoId)
    throw new BusinessException("Solicitação não pertence a este estabelecimento.");
```

| Severidade | **médio LGPD** |
|---|---|
| Vetor | Dono do estab A com `X-Estabelecimento-Id: 1` ativo chama `POST /solicitacoes-vinculo/42/aprovar`, onde 42 é uma solicitação destinada ao estab B (do qual ele NÃO é dono). Recebe a mensagem "Solicitação não pertence a este estabelecimento" — confirma que id 42 existe e pertence a outro estab. Permite enumerar IDs e correlacionar com outros estabs. |
| Recomendação | Mensagem genérica idêntica a "não encontrada" + 422 (não 404). Padrão: "Solicitação não encontrada ou indisponível." |
| Arquivos | `AprovarSolicitacaoVinculoCommandHandler.cs:36-37`, `RecusarSolicitacaoVinculoCommandHandler.cs:29-30` |

### 2.4 `CancelarSolicitacaoVinculo` (handler linha 25-26) — mesma classe de bug

```csharp
if (solicitacao.ProfissionalUsuarioId != command.SolicitanteUsuarioId)
    throw new BusinessException("Apenas o profissional que criou a solicitação pode cancelá-la.");
```

| Severidade | médio (idêntico à 2.2) |
|---|---|
| Recomendação | Devolver 422 "Solicitação não encontrada ou indisponível." sem distinguir not-found de IDOR. |

### 2.5 `AceitarConvite` (AceitarConviteCommandHandler.cs:24-25) — mesma classe

```csharp
if (vinculo.ProfissionalUsuarioId != command.UsuarioSolicitanteId)
    throw new BusinessException("Apenas o profissional convidado pode aceitar este convite.");
```

| Severidade | médio (idêntico) |
|---|---|

---

## 3. Repositórios — defense-in-depth

### 3.1 `IVinculoRepository.ObterPorId(long)` não exige `tenantId` / `usuarioId`

Arquivo: `IVinculoRepository.cs:5`, `VinculoRepository.cs:15-21`.

```csharp
public async Task<VinculoProfissionalEstabelecimento> ObterPorId(long id)
{
    var v = await _context.Vinculos.FindAsync(id);
    if (v is null)
        throw new KeyNotFoundException($"Vínculo {id} não encontrado.");  // → 500
    return v;
}
```

| Severidade | **crítico LGPD + defense-in-depth quebrada** |
|---|---|
| Problemas | (a) Não há gate por tenant — se algum handler novo chamar `ObterPorId(idQualquer)` sem checar `EstabelecimentoId` depois, tem IDOR. (b) `KeyNotFoundException` → 500 (mascara como "instabilidade" em vez de 422 esperado). |
| Recomendação | Padronizar pelo padrão das Fases 3.1/3.2: trocar por `ObterPorIdOuNulo(long id, long? estabelecimentoId, Guid? usuarioId)` que retorna `null` se o vínculo não existe OU se o requester não tem acesso. Handler decide: se `null` → `BusinessException` genérico. Para `Aceitar`, o "tenant" é o profissional (`Guid usuarioId`); para `Inativar`, o gate é "é dono do estab do vínculo OU é o próprio profissional". |
| Arquivos | `VinculoRepository.cs:15-21`, `IVinculoRepository.cs:5` |

### 3.2 `ISolicitacaoVinculoRepository.ObterPorId(long)` — idêntico

Arquivo: `ISolicitacaoVinculoRepository.cs:5`, `SolicitacaoVinculoRepository.cs:15-21`. Mesma classe de bug, mesma recomendação. Note que já existe `ObterPorIdOuNulo(long id)` no contrato (linha 6), mas SEM filtro de tenant — corrigir.

### 3.3 Queries Dapper de listagem — filtros de tenant OK, mas autorização do **caller** depende do handler

`VinculoQueryRepository.ListarProfissionaisDoEstabelecimento` (`:23`) e `SolicitacaoVinculoQueryRepository.ListarPorEstabelecimento` (`:51`) ambas filtram por `@EstabelecimentoId`. **OK** — sem vazamento horizontal direto na query.

Mas a autorização "este `EstabelecimentoId` é o do tenant ativo do caller?" depende inteiramente do handler chamar `EstabelecimentoRepo.ObterPorId(id).DonoUsuarioId == solicitante`. **OK nos handlers atuais** (`ListarProfissionaisEstabelecimentoQueryHandlers.cs:28`, `ListarSolicitacoesVinculoRecebidasQueryHandlers.cs:31`) — porém é uma armadilha de futuro: novo endpoint que esquecer essa checagem vaza a lista inteira.

| Recomendação | Adicionar parâmetro `Guid solicitanteId` nas queries Dapper e validar via `JOIN ... WHERE e.dono_usuario_id = @SolicitanteId`. Defense-in-depth — mesmo se o handler esquecer, a query devolve vazio. |

### 3.4 `ListarPorProfissional` (Dapper, `:22`) — OK

Filtra por `s.profissional_usuario_id = @ProfissionalUsuarioId` (passado do `sub` do JWT no controller). Handler `ListarMinhasSolicitacoesVinculoQueryHandlers.cs:18` passa direto. **Sem cross-tenant possível** — o `sub` é a identidade autenticada. **OK**.

### 3.5 `ListarConvitesPendentes` (Dapper, `:109`) — OK

Filtra por `v.profissional_usuario_id = @UsuarioId`. **OK**.

### 3.6 `TemVinculoAtivo`, `ObterTipoAcessoVinculoAtivo` — OK

Filtros explícitos por `usuarioId + estabelecimentoId`. Usados pelo `TenantAccessResolver` — não há vetor de cross-tenant.

---

## 4. Endpoints — autorização

### 4.1 `VinculoController` — sem `[RequiresPapel]`, usando `[RequiresPermissaoExtra(GerirProfissionais)]`

| Endpoint | Atual | Análise |
|---|---|---|
| `POST /estabelecimento/{id}/profissionais/convidar` | `[RequiresPermissaoExtra(GerirProfissionais, "estabelecimentoId")]` + handler valida `DonoUsuarioId == ConvidadoPorUsuarioId` | **CONTRADIÇÃO crítica**. O atributo deixa passar Recepcionista cujo modelo de permissão tenha `GerirProfissionais`. O handler então recusa com "Apenas o dono...". Resultado: 422 "Apenas o dono..." em vez de 403. **E pior**: a Fase 1 estabeleceu que `[RequiresPermissaoExtra(GerirProfissionais)]` significa "pode gerir" — então se o dono atribuir essa permissão a um Recepcionista, o handler ainda bloqueia (inconsistência de modelo). Decisão de produto necessária: ou (A) só dono pode convidar (alinhar atributo: `[RequiresPapel(TenantPapel.Dono)]` + remover check duplicado do handler) ou (B) qualquer um com `GerirProfissionais` pode convidar (remover check do handler). **Não pode ficar como está.** |
| `POST /vinculo/{id}/aceitar` | `[Authorize]` apenas | Correto — qualquer usuário autenticado pode aceitar SEU convite (handler valida ownership). Mas atualmente o handler retorna `KeyNotFoundException → 500` se o id não existe (ver §3.1). |
| `POST /vinculo/{id}/inativar` | `[Authorize]` apenas | Igual: handler valida (dono OU próprio profissional). Mesmo problema do 500 em not-found. |

### 4.2 `SolicitacaoVinculoController`

| Endpoint | Atual | Análise |
|---|---|---|
| `POST /solicitacoes-vinculo` (solicitar) | `[Authorize]` | OK — não exige tenant porque o solicitante ainda não é tenant deste estab. **Falta rate limit** (vetor: profissional malicioso enche todas as clínicas de spam). |
| `GET /solicitacoes-vinculo/minhas` | `[Authorize]` | OK — escopo do `sub`. |
| `GET /solicitacoes-vinculo/recebidas` | `[RequiresEstabelecimento]` + handler valida dono | **Falta `[RequiresPapel(TenantPapel.Dono)]`**. Hoje, Recepcionista do estab A consegue chamar (passa `RequiresEstabelecimento`), e só o handler bloqueia com 422. Deveria 403 antes do handler. |
| `POST /{id}/aprovar` | `[RequiresEstabelecimento]` + handler valida dono | Igual: falta `[RequiresPapel(TenantPapel.Dono)]`. |
| `POST /{id}/recusar` | igual | igual |
| `POST /{id}/cancelar` | `[Authorize]` | OK — handler valida que é o próprio profissional. Mesmo problema 422-vs-404 / KeyNotFoundException. |

### 4.3 `EstabelecimentoProfissionaisController`

| Endpoint | Atual | Análise |
|---|---|---|
| `GET /estabelecimento/{id}/profissionais` | `[Authorize]` apenas (handler valida dono) | **médio** — sem `[RequiresPermissaoExtra(GerirProfissionais, "estabelecimentoId")]` ou `[RequiresPapel(Dono)]`, qualquer usuário autenticado consegue chegar no handler e descobrir a existência do estabelecimento via mensagem "Apenas o dono...". Ver §2.x: 422 enumeração. Adicionar gate. |
| `GET /vinculo/convites/me` | `[Authorize]` | OK — escopo do `sub`. |

### 4.4 Resumo das ações de autorização

1. Definir produto: **convidar profissional** = papel Dono apenas? Ou qualquer um com `PermissoesExtras.GerirProfissionais`?
2. Após (1), aplicar atributo correto e **remover** check duplicado do handler.
3. Adicionar `[RequiresPapel(TenantPapel.Dono)]` em todos os endpoints de "recebidas" / "aprovar" / "recusar".
4. Adicionar gate em `GET /estabelecimento/{id}/profissionais` (dono OU permissão extra).

---

## 5. Event handlers — logs e PII

### 5.1 `ProfissionalConvidadoEventHandler.cs:18-21` e `VinculoAceitoEventHandler.cs:18-21`

```csharp
_logger.LogInformation(
    "Convite criado: Vinculo={VinculoId}, Profissional={ProfissionalUsuarioId}, Estabelecimento={EstabelecimentoId}",
    domainEvent.VinculoId, domainEvent.ProfissionalUsuarioId, domainEvent.EstabelecimentoId);
```

| Severidade | **OK (baixo)** |
|---|---|
| Análise | Loga apenas Guids/longs. Nenhum e-mail / nome. **Bom padrão** — manter. Considerar elevar para um audit table dedicada (ver §6) ou pelo menos adicionar `EventId` + `correlationId` para rastreio LGPD. |

### 5.2 `NotificarConviteAoConvidarProfissionalHandler.cs:67-70`

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Falha ao enviar email de convite para vínculo {VinculoId}.", domainEvent.VinculoId);
}
```

| Severidade | OK | Loga só `VinculoId`. **Bom**. Atenção: o `ex` da pilha pode incluir o e-mail dependendo do `IEmailService` — depende de como ele formata `SmtpException`. **Verificar** uma vez no provider real (não no escopo desta auditoria, mas anotar). |

### 5.3 `NotificarSolicitacaoCriadaHandler.cs:69-72` — idêntico

### 5.4 `NotificarSolicitacaoRespondidaHandler` — sem log, OK

### 5.5 `AoAprovarSolicitacaoCriarVinculoHandler` — sem log, OK

### 5.6 Texto das notificações — OK

Mensagens genéricas explícitas: `"Você recebeu um convite para vincular-se a um estabelecimento. Acesse 'Meus convites'..."` — não cita nome do estab nem do convidador. **Excelente** padrão LGPD (notificação push pode aparecer em lock-screen). Manter.

---

## 6. Audit trail (LGPD Art. 37)

Nenhum dos 8 commands grava em audit table:
- Convidar (quem convidou quem para qual estabelecimento, quando) — **alto** (registro de operação sobre dado do titular).
- Aceitar (quando o titular consentiu o vínculo) — **crítico** (consentimento bilateral é a base legal do acesso aos prontuários).
- Inativar (quem encerrou, quando) — **alto** (revogação de consentimento).
- Aprovar / Recusar / Cancelar solicitação — **alto**.
- AlterarModeloPermissao — **crítico** (mudança de escopo de acesso a dados sensíveis).

Os event handlers `ProfissionalConvidadoEventHandler` e `VinculoAceitoEventHandler` logam em `ILogger`, mas log estruturado **não é equivalente a audit table** (rotação, retenção, integridade, query). Padrão da Fase 3.2 (Prontuário) deve ser replicado: tabela `vinculo_audit_log` com `(quando, quem, acao, vinculo_id, estabelecimento_id, payload_jsonb)`.

| Recomendação | Criar `IVinculoAuditLogRepository` + tabela `vinculo_audit_log` (mesmo padrão da audit de Prontuários da Fase 3.2). Gravar dentro da MESMA transação dos commands (defense-in-depth: se a transação rollback, o audit também). |

---

## 7. Bugs latentes

### 7.1 `AceitarConviteCommandHandler` — não cria registro local de `Usuario`

`AceitarConviteCommandHandler.cs:19-34` apenas chama `Aceitar()` no aggregate. **Não verifica** se o registro local em `public.usuarios` existe.

| Severidade | **alto** |
|---|---|
| Cenário | Profissional convidado **nunca fez login** antes (conta criada via `CriarConviteAsync` no Supabase, link de magic link aberto direto via deep-link → `/aceitar`). Hoje, o `ConvidarProfissional` já garante `usuarioRepo.Salvar(novo)` (linha 62-67), então o registro existe. **Mas** se algum dia o fluxo de re-convite ou o aceite via solicitação aprovada cair primeiro (o aprovar cria via `CriarAtivoPorSolicitacao` sem essa garantia explícita — `AoAprovarSolicitacaoCriarVinculoHandler.cs:64-66` tem o check, OK), e o profissional aceitar antes de ter passado pelo onboarding... `AceitarConvite` confia que existe. Investigar caminho específico. |
| Recomendação | Adicionar checagem `usuarioRepo.ObterPorIdOuNulo(vinculo.ProfissionalUsuarioId)` em `AceitarConviteCommandHandler` para fail-fast com `BusinessException` em vez de FK violation no SaveChanges. |

### 7.2 Race condition no upsert de `Convidar`

`ConvidarProfissionalCommandHandler.cs:72-103` faz: (a) lê vínculo existente → (b) decide criar ou reativar → (c) salva. Sem lock pessimista nem unique constraint **no estado Convidado/Ativo** (a unique parcial só existe em `solicitacoes_vinculo`, não em `vinculo_profissional_estabelecimento` — confirmado em `VinculoProfissionalEstabelecimentoConfiguration.cs:28-29`).

| Severidade | médio |
|---|---|
| Cenário | Dois donos do estab (improvável mas possível com co-donos futuros) ou duas requisições simultâneas do mesmo dono → cria dois vínculos para o mesmo `(profissional, estab)` em estado `Convidado`. UI mostra dois convites. |
| Recomendação | Adicionar unique constraint parcial em `vinculo_profissional_estabelecimento(profissional_usuario_id, estabelecimento_id) WHERE status IN ('Ativo','Convidado')`. Catch da `DbUpdateException` no handler e rethrow como `BusinessException`. |

### 7.3 `AlterarModeloPermissaoDoVinculoCommand` — sem endpoint exposto

Handler existe (`AlterarModeloPermissaoDoVinculoCommandHandler.cs`) e está registrado no Container (`Container.cs:296,618`). **Não há controller que dispare**. Dead code OU foi removido sem limpar o handler.

| Severidade | baixo (dead code) — porém o **handler tem buraco arquitetural**: |
|---|---|
| Buraco | Linha 30: `if (vinculo.EstabelecimentoId != command.EstabelecimentoId) throw...`. Se o controller futuro passar `EstabelecimentoId` do tenant ativo (correto), não há IDOR. Mas o command aceita um `VinculoId` qualquer + `EstabelecimentoId` qualquer — defense-in-depth: usar `ObterPorIdOuNulo(VinculoId, EstabelecimentoId)` (ver §3.1). |
| Recomendação | Decidir: implementar o endpoint (`PATCH /vinculo/{id}/modelo-permissao`) com `[RequiresPapel(Dono)]` + `[RequiresEstabelecimento]`, OU remover o command + handler (parte da limpeza da Fase 1). |

### 7.4 `AoAprovarSolicitacaoCriarVinculoHandler` — não-determinismo do modelo padrão

Linha 68: `var modeloPadrao = await _modeloRepo.ObterPadraoDoEstabelecimento(...)`. Se houver mais de um modelo "padrão" (race), pega um. Se não houver, lança `BusinessException` — mas dentro do event handler na **mesma transação do command** → o command `Aprovar` falha 422 ao dono mesmo a solicitação tendo sido aprovada do ponto de vista do profissional ("aprovar" virou um erro silencioso para o solicitante). UX confusa.

| Severidade | médio |
|---|---|
| Recomendação | Validar antes de aprovar (no handler do command) que existe modelo padrão; abortar com mensagem clara: "Configure um modelo de permissão padrão antes de aprovar solicitações." |

### 7.5 Front: drift de tipos `SolicitacaoVinculo` vs `SolicitacaoVinculoDto`

Front declara (`solicitacaoVinculoService.ts:5-17`):
- `estabelecimentoNome?` (backend retorna `EstabelecimentoNomeFantasia`) → **mismatch silencioso**, sempre undefined no front.
- `respondidaPorUsuarioId` → **não existe no DTO do backend** (foi removido para minimização? ou nunca foi adicionado?). Sempre undefined.
- DTO backend tem `profissionalEmail` que o front **não declara** — vai pelo wire e some.

| Severidade | médio (drift de contrato) |
|---|---|
| Recomendação | Após corrigir os DTOs (§1), gerar/sincronizar tipos TS via geração automática (OpenAPI → ts) ou alinhar manualmente. Lint test: `vue-tsc` não pega esse drift porque `?` torna tudo opcional. |

### 7.6 `IUsuarioRepository.ObterPorId(Guid)` — uso seguro no escopo desta fase

O contrato (`Domain/Usuarios/IUsuarioRepository.cs:5-6`) tem `ObterPorId` que provavelmente lança `KeyNotFoundException` (mesmo padrão dos outros). Nos handlers de Vínculos, **só usamos `ObterPorIdOuNulo`** (`ConvidarProfissionalCommandHandler.cs:62`, `AoAprovarSolicitacaoCriarVinculoHandler.cs:64`, `NotificarConviteAoConvidarProfissionalHandler.cs:54`, `NotificarSolicitacaoCriadaHandler.cs:56`). **OK**. Não é o gargalo desta fase, mas anotar para Fase 3.5.

### 7.7 `ConvidarProfissional` — `actionLink` em DEV no body

`VinculoController.cs:60-69`: em `IsDevelopment`, devolve `actionLink` (link mágico do Supabase com token). **Esperado em dev** (mesma decisão da Fase 3.3 para `accessToken`), mas:
- Confirmar `Auth:ExposeActionLinkInBody=true` ou guarda extra em staging/preview Vercel.
- **Crítico se vazar em prod**: o link permite ativação imediata da conta sem o profissional ter feito nada.

---

## 8. Top 5 ações priorizadas

1. **Corrigir DTOs (§1)** — remover `ProfissionalEmail` + `EstabelecimentoNomeFantasia` de `SolicitacaoVinculoDto` (split em 2 DTOs: `MinhaDto` e `RecebidaDto`); remover `ConvidadoPorEmail` + `Telefone/Especialidade/Modelo` de `ConviteDto`. Atualizar SQL nos 2 query repositories. **Bloqueia LGPD; sem regressão funcional confirmada (campos não consumidos no front).**
2. **Defense-in-depth nos repos (§3.1, §3.2)** — substituir `ObterPorId(long)` por `ObterPorIdOuNulo(id, tenantContext)`; trocar `KeyNotFoundException` por retorno `null` + `BusinessException` genérico no handler ("Vínculo não encontrado ou indisponível"). Mata enumeração + IDOR + 500.
3. **Gates de autorização (§4)** — decidir Dono vs `GerirProfissionais` para `/convidar` e aplicar `[RequiresPapel(TenantPapel.Dono)]` em `recebidas/aprovar/recusar`. Adicionar gate em `/estabelecimento/{id}/profissionais`. Remove o "404-vs-422" + a inconsistência atual atributo×handler.
4. **Audit trail (§6)** — criar tabela `vinculo_audit_log` + `IVinculoAuditLogRepository` e gravar nos 8 commands (Convidar, Aceitar, Inativar, AlterarModelo, Solicitar, Aprovar, Recusar, Cancelar). LGPD Art. 37 + base legal de acesso aos prontuários depende disso.
5. **Rate limit (§4.4) + mensagem genérica em `/convidar` (§2.1)** — adicionar `[EnableRateLimiting("auth-sensitive")]` em `POST /convidar` e `POST /solicitacoes-vinculo` (e `/aceitar`); trocar mensagens distintas por uma única "Não foi possível processar este convite." Bloqueia enumeração de e-mails de profissionais entre clínicas.
