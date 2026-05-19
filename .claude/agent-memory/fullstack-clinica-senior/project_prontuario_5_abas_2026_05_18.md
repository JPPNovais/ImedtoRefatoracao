---
name: prontuario-5-abas-2026-05-18
description: Refactor 2026-05-18 — prontuário passou a ter 5 abas (consulta/anteriores/receitas/atestado/pedidos-exame); aba "exame" sumiu.
metadata:
  type: project
---

Refactor do prontuário em 2026-05-18 (PO ratificou todas as decisões).

**O que mudou:**
1. **Aba "Exame físico" foi removida** — `ExameFisicoTab.vue` apagado. O mapa corporal + regiões anatômicas (BodyMap + RegionSelectorPopup + RegionExamCard) agora vivem dentro de `SecaoExameFisico.vue` (chave `exame-fisico` do modelo de prontuário) — sinais vitais, antropometria, ectoscopia, mapa corporal e regiões coexistem na mesma seção.
2. **Aba "Atestado" criada** — domínio `Atestados` (aggregate `Atestado` + `ModeloAtestado`). Endpoints `POST /api/pacientes/{id}/atestados`, `GET /api/pacientes/{id}/atestados`, `GET /api/atestados/{id}`, `GET/POST/PUT/DELETE /api/modelos-atestado/...`. PDF no front via `useAtestadoPdf.ts`.
3. **Aba "Pedidos de exame" criada** — domínio `PedidosExame` (MVP — sem modelos). `POST/GET /api/pacientes/{id}/pedidos-exame`, `GET /api/pedidos-exame/{id}`. Lista de exames persistida como `jsonb`. PDF via `usePedidoExamePdf.ts`.

**Wiring crítico exame físico:**
- `POST /api/paciente/{id}/prontuario/evolucoes` agora retorna `{ evolucaoId }` no body 201 (antes era body vazio).
- `prontuarioService.registrarEvolucao` foi alterado para retornar `{ evolucaoId }`.
- `ProntuarioView.salvarEvolucao()` usa esse id para encadear `exameFisicoService.registrar(evolucaoId, {...})` quando `novaEvolucao["exame-fisico"].regioes` está preenchido.

**Why:**
- PO quis a UX rica de mapa corporal sempre disponível como parte da consulta, sem precisar trocar de aba.
- Atestados e pedidos de exame estavam ausentes do sistema atual — produto saúde precisa emitir esses documentos.

**How to apply:**
- Não recriar a aba "exame" no `ProntuarioTabs.vue` — ela foi removida intencionalmente.
- Ao mexer em `SecaoExameFisico`, lembrar que ela tem 4 estados: sinais vitais textuais + antropometria + ectoscopia + (mapa corporal interativo + regiões anatômicas). Os 3 primeiros viram JSON em `conteudo["exame-fisico"]`; o 4o vai para domínio dedicado via `exameFisicoService.registrar(evolucaoId, ...)`.
- Migrações: `20260518182322_criar_atestados_e_pedidos_exame.sql` cria `atestados`, `modelos_atestado`, `pedidos_exame`.
- Para emitir atestado/pedido de exame, padrão é "abrir `about:blank` ANTES do `await` (anti popup blocker) → emitir → buscar com `profissionalNome` via `obter(id)` → gerar PDF e setar `janela.location.href = blobUrl`". Fallback download se popup bloqueado.

Decisões intencionalmente NÃO implementadas (MVP):
- Pedido de exame não tem modelos (MVP — pediu o PO).
- Atestado de afastamento valida `dias > 0` e `<= 365` no aggregate; CID-10 regex `^[A-TV-Z]\d{2}(\.\d)?$`.
- Sem `[FeatureGate]` em Atestado/PedidoExame — não foram modelados como features pagas.
