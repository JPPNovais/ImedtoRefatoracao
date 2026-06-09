# Exame físico — redesign visual: layout lateral + coloração por vista

**ID**: 2026-06-09_007
**Status**: Aprovado por usuário em 2026-06-09
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (seção Exame físico — apresentação apenas)
**Escopo de pipeline**: frontend-only — **NÃO aciona `imedto-database`, NÃO toca backend** (handler, domínio, service, contrato, persistência intactos).

## 1. Contexto e motivação

O Exame físico hoje exibe o mapa corporal centralizado e, **abaixo** dele, a lista de "Regiões examinadas" empilhada verticalmente. Em telas largas isso desperdiça a lateral e força rolagem para correlacionar o que foi clicado no mapa com o card correspondente. O PO trouxe um mockup do Claude Design (`/tmp/exame_bundle/imedto/project/`) que resolve isso com **layout lateral** (mapa à esquerda, regiões à direita) e **coloração de seleção por vista** (anterior=azul, posterior=violeta, circunferencial=âmbar), tornando o mapa autoexplicativo via legenda de cores.

A demanda é **design-only**: melhorar a apresentação sem mudar fluxo, backend ou funcionamento. Os ganhos das waves anteriores — B1 (vista no popup, briefing 2026-06-08_005), B2 (fusão de polígonos de tronco + coloração por vista no mapa, 2026-06-08_006) e 004 (card agregado por confirmação, 2026-06-08_004) — **não podem regredir**.

## 2. Persona-alvo

Profissional de saúde (médico/enfermeiro) durante a evolução clínica, na seção Exame físico do prontuário. Uso recorrente a cada atendimento. Desktop é o ambiente primário; mobile precisa continuar usável (1 coluna).

## 3. Escopo

**Inclui** (apresentação apenas):
- Layout lateral da subseção do mapa: mapa à esquerda, coluna "Regiões examinadas" à direita, em grid responsivo. Em telas estreitas, colapsa para 1 coluna (mapa em cima, cards embaixo — comportamento atual).
- Coloração dos hotspots SVG acesos **por vista**: anterior=azul, posterior=violeta, circunferencial=âmbar (âmbar em ambos os polígonos anterior+posterior). Substitui a cor `primary` uniforme atual.
- Legenda de cores (Anterior / Posterior / Circunferencial) abaixo do mapa.
- Novos tokens de cor de vista em `main.css` (`--vista-anterior`, `--vista-posterior`, `--vista-circ`).
- Polimento visual do `RegionExamCard`: header com ícone, contador "Regiões examinadas (N)", badges coloridas por vista (azul/violeta/âmbar) + badge de lado, estado vazio estilizado.
- Polimento visual da modal (`RegionSelectorPopup`): segmented control de plano/vista colorido por vista, chips. **Estética apenas** — sem alterar passos, ordem ou opções.
- Redução proporcional do tamanho do mapa para dar espaço à coluna de cards (ajuste de largura/`max-width`, sem mexer no `viewBox` nem nos paths).
- Tipografia 100% via tokens CSS (`var(--text-*)`, `var(--font-weight-*)`), conforme CLAUDE.md §5.

**Não inclui** (cravado fora de escopo — não regredir):
- Estrutura SVG / paths dos polígonos do `BodyMap` (`bodyMapPaths.ts`). Mantém os paths atuais. **NÃO trocar SVG por PNG** como o mockup faz. Só muda a cor de preenchimento (`fill`/`stroke`) dos hotspots acesos.
- Campos do `RegionExamCard` (Exame / Achados / Observações). O mockup usa "textarea por sub-item" — isso muda o funcionamento e o contrato. **Manter os 3 campos atuais** (texto_exame, achados, observacoes).
- Fluxo da modal: passo de lado (D/E/Ambos para membro, briefing 2026-06-08_001), passo de vista/plano (B1), sub-regiões vindas do catálogo do backend. **NÃO** usar a lista `REGIONS` hardcoded do mockup.
- Backend, handler, domínio, `exameFisicoService`, payload de persistência, schema/migration.
- Lógica de fusão de tronco e bilateral de membro (B2) — só muda a cor aplicada, não quem acende.
- Sinais vitais, antropometria, ectoscopia, observações gerais — estética não faz parte desta demanda (não mexer).

