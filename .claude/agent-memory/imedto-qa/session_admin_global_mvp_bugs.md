---
name: session-admin-global-mvp-bugs
description: Bugs encontrados na validação do briefing 2026-05-30_001 admin-global-mvp; pipeline em rodada 4 — bug ag.data_inicio bloqueia estabelecimentos
metadata:
  type: project
---

Briefing: `planejamentos/2026-05-30_001_admin-global-mvp.md`.

## Rodada 4 (2026-05-30) — PIPELINE PAUSADA

**Bug A7 ativo (bloqueante)**: `column ag.data_inicio does not exist` em `AdminEstabelecimentosQueryRepository`

- Arquivo: `backend/src/Services/Imedto.Backend.Infrastructure/Admin/AdminEstabelecimentosQueryRepository.cs`
- Linhas afetadas: linha 96 (`ListarAsync`) e linha 194 (`ObterDetalheAsync`)
- Causa: Query SQL usa `ag.data_inicio >= DATE_TRUNC(...)` mas tabela `agendamentos` usa coluna `inicio_previsto` (confirmado em `AgendamentoQueryRepository.cs:46,61,81,86`)
- Fix: substituir `ag.data_inicio` por `ag.inicio_previsto` nas duas queries
- O bug A6 (v.ativo) foi corrigido na Rodada 3 — linhas 85 e 184 já estão com `v.status = 'Ativo'`
- O valor correto no domínio para VinculoStatus ativo é `'Ativo'` (não `'Aceito'` como foi sugerido pelo QA na rodada anterior)

## Rodada 3 (2026-05-30) — PIPELINE PAUSADA

**Bug A6 ativo (bloqueante)**: `column v.ativo does not exist` em `AdminEstabelecimentosQueryRepository`

- Arquivo: `backend/src/Services/Imedto.Backend.Infrastructure/Admin/AdminEstabelecimentosQueryRepository.cs`
- Linhas afetadas: 85 (`ListarAsync`) e 184 (`ObterDetalheAsync`)
- Causa: Query SQL usa `v.ativo = TRUE` mas `vinculo_profissional_estabelecimento` não tem coluna `ativo` — usa `status` (enum string: `Aceito`, `Pendente`, `Inativo`)
- Fix: substituir `AND v.ativo = TRUE` por `AND v.status = 'Aceito'` nas duas queries
- Referência para o valor correto: `VinculoProfissionalEstabelecimentoConfiguration.cs` e `VinculoStatus` enum

## O que passou na rodada 3

- CA1: login admin@imedto.com / 123123 → 200, JWT com `imedto_admin = "true"`, sem `estabelecimento_id` ✅
- CA2: login inválido → 401 "Credenciais inválidas" ✅
- CA9: JWT admin em /api/agendamentos → 403 "Acesso negado." ✅
- CA13: reset sem motivo → 422 ✅
- CA32: criar plano (com motivo) → 204 ✅
- CA33: nome duplicado → 422 "Já existe um plano com este nome." ✅
- CA36: gratuidade sem motivo → 422 ✅
- CA37: criar admin → 201 com senha temporária 20 chars ✅
- CA38: email duplicado → 422 "Já existe um admin com este e-mail." ✅
- CA39: desativar admin → 204 ✅
- CA40: trava último admin → 422 ✅
- CA3: refresh → 200 ✅
- CA4: logout → 200 ✅
- 79 testes backend passando (75 anteriores + 4 novos smoke de domínio adicionados pelo QA)
- CA12: todos os controllers admin têm [Authorize(Policy = "ImedtoAdmin")] ✅ (via grep)
- CA17: zero campo de paciente nos DTOs admin ✅ (via grep)
- CA47: módulo admin frontend não importa de fora (apenas ui/ e useDebouncedRef) ✅
- CA51: ARQUITETURA.md atualizado com seção Área Admin Global ✅
- CA52: LGPD.md atualizado com seção Acesso de admin global ✅

## Novos testes smoke adicionados pelo QA (rodada 3)

Arquivo: `backend/src/Tests/Imedto.Backend.Test/Domain/Admin/ImedtoAdminTests.cs`
- `Reativar_AdminInativo_ReativaCorretamente`
- `Reativar_AdminJaAtivo_LancaBusinessException`
- `AtualizarSenha_HashValido_ForceResetTrue_MarcaParaReset`
- `ConcluirResetSenha_LimpaFlag_ForcePasswordResetFalse`

## Fixes rodadas 1 e 2 confirmados

- A1: OnboardingCompletadoFilter bypass admin ✅
- A2: AdminController.cs legado deletado ✅
- A3: hash do seed corrigido ✅
- A4: AuditWriter virtual ✅
- A5: gratuidade_motivo threshold 10 chars ✅
- Assert teste rodada 2: `10 caracteres` ✅

## CAs bloqueados pelo bug A6

- CA4 até CA26 (todos que envolvem lista/detalhe de estabelecimento)
- CA10: reset de tenant (depende de ObterEstabelecimento)
- CA14/CA15: audit de reset/detalhe
- CA18/CA19: CPF mascarado/reveal
- CA21/CA22: estados de lista vazia/erro

**Why**: O dev usou `v.ativo` mas o schema real usa `v.status`. A tabela vinculo não tem coluna booleana ativo — usa status enum string.

**How to apply**: Quando receber próxima rodada, confirmar que `v.ativo` foi substituído por `v.status = 'Aceito'` em AMBAS as queries do repository, testar `/api/admin/estabelecimentos` via curl antes de fazer gates visuais.
