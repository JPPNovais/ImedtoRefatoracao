# Fase 1 — Hardening do que já existe

**Status geral:** ✅ concluída (com pendências documentadas)
**Iniciada em:** 2026-04-28
**Concluída em:** 2026-04-28

> **Por que esta fase:** corrigir invariantes faltantes e endurecer endpoints existentes antes de adicionar features novas. Cada item é cirúrgico, mexe em código já estabilizado.
>
> **Pré-requisitos:** nenhum.
>
> **Referência principal:** [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md) — fonte da verdade do plano completo.

## Plano de agentes

| Agente | Itens | Modelo |
|--------|-------|--------|
| `senior-software-engineer` | 1.1 (domain+handler), 1.3, 1.4 (domain+handler), 1.5 | Opus |
| `software-engineer` | 1.2 | Sonnet |
| `database-architect` | 1.1, 1.4, 1.7 (schema + migrations) | Opus |
| `security-engineer` | 1.6, 1.7 (rate limit + decorator) | Opus |
| `ui-implementer` | 1.1 (front) | Sonnet |
| `qa-engineer` | Testes unitários de todos os itens | Sonnet |
| `migration-engineer` | Revisão de paridade legado→novo | Opus |

**Ondas de execução:**
1. Wave 1 (paralela): backend, agendamento, security, frontend.
2. Wave 2 (sequencial): database-architect gera migrations.
3. Wave 3 (paralela): qa + migration-engineer.
4. Build + test.

---

## Schema fechado (todos os agentes seguem isso)

### Inventário (item 1.1)

**Tabela `itens_inventario`** — adicionar:
- `custo_medio` `numeric(18,4) not null default 0` — custo médio ponderado atual.

**Tabela `movimentacoes_estoque`** — adicionar:
- `custo_unitario` `numeric(18,4) not null default 0` — custo unitário no momento do movimento (snapshot).
- `custo_total` `numeric(18,4) not null default 0` — `custo_unitario * quantidade`, snapshot.

### Auditoria de delete (item 1.4)

**Tabela `audit_delete_attempts`** (nova):
```
id            bigserial primary key
tabela        varchar(80) not null
registro_id   varchar(80) not null      -- aceita bigint ou guid como string
estabelecimento_id bigint null          -- quando aplicável (multi-tenant)
usuario_id    uuid null                 -- quem tentou (pode ser null para sistema)
motivo        varchar(500) null
tentado_em    timestamptz not null default now()
```
Índices: `(tabela, tentado_em desc)` e `(estabelecimento_id, tentado_em desc)`.

**Soft delete** — adicionar nas tabelas: `prontuarios`, `prontuario_evolucoes`, `prontuario_anexos`, `movimentacoes_estoque`, `pacientes`:
- `deletado_em` `timestamptz null`
- `deletado_por_usuario_id` `uuid null`

Nas queries de listagem (Dapper) padrão: `WHERE deletado_em IS NULL`.

### IA Audit / Cache / Rate Limit (item 1.7)

**Tabela `ai_audit_logs`** (nova):
```
id              bigserial primary key
usuario_id      uuid not null
estabelecimento_id bigint not null
prompt_hash     varchar(64) not null     -- sha256
response_hash   varchar(64) null
tokens_in       int null
tokens_out      int null
modelo          varchar(80) not null
endpoint        varchar(80) not null     -- "sugestao-secao" etc.
duracao_ms      int null
sucesso         boolean not null
erro_mensagem   varchar(500) null
criado_em       timestamptz not null default now()
```
Índices: `(usuario_id, criado_em desc)`, `(estabelecimento_id, criado_em desc)`.

**Tabela `ai_outputs_cache`** (nova):
```
prompt_hash     varchar(64) primary key
estabelecimento_id bigint not null
endpoint        varchar(80) not null
output          text not null
tokens_in       int null
tokens_out      int null
expira_em       timestamptz not null
criado_em       timestamptz not null default now()
```
Índice: `(expira_em)` para limpeza.

**Tabela `ai_rate_limits`** (nova):
```
id              bigserial primary key
usuario_id      uuid not null
periodo_inicio  timestamptz not null
contagem        int not null default 1
ultimo_acesso   timestamptz not null default now()
```
Índice único `(usuario_id, periodo_inicio)`. Janela: 1 minuto.

### Seed financeiro (item 1.5)

**Categorias padrão** (ao criar estabelecimento):
- `Receita: Consulta`, `Receita: Procedimento`, `Receita: Outros`
- `Despesa: Folha`, `Despesa: Aluguel`, `Despesa: Insumos`, `Despesa: Outros`

**Formas de pagamento padrão:**
- `Dinheiro`, `PIX`, `Cartão de Crédito`, `Cartão de Débito`, `Transferência`, `Boleto`

