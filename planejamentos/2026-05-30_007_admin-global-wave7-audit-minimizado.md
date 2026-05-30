# Admin Global — Wave 7: minimizar audit log (corte + retenção)

**ID**: 2026-05-30_007
**Status**: Aprovado por usuário em 2026-05-30
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: admin-global (auth admin, abertura de detalhe de tenant), jobs recorrentes, LGPD/audit.

**Refere-se a**: Wave 1 (`2026-05-30_001`), Wave 2 (`2026-05-30_002`), Wave 3 (`2026-05-30_003`), Wave 4 (`2026-05-30_004`), Wave 5 (`2026-05-30_005`), Wave 6 (`2026-05-30_006`). Esta é a sétima onda do programa Admin Global — foco em **higiene operacional**, não em features novas.

---

## 1. Contexto e motivação

A Wave 1 introduziu `ImedtoAdminAuditWriter` e a tabela `imedto_admin_audit_log` para registrar ações administrativas (LGPD + forense). Sete dias depois, em `dev`, a tabela acumulou **202 linhas** com a seguinte distribuição:

| Ação | Linhas | % | Valor forense |
|---|---|---|---|
| `LOGIN_OK` | 131 | 65% | Baixo — gera ruído contínuo a cada acesso bem-sucedido |
| `LOGIN_FAIL` | 19 | 9% | **Alto** — tentativa de invasão, fraude |
| `ABRIR_DETALHE_TENANT` | 16 | 8% | Baixo — só revela navegação, sem mutação |
| `LOGOUT` | 11 | 5% | **Zero** — não há cenário forense que dependa dele |
| `REVELAR_CPF_DONO` | 3 | 1% | **Alto** — reveal de PII (LGPD Art. 11) |
| Mutações (`CRIAR_PLANO`, `DESATIVAR_PLANO`, `RESETAR_TENANT`, etc.) | 22 | 11% | **Alto** — LGPD/contrato/financeiro |

**~78% das linhas são ruído** (`LOGIN_OK` + `LOGOUT` + `ABRIR_DETALHE_TENANT`). Se a área Admin entrar em uso real por 5 admins ativos, a tabela cresce de forma linear com o número de logins (3-5×/dia/admin) — projeção de **~5k linhas/mês só de ruído**. Em 1 ano, ~60k linhas que não respondem a nenhuma pergunta forense útil. Custo de storage no RDS é baixo, mas o ruído **degrada o sinal** quando alguém precisar investigar um incidente real (busca por `LOGIN_FAIL` perto de mutações suspeitas fica enterrada).

### Feedback literal do usuário (2026-05-30)

> "minimize a quantidade de registros de historico que sao salvos, caso contrario o banco de dados vai ficar cheio rapido com informação desnecessaria no momento"

E em resposta à proposta consolidada (cortar no código + limpar retroativo no banco):

> "sim faça o remomendado, reduza ao maximo direto no codigo e no banco tanbém"

Escolha = **A + C** (cortar no código + cleanup retroativo + retenção via job).

## 2. Objetivo

**Reduzir audit do admin global ao essencial forense/LGPD: parar de gravar ações de baixo valor (login bem-sucedido, logout, abrir detalhe) e instaurar política de retenção automática para as ações restantes — sem perder o histórico mínimo legal das mutações.**

## 3. Escopo

### Inclui (4 frentes)

**Frente 1 — Cortar audit das 3 ações de ruído (código).**
- Remover `RegistrarAsync` para `AcoesAuditAdmin.LoginOk` em `AdminAuthController.Login` (linha 104).
- Remover `RegistrarAsync` para `AcoesAuditAdmin.Logout` em `AdminAuthController.Logout` (linha 193).
- Remover `RegistrarLeituraAsync` para `AcoesAuditAdmin.AbrirDetalheTenant` em `ObterEstabelecimentoAdminQueryHandler` (linha 37).
- **Manter** `LoginFail` (linha 82) — valor forense direto.
- **Manter** `RevelarCpfDono`, `ResetarTenant`, `ResetarSenhaAdmin` e todas as demais mutações intactas.

