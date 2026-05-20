---
name: project-termos-consentimento-fase1
description: Fase 1 (backend) da feature de Termos de Consentimento — entregue 2026-05-19. Domain+Infra+Application+API+migrations+seeds.
metadata:
  type: project
---

Fase 1 entregue 2026-05-19 (sem UI). Estado atual:

**O que ficou pronto:**
- Aggregates `TermoModelo` + `TermoModeloVersao` (snapshot imutável, versão bumpa só com mudança de HTML) + `TermoEmitido` (snapshot HTML+texto, hash SHA-256, status: pendente/assinado/recusado/revogado/expirado, AssinaturaTipo: pdf_anexado/aceite_link).
- `TermoAuditLog` (audit append-only, lista fechada de ações em kebab-case) + `TermoEmitidoAcessoLog` (log do fluxo público).
- Endpoints CRUD de modelo + clonar (padrão→tenant) + emissão + upload PDF (magic bytes %PDF-, 10MB max, SHA-256) + revogação. Stubs 501 para fluxo público (Fase 4) e PDF gerado on-the-fly (Fase 3).
- Sanitizer: `Ganss.Xss.HtmlSanitizer` (lib NuGet `HtmlSanitizer` 9.0.892). Whitelist restrita (p/br/strong/em/u/ul/ol/li/h1-6/blockquote/hr/span/div/a + só href http/https).
- Resolver de variáveis: lista fechada de 20 variáveis (paciente, estabelecimento, profissional, data, cidade). Server-side via Dapper.
- Permissões: nova área `termos` com chaves `emitir` e `gerenciar_modelos`. MedicoPadrao e RecepcaoPadrao ganharam `termos.emitir`.
- Estabelecimento ganhou colunas `cidade` (varchar 100) + `estado` (char 2). Método `AtualizarEndereco(end, cidade, uf)` no aggregate.
- 5 padrões do sistema seedados em SQL separado (`20260520014100_seed_termos_padrao_sistema.sql`) — LGPD, Imagem, Cirúrgico, Financeiro, Telemedicina.

**Tradeoffs registrados:**
- Sanitizer Ganss.Xss em vez de regex DIY: conteúdo médico = surface XSS real, vale a dep nova (singleton stateless após ctor).
- TTL link público = 7 dias hardcoded (TimeSpan estática no handler). Configurar via SSM se virar atrito.
- PDF storage reusa bucket de anexos (`BucketAnexosProntuario`) — mesma política LGPD; criar bucket dedicado se a retenção divergir no futuro.

**Como aplicar:**
- Migration: `dotnet ef migrations add CriarTermosDeConsentimento` gerou `20260520014048` (já no repo). SQL idempotente em `db/migrations/20260520014048_criar_termos_de_consentimento.sql`. Seed em `20260520014100_seed_termos_padrao_sistema.sql` (separado, pula linha já existente).
- Pipeline já aplica via `deploy/scripts/migrate.sh` em ordem alfabética dos arquivos `db/migrations/`.

**Não fazer ainda (próximas fases — não reabrir):**
- UI completa (Fase 2)
- PDF gerado on-the-fly (Fase 3) — stub retorna 501
- Fluxo público anônimo + e-mail (Fase 4) — stub retorna 501
- Unificar LgpdConsentimento legado no novo modelo (Fase 5)
- WhatsApp como canal (decidido: NÃO implementar)