> Se as tabelas `categorias_financeiras` e `formas_pagamento` ainda não existirem (verificar em `Domain/Financeiro/`), o agente que pega esse item deve documentar e mover o trabalho para depois de criá-las (registrar como bloqueio aqui no `.md`).

---

## Itens

### 1.1 Custo médio ponderado em movimentação de estoque

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer` (backend) + `database-architect` (migrations) + `ui-implementer` (front)
**Branch sugerido:** `feature/fase1-custo-medio-estoque`

#### Por quê

Legado tinha trigger `fn_movimento_estoque_set_custo` que recalculava custo unitário ponderado a cada movimento. Sem isso, relatórios financeiros que usam COGS retornam zero ou valor incorreto. **Bloqueia relatórios da Fase 4.**

#### Diff técnico

**Backend — Domain ([backend/src/Services/Imedto.Backend.Domain/Inventario/](../backend/src/Services/Imedto.Backend.Domain/Inventario/)):**

1. `ItemInventario.cs`:
   - Adicionar `public virtual decimal CustoMedio { get; protected set; }` (default 0).
   - Adicionar parâmetro `decimal custoUnitario` em `RegistrarEntrada` e `RegistrarSaida`.
   - Em `RegistrarEntrada`: recalcular `CustoMedio` = `((QuantidadeAtual * CustoMedio) + (quantidade * custoUnitario)) / (QuantidadeAtual + quantidade)`. Se `QuantidadeAtual == 0`, `CustoMedio = custoUnitario`. Validar `custoUnitario >= 0`.
   - Em `RegistrarSaida`: `custoUnitario` será o `CustoMedio` atual (snapshot). Se `QuantidadeAtual` chegar a 0 após saída, manter `CustoMedio` como está (próxima entrada redefine).

2. `MovimentacaoEstoque.cs`:
   - Adicionar `public virtual decimal CustoUnitario { get; protected set; }` e `public virtual decimal CustoTotal { get; protected set; }`.
   - Atualizar `Criar(...)` para receber `custoUnitario`. Calcular `custoTotal = quantidade * custoUnitario`.

**Backend — Application:**

3. `Contracts/Inventario/Commands/RegistrarMovimentacaoEstoqueCommand.cs`:
   - Adicionar `public decimal CustoUnitario { get; set; }` (opcional na saída — handler usa `CustoMedio` se 0).

4. `Application/Inventario/Commands/RegistrarMovimentacaoEstoqueCommandHandler.cs`:
   - Em `Entrada`: passar `cmd.CustoUnitario` para `item.RegistrarEntrada(quantidade, usuario, cmd.CustoUnitario, cmd.Observacao)`. Validar `> 0`.
   - Em `Saida`: usar `item.CustoMedio` como `custoUnitario` (snapshot). Não exigir `CustoUnitario` no command.

**Backend — Infrastructure:**

5. `Database/Configurations/ItemInventarioConfiguration.cs`:
   - `builder.Property(i => i.CustoMedio).HasColumnName("custo_medio").HasPrecision(18, 4).IsRequired();`

6. `Database/Configurations/MovimentacaoEstoqueConfiguration.cs`:
   - `builder.Property(m => m.CustoUnitario).HasColumnName("custo_unitario").HasPrecision(18, 4).IsRequired();`
   - `builder.Property(m => m.CustoTotal).HasColumnName("custo_total").HasPrecision(18, 4).IsRequired();`

**Backend — Repositórios de leitura (Dapper):**

7. Atualizar `ItemInventarioQueryRepository.cs` (se existir) para retornar `CustoMedio` no DTO.
8. Atualizar `MovimentacaoEstoqueQueryRepository.cs` para retornar `CustoUnitario`/`CustoTotal`.
9. Adicionar `custoMedio` em `ItemInventarioListadoDto` e `custoUnitario`/`custoTotal` em `MovimentacaoListadaDto` (em `Contracts/Inventario/Queries/`).

**Database (após backend):**

10. Gerar migration EF: `dotnet ef migrations add AdicionarCustoMedioEstoque --project Services/Imedto.Backend.Infrastructure --startup-project Services/Imedto.Backend.API --output-dir Database/Migrations`
11. Extrair SQL idempotente para `supabase/migrations/YYYYMMDDHHMMSS_adicionar_custo_medio_estoque.sql` (sem BEGIN/COMMIT).
12. Aplicar via `supabase db push`.

**Frontend ([frontend/src/views/inventario/](../frontend/src/views/inventario/)):**

13. Mostrar coluna "Custo médio" na lista de itens (se houver tela de listagem).
14. Mostrar "Custo unitário" e "Custo total" na lista de movimentações.
15. Em "Registrar entrada": adicionar campo numérico `custoUnitario` obrigatório.
16. Em "Registrar saída": NÃO mostrar campo de custo (handler usa CustoMedio).

#### Aceite (DoD)

- [ ] `dotnet build` limpo.
- [ ] Teste unitário: entrada 10 un a R$5 + entrada 10 un a R$7 → `CustoMedio` = R$6.
- [ ] Teste unitário: saída de 5 un após cenário acima → movimentação registra `CustoUnitario` = 6, `CustoTotal` = 30.
- [ ] Teste unitário: entrada com `custoUnitario < 0` lança `BusinessException`.
- [ ] Frontend mostra coluna "Custo médio" na listagem de itens.
- [ ] Migrations EF + supabase SQL geradas com mesmo timestamp.

---

### 1.2 Overlap de agenda na atualização

**Status:** ⏳ pendente
**Agente:** `software-engineer`
**Branch sugerido:** `feature/fase1-overlap-atualizacao-agenda`

#### Por quê

[CriarAgendamentoCommandHandler.cs:101-106](../backend/src/Services/Imedto.Backend.Application/Agendamentos/Commands/CriarAgendamentoCommandHandler.cs#L101-L106) chama `_agendamentoRepo.ExisteConflito(...)`. Mas `AtualizarAgendamentoCommandHandler` não. Profissional pode ser editado para horário ocupado. O repositório [IAgendamentoRepository.cs](../backend/src/Services/Imedto.Backend.Domain/Agendamentos/IAgendamentoRepository.cs) **já tem** o parâmetro `excluirAgendamentoId` na assinatura — só falta o handler chamar.

#### Diff técnico

1. [AtualizarAgendamentoCommandHandler.cs](../backend/src/Services/Imedto.Backend.Application/Agendamentos/Commands/AtualizarAgendamentoCommandHandler.cs) — após validar profissional e antes de `agendamento.Atualizar(...)`:
```csharp
if (await _agendamentoRepo.ExisteConflito(
        cmd.ProfissionalUsuarioId,
        cmd.InicioPrevisto,
        cmd.FimPrevisto,
        excluirAgendamentoId: cmd.AgendamentoId))
{
    throw new BusinessException("Já existe um agendamento neste horário para este profissional.");
}
```

2. Verificar implementação de `AgendamentoRepository.ExisteConflito` para garantir que `excluirAgendamentoId` é aplicado (`AND a.id <> @excluir`). Ajustar se não estiver.

#### Aceite (DoD)

- [ ] Teste unitário: editar agendamento mantendo o mesmo horário NÃO dispara conflito.
- [ ] Teste unitário: editar agendamento para horário ocupado por outro profissional/sala lança `BusinessException`.
- [ ] `dotnet build` limpo.

---

### 1.3 Reativação de vínculo Inativo ao re-convidar

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch sugerido:** `feature/fase1-reativar-vinculo-inativo`

#### Por quê

[VinculoProfissionalEstabelecimento.cs:61](../backend/src/Services/Imedto.Backend.Domain/Vinculos/VinculoProfissionalEstabelecimento.cs#L61) — `Aceitar()` só aceita `Convidado → Ativo`. O legado `aceitar_convite_profissional` reativa vínculo `Inativo`. Hoje, profissional inativado e re-convidado quebra porque [ConvidarProfissionalCommandHandler.cs:68-71](../backend/src/Services/Imedto.Backend.Application/Vinculos/Commands/ConvidarProfissionalCommandHandler.cs#L68-L71) só busca vínculo "ativo ou pendente" e cria novo — gerando duplicidade ou erro de unique constraint.

#### Diff técnico

1. `IVinculoRepository.cs`: adicionar:
```csharp
Task<VinculoProfissionalEstabelecimento?> ObterPorProfissionalEEstabelecimentoOuNulo(
    Guid profissionalUsuarioId, long estabelecimentoId);
