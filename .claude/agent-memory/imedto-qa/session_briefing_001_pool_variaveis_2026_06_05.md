---
name: session_briefing_001_pool_variaveis_2026_06_05
description: Briefing 2026-06-05_001 pool de variáveis + autocomplete prontuário — pipeline fechada, commits f167f1b+bef1c08 pushed
metadata:
  type: project
---

Pipeline fechada em 2026-06-06.

## Resultado

Build: 1298 backend + 458 frontend (100% green). CA15 cancelado pelo addendum (Expectativa sem campo).

## CAs validados (1–14 e 16)

CA1–autocomplete: SecaoHistoriaPregressa carrega por tipo via variavelPoolService.listar(); filtro client-side.
CA2–criação automática: PoolExtratorEvolucao extrai e cria inéditos no pool ao salvar evolução.
CA3–dedup acento/trim/case: NormalizadorPool.Normalizar + HashSet em memória.
CA4–reusa padrão-sistema: ListarAtivosPorTipo inclui EhPadraoSistema — match não cria cópia.
CA5–multi-tenant: PoolExtratorEvolucao usa estabelecimentoId do command; ListarAtivosPorTipo filtra por tenant.
CA6–sem ModelosProntuario para criação via evolução: PoolExtratorEvolucao não checa permissão.
CA7–CRUD manual exige ModelosProntuario: [RequiresPermissaoExtra] em POST/PUT/DELETE /pool.
CA8–LGPD só nome vira pool: extrator acessa apenas campo nome/parentesco; campos livres ignorados.
CA9–campos vazios não geram lixo: IsNullOrWhiteSpace guard no extrator.
CA10–estado vazio: AppAutocompleteCriavel exibe "Nenhuma opção cadastrada — digite para criar uma nova".
CA11–degradação em erro: prop erro=true → dropdown não abre, input permanece editável.
CA12–migration idempotente: DELETE Droga/AtividadeFisica, sem BEGIN/COMMIT, idempotente por natureza.
CA13–enum/admin/config sem Droga/AtividadeFisica: mensagem 422 lista 6 tipos; Expectativa aceito.
CA14–RelacaoFamiliar história familiar: SecaoHistoriaFamiliar usa AppAutocompleteCriavel com tipo RelacaoFamiliar.
CA16–transacionalidade: UnitOfWorkFilter abre IDbContextTransaction por request; rollback reverte pool+evolução.

## Arquivos novos críticos

- NomalizadorPool.cs (TYPO no filename — classe é NormalizadorPool). Compila normalmente em .NET. Registrado como debt cosmético no commit.
- PoolExtratorEvolucao.cs
- AppAutocompleteCriavel.vue + .test.ts

## Carona

EstabelecimentoView.vue: migração de nav customizado para AppTabs. Commitado separado (bef1c08), conforme feedback_caronas_vscode_ok.

## Notas de pipeline

- Lint frontend (ESLint) tem erro pré-existente desde commit inicial (e2e3e52): @typescript-eslint/recommended não encontrado. Não introduzido por esta entrega. Build (vue-tsc+vite) passou limpo.
- chrome-devtools/banco indisponíveis no sandbox — validação por análise de código + suíte. Migration pendente de apply em prod.
- SecaoProcedimentosIndicados.vue NÃO foi tocado (confirmado por git diff vazio).
