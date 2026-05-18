---
name: project-qa-posdeploy-f2dfa9b-2026-05-17
description: QA pós-deploy commit f2dfa9b (hotfix foto_url varchar(2000)) — bug P0 da rodada 1 resolvido em prod.
metadata:
  type: project
---

Hotfix `f2dfa9b` (run #26001546986): ampliar `foto_url` para `varchar(2000)` em estabelecimentos e profissionais, corrigindo o P0 da rodada 1 (`6414fe2`) onde `PUT /api/estabelecimento/{id}/foto` retornava 500 (Postgres 22001) porque presigned URL S3 SigV4 não cabia em `varchar(500)`.

**Why:** validação cirúrgica de um schema fix; pipeline tem step `migrate` que aplicou o SQL no RDS.
**How to apply:** consultar como evidência de que pipeline aplica `db/migrations/*.sql` corretamente e que o redesign de PDFs/foto está apto em prod.

Pipeline ✅ — test-backend, test-frontend, build-push, migrate (Apply migrations on RDS), deploy, smoke todos verde em ~3min.

Cenários validados em prod (https://app.imedto.com, conta `qa.foto2026@imedto.dev`, estab id=16):
- Upload de foto: PUT /api/estabelecimento/16/foto → 200, toast "Foto atualizada com sucesso", presigned URL persistido com 1826 chars (cabe em varchar(2000)).
- Persistência: reload da página → foto carrega de novo, botões mudam para "Trocar foto"/"Remover".
- Remoção: AppConfirmDialog "Remover foto?" → DELETE /api/estabelecimento/16/foto → 204, toast "Foto removida.", botões voltam para "Adicionar logo"/"Enviar foto".
- Re-upload (3º ciclo): novo PUT → 200, presigned URL 1826 chars novamente.
- Sem 5xx em nenhuma operação; sem console error de aplicação (só 404 do `/api/minha-assinatura` que é esperado para conta de teste sem plano).

Não validados nesta rodada (não-bloqueante):
- PDFs de Receita/Orçamento com logo: a conta `qa.foto2026` está sem assinatura ativa (redireciona pra /assinatura-expirada ao acessar /pacientes ou /orcamentos). Como o redesign de PDF (commit 19da92c) e a feature de logo (6414fe2) já foram validados em prod nas rodadas anteriores e o `usePdfHeader.ts` lê o mesmo campo via GET /api/estabelecimento (confirmado retornando URL 1826 chars), não há razão técnica para os PDFs falharem.

Sobre lixo no S3: backend usa chave fixa `estabelecimentos/{id}.{ext}` — overwrite acontece naturalmente, não acumula órfão. A foto removida via DELETE atualiza o banco (foto_url=null) mas o objeto S3 fica (cleanup explícito seria melhoria futura, não-bloqueante).

**Conclusão:** feature de foto institucional + ampliação de coluna **aprovada em prod**. Débito de validação visual dos PDFs com logo permanece (carregar uma conta QA com assinatura ativa resolve em <1min).

Links:
- Commit: f2dfa9b
- Run: https://github.com/JPPNovais/ImedtoRefatoracao/actions/runs/26001546986
- Screenshots: .qa-screenshots/01-upload-foto-ok.png, 02-foto-persistida-apos-reload.png, 03-foto-removida.png

Relacionado: [[project-qa-posdeploy-6414fe2-2026-05-17]] (rodada 1, onde o bug foi detectado).
