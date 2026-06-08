# Passo de lateralidade do membro no mapa corporal (Exame Físico)

**ID**: 2026-06-08_001
**Status**: Aprovado por usuário em 2026-06-08
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (exame físico / mapa corporal) — nenhuma em permissionamento/orçamento/relatório/estoque.

## 1. Contexto e motivação

No exame físico estruturado da evolução, o profissional clica numa região do mapa corporal (`BodyMap.vue`) e o `RegionSelectorPopup.vue` abre para selecionar sub-regiões e marcar a lateralidade (D / E / Bilateral) **por sub-região**.

Para membros (superior/inferior), o fluxo atual é confuso: ao clicar num membro, o popup mostra as sub-regiões do lado direito (a base é fixada em `dirBase`), e cada sub-região exibe três botões D/E/Bilateral. Isso gera dois problemas operacionais:

1. **Repetição mental**: o profissional decide o lado uma vez (ex.: "examinei o joelho direito"), mas precisa reescolher D/E em cada sub-região marcada — para um membro inteiro examinado de um lado só, isso é fricção pura.
2. **"Bilateral" hoje duplica registros**: ao escolher "Bilateral" numa sub-região, o `confirmar()` expande em **dois** cards (um D + um E, via `idEsquerdoDe`), poluindo a lista "Regiões examinadas" com duas entradas quando o achado é compartilhado.

A decisão de produto é mover a escolha do lado para **antes** da seleção de sub-regiões, transformando-a no primeiro passo do popup, e tratar "Ambos" como **uma** entrada bilateral (não duas). Isso alinha o software ao raciocínio clínico real: "vou examinar o membro X, do lado Y; agora quais estruturas dentro dele".

Evidência: decisão de produto do usuário (orquestrador) em 2026-06-08, fechando ambiguidade de UX no fluxo de exame físico.

## 2. Persona-alvo

**Profissional de saúde** (médico/fisioterapeuta) durante o atendimento, na etapa de **prontuário → exame físico**, ao registrar achados por região anatômica no mapa corporal. Frequência: toda consulta que envolve exame físico de membros — alta recorrência em ortopedia, fisiatria, reumatologia.

## 3. Escopo

**Inclui** (100% frontend):
- Novo **passo inicial de lateralidade** dentro do `RegionSelectorPopup.vue`, exibido **somente** quando a região clicada é um membro (superior/inferior, anterior/posterior) — i.e., quando `membroRegioes` está presente.
- Três opções de lado: **Direito**, **Esquerdo**, **Ambos**.
- Após escolher o lado, o popup avança para a seleção de sub-regiões **sem** os botões D/E/Bilateral por sub-região (o lado já foi decidido para o membro inteiro).
- A base (lista de sub-regiões + `regiao_id` gerado) passa a depender do lado escolhido: **Direito → `dirBase`**, **Esquerdo → `esquBase`**, **Ambos → `dirBase` como id canônico** (ver R5).
- "Ambos" gera **uma única** entrada com `lateralidade: 'bilateral'` por sub-região marcada (não mais duas entradas D+E).
- Navegação: "Voltar" no passo de sub-regiões retorna ao passo de lado; "Cancelar" no passo de lado fecha o popup sem criar nada (ver R6).

**Não inclui**:
- Regiões **não-laterais** (cabeça, pescoço, tórax, abdome etc.): comportamento **inalterado** — abrem o popup direto na seleção de sub-regiões, sem passo de lado (ver R1). Estas seguem podendo ter os botões D/E/Bilateral por sub-região **somente se o catálogo marcar `lateralidade: true`** na própria sub-região (preservar o comportamento atual delas).
- Backend, migration, schema, multi-tenant, LGPD, audit. O campo `lateralidade` ("D"|"E"|"bilateral"|null) já existe em `RegiaoAnatomicaSelecionada` e persiste via `exameFisicoService`. **NÃO aciona `imedto-database`.**
- Mudança no `RegionExamCard.vue` (já renderiza o badge "Bilateral" — confirmado na linha que mapeia `'bilateral' → 'Bilateral'`).
- Mudança no `BodyMap.vue` (continua emitindo `regiaoClicada` igual; o agrupamento de membro por hover/clique permanece).

## 4. Regras de negócio

