# Auditoria LGPD — Módulo Pacientes (Fase 3.1)

**Data**: 2026-05-02
**Escopo**: backend `Pacientes/*` + cross-check com `frontend/src/{services,views,components}/pacientes/`

## Sumário

| Categoria | Achados |
|---|---|
| Campos PII / dados desnecessários em DTO | **3** |
| Queries Dapper sem `WHERE estabelecimento_id` | **0** |
| Mensagens de erro com PII | **0** |
| Logs com PII | **0** |
| Gaps de autorização | **3** (1 crítico) |
| Audit trail | **gap completo** |
| Bugs | **2** (KeyNotFound→500, Salvar fora do UoW) |

## 1. DTOs — minimização

### `PacienteDto` (`Contracts/Pacientes/Queries/Results/PacienteDto.cs`)

| Campo | Lido no front? | Severidade | Ação |
|---|---|---|---|
| `Id` | sim (rotas) | — | manter |
| `EstabelecimentoId` | **não** | **crítico LGPD** | **remover** — amplia superfície de IDOR |
| `NomeCompleto` | sim | — | manter |
| `Cpf` | sim (`PacienteDetalheView.vue:174`) | — | manter |
| `DataNascimento` | sim | — | manter |
| `Genero` | sim | — | manter |
| `Telefone` | sim | — | manter |
| `Email` | sim (`PacienteDetalheView.vue:192`) | — | manter |
| `Endereco` | parcial — front exibe só Cidade/UF, mas form de edição faz round-trip | médio | manter (round-trip do form) |
| `Observacoes` | **não** no detalhe; só no form de edição (round-trip) | médio LGPD | mover para `PacienteEdicaoDto` separado |
| `CriadoEm` | sim (lista) | — | manter |
| `AtualizadoEm` | **não** | baixo | **remover** |

`PacienteListaItemDto`: minimização correta — não precisa de mudança.

## 2. Queries Dapper

`PacienteQueryRepository`:
| Método | `WHERE estabelecimento_id`? | Linha |
|---|---|---|
| `Listar` (count) | sim | 41 |
| `Listar` (items) | sim | 55 |
| `ObterPorId` | sim | 103 |

**Status: 100% das queries escopadas por tenant.** Clamp `tamanhoPagina ∈ [1,100]` em linha 27 — bom controle anti-DoS.

## 3. Mensagens de erro e logs

**Sem vazamento de PII** em mensagens nem logs estruturados. Padrão limpo: só IDs em `LogX`, mensagens genéricas em `BusinessException`.

## 4. Gaps de autorização

| Endpoint | Auth atual | Risco | Recomendação |
|---|---|---|---|
| `GET /api/paciente/{id}/exportar-dados` | qualquer vínculo | **crítico LGPD** — Recepcionista baixa todos dados pessoais | `[RequiresPapel(Dono)]` ou `(Profissional, Dono)` |
| `POST /api/paciente` | qualquer vínculo | médio — Recepcionista cria paciente livremente | `[RequiresPapel(Profissional, Dono)]` |
| `PUT /api/paciente/{id}` | qualquer vínculo | médio — Recepcionista edita observação clínica | `[RequiresPapel(Profissional, Dono)]` |

`PacienteRepository.ObterPorId(long id)` (linha 15-21): **busca sem `estabelecimento_id`**. Validação de tenant é feita no handler depois (`if (paciente.EstabelecimentoId != command.EstabelecimentoId) throw`). Defense-in-depth fraca — recomendação: `ObterPorId(long id, long estabelecimentoId)` filtrando dentro do `Where`. Falha-fechado por design.

## 5. Audit trail — GAP COMPLETO

Pacientes **não tem** equivalente do `ProntuarioAcessoLog`.

Hoje audit é apenas via `LogInformation` no stdout — volátil, não imutável.

**Recomendação**: criar `PacienteAcessoLog` (tabela append-only) registrando: `paciente_id`, `usuario_id`, `tipo_acesso (LEITURA/EDICAO/EXCLUSAO/EXPORT/ANONIMIZACAO)`, `criado_em`, `ip_origem`. Aplicar em `Obter`, `Atualizar`, `Deletar`, `ExportarDados`. Listagens podem ser audit agregado.

## 6. Bugs

1. **`KeyNotFoundException` → 500**: `ObterPorId` lança quando não acha. `AtualizarPacienteCommandHandler.cs:19` e `DeletarPacienteCommandHandler.cs:24`. Vira 500 no `GlobalExceptionFilter`. Trocar por `ObterPorIdOuNulo` + `BusinessException("Paciente não encontrado.")` → 422 ou 404 explícito.

