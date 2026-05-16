---
name: qa-posdeploy-a55ec79-2026-05-16
description: Pós-deploy a55ec79 (Config Orçamento 5 abas) — bug P0 AppSelect com :options não renderiza opções, quebra Tipo de Produto, Tipo Honorário, Base Cálculo, vincular Produto em Procedimento e seleções em Pacotes.
metadata:
  type: project
---

QA pós-deploy `a55ec79` em 2026-05-16 14:00 UTC, https://app.imedto.com com conta `qa-orc-1778940306@imedtoteste.local` (Dono, Estabelecimento Id=7).

**Pipeline GitHub Actions:** verde (test-backend, test-frontend, build-push, migrate 1m33s, deploy 57s, smoke 17s).

**Bug P0 — AppSelect quebrado em 4 componentes da feature:**

- O componente [AppSelect](frontend/src/components/ui/AppSelect.vue) renderiza um `<select>` nativo com `<slot/>` no template — espera `<option>` filhos.
- 7 chamadas em `frontend/src/components/orcamento/config/` usam `<AppSelect v-model="..." :options="opcoesXyz" />` — a prop `:options` é silenciosamente ignorada → `<select>` vazio.
- Locais afetados:
  - `ProdutosTab.vue:220` — drawer "Tipo" do produto (default "Outros" sempre)
  - `EquipeTab.vue:226` — "Tipo de honorário"
  - `EquipeTab.vue:238` — "Base de cálculo"
  - `PacotesTab.vue:311,326,342` — seletor de procedimento / produto / team-role dentro do drawer Pacote
  - `ProcedimentoProdutosLink.vue:142` — vincular produto ao procedimento
- Consequência: drawer "Novo produto" cria com tipo="Outros" sempre, drawer "Novo papel de equipe" não consegue salvar (tipoHonorario obrigatório), drawer "Novo pacote" não consegue adicionar itens, drawer "Editar procedimento" mostra "Vincular" desabilitado.
- Reproduzido em produção: criei "Prótese mamária 350cc" via UI → tipo="Outros" (esperado OPME). Via API direta com `tipo:"OPME"` → backend aceita corretamente (tipo="OPME") — bug é estritamente front.

**Correção esperada:** ou (a) trocar uso para `<AppSelect><option v-for="..."/></AppSelect>` em todos os componentes da feature, ou (b) estender `AppSelect.vue` para aceitar prop `options: {value, label}[]` e renderizar `<option v-for="..."/>` quando passada. Opção (b) é mais consistente com a API que os componentes já assumem que existe — é provável que tenham sido desenhados contra outra versão do AppSelect.

**Bug P1 — backend aceita tipo string vazio em CriarProduto:**
- `POST /api/orcamentos/configuracoes/produtos` com `tipo:""` retorna 200 e salva como "Outros". Espera-se 422 `BusinessException("Tipo é obrigatório.")` ou normalização explícita.
- Não bloqueante (front nunca manda string vazia hoje), mas defense-in-depth pede validação explícita.

**O que passou no QA:**
- Container `.app-page--wide` aplicado, centralizado.
- 6 abas com badges de contagem atualizando ao vivo (Procedimentos (1), Produtos (1), Equipe (0), etc.).
- Stats cards (CADASTRADOS, ATIVOS, TICKET MÉDIO, CATEGORIAS) refletem dados reais.
- AppPagination presente em Procedimentos e Produtos, "1–1 de 1 itens", seletor 10/20/30.
- Drawer "Novo procedimento" salva, tabela popula, badge atualiza.
- Querystring `?aba=...` persiste após F5.
- Lazy-load funcional: trocar para `?aba=anestesistas` faz APENAS request `/api/orcamentos/configuracoes/anestesistas`.
- Multi-tenant: `X-Estabelecimento-Id` inexistente → 404 "Estabelecimento não encontrado." (mensagem genérica ok).
- Empty state com CTA "Criar primeiro procedimento" / "Criar primeiro produto" / etc.
- Aba "Outras configurações" renderiza com aviso transitório + tabs filho (Local cirurgia, Implantes, Equipes legado, Pagamento).
- Regressão: tela `/orcamentos` legada e modal "Novo orçamento" continuam funcionando sem erro.
- Console limpo (zero errors/warns).
- Botões "Importar planilha" / "Exportar" disparam AppToast "em breve" (verificado via `dispatchEvent` — o `mcp.click` por algum motivo não dispara handler do Vue, mas comportamento real do usuário com mouse vai funcionar).

**Veredito:** ❌ BLOQUEADO — bug P0 inviabiliza uso real de 5 das 7 funcionalidades novas do drawer. Devolvido ao fullstack-clinica-senior.

**Cenário para re-teste após correção:**
1. Login → `/configuracoes/orcamento?aba=produtos` → Novo produto → conferir que combobox "Tipo" lista Outros/OPME/Descartável/Curativo.
2. Salvar com Tipo=OPME → tabela deve mostrar tipo="OPME" e stat "OPMES" deve ir para 1.
3. Aba Equipe → Novo papel → "Tipo de honorário" e "Base de cálculo" precisam listar opções.
4. Aba Pacotes → Novo pacote → adicionar procedimento via combobox de seleção.
5. Editar Procedimento → vincular produto via combobox.