> Todas as regras moram no **Front** (`RegionSelectorPopup.vue` e, onde indicado, `SecaoExameFisico.vue`). Não há espelho de backend porque não há regra de domínio nova: o backend já aceita e persiste o campo `lateralidade` como está. A "validação" aqui é de fluxo de UI, não de negócio sensível.

- **R1 — Passo de lado é exclusivo de membros.** O passo "Direito / Esquerdo / Ambos" só aparece quando `membroRegioes != null` (região clicada casa o regex de membro superior/inferior anterior/posterior). Para qualquer outra região (`membroRegioes == null`), o popup abre **direto** na seleção de sub-regiões, idêntico ao comportamento atual. Mora em: Front (`RegionSelectorPopup.vue`, estado de passo).

- **R2 — Três opções de lado.** No passo de lado, exatamente três opções: **Direito**, **Esquerdo**, **Ambos**. Nenhuma vem pré-selecionada; o avanço para o passo de sub-regiões só ocorre após a escolha explícita de uma das três. Mora em: Front.

- **R3 — Botões D/E/Bilateral por sub-região somem no fluxo de membro.** No passo de sub-regiões de um membro, **não** se renderiza o seletor D/E/Bilateral por sub-região (nem para a opção "(geral)" nem para os filhos). A lateralidade de cada sub-região marcada é **derivada do lado escolhido no passo 1**. Mora em: Front. (Para regiões não-laterais, o seletor por sub-região continua exatamente como hoje — R1.)

- **R4 — Base depende do lado escolhido.** A lista de sub-regiões exibida e o `regiao_id` de cada card gerado dependem do lado:
  - **Direito** → opera sobre `membroRegioes.dirBase` (sub-regiões e `regiao_id` do lado direito). Lateralidade de cada entrada = `'D'`.
  - **Esquerdo** → opera sobre `membroRegioes.esquBase` (sub-regiões e `regiao_id` do lado esquerdo). Lateralidade de cada entrada = `'E'`.
  - **Ambos** → opera sobre `membroRegioes.dirBase` para listar sub-regiões (ver R5). Lateralidade de cada entrada = `'bilateral'`.

  Hoje `regiaoClicada` é fixado em `dirBase` no `onRegiaoClicada` de `SecaoExameFisico.vue` (linha 217). A regra exige que, conforme o lado, a base usada para `regiaoAtual`/`getFilhos`/`regiao_id` mude para `esquBase` quando o lado for Esquerdo. Mora em: Front (`RegionSelectorPopup.vue` decide a base ativa a partir do lado escolhido + `membroRegioes.dirBase`/`esquBase` que já chegam por prop).

- **R5 — "Ambos" gera UMA entrada bilateral por sub-região (decisão de produto, base canônica = direita).** Ao escolher "Ambos" e marcar N sub-regiões, o `confirmar()` emite **N** seleções (não 2N), cada uma com `lateralidade: 'bilateral'` e `regiaoId` = o id da sub-região da **base direita** (`dirBase`). **Não** se gera mais a entrada espelho do lado esquerdo via `idEsquerdoDe`. O `RegionExamCard` renderiza o badge "Bilateral" e o card representa o achado compartilhado para os dois lados. **Decisão explícita registrada (revisável):** o `regiao_id` canônico de uma entrada bilateral é o do lado direito (`dirBase`). Justificativa: precisa de um id estável e único por sub-região para o card e para a dedup de `regioesJaSelecionadas`; a base direita já é a base default histórica. Mora em: Front (`confirmar()` do `RegionSelectorPopup.vue`).

  > **Simplificação confirmada para o developer:** com "Ambos" gerando uma única entrada bilateral apontando para a base direita, a função `idEsquerdoDe` e o casamento direito→esquerdo (`esquChild`) **deixam de ser necessários no caminho "Ambos"**. O caminho "Esquerdo" passa a operar diretamente sobre `esquBase` (R4), sem precisar traduzir ids do lado direito. Remova o código órfão que SUAS mudanças tornarem morto (ex.: `idEsquerdoDe`, ramo `lat === 'bilateral'` que duplicava, ramo `lat === 'E'` que traduzia id), conforme princípio "Surgical Changes". Não remova código não relacionado.

