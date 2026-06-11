# Financeiro F3B — Addendum: widget flutuante "Próximos passos" (substitui o modal) + fix do fundo transparente

**Refere-se a**: `2026-06-10_012_financeiro-f3b-conduta-pendencias.md`
**ID**: 2026-06-10_012-addendum
**Status**: Aprovado por usuário em 2026-06-11 (decisão de UX tomada vendo produção)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P/M
**Áreas regressivas tocadas**: prontuário (pós-save de evolução), página do paciente (painel persistente), design system (tokens HSL). **Sem backend.**
**Numeração de CAs**: continua em **CA190** (último CA usado na trilha financeiro é CA189).

---

## 1. Por que este addendum existe

Duas motivações, decididas pelo usuário em 2026-06-11 observando o comportamento em produção:

1. **Bug visual (raiz já diagnosticada)** — o modal `ProximosPassosModal.vue` renderiza com **fundo transparente**: o card aparece sobreposto à página e é percebido como inclicável. Causa técnica confirmada no código: a regra `.proximos-modal { background: var(--card); ... }` ([`ProximosPassosModal.vue:132`](../frontend/src/components/prontuario/ProximosPassosModal.vue)) usa o token **cru**, mas os tokens de cor do DS são **triplos HSL** (`--card: 0 0% 100%` em [`main.css:19`](../frontend/src/assets/main.css)). Sem envolver em `hsl()`, o valor `0 0% 100%` é inválido para `background` e o navegador descarta a regra → card transparente. O padrão correto já existe no próprio projeto: `background: hsl(var(--card))` ([`main.css:517`](../frontend/src/assets/main.css)). O mesmo vale para `border: 1px solid var(--border)` (linha 134/148/149) → deve ser `hsl(var(--border))`.

2. **Decisão de UX que substitui o modal** — em vez de um modal central que bloqueia a página depois de salvar, o usuário quer um **painel flutuante ancorado no canto inferior direito, estilo widget de chat**. Não bloqueia a tela, permanece visível enquanto o profissional navega, e é a porta para concluir as pendências sem perder o contexto da página.

**O modal central deixa de ser a UX de pós-save.** Este addendum redefine o comportamento pós-save para um widget flutuante. O briefing original (CA59–CA75) permanece **intocado e válido** em tudo o mais: criação de pendências, conclusão automática por gatilho, conclusão manual, painel persistente do paciente, multi-tenant, RBAC, LGPD, append-only.

## 2. Persona-alvo

- **Profissional** — acabou de salvar a evolução com ações de conduta marcadas; o widget aparece no canto e o acompanha enquanto ele navega para executar cada ação (criar receita, agendar retorno, criar orçamento). Não quer ser bloqueado por um modal central.
- **Frequência**: a cada evolução salva com ≥1 ação de conduta marcada. Alta.

## 3. Escopo

**Inclui**:
- Substituir a UX do modal central por um **widget flutuante** ancorado no canto inferior direito (`position: fixed`), estilo widget de chat, que **não bloqueia** a página (sem overlay que captura cliques fora dele).
- **Fundo sólido do DS via tokens HSL corretos** (`hsl(var(--card))`, `hsl(var(--border))`) — corrige a raiz do bug visual.
- **Header**: título "Próximos passos" + contador "X de N concluídas" + botões **minimizar** e **fechar**.
- **Corpo**: lista das ações marcadas, cada uma com **link direto** (reusa o `rotaParaAcao` atual, incluindo `evolucaoId` para "Criar orçamento") e **estado concluído/aberto**.
- **Atualização do estado concluído** sem websocket: re-fetch leve das pendências abertas (reusa `pendenciaService.listarAbertas`) quando a rota volta para a página do paciente **ou** quando o widget é expandido a partir do estado minimizado.
- **Minimizar** → vira **pílula compacta** no canto (ícone + contador); clicar na pílula expande de volta.
- **Fechar ("Fazer depois")** → o widget some da sessão; as pendências continuam no **painel persistente do paciente** (inalterado).
- **Por-sessão de navegação**: o widget não persiste entre reloads/refresh. Quem persiste é o painel do paciente (briefing original, CA68).
- **Mobile**: respeitar o viewport (`max-width`, sem cobrir a tela inteira).

