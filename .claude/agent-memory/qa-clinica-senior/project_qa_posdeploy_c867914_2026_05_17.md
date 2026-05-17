---
name: qa-posdeploy-c867914-2026-05-17
description: QA do commit c867914 (PDF por evolução + timeline compacta na aba Prontuário do paciente) — entregue, 6/6 critérios verde.
metadata:
  type: project
---

Commit `c867914` (2026-05-17): feat(prontuario) PDF por evolução + card compacto.

Pipeline GitHub Actions: 6 jobs verde (test-backend, test-frontend, build-push, migrate, deploy, smoke).

**Validação em produção (app.imedto.com)** — conta QA descartável `qa-pdf-1779025452@imedto.test`, estab 13, paciente 211, prontuário 8 com 2 evoluções:
- C1 timeline compacta: ✅ aba Prontuário do detalhe e aba "Consultas anteriores" exibem cards iguais (data destacada + modelo + autor + resumo ~220 chars + pills "n/9 seções"), sem expansão de todas as seções
- C2 botão PDF por card: ✅ aria-label "Gerar PDF desta evolução" presente em todos
- C3 PDF baixa: ✅ blobs PDF gerados de 45 KB (1 evolução) e 52 KB (histórico)
- C4 PDF contém só aquela evolução: ✅ comparação visual lado-a-lado dos PDFs confirma — individual mostra somente evolução de 17/05 com 2 seções; histórico mostra 17/05 + 10/05 com todas as seções preenchidas
- C5 "Exportar histórico" continua funcionando: ✅ POST /prontuario/registrar-exportacao → 204, PDF baixado
- C6 loading + double-click guard: ✅ 3 cliques sintéticos rápidos no mesmo botão → apenas 1 POST registrar-exportacao + 1 PDF gerado (guard `evolucaoSendoBaixada !== null` funcionou)

Cenários de borda (curl com cookie autenticado):
- Paciente inexistente → 422 "Paciente não encontrado." (genérico) ✅
- Evolução inexistente em paciente válido → 422 "Evolução não encontrada." ✅
- Tenant inexistente → 404 (middleware antes do handler) ✅

LGPD audit trail no banco (`prontuario_acesso_log`):
- Tipo_acesso "Exportacao" gravado uma vez por exportação efetiva (não por clique).
- Audit é gravado ANTES do PDF — se backend devolver 422/401, doc não é produzido.
- Auto-refresh do token funciona: 401 → /auth/refresh 200 → repeat 204 sem o usuário perceber.

Sem migration necessária: coluna `prontuario_acesso_log.tipo_acesso` é `varchar(20)` livre (sem check constraint), o novo valor "Exportacao" (10 chars) cabe.

Console limpo (zero erros/warns) durante toda a sessão.

Status final: **ENTREGUE** — pode arquivar.