```
Implementação no `VinculoRepository`: busca qualquer status (incluindo `Inativo`).

2. `VinculoProfissionalEstabelecimento.cs`: adicionar método:
```csharp
/// <summary>
/// Reativa um vínculo previamente inativado, transformando-o em novo Convite.
/// O profissional precisará aceitar de novo, mas o histórico (datas anteriores) é preservado.
/// </summary>
public virtual void ReativarComoConvite(long novoModeloPermissaoId, Guid convidadoPorUsuarioId)
{
    if (Status != VinculoStatus.Inativo)
        throw new BusinessException("Apenas vínculos inativos podem ser reativados.");
    if (novoModeloPermissaoId <= 0)
        throw new BusinessException("Modelo de permissão é obrigatório.");
    if (convidadoPorUsuarioId == Guid.Empty)
        throw new BusinessException("Usuário que convida é obrigatório.");

    Status = VinculoStatus.Convidado;
    ModeloPermissaoId = novoModeloPermissaoId;
    ConvidadoPorUsuarioId = convidadoPorUsuarioId;
    ConvidadoEm = DateTime.UtcNow;
    AceitoEm = null;
    InativadoEm = null;

    AddDomainEvent(new ProfissionalConvidadoEvent(
        Id, ProfissionalUsuarioId, EstabelecimentoId, ConvidadoPorUsuarioId));
}
```

3. `ConvidarProfissionalCommandHandler.cs`: substituir bloco `vinculoExistente`:
```csharp
var existente = await _vinculoRepo.ObterPorProfissionalEEstabelecimentoOuNulo(
    command.ProfissionalUsuarioId, command.EstabelecimentoId);

