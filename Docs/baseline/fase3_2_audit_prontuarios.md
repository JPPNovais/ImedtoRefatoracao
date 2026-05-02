# Auditoria LGPD — Módulo Prontuários (Fase 3.2)

**Data**: 2026-05-02
**Escopo**: backend `Prontuarios/*` (Prontuário, Evolução, ExameFísico, Anexo, Modelo, VariávelPool, AcessoLog) + cross-check com `frontend/src/{services,views,components}` correspondentes.

> Prontuário é o módulo mais sensível do sistema (Art. 11 LGPD — dado pessoal sensível de saúde). Áudio aplicado com tolerância zero a vazamento, ausência de audit trail, ou autorização frouxa.

## Sumário

| Categoria | Achados |
|---|---|
| Campos PII / dados desnecessários em DTO | **5** (1 crítico) |
| Queries Dapper sem `WHERE estabelecimento_id` | **1** crítico (`ObterReferenciaAnexo`) + **1** alto (regiões filhas sem re-validação) |
| Mensagens de erro com PII | **0** |
| Logs com PII | **0** |
| Gaps de autorização / controle de tenant | **3** (1 crítico) |
| Audit trail | **8 lacunas** (incluindo Listar Anexos, Templates) |
| Bug runtime conhecido (`exameFisicoService`) | **CONFIRMADO** — quebra em 3 fluxos |
| Bugs adicionais | **3** (KeyNotFound→500, Salvar fora UoW, sem soft-delete em handler) |
| Migração `IPacienteRepository` defense-in-depth | **3 pontos** identificados |

---

## 1. DTOs — minimização (cross-check com front)

### `ExameFisicoDto` (`Contracts/Prontuarios/Queries/Results/ExameFisicoDto.cs`)

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `Id` | sim | — | manter |
| `EvolucaoId` | sim (filtro de timeline) | — | manter |
| `ProntuarioId` | **não** (não há nav que precise no front) | médio LGPD | **remover** |
| `PacienteId` | **não** (já vem da rota) | médio LGPD | **remover** |
| `RealizadoEm` | sim (consumido como `criado_em` — bug) | — | manter |
| `RealizadoPorUsuarioId` | **não** lido (interface front pede `profissional_id`) | **alto LGPD** | **remover** — Guid de auth interno, exposição amplia superfície de ataque |
| `RealizadoPorNome` | parcial — tela espera `profissional_nome` (bug) | médio | manter (após corrigir front) |
| `DadosGeraisJson` (string) | front espera objeto `dados_gerais` (bug) | — | manter, corrigir front |
| `ObservacoesGerais` | front espera `observacoes` (bug) | — | manter, corrigir front |
| `CriadoEm` / `AtualizadoEm` | parcial | baixo | manter (auditoria via tela) |
| `Regioes` (lista detalhada) | sim em obter; **não** em timeline | — | manter |

### `ExameFisicoResumoDto`

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `Id`, `EvolucaoId` | sim | — | manter |
| `RealizadoEm` | sim (mapeado errado para `criado_em`) | — | manter |
| `RealizadoPorNome` | sim (mapeado errado para `profissional_nome`) | — | manter |
| `TotalRegioes` | **não** lido pelo `ExameFisicoTimeline.vue` | baixo | manter (útil em futuras telas) |
| `TemDadosGerais` | **não** lido | baixo | manter |
| `SeveridadeMaxima` | **não** lido | baixo | manter (badge clínica futura) |

> Severidade **alta** registrada apenas no `RealizadoPorUsuarioId`. Os IDs internos (Guid de Supabase Auth) são detalhe de implementação — expor permite enumeração de usuários auth ativos.