## 4. Regras de negócio

> Nenhuma regra de negócio nova. Todas as regras abaixo são **de apresentação** (mora no Front; não há espelho no back porque não há comportamento de domínio novo). A persistência segue idêntica.

- **R1 (cor por vista)**: ao acender um hotspot, a cor é definida pela `vista` da região examinada — `anterior → azul (--vista-anterior)`, `posterior → violeta (--vista-posterior)`, `circunferencial → âmbar (--vista-circ)`. Mora em: Front (`BodyMap.vue` CSS scoped + classes condicionais). Validada em: front (visual + teste de componente).
- **R2 (circunferencial pinta ambos)**: uma região de vista `circunferencial` pinta **âmbar** tanto no polígono anterior quanto no posterior. O conjunto de polígonos que acende **não muda** (B2 já resolve via `RAMOS_CIRCUNFERENCIAL`); muda apenas a cor aplicada a eles. Mora em: Front.
- **R3 (membro bilateral mantém a cor da sua vista)**: um membro examinado bilateralmente acende os dois lados (D+E, B2 existente) com a cor da **vista** daquela entrada (azul se anterior, violeta se posterior, âmbar se circunferencial) — não há cor especial de "bilateral". Mora em: Front.
- **R4 (precedência quando um polígono é alvo de mais de uma vista)**: se o mesmo hotspot for aceso por entradas de vistas diferentes (ex.: a mesma região examinada uma vez anterior e outra circunferencial), prevalece a cor **circunferencial (âmbar)** > **posterior (violeta)** > **anterior (azul)**. Justificativa: circunferencial é o estado "mais completo" do exame daquela região; a ordem evita flicker indeterminado. Mora em: Front.
- **R5 (badge do card por vista)**: a badge de vista do `RegionExamCard` usa a mesma cor da vista (azul/violeta/âmbar). A badge de lado (Direito/Esquerdo/Bilateral/Vários lados) mantém estilo neutro. Quando `vista` é nula/ausente, não renderiza badge de vista (comportamento atual preservado). Mora em: Front.
- **R6 (legenda)**: a legenda abaixo do mapa lista as 3 vistas com seus pontos de cor. É estática (sempre visível), informativa. Mora em: Front.

## 5. Modelo de dados

**Nenhuma mudança de schema. Nenhuma migration. Sem backend.**

O campo que viabiliza a coloração por vista **já existe**: `RegiaoAnatomicaSelecionada.vista` (`'anterior' | 'posterior' | 'circunferencial' | null`) em `SecaoExameFisico.vue` e `RegionExamCard.vue` — derivado do nó do catálogo, **não vai no payload de persistência** (já documentado no código). O card já recebe e exibe `vista`. Logo, a vista de cada entrada já está disponível no front; o trabalho é **propagá-la até o `BodyMap`**.

Sem PII nova, sem audit novo, sem endpoint novo.

## 6. UX e fluxo

### 6.1 Layout lateral (subseção Mapa corporal)

Hoje em `SecaoExameFisico.vue` há duas `.subsecao` separadas: "Mapa corporal" e "Regiões examinadas (N)", empilhadas. O redesign **funde a apresentação** das duas numa grade lateral:

```
┌─ Mapa corporal ─ clique em uma região para examinar ──────────────┐
│ ┌───────────────────────────┐   ┌───────────────────────────────┐ │
│ │  [ SVG anterior+posterior ]│   │ Regiões examinadas      (N)   │ │
│ │       (mapa reduzido)      │   │ ┌───────────────────────────┐ │ │
│ │                            │   │ │ [ic] Tórax    [Anterior•] │ │ │
│ │  ● Anterior ● Posterior    │   │ │ Exame / Achados / Obs     │ │ │
│ │  ● Circunferencial         │   │ └───────────────────────────┘ │ │
│ │  (legenda)                 │   │ ... cards ...                  │ │
│ └───────────────────────────┘   │  ou  estado vazio estilizado   │ │
│                                  └───────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────┘
```

