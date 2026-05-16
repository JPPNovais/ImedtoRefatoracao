---
name: validar-chunk-lazy-em-prod
description: Para validar deploy de feature com chunks lazy (Vite dynamic import), procurar o chunk pelo hash dentro do bundle pai — hashes diferem entre builds local e Docker.
metadata:
  type: feedback
---

Quando uma feature adiciona um chunk lazy via `await import("@/composables/...")`, o hash do chunk **será diferente entre o build local (`npm run build`) e o build em produção (Docker)**. Não tentar fetchar o chunk pelo hash que apareceu localmente — vai dar 404.

**Why:** o Vite gera hashes a partir do conteúdo + dependências resolvidas. Pequenas diferenças no ambiente (versão de node, plugins) mudam o hash mesmo com código idêntico. Em 2026-05-16, build local gerou `usePdfHeader-b06ffe0d.js` mas prod gerou `usePdfHeader-bebed171.js` para o mesmo commit (19da92c).

**How to apply (validar lazy chunk em prod sem login):**
1. Buscar o HTML root: `fetch("/").then(r => r.text())` → extrair `index-XXX.js`.
2. Buscar o bundle pai mais provável (ex: `ProntuarioView-XXX.js`): `fetch("/assets/" + indexJs)` → buscar `matchAll(/import\s*\(\s*["']([^"']+)["']\s*\)/g)` ou `matchAll(/usePdfHeader-[\w]+\.js/)`.
3. Confirmar HTTP 200 + content-type `application/javascript`.
4. Inspecionar o conteúdo do chunk para marcadores semânticos da feature (ex: `desenharWatermark`, `Nunito`, `ICP-Brasil`, `PDF_THEME`) — assim você valida que é o build com o redesign novo, não cache antigo.

Bonus: se o chunk usar `_resetPdfHeaderCacheParaTestes` ou função `@internal` similar, ela vira marcador único — útil pra confirmar versão.
