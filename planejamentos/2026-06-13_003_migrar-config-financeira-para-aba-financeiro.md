# Migrar Tabela de preços e Taxa por forma de pagamento para a aba Configurações do /financeiro

**ID**: 2026-06-13_003
**Status**: Aprovado por usuário em 2026-06-13
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: nenhuma (migração de UI/navegação — sem mudança de backend nem schema)

## 1. Contexto e motivação

Hoje a configuração financeira do estabelecimento está dividida em dois lugares distintos:

- **Comissões por profissional** vive em `/financeiro › aba Configurações` (`FinanceiroConfigTab.vue`, briefings 2026-06-11_002 / 2026-06-11_004), onde também existem dois cards placeholder **"Em breve"** (Taxa de cartão e Tabela de preços — decisão D4 do briefing 2026-06-11_002).
- **Tabela de preços de consulta** e **Taxa por forma de pagamento** (CRUDs reais, briefing F1) vivem em `Configurações do estabelecimento › Faturamento › Financeiro` (`FinanceiroConfigView.vue`, montada via `?secao=financeiro` no `EstabelecimentoView`).

Resultado: o usuário precisa visitar duas telas separadas para configurar a operação financeira, e a aba `/financeiro › Configurações` exibe "Em breve" para funcionalidades **que já existem em outro lugar**. Isso é atrito de navegação e inconsistência de produto — a aba Configurações do módulo Financeiro deve ser o lar único de toda configuração financeira.

Esta demanda **unifica** as três configurações (Comissões + Tabela de preços + Taxa) na aba `/financeiro › Configurações`, remove o local antigo no Estabelecimento e redireciona a rota legada. É migração de UI e navegação — **nenhuma regra de negócio, endpoint ou schema é alterado**.

## 2. Persona-alvo

- **Dono** do estabelecimento: configura comissões (já podia), tabela de preços e taxas — agora num só lugar.
- **Admin / Recepção com permissão `configuracoes.gerenciar`**: gerencia tabela de preços e taxas (mantém o gate atual do backend).
- Momento da jornada: setup inicial e manutenção esporádica da configuração financeira do estabelecimento. Baixa frequência, alto valor de consistência.

## 3. Escopo

**Inclui**:
- Absorver os CRUDs de **Tabela de preços de consulta** e **Taxa por forma de pagamento** (hoje em `FinanceiroConfigView.vue`) **dentro** de `FinanceiroConfigTab.vue`, adotando o estilo visual `.cfg-card` do destino, substituindo os dois cards "Em breve" na posição/grid atual (D2 + D3).
- Aplicar gate de renderização no front: os dois blocos só aparecem para quem tem `configuracoes.gerenciar`; quem não tem vê estado "sem permissão" (D1).
- **Deletar** `FinanceiroConfigView.vue` — fonte única, sem componente órfão (D2).
- Remover o item **"Financeiro"** do grupo **"Faturamento"** da sidebar de Configurações do estabelecimento e todo o cabeamento órfão associado (D4): item do menu, entrada em `TODAS_SECOES`, bloco `secaoAtiva === 'financeiro'`, import lazy `PainelFinanceiro`.
- Redirecionar a rota legada `/configuracoes/financeiro` (name `FinanceiroSettings`) — hoje aponta para `/estabelecimento?secao=financeiro` — para `/financeiro?aba=config` (D5).
- Adicionar suporte a **deep-link `?aba=`** em `FinanceiroView.vue`: ler a query no mount e manter sincronizado ao trocar de aba, espelhando o padrão `?secao=` do `EstabelecimentoView` (D5).
- Preservar as correções não-commitadas no working tree de `FinanceiroConfigView.vue` ao absorver o markup (ver §9).