**Não inclui (anti-escopo)**:
- Nenhuma mudança de **backend** — sem novo endpoint, sem nova entidade, sem nova migration. Reusa `GET /paciente/{id}/pendencias` e os handlers de conclusão existentes.
- Nenhuma mudança no **painel persistente do paciente** (`PainelPendencias.vue` / `PacienteDetalheView.vue`) — permanece como entregue no briefing original.
- Nenhuma mudança na **criação** nem na **conclusão automática/manual** de pendências (CA59–CA67 do original intactos).
- Persistência do widget entre reloads, websocket/realtime, badge global de sino — fora.

## 4. Regras de negócio (incrementais)

- **R16** (gatilho do widget) — Ao salvar uma evolução com ≥1 ação de conduta marcada, em vez do modal central, **abre-se o widget flutuante** no canto inferior direito, no estado **expandido**, com a lista das ações marcadas. Se nenhuma ação foi marcada, o widget **não aparece** (espelha CA61). Mora em: Front (handler de pós-save da view de prontuário, onde hoje se aciona o modal). Validada em: front.

- **R17** (não bloqueante) — O widget é `position: fixed` ancorado no canto inferior direito, **acima do conteúdo** (z-index coerente com a camada de overlays do DS, abaixo de modais verdadeiros de confirmação). **Não** usa overlay de fundo que intercepte cliques fora dele — a página continua interativa por trás. Mora em: Front (CSS scoped do widget). Validada em: front.

- **R18** (fundo sólido — fix da raiz) — Todo fundo/borda do widget usa **token HSL embrulhado**: `hsl(var(--card))`, `hsl(var(--border))`, `hsl(var(--muted))` etc. — **nunca** `var(--card)` cru. Mora em: Front (CSS scoped). Validada em: front + inspeção de CSS computado (sem `background` inválido/descartado).

- **R19** (re-fetch do estado concluído) — O widget reflete a conclusão das pendências re-buscando as abertas via `pendenciaService.listarAbertas(pacienteId)` em dois momentos: (a) quando a **rota volta** para a página do paciente após o usuário navegar para executar uma ação; (b) quando o widget é **expandido** a partir do minimizado. Sem websocket/polling. Uma ação não mais presente na lista de abertas é renderizada como **concluída** (riscada/check). Mora em: Front (watch de rota + reuso do service). Validada em: front.

- **R20** (contador) — O header mostra "X de N concluídas", onde N = total de ações que o widget abriu (as marcadas no save) e X = quantas dessas já não constam mais em `listarAbertas` (ou foram concluídas manualmente). Mora em: Front (derivado do estado local + re-fetch de R19). Validada em: front.

- **R21** (minimizar / pílula) — "Minimizar" colapsa o widget em uma **pílula compacta** (ícone + contador "X/N") fixa no mesmo canto; clicar na pílula **expande** de volta (disparando o re-fetch de R19b). O estado minimizado/expandido vive só em memória da sessão. Mora em: Front. Validada em: front.

- **R22** (fechar / "Fazer depois") — "Fechar" (ou "Fazer depois") remove o widget da sessão atual. **As pendências não se perdem** — continuam abertas no banco e visíveis no painel persistente do paciente. Reabrir o widget na mesma sessão não é requisito (some até o próximo save). Mora em: Front. Validada em: front.

- **R23** (por-sessão / sem persistência entre reloads) — O widget **não** reaparece após reload/refresh da página; sua existência é efêmera da navegação pós-save. A persistência da pendência é responsabilidade do painel do paciente (CA68 original). Mora em: Front (estado não persistido em storage). Validada em: front.

- **R24** (link direto reusado) — Cada item do widget usa o **mesmo `rotaParaAcao(pacienteId, acao, evolucaoId)`** já existente em [`pendenciaService.ts`](../frontend/src/services/pendenciaService.ts), incluindo o `evolucaoId` para `CriarOrcamento` (pré-preenchimento do orçamento, igual ao modal atual). `MarcarProcedimentoRealizado` continua sem rota (conclusão pelo painel/modal de procedimento). Mora em: Front (reuso, sem nova lógica de rota). Validada em: front.

- **R25** (mobile) — Em viewport estreito o widget respeita `max-width` (não excede a largura útil da tela menos as margens) e **não cobre a tela inteira**; permanece ancorado no canto inferior, acima do conteúdo. Mora em: Front (CSS responsivo). Validada em: front.

## 5. UX e fluxo

