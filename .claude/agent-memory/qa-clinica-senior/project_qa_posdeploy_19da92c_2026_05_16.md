---
name: qa-posdeploy-19da92c-2026-05-16
description: QA pós-deploy commit 19da92c (redesign PDF + hotfix LGPD authStore) — pipeline verde, bundle deployado, hotfix authStore confirmado no bundle; teste E2E logado pendente por falta de credenciais.
metadata:
  type: project
---

Commit `19da92c` (PDF redesign + authStore session-cleanup hotfix) pusheado em 2026-05-16 e pipeline rodou success em todos os 6 jobs (test-backend, test-frontend, build-push, migrate, deploy, smoke). Tempo ~5min.

**Validado via Chrome DevTools MCP em prod (sem login):**
- Bundle `index-7803fdba.js` deployado (last-mod Sat, 16 May 2026 05:23:27 GMT).
- Chunk lazy `usePdfHeader-bebed171.js` (hash diferente do local `b06ffe0d`) HTTP 200 com `desenharWatermark`, `Nunito`, `ICP-Brasil`, `PDF_THEME` presentes.
- `ProntuarioView-29f61049.js` faz `import("./usePdfHeader-bebed171.js")` corretamente — dynamic import preservado.
- Hotfix LGPD authStore confirmado: `imedto.atendimento_ativo`, `imedto.receitas.v1`, `limparSessao`, `useUpsellStore` presentes no `index.js` minificado.
- Endpoint `/api/receitas/{id}/pdf` retorna 401 sem cookie (não 500) — auth middleware blindado, sem vazamento de existência.
- Tentativas com ID negativo/zero/string/bigint → 401 ou 404, zero 500. Resiliente.

**Pendente — teste E2E logado:**
QA não dispõe de senha da conta de teste (`jppnovais@gmail.com` é o user de QA registrado nas memórias do `clinica-qa-specialist`, mas a senha não está em memória). Sem credencial não consigo:
- Abrir paciente com receita real e baixar PDF para inspeção visual.
- Validar marca d'água, layout do header e variante vermelha de receita controlada.
- Confirmar que `assinadoDigitalmente` só pinta com status real (já validado por código, mas não em prod).

**Why:** PDF é o entregável principal; inspeção visual em PDF gerado de paciente real é a única forma de pegar regressões visuais (overflow, fonte que não renderizou, watermark muito visível, etc).

**How to apply:**
- Próxima sessão: pedir senha de teste antes de começar QA, OU criar `.env.qa` local com credencial mockada num tenant de QA isolado, OU configurar uma conta `qa-readonly@imedto.com` com receitas pré-criadas para inspeção.
- Persistir o login na isolatedContext do Chrome DevTools MCP para reutilizar entre sessões.

**Backlog identificado (não-bloqueante):**
- `useRelatorioPdf` ficou sem caller ativo após o redesign — esperado pelo fullstack, mas precisa de ticket para integrar com a próxima `RelatoriosView`.
- QuestPdfReceitaService renderiza placeholder com iniciais em vez de baixar logo S3 — TODO documentado no código, decisão consciente do fullstack.
- Marca d'água via `setGState({opacity})` cai silenciosamente se o build do jsPDF não expuser `GState` — fallback existe mas sem teste em prod até inspeção visual.

**Lição de processo:** commit do redesign saiu misturado com hotfix authStore num único commit (`19da92c`) — surgical changes quebrado. Ver [[split-commits-quando-mistura-topicos]].