**Não inclui**:
- Qualquer mudança de backend, endpoint, contrato de API ou regra de negócio.
- Qualquer migration / mudança de schema. **Esta demanda NÃO aciona `imedto-database`.**
- Afrouxar o RBAC do backend (o gate `configuracoes.gerenciar` permanece exatamente como está — D1).
- Mexer no card de Comissões (continua só-Dono, como já é — D1/D3) além de garantir que permaneça no topo da aba.
- Implementar as taxas/tabela de preço em qualquer outra tela.
- Corrigir o anti-padrão `var(--spacing-N)` em outros arquivos do projeto (dívida cross-cutting apenas registrada — §8/§9).

## 4. Regras de negócio

> Toda regra de negócio desta feature **já existe e permanece no backend**. Esta entrega não cria nem altera regra de domínio; apenas relocaliza a UI que a consome e ajusta a renderização condicional no front.

- **R1 (RBAC — backend, inalterado)**: os endpoints de tabela de preço (`carregarTabelaPreco`/`salvarTabelaPreco`/`inativarTabelaPreco`) e de taxa por forma (`carregarConfigTaxa`/`salvarConfigTaxa`) exigem `configuracoes.gerenciar`. Mora em: Handler/controller do backend. Validado em: **back (fonte da verdade, 403)** + front (gate de renderização). **ZERO mudança no backend.** O dev deve confirmar no controller que o gate é `configuracoes.gerenciar` antes de espelhar no front; se divergir, parar e reportar (não afrouxar nada).
- **R2 (gate de renderização — front)**: os blocos Tabela de preços e Taxa só são renderizados quando o usuário tem `configuracoes.gerenciar` (via `permissoesStore`). Sem a permissão → exibe estado "sem permissão" (mensagem genérica, sem revelar dado). Mora em: Front (`FinanceiroConfigTab.vue`). Validado em: front (UX) com espelho no back (R1).
- **R3 (Comissões — front, inalterado)**: o card Comissões continua visível apenas para Dono (`ehDono`), no topo da aba. Mora em: Front. Validado em: front + back (já existente).
- **R4 (multi-tenant — backend, inalterado)**: toda query/comando de tabela de preço e taxa já filtra `estabelecimento_id` no backend. Mora em: Handler/Query do backend. Validado em: back (premissa não-negociável já implementada). **Esta entrega não toca nesse filtro** — declaração explícita de que multi-tenant permanece intacto.

## 5. Modelo de dados

**Nenhuma mudança.** Sem tabelas novas, sem colunas, sem índices, sem migration. Os dados de tabela de preço e taxa por forma de pagamento já existem e são consumidos pelos endpoints atuais via `useCobrancaConfigStore` (`cobrancaStore`). Multi-tenant (`estabelecimento_id`) e audit permanecem como já implementados no backend.

> **`imedto-database` NÃO é acionado nesta demanda.**

## 6. UX e fluxo

### Layout da aba `/financeiro › Configurações` (ordem D3, menor mudança visual)

```
┌─ aba Configurações ──────────────────────────────────────────────┐
│                                                                    │
│  [Card Comissões por profissional]            ← topo, só Dono     │
│  (cfg-card cfg-span — inalterado, R3)                              │
│                                                                    │
│  ── gate: configuracoes.gerenciar ──                               │
│  Com permissão:                                                    │
│    grid 2-col (cfg-grid), substituindo os cards "Em breve":        │
│    ┌────────────────────────┐  ┌────────────────────────┐         │
│    │ Taxa de cartão / por    │  │ Tabela de preços de     │        │
│    │ forma de pagamento      │  │ consulta                │        │
│    │ (CRUD real, era card    │  │ (CRUD real, era card    │        │
│    │  "Em breve")            │  │  "Em breve")            │        │
│    └────────────────────────┘  └────────────────────────┘         │
│                                                                    │
│  Sem permissão (mas com acesso à aba):                             │
│    estado "sem permissão" no lugar dos dois blocos                 │
│    (mensagem genérica, ex.: "Você não tem permissão para           │
│     gerenciar tabela de preços e taxas.")                          │
└────────────────────────────────────────────────────────────────────┘
```

