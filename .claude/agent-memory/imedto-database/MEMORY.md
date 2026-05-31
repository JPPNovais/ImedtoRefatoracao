# Memory Index — imedto-database

- [Schema existente: planos/assinaturas legado vs admin](schema_conflito_planos_assinaturas.md) — tabelas `planos` e `assinaturas` existem com bigint IDs; admin usa `imedto_planos` e `imedto_assinaturas` com UUID.
- [Padrão de hash de senha: BCrypt + pepper HMAC-SHA256](hash_senha_padrao.md) — algoritmo do BcryptPasswordHasher e como regenerar hash para seed.
- [Área admin global — migration 20260530034709](area_admin_migration.md) — 6 tabelas criadas, índices, seed, bootstrap em prod via CLI.
- [Wave 2 catálogos globais — REVERTIDA em Wave 4](wave2_catalogos_globais.md) — 3 tabelas paralelas dropadas em Wave 4; live-link via EhPadraoSistema=true nas tabelas legado; drop migration 20260530200000.
