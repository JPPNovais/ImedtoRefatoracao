---
name: project-estoque-cadastros-mvp
description: MVP de Cadastros do Estoque entregue em 2026-05-16 — 4 aggregates novos + evolução de ItemInventario com FKs.
metadata:
  type: project
---

Implementado em 2026-05-16: tela `/inventario/cadastros` com 5 abas (Produtos, Categorias, Fabricantes, Fornecedores, Locais) + evolução de `itens_inventario` para usar FKs (`categoria_id NOT NULL`, `fabricante_id`, `fornecedor_padrao_id`, `local_padrao_id`, `custo_unitario`).

**Why:** centralizar cadastros mestre do estoque, normalizando categoria (que era text livre) e adicionando fabricante/fornecedor padrão/local (campos que antes não existiam). Substitui o campo "marca" string conceitual por FK para `fabricantes_estoque`.

**How to apply:**
- Aggregate `CategoriaEstoque` é OBRIGATÓRIO em todo `ItemInventario`. Os outros 3 são opcionais.
- `ItemInventario.Categoria` (text) foi MANTIDA para deprecation: aggregate sincroniza o snapshot do nome ao salvar. Não tirar sem nova migration.
- Tipo de local é enum fechado: `Armario|Gaveta|Refrigerado|Cofre|Estante|Sala` (persistido como string).
- CNPJ é validado com dígito verificador no Domain (`CnpjValidator`) e espelhado no front (`utils/validateCnpj.ts`). Opcional (NULL aceito).
- Inativar categoria/fabricante/fornecedor/local com itens vinculados retorna 422 ("reatribua antes"). Padrão de "soft delete via flag `Ativo`" — nada nunca é apagado de fato.
- Migration `20260516125710_CriarCadastrosEstoque` faz data migration: cria 1 CategoriaEstoque por `(estabelecimento, categoria text)` distinto e popula `categoria_id` antes de aplicar `NOT NULL`. Itens com categoria vazia viram "Sem categoria".
- Papéis: gate via `[RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]` em POST/PUT/DELETE (lembrar: enum é `Recepcionista`, não `Recepcao`).
- Front consome `estoqueCadastrosService` (4 grupos: categorias/fabricantes/fornecedores/locais). `inventarioService` agora aceita `categoriaId` em vez de `categoria` string nos payloads.
- Tests: `CategoriaEstoqueTests`, `FornecedorEstoqueTests`, `LocalEstoqueTests`, `FabricanteEstoqueTests` em Domain. Handlers de Criar/Atualizar/Inativar com multi-tenant + duplicidade em Application.
