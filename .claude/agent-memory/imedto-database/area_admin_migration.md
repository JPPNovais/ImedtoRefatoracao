---
name: area-admin-migration
description: Migration 20260530034709 — 6 tabelas admin global criadas, índices validados, seed aplicado em dev
metadata:
  type: project
---

Migration EF Core gerada em 2026-05-30 (timestamp `20260530034709_CriarAreaAdminGlobal`).

Tabelas criadas:
- `imedto_admins` — UUID PK, citext email, BCrypt senha_hash
- `imedto_admin_refresh_tokens` — token_hash SHA256, CASCADE ao deletar admin
- `imedto_admin_audit_log` — append-only, SET NULL ao deletar admin, sem FK física para estabelecimentos
- `imedto_planos` — UUID PK, preco_mensal_centavos int nullable, limites_json jsonb
- `imedto_assinaturas` — histórico imutável, fim_em NULL = vigente, RESTRICT em FK para estabelecimentos e plano
- `imedto_config` — PK = chave text, valor jsonb

Seed permanente: plano "Gratuidade Vitalícia" com ID fixo `00000000-0000-0000-0000-000000000001`.
Seed dev: admin@imedto.com / 123123 via `app.environment = 'Development'` setado no startup da API.

**Why:** Área admin separada fisicamente de usuários comuns. Admin JWT nunca carrega estabelecimento_id.

**How to apply:** Bootstrap de admin em prod via CLI `seed-admin` (ainda a implementar pelo developer). Não inserir seeds de admin diretamente em arquivos SQL de produção.
