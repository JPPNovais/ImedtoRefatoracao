---
name: especialidade-por-vinculo
description: Especialidade do profissional é por vínculo/estabelecimento, não global — regra de domínio cross-cutting e os pontos canônicos de leitura.
metadata:
  type: project
---

Decisão de produto (briefing 2026-06-03_004): a especialidade efetiva do profissional é **por vínculo/estabelecimento**, com fallback para o cadastro global. Regra canônica: `COALESCE(v.especialidade_convidada, p.especialidade)` — o vínculo vence; se nulo, cai pro global (`profissionais.especialidade`). Vale também nos PDFs/termos clínicos (`{{profissional.especialidade}}`). O Dono edita continuamente a especialidade do vínculo (grava em `especialidade_convidada`), RBAC só Dono, espelhando `AlterarModeloPermissaoDoVinculoCommandHandler`.

**Why:** profissional atua em N estabelecimentos com especialidades distintas; tratar como atributo global ofusca a realidade do multi-vínculo. Antes do briefing o COALESCE estava **invertido** (global vencia), contrariando a regra — fácil regredir se não documentado.

**How to apply:** os 3 pontos canônicos de leitura são `VinculoQueryRepository` (lista interna `ListarProfissionaisDoEstabelecimento` e lista pública/seletores `ListarProfissionaisPublicoDoEstabelecimento`) e `TermoResolverDeVariaveis` (resolver de variáveis dos termos). Ao mexer em exibição/impressão de especialidade, confirmar a ordem do COALESCE (vínculo primeiro). Coluna `especialidade_convidada` já existe — sem migration. Relacionado: [[project_rbac_inativar_reativar_vinculo]].