### `ProntuarioDto` (`ProntuarioDto.cs`)

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `Id` | sim (download anexo, etc.) | — | manter |
| `PacienteId` | **não** (já vem do contexto da rota) | médio LGPD | **remover** |
| `EstabelecimentoId` | **não** | **crítico LGPD** (mesmo padrão da Fase 3.1) | **remover** |
| `ModeloDeProntuarioId` | sim (drop de troca de template) | — | manter |
| `ModeloNome`, `ModeloEstrutura` | sim | — | manter |
| `CriadoEm`, `AtualizadoEm` | parcial | — | manter (`AtualizadoEm` baixo) |

### `EvolucaoDto`

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `Id`, `ProntuarioId`, `AutorNome`, `Conteudo`, `ModeloSnapshot`, `ModeloNome`, `ModeloDeProntuarioIdOrigem`, `CriadaEm` | sim | — | manter |
| `AutorUsuarioId` (Guid) | **não** lido (front não consome) | **alto LGPD** | **remover** — mesma razão do `RealizadoPorUsuarioId` |

### `AnexoDto`

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `Id`, `NomeOriginal`, `MimeType`, `TamanhoBytes`, `CriadoEm`, `AutorNome` | sim | — | manter |
| `ProntuarioId` | **não** lido | baixo | **remover** |
| `EvolucaoId` | parcial | baixo | manter |

### `AnexoUrlDto`

URL **assinada com TTL** (default 5 min, configurável via `StorageOptions.TtlSignedUrlMinutos`). Path correto. **OK.** Recomendação adicional: registrar no `ProntuarioAcessoLog` o nome do anexo (não o storage path completo) já é feito — não logar a URL assinada nem em log nem em retorno extra (URL eterna em log = leak permanente; aqui ela tem TTL, mas mesmo assim **não logar a URL no `ILogger`**).

### `ModeloProntuarioDto` / `VariavelPoolDto`

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `EstabelecimentoId` | **não** lido | médio LGPD (Tenant ID em DTO de catálogo) | **remover** — UI já sabe se é padrão-sistema (`EhPadraoSistema`); expor o tenant é desnecessário |

---

## 2. Queries Dapper — multi-tenant

### `ProntuarioQueryRepository`

| Método | `WHERE estabelecimento_id`? | Linha | Status |
|---|---|---|---|
| `ObterDoPaciente` (sqlPront) | sim | 34 | ✅ |
| `ObterDoPaciente` (sqlEvo — evoluções) | **não filtra por estabelecimento** — só pelo `prontuario_id` | 51 | ⚠️ aceitável (`prontuario_id` veio do prontuário já validado), mas defense-in-depth recomenda re-filtrar. Severidade **baixa**. |

### `ExameFisicoQueryRepository`

Todos os métodos: `ObterCompleto`, `ObterPorEvolucao`, `ListarDoPaciente`, `Timeline` filtram por `e.estabelecimento_id = @EstabelecimentoId`. ✅

`CarregarRegioes` (linha 209): só filtra por `exame_fisico_id`. **Aceitável** (FK + valida no parent), mas recomendo manter o JOIN para defense-in-depth.

### `ProntuarioAnexoQueryRepository`

| Método | Filtro tenant | Severidade |
|---|---|---|
| `ListarDoProntuario` | só `prontuario_id` (linha 29) — handler valida `prontuario` antes | médio (defense-in-depth fraca) |
| `ObterReferenciaAnexo` (linha 47) | **NÃO TEM FILTRO DE TENANT NA QUERY** — busca o anexo por `id` global e devolve `estabelecimento_id` para o handler comparar | **CRÍTICO LGPD** — falha-fechado deveria ser na query, não fora dela. Se o `if (estabelecimentoId != query.EstabelecimentoId)` for removido por engano em refatoração futura, vira IDOR direto. |

**Recomendação**: passar `estabelecimentoId` como parâmetro e adicionar `AND estabelecimento_id = @EstabelecimentoId` no SQL. O `if` no handler vira redundante (defense-in-depth real).

### `ModeloProntuarioQueryRepository`

`ListarDisponiveis` e `ObterVisivelPara` usam `(eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId)` — escopo correto. ✅

### `VariavelPoolQueryRepository`

