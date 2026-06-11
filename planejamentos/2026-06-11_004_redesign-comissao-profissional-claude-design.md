# Redesign da seção "Comissão por profissional" (Configurações do Financeiro) para o Claude Design

**ID**: 2026-06-11_004
**Status**: Aprovado por usuário em 2026-06-11
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: nenhuma (frontend puro; configuração de comissão já existente, sem mudança de contrato)

## 1. Contexto e motivação

A seção "Comissões por profissional" da aba **Configurações** do módulo Financeiro (`FinanceiroConfigTab.vue`, visível só para o Dono — CA178 do briefing 2026-06-11_002) usa hoje um padrão "chips + formulário": o Dono clica no chip de um profissional, abrem dois campos (% consulta e % procedimento) e um botão "Salvar comissão". Só é possível ver/editar **um profissional por vez** — não há visão geral de quem tem qual comissão, e descobrir o percentual de cada um exige clicar um a um.

O design de referência (Claude Design) resolve isso com uma **lista**: cada profissional é uma linha mostrando o nome, os percentuais vigentes e um lápis para editar. Quem está no padrão do sistema ganha um badge "PADRÃO". A edição acontece em modal. Isso dá visão panorâmica e reduz cliques.

Referência visual: `Docs/Roadmap/prototipacao-financeiro/design-handoff/components/ClinicFinanceTabs.jsx` (linhas 237-252) e `screenshots/cf-config2.png`. CSS de referência (`.cfg-comm-row`, `.cfg-comm-name`, `.cfg-comm-pct`, `.cfg-padrao`, `.cfg-default-tag`) em `styles/clinic-finance.css` (~200-208). É referência **visual** — recriar o resultado em Vue 3 + design system, sem copiar a estrutura React nem valores literais de CSS.

## 2. Persona-alvo

**Dono do estabelecimento**, configurando comissões da equipe. Acessa esporadicamente (onboarding de profissional novo, revisão de regra de repasse). Precisa de visão geral rápida e edição pontual sem fricção.

## 3. Escopo

**Inclui**:
- Substituir o bloco "chips + formulário" da seção de comissão (template linhas 93-140, script 19-75, style 256-296 do arquivo atual) por uma **lista de profissionais** no estilo Claude Design.
- Cabeçalho da seção exibindo "Padrão do sistema: {percentualPadrao}%".
- Cada linha: ícone + nome do profissional, os **dois** percentuais vigentes (consulta e procedimento), badge "PADRÃO" quando aplicável, e botão de lápis para editar.
- Modal de edição (reuso de `AppModal`) com os dois campos (% consulta, % procedimento) + salvar/cancelar.
- Carregar a comissão de **todos** os profissionais ao abrir a aba (hoje só carrega ao clicar).
- Estado de carregamento, estado vazio (sem profissionais), estado de erro genérico.

**Não inclui**:
- Qualquer mudança de backend, contrato de API, schema ou migration. (Ver §8 — ponto a validar pelo dev quanto à estratégia de carregamento.)
- Os cards "Taxa de cartão" e "Tabela de preços" (permanecem "Em breve", intocados).
- Qualquer alteração na regra de cálculo de comissão (D5/Fase 7) — esta demanda é só de visualização e edição da configuração já existente.

## 4. Regras de negócio