**Widget expandido** (canto inferior direito, `position: fixed`):
- Header: ícone + **"Próximos passos"** + contador **"X de N concluídas"** + botão **minimizar** (ícone "—" ou chevron) + botão **fechar** ("X", tooltip/aria "Fazer depois").
- Corpo: lista das ações marcadas. Cada linha:
  - **aberta** → rótulo (`ACAO_LABELS[acao]`) + botão de link "ir" (seta) usando `rotaParaAcao` (com `evolucaoId` para orçamento); `MarcarProcedimentoRealizado` mostra "Conclusão manual pelo painel" (sem link), igual ao modal atual.
  - **concluída** → rótulo com check/risco, sem botão de ir.
- Rodapé opcional curto: "Visíveis no painel do paciente" (reaproveitar a copy do modal atual).

**Widget minimizado** (pílula): ícone + contador compacto "X/N" no canto inferior direito. Clique → expande.

**Fechado**: nada na tela. Pendências seguem no painel persistente do paciente.

**Design system / tipografia**: reutilizar tokens tipográficos (CLAUDE.md §5 — `--text-sm`, `--font-weight-*`, sem literais). Fundo/bordas em `hsl(var(--...))`. Botões via padrão DS (`.btn-*` ou `AppButton`), sem `<button>` cru com borda default (gotcha do preflight desligado). Reaproveitar o máximo do markup/CSS de `ProximosPassosModal.vue` adaptando de modal-central para widget-canto.

## 6. Critérios de aceite (testáveis) — continuam em CA190

- **CA190 (widget abre no pós-save com itens)**: Dado um profissional que salva uma evolução com ≥1 ação de conduta marcada, Quando a resposta retorna, Então o **widget flutuante** "Próximos passos" aparece ancorado no canto inferior direito (`position: fixed`), no estado expandido, listando exatamente as ações marcadas — e **nenhum modal central** é exibido.

- **CA191 (não abre sem itens)**: Dado o salvamento de uma evolução **sem** ação de conduta marcada, Quando a resposta retorna, Então o widget **não** aparece (espelha CA61 do original).

- **CA192 (fundo sólido — fix da raiz)**: Dado o widget renderizado, Quando o CSS computado é inspecionado, Então `background` resolve para a cor sólida do card via `hsl(var(--card))` (não `var(--card)` cru, que seria inválido/descartado) e as bordas usam `hsl(var(--border))` — o card é opaco e clicável. **Verificação explícita do gotcha**: nenhuma declaração de cor no componente usa token de cor HSL sem `hsl()`.

- **CA193 (não bloqueante)**: Dado o widget aberto, Quando o usuário clica em qualquer elemento da página por trás do widget (fora da sua área), Então o clique é recebido pela página (não há overlay capturando o evento) e o widget permanece visível.

- **CA194 (header e contador)**: Dado o widget aberto com N ações, Quando renderiza, Então o header mostra título "Próximos passos", contador "0 de N concluídas" e os botões minimizar e fechar.

- **CA195 (link direto reusado, incl. orçamento)**: Dado o widget com a ação "Criar orçamento" e a evolução recém-salva, Quando o usuário clica no link da ação, Então navega via `rotaParaAcao(pacienteId, 'CriarOrcamento', evolucaoId)` para `/orcamentos/novo?evolucaoId=...&pacienteId=...` (mesma rota e pré-preenchimento do modal atual); demais ações usam suas rotas existentes; `MarcarProcedimentoRealizado` não tem link.

- **CA196 (re-fetch ao voltar para a página)**: Dado que o usuário clicou em uma ação do widget, executou-a e a rota voltou para a página do paciente, Quando a página é reexibida, Então o widget re-busca as pendências abertas (`pendenciaService.listarAbertas`) e a ação concluída aparece como **concluída** (riscada/check), com o contador atualizado para "X de N".

- **CA197 (minimizar e expandir)**: Dado o widget expandido, Quando o usuário clica em "minimizar", Então ele colapsa em uma **pílula** compacta (ícone + "X/N") fixa no canto; Quando clica na pílula, Então o widget **expande** de volta e re-busca as pendências abertas (refletindo conclusões ocorridas enquanto minimizado).

- **CA198 (fechar / "Fazer depois")**: Dado o widget aberto, Quando o usuário clica em "Fechar" / "Fazer depois", Então o widget some da sessão **e** as pendências **permanecem** abertas no banco e visíveis no painel persistente do paciente (nenhuma pendência é concluída nem apagada).

- **CA199 (por-sessão — sem persistência entre reloads)**: Dado o widget visível (expandido ou minimizado), Quando o usuário dá refresh/reload na página, Então o widget **não** reaparece automaticamente (efêmero da navegação pós-save), enquanto as pendências continuam no painel persistente.