- A posição de cada bloco no grid `.cfg-grid` substitui o respectivo card "Em breve" atual (Taxa de cartão à esquerda, Tabela de preços à direita) — menor mudança visual possível.
- O conteúdo absorvido adota o estilo `.cfg-card` do destino (cabeçalho `cfg-card-h`, ícone `cfg-ic`, etc.), **não** o estilo `.fin-config`/`AppCard` da origem. As correções de espaçamento (§9) são adaptadas ao novo estilo, não copiadas literalmente.

### Componentes do design system reutilizados
- `AppButton`, `AppField`, `AppInputDecimal`, `AppSelect`, `AppEmptyState`, `AppModal`, `AppSearchInput`, `AppToast` (já usados na origem/destino).
- `useDebouncedRef` para a busca de profissionais na tabela de preços (300ms — preservar).

### Estados (preservar nos dois blocos absorvidos)
- **Carregando**: indicador de loading enquanto `store.carregando`.
- **Vazio (tabela de preços)**: `AppEmptyState` "Nenhum preço cadastrado" com CTA.
- **Vazio (taxa)**: mensagem "Nenhuma forma de pagamento ativa…".
- **Erro (salvar preço)**: mensagem genérica de erro no modal (`erroPreco`).
- **Sucesso**: comportamento atual (lista recarrega; toast onde já existe).
- **Sem permissão (novo)**: estado dedicado no lugar dos dois blocos quando falta `configuracoes.gerenciar`.