Mesma fórmula do `ModeloProntuarioQueryRepository`. ✅

---

## 3. Mensagens de erro e logs

### Mensagens de `BusinessException`

Todas as 21 mensagens revisadas em `Application/Prontuarios/**` são genéricas (sem PII): `"Paciente não pertence a este estabelecimento."`, `"Modelo inativo."`, `"Anexo não encontrado."`, etc.

**Sem vazamento.** ✅

### Logs estruturados

`ProntuarioIniciadoEventHandler` e `EvolucaoRegistradaEventHandler` (`Events/ProntuarioIniciadoEventHandler.cs`) logam apenas IDs (Long e Guid). Sem PII (nome, CPF, conteúdo clínico).

**Sem vazamento.** ✅

> Recomendação preventiva: o `ILogger` do exception filter eventualmente vai logar `BusinessException` com a mensagem; como não há PII nas mensagens, está limpo.

---

## 4. Audit trail (`ProntuarioAcessoLog`) — análise por handler

`TipoAcessoProntuario` só tem dois valores: `Leitura` e `Escrita`. **Lacuna conceitual** — não cobre `Exclusao`, `Export`, `Anonimizacao` exigidos pela LGPD para titular pedir relatório.

| Handler | Tipo de acesso | Hoje registra? | Severidade | Ação |
|---|---|---|---|---|
| `ObterProntuarioDoPacienteQueryHandlers` | Leitura | sim (cond. `Prontuario != null`) | — | OK |
| `ObterExameFisicoQueryHandlers.Handle(ObterExameFisicoQuery)` | Leitura | sim | — | OK |
| `ObterExameFisicoQueryHandlers.Handle(ObterPorEvolucaoQuery)` | Leitura | sim | — | OK |
| `ObterExameFisicoQueryHandlers.Handle(ListarExamesFisicosDoPacienteQuery)` | Leitura | sim, **só se Total > 0** | médio LGPD | listagem com 0 resultados também é acesso (informação de "olhei se tinha exame"). Auditar incondicionalmente, com `prontuario_id` resolvido a partir do `paciente_id`. |
| `ObterExameFisicoQueryHandlers.Handle(TimelineQuery)` | Leitura | sim, condicionado | médio LGPD | idem acima |
| `ObterUrlAnexoQueryHandlers` | Leitura | sim (após gerar URL) | — | OK |
| `ListarAnexosDoProntuarioQueryHandlers` | Leitura | **NÃO REGISTRA** | **alto LGPD** | listar anexos é leitura de metadata sensível (nomes de arquivos clínicos podem indicar diagnóstico — "Laudo Mamografia 2024.pdf") |
| `ObterModeloDeProntuarioQueryHandlers` | Leitura | não (template, ok) | — | sem PII; OK não auditar |
| `ListarModelosDisponiveisQueryHandlers` | Leitura | não (template) | — | OK |
| `ListarVariaveisPoolQueryHandlers` | Leitura | não (catálogo) | — | OK |
| `IniciarProntuarioCommandHandler` | Escrita | sim | — | OK |
| `RegistrarEvolucaoCommandHandler` | Escrita | sim | — | OK |
| `AdicionarAnexoCommandHandler` | Escrita | sim | — | OK |
| `RegistrarExameFisicoCommandHandler` | Escrita | sim | — | OK |
| `AtualizarExameFisicoCommandHandler` | Escrita | sim | — | OK |
| `CriarModeloDeProntuarioCommandHandler` | Escrita (config) | não | baixo | aceitável — modelo é template, sem dado de paciente |
| `AtualizarModeloDeProntuarioCommandHandler` | Escrita (config) | não | baixo | OK |
| `ExcluirModeloDeProntuarioCommandHandler` | Escrita (config) | não | baixo | OK |
| `AdicionarVariavelPoolCommandHandler` / `Atualizar` / `Excluir` | Escrita (config) | não | baixo | OK |

### Conteúdo do log (`ProntuarioAcessoLog`)