- **R1** (badge PADRÃO): a badge "PADRÃO" aparece na linha de um profissional **quando ele não tem override** — ou seja, quando **ambos** os percentuais (consulta e procedimento) estão iguais ao `percentualPadrao` do sistema (hoje 30%). Mora em: Front (derivação de exibição a partir do `ConfigComissao` já retornado pelo back). Sem regra nova no backend — `obterConfigComissao` já devolve `percentualConsulta`, `percentualProcedimento` e `percentualPadrao`. Critério canônico: `linha.percentualConsulta === percentualPadrao && linha.percentualProcedimento === percentualPadrao`.
- **R2** (RBAC): a seção inteira de comissão só é renderizada para `ehDono === true` (premissa herdada da prop existente; CA178 do briefing 2026-06-11_002). Não-dono nunca vê a lista, o badge nem o lápis. Mora em: Front (`v-if="ehDono"`) com espelho no back (o endpoint de comissão já é protegido por RBAC do Dono — sem alteração).
- **R3** (salvar): ao confirmar o modal, persiste via `financeiroService.salvarConfigComissao({ profissionalUsuarioId, percentualConsulta, percentualProcedimento })` — **mesmo contrato de hoje**. Após salvar, a linha correspondente é atualizada (re-busca a config do profissional editado e recalcula a badge). Mora em: Handler/back (inalterado) + Front (atualização otimizada da linha).
- **R4** (mensagem genérica / LGPD): em caso de erro do back (422/500), exibir mensagem genérica ("Erro ao salvar comissão.") sem PII e sem expor detalhe de tenant. Mora em: Front (tratamento de erro, espelhando o 422 do `BusinessException`).

## 5. Modelo de dados

Sem alteração. Nenhuma tabela, coluna, índice ou migration. Contratos consumidos (já existentes):
- `vinculoService.listarProfissionaisPublico()` → `ProfissionalPublico[]` (`{ usuarioId, nomeCompleto, ... }`).
- `financeiroService.obterConfigComissao(usuarioId)` → `ConfigComissao { percentualConsulta, percentualProcedimento, percentualPadrao }`.
- `financeiroService.salvarConfigComissao({ profissionalUsuarioId, percentualConsulta?, percentualProcedimento? })`.

A tela já opera no escopo do estabelecimento ativo (multi-tenant garantido pelos serviços existentes). Sem PII nova exposta — apenas nome do profissional, que já é exibido hoje.

## 6. UX e fluxo

Layout alvo (lista, dentro do card de comissão já existente — `cfg-span`):

```
┌─ Comissões por profissional ───────────────── Padrão do sistema: 30% ─┐
│  [ícone] Dra. Ana Souza        Consulta 30% · Procedimento 35%   [✎]  │
│  [ícone] Dr. Bruno Lima        Consulta 30% · Procedimento 30%  PADRÃO [✎] │
│  [ícone] Dra. Carla Reis       Consulta 40% · Procedimento 45%   [✎]  │
└────────────────────────────────────────────────────────────────────────┘
```

- **Cabeçalho do card**: mantém título "Comissões por profissional" + ícone de percent. À direita do cabeçalho (ou logo abaixo), texto "Padrão do sistema: {percentualPadrao}%".
- **Linha de profissional** (`cfg-comm-row` equivalente): ícone `fa-user-doctor` + nome à esquerda; no centro/direita os dois percentuais formatados como "Consulta {x}% · Procedimento {y}%"; badge "PADRÃO" (reuso de `AppBadge` com `variant="muted"` e `label="PADRÃO"`) quando R1 for verdadeira; botão de lápis (`fa-pen`) à direita.
- **Modal de edição** (`AppModal`, `largura="sm"`, `titulo="Editar comissão — {nomeCompleto}"`):
  - Dois campos via `AppField` + `AppInputDecimal` (% consulta, % procedimento), `:min="0" :max="100"`, placeholder "Padrão: {percentualPadrao}%".
  - Slot `rodape`: `AppButton` secundário "Cancelar" (emite `fechar`) + `AppButton` "Salvar" (`:loading="salvando"`).
  - Mensagem de erro genérica dentro do corpo do modal quando o salvar falha.
- **Estados**:
  - **Carregando**: enquanto busca profissionais + suas configs, exibir indicação de carregamento ("Carregando comissões...").
  - **Vazio** (sem profissionais com vínculo): exibir hint textual ("Nenhum profissional cadastrado neste estabelecimento.") — sem linhas, sem modal.
  - **Erro ao carregar**: mensagem genérica; não quebrar a aba.
  - **Sucesso ao salvar**: `AppToast` "Comissão salva." (reuso do toast já presente no componente) + modal fecha + linha atualizada.
