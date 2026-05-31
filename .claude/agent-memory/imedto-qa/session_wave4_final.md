---
name: session-wave4-final
description: Wave 4 admin global — pipeline fechada, commit e81ae74 pushed. CAs validados, drop tabelas Wave 2, live-link via EhPadraoSistema.
metadata:
  type: project
---

Wave 4 (briefing `planejamentos/2026-05-30_004_admin-global-wave4-catalogos-livelink.md`) pipeline fechada em 2026-05-30. Commit `e81ae74` pushed para `origin/main`.

**Why:** Descoberta que sistema já tinha live-link nativo via `eh_padrao_sistema=true` tornou as tabelas Wave 2 (`imedto_*_global`) órfãs. Wave 4 eliminou o modelo de cópia e passou para operação direta nas tabelas legado.

**How to apply:** Wave 5 começa a partir do estado de `e81ae74`. Credenciais dev: `admin@imedto.com / 123123`. Backend sobe na porta 5050 (Vite proxy aponta para 5050). Usar SPA navigation (click em links da sidebar) para preservar cookies HttpOnly.

## CAs validados

- W4-CA16 (criação região com nível inválido) → 422 "Nível inconsistente com pai." PASSOU
- W4-CA16 (criação região com vista inválida) → 422 "Vista deve ser igual à do pai." PASSOU
- W4-CA19 (exclusão região com filhos) → 422 "Esta região tem subgrupos. Inative em vez de excluir, ou remova os subgrupos primeiro." PASSOU
- W4-CA34 (docs atualizadas) → ARQUITETURA.md §"Catálogos Globais" e DESIGN.md §RegiaoTreeView presentes. PASSOU
- Live-link variáveis: criação admin aparece imediatamente via query tenant (WHERE eh_padrao_sistema=true OR estabelecimento_id=@X). PASSOU
- Live-link modelos: 4 modelos padrão sistema listados via endpoint admin. PASSOU
- RegiaoTreeView: 18 regiões nível 1 (9 Anterior, 9 Posterior) carregadas, expand/collapse funcional. PASSOU
- RBAC: /api/admin/* retorna 401 sem token. PASSOU
- Audit: motivo < 10 chars → 422 "Informe o motivo da alteração (mínimo 10 caracteres).". PASSOU
- Estados (vazio): Variáveis pool lista vazia → AppEmptyState correto. PASSOU
- Responsivo: 375px / 768px / 1280px — sem overflow horizontal. PASSOU
- LGPD: sem PII em payloads/console. PASSOU

## Gates automatizados

- Backend build: 0 erros, 0 warnings
- Backend tests: 1136 passed, 0 failed (77 skipped — integração)
- Vitest: 359 passed (42 suítes)
- Lint: falha pré-existente (dependência ESLint quebrada no ambiente local — não regressão desta wave)
- Vite build: OK

## Observações operacionais

- Vite proxy aponta para `localhost:5050` — subir backend com `ASPNETCORE_URLS="http://localhost:5050"`.
- O 500 initial era processo antigo com DLL velha — reiniciar backend resolve.
- Variável "Penicilina (QA-teste)" criada durante validação (pode ser inativada/excluída).
- Migration `20260530200000_drop_catalogos_globais_wave2.sql` ainda não aplicada em RDS — pendente para deploy.
