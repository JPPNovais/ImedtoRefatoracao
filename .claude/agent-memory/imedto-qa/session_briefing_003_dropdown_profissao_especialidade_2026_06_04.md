---
name: session_briefing_003_dropdown_profissao_especialidade_2026_06_04
description: Pipeline fechada para briefing 2026-06-04_003 — dropdown estrito profissão+especialidade no modal do profissional. Commits ce0c954+1649e81 pushed.
metadata:
  type: project
---

Pipeline fechada com aprovação em 2026-06-04.

**Why:** Substituição de AppInput texto livre por dropdowns de catálogo estrito (profissão → especialidade) no ProfissionalDetalhesModal.vue, com paridade ao ConvidarProfissionalModal. Profissão passou a ser editável no vínculo pós-convite via comando atômico.

**How to apply:** Próximas features que toquem profissão/especialidade de vínculo devem usar useProfissaoEspecialidade composable (fonte única). Endpoint legado /especialidade está @deprecated no frontend — migrar para /profissao-especialidade.

## Commits pushados

- `ce0c954` feat(equipe): dropdown estrito de profissão+especialidade por estabelecimento no modal do profissional
- `1649e81` chore(memory): registrar sessão QA do briefing 2026-06-04_003

## Resultado da suíte

- Backend: 1261 passando, 0 falhas (1338 total com 77 skipped de integração)
- Frontend: 405 passando, 0 falhas (48 test files)
- Build: verde
- Lint: verde (exit 0 — aviso ESLint config pré-existente não relacionado)
- Typecheck (vue-tsc via vite build): verde

## Novos testes

- 10 NUnit: `AlterarProfissaoEspecialidadeDoVinculoCommandHandlerTests.cs`
- 13 Vitest: `useProfissaoEspecialidade.test.ts`
- 8 Vitest: `ProfissionalDetalhesModal.test.ts`
Total: 23 novos

## CAs validados (análise de código)

- CA1 dropdown profissão pré-selecionado: carregarProfissoes() em onMounted + inicializarComVinculo com profissaoConvidadaId do vínculo
- CA2 especialidade dependente: watch(profissaoId) → listarEspecialidades(id) + pré-seleção
- CA3 troca de profissão limpa especialidade: watch com idAnterior !== undefined → especialidade.value = ""
- CA4 persistência atômica: alterarProfissaoEspecialidade (não dois PUTs separados)
- CA5 multi-tenant: ObterPorIdNoEstabelecimentoOuNulo filtra por estabelecimento_id; mensagem genérica
- CA6 RBAC: RequiresPapel(Dono) + estab.DonoUsuarioId; front usa permissoes.ehDono
- CA7 catálogo estrito: ExisteEspecialidadeAtivaPorNome → 422 BusinessException
- CA8 migração match → canônico: UPDATE com lower(unaccent(trim)) match
- CA9 migração sem match → NULL: NOT EXISTS subquery
- CA10 migração idempotente: guards IS DISTINCT FROM / IS NOT NULL + RAISE NOTICE
- CA11 estados dropdown: carregandoEspecialidades, profissaoTemEspecialidades, placeholders
- CA12 conselho acompanha profissão: conselhoSigla computed de profissaoSelecionada.conselhoSigla
- CA13 não-regressão COALESCE: 3 pontos canônicos intocados (VinculoQueryRepository x2 + TermoResolverDeVariaveis)

## Nota de débito técnico

PUT /profissao-especialidade colocado no ModeloPermissaoController com rota absoluta (mesmo padrão do AtribuirAoVinculo). Idealmente em EstabelecimentoProfissionaisController — débito de organização sem impacto funcional.

## Migration pendente em prod

`20260604131549_normalizar_especialidade_convidada_legado.sql` aplicado via pipeline (psql direto, fora do EF). Necessário aplicar em produção. Relatório de contagem via RAISE NOTICE.