if (existente is { Status: VinculoStatus.Ativo or VinculoStatus.Convidado })
    throw new BusinessException("Este profissional já tem um vínculo ativo ou convite pendente para este estabelecimento.");

VinculoProfissionalEstabelecimento vinculo;
if (existente is { Status: VinculoStatus.Inativo })
{
    existente.ReativarComoConvite(modeloId, command.ConvidadoPorUsuarioId);
    vinculo = existente;
    await _vinculoRepo.Salvar(vinculo);
}
else
{
    vinculo = VinculoProfissionalEstabelecimento.Convidar(
        command.ProfissionalUsuarioId, command.EstabelecimentoId, modeloId, command.ConvidadoPorUsuarioId);
    await _vinculoRepo.Salvar(vinculo);
    vinculo.MarcarComoConvidado();
}

foreach (var evt in vinculo.DomainEvents)
    await _eventBus.Publish(evt);

vinculo.ClearDomainEvents();
```

#### Aceite (DoD)

- [ ] Teste unitário: convidar profissional já inativo recoloca em status `Convidado` com novo modelo de permissão.
- [ ] Teste unitário: convidar profissional ativo lança `BusinessException`.
- [ ] Teste unitário: convidar profissional convidado (pendente) lança `BusinessException`.
- [ ] Teste unitário: histórico de aceitação anterior é zerado (`AceitoEm = null`).
- [ ] `dotnet build` limpo.

---

### 1.4 Auditoria de tentativas de delete + proteção histórica

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer` (domain) + `database-architect` (schema/migration)
**Branch sugerido:** `feature/fase1-audit-delete-soft-delete`

#### Por quê

Legado tem triggers `audit_delete_attempts`, `protect_movement_history`, `protect_medical_records`. Hoje, novo permite delete livre de prontuário e movimento de estoque — risco LGPD + integridade contábil.

#### Diff técnico

**Backend — SharedKernel:**

1. `Imedto.Backend.SharedKernel/Domain/ISoftDeletable.cs`:
```csharp
public interface ISoftDeletable
{
    DateTime? DeletadoEm { get; }
    Guid? DeletadoPorUsuarioId { get; }
    void MarcarComoDeletado(Guid usuarioId);
}
```

**Backend — Domain:**

2. Implementar `ISoftDeletable` em: `Prontuario`, `EvolucaoProntuario`, `ProntuarioAnexo`, `MovimentacaoEstoque`, `Paciente`. Cada um:
```csharp
public virtual DateTime? DeletadoEm { get; protected set; }
public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

public virtual void MarcarComoDeletado(Guid usuarioId)
{
    if (usuarioId == Guid.Empty)
        throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
    if (DeletadoEm is not null)
        throw new BusinessException("Registro já está deletado.");
    DeletadoEm = DateTime.UtcNow;
    DeletadoPorUsuarioId = usuarioId;
}
```

3. Criar aggregate `Imedto.Backend.Domain/Auditoria/AuditDeleteAttempt.cs`:
```csharp
public class AuditDeleteAttempt : Entity
{
    public virtual string Tabela { get; protected set; } = string.Empty;
    public virtual string RegistroId { get; protected set; } = string.Empty;
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual Guid? UsuarioId { get; protected set; }
    public virtual string? Motivo { get; protected set; }
    public virtual DateTime TentadoEm { get; protected set; }

    protected AuditDeleteAttempt() { }

    public static AuditDeleteAttempt Registrar(
        string tabela, string registroId, long? estabelecimentoId, Guid? usuarioId, string? motivo)
    {
        return new AuditDeleteAttempt
        {
            Tabela = tabela,
            RegistroId = registroId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            Motivo = motivo,
            TentadoEm = DateTime.UtcNow
        };
    }
}
```

4. `IAuditDeleteAttemptRepository.cs`:
```csharp
public interface IAuditDeleteAttemptRepository
{
    Task Salvar(AuditDeleteAttempt registro);
}
```

**Backend — Infrastructure:**

5. `AuditDeleteAttemptConfiguration.cs` (EF). Mapeia tabela `audit_delete_attempts`.
6. `AuditDeleteAttemptRepository.cs` (escrita simples via EF).
7. `AppDbContext.cs`: adicionar `DbSet<AuditDeleteAttempt> AuditDeleteAttempts`.
8. Registrar repositório em `Container.cs`.
9. Atualizar EF configurations dos 5 aggregates marcados como `ISoftDeletable` para mapear as duas colunas novas.

