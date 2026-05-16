---
name: qa-posdeploy-4cc451e-2026-05-16
description: QA pós-deploy commit 4cc451e — 3 itens validados; bug crítico de sessionStorage estabelecimentoAtivo não limpo entre logins (vazamento de papel/permissões).
metadata:
  type: project
---

Deploy 4cc451e em prod (bundle `index-4f8220b1.js`, last-mod `Sat, 16 May 2026 04:59 GMT`). 3 melhorias validadas:

**Item 1 — Cards Home filtrados por papel** ✅ OK
- Dono: 6 cards (Agenda, Pacientes, Financeiro, Orçamentos, Inventário, Relatórios)
- Profissional: 4 cards (Agenda, Pacientes, Orçamentos, Relatórios) — sem Financeiro/Inventário.
- Sidebar e cards usam mesma lógica de filtragem.
- Gap menor: Home não traz cards de "Equipe" nem "Automação" mesmo para Dono (sidebar tem). Provavelmente intencional (cards são "atalhos de operação diária").

**Item 2 — Toast em redirect por permissão** ✅ OK
- Toast `.toast.toast-info` "Esta área é restrita ao seu papel." aparece em /financeiro, /equipe, /inventario, /configuracoes/ia, /automacoes quando Profissional digita URL.
- URL final = /home (sem `?bloqueado=` resíduo).
- Dono em /home direto: nenhum toast.
- TTL do toast curto; capturar via MutationObserver, não snapshot atrasado.

**Item 3 — donoUsuarioId removido do bootstrap** ✅ OK
- `/api/auth/bootstrap` response NÃO traz mais `donoUsuarioId` em `estabelecimentos[]`. Confirmado para Dono e Profissional.
- Keys restantes: id, nomeFantasia, razaoSocial, cnpj, telefone, endereco, fotoUrl, status, criadoEm, papelDoUsuario, horarioInicio, horarioFim, duracaoConsultaPadraoMinutos, intervaloEntreConsultasMinutos, diasSemanaFuncionamento, horariosBloqueados, datasBloqueadas, permissoes, permissoesExtras.

**Bug NOVO crítico descoberto — sessionStorage `imedto.estabelecimentoAtivo` não limpa entre logins**
- **Why**: Ao logar com Conta A (Dono), o sessionStorage guarda `{papel:"Dono", permissoes:[...]}`. Logout e login com Conta B (Profissional) NÃO limpa esse cache → header mostra "Dono · novaEra" para Conta B, sidebar mostra TODAS as rotas, cards Home traz todos os 6, router-guard NÃO redireciona. Backend continua negando 422, mas defense-in-depth do front quebra.
- **Como aplicar**: Validar que `authStore.logout()` (e/ou `authStore.login()`) chamem `sessionStorage.removeItem('imedto.estabelecimentoAtivo')`. Buscar onde sessionStorage é hidratado: provavelmente em useEstabelecimentoAtivo composable / estabelecimentoStore.
- **Reprodução**: 1) Login Dono jppnovais. 2) Logout API. 3) Login Profissional (qa-imedto-convite-2026+1). 4) /home → badge "Dono", sidebar/cards completos. Limpar sessionStorage e recarregar resolve. 
- **Risco LGPD**: alto. Em browser compartilhado (recepção da clínica), próximo usuário "vê" navegação como se fosse o anterior, pelo menos até o `papelDoUsuario` do bootstrap chegar e reidratar. Pode levar a click em rota privilegiada e acidentalmente exfiltrar 422 com mensagens (baixo) ou simplesmente trair que o estabelecimento tem ou não tal módulo.

**Smoke 12 anteriores — sem regressão**
- Sidebar Profissional (6 itens) ✅
- LGPD acentuação ✅
- Anti-enumeração forgot/reenviar (204) ✅
- /minha-conta "Trocar senha" presente ✅
- Modal novo agendamento usa /api/paciente/busca-rapida?limite=8 retornando só id+nomeCompleto ✅ (sem regressão da rodada 4)
- /orcamentos sem pré-carga /api/paciente ✅
- Rate-limit 429 após múltiplas tentativas de login ✅
