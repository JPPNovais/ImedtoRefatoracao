# Comandos

> **Quando ler**: ao rodar build, testes, lint, migrations, subir dev local, ou executar tarefa CLI.
>
> **Quando atualizar**: ao introduzir novo script `npm`/`dotnet`, mudar caminho de projeto, alterar fluxo de migration, adicionar comando recorrente (ex: seed, snapshot, refresh).

---

## Backend (raiz `backend/src/`)

- Build: `dotnet build Imedto.Backend.sln`
- Rodar API (dev): `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Services/Imedto.Backend.API --no-launch-profile` (Swagger em `/swagger`)
- Testes: `dotnet test Tests/Imedto.Backend.Test`
- Teste único: `dotnet test Tests/Imedto.Backend.Test --filter "FullyQualifiedName~NomeDoTeste"`

## Migrations (EF Core autora, pipeline aplica em RDS)

Toda alteração de schema tem **duas etapas**:

1. **Gerar no EF Core** (código C# em `backend/src/Services/Imedto.Backend.Infrastructure/Database/Migrations/`):
   ```bash
   cd backend/src
   dotnet ef migrations add <Nome> \
     --project Services/Imedto.Backend.Infrastructure \
     --startup-project Services/Imedto.Backend.API \
     --output-dir Database/Migrations
   ```

2. **Exportar SQL idempotente e salvar em `db/migrations/`**:
   ```bash
   dotnet ef migrations script <MigrationAnterior> <MigrationNova> \
     --project Services/Imedto.Backend.Infrastructure \
     --startup-project Services/Imedto.Backend.API \
     --idempotent --output /tmp/next.sql
   # Remover BEGIN/COMMIT do script (a pipeline gerencia a transação).
   # Salvar como db/migrations/YYYYMMDDHHMMSS_descricao.sql (mesmo timestamp da migration EF).
   ```

3. **Aplicar no banco**:
   - Em CI/CD: a pipeline envia o SQL para a EC2 e roda [deploy/scripts/migrate.sh](../deploy/scripts/migrate.sh), que executa `psql` contra o RDS lendo host/senha do AWS SSM.
   - Em dev local: rodar `psql` direto contra o RDS (ou banco local) com o arquivo SQL. **Nunca usar `dotnet ef database update`** — o `dotnet ef` é só para autoria do SQL.

Functions SQL, triggers, índices CONCURRENTLY e seed data que não tenham equivalente no EF (ou não devam existir no modelo .NET) são escritos **direto como `.sql`** em `db/migrations/` — sem passar pelo EF.

## Frontend (raiz `frontend/`)

- Dev: `npm run dev` (Vite, porta 3000, proxy `/api` → `http://localhost:5000`)
- Build: `npm run build` (type check + vite build)
- Lint/fix: `npm run lint`
- Testes: `npm test` (Vitest)

## Acesso ao RDS em dev

Túnel SSH via EC2 (ver [INFRA.md](INFRA.md#database-1-rds-postgres--free-tier)):

```bash
ssh -i ~/.ssh/imedto-deploy.pem -L 5432:imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com:5432 ec2-user@56.125.254.136
# Em outro terminal:
PGPASSWORD=$(aws ssm get-parameter --name /imedto/dev/db-password --with-decryption --query Parameter.Value --output text) \
    psql -h localhost -U imedto -d imedto
```
