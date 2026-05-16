---
name: qa-rodada3-2026-05-13
description: Rodada 3 de QA prod (app.imedto.com) — anexos S3, audit log LGPD, receita controlada, hard-delete integrity, performance prontuário com 30+ evoluções.
metadata:
  type: project
---

Sessão 3 (~70 min) em 2026-05-13, focada em anexos reais, audit log, IA, integridade referencial. Sequência das rodadas [[qa-baseline-2026-05-12]] e [[qa-rodada2-2026-05-13]].

**Implementado e validado (não regredir):**
- Upload de anexos (PDF/JPG) funciona via UI `/api/paciente/{id}/prontuario/anexos` (POST multipart). Backend valida MIME e tamanho (50MB) → 422 com mensagem amigável.
- Presigned URL S3 com TTL **5 min** (X-Amz-Expires=300). Após TTL → HTTP 403. Bucket `imedto-anexos-155684258219` em `sa-east-1`.
- Multi-tenant cross-tenant blindado em `/api/paciente/.../anexos/{id}/url`: tenant alheio → 403 SemAcesso, tenant inexistente → 404.
- Anti-enumeração `/api/auth/aceitar-convite`: mensagem genérica "Convite inválido ou expirado." independente de email existir + rate limit 429 (~1 min).
- Performance prontuário com 32 evoluções: 56ms / 40KB / FCP 760ms — sem virtualização (32 cards no DOM) mas dentro de orçamento.
- Lista de paciente DTO `nomeCompleto` (não `nome`).
- Config de IA existe (`/configuracoes/ia`): habilitação on/off, modelo (Claude Sonnet/Opus/Haiku), nível de minimização LGPD, rate limit por usuário (10/min, 200/dia). Mas nenhum endpoint `/api/ia/*` está implementado — só configuração sem feature.

**Achados críticos novos (precisam de fix):**

1. **Upload bypass do path-param `pacienteId`**: `GET /api/paciente/{qualquerId}/prontuario/anexos/{anexoId}/url` retorna a presigned URL do anexo SE o anexo pertence ao tenant do usuário — sem validar se o anexo está vinculado ao paciente da URL. Testado com pacienteId=2, 99999, -1 → todos 200 com a URL real. Backend não valida `anexo.PacienteId == route.pacienteId`. Risco: dentro do mesmo tenant um usuário poderia enumerar anexos de outros pacientes apenas iterando IDs.

2. **Listagem 404 vaza ProblemDetails RFC**: `GET /api/paciente/16` (paciente deletado) retorna `{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"..."}` — formato padrão ASP.NET, **fora** do padrão `{tipo, mensagem}` que o app usa. Vaza `traceId` interno e link RFC, inconsistente com `BusinessException → 422`.

3. **Lista de espera órfã após hard-delete de paciente**: item id=2 ficou em `/api/agendamentos/lista-espera` com `pacienteId=16, pacienteNome="Paciente QA Volume 002"` mesmo após o paciente ser deletado (`GET /api/paciente/16` → 404). FK sem CASCADE ou job de cleanup faltando. Foi limpo manualmente via DELETE `/api/agendamentos/lista-espera/2` (endpoint existe, 204).

4. **Receita controlada = receita simples + label**: `Nova controlada` abre o mesmo formulário (Medicamento, Concentração, Posologia, Via). Sem CID-10 obrigatório, sem categoria da Portaria 344 (A1/A2/A3/B1/B2/C1...), sem RG do paciente, sem destacar tipo de receituário (azul/amarelo/branco). Inválida juridicamente para controlados — só aparência. Backend aceita exatamente o mesmo payload da simples.

5. **Audit log de acesso a prontuário inexistente**: nenhum endpoint `/api/{paciente,prontuario,audit,auditoria,acessos}` existe. Premissa do projeto (CLAUDE.md "Audit trail: log de acesso a dados de paciente/prontuário em audit table (quem, quando, qual registro)") + LGPD Art. 16 não cumpridos para o módulo de prontuário/anexos. Endpoint `/api/minha-conta/atividades` também não existe.

6. **Mensagem de erro vaza interno Kestrel**: upload >60MB (acima do limite do Kestrel, mas o doc diz 50MB) → 400 `"Failed to read the request form. Request body too large. The max request body size is 62914560 bytes."` — em inglês, com detalhe técnico, fora do padrão `{tipo: "ErroDeNegocio", mensagem}`. Limite real do servidor é 60MB, mas a regra de negócio é 50MB — espera entre 50-60MB cai no caminho técnico, acima de 60MB cai no Kestrel.

7. **Não há endpoint DELETE descoberto para anexo**: testados `/anexos/{id}`, `/anexos/{id}/excluir`, `/prontuario/anexos/{id}` — todos 404. UI também não expõe botão "Excluir anexo". Direito LGPD Art. 18 VI (eliminação) parcialmente atendido — paciente pode pedir mas não há fluxo.

8. **`autorUsuarioId` da evolução zerado**: ao salvar evolução pelo próprio Dono, `GET /api/paciente/1/prontuario` retorna `evolucoes[].autorUsuarioId: "00000000-0000-0000-0000-000000000000"` mesmo o `autorNome` vindo corretamente. Bug de mapeamento — perde rastreabilidade.

9. **Bootstrap `donoUsuarioId` zerado também para o próprio Dono**: já documentado para convidado na rodada 2, agora confirmado para o Dono logado vê `estabelecimentos[].donoUsuarioId: "00000000-..."` no seu próprio tenant. Faz papel de "esconder do front" mas deveria pelo menos retornar o próprio ID para o próprio Dono.

10. **`Imprimir` do prontuário é `window.print()` puro**: sem template estruturado, sem cabeçalho da clínica, sem dados do paciente formatados, sem rodapé com CRM. Imprime o HTML da SPA (com sidebar, header). Não substitui prontuário em PDF próprio. Recomendado: gerar PDF server-side ou usar componente `react-to-print`-like com layout dedicado.

11. **Botão `Exportar histórico` no histórico de evoluções não faz nada**: nenhuma request, nenhum download, nenhum modal. Botão morto.

12. **Receita usa `window.confirm()` ainda**: confirmado na cancelamento da receita ("Excluir receita em rascunho? Esta ação é irreversível.") — anti-padrão já catalogado na rodada 2.

**Reconfirmações da rodada 2 (continuam):**
- Export LGPD `/api/minha-conta/exportar-dados`: `vinculos: []`, `consentimentos: []`, `notificacoes: []` mesmo para Dono que tem vínculo, sem `Content-Disposition: attachment`.
- Backend impede 2º estabelecimento por Dono (422) — não permite testar cenário 8 sem cobaia.

**Não testado / fica para próxima:**
- Notificação em tempo real real (precisa 2 usuários ativos no mesmo tenant — só temos 1 Dono e 1 convidado que requer aceitar convite).
- Cenário 8 (Médico em 2+ tenants) — backend impede 2º tenant por Dono.
- Audit log via banco direto (psql) — pode existir tabela mas sem endpoint.
- Excluir anexo via UI quando feature existir.
- Receita controlada Portaria 344 (categorias e validações).

**Cleanup pendente da sessão:**
- 30 evoluções "bulk" criadas no paciente 1 NÃO foram apagadas — não há endpoint DELETE de evolução (provavelmente WORM por design clínico). Próxima rodada vai começar com 32 evoluções no paciente Fulano.
- 2 anexos (test.pdf, test.jpg) ficaram no S3 e no banco (sem endpoint DELETE descoberto).
- Agendamento id=16 cancelado (status=Cancelado, motivo "QA cleanup").
- Lista de espera órfã id=2 removida.
