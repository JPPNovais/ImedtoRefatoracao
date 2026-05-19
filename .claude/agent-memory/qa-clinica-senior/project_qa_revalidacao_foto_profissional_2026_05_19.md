---
name: qa-revalidacao-foto-profissional-2026-05-19
description: Revalidação da feature foto do profissional (commits 04031d2 + a6b82ca). 10/10 fluxos verde em prod, 3 débitos P1/P2/P3 não-bloqueantes.
metadata:
  type: project
---

Feature já estava commitada em `04031d2` (RemoverFotoProfissional + AppAvatar) + `a6b82ca` (AppAvatarSelect + agenda) e o pipeline `26073413443` passou em verde antes desta sessão de QA — então não houve commit/push aqui, apenas validação em produção via MCP.

**Conta descartável:** `qa-foto-2026@imedto.dev` criada via signup + RDS UPDATE (cf. [[qa-login-via-signup-e-rds-confirm]]), CPF/onboarding/estab/assinatura Enterprise via SQL, **tudo limpo no fim** (DELETE em cascata + tunnel SSH fechado).

**10 fluxos golden — todos OK em prod:**
1. Login OK.
2. Upload PNG 300×300 (gerado via canvas + DataTransfer no input file) → `PUT /api/profissional/me/foto` 200, S3 presigned URL aparece sem refresh.
3. Remover foto → `DELETE /api/profissional/me/foto` 204, volta para iniciais "QF".
4. Validação client-side: arquivo 3MB → "Máximo 2 MB" inline; PDF → "Formato não suportado". Nenhuma request à API em casos inválidos.
5. AbaProfissionais com avatar real (presigned URL renderiza ok).
6. ProfissionalDetalhesModal header mostra foto.
7. Step 2 do NovoAgendamentoModal: `AppAvatarSelect` mostra avatar+nome+especialidade no trigger e no listbox.
8. EditarAgendamentoModal: idem (avatar pré-selecionado no profissional existente).
9. Filtro `AppAvatarSelect` na AgendaView: abre listbox com avatar nas opções; selecionar muda trigger; "Todos" (permite-limpar) restaura placeholder.
10. AgendamentoRow exibe avatar pequeno com foto do profissional.

**Edge case validado:** remover foto via MinhaConta → navegar para Agenda → AgendamentoRow refletiu **imediatamente** o avatar de iniciais "QF" sem refresh. Reatividade Pinia + `LEFT JOIN ... deletado_em IS NULL` funcionando ponta-a-ponta.

**Débitos não-bloqueantes (melhoria, não devolução):**
- **P1 — `confirm()` nativo em `aoRemoverFoto`** ([MinhaContaView.vue:164](frontend/src/views/minhaConta/MinhaContaView.vue#L164)). Viola padrão do design system; o commit b39f12b já corrigiu o mesmo problema em outras telas. Substituir por `AppConfirmDialog`.
- **P2 — a11y label duplicado no AppAvatarSelect**. `AppAvatar` usa `title={nome}` que vira aria-name na div; o `<b>{{nome}}</b>` do label do AppAvatarSelect repete o nome → screen reader narra "QA Foto QA Foto Clínica Geral". Solução: `aria-hidden="true"` no avatar dentro de option/trigger, ou remover `title` quando aninhado em listbox.
- **P3 — fallback do Dono no AgendaView** ([AgendaView.vue:92-98](frontend/src/views/agenda/AgendaView.vue#L92)) não passa `fotoUrl`. Se a borda dispara (lista pública sem Dono), foto não aparece no fallback. Adicionar `fotoUrl: perfilProprio.value?.fotoUrl ?? null`.

**Padrão validado para futuras revisões:** filter global `UnitOfWorkFilter` em [Program.cs:166](backend/src/Services/Imedto.Backend.API/Program.cs#L166) faz `SaveChangesAsync` automático após action. Por isso `ProfissionalRepository.Salvar` não chama SaveChanges direto — handlers funcionam sem `[UnitOfWork]` explícito. Ao revisar handler de aggregate, **não** marcar como bug se faltar SaveChanges; é responsabilidade do filter.

**Console em prod:** apenas erros de SignalR WebSocket (`Connection disconnected`/`ERR_CONNECTION_REFUSED` no `/hubs/estabelecimento`) — não relacionados à feature de foto.