- **R6 — Navegação Voltar/Cancelar (decisão de produto DEFAULT, revisável).** No fluxo de membro:
  - No **passo de sub-regiões**, o botão **"Voltar"** retorna ao **passo de lado** (permite trocar Direito → Esquerdo → Ambos sem fechar e reabrir o popup). A escolha de sub-regiões em andamento pode ser descartada ao voltar (estado limpo ao reentrar no passo de sub-regiões com lado diferente).
  - No **passo de lado**, o botão **"Cancelar"** fecha o popup **sem criar nada** (nenhuma região é adicionada).
  - Para regiões **não-laterais**, "Voltar" mantém o comportamento atual (subir um nível de breadcrumb hierárquico) — R1.

  Mora em: Front. Marcada como decisão DEFAULT recomendada — pode ser revista em iteração futura sem invalidar o resto do briefing.

## 5. Modelo de dados

**Nenhuma mudança de schema.** O campo `lateralidade: "D" | "E" | "bilateral" | null` já existe em `RegiaoAnatomicaSelecionada` (`SecaoExameFisico.vue`) e no payload de `exameFisicoService`. A persistência (POST `/api/evolucoes/{id}/exame-fisico`) permanece idêntica — apenas muda a **quantidade e o valor** das entradas geradas no caminho "Ambos" (1 entrada `bilateral` em vez de 2 entradas D+E).

Sem migration, sem coluna nova, sem índice, sem audit novo. Multi-tenant e LGPD inalterados (o vínculo evolução↔estabelecimento↔paciente já é garantido pelo fluxo existente de salvar a evolução; este briefing não toca esse caminho).

## 6. UX e fluxo

Componente: `RegionSelectorPopup.vue` (usa `AppModal`, `AppButton` do design system).

**Fluxo região NÃO-lateral (inalterado):**
```
Clique na região → Popup abre direto na lista de sub-regiões (com breadcrump hierárquico)
  → marca sub-regiões (+ D/E/Bilateral por sub-região SE o catálogo marcar lateralidade) → Confirmar
```

**Fluxo MEMBRO (novo):**
```
Clique no membro (sup/inf, ant/post)
  → PASSO 1 (lado): título = nome do membro sem "direito/esquerdo"
       [ Direito ]  [ Esquerdo ]  [ Ambos ]
       rodapé: [Cancelar]   (sem "Confirmar" — escolher um lado avança)
  → PASSO 2 (sub-regiões): lista derivada da base do lado escolhido
       - opção "(geral)" do membro + filhos, com checkbox
       - SEM botões D/E/Bilateral por sub-região
       - badge/indicador do lado escolhido visível no topo (ex.: "Lado: Direito" / "Esquerdo" / "Ambos (bilateral)")
       rodapé: [Voltar → passo 1]   [Cancelar]   [Confirmar (N)]
  → Confirmar → cards em "Regiões examinadas":
       - Direito  → cards com badge derivado de lateralidade 'D'
       - Esquerdo → cards com lateralidade 'E'
       - Ambos    → 1 card por sub-região com badge "Bilateral"
```

**Componente do design system para o seletor de lado:** preferir reuso de **`AppPillToggle`** (`frontend/src/components/ui/AppPillToggle.vue` — toggle segmentado para escolha binária/segmentada) para as três opções Direito/Esquerdo/Ambos, em vez de criar três `<button>` custom novos. Se `AppPillToggle` não acomodar bem três opções com avanço imediato, é aceitável usar `AppButton` em linha — mas justificar no PR. (Nota: `AppPillToggle` ainda não consta em `Docs/DESIGN.md`; ver seção 10.)

**Estados:**
- **Loading**: catálogo de regiões já carrega em `SecaoExameFisico` (`onMounted`); o popup não tem fetch próprio. Sem novo estado de loading.
- **Erro**: se o catálogo falhou ao carregar, o mapa nem renderiza hotspots clicáveis (comportamento atual preservado). Nenhum erro novo introduzido pelo passo de lado.
- **Vazio**: se o membro não tiver sub-regiões na base escolhida, manter a mensagem atual "Nenhuma sub-região disponível." no passo de sub-regiões.
- **Sucesso**: ao confirmar, popup fecha e estado interno é limpo (`navegacao`, `idsSelecionados`, lado escolhido, lateralidades).

