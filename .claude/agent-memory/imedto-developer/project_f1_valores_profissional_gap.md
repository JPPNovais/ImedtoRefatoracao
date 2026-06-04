---
name: project-f1-valores-profissional-gap
description: F1 do briefing 2026-06-04_006 — tabela orcamento_valor_profissional vazia é gap de UI, não bug de escrita
metadata:
  type: project
---

A tabela `orcamento_valor_profissional` fica vazia porque **não há UI de cadastro** na `OrcamentoSettingsView` atual. O serviço `orcamentoCatalogoService.criarValorProfissional` existe no frontend e o handler `CriarValorProfissionalCommandHandler` existe no backend — mas nenhuma aba das Settings (`ProcedimentosTab`, `EquipeTab`, `ProdutosTab`, `AnestesistasTab`, `PacotesTab`, `OutrasConfigsTab`) implementa o fluxo de cadastro.

**Why:** Funcionalidade do legado Vue+Supabase que ficou como stub no service durante a refatoração mas sem componente correspondente.

**How to apply:** Quando o usuário reportar "tabela vazia de valores-profissional", confirmar que é gap de feature e escalar para briefing próprio — não é bug de escrita nem de permission gate. A aba Settings que deveria ter esse cadastro ainda não foi implementada.