### Navegação / deep-link
- `/financeiro?aba=config` abre direto na aba Configurações.
- Trocar de aba atualiza a query (`router.replace`, sem poluir histórico) — espelha o padrão `?secao=` do `EstabelecimentoView` (linhas ~150-168).
- Valores de `aba` válidos: `visao-geral`, `caixa`, `comissoes`, `config`. Query inválida/ausente → fallback para `visao-geral` (aba default atual).
- Manter `abasCarregadas` (lazy CA188): abrir via deep-link em `config` deve marcar `config` como carregada e instanciar a aba sob demanda.
- Rota legada `/configuracoes/financeiro` (name `FinanceiroSettings`, `router/index.ts` ~207-211) passa a redirecionar para `/financeiro?aba=config` (hoje vai para `/estabelecimento?secao=financeiro`).

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — tabela de preços)**: Dado um Dono em `/financeiro › Configurações`, Quando cadastra/edita/inativa um preço de consulta, Então a operação persiste pelo mesmo endpoint de hoje e a lista reflete a mudança, idêntica ao comportamento atual da tela antiga.
- **CA2 (caminho feliz — taxa)**: Dado um usuário com `configuracoes.gerenciar` em `/financeiro › Configurações`, Quando edita a taxa de uma forma de pagamento e salva, Então a taxa persiste pelo mesmo endpoint de hoje e a edição inline reflete o valor salvo.
- **CA3 (RBAC — front com permissão)**: Dado um usuário com `configuracoes.gerenciar`, Quando abre a aba Configurações, Então vê os blocos Tabela de preços e Taxa renderizados (substituindo os cards "Em breve").
- **CA4 (RBAC — front sem permissão)**: Dado um usuário sem `configuracoes.gerenciar` (mas com acesso ao módulo via `financeiro.ver`), Quando abre a aba Configurações, Então NÃO vê os blocos Tabela de preços/Taxa e vê em seu lugar o estado "sem permissão" com mensagem genérica.
- **CA5 (RBAC — backend é a fonte da verdade)**: Dado um usuário sem `configuracoes.gerenciar`, Quando (por qualquer via) dispara `salvarTabelaPreco` ou `salvarConfigTaxa`, Então o backend retorna 403 e nada é persistido — o gate do front é apenas UX, o backend não foi afrouxado.
- **CA6 (Comissões inalterado)**: Dado um Dono na aba Configurações, Quando a aba carrega, Então o card Comissões aparece no topo (full-width), com o mesmo comportamento de lista + modal de hoje; e Dado um não-Dono, Então o card Comissões não aparece.
- **CA7 (multi-tenant inalterado)**: Dado um usuário do estabelecimento B, Quando lista/edita tabela de preços ou taxas, Então só vê/altera dados do estabelecimento B (filtro `estabelecimento_id` no backend permanece intacto — nenhuma mudança de backend nesta entrega).
- **CA8 (LGPD — mensagem genérica)**: Dado um erro de validação ou permissão (422/403) ao salvar preço/taxa, Quando o back responde, Então a mensagem exibida é genérica e não contém PII (comportamento preservado da origem).
- **CA9 (estados preservados)**: Dado o bloco Tabela de preços sem registros, Quando carrega, Então exibe `AppEmptyState` com texto específico; Dado o bloco Taxa sem formas ativas, Então exibe a mensagem de vazio; Dado loading, Então exibe indicador de carregamento.
- **CA10 (performance — busca debounced)**: Dado o bloco Tabela de preços, Quando o usuário digita na busca de profissional, Então a busca usa debounce ~300ms (`useDebouncedRef`) antes de recarregar — preservado da origem.
- **CA11 (performance — aba lazy / CA188)**: Dado que o usuário abre `/financeiro` na aba Visão geral, Quando NÃO clica na aba Configurações, Então a aba Configurações não é instanciada e os endpoints de tabela de preço/taxa NÃO são chamados; os blocos só consultam ao abrir a aba.
- **CA12 (deep-link — abrir)**: Dado o usuário navega para `/financeiro?aba=config`, Quando a página monta, Então a aba Configurações abre diretamente (sem clique) e é marcada como carregada.
- **CA13 (deep-link — sincronizar)**: Dado o usuário em `/financeiro`, Quando troca para a aba Configurações, Então a URL passa a `/financeiro?aba=config` (via `router.replace`, sem novo item no histórico); e Quando troca para outra aba, a query `aba` atualiza de acordo.
- **CA14 (deep-link — query inválida)**: Dado o usuário navega para `/financeiro?aba=inexistente` (ou sem `aba`), Quando a página monta, Então cai no fallback para a aba `visao-geral` sem erro.
- **CA15 (redirect rota legada)**: Dado o usuário navega para `/configuracoes/financeiro`, Quando a rota resolve, Então é redirecionado para `/financeiro?aba=config` (não mais para `/estabelecimento?secao=financeiro`).
- **CA16 (remoção sem órfãos — sidebar)**: Dado o usuário abre Configurações do estabelecimento, Quando vê o grupo "Faturamento", Então NÃO existe mais o item "Financeiro" (apenas "Convênios" permanece no grupo); e o grupo continua renderizando corretamente.
- **CA17 (remoção sem órfãos — código)**: Dado o repositório após a entrega, Quando se busca por referências, Então: (a) `FinanceiroConfigView.vue` foi deletado; (b) o import lazy `PainelFinanceiro` foi removido de `EstabelecimentoView.vue`; (c) o bloco `secaoAtiva === 'financeiro'` foi removido; (d) `"financeiro"` foi removido de `TODAS_SECOES` e do tipo `SecaoId`; (e) o item do menu (linha ~105) foi removido; (f) nenhum link aponta para `secao=financeiro` ou para name `FinanceiroSettings` como destino de navegação ativa; (g) o projeto compila (`vite build` / `vue-tsc`) sem imports/variáveis órfãos.
- **CA18 (sem regressão no Estabelecimento)**: Dado o usuário acessa `/estabelecimento?secao=dados` e demais seções, Quando navega, Então todas as outras seções continuam funcionando — a remoção do `financeiro` não quebra a navegação `?secao=` nem o fallback para `dados`.
- **CA19 (tipografia / DS §5)**: Dado o código absorvido e ajustado, Quando inspecionado, Então não há literais de `font-size`/`font-weight` (apenas tokens `--text-*`/`--font-weight-*`) e `npm run check:typography -- --ci` passa; o estilo segue `.cfg-card` do destino.

## 8. Riscos e dependências