- Grade: mapa à esquerda (~1.55fr), coluna de regiões à direita (~minmax(330px, 1fr)), gap ~26px, `align-items: start`. Referência do mockup: `.map-grid`.
- Breakpoint: abaixo de ~940px colapsa para 1 coluna (mapa em cima, cards embaixo). Mantém alinhamento com o breakpoint existente de 900px da seção (dev pode unificar em um só — preferir 900px para consistência com `.grade-sv`/`.grade-antro`).
- Mapa reduzido: ajustar `max-width` do container do SVG (hoje `max-w-2xl`) para caber na coluna esquerda sem distorcer. **NÃO** mudar `viewBox` nem paths.
- A subseção "Observações gerais do exame físico" permanece **abaixo** da grade, largura total, como hoje.

### 6.2 Coluna "Regiões examinadas"

- Cabeçalho: rótulo "Regiões examinadas" + contador `(N)` em pill (badge), só visível quando `N > 0`.
- Estado vazio (quando nenhuma região examinada): card tracejado centralizado com ícone, título "Nenhuma região examinada" e auxílio "Clique em uma região do mapa corporal para registrar o exame." Hoje a subseção de regiões simplesmente não aparece (`v-if regioes.length > 0`); o redesign mostra o **estado vazio** estilizado na coluna direita.
- Cards: `RegionExamCard` empilhados, com polimento de header (ícone + título + badges).

### 6.3 Estados

- **Loading do catálogo**: mantém o hint atual ("carregando regiões...") no título da subseção.
- **Erro do catálogo**: mantém hint de erro atual ("Não foi possível carregar as regiões anatômicas. Recarregue a página.").
- **Vazio**: estado vazio estilizado na coluna direita (6.2).
- **Sucesso**: cards renderizados; hotspots acesos coloridos por vista; legenda visível.
- **readOnly**: a seção do mapa não renderiza no modo readOnly hoje (`v-if="!readOnly"`). Mantém esse comportamento.

### 6.4 Cor por vista — tokens

Definir em `main.css` `:root` (e dark mode se aplicável), espelhando o mockup:
- `--vista-anterior: 217 91% 60%;` (azul — canais HSL para `hsl(var(--vista-anterior))`)
- `--vista-posterior: 262 70% 56%;` (violeta)
- `--vista-circ: 35 92% 50%;` (âmbar)

Usar via `hsl(var(--vista-*) / <alpha>)` no `fill`/`stroke` dos hotspots, nos pontos da legenda e nas badges do card. O dev decide os alphas finais (referência do mockup: fill ~0.34 anterior/posterior, ~0.42 circ; badge bg ~0.13–0.16). Manter o token canônico — não literais HSL espalhados.

### 6.5 Ponto técnico — propagar a vista até o BodyMap (não-negociável)

Hoje `BodyMap` recebe `regioesExaminadas: string[]` (ids de nível-1) e aplica `.region-selected` uniforme (cor `primary`). Para colorir por vista, o componente precisa saber a **vista de cada id aceso**.

Regra para o dev (apresentação pura, **sem tocar persistência**):
- Estender a saída de `regioesExaminadasMapa` (computed em `SecaoExameFisico.vue`) para carregar a vista de cada id, p.ex. um mapa `Record<string, 'anterior'|'posterior'|'circunferencial'>` (id → vista) **em paralelo** ao array atual, OU trocar o contrato da prop por `Array<{ id: string; vista: ... }>`. Preferir adicionar uma **prop nova opcional** (`vistasPorId?: Record<string, Vista>`) para não quebrar os testes/usos atuais de `regioesExaminadas`.
- A vista de cada id sai da entrada `RegiaoAnatomicaSelecionada.vista` que a gerou. Para os ids derivados/expandidos (espelhamento bilateral de membro e ramos circunferenciais via `RAMOS_CIRCUNFERENCIAL`), atribuir a vista da entrada que originou a expansão (R2/R3). Aplicar a precedência R4 ao montar o mapa (circ > posterior > anterior) quando um id receber mais de uma vista.
- No `BodyMap`, a classe/cor do hotspot aceso passa a depender da vista resolvida desse id. Os pseudo-hotspots de tronco (`troncoHotspots`) e os hotspots de catálogo (`regioesComPath`) usam a mesma regra de cor.
- **Não** mudar `regioesJaSelecionadas`, `onConfirmarRegioes`, nem o payload. Só leitura/apresentação.

### 6.6 Modal (polimento estético)