2. **`Salvar` fora do UoW**: `PacienteRepository.cs:40` no caminho create faz `SaveChangesAsync()` direto. Se `PacienteCadastradoEvent` falhar, paciente fica salvo sem evento. **Severidade alta** — quebra integridade do audit do evento.

3. **`ExportarDados` retorna `object` anônimo** (`ExportarDadosPacienteQueryHandlers.cs`): sem tipagem para o front, e omite metadados de soft-delete/anonimização que o titular tem direito de ver (Art. 18 LGPD). Criar `PacienteExportLgpdDto`.

## 7. Performance

- **AsNoTracking()**: aplicado onde necessário. ✅
- **Paginação**: clamp [1,100]. ✅
- **N+1**: nenhum encontrado. ✅
- **Índices**:
  - `(estabelecimento_id, deletado_em)` ✅
  - Único `(estabelecimento_id, cpf) WHERE cpf IS NOT NULL AND deletado_em IS NULL` ✅
  - **Faltando**: índice trigram para busca por nome (`nome_completo ILIKE '%…%'` invalida B-tree). Recomendar `pg_trgm` GIN em `lower(unaccent(nome_completo))`.

## 8. Top 3 ações por prioridade

1. **Restringir `ExportarDados` a `Dono`** + criar `PacienteAcessoLog` + registrar export.
2. **Criar `PacienteExportLgpdDto`** dedicado substituindo `object` anônimo.
3. **Remover `EstabelecimentoId`/`AtualizadoEm`/`Observacoes`** de `PacienteDto`; adicionar `RequiresPapel` em POST/PUT/EXPORT; mudar `ObterPorId` para exigir `estabelecimentoId`.

## 9. Plano de execução — status

| # | Mudança | Status | Commit |
|---|---|---|---|
| F3.1.1 | Bugs (KeyNotFound→422) + minimização DTO (2 campos removidos) | ✅ feito | 67cf1f6 |
| F3.1.2 | `[RequiresPapel]` em POST/PUT/EXPORT | ✅ feito | 287a0e7 |
| F3.1.3 | Defense-in-depth no `PacienteRepository.ObterPorId` (overload + Obsolete) | ✅ feito | 4c2b82f |
| F3.1.4 | `PacienteExportLgpdDto` tipado + metadados LGPD | ✅ feito | 4703200 |
| F3.1.5 | Índice trigram `pg_trgm` em `nome_completo` | ⏭️ Fase 7 (otimização) | — |
| F3.1.6 | `Salvar` dentro do UoW | ⏭️ Fase 7 ou pós-6 (refactor arquitetural) | — |
| F3.1.X | `PacienteAcessoLog` (tabela + service + handler) | ⏭️ sessão dedicada (follow-up) | — |

### Por que F3.1.5 e F3.1.6 foram diferidas

- **F3.1.5 (índice trigram)**: requer migration EF + SQL idempotente + extensão `pg_trgm` no Postgres. Otimização de leitura, não LGPD nem bug. Encaixa melhor na Fase 7 onde temos visão global de índices faltantes em todos os módulos.
- **F3.1.6 (Salvar fora do UoW)**: o padrão `SaveChangesAsync` direto no `Salvar` para obter `Id` antes do evento é repetido em vários repositórios (Profissionais, Estabelecimentos, etc.). Mudar só Pacientes deixaria inconsistente. Refactor adequado: usar `IDomainEventDispatcher` rodando após `SaveChangesAsync` do `UnitOfWorkFilter`. Trabalho arquitetural — Fase 7 ou sessão dedicada pós-Fase 6.

### Por que F3.1.X (audit log) foi diferido

Criar `PacienteAcessoLog` envolve:
- Aggregate root + repositório + service
- Migration EF + SQL idempotente para `supabase/migrations/`
- Index Postgres em `(paciente_id, criado_em)`
- Aplicar nos 4+ pontos de acesso (Obter, Atualizar, Deletar, ExportarDados)
- Cuidar de retenção/rotação (tabela cresce muito)

É 1 sessão de trabalho dedicada. Decisão: encerrar Fase 3.1 com os 4 commits LGPD-críticos feitos, registrar como follow-up explícito, e seguir para Fase 3.2 onde o `ProntuarioAcessoLog` (que **já existe**) pode servir de molde.