Hoje captura: `prontuario_id`, `usuario_id`, `estabelecimento_id`, `tipo_acesso`, `ocorrido_em`. Faltam para padrão LGPD/ISO 27799:

| Campo faltando | Severidade | Ação |
|---|---|---|
| `paciente_id` | alto | adicionar — facilita responder a pedido de relatório do titular ("quem viu meus dados?") sem precisar de JOIN com `prontuarios` |
| `ip_origem` | médio | adicionar (vem do `HttpContext.Connection.RemoteIpAddress`) |
| `user_agent` | baixo | opcional — útil em forense |
| Enum `Exclusao`, `Export`, `Anonimizacao` no `TipoAcessoProntuario` | médio | já preparar para fases futuras (não criar handler ainda) |

### Resiliência do log

`ProntuarioAcessoLogService.RegistrarAsync` chama `_context.SaveChangesAsync()` **direto** (linha 22). Isso:

1. **Quebra a transação do `UnitOfWorkAttribute`** se a action vier dentro de UoW. Nos handlers de **Command** (Iniciar, RegistrarEvolucao, etc.) o `Salvar(...)` antes do log já chama `SaveChanges` próprio também — então não há UoW real (cada operação isolada). Ainda assim: **se um command falhar após o `Salvar` mas antes do log, o log nunca grava**. Mau para auditoria.
2. **Em queries (read-side)**: handler de query não tem UoW e não há outro `SaveChanges` envolvido — está OK, mas a perda de log em caso de erro de DB ainda é um risco.

**Recomendação**: usar `INSERT ... ON CONFLICT DO NOTHING` direto via Dapper na conexão de escrita (independente do `AppDbContext`), ou fire-and-forget para outbox. Audit deveria ser durável mesmo se o DbContext explodir.

---

## 5. Autorização

### Estado atual

| Endpoint | Atributos | Status |
|---|---|---|
| `ProntuarioController` (todos) | `RequiresPapel(Profissional, Dono)` | ✅ — Recepcionista bloqueado |
| `ExameFisicoController` (todos) | `RequiresPapel(Profissional, Dono)` + `FeatureGate(ExameFisico)` | ✅ |
| `ProntuarioAnexoController` (todos) | `RequiresPapel(Profissional, Dono)` | ✅ |
| `ProntuarioTemplateController` GET (modelos/pool) | apenas `RequiresEstabelecimento` (sem `RequiresPapel`) | ⚠️ — Recepcionista lista modelos/variáveis (não viu dado clínico, mas vazio: o que vai na variável é o que vai no prontuário). Severidade **baixa** — modelos são metadata. |
| `ProntuarioTemplateController` POST/PUT/DELETE (modelos/pool) | `RequiresPermissaoExtra(ModelosProntuario)` | ✅ |

### Gaps reais

| # | Gap | Severidade | Recomendação |
|---|---|---|---|
| G1 | `ObterReferenciaAnexo` (Dapper) busca anexo sem filtro de tenant na query — re-validação no handler é única defesa | **crítico** | mover filtro para o SQL (item §2 acima) |
| G2 | `ProntuarioAnexoController.Listar` aceita `evolucaoId` opcional sem validar que a evolução pertence ao prontuário do paciente | médio | filtrar com JOIN/EXISTS na query e/ou validar no handler — hoje qualquer `evolucaoId` numérico passa (volta vazio se for de outro tenant, mas vaza informação "se eu enviar 7 retorna lista vazia"). Atenção: timing-attack baixíssimo aqui |
| G3 | `ExameFisicoController.Atualizar` (`PUT /api/exame-fisico/{id}`) — handler valida tenant via `if (exame.EstabelecimentoId != command.EstabelecimentoId)` (linha 31). Mesma fragilidade do G1 — falha-aberta se o `if` for removido | médio | `IExameFisicoRepository.ObterPorId(long id, long estabelecimentoId)` filtrando dentro do `Where`. Falha-fechada por design. |
| G4 | "Profissional pode acessar prontuário de paciente de qualquer outro Profissional do mesmo estabelecimento" — não há validação de "prontuário pertence à minha equipe / minhas consultas" | informativo | decisão de produto: se for por design (clínica compartilha base), está OK; auditar via `ProntuarioAcessoLog` sustenta a rastreabilidade. Documentar em CLAUDE.md ou no controller. |