**Frente 2 — Política de retenção via job recorrente (código).**
- Criar `Imedto.Backend.Domain.Admin.AuditLogRetencao` (classe estática) com mapa `<acao, dias>` cobrindo todas as 31 constantes de `AcoesAuditAdmin` + default fallback de 365 dias.
- Criar `Imedto.Backend.Infrastructure.Jobs.Handlers.LimparAuditAdminJob : IJobHandler` com `Nome = "limpar-audit-admin"`.
- Registrar handler como Scoped no `Container` (mesmo padrão de `LimparAuditAntigoJob`).
- Adicionar entrada em `JobsRegistrados.Todos` com intervalo `24 * 60 * 60` (1×/dia, alinhado com o padrão dos outros jobs diários do sistema).
- DELETE em batches de até 10.000 linhas por ação (loop `while` até zerar) para evitar lock pesado em produção.
- Log estruturado por execução: por ação removida → quantidade + duração.

**Frente 3 — Cleanup retroativo (migration SQL).**
- Nova migration idempotente: `db/migrations/20260530210000_limpar_audit_admin_retroativo.sql`.
- `DELETE FROM imedto_admin_audit_log WHERE acao IN ('LOGIN_OK', 'LOGOUT', 'ABRIR_DETALHE_TENANT')` (sem filtro de data — Frente 1 já garante que não vão mais entrar; limpa o backlog inteiro do ruído).
- Idempotente por construção: se rodar 2×, 2ª passagem deleta 0 linhas.
- Wrappar em transação simples (`BEGIN; DELETE...; COMMIT;`) — volume esperado em prod é baixo (~200 linhas).

**Frente 4 — Documentação viva.**
- Atualizar `Docs/ARQUITETURA.md`: adicionar seção curta "Política de retenção do audit admin" referenciando `AuditLogRetencao` como fonte de verdade do TTL por ação.

### Não inclui (Escopo OUT — explícito)

- **Dashboards de métricas do audit** (volume por ação, gráfico de retenção). Backlog futuro se necessário.
- **Export do audit** (CSV/PDF para compliance externo). Sob demanda, fora deste briefing.
- **Rotação para storage frio** (S3 Glacier antes do delete definitivo). Premissa atual: deletar é suficiente — não há requisito legal para conservar mutações Admin além do janelamento aqui definido.
- **Anonimização** de linhas antigas em vez de delete. Dado do audit já é mínimo (`admin_id`, `acao`, `payload_json`) e o `admin_id` é interno — não há PII de paciente.
- **Audit do próprio job de limpeza**. Filosofia: zero auto-referência. O job apenas loga via `ILogger`.
- **Cron real (HH:MM)**. O `JobScheduler` atual opera por intervalo em segundos, não cron expression. Manter o padrão existente.
- **Mudanças no frontend** (UI da Wave 6 que lista audit). O frontend continua exibindo o que existe; sumir o ruído é positivo para a UX dele.

## 4. Decisões cravadas (sem perguntar ao usuário)

| # | Decisão | Por quê |
|---|---|---|
| D1 | Job dedicado novo `LimparAuditAdminJob`, **não** estender `LimparAuditAntigoJob` | `LimparAuditAntigoJob` limpa `audit_delete_attempts` (tabela diferente). Misturar viola Single Responsibility e dificulta evolução de TTL independente. |
| D2 | Intervalo `24*60*60` (1×/dia) | Alinha com `limpar-audit-antigo` e `expirar-trials`. Scheduler do projeto não tem cron real — intervalo é o contrato. |
| D3 | Batches de 10k linhas via `Take(10000).ExecuteDeleteAsync` em loop | Postgres com índice em `criado_em` segura DELETE de 10k sem travar. Loop continua até `deletados == 0` na rodada. |
| D4 | Mapa `AuditLogRetencao` em `Domain.Admin` (estático) | Fonte de verdade legível, testável, sem migration para alterar TTL. Reuso da constante `AcoesAuditAdmin`. |
| D5 | Default fallback de 365 dias para ação não mapeada | Garante que ação nova introduzida sem atualizar o mapa não vira lixo eterno **nem** é deletada agressivamente. 365 é conservador. |
| D6 | Cleanup retroativo é DELETE INLINE na migration (sem `--CONCURRENTLY`, sem batch) | Volume atual em prod é ~200 linhas. Não precisa de cuidado especial. |
| D7 | Sem audit do próprio job | Auto-referência inútil. Logs estruturados via `ILogger` cobrem o forense da execução. |
| D8 | `DateTimeOffset.UtcNow` no job | Lição da Wave 6 — evitar drift de timezone do servidor. |
| D9 | Job **não** quebra deploy se nenhuma linha for elegível | DELETE de 0 linhas é sucesso. Log informa "0 removidas". |
| D10 | Testes unitários do `AuditLogRetencao` e integração do `LimparAuditAdminJob` | Garante que mapa cobre todas as constantes conhecidas e job respeita TTL. |

