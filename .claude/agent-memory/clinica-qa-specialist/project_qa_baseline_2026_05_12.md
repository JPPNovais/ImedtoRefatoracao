---
name: qa-baseline-2026-05-12
description: Estado funcional da plataforma Imedto em prod (app.imedto.com) capturado na varredura QA de 12/05/2026 — escopo, achados estruturais e expectativas para próximas rodadas.
metadata:
  type: project
---

Varredura completa de QA em prod feita em 2026-05-12 (sessão de ~75 min, persona Dono `jppnovais@gmail.com`).

**Cobertura confirmada como já implementada e funcional (não regredir):**
- Auth/onboarding: signup, confirmação de e-mail obrigatória, login com cooldown de reenvio de 5 min, forgot-password com 204 anti-enumeração, BFF com cookies HttpOnly (não tem token em JS, só `imedto-theme` em localStorage e `imedto.estabelecimentoAtivo` em sessionStorage).
- Multi-tenant: header `X-Estabelecimento-Id` obrigatório, retorno `TenantAusente` quando ausente, `SemAcesso` 403 ao tentar tenant alheio.
- Agenda: novo agendamento com stepper 3 passos, disponibilidade com slots ocupados marcados, validação de conflito no backend (422 `ErroDeNegocio`), lista de espera, ocupação do dia, lembretes WhatsApp/e-mail.
- Prontuário: anamnese estruturada extensa (QP, HDA, HPP, exame físico com sinais vitais/antropometria/ectoscopia, CID-10, conduta), 4 modelos de sistema, troca de modelo, salvar evolução, linha do tempo, anexos via presigned S3.
- Equipe: convite por e-mail/WhatsApp, permissões sistema (Admin/Médico/Recepção) + custom, Dono não inativável, especialidades.
- Endpoints LGPD que **existem mas não têm UI**: `GET /api/minha-conta/exportar-dados` (200) e `DELETE /api/minha-conta` (precisa body) — discoverability quebrada.

**Endpoints que NÃO existem e deveriam existir (gap funcional):**
- Trocar senha do usuário autenticado: testados `/api/auth/senha`, `/api/auth/alterar-senha`, `/api/conta/senha`, `/api/minha-conta/alterar-senha`, `/api/minha-conta/senha`, `/api/usuario/senha` — todos 404. Único caminho disponível é "esqueci a senha".

**Anti-padrões críticos confirmados em prod:**
- `window.prompt()` nativo é usado para "Motivo do cancelamento" de agendamento na agenda — totalmente fora do design system.
- Listagem de pacientes da agenda usa `paciente?pagina=1&tamanho=200` (capada em 200) — quebra em estabelecimento real.
- DTO de listagem de pacientes retorna `cpf`, `dataNascimento`, `documentoInternacional`, `telefone` mesmo para listagem (violação de minimização LGPD).
- DTO `/api/estabelecimento/{id}/profissionais` retorna `email` de cada profissional para qualquer membro do tenant.
- Telefone exibido na lista/detalhe sem máscara (raw "11988887777") enquanto o cadastro mostra com máscara.
- "NaoInformado" (PascalCase do enum) vazado no header do prontuário; "evoluçãoões" typo no detalhe do paciente.

**Multi-tenant — sessionStorage não é fonte de verdade:**
- Manipular `imedto.estabelecimentoAtivo` (papel/permissões) afeta UI do front (menu lateral some, header muda) mas backend continua validando via cookie HttpOnly. Confirmado: tentar `/equipe` redireciona para `/home` (router guard). Backend OK.

**Higiene de console: limpo.** Apenas 2 issues estruturais detectadas (a11y de form sem id/label).

**Performance percebida:** todas as páginas abriram em <1s, requests visíveis: home faz 5 requests (dashboard, contador-notif, minha-assinatura, bootstrap, hub negotiate). Sem N+1 aparente.

**Screenshots da sessão:** `.qa-screenshots/` (gitignored se for o caso) — 01 a 23, cobrindo landing, signup, login, dashboard, agenda, prontuário, pacientes, equipe, permissões, financeiro, orçamentos, inventário, relatórios, automações, mobile.

Quando voltar a testar, focar em: criação real de convite + aceitação por outro usuário (não testado), troca de estabelecimento (Dono só tem 1), receita digital, módulos de IA/automação, módulos de funcionamento/repartições/unidades, fluxo de no-show + cobrança, conflito de agendamento com bloqueio de horário (feature não vista).
