---
name: assinatura-digital-migration
description: Migration 20260601120000 — Assinatura Digital ICP-Brasil. Schema criado, decisões de tipo, estrutura de entidades e bounded context.
metadata:
  type: project
---

Migration `20260601120000_CriarAssinaturaDigital` — criada em 2026-06-01 para o briefing `2026-06-01_001`.

**Why:** Receitas precisam de assinatura digital ICP-Brasil (CFM Res. 2.299/2021 + Lei 14.063/2020). Provedor MVP: BirdID (Soluti).

**Decisões de schema tomadas:**

- A coluna `assinatura_digital_status` (varchar 20) já existia em `receitas` desde `20260514111139`. NÃO foi criada nova coluna `status_assinatura` — o modelo C# `Receita.AssinaturaDigitalStatus` continua mapeando para `assinatura_digital_status`.
- O enum `StatusAssinaturaDigital` foi expandido com: `AssinaturaPendente=3`, `FalhaAssinatura=4`, `AssinaturaExpirada=5` (os valores 0/1/2 existentes foram preservados).
- `assinatura_certificados.medico_id` é `uuid` (não `bigint`) — porque `usuarios.id` é `uuid` (Entity<Guid>).
- `assinatura_audit_log.usuario_id` é `uuid` (mesmo motivo acima).
- `assinatura_certificados.id` é `uuid PK` — aggregate root com `Entity<Guid>`, gerado no aggregate (`Guid.NewGuid()`).
- `assinatura_audit_log.id` é `bigint GENERATED ALWAYS AS IDENTITY` — tabela append-only, sem aggregate root C# para esta coluna.
- Sem FK física de `assinatura_audit_log.receita_id` → `receitas.id` (briefing especifica: log permanece mesmo após receita soft-deleted).
- Sem `estabelecimento_id` em `assinatura_certificados` — decisão D4 do briefing: vínculo por médico (usuário), não por estabelecimento.

**Novos arquivos de domínio:**
- `Domain/AssinaturaDigital/AssinaturaCertificado.cs` — aggregate root
- `Domain/AssinaturaDigital/AssinaturaAuditLog.cs` — entidade append-only
- `Domain/Receitas/StatusAssinaturaDigital.cs` — enum expandido
- `Domain/Receitas/Receita.cs` — 3 propriedades novas + 4 métodos de transição de estado

**Índices CONCURRENTLY:**
- `ix_receitas_status_assinatura_pendente` — parcial WHERE assinatura_digital_status = 'AssinaturaPendente' (para o job de expiração)
- `ix_receitas_assinatura_solicitada_em` — parcial WHERE AssinaturaPendente (para filtro de janela de tempo do job)
- Ambos em `20260601120001_indices_assinatura_digital_concurrently.sql` (fora da transação principal)

**How to apply:** Ao trabalhar em handlers de assinatura digital, lembrar que a coluna de status é `assinatura_digital_status` (não `status_assinatura`). O bounded context está em `Domain/AssinaturaDigital/`.