### Mapa de retenção (cravado)

| Ação | TTL | Racional |
|---|---|---|
| `LOGIN_OK` | 30 dias | Após Frente 1, nem entra mais. Job limpa backlog rapidamente. |
| `LOGOUT` | 30 dias | Idem. |
| `ABRIR_DETALHE_TENANT` | 30 dias | Idem. |
| `LOGIN_FAIL` | 365 dias | Forense de tentativa de invasão. 1 ano cobre auditorias anuais. |
| `RESET_SENHA_PROPRIA` | 365 dias | Segurança do próprio admin. |
| `REVELAR_CPF_DONO` | 730 dias (2 anos) | LGPD: revelação de PII deve ser rastreável por período prolongado. |
| `RESETAR_TENANT`, `CRIAR_PLANO`, `ATUALIZAR_PLANO`, `ATIVAR_PLANO`, `DESATIVAR_PLANO`, `EDITAR_PLANO`, `TROCAR_PLANO`, `ALTERAR_ASSINATURA`, `CONCEDER_GRATUIDADE`, `ENCERRAR_ASSINATURA`, `RESETAR_SENHA_ADMIN`, `CRIAR_ADMIN`, `DESATIVAR_ADMIN`, `REATIVAR_ADMIN`, `ATUALIZAR_CONFIG` | 730 dias (2 anos) | Mutação de plano/assinatura/admin tem implicação contratual e financeira. 2 anos cobre prazo prescricional padrão. |
| `CRIAR_MODELO_PADRAO_SISTEMA`, `ATUALIZAR_MODELO_PADRAO_SISTEMA`, `INATIVAR_MODELO_PADRAO_SISTEMA`, `REATIVAR_MODELO_PADRAO_SISTEMA`, `CRIAR_VARIAVEL_PADRAO_SISTEMA`, `ATUALIZAR_VARIAVEL_PADRAO_SISTEMA`, `INATIVAR_VARIAVEL_PADRAO_SISTEMA`, `REATIVAR_VARIAVEL_PADRAO_SISTEMA`, `CRIAR_REGIAO_ANATOMICA`, `ATUALIZAR_REGIAO_ANATOMICA`, `INATIVAR_REGIAO_ANATOMICA`, `EXCLUIR_REGIAO_ANATOMICA` | 365 dias | Mutação de catálogo padrão-sistema. 1 ano suficiente — mudança é raramente disputada e o estado atual fica no próprio catálogo. |
| **Default (ação nova não mapeada)** | 365 dias | Conservador. PR que introduzir ação nova deve atualizar o mapa explicitamente. |

## 5. Arquitetura proposta (alto nível por frente)

### Frente 1 — Cortes pontuais no código (3 arquivos)

| Arquivo | Linha atual | Mudança |
|---|---|---|
| `backend/src/Services/Imedto.Backend.API/Controllers/Admin/AdminAuthController.cs` | ~104 | Remover bloco `await _audit.RegistrarAsync(AcoesAuditAdmin.LoginOk, ...)`. Manter LoginFail intacto na linha 82. |
| `backend/src/Services/Imedto.Backend.API/Controllers/Admin/AdminAuthController.cs` | ~193 | Remover bloco `await _audit.RegistrarAsync(AcoesAuditAdmin.Logout, ...)`. |
| `backend/src/Services/Imedto.Backend.Application/Admin/Estabelecimentos/Queries/ObterEstabelecimentoAdminQueryHandler.cs` | ~37 | Remover bloco `await _audit.RegistrarLeituraAsync(AcoesAuditAdmin.AbrirDetalheTenant, ...)`. Se o construtor injeta `IImedtoAdminAuditWriter` só para essa chamada, **remover a dependência** também. Caso contrário, manter (reuso). |

