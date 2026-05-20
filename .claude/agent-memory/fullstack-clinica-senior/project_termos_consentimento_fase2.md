---
name: project-termos-consentimento-fase2
description: Fase 2 entregue 2026-05-19 — UI de configuração de termos (TipTap) + Cidade/UF expostas no form do Estabelecimento. Backend Fase 1 já existia.
metadata:
  type: project
---

**Fase 2 da feature "Termos de consentimento" entregue em 2026-05-19** (não commitado pelo agent — usuário pediu QA antes).

**Why:** A Fase 1 (backend completo + 5 padrões seedados + Cidade/UF no aggregate `Estabelecimento`) já estava em `main` (commit `837b50d`). Faltava a tela de gestão. A próxima fase (Fase 3) depende destes modelos pra emitir termos nas telas de paciente/prontuário.

**How to apply:** Quando começar a Fase 3 (emissão de termos), o front já consulta `/api/termos/modelos` via `termoModeloService.listarModelos`. Reutilizar esse service. Hoje, o gate de UI é `termos.gerenciar_modelos`; pra emissão será `termos.emitir`.

**Entregas:**
- **Backend (mínimo)**:
  - `AtualizarEstabelecimentoCommand` + handler + controller request agora carregam `Cidade` e `Estado`; handler chama `AtualizarEndereco` (validação de UF de 2 letras vive no aggregate).
  - `EstabelecimentoDto` + `EstabelecimentoQueryRepository` agora retornam `cidade` e `estado` no SQL.
  - `dotnet build` verde, 156 testes unitários passando, sem warnings novos.
- **Frontend novo**:
  - `services/termoModeloService.ts` — 7 métodos espelhando 1:1 os endpoints `/api/termos/modelos/*`.
  - `constants/termoVariaveis.ts` — 19 variáveis catalogadas (paciente/estabelecimento/profissional/data), com `exemplo` para preview + `resolverVariaveisFake()`. 6 categorias com label e cor de AppBadge.
  - `components/termos/TermoEditorTipTap.vue` — editor WYSIWYG (StarterKit v3 + Placeholder) + Node custom `variavel` (atom inline) que renderiza chips `<span class="termo-variavel">{{chave}}</span>`. Expõe `inserirVariavel(chave)` via defineExpose.
  - `views/configuracoes/TermosListaView.vue` — abas Meus/Padrões; tabela com filtros (busca debounced + categoria + mostrar inativos), AppPagination, cards de padrões, drawer de visualização read-only.
  - `views/configuracoes/TermoFormView.vue` — 2 colunas: editor + sidebar de variáveis (clique insere) + card de preview opt-in.
  - Rotas `TermosModelos` (lista), `TermosNovo`, `TermosEditar` — todas gateadas por `termos.gerenciar_modelos` em `routePermissions.ts`. **Cuidado:** já existia rota `name: "Termos"` para `/termos` (página legal de Termos de Uso) — usei nomes `TermosModelos`/`TermosNovo`/`TermosEditar` para evitar colisão.
  - Menu: card atalho na aba "Geral" do `EstabelecimentoView`; `ROTAS_CONFIG` do `AppLayout` agora inclui `TermosModelos`.
- **Pacotes adicionados**: `@tiptap/vue-3`, `@tiptap/starter-kit`, `@tiptap/extension-placeholder` (NÃO `extension-underline` — StarterKit v3 já inclui). TermoFormView bundleia em ~366 kB (gzip 117 kB) — aceitável já que é lazy-load.
- **Testes Vitest**: 8 testes para `termoModeloService` + 2 smoke tests para `TermoEditorTipTap` = 10 testes novos, todos passando. Total da suite frontend: 281 testes.
- **Lint quebrado pre-existente** — `@typescript-eslint/recommended` não resolve; não relacionado às minhas mudanças.

**Restrições da entrega (não-fazer da Fase 2 que continuam pendentes):**
- Tela "Termos" do paciente (aba) — Fase 3
- Modal de emitir termo — Fase 3
- Página pública de aceite — Fase 4
- E-mail transacional (Resend) — Fase 4
- Migração LGPD legado — Fase 5

**Padrão adotado em telas de configuração com editor pesado:**
- 2 colunas grid: principal + sidebar 340px sticky.
- Sidebar com lista clicável "variável → editor" via método exposto no editor (`defineExpose({ inserirVariavel })`).
- Preview opt-in (botão Mostrar/Ocultar) — evita re-render constante e mantém o foco no editor.
- Editor com TipTap + Node custom para tokens dinâmicos (variáveis) renderizados como atom inline com classe global `.termo-variavel` (mesma classe consumida no preview e no drawer de visualização — garante consistência visual).