**Backend — Application (commands de delete):**

10. Para cada handler de delete existente (revisar grep `: ICommandHandler<.*Excluir`/`Deletar`/`Cancelar`):
    - Se o aggregate é `ISoftDeletable`: trocar `repo.Excluir(id)` por `aggregate.MarcarComoDeletado(usuarioId); await repo.Salvar(aggregate);`.
    - Se for cancelamento (Agendamento), manter como está (não é delete físico).

11. Repositórios de leitura Dapper: adicionar `WHERE deletado_em IS NULL` em todas as queries dos 5 aggregates.

12. **Bloqueio de hard delete**: adicionar `SaveChangesInterceptor` ou similar no EF que detecta `EntityState.Deleted` em entidades `ISoftDeletable`, registra `AuditDeleteAttempt` e lança `BusinessException("Não é permitido excluir registros desta entidade. Use a operação de exclusão lógica.")`.

13. Para tentativas de delete em prontuário/evolução/anexo (mesmo lógico) — deve ser permitido apenas pelo dono do prontuário. Validar no handler.

**Database (após backend):**

14. Migration EF + supabase SQL — schema definido na seção "Schema fechado" acima.

#### Aceite (DoD)

- [ ] Soft delete funcional em `Prontuario`, `EvolucaoProntuario`, `ProntuarioAnexo`, `MovimentacaoEstoque`, `Paciente`.
- [ ] Listagens não retornam registros com `deletado_em IS NOT NULL`.
- [ ] Tentativa de hard delete via EF lança `BusinessException` e registra linha em `audit_delete_attempts`.
- [ ] Migration EF + supabase SQL idênticas (mesmo timestamp).
- [ ] `dotnet build` limpo.

---

### 1.5 Seed de categorias financeiras + formas de pagamento padrão

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch sugerido:** `feature/fase1-seed-financeiro`

#### Pré-condição (verificar antes)

Conferir se existem aggregates `CategoriaFinanceira` e `FormaPagamento` em `Domain/Financeiro/`. Se NÃO existirem:
- Documentar como bloqueio aqui no `.md`.
- Criar os aggregates + EF config + migration ANTES de implementar o seed (parte do escopo do item).

#### Por quê

Legado tem `criar_categorias_financeiras_padrao` e `criar_formas_pagamento_padrao` chamados ao criar estabelecimento. Hoje, estabelecimento novo nasce sem categorias/formas — `FinanceiroView` quebra.

#### Diff técnico

1. Verificar/criar aggregates `CategoriaFinanceira` (Id, EstabelecimentoId, Nome, Tipo: Receita|Despesa, Padrao: bool, Ativo) e `FormaPagamento` (Id, EstabelecimentoId, Nome, Padrao, Ativo) — se não existirem.

2. Criar `Application/Estabelecimentos/Events/CriarSeedFinanceiroAoCriarEstabelecimentoHandler.cs`:
```csharp
public class CriarSeedFinanceiroAoCriarEstabelecimentoHandler
    : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly ICategoriaFinanceiraRepository _categoriaRepo;
    private readonly IFormaPagamentoRepository _formaRepo;

    public CriarSeedFinanceiroAoCriarEstabelecimentoHandler(
        ICategoriaFinanceiraRepository categoriaRepo,
        IFormaPagamentoRepository formaRepo) { ... }

    public async Task Handle(EstabelecimentoCriadoEvent evt)
    {
        foreach (var (nome, tipo) in SeedsFinanceiro.Categorias)
            await _categoriaRepo.Salvar(CategoriaFinanceira.CriarPadrao(evt.EstabelecimentoId, nome, tipo));

        foreach (var nome in SeedsFinanceiro.FormasPagamento)
            await _formaRepo.Salvar(FormaPagamento.CriarPadrao(evt.EstabelecimentoId, nome));
    }
}
```

3. `Application/Financeiro/SeedsFinanceiro.cs` (classe estática com listas):
```csharp
public static class SeedsFinanceiro
{
    public static readonly IReadOnlyList<(string Nome, TipoCategoria Tipo)> Categorias = new[]
    {
        ("Receita: Consulta",    TipoCategoria.Receita),
        ("Receita: Procedimento",TipoCategoria.Receita),
        ("Receita: Outros",      TipoCategoria.Receita),
        ("Despesa: Folha",       TipoCategoria.Despesa),
        ("Despesa: Aluguel",     TipoCategoria.Despesa),
        ("Despesa: Insumos",     TipoCategoria.Despesa),
        ("Despesa: Outros",      TipoCategoria.Despesa),
    };

    public static readonly IReadOnlyList<string> FormasPagamento = new[]
    {
        "Dinheiro", "PIX", "Cartão de Crédito", "Cartão de Débito", "Transferência", "Boleto"
    };
}
```