Cortes alinhados ao princípio "Surgical Changes" do `CLAUDE.md`: não tocar em mais nada nesses arquivos.

**Importante**: as constantes `AcoesAuditAdmin.LoginOk`, `AcoesAuditAdmin.Logout` e `AcoesAuditAdmin.AbrirDetalheTenant` **permanecem definidas** em `ImedtoAdminAuditLog.cs`. Razões: (1) testes existentes em `ImedtoAdminAuditWriterTests.cs` referenciam `LoginOk` para validar o writer genericamente; (2) o mapa de retenção (Frente 2) ainda precisa do nome para limpar backlog histórico; (3) ação pode voltar no futuro — não invalidar a constante.

### Frente 2 — Job + mapa de retenção

**`backend/src/Services/Imedto.Backend.Domain/Admin/AuditLogRetencao.cs`** (novo arquivo)

```csharp
namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Fonte de verdade da política de retenção do audit admin.
/// Mapeia cada ação de <see cref="AcoesAuditAdmin"/> para um TTL em dias.
/// Ação não mapeada cai no <see cref="DefaultDias"/> (conservador).
/// </summary>
public static class AuditLogRetencao
{
    public const int DefaultDias = 365;

    public static readonly IReadOnlyDictionary<string, int> PorAcao = new Dictionary<string, int>
    {
        // Ruído residual — backlog será limpo, depois mantemos janela curta caso algo volte.
        [AcoesAuditAdmin.LoginOk] = 30,
        [AcoesAuditAdmin.Logout] = 30,
        [AcoesAuditAdmin.AbrirDetalheTenant] = 30,

        // Forense de segurança — janela de 1 ano.
        [AcoesAuditAdmin.LoginFail] = 365,
        [AcoesAuditAdmin.ResetSenhaPropria] = 365,

        // LGPD — revelação de PII tem janela longa.
        [AcoesAuditAdmin.RevelarCpfDono] = 730,

        // Mutações contratuais/financeiras — 2 anos.
        [AcoesAuditAdmin.ResetarTenant] = 730,
        [AcoesAuditAdmin.CriarPlano] = 730,
        [AcoesAuditAdmin.AtualizarPlano] = 730,
        [AcoesAuditAdmin.AtivarPlano] = 730,
        [AcoesAuditAdmin.DesativarPlano] = 730,
        [AcoesAuditAdmin.EditarPlano] = 730,
        [AcoesAuditAdmin.TrocarPlano] = 730,
        [AcoesAuditAdmin.AlterarAssinatura] = 730,
        [AcoesAuditAdmin.ConcederGratuidade] = 730,
        [AcoesAuditAdmin.EncerrarAssinatura] = 730,
        [AcoesAuditAdmin.ResetarSenhaAdmin] = 730,
        [AcoesAuditAdmin.CriarAdmin] = 730,
        [AcoesAuditAdmin.DesativarAdmin] = 730,
        [AcoesAuditAdmin.ReativarAdmin] = 730,
        [AcoesAuditAdmin.AtualizarConfig] = 730,

        // Catálogos padrão-sistema — 1 ano.
        [AcoesAuditAdmin.CriarModeloPadraoSistema] = 365,
        [AcoesAuditAdmin.AtualizarModeloPadraoSistema] = 365,
        [AcoesAuditAdmin.InativarModeloPadraoSistema] = 365,
        [AcoesAuditAdmin.ReativarModeloPadraoSistema] = 365,
        [AcoesAuditAdmin.CriarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.AtualizarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.InativarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.ReativarVariavelPadraoSistema] = 365,
        [AcoesAuditAdmin.CriarRegiaoAnatomica] = 365,
        [AcoesAuditAdmin.AtualizarRegiaoAnatomica] = 365,
        [AcoesAuditAdmin.InativarRegiaoAnatomica] = 365,
        [AcoesAuditAdmin.ExcluirRegiaoAnatomica] = 365,
    };

    public static int TtlDiasParaAcao(string acao)
        => PorAcao.TryGetValue(acao, out var dias) ? dias : DefaultDias;
}
```