Segmented control de plano/vista com a cor da vista selecionada (azul/violeta/âmbar) e chips de resumo. **Sem** alterar: ordem dos passos, opção de lado para membro, sub-regiões do catálogo, regra de confirmação. Se houver risco de tocar lógica, o dev deve preferir **não** mexer na modal e entregar só o item 6.1–6.5 (a modal é o item de menor prioridade e explicitamente "opcional" pelo PO).

## 7. Critérios de aceite (testáveis)

- **CA1 (layout lateral — desktop)**: Dado a seção Exame físico aberta em viewport ≥ 940px com ao menos 1 região examinada, Quando renderiza, Então o mapa corporal aparece à esquerda e a coluna "Regiões examinadas" à direita na mesma linha (grade de 2 colunas), e a subseção "Observações gerais" permanece abaixo em largura total.
- **CA2 (layout responsivo — mobile)**: Dado viewport < 940px (usar o breakpoint unificado escolhido), Quando renderiza, Então a grade colapsa para 1 coluna (mapa acima, cards/estado-vazio abaixo) sem corte horizontal nem overflow.
- **CA3 (cor anterior = azul)**: Dado uma região examinada com `vista = 'anterior'`, Quando o mapa renderiza, Então o(s) hotspot(s) correspondente(s) acende(m) em azul (`--vista-anterior`), e não na cor `primary` antiga.
- **CA4 (cor posterior = violeta)**: Dado uma região com `vista = 'posterior'`, Quando renderiza, Então o hotspot acende em violeta (`--vista-posterior`).
- **CA5 (circunferencial = âmbar em ambos)**: Dado uma região com `vista = 'circunferencial'`, Quando renderiza, Então os polígonos anterior **e** posterior daquela região acendem em âmbar (`--vista-circ`), e o conjunto de polígonos que acende é idêntico ao comportamento B2 atual (nenhum polígono a mais ou a menos).
- **CA6 (membro bilateral mantém cor da vista)**: Dado um membro examinado bilateralmente com `vista = 'anterior'`, Quando renderiza, Então os dois lados (direito e esquerdo) acendem em azul; trocando a vista para posterior, ambos acendem em violeta.
- **CA7 (precedência de cor)**: Dado o mesmo hotspot alvo de duas entradas com vistas diferentes (uma anterior e uma circunferencial), Quando renderiza, Então o hotspot acende em âmbar (circ vence anterior, conforme R4).
- **CA8 (legenda)**: Dado o mapa renderizado, Quando o usuário olha abaixo do SVG, Então vê a legenda com três itens — Anterior (azul), Posterior (violeta), Circunferencial (âmbar) — com o ponto de cor de cada um.
- **CA9 (badges do card por vista)**: Dado um card de região com `vista = 'posterior'` e `lateralidade = 'bilateral'`, Quando renderiza, Então exibe a badge "Posterior" na cor violeta e a badge "Bilateral" em estilo neutro; com `vista` nula, nenhuma badge de vista aparece.
- **CA10 (contador)**: Dado N regiões examinadas (N ≥ 1), Quando renderiza, Então o cabeçalho da coluna mostra "Regiões examinadas" com o contador `(N)`; com N = 0 o contador não aparece.
- **CA11 (estado vazio)**: Dado nenhuma região examinada, Quando a coluna direita renderiza, Então exibe o estado vazio estilizado (ícone + "Nenhuma região examinada" + auxílio), e não um espaço em branco.
- **CA12 (não-regressão de funcionamento — campos do card)**: Dado um card aberto, Quando o usuário edita Exame, Achados e Observações, Então os três campos persistem exatamente como hoje (via `atualizar`/`atualizarRegiao`), sem nenhum campo novo "textarea por sub-item" do mockup.
- **CA13 (não-regressão — fluxo da modal)**: Dado o clique em uma região (membro, tronco ou cabeça/pescoço), Quando a modal abre, Então os passos (lado quando membro → vista/plano → sub-regiões do catálogo) e as opções são idênticos ao comportamento atual; nenhuma sub-região vem da lista hardcoded do mockup.
- **CA14 (não-regressão — persistência/contrato)**: Dado uma confirmação de regiões, Quando o exame é salvo, Então o payload enviado ao backend é byte-a-byte equivalente ao atual (sem `vista` no payload, sem campo novo); nenhuma chamada ou request muda.
- **CA15 (não-regressão B2 — fusão de tronco)**: Dado um exame de tronco (anterior/posterior/circunferencial), Quando renderiza no mapa, Então a fusão de polígonos e o "OU das partes" (PARTE_PARA_TRONCO) continuam acendendo os mesmos polígonos de antes — só a cor muda.
- **CA16 (polígonos inalterados)**: Dado o SVG do mapa, Quando comparado ao atual, Então `bodyMapPaths.ts` não muda (mesmos `d`, mesmo `viewBox` 0 25 1400 970), e nenhum PNG substitui os paths.
- **CA17 (tipografia por tokens)**: Dado o CSS novo/alterado das views e componentes tocados, Quando inspecionado, Então não há nenhum `font-size`/`font-weight` literal — todas as declarações usam `var(--text-*)`/`var(--font-weight-*)` (CLAUDE.md §5), e as cores de vista usam os tokens `--vista-*` (sem HSL literal espalhado).
- **CA18 (multi-tenant / RBAC / LGPD — sem impacto)**: Dado que a demanda é apresentação pura, Quando avaliada, Então não há novo endpoint, query, claim, mensagem de erro nem log com PII; o catálogo de regiões e a persistência seguem os mesmos controles já existentes. (CA de confirmação de não-impacto — nada novo a validar no back.)

