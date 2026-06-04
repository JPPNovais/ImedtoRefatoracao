---
name: feedback_eslint_preexistente
description: Erro de lint @typescript-eslint/recommended é pré-existente no ambiente — não bloquear validação por isso
metadata:
  type: feedback
---

O ESLint no frontend falha com "ESLint couldn't find the config '@typescript-eslint/recommended'" desde antes
desta entrega. O arquivo `.eslintrc.cjs` não é tocado pela maioria das features.

**Why:** Problema de versão de pacote npm no ambiente de desenvolvimento, não de código da feature.

**How to apply:** Ao rodar `npm run lint` e ver esse erro, confirmar que `.eslintrc.cjs` não foi modificado
na entrega. Se não foi, registrar como "lint: FALHA PRÉ-EXISTENTE" e não bloquear o QA gate por isso.
Typecheck via `vue-tsc` (embutido no `npm run build`) é o check relevante de tipos.
