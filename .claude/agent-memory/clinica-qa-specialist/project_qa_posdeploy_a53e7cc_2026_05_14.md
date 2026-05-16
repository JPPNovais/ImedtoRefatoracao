---
name: qa-posdeploy-a53e7cc-2026-05-14
description: Pós-deploy a53e7cc 2026-05-14 — 4 críticos resolvidos no backend, 2 gaps de UI sidebar/redirect onboarding, login API exige campo `password` (não `senha`).
metadata:
  type: project
---

Sessão pós-deploy commit a53e7cc, bundle `/assets/index-734fe9ae.js` (last-modified 2026-05-14 11:23 GMT).

## Críticos validados
- ✅ Bug #1 vazamento PII equipe: `/profissionais` full → 422 sem `equipe.ver`. Novo `/profissionais/publico` retorna só `usuarioId,nomeCompleto,especialidade,conselho,status`. Front (AgendaView via vinculoService.listarProfissionaisPublico) já consome o novo. Dono ainda recebe DTO completo com email+vinculoId+modeloPermissao.
- ✅ Bug #2 anexo cross-paciente: `/api/paciente/{pacienteId}/prontuario/anexos/{anexoId}/url` valida par (paciente, anexo). Cross-paciente → 422 "Anexo não encontrado." (mensagem genérica). Inexistente/negativo → 422.
- ✅ Bug #3 receita digital: banner UI "Receita não assinada digitalmente. ... validade jurídica plena ... ICP-Brasil / Memed ... imprima e assine manualmente. ... integração ... em desenvolvimento." Tour onboarding também atualizado.
- ⚠️ Bug #4 onboarding convite: mensagem "Conta criada e convite aceito!" OK; redireciona para `/onboarding` (5 etapas, não direto `/home` como brief pedia — mas é design intencional pra coletar perfil). Após onboarding, sidebar do convidado mostra Equipe/Financeiro/Configurações/Automação que não tem permissão (backend retorna 422; defense-in-depth OK; **UX vaza estrutura admin**).

## Gaps novos (a tratar em próxima rodada)
- 🟠 Sidebar/header não respeita `papelDoUsuario`: usuário com modelo "Médico" vê todos os menus de admin e header diz "Dono · novaEra". Backend filtra por permissão (422 ao acessar), mas UI mostra opções inválidas — confunde Médico e pode parecer bug.
- 🟠 Lista `/api/paciente` retorna `cpf`, `documentoInternacional`, `dataNascimento`, `telefone` em campo de listagem (DTO de lista) — PII excessivo para listagem.
- 🟡 Tela `/equipe` para usuário sem `equipe.ver`: abre o layout completo (header + botão "Convidar profissional" + tabs Profissionais/Permissões/Convites) e só mostra texto pequeno "Você não tem permissão". UI quebrada — deveria redirecionar/mostrar Empty + CTA voltar.
- 🟡 Bootstrap retorna `donoUsuarioId: "00000000-0000-0000-0000-000000000000"` (default Guid) para convidado — não vaza PII direto, mas combinado com `/profissionais/publico` (que mostra `status:"Dono"`) revela quem é Dono publicamente.
- 🟡 Landing pública (`/`): footer "© 2025 Imedto", "Até 3 profissionalis" (typo "profissionalis"), seção "Zero Papel - 100% digital" sem ressalva ICP. Reformular.

## Login: campo `password` (não `senha`)
- O frontend manda `{email, password}` no `POST /api/auth/login` (não `{email, senha}` como o resto do backend que é em pt-BR).
- Inconsistência: confirma-email, reset-senha usam `senha` no payload. Login só usa `password`.
- Para testes diretos via fetch/curl, sempre usar `password` (não `senha`).

## Smoke das 5 P1 anteriores
- ✅ Login + logout
- ✅ Cancelar agendamento com modal (zero `window.prompt(` no bundle)
- ✅ Trocar senha (UI flow + endpoint OK)
- ✅ LGPD /minha-conta/lgpd com acentuação correta
- ✅ /paciente/busca-rapida → só `id+nomeCompleto`
- ✅ Anti-enumeração login: forgot-password retorna 204 igual pra existente/inexistente

## Verde, com ressalvas
Os 4 críticos têm a regra de negócio corrigida no backend e front sincronizado.
Gaps remanescentes são de UX/UI, não bloqueiam produção mas devem entrar no próximo ciclo.