### Vazamento de existência

`Anexo não pertence a este estabelecimento.` (linha 63 de `ListarAnexosDoProntuarioQueryHandlers.cs`) — **vaza que o anexo existe**. Usuário consegue saber se um `anexoId` global pertence a outro tenant. Severidade **médio LGPD**. Trocar por `Anexo não encontrado.` (mesma mensagem do não-encontrado).

---

## 6. Performance e resiliência

| Item | Status | Observação |
|---|---|---|
| `AsNoTracking()` em queries Dapper | N/A — Dapper já não rastreia | ✅ |
| `AsNoTracking()` em EF (read no `RegistrarExameFisicoCommandHandler`) | sim (linhas 40-41) | ✅ |
| Clamp de paginação | `ListarExamesFisicosDoPacienteQuery` (1-100 ✓), `Timeline` (1-50 ✓), `ObterDoPaciente.timeline` (1-500) | clamp 500 é generoso — considerar 100 |
| N+1 em listagem de anexos | não — query única com JOIN em usuarios | ✅ |
| N+1 em ExameFisico | há 2 subqueries por linha (`TotalRegioes` + `SeveridadeMaxima`). Para timeline curto (≤50) é OK; para listagem (100) começa a custar | melhorar com `LATERAL JOIN` ou agregação CTE. Severidade **médio performance**, não LGPD |
| Índices Postgres | revisados nas Configurations: `ix_exame_fisico_paciente_realizado` (DESC), `ix_evolucoes_prontuario_data`, `ix_anexos_prontuario`, `ix_acesso_log_prontuario_data`, `uq_prontuario_paciente_estabelecimento`, `ux_exame_fisico_regiao_codigo` | ✅ cobertura boa |
| Falta de índice | `prontuario_anexos.estabelecimento_id` — não tem (todas as buscas vão por `prontuario_id`, então OK) | informativo |
| Limite de upload | `RequestSizeLimit(60 MB)` no controller + validação `StorageOptions.TamanhoMaxMb` no handler | ✅ |
| Validação de MIME type | sim (allowlist em `StorageOptions.MimeTypesPermitidos`) | ✅ — boa prática |
| Sanitização de nome de arquivo | `SanitizarNome` em `AdicionarAnexoCommandHandler` (`Path.GetFileName` + whitelist) | ✅ excelente — bloqueia path traversal |

---

## 7. Bug runtime — `exameFisicoService.ts` vs novos endpoints

### Confirmação: bug REAL — não falso positivo

Backend retorna **camelCase** (System.Text.Json default) com nomes da `ExameFisicoDto` C#. Front consome com interface **snake_case legada** (formato antigo Supabase). Mismatch em 3 fluxos:

| Local | Linha | Campo do front | Campo real do backend | Resultado runtime |
|---|---|---|---|---|
| `ExameFisicoTimeline.vue` | 81 | `exame.criado_em` | `realizadoEm` | **string `undefined` em `formatDate`** → "Invalid Date" no header da timeline |
| `ExameFisicoTimeline.vue` | 84 | `exame.profissional_nome` | `realizadoPorNome` | **mostra `undefined`** no card |
| `ExameFisicoTimeline.vue` | 88 | `exame.evolucao_prontuario_id` | `evolucaoId` | badge "Vinculado" **nunca aparece** |
| `ExameFisicoTimeline.vue` | 29 | `exame.regioes_examinadas` | timeline retorna **resumo sem regiões** (`ExameFisicoResumoDto` não tem `Regioes`) | `getRegioesResumo` sempre devolve "Sem regiões específicas" |
| `ExameFisicoTab.vue` | 200 | `exame.dados_gerais` (em `onDuplicarExame`) | `obterPorEvolucao` retorna `ExameFisicoDto` com `dadosGeraisJson: string` (JSON serializado) | **`Object.assign(...JSON.parse(JSON.stringify(string)))` produz objeto vazio** → função "duplicar" não copia nada |
| `ExameFisicoTab.vue` | 201-202 | `exame.regioes_examinadas` | retorno tem `regioes: RegiaoExameFisicoDto[]` com chaves `regiaoCodigo`/`achados`/`severidade`/`lateralidade` (não `regiao_id`/`caminho`/`texto_exame`) | **regiões não duplicam** |