**`backend/src/Services/Imedto.Backend.Infrastructure/Jobs/Handlers/LimparAuditAdminJob.cs`** (novo arquivo)

Esqueleto sugerido (dev tem liberdade de ajustar — desde que respeite os CAs):

```csharp
public class LimparAuditAdminJob : IJobHandler
{
    public string Nome => "limpar-audit-admin";
    private const int BatchSize = 10_000;

    private readonly AppDbContext _db;
    private readonly ILogger<LimparAuditAdminJob> _logger;

    public LimparAuditAdminJob(AppDbContext db, ILogger<LimparAuditAdminJob> logger) { ... }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var agora = DateTimeOffset.UtcNow;
        var totalRemovidoNaRodada = 0;

        // 1. Para cada ação no mapa, calcula corte e deleta em batches.
        foreach (var (acao, dias) in AuditLogRetencao.PorAcao)
        {
            var corte = agora.AddDays(-dias).UtcDateTime;
            var removidosDaAcao = 0;
            int batch;
            do
            {
                batch = await _db.ImedtoAdminAuditLog
                    .Where(l => l.Acao == acao && l.CriadoEm < corte)
                    .Take(BatchSize)
                    .ExecuteDeleteAsync(ct);
                removidosDaAcao += batch;
            } while (batch == BatchSize && !ct.IsCancellationRequested);

            if (removidosDaAcao > 0)
                _logger.LogInformation("[Job:{Nome}] Ação {Acao}: removidas {Total} linhas (TTL {Dias}d).",
                    Nome, acao, removidosDaAcao, dias);
            totalRemovidoNaRodada += removidosDaAcao;
        }

        // 2. Default fallback: ações não mapeadas → TTL DefaultDias.
        // Implementar via subquery NOT IN (...mapeadas...) AND criado_em < agora - default.

        _logger.LogInformation("[Job:{Nome}] Rodada concluída — total removido: {Total}.", Nome, totalRemovidoNaRodada);
    }
}
```

Registro DI: adicionar em `Container.cs` (ou onde os outros jobs estão sendo registrados — dev confere o padrão de `LimparAuditAntigoJob`).

Registro `JobsRegistrados`: nova entrada `new("limpar-audit-admin", IntervaloSeg: 24 * 60 * 60),`.

### Frente 3 — Migration de cleanup retroativo

**`db/migrations/20260530210000_limpar_audit_admin_retroativo.sql`** (novo arquivo; ID exato confirmado pelo `imedto-database` no momento da geração — sequência relativa a `20260530200000_drop_catalogos_globais_wave2.sql`):

```sql
-- Wave 7 — Cleanup retroativo do audit admin.
-- Remove backlog das 3 ações de ruído (LOGIN_OK, LOGOUT, ABRIR_DETALHE_TENANT)
-- que foram cortadas no código nesta mesma entrega. Idempotente.

BEGIN;

DELETE FROM imedto_admin_audit_log
WHERE acao IN ('LOGIN_OK', 'LOGOUT', 'ABRIR_DETALHE_TENANT');

COMMIT;
```

**Importante**: NÃO aplica retenção de outras ações nesta migration. A retenção contínua é responsabilidade do `LimparAuditAdminJob` (Frente 2). A migration limpa **apenas** o ruído backlog que está sendo cortado no código — operação cirúrgica.

### Frente 4 — Documentação

Atualizar `Docs/ARQUITETURA.md` adicionando seção curta (≤15 linhas) "Audit admin global — política de retenção" perto da seção de jobs ou logging. Conteúdo:

- Aponta para `AuditLogRetencao` como fonte de verdade.
- Cita o job `limpar-audit-admin` (1×/dia, batches de 10k).
- Esclarece que login bem-sucedido / logout / abrir-detalhe **não são** registrados (decisão da Wave 7 — alto ruído, baixo valor).
- Aponta que ações novas sem mapeamento caem em 365d por default.

## 6. Modelo de dados

**Zero nova tabela. Zero nova coluna. Zero novo índice.**

A tabela `imedto_admin_audit_log` (Wave 1) já tem índice em `criado_em` e índice composto `(acao, criado_em)` que cobrem o DELETE do job. Confirmado pelo orquestrador.

A migration Frente 3 é apenas DML (DELETE), não DDL.

