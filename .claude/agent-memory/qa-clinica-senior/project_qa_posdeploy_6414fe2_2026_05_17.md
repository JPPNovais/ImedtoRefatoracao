---
name: qa-posdeploy-6414fe2-2026-05-17
description: QA pós-deploy 6414fe2 (foto estabelecimento + logo nos PDFs) — bug P0 varchar(500) em foto_url. DEVOLVIDO.
metadata:
  type: project
---

Commit `6414fe2` em main; pipeline verde (test-backend 928/928, test-frontend 245/245, build-push, migrate, deploy, smoke).

UI deployada corretamente (`AppPhotoUpload` renderiza placeholder com iniciais, botão "Enviar foto", descrição completa, sem botão "Remover" enquanto sem foto). Tab "Dados do estabelecimento" abre sem console errors.

**P0 BLOQUEANTE — upload 100% quebrado em prod.**

PUT `/api/estabelecimento/16/foto` retorna **500**. Causa raiz nos logs do backend:

```
Npgsql.PostgresException 22001: value too long for type character varying(500)
ActionName: EstabelecimentoController.AlterarFoto
RequestPath: /api/estabelecimento/16/foto
```

A coluna `estabelecimentos.foto_url` é `varchar(500)`, mas a presigned URL gerada por `S3FotoStorageService.GetPreSignedURL` (`Expires=24h`, `SigV4`) tem ~700-1200 chars facilmente. Toda tentativa de upload falha após o S3 já ter recebido o blob — sobra **objeto órfão no bucket** (`imedto-fotos-155684258219/estabelecimentos/16.png`).

**Mesma falha latente em `profissionais.foto_url` (varchar(500))** — não foi pego antes porque hoje a tabela tem 0 registros com foto.

**Devolvido para `fullstack-clinica-senior`** com diagnóstico completo. Correção precisa:
1. Migration EF + SQL em `db/migrations/` para `ALTER TABLE estabelecimentos ALTER COLUMN foto_url TYPE varchar(2000)` (ou `TEXT`).
2. Mesma alteração em `profissionais.foto_url`.
3. Ajustar `EstabelecimentoConfiguration.cs:22` e `ProfissionalConfiguration.cs:21` — `.HasMaxLength(2000)` (ou remover constraint).
4. Considerar limpar o blob órfão `estabelecimentos/16.png` (não crítico — chave colide com qualquer próximo upload do estab 16).
5. (Opcional) Em `AlterarFotoEstabelecimentoCommandHandler`, considerar compensação S3 (delete) caso `Salvar` falhe — atualmente UoW rollback deixa blob órfão. Aceitável documentar tradeoff e deixar como TODO.

Validação restante OK (não precisa retestar quando voltar):
- Tela renderiza corretamente
- Bootstrap/network sem erros
- Console limpo
- Texto/descrição/iniciais corretos
- Sem botão Remover quando ausente de foto

**Cenários para re-testar quando voltar:**
1. PUT foto ≤2 MB JPG → 200, FotoUrl gravado com URL > 500 chars
2. GET `/api/estabelecimento` retorna a URL pós-upload
3. UI mostra a foto e o botão Remover aparece
4. DELETE foto → 204, FotoUrl null, blob removido do S3
5. PDF de receita com logo (precisa criar receita; fora do escopo desta validação imediata)
6. PDF de orçamento com logo via `useOrcamentoPdf`/`usePdfHeader`

**Screenshots:** `.qa-screenshots/foto-est-config-sem-foto-2026-05-17.png` (placeholder OK).

**Conta de QA criada nesta sessão:** `qa.foto2026@imedto.dev` / `QaFoto@2026` — usuário `0153c64c-b086-4f9b-a9cb-86fce04a04de`, estabelecimento `16` (QA Foto Estab). Reutilizar.
