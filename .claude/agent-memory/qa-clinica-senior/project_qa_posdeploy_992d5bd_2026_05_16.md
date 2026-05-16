---
name: qa-posdeploy-992d5bd-2026-05-16
description: QA pós-deploy 992d5bd em /configuracoes/orcamento (6 abas) — funciona end-to-end com 1 P1 (alert/confirm nativo) + 1 LGPD (telefone na listagem de anestesistas).
metadata:
  type: project
---

QA pós-deploy run `25965235667` (commit `992d5bd` "subindo memoria claude" — só docs, código já estava em produção pelos commits `a55ec79`, `48543c3`, `ad53a55`).

**Pipeline:** completed success em 9m14s.

**Cenários OK em produção (https://app.imedto.com como Dono · QA Deep Estab id=9):**
- Descoberta: botão "Configurações" no header de /orcamentos navega via SPA. URL direta /configuracoes/orcamento OK. Menu lateral SEM entrada dedicada (alinhado a Estoque).
- `app-page--wide` com max-width 1480px confirmado via CSS.
- 6 abas com badge de contagem na aba ativa (lazy-load: contagem das outras só aparece quando ativadas). Querystring `?aba=` persiste em F5.
- CRUD Procedimentos / Produtos / Equipe / Anestesistas / Pacotes funcional. Avatares com iniciais (Dra./Enf./Téc. removidos).
- Anestesista replace-all de faixas funciona (3 → remove 1 + add 2 → 4 faixas reidratam corretamente). Cálculo de média correto.
- Pacote com chips (1 procs/1 produtos/1 papéis/anestesista) + valor formatado.
- **Validação backend**: tipo de produto vazio → 422 "Tipo do produto é obrigatório." ✓
- **Bloqueio referencial**: tentar inativar procedimento em uso por pacote ativo → 422 "Procedimento está em uso por um ou mais pacotes ativos. Desative o pacote primeiro." ✓
- **Multi-tenant**: GET com `X-Estabelecimento-Id: 999` → 404 "Estabelecimento não encontrado." (mensagem genérica) ✓
- **Regressão**: /orcamentos lista e drawer "Novo orçamento" abrem sem erro.
- Console limpo (zero warn/error).

**P1 — alert()/confirm() nativos em vez de design system:**
- Inativar (procedimentos/produtos/equipe/anestesistas/pacotes) usa `window.confirm()` nativo.
- Erros de negócio (422 do back) usam `window.alert()` em vez de `AppToast`/`AppNotification`.
- Validação client-side (ex: "Descrição é obrigatória.") também via `alert()`.
- Quebra padrão visual do produto. Anti-padrão em apps modernos.
- **How to fix**: trocar por `useToast()` (sucesso/erro) + `AppConfirmDialog` (confirmação destrutiva) do design system.

**P1 — LGPD minimização: telefone vaza na listagem de anestesistas:**
- `GET /api/orcamentos/configuracoes/anestesistas` retorna campo `telefone` no payload.
- Tela de listagem mostra só nome / especialidade / CRM / tabela / faixas. Telefone só aparece no drawer de edição (detalhe).
- CLAUDE.md: "query/DTO retorna apenas os campos que a tela usa". Violação de minimização.
- **How to fix**: separar `AnestesistaListaDto` (sem telefone) de `AnestesistaDetalheDto` (com telefone) ou criar endpoint `GET /api/orcamentos/configuracoes/anestesistas/{id}` para detalhe.

**P2 — botões "Importar planilha" / "Exportar" mudos:**
- Cliques não emitem feedback algum (sem toast "Em breve", sem disable, sem nada). Usuário não sabe se clicou.
- **How to fix**: ou implementar o stub com toast "Em breve", ou esconder o botão até a feature estar pronta.

**P2 — sem campo "Nome" e sem campo "Descrição longa" em Procedimento:**
- Drawer só tem "Descrição" (1 linha) como nome. Não há campo livre para detalhes do procedimento.
- Roteiro descrevia ambos. Decisão de produto — pode estar OK.

**P2 — Pacote: textos "1 procs / 1 produtos / 1 papéis":**
- "procs" abreviado; "produtos/papéis" com `s` mesmo no singular. Polimento de copy: pluralizar dinamicamente.

**Não testados (out of scope desta rodada):**
- Permissão de papel não-Dono (não conseguimos trocar de papel sem mais setup).
- Edição de procedimento vinculando produto (testaria reload completo do drawer).

**Status final:** ⚠️ **pronto com débitos catalogados** — funciona end-to-end para a gestora, mas precisa polir (alert/confirm + LGPD anestesista) antes de declarar production-ready para clientes pagos.

**How to apply:** Quando consertarem o alert/confirm + LGPD anestesista, reabrir a aba Anestesistas e re-validar payload de listagem; revalidar `window.alert/confirm` chamando inativar de cada entidade e validando ausência destes.
