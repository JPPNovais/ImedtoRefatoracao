---
name: project-qa-rodada4-2026-05-13
description: Re-validação P1 pós-commit 5ed930c (deploy run 25837832739) — 3/5 entregues OK, 1 bug real (acentuação LGPD), 1 melhoria (extra request PII no modal novo agendamento).
metadata:
  type: project
---

Re-validação dos 5 P1 após o deploy do commit `5ed930c` (commit "fix(p1): correções de UX, segurança e LGPD apontadas pelo QA"; pipeline run 25837832739 às 02:20 UTC). Bundle ativo em prod: `assets/index-2e2de650.js`; HTML last-modified `Thu, 14 May 2026 02:22:19 GMT`. Personas testadas: Dono (jppnovais@gmail.com / 123123123 — restaurada após teste de troca de senha).

Resultado por P1:
- P1#1 cancelar agendamento (modal 6 motivos): ✅ funciona após hard reload — bundle inicial parecia rodar `window.prompt`, mas era cache stale do MCP. Após `ignoreCache:true`, modal abre com motivos esperados.
- P1#2 trocar senha (card Segurança + modal + invalidação sessões): ✅ funciona — senha errada → "Senha atual incorreta."; troca completa OK; mensagem pós-troca clara em pt-BR.
- P1#3 LGPD UI: ❌ ACENTUAÇÃO QUEBRADA no source — `frontend/src/views/minhaConta/MinhaContaLgpdView.vue` linhas 107, 111, 160 (+ corpo) usam "Protecao", "informacoes", "anonimizacao", "voce", "prontuarios", "acao", "irreversivel", "clinicos", "serao", "historico". Bug real (não cache). Card resumo em `MinhaContaView.vue` está correto ("anonimização", "consentimentos"); só a view filha que tem o problema.
- P1#4 anti-enumeração + link reenviar confirmação: ✅ funciona — mensagem genérica "Credenciais inválidas." para senha errada e e-mail inexistente; botão "Não recebeu? Reenviar e-mail de confirmação" aparece após erro (cache stale antes induziu falso negativo).
- P1#5 autocomplete PII: ✅ parcial — boot de `/agenda` NÃO mais carrega `/api/paciente?tamanho=200`; `busca-rapida` retorna só `{id, nomeCompleto}` e cards mostram só o nome. ⚠️ resíduo: abrir modal "Novo agendamento" dispara `GET /api/paciente?pagina=1&tamanho=30` que retorna `documentoInternacional`, `dataNascimento`, `telefone`, `tags`, `qtdAlertas` (não exibidos na UI, mas trazidos do servidor — viola minimização LGPD).

**Why:** seguir como referência da próxima rodada para validar somente o LGPD acentuação + remoção da segunda chamada `/api/paciente?tamanho=30` no AbrirModalNovo da AgendaView. Não revisitar P1#1, #2, #4 — confirmados após reload limpo.

**How to apply:** próxima rodada foca em LGPD (corrigir as ~10 strings da view + considerar outras views que também podem ter o mesmo issue) e em refatorar AbrirModalNovo para usar apenas `busca-rapida` (sem pré-carregar 30 pacientes com PII completa).