## 7. Critérios de aceite (Dado / Quando / Então)

### Frente 1 — Cortes no código

**W7-CA1** — Login bem-sucedido **não** registra audit.
- **Dado** um admin autenticando com credenciais válidas
- **Quando** o `POST /api/admin/auth/login` retorna 200
- **Então** nenhuma linha é inserida em `imedto_admin_audit_log` com `acao = 'LOGIN_OK'`
- **E** o log estruturado do controller pode informar o login (via `ILogger`), mas sem chamada a `_audit.RegistrarAsync` para essa ação

**W7-CA2** — Logout **não** registra audit.
- **Dado** um admin autenticado
- **Quando** chama `POST /api/admin/auth/logout` e recebe 200
- **Então** nenhuma linha é inserida em `imedto_admin_audit_log` com `acao = 'LOGOUT'`

**W7-CA3** — Abertura de detalhe de tenant **não** registra audit.
- **Dado** um admin com acesso válido
- **Quando** chama `GET /api/admin/estabelecimentos/{id}` (rota usada pelo `ObterEstabelecimentoAdminQuery`) e recebe 200
- **Então** nenhuma linha é inserida em `imedto_admin_audit_log` com `acao = 'ABRIR_DETALHE_TENANT'`

**W7-CA4** — Login com falha **continua** registrando audit (regressão negativa).
- **Dado** uma tentativa de login com senha errada
- **Quando** o endpoint retorna 401
- **Então** **uma** linha é inserida em `imedto_admin_audit_log` com `acao = 'LOGIN_FAIL'` (comportamento preservado)

**W7-CA5** — Mutações continuam registrando audit (regressão negativa).
- **Dado** uma mutação como `CRIAR_PLANO`, `RESETAR_TENANT`, `CONCEDER_GRATUIDADE`, `REVELAR_CPF_DONO`
- **Quando** executada com sucesso
- **Então** uma linha continua sendo inserida em `imedto_admin_audit_log` com a ação correspondente (comportamento preservado)

### Frente 2 — Mapa + job de retenção

**W7-CA6** — Mapa `AuditLogRetencao.PorAcao` cobre todas as constantes de `AcoesAuditAdmin`.
- **Dado** o mapa estático em `Domain.Admin.AuditLogRetencao`
- **Quando** um teste unitário itera sobre todas as constantes públicas de `AcoesAuditAdmin` via reflection
- **Então** cada constante tem entrada no mapa **OU** o teste documenta explicitamente a ação como "fallback default 365d"
- **E** `AuditLogRetencao.TtlDiasParaAcao("ACAO_INEXISTENTE")` retorna `AuditLogRetencao.DefaultDias` (=365)

**W7-CA7** — Job `limpar-audit-admin` está registrado e roda no scheduler.
- **Dado** o serviço subiu com sucesso
- **Quando** o `JobScheduler` faz o bootstrap (`SemearJobsRegistrados`)
- **Então** existe linha em `jobs_agendados` com `nome = 'limpar-audit-admin'` e `intervalo_seg = 86400`
- **E** `JobsRegistrados.Todos` contém a entrada
- **E** o DI resolve `LimparAuditAdminJob` quando o scheduler procura por `Nome = "limpar-audit-admin"`

**W7-CA8** — Job remove linhas além do TTL respeitando o mapa.
- **Dado** linhas em `imedto_admin_audit_log` com diversas ações e datas (algumas antes do TTL, outras dentro da janela)
- **Quando** o `LimparAuditAdminJob.ExecutarAsync` roda uma vez
- **Então** linhas com `criado_em < UtcNow - TtlDiasParaAcao(acao)` são removidas
- **E** linhas dentro da janela (mesma ação, data mais recente) permanecem
- **E** ações **não** mapeadas usam o default de 365 dias
- **E** logs estruturados informam total removido por ação executada

**W7-CA9** — Job usa batches de 10.000 linhas (proteção contra lock pesado).
- **Dado** uma simulação com 25.000 linhas elegíveis para uma única ação
- **Quando** o job roda
- **Então** o DELETE é executado em rounds de até `BatchSize` (10.000) por ação até zerar
- **E** o log final reporta 25.000 linhas removidas para essa ação

