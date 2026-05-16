---
name: qa-rodada2-2026-05-13
description: Rodada 2 de QA prod (app.imedto.com) — convite, permissões, receita, anexos, lista de espera, performance e teleconsulta. Achados que devem virar regressão.
metadata:
  type: project
---

Sessão 2 (~75 min) em 2026-05-13, focada nos gaps deixados pela [[qa-baseline-2026-05-12]].

**Implementado e validado (não regredir):**
- Convite por e-mail entrega via Resend de noreply@imedto.com. Link `app.imedto.com/auth/aceitar-convite?token=<urlsafe-base64>`. TTL 7 dias documentado no corpo do e-mail.
- Tela `/auth/aceitar-convite` aceita e-mail + nova senha → cria conta + auto-login. Backend `POST /api/auth/aceitar-convite` retorna 200.
- Onboarding em 3 etapas: Sua conta (CPF/telefone), Especialidade (profissão+especialidades+conselho+tipos), Tour. Salva via `PATCH /api/onboarding/*` (provavelmente).
- Lista de espera funcional: toggle "Adicionar à lista de espera" abre campos "Preferência de período" (Manhã/Tarde/Qualquer) + "Urgência" (Rotina/Prioritário/Urgente). Aparece no widget lateral com botão Encaixar/Remover.
- Performance backend: criar 200 pacientes em paralelo (concorrência 10) levou 1.5s (~7.5ms/req). Listagem com debounce (1 request única para `busca=Volume+1`).
- Multi-tenant cross-tenant continua blindado: `/api/estabelecimento/{id}/profissionais` com tenant alheio → 403 `SemAcesso`; tenant inexistente → 404.
- HttpOnly cookies funcionam (`document.cookie` vazio). Refresh sem cookie → 401 `{"mensagem":"Sessão não encontrada."}`.
- Backend impede múltiplos estabelecimentos por dono: `POST /api/estabelecimento` → 422 "Cada usuário pode ter apenas um."

**Achados críticos novos (precisam de fix):**

1. **`GET /api/estabelecimento/{id}/profissionais` retorna lista detalhada para Médico simples** (sem `[RequiresPapel]`). Vaza `email` de todos os colegas + `usuarioId` + `modeloPermissaoId` + datas de convite. **LGPD + permissão**. Esperado: 403 ou DTO mínimo (só nome + especialidade + profissão).

2. **Tour de onboarding mente**: ao final mostra "Tudo pronto, Dra 🎉" + botão "Acessar minha agenda", mas `/home` mostra "Você ainda não está vinculado a um estabelecimento". O sistema fechou o onboarding antes de o convite ter sido aceito. Fluxo deveria automaticamente redirecionar para `/meus-convites` ou pré-aceitar o convite quando o convidado completa o onboarding.

3. **Tela de aceitar-convite mente também**: ao criar conta exibe "Conta criada e convite aceito!" mas o convite ainda está pendente — precisa clicar em "Ver meus convites" e aceitar de novo. Texto enganoso.

4. **Receita digital ≠ assinada digitalmente**: tour de onboarding promete "Receituário e atestados assinados digitalmente, válidos em todo o território nacional". Realidade: ao finalizar receita, abre nova aba com HTML simples + linha "___ Assinatura" tradicional. Sem ICP-Brasil/gov.br/Memed, sem QR Code, sem hash, sem CRM exibido. Receita não é juridicamente válida.

5. **Teleconsulta é só uma label**: tipo "Teleconsulta" existe no enum mas selecionar não dispara configuração de sala de vídeo, não gera link compartilhável, não aparece nada extra na confirmação. Sem provedor de vídeo integrado.

6. **Bug texto "Olá, Dra"**: backend usa primeira palavra de `nomeCompleto` como first name. Para "Dra QA Convidada" mostra "Olá, Dra" — saudação ridícula. Filtrar pronomes/títulos (Dr, Dra, Sr, Sra) ou tratar saudação no front.

7. **Receita usa `window.confirm()` nativo** para "Finalizar receita? Após finalizada só pode ser cancelada..." — mesmo anti-padrão de `window.prompt()` na agenda. Padronizar com modal do design system.

8. **Stepper visual do onboarding inconsistente**: mostra "1 Sua conta · 3 Especialidade · 5 Tour" (pula 2 e 4) mas texto inferior diz "Etapa 2 de 3 · Especialidade". Bug visual no stepper.

9. **Export LGPD `/api/minha-conta/exportar-dados` está incompleto**: retorna `vinculos: []` e `consentimentos: []` mesmo para Dono com profissionais vinculados. Não atende LGPD Art. 18 V (portabilidade). Falta também `Content-Disposition: attachment` (volta JSON inline).

10. **Bootstrap retorna `donoUsuarioId: "00000000-..."`** (zerado) para profissional convidado — ok pra esconder o ID do dono, mas inconsistente: o resto do payload tem usuárioId real do convidado.

11. **`numeroRegistro` do CRM armazenado com prefixo duplicado**: usuário digita "CRM/SP 184532" no campo "Número do registro" e backend armazena literalmente assim, mesmo havendo campo `conselho: "CRM"` e `uf: "SP"` separados. Resultado: na receita ficaria "CRM/SP CRM/SP 184532" se exibisse. Validar e normalizar.

12. **Botão "Trocar estabelecimento" no menu do header redireciona pra `/home` quando há só 1 estabelecimento**, sem feedback. Deveria desaparecer ou abrir wizard de criar novo (que tampouco existe na UI — só via API 422).

13. **Toast de sucesso do convite vaza no formato `<status>` (ARIA live region)** mas a aba "Convites pendentes" não mostra: data de envio, TTL/validade, e-mail mascarado. Apenas "Reenviar" e "Cancelar".

14. **Não há sidebar nav nem botão "Sair" pra usuário recém-criado sem vinculo** (estado intermediário). Fica um limbo — só Notificações, Avatar e Ajuda. Usuário não sabe que precisa aceitar o convite.

15. **CEP enriquecido por API externa (ViaCEP)** funcionou bem no cadastro de unidade — mas Sede Principal (primária do tenant) fica sem indicador visual de "principal" nem proteção textual de "não pode excluir".

**Reconfirmações da rodada 1 (continuam):**
- `documentoInternacional` + `dataNascimento` + `telefone` em DTO de listagem (LGPD não-minimização).
- `window.prompt()` na agenda + `window.confirm()` na receita = anti-padrões nativos no DS.
- Endpoints LGPD órfãos (sem UI).

Quando voltar: testar anexos reais (upload de arquivos via UI, validar MIME/tamanho/presigned URL), notificações em tempo real entre 2 abas (SignalR hub `/hubs/estabelecimento` já está conectado), audit log de acesso a paciente, exportação PDF do prontuário, e fluxo de check-in completo (recepção → médico → conclusão → cobrança).