- **Responsivo**: em telas estreitas (< 600px) a linha pode quebrar os percentuais abaixo do nome; o lápis permanece acessível. Mobile-ready.
- **Tipografia**: toda declaração via tokens (`--text-*`, `--font-weight-*`) — nunca literal (CLAUDE.md §5). Reusar classes utilitárias de título do DS quando aplicável.

## 7. Critérios de aceite (testáveis)

- **CA1** (render da lista — caminho feliz): Dado um Dono com 3 profissionais vinculados, Quando abre a aba Configurações do Financeiro, Então vê 3 linhas, cada uma com nome do profissional e os dois percentuais no formato "Consulta {x}% · Procedimento {y}%", e o cabeçalho exibe "Padrão do sistema: 30%".
- **CA2** (badge PADRÃO presente): Dado um profissional cujos percentuais de consulta e procedimento são **ambos** iguais ao padrão do sistema (30% e 30%), Quando a lista renderiza, Então sua linha exibe o badge "PADRÃO".
- **CA3** (badge PADRÃO ausente): Dado um profissional com pelo menos um percentual diferente do padrão (ex.: consulta 30%, procedimento 35%), Quando a lista renderiza, Então sua linha **não** exibe o badge "PADRÃO".
- **CA4** (abrir modal): Dado a lista renderizada, Quando o Dono clica no lápis de uma linha, Então abre um modal titulado "Editar comissão — {nome}" com dois campos pré-preenchidos com os percentuais vigentes daquele profissional.
- **CA5** (editar e salvar): Dado o modal aberto com percentuais carregados, Quando o Dono altera o % de procedimento de 30% para 40% e clica em "Salvar", Então `salvarConfigComissao` é chamado com `{ profissionalUsuarioId, percentualConsulta: 30, percentualProcedimento: 40 }`, o modal fecha, um toast "Comissão salva." aparece, e a linha passa a exibir "Procedimento 40%" sem recarregar a página.
- **CA6** (badge recalcula após salvar): Dado um profissional que exibia "PADRÃO" (30%/30%), Quando o Dono salva procedimento = 40% via modal, Então a linha deixa de exibir o badge "PADRÃO". E, inversamente: Dado um profissional sem badge, Quando o Dono salva ambos os percentuais de volta para 30%, Então a linha passa a exibir "PADRÃO".
- **CA7** (cancelar modal): Dado o modal aberto com alterações não salvas, Quando o Dono clica em "Cancelar" (ou fecha o modal), Então o modal fecha, nenhuma chamada de salvar é feita, e a linha permanece com os valores originais.
- **CA8** (estado vazio): Dado um Dono cujo estabelecimento não tem profissionais vinculados, Quando abre a aba, Então vê a mensagem "Nenhum profissional cadastrado neste estabelecimento." e nenhuma linha de comissão.
- **CA9** (RBAC — não-dono não vê): Dado um usuário com `ehDono === false`, Quando abre a aba Configurações do Financeiro, Então a seção de comissão (cabeçalho, lista, badges e lápis) **não** é renderizada.
- **CA10** (carregamento): Dado o Dono abrindo a aba, Quando os dados ainda estão sendo buscados, Então um indicador de carregamento ("Carregando comissões...") é exibido no lugar da lista até os percentuais de todos os profissionais estarem disponíveis.
- **CA11** (erro genérico ao salvar — LGPD): Dado o back retornando 422/500 ao salvar, Quando o Dono confirma o modal, Então uma mensagem genérica ("Erro ao salvar comissão.") aparece, sem PII e sem detalhe de tenant, e o modal permanece aberto para nova tentativa.
- **CA12** (gate tipográfico): Dado o componente final, Quando `npm run check:typography -- --ci` roda, Então não há nenhuma declaração `font-size`/`font-weight` literal no CSS scoped do componente (toda tipografia via tokens — CLAUDE.md §5).
- **CA13** (sem regressão de contrato): Dado o redesign concluído, Quando inspeciona-se as chamadas de rede, Então apenas `listarProfissionaisPublico`, `obterConfigComissao` e `salvarConfigComissao` (contratos já existentes) são usados — nenhum endpoint novo, nenhum payload alterado.

