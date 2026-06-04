---
name: ef-snapshot-divergencia-wave2-assinatura
description: Snapshot EF diverge do banco real em 2 pontos — Wave 2 e assinatura digital — causando DropTable/CreateTable espúrios em novas migrations
metadata:
  type: feedback
---

O `AppDbContextModelSnapshot.cs` não está em sincronia com o banco por dois motivos históricos:

1. **Wave 2 (3 tabelas imedto_*_global)**: dropadas via SQL direto em `20260530200000_drop_catalogos_globais_wave2.sql`, mas o código C# das entidades ainda existe no projeto. O EF continua vendo essas entidades no model, então toda nova migration gerada inclui `DropTable` no `Up()` e `CreateTable` no `Down()`.

2. **Assinatura digital (2 tabelas + 3 colunas)**: migration `20260601120000_CriarAssinaturaDigital.cs` existe mas SEM arquivo Designer, então o EF não a reconhece no assembly. O model snapshot já reflete essas entidades, mas o EF acha que precisa criá-las de novo.

**Why:** As migrations foram aplicadas via SQL direto sem `dotnet ef migrations add`, quebrando a cadeia EF.

**How to apply:** Toda nova migration gerada vai incluir essas operações espúrias. SEMPRE editar o `.cs` gerado e remover as operações que não pertencem à feature:
- Remover `DropTable` das 3 tabelas `imedto_*_global` do `Up()`
- Remover `CreateTable` das 3 tabelas `imedto_*_global` do `Down()`
- Remover `CreateTable` de `assinatura_certificados` e `assinatura_audit_log` do `Up()`
- Remover o que o `Down()` reverter dessas tabelas de assinatura
- O SQL em `db/migrations/` deve conter APENAS as operações da feature nova
