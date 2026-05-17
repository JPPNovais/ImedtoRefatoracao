---
name: window-open-noopener-quebra-navegacao
description: Usar "noopener,noreferrer" em window.open impede navegação posterior via janela.location.href em Chrome moderno.
metadata:
  type: feedback
---

`window.open("about:blank", "_blank", "noopener,noreferrer")` no Chrome moderno (testado em 2026-05-17) abre a aba e retorna um handle, mas qualquer atribuição posterior em `janela.location.href = blobUrl` é **silenciosamente ignorada**. A aba fica permanentemente em about:blank.

**Why:** A flag `noopener` deliberadamente quebra o link entre opener e a nova janela — isso inclui a navegação programática. MDN: "Specifying noopener causes the returned reference to lose the ability to navigate the new window in some implementations." Chrome 88+ implementou essa quebra.

**How to apply:** Quando o padrão é "abrir aba síncrono no clique → gerar blob async → navegar a aba aberta" (típico em export de PDF client-side com jsPDF):
- **NÃO usar** `"noopener,noreferrer"` na string de features de `window.open`. Pode passar string vazia ou apenas `"_blank"` como target.
- Risco: opener fica acessível na nova aba via `window.opener`. Mitigação para blobs locais (mesma origem): risco mínimo, blob não tem código ativo.
- Alternativas se segurança de opener for crítica: (a) gerar PDF primeiro e usar anchor `<a>` programatico com target=_blank rel=noopener — funciona mas perde o padrão "abre aba imediato"; (b) escrever HTML completo na aba via `janela.document.write` em vez de location.href — também funciona; (c) usar `window.open(blobUrl, ...)` direto sem about:blank — mas precisa do blob pronto, contradiz o padrão sincronicidade.

Relacionado: [[qa-posdeploy-043704e-2026-05-17]] — bug encontrado em prod.
