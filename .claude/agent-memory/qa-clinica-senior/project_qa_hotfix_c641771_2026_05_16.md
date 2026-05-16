---
name: qa-hotfix-c641771-2026-05-16
description: Hotfix c641771 — endpoints /opcoes para Estoque/Cadastros eliminam 422 do drawer Novo produto. Validado em produção.
metadata:
  type: project
---

Hotfix do bug crítico identificado em [[project_qa_posdeploy_54a7292_2026_05_16]] (drawer "Novo produto" em /inventario/cadastros não populava 4 selects porque o front pedia tamanho=200 e back limitava paginação em 100 → 422).

**Why:** bug crítico travava o fluxo principal da feature "Estoque — Cadastros" recém-publicada; recepção/operador não conseguia criar produto algum.

**How to apply:** confiar que `/api/inventario/cadastros/{categorias|fabricantes|fornecedores|locais}/opcoes` retorna `[{id, nome}]` (DTO mínimo, somente ativos, LIMIT 500). Esses endpoints são reutilizáveis em qualquer outra tela do front que precise listar opções desses 4 cadastros sem paginação — não criar GETs paralelos.

Run CI: `25963599152` (success, 6 jobs verdes incluindo smoke).
Commit SHA: `c641771`.
Cenário re-validado em prod (jppnovais@gmail.com, persona Dono · novaEra):
- 4× GET `/opcoes` → 200 (zero 422).
- Drawer "Novo produto" abre com Categoria/Fornecedor populados (Fabricante/Local vazios apenas porque o estab. ainda não tem cadastros nessas tabelas — corretamente exibe só "— Nenhum —").
- POST `/api/inventario/itens` → 201, produto aparece na listagem com "1–1 de 1 produtos".

Working tree na sessão tinha WIP de Orçamento (`OrcamentoCatalogoQueryRepository`, `AppDbContext`, `Domain/Orcamentos/Catalogos/*`, `frontend/components/orcamento/config/`) que **quebra o build local** — fica em stash até o fullstack finalizar a feature. Stash WIP foi restaurado pós-push para preservar o trabalho.

Pendências/sugestões (não-bloqueantes):
- Quando o cadastro de Fabricante e Local for criado, o drawer Novo produto deveria oferecer botão "+ Adicionar novo" inline em cada select (recepção que descobre que falta cadastrar fabricante hoje tem que cancelar drawer, ir na aba, criar, voltar e refazer). Atrito de 3+ cliques.
