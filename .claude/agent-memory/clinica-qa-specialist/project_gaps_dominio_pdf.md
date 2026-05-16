---
name: gaps-dominio-pdf-receita-estabelecimento
description: Gaps de campos no domínio que afetam templates de PDF — Estabelecimento sem email/site/tagline/cidade; Receita sem flag UsoContinuo.
metadata:
  type: project
---

Em 2026-05-16, ao planejar o redesign dos PDFs (`PLANO_REDESIGN_PDF.md`), confirmei que o domínio atual **não tem**:

- **`Estabelecimento`**: campos `Email`, `Site`, `Tagline`, `Cidade`, `CEP` separados. Só existe `Endereco` (string única), `Telefone`, `Cnpj`, `FotoUrl` (vira logo do PDF), `NomeFantasia`, `RazaoSocial`.
- **`Receita`**: flag `UsoContinuo`. Os tipos regulatórios existentes são `Comum`, `Controlada`, `Antibiotico`, `Especial` (enum `TipoReceita`) + `TipoNotificacao` (A/B/C/Especial) só para Controlada. "Uso contínuo" do mock de design é **conceito clínico que ainda não existe** no aggregate.
- **Assinatura digital**: o campo `AssinaturaDigitalStatus` existe (enum), mas hoje toda receita emitida nasce `NaoAssinada`. Não há integração ICP-Brasil/Memed — o selo verde do mock seria falsa afirmação.

**Why:** templates de PDF mock referenciam dados que não estão no modelo. Sem checar, o agente fullstack pode (a) pedir migration desnecessária no meio da implementação, (b) hardcodar placeholder em vez de fallback condicional, (c) exibir afirmação de ICP-Brasil falsa.

**How to apply:** ao receber tarefa que toque PDFs/templates/comunicação externa de receita/estabelecimento, conferir antes os arquivos:
- `backend/src/Services/Imedto.Backend.Domain/Estabelecimentos/Estabelecimento.cs`
- `backend/src/Services/Imedto.Backend.Domain/Receitas/Receita.cs` + `TipoReceita.cs` + `StatusAssinaturaDigital.cs`
- `backend/src/Services/Imedto.Backend.Domain/Receitas/ConfiguracaoReceitaEstabelecimento.cs` (tem `CabecalhoHtml`/`RodapeHtml`/`EmissorPadrao` configuráveis pelo dono).

Se o requisito pede campo inexistente, decidir entre: omitir condicional, propor migration explícita no plano, ou marcar como backlog. Não inventar dado.

Relacionado: [[gap-uso-continuo-receita]] (a criar quando virar tarefa).
