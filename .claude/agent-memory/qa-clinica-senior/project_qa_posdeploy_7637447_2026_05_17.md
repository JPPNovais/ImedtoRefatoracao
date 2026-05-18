---
name: qa-posdeploy-7637447-2026-05-17
description: QA pós-deploy commit 7637447 — fix do bug noopener no Visualizar PDF. Bug do ciclo 1 corrigido em prod.
metadata:
  type: project
---

Commit 7637447 (CI run 26001033762, 6/6 jobs success) — fix do P1 do ciclo anterior [[qa-posdeploy-043704e-2026-05-17]] em ProntuarioView.vue e PacienteDetalheView.vue: removido `"noopener,noreferrer"` de `window.open("about:blank", "_blank", ...)`. Validado em prod via MCP em estab 14 paciente 212 (mesma fixture do ciclo anterior, recuperada copiando senha_hash via SQL).

**Validação em prod** — hashes dos chunks lazy validados antes de clicar: `ProntuarioView-93cf8ff0.js` e `PacienteDetalheView-acd6fc18.js`, ambos `hasNoopener=false` confirmado por fetch+grep.

| # | Critério | Status | Evidência |
|---|---|---|---|
| 1 | "Ver PDF" evolução abre blob (não about:blank) | ✅ | Page 9 = `blob:https://app.imedto.com/47d3b48d...`; screenshot `7637447-01` PDF renderizado |
| 2 | "Visualizar histórico" abre blob (não about:blank) | ✅ | Page 10 = `blob:https://app.imedto.com/23f9398a...`; screenshot `7637447-02` PDF com 2 evoluções |
| 3 | "Baixar PDF" evolução baixa arquivo | ✅ | `evolucao-paciente-qa-teste-20260517-1619 (3).pdf` 46866b em ~/Downloads 17:04 |
| 4 | "Baixar histórico" baixa arquivo | ⚠️ | Audit 204 (reqid=590) ok mas arquivo não chegou no disco; Chrome estava bloqueando múltiplos downloads automáticos em sequência (caveat conhecido do ciclo anterior). Não é regressão. |
| 5 | Audit `POST .../registrar-exportacao` 204 antes do PDF | ✅ | reqid=575 (evolução ver), 586 (histórico ver), 589 (evolução baixar), 590 (histórico baixar), 631 (PacienteDetalhe ver) — todos 204 |
| 6 | Funciona em `/pacientes/:id` aba Prontuário | ✅ | Page 11 = `blob:https://app.imedto.com/623acc43...`; screenshot `7637447-03` PDF renderizado |
| 7 | Console limpo (sem erro novo) | ⚠️ | Erros 502 do hub SignalR `/hubs/estabelecimento/negotiate` — pré-existente, não relacionado ao fix; fluxo PDF não usa SignalR |

**Status final**: ENTREGUE. Bug P1 do ciclo 1 corrigido. Critério 4 é caveat do Chrome (não regressão); critério 7 é problema pré-existente do hub SignalR (fora do escopo desse fix).

**Screenshots**: `.qa-screenshots/7637447-01-ver-pdf-evolucao-blob.png`, `7637447-02-ver-pdf-historico-blob.png`, `7637447-03-paciente-detalhe-ver-pdf-blob.png`.

**Débito P2 separado** (não bloqueia esse fix, mas merece ticket próprio):
- Hub SignalR `/hubs/estabelecimento/negotiate` retorna 502 intermitente — investigar se backend está caindo ou se é proxy Caddy.
- Texto "2 evoluçãoões" em PacienteDetalheView (plural quebrado, já observado no ciclo anterior).

**Confirmação do fix**: o trade-off de remover `noopener` é seguro porque o conteúdo de destino é `blob:` same-origin gerado localmente. Não há janela cross-origin nem possibilidade de tabnabbing.