4. Registrar handler em `Container.RegistrarHandlers` (scoped) e em `Container.RegistrarBuses` no `IEventBus`.

#### Aceite (DoD)

- [ ] Criar estabelecimento → 7 categorias e 6 formas de pagamento populadas para o estabelecimento.
- [ ] Listagem de categorias e formas no front mostra os itens recém-seedados.
- [ ] Teste unitário do handler (com Moq dos repositórios).
- [ ] `dotnet build` limpo.

---

### 1.6 Rate limit em `/auth/login` e `/auth/refresh`

**Status:** ⏳ pendente
**Agente:** `security-engineer`
**Branch sugerido:** `feature/fase1-rate-limit-auth`

#### Por quê

BFF expõe `/api/auth/login`, `/api/auth/refresh`, `/api/auth/signup`, `/api/auth/forgot-password` diretamente. Sem trava é convite a brute force + envenenamento de cache de token.

#### Diff técnico

1. `Program.cs` — adicionar `builder.Services.AddRateLimiter(options => { ... })` com policies:
   - `auth-login`: sliding window de 5 / 60s por IP.
   - `auth-refresh`: sliding window de 10 / 60s por IP.
   - `auth-sensitive`: sliding window de 3 / 60s por IP (signup, forgot-password — operações lentas/caras).
   - Usar `httpContext.Connection.RemoteIpAddress` como partition key. Considerar cabeçalho `X-Forwarded-For` se atrás de proxy.

2. Aplicar `app.UseRateLimiter()` no pipeline.

3. `AuthController.cs`: aplicar `[EnableRateLimiting("auth-login")]` em `Login`, `[EnableRateLimiting("auth-refresh")]` em `Refresh`, `[EnableRateLimiting("auth-sensitive")]` em `Signup` e `ForgotPassword`.

4. Configurar resposta 429:
   - Header `Retry-After` populado.
   - Body genérico: `{ "mensagem": "Muitas tentativas. Tente novamente em alguns instantes." }`.
   - **Não** incluir email/IP/contagem na resposta.

5. Logger estruturado no rejeição: nível `Warning`, sem PII (apenas IP truncado ou hash).

6. Considerar registrar em `audit_delete_attempts` ou similar tabela de auditoria? **Não** — escopo é limit. Logger é suficiente. (Marcar como decisão.)

#### Aceite (DoD)

- [ ] 6ª tentativa de login no mesmo minuto (mesmo IP) → 429.
- [ ] Header `Retry-After` populado.
- [ ] Log estruturado **sem** vazar email/senha em caso de bloqueio.
- [ ] Teste manual via `curl` em loop ou xUnit com TestServer.
- [ ] `dotnet build` limpo.

---

### 1.7 Rate limit + audit + cache no `IIaService`

**Status:** ⏳ pendente
**Agente:** `security-engineer` + `database-architect` (schema)
**Branch sugerido:** `feature/fase1-ia-rate-cache-audit`

#### Por quê

[AnthropicIaService.cs](../backend/src/Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs) está exposto sem proteção. Risco de custo descontrolado + LGPD (input clínico vai para LLM externo sem audit). Legado tinha `check_ai_rate_limit`, `is_ai_enabled`, `log_ai_request`, `cleanup_expired_ai_cache`, `has_assistente_clinico_permission`.

#### Diff técnico

1. Tabelas (definidas no Schema fechado): `ai_audit_logs`, `ai_outputs_cache`, `ai_rate_limits`. Migrations.

2. `Domain/Ia/`:
   - `IAiAuditRepository.cs`: `Task RegistrarAsync(AiAuditLog log);`
   - `IAiCacheRepository.cs`: `Task<string?> ObterAsync(string promptHash);`, `Task SalvarAsync(...);`, `Task RemoverExpiradosAsync();`
   - `IAiRateLimitRepository.cs`: `Task<bool> RegistrarTentativaAsync(Guid usuarioId, int limitePorMinuto);`
   - Entidade `AiAuditLog` simples.