**Mobile-ready**: o popup usa `AppModal` largura `sm`, já responsivo. As três opções de lado devem caber em telas estreitas (empilhar ou usar pill compacto).

**Atalho de teclado**: nenhum novo exigido. "Esc" do `AppModal` (fechar) deve continuar funcionando e equivaler a Cancelar.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — lado Direito):** Dado um membro clicado no mapa, Quando o profissional escolhe "Direito" e marca 2 sub-regiões e confirma, Então são criados exatamente 2 cards em "Regiões examinadas", cada um com `lateralidade: 'D'`, e os `regiao_id` correspondem às sub-regiões da base direita (`dirBase`).

- **CA2 (lado Esquerdo opera sobre esquBase):** Dado um membro clicado, Quando o profissional escolhe "Esquerdo", Então o passo de sub-regiões lista as sub-regiões de `esquBase` (não as de `dirBase`); e ao marcar 1 sub-região e confirmar, é criado 1 card com `lateralidade: 'E'` cujo `regiao_id` pertence à base esquerda.

- **CA3 ("Ambos" = 1 entrada bilateral, base canônica direita):** Dado um membro clicado, Quando o profissional escolhe "Ambos", marca 1 sub-região e confirma, Então é criado exatamente **1** card (não 2) com `lateralidade: 'bilateral'`, badge "Bilateral" visível, e `regiao_id` = id da sub-região da base direita (`dirBase`). Verificação negativa: nenhuma segunda entrada com `lateralidade: 'E'` é gerada.

- **CA4 (sem botões D/E/Bilateral por sub-região no fluxo de membro):** Dado o passo de sub-regiões de um membro (qualquer lado), Quando a lista é renderizada, Então **nenhum** botão D/E/Bilateral por sub-região aparece (nem na opção "(geral)" nem nos filhos).

- **CA5 (região NÃO-lateral inalterada):** Dado o clique numa região não-lateral (ex.: tórax, abdome, cabeça, pescoço), Quando o popup abre, Então **não** há passo de lado — abre direto na seleção de sub-regiões com o breadcrumb hierárquico, idêntico ao comportamento atual; e o seletor D/E/Bilateral por sub-região continua aparecendo apenas onde o catálogo marca `lateralidade: true`.

- **CA6 (navegação Voltar → passo de lado):** Dado que o profissional escolheu "Direito" e está no passo de sub-regiões de um membro, Quando clica em "Voltar", Então retorna ao passo de lado (vê as 3 opções Direito/Esquerdo/Ambos) e pode escolher "Esquerdo"; ao avançar, a lista passa a refletir a base esquerda (CA2).

- **CA7 (Cancelar no passo de lado não cria nada):** Dado o passo de lado de um membro recém-aberto, Quando o profissional clica em "Cancelar" (ou Esc), Então o popup fecha e **nenhuma** região é adicionada a "Regiões examinadas".

- **CA8 (estado limpo ao reconfirmar):** Dado que o profissional confirmou seleções de um membro e o popup fechou, Quando reabre o mesmo membro, Então o passo de lado reaparece do zero (sem lado pré-selecionado e sem sub-regiões marcadas remanescentes).

- **CA9 (dedup preservada):** Dado que uma sub-região de um membro já foi examinada (já consta em `regioesJaSelecionadas`), Quando o profissional reabre o membro no mesmo lado, Então essa sub-região aparece como já selecionada (não recriável em duplicata), preservando o comportamento atual de `jaFoiSelecionada` / `regioesJaSelecionadas`.

- **CA10 (sem regressão no payload de persistência):** Dado um exame físico com regiões de membro confirmadas, Quando a evolução é salva, Então o payload enviado a `exameFisicoService` contém para cada entrada `{ regiao_id, caminho, lateralidade, texto_exame, achados, observacoes, timestamp }` no mesmo formato atual — apenas com a contagem/valor de `lateralidade` ajustados conforme R5.

- **CA11 (regressão automatizada):** Dado a suíte de testes do exame físico (`RegionExamCard.test.ts`, `BodyMap.test.ts`, `SecaoExameFisico.test.ts`), Quando rodada após a mudança, Então passa; testes que assumiam a expansão "bilateral → 2 entradas D+E" são atualizados para refletir "Ambos → 1 entrada bilateral" (a expectativa antiga é parte da spec antiga e deve mudar com a feature).

