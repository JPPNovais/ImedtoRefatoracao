---
name: session-briefing-005-tipo-prazo-entrega-2026-06-04
description: Briefing 2026-06-04_005 tipo de prazo de entrega fornecedor — 2 bugs Tipo A encontrados, pipeline devolvida ao dev
metadata:
  type: project
---

Pipeline do briefing 2026-06-04_005 devolvida ao dev com 2 bugs Tipo A. Nenhum commit efetuado.

**Bug 1 (bloqueante):** `CadastrosEstoqueController.cs` não mapeou `TipoPrazoEntrega`.
- `FornecedorPayloadDto` (linha 386-393) não tem o campo.
- Mapeamento em `CriarFornecedor` (linhas 238-248) e `AtualizarFornecedor` (linhas 257-268) não repassa o campo ao command.
- O arquivo não foi tocado no diff — foi esquecido pelo dev.
- Resultado: CA1, CA3, CA4 falhariam em produção (sempre persiste 'corridos').

**Bug 2 (bloqueante):** Carona `OnboardingView.vue:704` com erro TS2322.
- `v-maska="['(##) ####-####', '(##) #####-####']"` — array não é assignable ao tipo declarado pela diretiva.
- `npm run build` retorna exit code 2.
- A intenção (suportar telefone fixo e celular) é válida mas a tipagem está errada.

**Why:** Dev implementou todas as camadas (Domain/Command/Handler/DTO/EF/Dapper/Frontend) mas esqueceu de atualizar o controller (camada HTTP) e trouxe uma carona com tipo incompatível.

**How to apply:** Ao revisar futuras features de estoque, checar explicitamente `CadastrosEstoqueController.cs` — o controller é um arquivo de mapeamento separado dos outros artefatos, fácil de esquecer.

Números da suíte no momento da revisão: 1271 backend / 412 frontend (Vitest) passando. Build backend OK. Build frontend falhou.