3. `Infrastructure/Ia/RateLimitedIaService.cs` — decorator de `IIaService`:
```csharp
public class RateLimitedIaService : IIaService
{
    private readonly IIaService _inner;
    private readonly IAiAuditRepository _audit;
    private readonly IAiCacheRepository _cache;
    private readonly IAiRateLimitRepository _rate;
    private readonly IUsuarioContexto _ctx;     // já existe? confirmar
    private readonly IOptions<IaOptions> _opts; // limites configuráveis

    public async IAsyncEnumerable<string> SugerirSecaoProntuarioAsync(
        SugestaoSecaoProntuarioRequest req,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var usuarioId = _ctx.UsuarioId;
        var estabId   = _ctx.EstabelecimentoId;

        // 1) Rate limit
        var permitido = await _rate.RegistrarTentativaAsync(usuarioId, _opts.Value.LimitePorMinuto);
        if (!permitido)
            throw new BusinessException("Limite de uso da IA atingido. Aguarde 1 minuto.");

        // 2) Cache
        var promptHash = Hashing.Sha256(JsonSerializer.Serialize(req));
        var cached = await _cache.ObterAsync(promptHash);
        if (cached is not null)
        {
            yield return cached;   // streaming "fake" de uma vez
            yield break;
        }

        // 3) Chama inner + acumula resposta
        var sb = new StringBuilder();
        var inicio = DateTime.UtcNow;
        Exception? erro = null;
        try
        {
            await foreach (var chunk in _inner.SugerirSecaoProntuarioAsync(req, ct))
            {
                sb.Append(chunk);
                yield return chunk;
            }
        }
        catch (Exception ex) { erro = ex; throw; }
        finally
        {
            var resposta = sb.ToString();
            var duracao = (int)(DateTime.UtcNow - inicio).TotalMilliseconds;

            await _audit.RegistrarAsync(new AiAuditLog
            {
                UsuarioId = usuarioId,
                EstabelecimentoId = estabId,
                PromptHash = promptHash,
                ResponseHash = erro is null ? Hashing.Sha256(resposta) : null,
                Modelo = "claude-...",
                Endpoint = "sugestao-secao",
                DuracaoMs = duracao,
                Sucesso = erro is null,
                ErroMensagem = erro?.Message?.Substring(0, Math.Min(500, erro.Message.Length))
            });

            if (erro is null && !string.IsNullOrWhiteSpace(resposta))
            {
                await _cache.SalvarAsync(promptHash, estabId, "sugestao-secao", resposta,
                    expiraEm: DateTime.UtcNow.AddHours(_opts.Value.CacheTtlHoras));
            }
        }
    }
}
```

4. Registro DI em `Container.cs`:
```csharp
services.AddScoped<AnthropicIaService>();
services.AddScoped<IIaService>(sp => new RateLimitedIaService(
    sp.GetRequiredService<AnthropicIaService>(), ...));
```

5. `IaOptions` em `appsettings`:
```json
"Ia": { "LimitePorMinuto": 10, "CacheTtlHoras": 24 }
```

6. **Não** logar prompt cru — apenas hash. `ai_audit_logs` é fonte de auditoria sem PII.

7. Job de limpeza de cache expirado — pode ser placeholder agora (Fase 2 implementa scheduler real) — anotar TODO.

8. **Permissão**: validar que o usuário tem permissão `assistente_clinico` no modelo de permissão antes de chamar IA. Se não tiver, 403. Adicionar método em `IModeloPermissaoRepository.UsuarioTemPermissaoIa(Guid usuarioId, long estabelecimentoId)`.

#### Aceite (DoD)

- [ ] 11ª chamada IA por usuário em 1 minuto → 429 (ou `BusinessException` 422 — definir).
- [ ] Mesmo prompt em janela de cache retorna sem chamar Anthropic (audit log marca como cache-hit? — opcional; se sim, tipo `endpoint = "sugestao-secao-cache"`).
- [ ] `ai_audit_logs` recebe linha por chamada.
- [ ] Audit log NÃO contém prompt cru — apenas hash.
- [ ] Usuário sem permissão `assistente_clinico` recebe 403.
- [ ] Migration EF + supabase SQL idênticas.
- [ ] `dotnet build` limpo.

---

## Branches e commits

Cada item pode virar branch ou commit separado. Padrão de branch: `feature/fase1-<slug>`.

Padrão de commit message:
```
feat(<area>): <descrição curta>

<detalhe técnico relevante>

Refs: Docs/01_FASE_1_HARDENING.md (item 1.X)
```

## Status por item

| Item | Status | Observações |
|------|--------|-------------|
| 1.1 Custo médio ponderado em estoque | ✅ | Backend + EF + Front + migration. |
| 1.2 Overlap de agenda na atualização | ✅ | Handler ajustado, repo já tinha o parâmetro. |
| 1.3 Reativação de vínculo Inativo | ✅ | Aggregate + handler + repo novo. |
| 1.4 Auditoria de delete + soft delete | ✅ | 5 aggregates `ISoftDeletable` + interceptor + audit table. |
| 1.5 Seed financeiro | ⛔ BLOQUEADO | Aggregates `CategoriaFinanceira` e `FormaPagamento` não existem. Reagendado para sub-item dedicado em Fase 2 (precede o seed: criar aggregates + EF + migration + repo + endpoints). |
| 1.6 Rate limit em /auth | ✅ | 3 policies, hash de IP em log, 429 sem PII. |
| 1.7 Rate limit + audit + cache + sanitização PII no IIaService | ✅ | Decorator com rate, cache, audit (hash), sanitização PII (CPF/CNPJ/tel/email/CEP/RG) e trava de vínculo ativo. |

