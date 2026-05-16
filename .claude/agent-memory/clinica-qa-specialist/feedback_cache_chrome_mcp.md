---
name: feedback-cache-chrome-mcp
description: Chrome MCP cacheia bundles JS entre sessões; sempre use query string ?nocache=N + ignoreCache=true ao validar deploys recentes.
metadata:
  type: feedback
---

Ao re-validar deploys frescos via Chrome MCP, NÃO basta `navigate_page` reload — o MCP pode servir bundles antigos do cache HTTP (testado 2026-05-13: bundle `index-8e2b97c4.js` carregou no browser mesmo já tendo sido substituído em prod por `index-2e2de650.js`; o asset 8e2b97c4 retornava 404 ao re-fetch). Sintomas: features aparecem "quebradas" mas o source local tem a correção e o chunk em prod confirma a presença das strings.

**Why:** durante a rodada 4 (P1 re-validation 2026-05-13), 3 P1s pareceram quebradas inicialmente (`window.prompt` ativo, telefone exposto, link reenviar ausente) — todas resolveram após `navigate_page` com `ignoreCache:true` e `?nocache=N`. O culpado era cache em memória da página, não código.

**How to apply:**
1. Antes de validar P1/correção recente, comparar bundle servido pelo browser (`document.scripts`) com hash em `https://app.imedto.com/` raw HTML.
2. Sempre adicionar `?nocache=<timestamp>` ao path e `ignoreCache: true` no `navigate_page`.
3. Limpar `window.caches` no início via `caches.keys()→delete`.
4. Se `fetch(scriptUrl)` retornar 404 mas o script carregou 200, é stale — recarregar com cache busting.
