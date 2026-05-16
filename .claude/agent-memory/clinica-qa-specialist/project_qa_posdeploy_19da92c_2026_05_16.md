---
name: qa-posdeploy-19da92c-2026-05-16
description: Pós-deploy 19da92c (fix vazamento storage entre logins) — UI flow OK (cenários A/B/C/D), API flow ainda vaza (cenário E)
metadata:
  type: project
---

Fato: Bundle `index-7803fdba.js` (last-modified Sat, 16 May 2026 05:23 GMT) corrige o vazamento de papel/permissões entre logins **pelo caminho da UI** (botão Sair → tela login → submit). Cenários A, B, C, D passam: sessionStorage é limpo no logout (via authStore.logout() / useAuthLifecycle), localStorage `imedto-theme` persiste corretamente, papel/sidebar/cards são corretamente reidratados ao trocar de usuário.

**REGRESSÃO/GAP — cenário E (relogin sem logout)**: se um POST /api/auth/login é disparado enquanto o sessionStorage ainda tem `imedto.estabelecimentoAtivo` do user anterior (cookie expirado, refresh falhou, ou login programático), o storage stale **permanece** e a UI é renderizada com papel cruzado:
- Nome correto (novo user).
- Papel vem do storage stale (user anterior).
- Sidebar fica em estado quebrado (intersecção de permissões stale + permissões reais) — observei sidebar com apenas 1 item ("Painel inicial") + Ajuda quando Dono entrou via API com sessionStorage de Profissional ainda presente.

Why: o lifecycle de limpeza está acoplado ao `logout()` (e ao `beforeEach` da troca de rota para `/login`), não ao "início de um novo login" — então um login que ocorre sem passar pela tela de login (programático/refresh-fail) não limpa o storage stale.

How to apply: o store de auth precisa, no início de `login()`/após resposta 200 de /api/auth/login, **resetar o sessionStorage do estabelecimento ativo antes de hidratar com os dados do novo user**. Alternativa: o `me()` pós-login deve sobrescrever (ou apagar) `imedto.estabelecimentoAtivo` ao detectar mudança de `usuario.id` desde a última escrita. Investigar `frontend/src/stores/authStore.ts` e `frontend/src/composables/useAuthLifecycle.ts` (ou equivalente).

Reproduzido em produção em 16/05/2026 com: login Médico → fetch direto POST /api/auth/login com credenciais Dono → navegar /home → UI mostra "Dono Profissional".

Bundle ativo confirmado: `index-7803fdba.js`. Cookie API espera campo `password` (não `senha`) — já memorizado em [[qa-posdeploy-a53e7cc-2026-05-14]].

Veredito: 🟡 OK com ressalva — fix da UI entregue, mas vetor de relogin sem logout segue como vazamento de papel. Não é necessário reverter, mas requer follow-up.
