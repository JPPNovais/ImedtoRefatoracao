---
name: qa-posdeploy-8ad2f2c-2026-05-16
description: Pós-deploy commit 8ad2f2c — Cenário E (relogin sem logout via fetch direto) ainda vaza papel stale; UI fica em estado misto e sessionStorage continua "Profissional" mesmo após cookie virar do Dono.
metadata:
  type: project
---

Pós-deploy `8ad2f2c` / bundle `index-285580ca.js` (sa-east-1, 2026-05-16). Tentativa de defense-in-depth no `hidratarUsuario` para detectar mudança de id do usuário entre access-token e store local.

**Resultado**:
- Cenários A (Dono→logout UI→login Médico), B (login direto Médico), C (Médico→logout UI→login Dono), D (theme persiste / atendimento_ativo limpo): todos OK sem regressão.
- **Cenário E (relogin sem logout via fetch direto `/api/auth/login`)**: ainda quebrado, mas de jeito diferente da rodada anterior. O fix detectou que o id mudou (Profissional → Dono) e capou o estado, mas não restaurou o papel correto. Estado misto persistente:
  - Header com nome do novo usuário ("JOAO PAULO PEREIRA NOVAIS" — Dono)
  - Badge ainda com "Profissional · novaEra" (papel stale do Médico)
  - Sidebar reduzida a 1 item ("Painel inicial" + Ajuda) em vez dos 10 esperados do Dono
  - `sessionStorage.imedto.estabelecimentoAtivo` permanece `{papel: "Profissional", permissoes: []}`
  - `/api/auth/bootstrap` retorna corretamente `papelDoUsuario: "Dono"` mas o front não consome
  - Mesmo após `location.reload()` (init completo), comportamento idêntico
  - Router-guard ainda funciona: `/financeiro` bloqueia com toast "Esta área é restrita ao seu papel."

**Why**: o vetor de ataque é real (atacante que rouba cookie e injeta auth direto sem passar pelo logout). Embora o backend valide tudo, a UI mostrar nome do usuário A com papel/permissões do B é desorientador e ainda quebra a UX a ponto de impedir uso.

**How to apply**: o fix `hidratarUsuario` precisa, ao detectar id divergente, **forçar `limparSessao()` completa e re-resolver `estabelecimentoAtivo` a partir do `/api/auth/bootstrap`** — não apenas zerar permissoes mantendo papel antigo. Ideal: detectar mudança via `useStateChangeListener` no `recarregarMe()` e disparar fluxo completo: clear sessionStorage → setar estabelecimentoAtivo do bootstrap → re-renderizar layout.

Severidade: 🟠 Alto. Não bloqueia operação normal (caminhos A-D OK), mas o vazamento de papel stale no cenário E permanece — apenas mitigado, não fechado.

Bundle hash: `index-285580ca.js`. Veja [[qa-posdeploy-19da92c-2026-05-16]] para o estado anterior do Cenário E.
