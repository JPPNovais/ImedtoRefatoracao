---
name: feedback_aspas_mensagem_confirm_dialog
description: Como interpoler nomes em :mensagem do AppConfirmDialog sem quebrar o compilador Vue/TS
metadata:
  type: feedback
---

Ao usar `:mensagem="confirmacao.alvo ? \`Inativar ${confirmacao.alvo.nome}?\` : ''"` no template Vue, não incluir aspas ao redor da interpolação dentro do template literal.

**Por que:** Aspas normais ASCII `"` dentro de atributos Vue delimitados por `"` fecham o atributo prematuramente. Aspas tipográficas Unicode (U+201C/U+201D) parecem funcionar mas o compilador TypeScript do vue-tsc emite `TS1127: Invalid character` em algumas versões/configs.

**Como aplicar:** Usar simplesmente `Inativar ${nome}?` sem aspas ao redor da interpolação. Alternativa: usar aspas simples `'${nome}'` já que o delimitador do atributo é `"`. Não usar aspas normais `"${nome}"` dentro de atributo `"..."`.

Exemplo correto:
```vue
:mensagem="confirmacao.alvo ? `Inativar ${confirmacao.alvo.nome}?` : ''"
```