> **Nota de CAs ausentes (justificada):** este briefing NÃO inclui CA de multi-tenant, RBAC, LGPD/audit ou performance de lista grande porque a demanda é puramente de UX no front, sem novo endpoint, sem PII nova exposta, sem permissão nova, sem query nova. O acesso ao prontuário já é auditado no fluxo existente de abrir a evolução — este passo não cria novo ponto de exposição. QA: confirmar que nenhum log novo com PII foi introduzido e que nenhuma nova request é disparada pelo passo de lado (ele é puramente client-side sobre catálogo já carregado).

## 8. Riscos e dependências

- **Risco de regressão em testes existentes**: testes que esperavam 2 entradas para "bilateral" vão quebrar — isso é esperado e parte do escopo (CA11). Não "consertar" forçando o comportamento antigo.
- **Risco de id canônico**: usar `dirBase` como id canônico de "Ambos" assume que toda sub-região com lado direito tem id estável. Se alguma sub-região existir só no lado esquerdo (assimetria de catálogo), o card bilateral ainda usaria o id direito — improvável no catálogo atual (membros são simétricos), mas o developer deve usar o id da base efetivamente escolhida para listar ("Ambos" lista por `dirBase`, então o id existe).
- **Dependência de prop**: `membroRegioes` (`dirBase`/`esquBase`) já chega ao popup via `SecaoExameFisico.vue`. O ajuste de R4 (base muda por lado) deve preferencialmente ficar **dentro do popup** (que já recebe ambas as bases), evitando mexer no `onRegiaoClicada` do pai além do necessário. Se for preciso ajustar `regiaoClicada`/base no pai, manter cirúrgico.
- **Área regressiva**: prontuário → exame físico. Não toca permissionamento, orçamento, relatório, estoque.

## 9. Observações para execução

**Não-negociável:**
- 100% frontend. **NÃO** criar migration, endpoint, DTO de backend, nem acionar `imedto-database`.
- "Ambos" gera **uma** entrada `bilateral` por sub-região (R5/CA3). Não manter a expansão D+E.
- Passo de lado **só** para membros (R1/CA5). Regiões não-laterais intocadas.
- Sem botões D/E/Bilateral por sub-região no fluxo de membro (R3/CA4).

**Liberdade técnica do developer:**
- Como modelar o estado de "passo atual" (lado vs sub-regiões) dentro do `RegionSelectorPopup.vue` (ex.: `ref<'lado' | 'subregioes'>`).
- Se decide a base ativa via computed a partir do lado escolhido + props `dirBase`/`esquBase`, ou ajusta no pai. Preferência: resolver dentro do popup (já recebe ambas as bases).
- Usar `AppPillToggle` vs `AppButton` para as 3 opções de lado (preferência por `AppPillToggle`; justificar se não usar).
- Remover código órfão que a mudança tornar morto (`idEsquerdoDe`, ramos de casamento direito→esquerdo no `confirmar()`), conforme "Surgical Changes". Não remover código não relacionado.

**Para o QA:**
- Validar CA1–CA11. Como `chrome-devtools` está indisponível no sandbox, validar por análise de código + suíte automatizada; validação visual final fica para o usuário em prod.
- Confirmar ausência de PII em log e ausência de nova request disparada pelo passo de lado.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar `AppPillToggle` à seção de componentes do design system (toggle segmentado para escolha binária/segmentada). O componente já existe em `frontend/src/components/ui/AppPillToggle.vue` mas ainda não está documentado; esta entrega o reusa para o seletor de lado, então é a oportunidade de registrar a dívida de doc. Mudança incremental e cirúrgica — apenas acrescentar o item, sem reescrever a seção. (Responsável: feita pelo BA no mesmo ciclo, ou pelo developer ao introduzir o uso — registrar no PR.)
- Demais docs (`ARQUITETURA.md`, `INFRA.md`, `COMANDOS.md`, `LGPD.md`): **nenhuma atualização** — a demanda é UX no front, segue padrões existentes, sem mudança de arquitetura/infra/regra cross-cutting/LGPD.
