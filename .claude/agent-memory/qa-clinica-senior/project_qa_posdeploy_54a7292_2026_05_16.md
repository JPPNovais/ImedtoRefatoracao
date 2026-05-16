---
name: project-qa-posdeploy-54a7292-2026-05-16
description: Pós-deploy feature Estoque-Cadastros — pipeline ok; bug crítico no drawer "Novo produto" (tamanho=200 dispara 422 do back, selects vazios).
metadata:
  type: project
---

Feature `feat(estoque): tela de cadastros` (SHA 54a7292, run 25963097313) deployada em prod 2026-05-16 13:25 UTC. Pipeline 100% verde (test/build/migrate/deploy/smoke).

**Validação MCP (Dono jppnovais@gmail.com / novaEra):**
- /inventario/cadastros: header `.app-page`, AppTabs 5 abas, "Importar planilha" disabled com tooltip — OK.
- Categorias: criar "Anestésicos" (Azul/Injetáveis) → badge `Categorias (1)`, AppPagination, `.btn-icon-*` — OK.
- Fornecedores: validação CNPJ inline `11.222.333/0001-44` bloqueada no front; `11.222.333/0001-81` criado — OK.
- **Bug crítico**: drawer "Novo produto" abre selects vazios mesmo com Categoria/Fornecedor criados.

**Diagnóstico**: `frontend/src/components/estoque/cadastros/CadastroProdutosTab.vue:39-42` chama os 4 listar com `tamanho: 200`, mas `CadastrosEstoqueQueryRepository.ValidarPagina` (Infrastructure/Database/Repositories/Cadastros/CadastrosEstoqueQueryRepository.cs:232-233) trava em `tamanho > 100` com `BusinessException("Tamanho da página deve estar entre 1 e 100.")` → 422. Sem categorias no select, campo obrigatório, drawer inutilizado.

**Why**: discrepância de premissa entre front (200) e back (100); regra dupla (defense-in-depth) que não foi conciliada.

**How to apply**: ao revisar tela paginada que precisa "listar tudo pra dropdown", checar limite do back (CadastrosEstoqueQueryRepository.ValidarPagina) antes de aceitar `tamanho: N`. Padrão preferível: criar endpoint `/leve` (sem paginação, retorna até 500 com 1 só select) ou subir limite do back para 500 quando dropdown for o uso esperado.

**Status final**: 4/5 fluxos OK (Categorias, Fabricantes, Fornecedores, Locais via tabs+CRUD); 1 quebrado (Novo Produto). Despachado pra correção em `fullstack-clinica-senior`.
