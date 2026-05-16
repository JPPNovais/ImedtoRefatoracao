---
name: qa-posdeploy-48543c3-2026-05-16
description: Pós-deploy 48543c3 (fix AppSelect options + ParseTipoProduto) — todos os 6 testes passaram, drawer Tipo de Produto/Equipe/Pacote funcional, P0 da rodada [[qa-posdeploy-a55ec79-2026-05-16]] resolvido.
metadata:
  type: project
---

QA pós-deploy `48543c3` em 2026-05-16 14:30 UTC, https://app.imedto.com com conta `qaorcfix1778941822068@imedtoteste.local` (Dono, Estabelecimento Id=8, plano Pro).

**Pipeline GitHub Actions:** verde (test-backend, test-frontend, build-push, migrate, deploy, smoke). Duração 6m41s.

**Re-teste do bug P0 da rodada [[qa-posdeploy-a55ec79-2026-05-16]]:**

1. Produtos → Novo produto → combobox Tipo lista 4 opções (Outros/OPME/Descartável/Curativo). Selecionei OPME, criou com `tipo=OPME`, stat OPMES=1, ticket R$ 1.500,00. ✅
2. Equipe → Novo papel → Tipo de honorário e Base de cálculo listam opções. Salvou "Cirurgião principal 60% procedimento". ✅
3. Pacotes → Novo pacote → 4 comboboxes populados (Anestesista, Procedimento, Produto, Papel). Adicionei 1 de cada + R$ 10.000. Card resumo: chips "1 procs / 1 produtos / 1 papéis / Dr. Roberto Anestesista". ✅
4. Editar Procedimento → Vincular Produto → combobox lista "Prótese mamária 350cc — R$ 1.500,00", botão Vincular ativa após seleção, Total sugerido = base R$ 8.000 + produto R$ 1.500 = R$ 9.500. Toggle Incluído→Opcional zera produto. ✅
5. Defense-in-depth backend: `POST /produtos` com `tipo:""` → 422 "Tipo do produto é obrigatório."; `tipo:null` → idem; `tipo:"TipoInexistente"` → 422 "Tipo de produto inválido.". ✅
6. Regressão: lazy-load aba (só requisição da aba ativa); multi-tenant `X-Estabelecimento-Id:999` → 404 "Estabelecimento não encontrado." (mensagem genérica OK, não vaza info); `/api/orcamentos` legado → 200. ✅

**Status final:** ✅ pronto para produção. Bug P0 da rodada anterior fechado; bug P1 backend defense-in-depth resolvido.

**Caveats observadas (não-bloqueantes):**
- `select.value` no DOM expõe `id` numérico, mas a a11y tree mostra o texto da option (`uid.value="Prótese mamária 350cc — R$ 1.500,00"`). Para `evaluate_script`, usar o `id` real (`target.value='4'`).
- Confirmado que `mcp__chrome-devtools__click` em AppButton não dispara handler Vue para alguns botões — workaround via `evaluate_script` + `dispatchEvent(new MouseEvent('click'))` continua necessário.