## 8. Riscos e dependências

- **Risco — quebrar B2/004**: a coloração por vista mexe na mesma área de `BodyMap`/`regioesExaminadasMapa` que B2 (2026-06-08_006). Mitigação: adicionar prop nova (`vistasPorId`) em vez de trocar `regioesExaminadas`; rodar `BodyMap.test.ts` e `SecaoExameFisico.test.ts` antes/depois (CA15).
- **Risco — testes existentes**: `BodyMap.test.ts`, `RegionExamCard.test.ts`, `SecaoExameFisico.test.ts` podem assertar a cor `primary` ou a estrutura empilhada. Dev deve atualizar os testes que validavam o visual antigo, mantendo as asserções de comportamento (clique, acende, persiste).
- **Risco — modal tocar lógica**: o polimento da modal é o ponto mais arriscado (lógica de passos densa). Mitigação: tratar como item opcional de menor prioridade; se conflitar com a lógica, entregar sem o polimento da modal (PO classificou como opcional).
- **Dependência**: nenhuma externa. Não depende de backend nem de DB.

## 9. Observações para execução

- **Não-negociável**: paths do SVG intactos (CA16); 3 campos do card intactos (CA12); fluxo da modal e catálogo intactos (CA13); payload intacto (CA14); tipografia e cores por token (CA17).
- **Liberdade técnica**: forma exata de propagar a vista (prop `vistasPorId` vs array de objetos — preferir prop nova opcional), alphas finais das cores, ícones do header do card, valores exatos do grid (frações, gap), e se funde as duas `.subsecao` numa só ou usa um wrapper de grade. Preferir **reuso** dos componentes existentes (`RegionExamCard`, `BodyMap`, `AppButton`) — só polir, não recriar.
- **Reuso > duplicação**: a badge de vista já existe no `RegionExamCard` (campo `vista` já consumido) — estender o estilo, não criar componente novo. O estado vazio pode reusar padrão de empty state do design system se houver equivalente; senão, CSS scoped simples.
- **1 push por sessão**: agrupar tudo num só ciclo de QA.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — atualizar na mesma entrega (responsabilidade do BA; o dev/QA confirmam):
  - Adicionar os tokens de cor de vista (`--vista-anterior` azul, `--vista-posterior` violeta, `--vista-circ` âmbar) à seção de tokens/paleta, com a semântica "cor de vista anatômica do Exame físico".
  - Documentar o **layout lateral** do `BodyMap` (mapa à esquerda, regiões examinadas à direita, colapso para 1 coluna no breakpoint) como o padrão da seção Exame físico.
  - Registrar a regra de **coloração de hotspot por vista** e a precedência R4 (circ > posterior > anterior) como regra cross-cutting do mapa corporal.
- Demais docs: **nenhum**. Sem mudança de arquitetura (`ARQUITETURA.md`), infra (`INFRA.md`), comandos (`COMANDOS.md`) ou LGPD (`LGPD.md`) — a demanda é apresentação frontend-only sem novo dado pessoal, endpoint, audit ou padrão de service/store.