### Frente 3 — Cleanup retroativo

**W7-CA10** — Migration de cleanup zera o backlog das 3 ações cortadas.
- **Dado** a migration `20260530210000_limpar_audit_admin_retroativo.sql` aplicada
- **Quando** consultar `SELECT COUNT(*) FROM imedto_admin_audit_log WHERE acao IN ('LOGIN_OK', 'LOGOUT', 'ABRIR_DETALHE_TENANT')`
- **Então** retorna `0` (independente da data de criação)

**W7-CA11** — Migration é idempotente.
- **Dado** a migration já aplicada uma vez
- **Quando** rodar de novo (manualmente)
- **Então** retorna sucesso e a contagem do passo anterior permanece em `0`
- **E** não falha por nenhum constraint

### Frente 4 — Cross-cutting

**W7-CA12** — Documentação atualizada.
- **Dado** a entrega da Wave 7
- **Quando** lê-se `Docs/ARQUITETURA.md`
- **Então** existe seção curta sobre política de retenção do audit admin citando `AuditLogRetencao` como fonte de verdade, o nome do job `limpar-audit-admin` e a decisão de não registrar `LoginOk`/`Logout`/`AbrirDetalheTenant`

**W7-CA13** — Zero PII em logs do job.
- **Dado** o `LimparAuditAdminJob` rodando
- **Quando** ele emite logs via `ILogger`
- **Então** as mensagens contêm apenas `nome do job`, `ação`, `quantidade removida`, `TTL em dias` — nunca `payload_json`, `admin_id`, IP ou qualquer PII

**W7-CA14** — Build e suíte de testes verdes.
- **Dado** a entrega completa
- **Quando** roda `dotnet build` e `dotnet test`
- **Então** ambos retornam sucesso e nenhum teste existente quebra

**W7-CA15** — Regressão de comportamento do Wave 6 (UI lista audit).
- **Dado** a tela do dashboard Wave 6 que lista audit
- **Quando** carrega após a Wave 7 deployada
- **Então** continua listando linhas restantes (mutações, LOGIN_FAIL, REVELAR_CPF_DONO etc.) corretamente
- **E** a redução do ruído é visível (sem `LOGIN_OK`/`LOGOUT`/`ABRIR_DETALHE_TENANT`)

## 8. Riscos e mitigações

| # | Risco | Probabilidade | Mitigação |
|---|---|---|---|
| R1 | DELETE pesado em prod trava tabela durante o job | Baixa (volume atual ~200 linhas; produção ainda crescendo) | Batches de 10k + índice composto `(acao, criado_em)` que cobre o WHERE. Job roda 1×/dia em horário definido pelo poll interval do scheduler — não há acúmulo gigante por rodada. |
| R2 | Ação nova adicionada em outro PR sem atualizar o mapa de retenção | Média | Default de 365 dias evita lixo eterno **e** delete agressivo. Teste W7-CA6 alerta se constante nova não tem entrada explícita. Mensagem PR de devs futuros pode citar o mapa. |
| R3 | Drift de timezone do servidor faz job rodar em momento inesperado | Baixa (lição da Wave 6 internalizada) | `DateTimeOffset.UtcNow` no cálculo do corte. Scheduler já opera em UTC. |
| R4 | Migration Frente 3 deleta linhas que ainda eram úteis para alguém investigando | Muito baixa | As 3 ações cortadas têm valor forense próximo de zero — login bem-sucedido fica no log estruturado do controller (ILogger), suficiente para investigação operacional. |
| R5 | Frontend Wave 6 quebrar por ausência das ações cortadas (filtro, gráfico) | Baixa | UI provavelmente lista o que existe, sem assumir ações específicas. QA valida W7-CA15. Se quebrar, é refactor pequeno do front. |
| R6 | DI esquece de registrar `LimparAuditAdminJob` → job não executa silenciosamente | Média | Scheduler já loga warning quando handler não está registrado (`"Handler não registrado para job '{Nome}'"`). QA valida W7-CA7 conferindo o log de bootstrap. |

## 9. Observações para execução