## 8. Riscos e dependências

- **Estratégia de carregamento da lista (ponto a validar pelo dev)**: o requisito de mostrar os percentuais de **todos** os profissionais de uma vez exige a config de cada um. Hoje só existe `obterConfigComissao(usuarioId)` — **um por chamada**. Duas opções:
  - **(A) Solução mais simples, recomendada para começar — N chamadas em paralelo**: após `listarProfissionaisPublico()`, disparar `obterConfigComissao` para cada profissional via `Promise.all` (front), montando as linhas. Para os volumes típicos de uma clínica (poucos a algumas dezenas de profissionais por estabelecimento) isso é aceitável e **zero backend**. **Atenção**: respeitar a premissa Npgsql (memória do projeto) — o paralelismo aqui é no **front** (várias requisições HTTP independentes), não no back na mesma conexão; portanto seguro. Se o back tiver alguma limitação de concorrência por requisição, o dev pode serializar com pool controlado.
  - **(B) Endpoint de listagem agregada (só se A não escalar)**: um `GET` que devolve todos os profissionais do estabelecimento já com seus percentuais (`{ usuarioId, nomeCompleto, percentualConsulta, percentualProcedimento, percentualPadrao }[]`). Reduz N round-trips a 1. **Isto é dependência de backend** (nova query Dapper + endpoint) — se o dev julgar A insuficiente, **escalar de volta ao BA** para addendum que especifique o contrato do endpoint, ou tratar como tarefa de backend separada. **Não decidir arquitetura de backend sem registro.**
  - **Decisão para o dev**: começar por (A). Só migrar para (B) se medição mostrar lentidão real (muitos profissionais) — e nesse caso, voltar ao BA.
- **Reuso de componentes**: `AppModal` (modal), `AppBadge` (badge PADRÃO), `AppField` + `AppInputDecimal` (campos), `AppButton` (ações), `AppToast` (sucesso) já existem em `frontend/src/components/ui/`. Não criar componentes novos — montar a partir desses (Design System primeiro). Antes de escrever CSS scoped novo, conferir `ui/`.
- **Regressão visual**: o card de comissão é `cfg-span` (full-width) dentro do grid de Configurações; manter esse comportamento de layout para não desalinhar os cards "Em breve".
- **Dependência de briefing anterior**: este redesign vive dentro do que o briefing 2026-06-11_002 entregou (aba Config, gate `ehDono`). Não reabrir aquelas decisões.

## 9. Observações para execução

- **Não-negociável**: zero mudança de backend/contrato; RBAC `ehDono`; badge por R1 (ambos no padrão); tipografia por tokens; reuso de `AppModal`/`AppBadge`/`AppInputDecimal`/`AppField`/`AppButton`/`AppToast`; mensagens genéricas (LGPD).
- **Liberdade técnica**: composição da linha, exato wording do estado de carregamento/vazio, organização do CSS scoped (desde que via tokens), e a escolha A vs. B de carregamento (com a regra de escalar ao BA se for para B).
- **Arquivo único alvo**: `frontend/src/views/financeiro/tabs/FinanceiroConfigTab.vue`. Substituir o bloco de comissão (chips + form) pela lista + modal; os cards "Em breve" e a casca do card permanecem.
- **Formatação dos percentuais**: exibir como inteiro/decimal conforme o valor retornado, com sufixo "%". Seguir o que `AppInputDecimal` já produz para consistência. Sem locale especial além do padrão do app.

## 10. Atualização de documentação

Nenhuma atualização obrigatória em `Docs/`. A demanda **segue padrões já documentados** (reuso de `AppModal`/`AppBadge`, master-detail/lista já presentes no DESIGN.md, sem novo componente, sem nova regra cross-cutting, sem infra, sem LGPD nova). Caso o dev acabe escolhendo a opção (B) (endpoint agregado), aí sim haverá impacto de arquitetura — e nesse cenário o fluxo é voltar ao BA para addendum, que então atualizará `Docs/ARQUITETURA.md` (padrão de leitura agregada) como parte da entrega.
