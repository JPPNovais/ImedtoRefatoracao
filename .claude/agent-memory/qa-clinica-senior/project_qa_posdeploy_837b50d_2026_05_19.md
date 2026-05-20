---
name: qa-posdeploy-837b50d-2026-05-19
description: QA Termos de Consentimento (837b50d) — P0 em emissão por SQL com p.id (profissionais.usuario_id).
metadata:
  type: project
---

Deploy 837b50d (2026-05-19, pipeline run 26136250076 verde). Backend completo de Termos: 80+ arquivos, CRUD modelos OK, multi-tenant blindado, audit trail OK, listagem/clone/exclude/ativo/revogar OK. Front: tab "Termos" no detalhe do paciente existe mas é apenas placeholder estático "serão listados aqui em breve" (sem chamada de API).

**P0 bloqueador — POST /api/pacientes/{id}/termos retorna 500**
- Arquivo: `backend/src/Services/Imedto.Backend.Infrastructure/Termos/TermoResolverDeVariaveis.cs:120-131`.
- Causa: query Dapper referencia `p.id` e join `u.id = p.id`, mas a tabela `public.profissionais` tem PK `usuario_id` (uuid), não `id`. Postgres retorna 42703 "column p.id does not exist".
- Agravante 1: o handler `EmitirTermoCommandHandler` (linha 72) passa `cmd.EmissorUsuarioId` (sempre o usuário logado) como `ProfissionalUsuarioId` do `ContextoDeVariaveis`. A condição `pid != Guid.Empty` é sempre true para qualquer usuário autenticado, então o resolver sempre executa o SELECT mesmo para emissores não-profissionais (Dono, Recepcionista). Fix mínimo: corrigir o SQL para `p.usuario_id`, ajustar o JOIN para `u.id = p.usuario_id`, e mudar `WHERE p.id` → `WHERE p.usuario_id = @UsuarioId`. Fix completo: separar conceitos "emissor" (administrativo) vs "profissional citado no termo" — usuário Dono não-profissional não deve resolver `{{profissional.nome}}`, deve cair no fallback.
- Agravante 2: a query do profissional não filtra por `estabelecimento_id` nem por vínculo do profissional ao estab — defense-in-depth ausente. Se um usuário é profissional em estab A, o termo emitido em estab B vai puxar registros dele do A. (LGPD: não vaza nada do paciente do outro tenant, mas o conselho/UF do profissional vaza entre estabs do mesmo profissional.)

**Testes que passaram (8/9)**
1. CRUD de modelos: POST/GET/PUT/PATCH ativo/DELETE/clonar → tudo 201/204/200. (modelos 6 e 7 criados/clonados, 6 deletado).
2. Listar modelos paginado com padrões mesclados → 5 padrões do seed visíveis, ordem (padrões depois dos próprios) correta.
3. ListarVariaveis → 20 variáveis, 4 categorias.
4. Multi-tenant em listagem termos do paciente: paciente de outro tenant → 422 "Paciente não encontrado." (genérica).
5. Tenant inválido no header → 404 "Estabelecimento não encontrado."
6. Sem header → 400 "TenantAusente".
7. Revogar/PDF de termo inexistente → 422 "Termo não encontrado." (não revela existência).
8. Clonar id inexistente → 422 "Modelo padrão não encontrado." (genérica).
9. Paginação prontuário (evoluções/receitas/atestados/pedidos): todas 4 endpoints retornam `{itens,total,pagina,tamanhoPagina}` corretamente. Não testei com dataset grande por conta nova sem evoluções; estrutura valida.

**Conta QA usada**
- email `qa.termos.1779242290094@imedto.local`, password `QaTermos!2026#X`, usuário uuid `d58452c4-7e43-4d0a-9c96-f4c70322ab3e`, estab 24 "QA Termos Estab", assinatura 21 Pro Trial 30d, paciente 220.

**Stubs em produção (501, mas com rotas registradas)**
- `GET/POST /api/publico/termos/aceite/{token}` (TermoPublicoController) — fluxo de aceite via link, Fase 4.
- `GET /api/termos/{id}/pdf-gerado` — geração de PDF a partir do snapshot, Fase 3.
- `POST /api/termos/{id}/reenviar-link` — reenvio de link, Fase 4.
Anotar como débito: rotas 501 aumentam ruído do `swagger.json`. Não bloqueia.

**Frontend de Termos**
- Tab "Termos" no PacienteDetalheView é placeholder estático, sem service no front consumir as APIs novas.
- Necessário criar `termosService.ts`, drawer de emissão, listagem por paciente, integrar com permissões `termos.emitir`/`termos.gerenciar_modelos`. Não é regressão — apenas backlog explícito.