- **Ordem de execução obrigatória**: (1) `imedto-database` aplica migration Frente 3 → (2) `imedto-developer` implementa Frentes 1, 2, 4 → (3) `imedto-qa` valida tudo. A migration vem **primeiro** porque é independente do código e zera o backlog imediato; o job da Frente 2 fica para retenção contínua.
- **Nomenclatura**: `AuditLogRetencao` (não `RetencaoAuditAdmin`) — alinha com convenção do projeto de "substantivo do domínio + qualificador". Constantes em `AcoesAuditAdmin` já existem.
- **Testes obrigatórios**:
  - Unitário: `AuditLogRetencaoTests` — cobertura do mapa via reflection sobre `AcoesAuditAdmin` + asserção de default.
  - Integração leve: `LimparAuditAdminJobTests` — semear linhas com datas variadas, executar job, validar que só as elegíveis são deletadas.
  - Regressão: rodar `ImedtoAdminAuditWriterTests` existentes para confirmar que o writer não foi tocado.
- **Não tocar**: `ImedtoAdminAuditWriter`, `IImedtoAdminAuditWriter`, schema da tabela, índices existentes. Frentes 1 e 2 são surgical.
- **Liberdade técnica do dev**: estrutura interna do `LimparAuditAdminJob` (loop, default fallback via subquery NOT IN ou via map adicional), desde que respeite os CAs. O esqueleto na Seção 5 é guia, não contrato.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar seção curta "Audit admin global — política de retenção". Conteúdo:
  - Fonte de verdade: `Imedto.Backend.Domain.Admin.AuditLogRetencao`.
  - Job: `limpar-audit-admin` (1×/dia, batches de 10k).
  - Decisão: `LOGIN_OK`, `LOGOUT`, `ABRIR_DETALHE_TENANT` **não** são registradas (Wave 7 — alto ruído, baixo valor forense).
  - Default para ação não mapeada: 365 dias.
- **`Docs/LGPD.md`** — **não** atualizar nesta entrega. Audit do admin global não envolve PII de paciente; mudança é operacional. Se o orquestrador discordar, adicionar nota de 2 linhas na seção de audit citando a política.
- **Demais docs** — nenhuma mudança. Wave 7 é higiene, não introduz padrão arquitetural novo.

## 11. Hand-off

**Sequência**:

1. **`imedto-database`** — Frente 3 (migration `20260530210000_limpar_audit_admin_retroativo.sql`). Confirmar ID do timestamp e idempotência. Aplicar em `dev` via MCP RDS ou túnel SSH. Validar com `SELECT COUNT(*) FROM imedto_admin_audit_log WHERE acao IN (...)` = 0.
2. **`imedto-developer`** — Frentes 1, 2 e 4:
   - Cortar 3 chamadas de audit (Frente 1).
   - Criar `AuditLogRetencao` + `LimparAuditAdminJob` + registrar DI + `JobsRegistrados` (Frente 2).
   - Atualizar `Docs/ARQUITETURA.md` (Frente 4).
   - Adicionar testes unitários + integração leve.
3. **`imedto-qa`** — validar W7-CA1 a W7-CA15. Em particular:
   - Smoke do login válido (CA1), logout (CA2), abrir detalhe tenant (CA3) sem audit.
   - Login com falha continua gerando audit (CA4).
   - Mutação `CONCEDER_GRATUIDADE` ou similar continua gerando audit (CA5).
   - Verificar no banco que backlog das 3 ações está em zero (CA10).
   - Confirmar que `JobsRegistrados` está populado e `LimparAuditAdminJob` foi resolvido pelo DI (CA7).
   - Commit + push com referência ao briefing `2026-05-30_007`.

## 12. Próximos briefings sugeridos (backlog)

- **Wave 8 (opcional)** — Rotação para storage frio (S3) antes do delete definitivo, **se** auditoria externa exigir conservação plurianual de mutações financeiras.
- **Wave 9 (opcional)** — Dashboard de métricas do audit no painel Wave 6 (volume por ação, top admins, distribuição temporal). Útil só quando o admin tiver >10 usuários ativos.
- **Hardening** — Adicionar verificação de cobertura do mapa via análise estática (Roslyn analyzer) que falha o build se uma constante de `AcoesAuditAdmin` não tem entrada explícita no `AuditLogRetencao.PorAcao`. Hoje o teste unitário cumpre essa função.