**Não quebra no compilador TypeScript** (campos são `?` ou tela ignora silenciosamente), mas **quebra silenciosa em produção** (timeline mostrando `undefined`/datas inválidas; funcionalidade "duplicar exame" virou no-op).

### Correção (proposta — apenas para o relatório, não aplicar agora)

1. Reescrever interfaces TS para refletir os DTOs reais (camelCase): `ExameFisicoDto`, `ExameFisicoResumoDto`, `RegiaoExameFisicoDto` separadas — não há "registro único".
2. Em `ExameFisicoTab.onDuplicarExame`: precisa **buscar o exame completo via `obterPorEvolucao`** (não dá pra duplicar a partir do resumo da timeline).
3. `dadosGeraisJson` é string — front faz `JSON.parse` antes de usar.
4. Mapear `regiao.regiaoCodigo → regiaoId`, `regiao.achados → achados`, etc., considerando que o modelo de domínio agora é por **regiões anatômicas codificadas (`RegiaoCodigo`)** e não por **id de regiao + caminho_texto**.

> **Atenção LGPD**: ao corrigir, garantir que o exame retornado para `Duplicar` passe **pelo audit log** (já faz — `ObterExameFisicoQueryHandlers` audita). OK.

---

## 8. Bugs adicionais

### B1 — `KeyNotFoundException` → 500

`ProntuarioRepository.ObterPorId(long id)` linha 18, `ProntuarioAnexoRepository.ObterPorId(long id)` linha 17, `ExameFisicoRepository.ObterPorId(long id)` linha 20, `ModeloDeProntuarioRepository.ObterPorId(long id)` linha 17, `ProntuarioVariavelPoolRepository.ObterPorId(long id)` linha 18.

Cinco repositórios lançam `KeyNotFoundException` em vez de `BusinessException`. `GlobalExceptionFilter` mapeia somente `BusinessException` para 422 — `KeyNotFound` vira **500 com stack trace** (vazamento de implementação + UX ruim). Severidade **alto**.

### B2 — `Salvar` chama `SaveChangesAsync` fora do UoW

Mesmo problema da Fase 3.1. Em `ProntuarioRepository.Salvar` (linha 33), `ProntuarioAnexoRepository` (24), `ExameFisicoRepository` (36), `ModeloDeProntuarioRepository` (28), `ProntuarioEvolucaoRepository` (56), `ProntuarioVariavelPoolRepository` (37) — todos fazem `SaveChangesAsync()` no caminho de create (Id == 0).

**Consequência LGPD**: se o evento subsequente (`MarcarComoIniciado`, `RegistrarAsync` do log) falhar, o aggregate fica salvo **sem evento e sem audit**. Severidade **alta** — quebra a integridade do audit trail.

### B3 — `RegistrarEvolucaoCommandHandler` não checa `EstaDeletado` do prontuário

Linha 43-44: pega prontuário, mas não valida `prontuario.DeletadoEm`. Se um prontuário soft-deletado existir (Fase futura), permitirá registrar evolução. Severidade **médio**.

### B4 — Concorrência em "iniciar prontuário"