## Resumo final da fase

### O que foi entregue

**Backend:**
- `ItemInventario`, `MovimentacaoEstoque` com custo médio ponderado + snapshot.
- `AtualizarAgendamentoCommandHandler` com check de overlap.
- `Vinculo.ReativarComoConvite` + handler `Convidar` adaptado.
- `ISoftDeletable` em `Prontuario`, `EvolucaoProntuario`, `ProntuarioAnexo`, `MovimentacaoEstoque`, `Paciente`.
- Aggregate `AuditDeleteAttempt` + `SoftDeleteInterceptor` (registro em conexão independente para sobreviver a rollback).
- Rate limit em `/auth/login` (5/60s), `/auth/refresh` (10/60s), `/auth/sensitive` (3/60s).
- `RateLimitedIaService` decorator: rate limit, cache, audit (apenas hash SHA256), sanitização PII e trava de autorização por vínculo.
- `PiiSanitizer` (CPF, CNPJ, telefone BR, e-mail, CEP, RG).

**Database:**
- Migration `20260429023829_HardeningFase1Schema` (par EF + supabase SQL): colunas `custo_medio`, `custo_unitario`, `custo_total`, `deletado_em`, `deletado_por_usuario_id`, `audit_delete_attempts`, `ai_audit_logs`.
- Migration SQL puro `20260429023830_criar_ai_cache_e_rate_limit.sql` (sem entidade EF — usado por Dapper).

**Frontend:**
- `formatarMoedaBrl` em `utils/format.ts`.
- `InventarioView`: colunas custo médio/unitário/total, campo de custo na entrada, mensagem auxiliar na saída.
- `inventarioService` tipado com novos campos.

**Testes:** +48 testes. Total 98 verdes.

### Pendências documentadas (não-bloqueadores)

1. **1.4** — `Profissional` como `ISoftDeletable`. Legado tinha campo `ativo` em `profissionais`. Decisão: postergar para Fase 2 com migration adicional.
2. **1.7** — Permissão fina `assistente_clinico` (modelo de permissão). Hoje a trava é "vínculo ativo" — defesa parcial. Refinamento na Fase 3 quando o ModeloPermissao for migrado plenamente.
3. **1.7** — Tabela `establishment_ai_settings` (toggle por estabelecimento, modelo, limite diário). Postergado para Fase 2.
4. **1.7** — FKs em `ai_audit_logs` para paciente/prontuário/evolução. Postergado para Fase 2.
5. **1.7** — Rate limit particionado por (usuário, estabelecimento). Hoje só por usuário. Postergado para Fase 2.
6. **1.2** — Constraint `EXCLUDE USING gist` no Postgres como defense-in-depth do overlap. Postergado para Fase 2.
7. **1.4** — `SaveChangesInterceptor` é difícil de testar em unidade isolada. Cobrir com teste de integração (`InMemoryDatabase`) em sprint dedicada.
8. **1.6** — Validação real de rate limit em load test pendente (load test com `WebApplicationFactory`).
9. **1.4** — Documentar como `admin-reset-estabelecimento` (legado) será replicado na Fase 2 sem ser bloqueado pelo `SoftDeleteInterceptor`.

### Mudanças semânticas em relação ao legado (intencionais, documentadas)

- **Custo de estoque**: legado usava modelo de **lotes** com lookup do "último lote". Novo usa **custo médio ponderado**. Se rastreabilidade por lote (Anvisa) virar requisito, abrir item dedicado.
- **Reativação de vínculo**: legado fazia `ON CONFLICT DO UPDATE → Ativo direto`. Novo exige novo aceite (`Convidado` → `Ativo`). Decisão pró-LGPD (re-consentimento explícito).
- **Audit de delete**: novo registra audit em conexão independente; legado não tinha esse padrão. Tabela `audit_delete_attempts` tinha sido `DROP`-ada no legado mais recente — novo a re-introduz.

### Build & testes finais

- `dotnet build Imedto.Backend.sln`: **0 errors**, 4 warnings (vulnerabilidade transitiva `System.Security.Cryptography.Xml 9.0.0`, fora do escopo).
- `dotnet test Tests/Imedto.Backend.Test`: **Passed: 98, Failed: 0, Skipped: 0**.
- `npm run build` (frontend): **OK**, sem type errors.

### Próximos passos

1. Aplicar as migrations no banco:
```bash
cd /Users/joao/Documents/GitHub/ImedtoRefatoracao
SUPABASE_ACCESS_TOKEN=<PAT> SUPABASE_DB_PASSWORD=<senha> supabase db push
```
2. Atualizar **Status** no [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md) marcando Fase 1 como concluída.
3. Gerar `02_FASE_2_PLATAFORMA.md` quando for iniciar Fase 2 (incorporando os 9 itens pendentes desta fase no escopo).