- **Risco — refs em Vue 3 com `v-if`/`hidden`**: o padrão atual de `FinanceiroView.vue` usa um elemento por aba (sem `v-for`) justamente para refs funcionarem. Ao adicionar deep-link, preservar esse padrão; não trocar por `v-for`.
- **Risco — `abasCarregadas` + deep-link**: abrir direto em `config` deve adicionar `config` ao Set antes do render, senão a aba não instancia. Validar via CA12.
- **Risco — gate do front divergente do back**: se o dev espelhar a permissão errada, blocos podem sumir para quem deveria ver (ou vice-versa). Mitigação: CA5 valida o 403 do back; o front confirma o gate `configuracoes.gerenciar` lendo o controller (R1).
- **Risco — perda das correções não-commitadas**: o working tree tem fixes em `FinanceiroConfigView.vue` (§9). Como o arquivo será deletado, o dev deve **portar** as correções para o markup absorvido, não perdê-las.
- **Dívida cross-cutting registrada (fora de escopo)**: o anti-padrão `var(--spacing-N)` (tokens inexistentes que colapsam padding/gap) pode existir em outros arquivos. Apenas registrado aqui — backlog separado, não corrigir nesta entrega.
- **Sem dependência de backend/DB**: entrega puramente front. Não há ordem de execução com `imedto-database`.

## 9. Observações para execução

**Não-negociável:**
- **ZERO mudança no backend.** Nenhum endpoint, handler, controller, contrato ou permissão é alterado. Se o dev sentir necessidade de tocar o back, parar e reportar (provável spec gap → Modo B).
- **Não afrouxar RBAC.** O gate `configuracoes.gerenciar` é a fonte da verdade no back; o front só espelha para UX. Confirmar o gate exato no controller antes de espelhar.
- **Fonte única:** `FinanceiroConfigView.vue` é **deletado**. Não deixar componente órfão nem rota apontando para seção morta.
- **Preservar correções não-commitadas** ao portar o markup de `FinanceiroConfigView.vue`:
  - A causa-raiz do design quebrado era o uso de tokens `--spacing-*` **inexistentes** no projeto (padding/gap colapsavam) — foram trocados por valores `rem` literais. Ao absorver no `.cfg-card`, usar o espaçamento do destino (que já usa `px`/`rem` literais coerentes), garantindo que nada colapse.
  - A busca caseira (ícone absoluto sobre placeholder) foi trocada por `AppSearchInput` — manter `AppSearchInput`.
- **Deep-link espelha `EstabelecimentoView`:** seguir o mesmo padrão de `resolverSecaoInicial` / `watch(abaAtiva → URL)` / `watch(route.query.aba → abaAtiva)` já existente para `?secao=`. Reuso de padrão, não invenção nova.

**Liberdade técnica:**
- Como organizar o markup absorvido dentro de `FinanceiroConfigTab.vue` (subcomponentes internos vs. blocos inline) fica a critério do dev, desde que respeite o estilo `.cfg-card`, o gate de renderização e os estados. Se extrair subcomponentes, mantê-los dentro de `views/financeiro/tabs/` e sem deixar órfãos.
- O texto exato do estado "sem permissão" fica a critério do dev, desde que genérico e sem PII.

**Reuso (premissa):** antes de criar qualquer helper/estado novo, conferir o que já existe em `cobrancaStore`/`useCobrancaConfigStore`, `financeiroService`, `vinculoService` — toda a lógica de dados já existe na origem e deve ser reaproveitada, não reescrita.

## 10. Atualização de documentação

- **Nenhum doc em `Docs/` precisa de atualização.** A demanda é relocalização de UI/navegação que segue padrões já documentados (abas com deep-link `?aba=`, master-detail `?secao=`, design system `.cfg-card`, gate de permissão no front com espelho no back). Não introduz componente novo no design system, padrão novo de store/service, recurso de infra, regra cross-cutting nem novo tratamento de PII.
- Observação: o anti-padrão `var(--spacing-N)` observado é dívida técnica para backlog — se vier a virar regra documentada (ex.: nota em `Docs/DESIGN.md`), será objeto de briefing próprio, fora deste escopo.