`IniciarProntuarioCommandHandler` faz `if (existente is not null) throw` mas **não há lock**. Duas requests simultâneas iniciando prontuário do mesmo paciente passam pelo `if`, criam duplicata, e a segunda explode com unique violation `uq_prontuario_paciente_estabelecimento` → **500**. Severidade **médio**. Solução: catch de `DbUpdateException` por unique e converter para `BusinessException`.

---

## 9. Migração defense-in-depth `IPacienteRepository.ObterPorId` (Fase 3.1 → 3.2)

Os 3 handlers de Prontuários consomem o método **deprecated** `ObterPorId(long)`:

| Arquivo | Linha | Como migrar |
|---|---|---|
| `IniciarProntuarioCommandHandler.cs` | 34 | `var paciente = await _pacienteRepo.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId) ?? throw new BusinessException("Paciente não encontrado.");` — remove o `if (paciente.EstabelecimentoId != command.EstabelecimentoId)` da linha 35-36 (já garantido pelo filtro do repo) |
| `RegistrarEvolucaoCommandHandler.cs` | 37 | mesma migração; remove `if EstabelecimentoId != ...` da linha 38-39 |
| `AdicionarAnexoCommandHandler.cs` | 38 | mesma migração; remove `if EstabelecimentoId != ...` da linha 39-40 |

`EstabelecimentoId` correto = `command.EstabelecimentoId` (já vem do `RequiresEstabelecimento` filter, populado em `ICurrentTenantAccessor`). Resultado: handler perde 1 if de validação manual, ganha falha-fechada por design.

---

## 10. Top 5 ações em ordem de prioridade

1. **CRÍTICO LGPD** — `ProntuarioAnexoQueryRepository.ObterReferenciaAnexo`: adicionar `AND estabelecimento_id = @EstabelecimentoId` no SQL e tornar a validação no handler **redundante** (defense-in-depth real). Hoje o IDOR está bloqueado por um único `if` no handler. (G1)

2. **CRÍTICO LGPD** — Bug do `exameFisicoService.ts`: reescrever as interfaces TS para refletir os DTOs camelCase reais (`ExameFisicoDto`, `ExameFisicoResumoDto`, `RegiaoExameFisicoDto`) e ajustar `ExameFisicoTimeline.vue`/`ExameFisicoTab.vue`. Hoje a timeline mostra `undefined`/`Invalid Date` e a função "duplicar exame" é no-op silencioso. **Resolver antes de mexer em qualquer DTO de exame.**

3. **ALTO LGPD** — Adicionar audit no `ListarAnexosDoProntuarioQueryHandlers` (gap completo); auditar incondicionalmente em `Listar`/`Timeline` de exames físicos (hoje só audita se `Total > 0` — perde a informação "olhei e não tinha"); incluir `paciente_id` e `ip_origem` no `ProntuarioAcessoLog` + estender `TipoAcessoProntuario` com `Exclusao`/`Export`.

4. **ALTO LGPD / arquitetura** — Migrar `IPacienteRepository.ObterPorId(long)` deprecated em 3 handlers de Prontuários para `ObterPorIdOuNulo(long, long)` + remover os `if` redundantes. Bloqueia a remoção da sobrecarga deprecated da Fase 3.1. Aplicar mesmo princípio para `IExameFisicoRepository.ObterPorId(long)` e `IProntuarioAnexoRepository.ObterPorId(long)` (G3).

5. **ALTO** — Bugs B1 (KeyNotFound→500: 5 repositórios) e B2 (Salvar fora do UoW: 6 repositórios). Ambos são erros sistêmicos do módulo. Após corrigir, **remover** `RealizadoPorUsuarioId` e `AutorUsuarioId` (Guid) dos DTOs `ExameFisicoDto`/`EvolucaoDto`, `EstabelecimentoId` de `ProntuarioDto`/`ModeloProntuarioDto`/`VariavelPoolDto`, `PacienteId`/`ProntuarioId` quando redundantes. Mensagem `"Anexo não pertence a este estabelecimento."` → `"Anexo não encontrado."` (vazamento de existência).