- **CA200 (mobile — não cobre a tela)**: Dado um viewport estreito (mobile), Quando o widget é exibido, Então ele respeita `max-width` (não excede a largura útil da tela) e **não cobre a tela inteira**, permanecendo ancorado no canto inferior, acima do conteúdo.

- **CA201 (regressão — painel e pendências intactos)**: Dado o fluxo completo de pendências, Quando o widget é introduzido, Então: (a) o painel persistente do paciente continua listando/concluindo pendências como antes (CA68/CA67 do original); (b) a criação de pendências ao salvar a evolução continua intacta (CA59); (c) a conclusão automática por gatilho continua intacta (CA63/CA64); (d) **nada muda no backend** — nenhum endpoint/migração novo.

## 7. Riscos e dependências

- **Risco — z-index**: o widget deve ficar acima do conteúdo, mas **abaixo** de modais reais de confirmação (ex.: o modal de "Marcar procedimento realizado" da F4) para não conflitar. Mitigação: usar uma camada de z-index coerente com o DS, abaixo da camada de modais (`modal-overlay` usa `z-index: 900`; o widget deve ficar abaixo disso, ex.: faixa 700–800). CA193 cobre o não-bloqueio.
- **Risco — re-fetch redundante**: o watch de rota pode disparar `listarAbertas` mais vezes que o necessário. Aceitável — é um GET leve já indexado (CA74 original). Não introduzir polling.
- **Dependência**: nenhuma nova. Reusa `pendenciaService`, `rotaParaAcao`, `ACAO_LABELS`, tipos `AcaoPendencia`/`PendenciaAberta` e o endpoint existente.
- **Risco — reaproveitar `ProximosPassosModal.vue`**: ao converter de modal para widget, garantir que o `.test.ts` associado seja ajustado/substituído pelo dev e revalidado pelo QA (o componente muda de natureza). Não deixar teste do modal central testando comportamento que não existe mais.

## 8. Observações para execução

**Não-negociável**:
- Corrigir a **raiz** do bug: toda cor/borda do componente via `hsl(var(--token))`. É o ponto central do CA192 — não basta trocar por uma cor literal; usar o token embrulhado, como `main.css:517`.
- **Zero backend**: este addendum é puramente de front. Se o dev sentir necessidade de mexer no back, parar e reportar — algo está fora do escopo combinado.
- **Painel persistente do paciente intocado** (CA201a). O widget é uma camada nova de UX pós-save, não substitui o painel.
- Tipografia por tokens (CLAUDE.md §5) e DS-first (botões via DS, sem `<button>` cru — gotcha do preflight desligado).

**Liberdade técnica do dev**:
- Reaproveitar/renomear `ProximosPassosModal.vue` como widget (ex.: `WidgetProximosPassos.vue`) **ou** evoluir o componente atual mantendo o nome — escolha do dev, desde que o consumidor (view de prontuário pós-save) passe a renderizar o widget e os CAs passem.
- Mecânica exata do re-fetch (watch de `route` na view do paciente vs. listener no widget) — escolha do dev, desde que CA196/CA197 passem sem websocket/polling.
- Forma da pílula minimizada (ícone + "X/N") — liberdade visual dentro do DS.

**Reuso obrigatório**: `pendenciaService.listarAbertas`, `rotaParaAcao` (com `evolucaoId`), `ACAO_LABELS`, tipos do `pendenciaService.ts`. Não duplicar lógica de rota nem de fetch.

## 9. Atualização de documentação

- **Nenhum delta obrigatório em `Docs/`**: a mudança é de UX de front (modal → widget) sem novo padrão de arquitetura/infra/LGPD. A entidade `PendenciaAtendimento`, o padrão de conclusão por gatilho e o endpoint já estão documentados pelo briefing original em `Docs/ARQUITETURA.md`.
- **`Docs/DESIGN.md`** — *opcional*: se o widget virar componente reutilizável de DS (ex.: `AppFloatingWidget`/`WidgetProximosPassos`), o `imedto-developer` o documenta na entrega; se ficar específico do fluxo de prontuário, não precisa. Decisão do dev conforme reuso real.
- **`Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md`** — atualizar a linha de status do bloco **F3B** registrando este addendum (widget flutuante substitui o modal central; fix do fundo transparente `var(--card)` → `hsl(var(--card))`).
