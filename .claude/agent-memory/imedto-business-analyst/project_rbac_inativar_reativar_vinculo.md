---
name: rbac-inativar-reativar-vinculo
description: Autorização atual (pré-briefing 003) de inativar/reativar vínculo profissional diverge de "Dono + gerir_profissionais"
metadata:
  type: project
---

Autorização dos handlers de vínculo profissional×estabelecimento, conforme código em 2026-06-03:
- `InativarVinculoCommandHandler`: permite **Dono OU o próprio profissional** (não permite não-Dono com `gerir_profissionais`).
- `ReativarVinculoCommandHandler`: permite **apenas o Dono**.

**Why:** A decisão de produto do briefing `2026-06-03_003` é "Dono + perfis com `gerir_profissionais`". Há divergência real com o backend, que o briefing trata como CA explícito de ajuste (CA9/CA10). Os endpoints `/vinculo/{id}/inativar` e `/vinculo/{id}/reativar` recebem **só vinculoId** na rota — não dá pra usar o atributo `RequiresPermissaoExtra` (que precisa de `estabelecimentoId` na rota, como nos endpoints de convite). A checagem de `gerir_profissionais` precisa rodar **dentro do handler**, resolvendo o estabelecimento a partir do vínculo.

**How to apply:** Em futuras demandas que mexam em permissão de inativar/reativar/remover vínculo, lembrar dessa assimetria rota-sem-estabelecimentoId. Verificar o código atual antes de afirmar (pode já ter sido ajustado pelo dev ao executar o briefing 003). Regras de domínio que NÃO são autorização e já existem: Dono nunca desativável, reativar exige `AceitoEm != null` (senão reenviar convite). Veja também [[regra-funcionamento-estabelecimento]] para padrão de validação multi-tenant no front.
