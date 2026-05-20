---
name: project-termos-consentimento-fase5
description: Fase 5 (2026-05-20) — limpeza do módulo legado lgpd_consentimentos; tabela renomeada, código deletado, ExportarMeusDados retorna Consentimentos vazio até existir vínculo paciente-usuário.
metadata:
  type: project
---

Fase 5 de Termos de Consentimento (executada 2026-05-20) — remoção do módulo legado `lgpd_consentimento` que era órfão (POST `/api/lgpd/consentimentos` nunca era chamado pelo front; `MinhaContaLgpdView` só listava).

**Why:** A nova fonte de verdade dos aceites de termo é `termo_emitido` (por paciente, com snapshot HTML + hash + status Pendente/Assinado). O módulo `lgpd_consentimentos` (por `usuario_id`, sem vínculo com paciente) duplicava o conceito e nunca foi de fato usado em produção — manter dois fluxos seria fonte de bugs.

**How to apply:**
- Endpoint `POST /api/lgpd/consentimentos` e `GET /api/lgpd/consentimentos/meus` **não existem mais** (controller deletado).
- Tabela `lgpd_consentimentos` foi **renomeada** para `lgpd_consentimentos_arquivo` na migration `20260520105013_ArquivarLgpdConsentimentos` (RENAME, não DROP — rollback safety por 30 dias). Não dropar antes de 2026-06-20.
- `LgpdQueryRepository.ExportarMeusDados` ainda devolve `Consentimentos` no DTO, mas **sempre vazio** — comentário inline aponta que será populado de `termo_emitido` quando existir vínculo formal paciente↔usuário. Hoje **Paciente NÃO tem `usuario_id`** no schema; só `PacienteAcessoLog` tem.
- `ConsentimentoDto` permanece em `MeusDadosLgpdDto.cs` pra manter contrato do export estável.
- `Anonimizar conta` (`DELETE /api/minha-conta`) e `Exportar dados` (`GET /api/minha-conta/exportar-dados`) **mantidos íntegros** — são fluxos diferentes do consentimento.
- View `MinhaContaLgpdView.vue` perdeu a seção "Meus consentimentos"; ganhou nota sutil "termos clínicos ficam na ficha de paciente".
- Relacionado: [[project-termos-consentimento-fase1]], [[project-termos-consentimento-fase3]], [[project-termos-consentimento-fase4]].

**Armadilha futura:** quando criarmos a tabela de vínculo paciente↔usuário (ex: `paciente_usuario`), atualizar a query de `ExportarMeusDados` pra puxar `termo_emitido` com `categoria='lgpd'` (note: seed grava lowercase, mas enum `CategoriaTermo.Lgpd` via `HasConversion<string>` gravaria PascalCase — usar `LOWER(m.categoria) = 'lgpd'` ou normalizar).
