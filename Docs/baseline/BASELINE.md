# Baseline pré-Fase 1

Capturado em 2026-05-02 (commit `9740259`).

## Build

- Comando: `dotnet build Imedto.Backend.sln -c Debug`
- Resultado: **0 errors / 582 warnings**
- Log completo: [baseline_build.log](baseline_build.log)

### Warnings por categoria

| Código | Qtd | Descrição | Ação prevista |
|---|---|---|---|
| CS8632 | 1136* | Nullable annotation `?` fora de contexto `#nullable enable` | Resolvido em PRÉ-4 ao ativar `<Nullable>enable</Nullable>` no `Directory.Build.props` (vai gerar warnings novos sobre não-nullable não inicializados — endereçados na Fase 1) |
| NU1902 | 16* | Pacote NuGet com vulnerabilidade moderada | **Atualizar OpenTelemetry.Api** (Fase 2) |
| NU1903 | 8* | Pacote NuGet com vulnerabilidade alta | **Atualizar System.Security.Cryptography.Xml** (Fase 2) |
| CS0162 | 2* | Código inalcançável | Remover (Fase 1) |
| CA2024 | 2* | Uso de `reader.EndOfStream` em método async | Corrigir em [AnthropicIaService.cs:86](backend/src/Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs#L86) (Fase 6) |

> \* Contagem por linha de log; o dotnet reporta 582 únicos. Diferença vem de repetição entre projeto e `.sln`.

### Warnings por projeto

| Projeto | Qtd |
|---|---|
| Imedto.Backend.Domain | 376 |
| Imedto.Backend.Contracts | 268 |
| Imedto.Backend.Infrastructure | 226 |
| Imedto.Backend.API | 178 |
| Imedto.Backend.Application | 48 |
| Imedto.Backend.Test | 42 |
| Imedto.Backend.IntegrationTest | 2 |

### Vulnerabilidades NuGet (CRÍTICO)

| Pacote | Versão | Severidade | Advisory |
|---|---|---|---|
| `OpenTelemetry.Api` | 1.10.0 | Moderate | [GHSA-8785-wc3w-h8q6](https://github.com/advisories/GHSA-8785-wc3w-h8q6) |
| `OpenTelemetry.Api` | 1.10.0 | Moderate | [GHSA-g94r-2vxg-569j](https://github.com/advisories/GHSA-g94r-2vxg-569j) |
| `System.Security.Cryptography.Xml` | 9.0.0 | **High** | [GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx) |
| `System.Security.Cryptography.Xml` | 9.0.0 | **High** | [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf) |

Afeta: `Imedto.Backend.API`, `Imedto.Backend.Infrastructure`, `Imedto.Backend.IntegrationTest`.

### Outros achados acionáveis na Fase 1

- [Tests/Imedto.Backend.Test/Infrastructure/Ia/RateLimitedIaServiceTests.cs:411](backend/src/Tests/Imedto.Backend.Test/Infrastructure/Ia/RateLimitedIaServiceTests.cs#L411) — código inalcançável.
- [Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs:86](backend/src/Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs#L86) — `reader.EndOfStream` em método async (CA2024).

## Testes

- Comando: `dotnet test Imedto.Backend.sln --no-build -c Debug`
- Log completo: [baseline_tests.log](baseline_tests.log)

| Projeto | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| Imedto.Backend.Test (unit) | 225 | 0 | 0 | 225 |
| Imedto.Backend.IntegrationTest | 11 | 0 | 1 | 12 |
| **Total** | **236** | **0** | **1** | **237** |

Skipped: `HardDelete_Paciente_GraváUmaLinhaDeAuditoria` (verificar motivo na Fase 3 — Pacientes/LGPD).

Tempo total: ~916 ms.

## Build após PRÉ-4 (analyzers ativos)

Após adicionar [Directory.Build.props](backend/src/Directory.Build.props) com Roslynator + Meziantou + `AnalysisLevel=latest-recommended`:

- **0 errors / 1065 warnings** (vs. 582 antes — +483 warnings novos do NetAnalyzers em modo recomendado).
- Testes seguem **236 passed / 1 skipped / 0 failed**.

### Top códigos novos (a atacar na Fase 1)

| Código | Qtd | Significado |
|---|---|---|
| CA1707 | 486 | Underscores em nomes de membro (testes — podem ficar suprimidos por convenção de teste) |
| CA1848 | 170 | Usar `LoggerMessage` delegates em vez de `ILogger.LogX` direto |
| CA1861 | 166 | Evitar arrays constantes como argumentos |
| CA1305 | 36 | Especificar `IFormatProvider` em conversões de string |
| CA1711 | 32 | Sufixos de tipo enganosos (`*Exception`, `*Stream` etc.) |
| CA1725 | 20 | Renomear parâmetro para casar com a interface |
| CA1852 | 14 | Selar tipos internos sem herdeiros |
| CA1311 | 12 | Especificar cultura em `ToUpper`/`ToLower` |
| CA1304 | 12 | Especificar `CultureInfo` |
| CA1862 | 6 | Usar overload `StringComparison` em vez de `ToLower` para comparar |
| CA1310 | 6 | Especificar `StringComparison` em `StartsWith`/`EndsWith` |
| CA1822 | 4 | Tornar membro `static` quando não usa estado |
| CA1716 | 4 | Identificador conflita com keyword reservada |
| CA2219 | 2 | Não lançar exceção em `finally` |
| CA1866 | 2 | Usar `string.StartsWith(char)` em vez de `string` |
| CA1859 | 2 | Trocar tipo concreto retornado por interface |

Log: [baseline_build_with_analyzers.log](baseline_build_with_analyzers.log).

## Ambiente

- .NET SDK: `10.0.101`
- OS: Darwin 25.4.0 (arm64)
- Docker: instalado (`/Applications/Docker.app`, CLI `/usr/local/bin/docker`), **daemon parado**.
  - Necessário **apenas na Fase 10** (Testcontainers para Postgres). Iniciar com Docker Desktop antes de começar essa fase.
- Working tree: limpo no commit `9740259`.
