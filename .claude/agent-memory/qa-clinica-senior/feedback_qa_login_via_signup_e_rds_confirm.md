---
name: qa-login-via-signup-e-rds-confirm
description: Como criar conta de QA quando não há credencial: signup + confirmar email via UPDATE direto no RDS via túnel SSH.
metadata:
  type: feedback
---

Quando não houver credencial salva para testar em app.imedto.com, a forma legítima é:

1. `POST /api/auth/signup` com `{email, password, nome}` → 201 com `requerConfirmacaoEmail:true`. **Atenção**: o DTO usa `password` (em inglês), não `senha`. Se mandar `senha`, o backend retorna "Senha deve ter no mínimo 8 caracteres" mesmo com senha longa (porque está nullo).
2. Subir túnel SSH para RDS: `ssh -i ~/.ssh/imedto-deploy.pem -L 5433:imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com:5432 -N -f ec2-user@56.125.254.136`.
3. Buscar senha em SSM: `aws ssm get-parameter --name /imedto/dev/db-password --with-decryption --query Parameter.Value --output text` (com `AWS_PROFILE=imedto`).
4. Confirmar e-mail manualmente: `UPDATE auth_credenciais SET email_confirmado_em = NOW() WHERE email='...'`. Coluna correta é `email_confirmado_em` (timestamp), não `email_confirmado` (bool).
5. Login normal funciona depois.
6. Conta nova exige passar pelo onboarding de 5 etapas (Sua conta + Sua clínica + Especialidade + Horários + Tour) — CPFs comuns como 12345678909/52998224725 já estão tomados, gerar CPF válido com algoritmo padrão.
   - **Atalho via SQL** (evita 5 etapas): `UPDATE usuarios SET nome_completo='QA', onboarding_completo=true, status='ativo' WHERE id='<uuid>'` + `INSERT INTO estabelecimentos(dono_usuario_id, nome_fantasia, status, criado_em) VALUES('<uuid>','QA Estab','ativo',NOW())`.
   - **Pegada**: `OnboardingCompletadoFilter` cacheia o flag por **2 min** em memória (`cacheKey=onboarding:{usuarioId}`); após UPDATE, ou espera ~2min ou ajuda logar antes de bater na API. O logout NÃO invalida o cache.
   - **Pegada 2**: Mesmo com onboarding completo, o estab precisa de assinatura ativa (plano `Pro` ou `Enterprise`) para acessar `orcamento_completo`. Plano `Free` retorna 402 `FeatureBloqueada`. Inserir em `assinaturas` com `plano_id=3` (Pro), `status='Trial'`, `expira_em=NOW()+'14 days'`.
7. Encerrar túnel: `pkill -f "ssh -i.*imedto-deploy.*5433"`.

**Why:** Memória anterior `project_qa_posdeploy_19da92c_2026_05_16.md` registrou que QA não tinha senha pra `jppnovais@gmail.com`. Sem credencial não dá pra testar UI logada. Esse fluxo cria conta descartável sem precisar de inbox de email externo.

**How to apply:** Em toda sessão de QA pós-deploy onde for testar tela logada e não houver credencial documentada. Não usar pra contas reais — apenas para tenant de QA descartável.

**Caveat:** o `mcp__chrome-devtools__click` em alguns AppButton **não dispara o handler `@click` do Vue** (provavelmente `element.click()` em vez de `MouseEvent`). Workaround: `mcp__chrome-devtools__evaluate_script` disparando `dispatchEvent(new MouseEvent('click', { bubbles: true, view: window }))` no `<button>` interno.
