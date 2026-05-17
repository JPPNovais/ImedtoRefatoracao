---
name: project_foto_estabelecimento_pdfs
description: Foto/logo do estabelecimento — endpoints + cache + integração nos PDFs (Receita backend, demais frontend).
metadata:
  type: project
---

A foto/logo do estabelecimento alimenta TODOS os PDFs do produto. Cuidados não-óbvios:

**Permissão**: o controller `EstabelecimentoController` usa `[RequiresPermissaoExtra(ConfigEstabelecimento, "id")]` (libera Dono + qualquer admin com a permissão fina). Os **4 handlers do agregado** (`AlterarFoto`, `RemoverFoto`, `AtualizarEstabelecimento`, `AtualizarFuncionamento`) revalidam via `IModeloPermissaoRepository.UsuarioTemPermissaoExtra(usuarioId, estabId, PermissoesExtras.ConfigEstabelecimento)` (defense-in-depth). O método **trata Dono como pass-through interno** — não precisa de check separado de `DonoUsuarioId`. Mensagem de erro genérica: "Você não tem permissão para alterar este estabelecimento." (LGPD/multi-tenant). Verificação duplicada inline nos 4 handlers — `TODO` marcado em `AlterarFotoEstabelecimentoCommandHandler` para extrair quando a regra crescer (premissa: simplicidade > abstração especulativa).

**Storage**: `IFotoStorageService.UploadFotoAsync` + `RemoverFotoAsync`. S3 DeleteObject é idempotente — não lança se a chave já não existe. Path canônico: `estabelecimentos/{id}.{ext}`. A presigned URL (TTL 24h) tem query string com `X-Amz-Signature` — o handler de remover extrai a extensão pelo path, ignorando query.

**Why:** garantir que próxima geração de PDF não use logo stale.
**How to apply:** quando a view tocar foto do estabelecimento, **chamar `invalidarCacheEstabelecimentoAtivo()`** do `usePdfHeader` após upload/remoção. A cache de logo já invalida sozinha porque a key combina `id + fotoUrl`, mas a cache de estabelecimento (`estabelecimentoCache`) é por id apenas. Sem isso, PDF gerado na mesma sessão renderiza a foto antiga.

**PDF Receita backend (QuestPDF)**:
- `QuestPdfReceitaService` injeta `IHttpClientFactory` e baixa a logo via HttpClient nomeado `"PdfReceitaLogo"` (registrado no `Program.cs` com timeout 5s; caller usa cts de 3s).
- Falha gracioso: timeout/404/conteúdo inválido → `LogoBytes=null` → fallback para placeholder com iniciais. **Nunca bloqueia emissão**.
- `DadosPdf` é um record com `init`-only `LogoBytes` — testes que chamam `GerarPdf(dados)` direto continuam funcionando (null = placeholder).

**PDFs que reusam `usePdfHeader`** (frontend): Prontuário, Relatórios, **Orçamento**. Orçamento usa apenas `desenharCabecalho(...)` no topo — o corpo (autotable das seções) **continua em helvetica** propositalmente para evitar regressão visual no fluxo de vendas; só o header tem Nunito + logo. [[project_pdfs_institucionais]]

**Componente UI**: `AppPhotoUpload` (design system) — avatar circular + iniciais como fallback + botões "Trocar"/"Remover". Não faz HTTP: emite `@upload`/`@remover`. Reusa `redimensionarImagem` (services/imageUtils) para 512×512 antes do envio (~50-100 KB). Usado primeiro em `EstabelecimentoView.vue` aba "Dados".
