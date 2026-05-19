---
name: qa-posdeploy-da3a906-2026-05-19
description: QA pós-deploy commit da3a906 — fix presigned URL expirando em 24h. fotoUrl agora é fresca em toda resposta.
metadata:
  type: project
---

## QA do fix de presigned URL em `foto_url` (commit `da3a906`, 2026-05-19)

**Sintoma original:** backend salvava a presigned URL completa do S3 (com TTL 24h) em `estabelecimentos.foto_url`/`profissionais.foto_url`. Após 24h a URL expirava → imagem quebrava em todas as telas e PDFs.

**Solução:** `IFotoStorageService.UploadFotoAsync` retorna só a S3 key; novo método `GerarUrlLeitura(path)` gera presigned fresca a cada request; 4 query repositories (`Estabelecimento`, `Profissional`, `Vinculo`, `Agendamento`) reescrevem `foto_url` antes de devolver no DTO. Migration `20260519100000_normalizar_foto_url_para_path.sql` normalizou dados existentes (idempotente).

**Validação em produção (https://app.imedto.com):**
- Sessão já autenticada (`/api/auth/me` = 200).
- Logo do estabelecimento UI: carrega imagem real em `Configurações → Dados do estabelecimento` (não placeholder).
- PDF de evolução (paciente Fulano de tal, evolução 14/05/2026 08:43): logo real no cabeçalho ao lado de "novaEra", sem placeholder "NO".
- Validação técnica via `evaluate_script` em `/api/estabelecimento`:
  - `fotoUrl` length = 1819 chars
  - Contém `X-Amz-Signature`, `X-Amz-Algorithm`, `X-Amz-Expires=86400`
  - Fetch presigned: 200 + `image/jpeg`
- Bootstrap (`/api/auth/bootstrap`) traz `estabelecimentos[].fotoUrl` presigned válida — toda chamada gera URL fresca.
- Foto de profissional: 0 dos 4 profissionais cadastrados em `novaEra` têm foto. Cenário n/a (não havia dado pra testar).
- Console limpo (só 2 erros 400 do próprio script de validação sem header `X-Estabelecimento-Id`, não da aplicação).

**Status:** APROVADO. Fix em produção e validado.

**Observação para futuro:** Quando algum profissional cadastrar foto, refazer rodada validando que `vinculoService.listarProfissionaisPublico` e `agendamentos` (na grade) também trazem URL fresca — os 4 repositories foram atualizados, mas só o `EstabelecimentoQueryRepository` foi observado in-vivo nesta rodada.

**Cenários a reusar em rodadas futuras:**
- Logar e bater em `/api/estabelecimento` validando que `fotoUrl` contém `X-Amz-Signature` e fetch retorna 200.
- Abrir PDF de evolução e confirmar logo no header (não placeholder).
- Quando houver foto de profissional: `GET /api/estabelecimento/{id}/profissionais` com `X-Estabelecimento-Id` header e validar fetch da `fotoUrl` retornado.

[[feedback-elementfrompoint-armadilha-reka-dialog]] [[project-qa-revalidacao-foto-profissional-2026-05-19]]
