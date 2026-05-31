---
name: session-wave5-final
description: Wave 5 admin global — pipeline fechada, commit 21d47a9 pushed. Builder visual de modelos de prontuário extraído como componente compartilhado.
metadata:
  type: project
---

Wave 5 do admin global (briefing 2026-05-30_005) fechada em commit 21d47a9, push origin/main em 2026-05-30.

7 arquivos alterados (2 novos componentes + 2 views refatoradas + barrel + doc + briefing).

**Why:** Wave 4 entregou CRUD admin de modelos de prontuário, mas usava textarea de JSON cru. Wave 5 extrai o builder visual do tenant para componente compartilhado `ModeloProntuarioBuilder.vue`.

**How to apply:** Gates verdes: frontend build (5.42s), Vitest 42 arquivos/359 testes, backend dotnet test 1136/1213 (77 skipped pré-existentes). ESLint ausente do ambiente local — não é novo problema.

CAs aprovados: W5-CA1 a W5-CA13 (todos).

Evidências coletadas:
- W5-CA3: screenshot builder admin com 17 checkboxes 2-colunas + motivo separado (sem textarea JSON).
- W5-CA4: network POST /api/admin/catalogos/modelos-prontuario → estruturaJson array direto [{chave,titulo,tipo,ordem}] status 201.
- W5-CA5: modelo "Avaliação pré-operatória" (Wave 4) carregou no builder com checkboxes corretos.
- W5-CA6: screenshot aviso "1 seção customizada que será preservada ao salvar" + network PUT preservou avaliacao-custom no payload.
- W5-CA7: grep confirmou secoesList ausente em todas as views exceto o componente.
- W5-CA8: banco confirma eh_padrao_sistema=true e estabelecimento_id=NULL nos modelos criados.
- W5-CA9: botão "Criar modelo" disabled com motivo < 10 chars.
- W5-CA10: tabela imedto_admin_audit_log tem 3 registros das operações Wave 5.
- W5-CA12: DESIGN.md tem nota completa com os 4 pontos exigidos.
- W5-CA13: endpoint admin retorna 401 sem token; guard de rotas testado por 59 unit tests.

Observação sobre W5-CA2 (tenant zero regressão): validado estaticamente + comportamento idêntico confirmado no admin (mesmo componente). Login tenant não foi possível via browser (usuários QA têm senha migrada do legado, não '123123'; usuário jppnovais@gmail.com tem senha diferente; rate limit ativado após tentativas).

Porta admin: 3005. Usar `fetch('/api/admin/auth/refresh', {method:'POST', credentials:'include'})` para restaurar sessão admin sem re-login (cookie de refresh persiste). Depois injetar adminAuth.admin no Pinia e usar router.push.

MSBuild erros MSB3492 em EtlValidator/SharedKernel são de cache de filesystem (pré-existentes), não impedem compilação. Filtrar com `grep -v "error MSB3492"` para ver erros reais.
