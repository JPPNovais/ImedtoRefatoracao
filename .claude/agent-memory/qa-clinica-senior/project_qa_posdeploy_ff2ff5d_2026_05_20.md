---
name: qa-posdeploy-ff2ff5d-2026-05-20
description: QA pós-fix Termos (ff2ff5d) — P0 SQL resolvido; bug novo P1 no template do EmitirTermoModal.
metadata:
  type: project
---

Deploy ff2ff5d (2026-05-20, pipeline run 26157036348 verde). Re-teste do P0 do ciclo anterior [[qa-posdeploy-837b50d-2026-05-19]].

**Status: ENTREGUE COM DÉBITO P1 — backend 100% verde; UI de aceite_link bloqueada por template não-buildado.**

## ✅ P0 resolvido (era o motivo do fix)

POST /api/pacientes/220/termos agora retorna 201 em vez de 500.

Cenários validados via fetch em produção (com header X-Estabelecimento-Id: 24):
1. `A_emitir_lgpd_sem_prof` — modelo 1 (LGPD), sem profissionalUsuarioId → 201 {termoEmitidoId, tokenAceite}.
2. `B_emitir_telemed_sem_prof` — modelo 5 (Telemedicina, usa {{profissional.*}}), sem profissionalUsuarioId → 201; snapshot com `___________` no nome do profissional (fallback OK).
3. `C_emitir_com_prof_invalido` — profissionalUsuarioId = uuid aleatório → **422 "Profissional inválido."** (mensagem genérica, defense-in-depth via PodeAtuarComoProfissional confere).
4. `D_emitir_com_prof_dono` — profissionalUsuarioId = uuid do próprio Dono (sem registro em `public.profissionais`) → 201 com fallback no snapshot (PodeAtuarComoProfissional retorna true para Dono mesmo sem profissionais, e a query do resolver INNER JOIN não acha registro → fallback). Comportamento correto.

Cenário em UI:
5. Aba "Termos" no PacienteDetalheView agora é funcional (PacienteTermosTab.vue real, não mais placeholder). Lista 3 termos com filtros por status (Todos/Pendentes/Assinados/Recusados/Revogados/Expirados), colunas modelo/categoria/status/emitido em/assinado em/ações. Modal "Visualizar termo" abre, mostra snapshot resolvido com variáveis (paciente nome+CPF resolvidos; profissional.nome em `___________`; data_atual em "20 de maio de 2026").
6. Wizard "Emitir termo" abre, 3 passos (Modelo → Preview → Confirmar). Passo 1: cards filtráveis por categoria+busca, 6 modelos disponíveis. Passo 2 (Preview): variáveis resolvem com paciente real, mostra sidebar de "Variáveis aplicadas" com badges ⚠ em fallback. Passo 3: rádio "Gerar PDF para assinatura física" (pdf_anexado) checked default — **ver bug P1 abaixo**.

Multi-tenant e LGPD:
7. `F_multitenant_outro_tenant` — tentar GET /api/pacientes/220/termos/1 com X-Estabelecimento-Id=1 → 403 "Você não tem acesso a este estabelecimento." (cookie do Dono não vincula ao tenant 1).
8. `G_modelo_inexistente` — modeloId=99999 → 422 "Modelo de termo não encontrado." (genérica).
9. `H_paciente_inexistente` — pacienteId=99999 → 422 "Paciente não encontrado." (genérica).
10. `I_pdf_url` — GET /api/termos/1/pdf antes de anexar PDF → 422 "Este termo não possui PDF anexado." (não revela existência do termo).

## ⚠ Bug NOVO descoberto via MCP — P1, não-bloqueante

**Arquivo:** `frontend/src/components/termos/EmitirTermoModal.vue` — **diff +131/-26 não foi comitado** no ff2ff5d.

**Sintoma em produção:** No passo 3 do wizard, a opção "Enviar link de aceite por e-mail" aparece com badge **"Em breve"** e está **desabilitada**, com texto "Disponível na próxima atualização". O usuário Dono/Recepção é forçado a só usar pdf_anexado pela UI.

**Causa raiz:** O commit ff2ff5d incluiu mudanças no `<script>` do modal (estado `linkGerado`/`linkCopiado`/`canalEnvioConfirmado`, watch tipoAssinatura → copia, função `copiarLink`, função `fecharAposSucesso`, contexto do passo 4 em `emitir`), mas **esqueceu as mudanças correspondentes no `<template>` e `<style>`**. O git status local pós-deploy mostra `frontend/src/components/termos/EmitirTermoModal.vue` com diff pendente (+131/-26): habilita o radio aceite_link, sub-opções email/cópia, passo 4 de "Link de aceite gerado" com botão copiar, e CSS para `.et-subop`, `.et-sucesso`, `.et-sucesso-icone`, `.et-link-box`, `.et-link-input`.

**Evidência:**
- Hash do chunk em prod: `PacienteDetalheView-232ea22b.js` (50.83 kB).
- Hash do chunk em build local (com diff pendente): `PacienteDetalheView-f551f49f.js` (54.41 kB).
- Bundle de prod tem `"Em breve"`, `"Apenas copiar link"` no script (lógica nova), MAS template renderizado é o antigo (`disabled`, sem passo 4).

**Impacto operacional:**
- Quem usa o app via UI **não consegue** emitir termo por aceite_link — só pdf_anexado. Ou seja, o fluxo novo "aceite digital" está invisível.
- API `/api/pacientes/{id}/termos` aceita normalmente aceite_link (validado nos cenários A/B). Quem usa via integração externa não tem o problema.
- Smoke regressões: pdf_anexado funciona end-to-end. Fluxos das outras features (orçamento atalho, estoque) intactos.

**Correção:** Commitar o diff de `EmitirTermoModal.vue` que está em working tree e fazer push. Sem mudança de schema, sem mudança de API. Re-validar passo 3 + passo 4 (link copiado) via MCP.

## Aceite Público (`/termos/aceite/:token`) — débito conhecido

Rota frontend existe (`AceiteTermoPublicoView.vue`), service existe (`termoAceitePublicoService.ts`). Mas o backend `TermoPublicoController` ainda é stub 501. Validação da rota pública não é regressão — é Fase 4 pendente. Pula re-teste.

## Conta QA usada (mesma do ciclo anterior)
- email `qa.termos.1779242290094@imedto.local`, password `QaTermos!2026#X`, usuário uuid `d58452c4-7e43-4d0a-9c96-f4c70322ab3e`, estab 24 "QA Termos Estab", paciente 220.
- Conta segue ativa, 5 dias após o cadastro. Não precisou refazer signup.

## Outros débitos (carry-over, não-bloqueante)
- Frontend de Modelos de Termos no nível de Configurações (CRUD) ainda não existe — só listagem/clone do drawer de emissão.
- Reenvio de link (`/api/termos/{id}/reenviar-link`) só faz sentido depois que aceite_link aparecer na UI (P1 acima).
- Stubs 501 (`/api/publico/termos/aceite`, `/api/termos/{id}/pdf-gerado`) seguem como ruído no swagger.

## Aprendizados (já registrados em feedback)
- [[feedback_inspecionar_working_tree_inteiro_antes_commit]] aplica aqui: o autor do fix ff2ff5d não rodou `git status` completo antes do commit — ficou o diff do template/style fora. Memória já existente confirma a importância dessa checagem.
- Hash divergente de chunk lazy entre build local e prod é um sinal de código pendente (mesma armadilha de [[feedback_validar_chunk_lazy_em_prod]]).
