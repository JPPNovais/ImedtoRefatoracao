# Paridade Regiões Anatômicas ↔ Exame Físico (cache, casar por código, nível 3)

**ID**: 2026-06-25_001
**Status**: Aguardando OK explícito do usuário (decisões de produto pré-confirmadas via orquestrador — ver §0)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: catálogo de regiões anatômicas (admin global) / exame físico do prontuário / mapa corporal (BodyMap)

---

## 0. Nota de governança (ler antes de executar)

As 3 decisões de produto deste briefing (fronteira da paridade, UX do seletor de pai nível 3, semântica de "repercutir na hora") foram **destravadas pelo orquestrador com o usuário** e relatadas ao BA. O ambiente da pipeline sinaliza que relato de orquestrador não substitui confirmação direta do usuário. Portanto:

- O conteúdo técnico (causas-raiz, arquivos, comportamento atual) é **factual e verificado no código** — pode ser tratado como verdade.
- As 3 decisões de produto estão registradas **na recomendação do BA**, que coincide com o que o orquestrador relatou ter sido aceito.
- O **dev só inicia após OK explícito do usuário** no fluxo da pipeline. Se o usuário ajustar qualquer das 3 decisões antes do início, o BA atualiza via addendum (briefing imutável). Não há ação de risco aqui: as 3 decisões batem com a recomendação técnica conservadora.

---

## 1. Contexto e motivação

O dono do produto cravou um **requisito-mãe**: "tudo que eu altero na região anatômica tem que repercutir no exame físico, pois essa configuração corresponde a ela" — paridade entre o catálogo global de regiões (admin) e o que o profissional vê no exame físico do prontuário.

A investigação técnica confirmou **3 problemas distintos** que quebram essa paridade:

**Problema 1 — Edito no admin e não reflete no exame físico (causa: cache de 30 min nunca invalidado).**
O exame físico consome `GET /api/catalogo/regioes-anatomicas` (via `frontend/src/services/exameFisicoService.ts` → `listarRegioes`). O handler `backend/src/Services/Imedto.Backend.Application/Catalogo/Queries/ListarRegioesCatalogoQueryHandlers.cs` usa `IMemoryCache.GetOrCreateAsync` com TTL de 30 min (chave `catalogo:regioes:vista={...}:ativas={...}`). **Nenhum** dos 5 handlers admin de mutação (`CriarRegiaoAdminCommandHandler`, `AtualizarRegiaoAdminCommandHandler`, `InativarRegiaoAdminCommandHandler`, `ReativarRegiaoAdminCommandHandler`, `ExcluirRegiaoAdminCommandHandler`, em `backend/src/Services/Imedto.Backend.Application/Admin/Regioes/`) invalida esse cache. Resultado: qualquer edição (foi o caso do dono, que editou template/texto) leva **até 30 min** (ou restart do backend) para aparecer no exame físico. Esta é a causa direta do sintoma principal de "não reflete".

> Há precedente no próprio projeto: o `IConfigGlobalReader` (ver `Docs/ARQUITETURA.md §Configurações Globais`) já usa o padrão "lê via `IMemoryCache` + **invalida cache após mutação**". O catálogo de regiões é a exceção que esqueceu de invalidar. O fix alinha o catálogo de regiões a um padrão que já existe.

**Estratégia definitiva do P1 (decisão de produto fechada) — manter e REFORÇAR o cache, em 3 camadas:**
1. **Manter o cache e aumentar o TTL** — hoje 30 min; passa a um valor bem maior (ex.: 6h–24h, o dev define), porque o catálogo raramente muda. Performance máxima na leitura do exame físico.
2. **Invalidação automática** do cache `catalogo:regioes:*` em **toda** mutação admin (Criar/Atualizar/Inativar/Reativar/Excluir) — rede de segurança principal da paridade, sem o admin precisar lembrar de nada. Espelha o `IConfigGlobalReader`.
3. **Invalidação manual** via um endpoint admin dedicado (botão "Forçar atualização do exame físico" no configurador de regiões) — reforço por cima da automática, para o admin garantir o reflexo na hora se desconfiar de descompasso.

Atenção factual: a tela admin já tem um botão "Atualizar", mas ele só recarrega a árvore do **próprio admin** (`GET /api/admin/catalogos/regioes-anatomicas`, **sem cache**) — **não** toca o cache do catálogo do prontuário (`GET /api/catalogo/regioes-anatomicas`). O botão/endpoint novo precisa explicitamente invalidar `catalogo:regioes:*`.

**Problema 2 — Não consigo cadastrar o terceiro nível; trava no nível 2.**
No formulário `frontend/src/modules/admin/views/RegioesGlobaisFormView.vue`, o nível é derivado do pai (`nivel = pai.nivel + 1`, watcher de `paiCodigo`). O único seletor prático de pai é o BodyMap embutido, e `regioesParaMapa` filtra `no.nivel === 1` → só dá pra escolher pai nível 1 → só cria nível 2. Para criar nível 3 seria preciso apontar uma sub-região nível 2 como pai, mas não existe seletor que liste nível 2 (o campo "Código do pai" é texto livre, frágil). O **backend já aceita nível 3** (valida `nivel == pai.nivel+1` e `vista == pai.vista`, sem limite de profundidade) — o gargalo é só de UX no front.

**Problema 3 — A região principal não aparece toda no exame físico (casa por NOME hardcoded).**
`frontend/src/components/exame-fisico/BodyMap.vue` filtra os hotspots por `currentPaths.value[r.nome]` — casa região↔desenho por **NOME literal** contra 14 chaves hardcoded em `frontend/src/components/exame-fisico/bodyMapPaths.ts` (`maleRegionPaths`/`femaleRegionPaths` são `Record<string, BodyRegionPath>` com chaves do tipo "Cabeça (anterior)"). Casar por nome torna o mapeamento **frágil a renomeação**: se o admin edita o nome de uma região nível-1, ela some do boneco. Decisão do usuário: **casar por CÓDIGO** (re-indexar `bodyMapPaths` por código). O exame físico já usa código como identidade (`exameFisicoService.mapCatalogoParaLocal`: `id = dto.codigo`, `pai_id = dto.paiCodigo`), então re-indexar é cirúrgico e elimina a fragilidade sem virar boneco dinâmico.

---

## 2. Persona-alvo

- **Dono / administrador da plataforma (admin global, policy `ImedtoAdmin`)** — configura o catálogo de regiões anatômicas em Admin → Catálogos → Regiões anatômicas. Edita nome/template, cria sub-regiões (nível 2 e 3). Frequência: baixa (configuração), mas com expectativa de ver o efeito imediato no exame físico.
- **Profissional de saúde (médico)** — consome o resultado no exame físico do prontuário (mapa corporal + popup de regiões). Não edita o catálogo; só colhe o reflexo da configuração. Alta frequência de uso da tela.

---

## 3. Escopo

**Inclui:**
- **(P1a) TTL longo:** aumentar o TTL do cache do catálogo de regiões em `ListarRegioesCatalogoQueryHandlers.cs` de 30 min para um valor bem maior (ex.: 6h–24h, dev define) — o catálogo raramente muda; performance máxima.
- **(P1b) Invalidação automática:** os 5 handlers admin de mutação de região (Criar/Atualizar/Inativar/Reativar/Excluir) invalidam a(s) chave(s) `catalogo:regioes:*` do `IMemoryCache` após persistir, alinhando ao padrão do `IConfigGlobalReader`. Efeito: a próxima abertura do exame físico após salvar no admin reflete a edição, automaticamente.
- **(P1c) Invalidação manual:** endpoint admin novo (ex.: `POST /api/admin/catalogos/regioes-anatomicas/invalidar-cache`, policy `ImedtoAdmin`) + botão no configurador de regiões do admin ("Forçar atualização do exame físico") que dispara só a invalidação de `catalogo:regioes:*`. Reforço por cima da automática.
- **(P2) Seletor de pai que destrava nível 3:** no `RegioesGlobaisFormView.vue` (modo criação), adicionar um seletor (dropdown/autocomplete) que lista regiões de **nível 1 e nível 2**, achatando `store.arvore`. Escolher uma opção preenche "Código do pai" (dispara o watcher existente que deriva vista e nível). O BodyMap continua como atalho para nível 1. O campo de texto livre "Código do pai" permanece como fallback. Profundidade máxima mantida em **3 níveis**.
- **(P3) Casar BodyMap por código:** re-indexar `bodyMapPaths.ts` por **código** das 14 regiões canônicas (cabeça, pescoço, 8 membros, tronco anterior/posterior) e ajustar o filtro do `BodyMap.vue` para casar por `r.id` (= código) em vez de `r.nome`. O conjunto de polígonos e a geometria do desenho **não mudam** (boneco canônico).
- Atualização de `Docs/ARQUITETURA.md` registrando a invalidação de cache do catálogo de regiões (ver §10).

**Não inclui (anti-scope-creep):**
- **Boneco dinâmico.** Criar uma região **nível 1 totalmente nova** (código que não corresponde a uma das 27 canônicas) **não ganha hotspot desenhado** — exigiria coordenadas SVG que o sistema não captura. O desenho permanece a anatomia canônica. Decisão fechada do usuário (fronteira aceita).
- **Realtime de tela já aberta.** "Repercutir" = próxima abertura/recarregamento do exame físico reflete a edição. Não há push para uma tela do prontuário já carregada (decisão fechada do usuário).
- **Captura/edição de `svg_coords`.** A coluna existe mas continua NULL e não é fonte de verdade (mantém estado pós-briefing 2026-06-22_004).
- **Mudança de regra de profundidade no backend** — o backend já aceita nível 3; nada a mudar lá.
- **Alterar as proteções do nível 1** (briefing 2026-06-23_001: nível 1 não inativa nem exclui) — permanecem intactas.

---

## 4. Regras de negócio

- **R1a (P1 — TTL longo):** o cache do catálogo de regiões em `ListarRegioesCatalogoQueryHandlers.cs` mantém-se ativo com TTL aumentado para um valor bem maior que os 30 min atuais (ex.: 6h–24h, dev define). Mora em: Handler de leitura. Justificativa: catálogo raramente muda; o reflexo da paridade não depende do TTL (depende de R1b/R1c), então o TTL pode ser alto sem prejuízo de paridade. Validada em: back.
- **R1b (P1 — invalidação automática):** ao criar, atualizar, inativar, reativar ou excluir uma região anatômica no admin, o cache `catalogo:regioes:*` é invalidado **na mesma operação**, após o commit/persistência. Mora em: Handler (os 5 `*RegiaoAdminCommandHandler`). Padrão de referência: `IConfigGlobalReader` ("invalida cache após mutação"). A invalidação deve cobrir **todas as variações de chave** que a query gera — a chave varia por `vista` (`anterior`/`posterior`/`circunferencial`/`all`) e por `ApenasAtivas` (`true`/`false`). Como `IMemoryCache` não tem "remover por prefixo" nativo, a estratégia (a critério do dev, mas obrigatória no efeito) deve garantir que **nenhuma variação fique servindo dado velho** — ex.: rastrear as chaves emitidas, ou trocar por um token de versão/`CancellationChangeToken` compartilhado entre as variações. Validada em: back (efeito observável na query).
- **R1c (P1 — invalidação manual):** um endpoint admin dedicado (ex.: `POST /api/admin/catalogos/regioes-anatomicas/invalidar-cache`, policy `ImedtoAdmin`) invalida `catalogo:regioes:*` sob demanda, reutilizando **o mesmo invalidador** de R1b (não duplicar lógica). O configurador de regiões do admin ganha um botão ("Forçar atualização do exame físico") que chama esse endpoint. Mora em: Controller/Handler admin (back) + view do configurador (front). Não confundir com o botão "Atualizar" existente, que só recarrega a árvore do admin (`/api/admin/catalogos/regioes-anatomicas`, sem cache) e **não** toca `catalogo:regioes:*`. Validada em: back (efeito) + front (botão dispara o endpoint certo).
- **R2 (P2 — seletor de pai nível 1+2):** o seletor de pai (modo criação) lista todas as regiões **ativas** de nível 1 e nível 2, com rótulo legível (nome + indicação de nível/vista) e ordenação estável. Escolher uma opção preenche `paiCodigo` com o `codigo` da região escolhida; o watcher existente deriva `vista` e `nivel`. Mora em: Front (`RegioesGlobaisFormView.vue`), consumindo `store.arvore` (já carregada via `carregarArvore`). Sem novo endpoint. Validada em: front (UX). A regra de profundidade (1..3) e a validação `nivel == pai.nivel+1`/`vista == pai.vista` continuam no back (espelho já existente).
- **R3 (P2 — guard de pai circunferencial mantido):** o seletor **não** oferece nós circunferenciais como pai (eles são agregadores nível-1 sem filhos). O guard existente (`MSG_CIRCUNFERENCIAL` / `erroCircunferencial`) permanece e cobre também escolhas vindas do novo seletor. Mora em: Front + Back (`BusinessException` 422 já existe). Validada em: back + front.
- **R4 (P3 — casar por código):** o `BodyMap.vue` resolve o polígono de cada região pelo **código** (`r.id`), não pelo nome. `bodyMapPaths.ts` passa a ser indexado por código. As 14 regiões canônicas com polígono mantêm o mesmo desenho; renomear uma região nível-1 no admin **não** a remove do boneco (o vínculo é por código). Mora em: Front (`BodyMap.vue`, `bodyMapPaths.ts`). Validada em: front.
- **R5 (P3 — paridade do nome/template/vista/lateralidade/status):** o que o admin edita repercute no exame físico na próxima abertura — **nome** (rótulo/tooltip), **template/texto sugerido** de achado, **vista**, **lateralidade**, **status ativo/inativo** (inativa some do exame físico; reativa volta) e **hierarquia de sub-regiões nível 2/3** (aparecem/somem no popup ao clicar na região-pai). Essa repercussão é consequência direta de R1 (cache invalidado) + R4 (casar por código) — não exige código novo de paridade além de R1/R4. Validada em: back (R1) + front (R4 e consumo já existente do popup).
- **R6 (não-regressão do tronco/circunferencial):** os pseudo-hotspots de tronco fundido (`Tronco (anterior)`/`Tronco (posterior)`) e a navegação circunferencial (via `PARTE_PARA_TRONCO`/`RAMOS_CIRCUNFERENCIAL`, indexados por código) continuam funcionando. O re-index por código **não** pode quebrar o acendimento do tronco nem o popup de partes. Mora em: Front. Validada em: front.

---

## 5. Modelo de dados

**Nenhuma migration esperada.** Tudo é comportamento de aplicação:

- **P1** — TTL longo + invalidação de `IMemoryCache` (memória de processo) + endpoint admin novo que só dispara invalidação. Sem schema (o endpoint não persiste nada; só limpa cache de memória).
- **P2** — consumo de dados já expostos (`listarArvore`). Sem schema.
- **P3** — re-index de constante de front (`bodyMapPaths.ts`). Sem schema.

Pontos de atenção que confirmam "sem migration":
- A tabela `regioes_anatomicas_catalogo` já tem `codigo`, `nome`, `vista`, `pai_codigo`, `nivel`, `template_texto`, `ativo`. Nada novo.
- O `imedto-database` deve apenas **confirmar** (sem alterar) que os 14 códigos canônicos usados no re-index (`cabeca-anterior`, `pescoco-anterior`, `torax-anterior`/`abdome-anterior`/`pelve-anterior` → fundidos no tronco, `membro-*-anterior/posterior`, `cabeca-posterior`, `pescoco-posterior`, `torax-posterior`, `lombossacra-posterior`, `pelve-posterior`) batem com os seeds atuais (`db/migrations/20260526000001_seed_regioes_anatomicas_catalogo.sql`). Se baterem (esperado), **zero migration**.

**Multi-tenant:** N/A para o catálogo (é **global**, `eh_padrao_sistema=true`, sem `estabelecimento_id`). O exame físico que o consome é por estabelecimento, mas o catálogo lido é o global — sem cruzamento de tenant. Confirmar que a invalidação de cache não vaza entre tenants: a chave de cache é global (não tem tenant), então invalidar afeta todos igualmente — correto, pois o dado é global.

**LGPD:** o catálogo de regiões **não contém PII** (são rótulos anatômicos e templates clínicos genéricos). Nenhuma mensagem de erro deve vazar dado. Audit: as mutações admin já gravam audit (motivo ≥10 chars + audit admin existente) — **inalterado**; nem a invalidação automática nem a manual acrescentam audit (não mutam dado de domínio — apenas limpam cache de memória). O endpoint manual (R1c) não recebe payload sensível e não precisa de motivo.

---

## 6. UX e fluxo

**P1 — cache reforçado (uma adição de UI: botão manual):**
Fluxo automático: admin edita template da região "Tórax (anterior)" às 14h00 → salva (audit + 422 se inválido) → cache invalidado automaticamente (R1b) → profissional abre o prontuário/exame físico às 14h01 → vê o template novo. Antes: até 30 min de atraso.

Botão manual (R1c) no configurador de regiões do admin (`RegioesGlobaisListView.vue` ou onde fica o botão "Atualizar" atual):
```
┌─ Regiões anatômicas ────────────────────────────────────┐
│  [ Atualizar (árvore admin) ]  [ Forçar atualização     │  ← NOVO: botão dedicado
│                                  do exame físico ]       │     (chama .../invalidar-cache)
│  ...árvore de regiões...                                  │
└──────────────────────────────────────────────────────────┘
```
- Componente: `AppButton` (variant secundário/ghost). Ao clicar → `POST .../regioes-anatomicas/invalidar-cache` → feedback de sucesso (toast/inline "Exame físico atualizado").
- **Estados:** *loading* no botão durante a chamada; *sucesso* (toast curto); *erro* (mensagem genérica, não bloqueia). O botão é reforço — a invalidação automática (R1b) já cobre o caso normal.
- **Decisão de UX (recomendada e adotada):** botão **dedicado e separado** do "Atualizar" existente, rótulo explícito "Forçar atualização do exame físico" — porque os dois têm efeitos diferentes (um recarrega a árvore do admin, outro limpa o cache do prontuário) e fundi-los esconderia o efeito real. Sem mudança no botão "Atualizar" legado.

**P2 — seletor de pai (modo criação do `RegioesGlobaisFormView.vue`):**
```
┌─ Nova região anatômica ─────────────────────────────────┐
│  Código *            [ ABD-SUP-D            ]            │
│                                                          │
│  ┌ Selecionar pai pelo mapa (atalho) ─────────────┐     │
│  │   [ boneco BodyMap clicável — nível 1 ]         │     │
│  └─────────────────────────────────────────────────┘     │
│                                                          │
│  Pai (lista)         [ ▼ Selecione nível 1 ou 2     ]   │  ← NOVO (R2)
│     • Tórax (anterior)            — nível 1              │
│     • Abdome (anterior)           — nível 1              │
│     • Quadrante sup. dir. (abd.)  — nível 2             │
│     ...                                                  │
│                                                          │
│  Vista               [ derivada do pai ] (desabilitado) │
│  Código do pai       [ ABD            ] (texto livre,    │  ← fallback mantido
│                                          preenchido pela │
│                                          escolha acima)  │
│  Nível               [ 3 ] (derivado, desabilitado)     │
│  ...                                                      │
└──────────────────────────────────────────────────────────┘
```
- Componente do design system: usar `AppSelect` (já importado na view) ou um autocomplete equivalente do DS se o volume justificar (catálogo é pequeno — `AppSelect` basta). Não criar componente novo se o `AppSelect` resolve.
- O seletor é **atalho/conveniência**: o campo "Código do pai" continua editável manualmente (fallback). Escolher no seletor é equivalente a digitar o código.
- **Estados:**
  - *Loading:* enquanto `store.arvore` carrega, o seletor mostra estado de carregamento ou opções vazias (não bloqueia o formulário — comportamento silencioso já existente, CA11 do briefing anterior).
  - *Vazio:* se a árvore não carregou (falha silenciosa), o seletor fica vazio e o admin usa o campo texto livre (fallback).
  - *Circunferencial escolhido:* bloqueado com `MSG_CIRCUNFERENCIAL` (R3).
- **Edição:** o seletor **não aparece** no modo edição (hierarquia imutável) — igual ao BodyMap (`v-if="!editando"`).

**P3 — casar por código (sem mudança visual perceptível quando os nomes estão corretos):**
- O boneco desenha exatamente as mesmas 14 regiões canônicas. A diferença é robustez: renomear uma região no admin não a faz sumir do boneco.
- **Estado de não-correspondência:** se um código nível-1 do catálogo não tiver polígono correspondente em `bodyMapPaths` (ex.: região nível-1 nova criada pelo admin), ela simplesmente não vira hotspot — sem erro, sem quebra (comportamento canônico aceito, §3 "Não inclui").

---

## 7. Critérios de aceite (testáveis)

- **CA1 (P1b — caminho feliz, invalidação automática):** Dado um admin que edita o **template** da região "Tórax (anterior)" e salva, Quando o profissional abre o exame físico do prontuário em seguida (nova requisição a `GET /api/catalogo/regioes-anatomicas`), Então o texto sugerido reflete o novo template **na primeira abertura após o salvamento** (sem esperar o TTL, sem restart do backend) — a invalidação automática disparou no salvar.
- **CA2 (P1 — nome reflete):** Dado um admin que edita o **nome** de uma região nível-1 e salva, Quando o exame físico é reaberto, Então o novo nome aparece no rótulo/tooltip do mapa (e a região continua desenhada no boneco — ver CA9, casa por código).
- **CA3 (P1 — inativar/reativar reflete):** Dado um admin que **inativa** uma sub-região nível 2 e salva, Quando o exame físico é reaberto, Então a sub-região não aparece mais no popup; e Quando o admin a **reativa** e o exame físico é reaberto, Então ela volta a aparecer — ambos na primeira abertura após salvar.
- **CA4 (P1 — todas as variações de chave invalidadas):** Dado que a query é cacheada por `vista` e `ApenasAtivas` (múltiplas chaves), Quando uma mutação admin ocorre, Então **nenhuma** variação de chave (`vista=anterior/posterior/circunferencial/all` × `ativas=true/false`) continua servindo o dado anterior à mutação — todas refletem o estado pós-mutação na próxima leitura.
- **CA5 (P2 — destrava nível 3, caminho feliz):** Dado um admin no formulário de criação que escolhe no **seletor de pai** uma sub-região de **nível 2**, Quando o sistema deriva o nível, Então o nível passa a **3** (e vista herdada do pai), e Quando salva, Então a região nível-3 é criada com sucesso (backend aceita) e aparece no popup do exame físico ao navegar até a região-pai.
- **CA6 (P2 — seletor lista nível 1 e 2, não nível 3):** Dado o seletor de pai aberto, Quando o admin abre as opções, Então só aparecem regiões **ativas de nível 1 e 2** (nunca nível 3, para respeitar o teto de profundidade 3); regiões inativas e circunferenciais não são oferecidas.
- **CA7 (P2 — guard circunferencial via seletor):** Dado que o admin escolhe (ou digita) um pai circunferencial, Quando o formulário valida, Então mostra `MSG_CIRCUNFERENCIAL` e bloqueia o submit (espelho do 422 do backend).
- **CA8 (P2 — fallback texto livre intacto):** Dado um admin que ignora o seletor e digita o código do pai no campo de texto livre, Quando o código existe e é válido, Então o comportamento atual (derivação de vista/nível pelo watcher) permanece idêntico — o seletor não quebra o fluxo legado.
- **CA9 (P3 — casa por código, renomear não some):** Dado que o admin **renomeia** a região nível-1 "Tórax (anterior)" para outro rótulo e salva, Quando o exame físico é reaberto, Então o hotspot do tronco/tórax **continua desenhado e clicável** no boneco (o vínculo polígono↔região é por código, não por nome).
- **CA10 (P3 — boneco canônico inalterado):** Dado o exame físico aberto com o catálogo nos códigos canônicos, Quando o mapa renderiza, Então as 14 regiões canônicas (cabeça, pescoço, 8 membros, tronco ant./post.) aparecem com a **mesma geometria** de antes do briefing (sem regressão visual) e a coloração por vista (`vistasPorId`) e o tronco fundido continuam funcionando (R6).
- **CA11 (P3 — região nível-1 nova sem polígono = no-op):** Dado que o admin cria uma região **nível-1 nova** com código sem polígono em `bodyMapPaths`, Quando o exame físico renderiza, Então ela **não vira hotspot** no boneco e **nenhum erro** é lançado (fronteira do boneco canônico, §3).
- **CA12 (RBAC — admin global):** Dado um usuário **sem** a policy `ImedtoAdmin`, Quando tenta acessar o formulário/endpoints de mutação de região, Então recebe a negativa padrão da área admin (não autenticado → redirect `/admin/login`; sem claim → bloqueado) — comportamento existente, não regride.
- **CA13 (multi-tenant — catálogo global):** Dado que o catálogo de regiões é **global** (sem `estabelecimento_id`), Quando um admin invalida o cache via mutação, Então a invalidação vale para todos os estabelecimentos igualmente (o dado é global por design) e nenhuma leitura de exame físico de outro tenant vê dado inconsistente entre si.
- **CA14 (LGPD — sem PII em erro/log):** Dado qualquer erro (validação 422 ao criar/editar, falha de carga da árvore no seletor), Quando a mensagem é exibida/logada, Então é genérica e **não contém PII** (o catálogo não tem PII; confirmar que nada de paciente vaza por aqui).
- **CA15 (P1a — performance, TTL longo e cache preservado):** Dado o exame físico aberto repetidamente **sem** mutação admin no intervalo, Quando as requisições a `GET /api/catalogo/regioes-anatomicas` ocorrem, Então o cache continua servindo (o payload pesado não é re-serializado a cada abertura) e o **TTL aumentado** (≥6h) mantém o dado cacheado por muito mais tempo que os 30 min antigos — a invalidação **não** desliga o cache, só o limpa após mutação ou disparo manual.
- **CA18 (P1c — invalidação manual via botão):** Dado um admin no configurador de regiões que clica em "Forçar atualização do exame físico", Quando o `POST .../regioes-anatomicas/invalidar-cache` responde, Então o cache `catalogo:regioes:*` (todas as variações) é limpo e a próxima abertura do exame físico relê do banco; o botão dá feedback de sucesso e **não** afeta o botão "Atualizar" da árvore admin.
- **CA19 (RBAC — endpoint de invalidação manual):** Dado um usuário **sem** a policy `ImedtoAdmin`, Quando chama `POST .../regioes-anatomicas/invalidar-cache`, Então recebe a negativa padrão da área admin (não autenticado → 401/redirect; autenticado sem claim admin → bloqueado), e o botão não está acessível fora do admin.
- **CA16 (performance — seletor sem request extra):** Dado o formulário de criação, Quando o seletor de pai é populado, Então ele reutiliza `store.arvore` já carregada (a mesma usada pelo BodyMap) e **não dispara uma requisição adicional** dedicada ao seletor.
- **CA17 (documentação viva):** Dado que a entrega introduz a estratégia de cache reforçado (TTL longo + invalidação automática + invalidação manual via endpoint admin) no catálogo de regiões, Quando o PR é aberto, Então `Docs/ARQUITETURA.md` registra esse comportamento na seção do catálogo (§10) — o QA valida que o doc foi atualizado.

---

## 8. Riscos e dependências

- **Risco P1 — invalidação parcial de chave (o mais perigoso):** se o dev invalidar só a chave atual (ex.: `vista=all:ativas=true`) e esquecer as outras variações, o exame físico pode continuar servindo dado velho em alguma vista. **CA4 é o guard explícito.** Recomendação: usar um mecanismo que cubra todas as variações de uma vez (token de versão / `CancellationChangeToken` compartilhado, ou rastrear o conjunto de chaves emitidas). O dev tem liberdade técnica desde que CA4 passe.
- **Risco P3 — regressão silenciosa do tronco/membros:** o re-index por código precisa preservar o acendimento do tronco fundido e o agrupamento de membros (que hoje dependem de `PARTE_PARA_TRONCO`, já por código, e de regex de nome em `getMembroGroup`). **Atenção:** `getMembroGroup` em `BodyMap.vue` usa regex no **nome** (`MEMBRO_RE`) para agrupar membros no hover — isso é hover/agrupamento, não casamento de polígono, e pode permanecer por nome; o re-index por código é só para resolver o **polígono**. CA10/CA6/R6 cobrem a não-regressão. Único consumidor de produção do BodyMap no exame físico: `SecaoExameFisico.vue` (via `ConsultaAtualTab.vue`) — validar lá.
- **Risco P2 — seletor confunde com o campo texto livre:** dois caminhos para o mesmo dado (`paiCodigo`). Mitigação: o seletor só **preenche** o campo texto livre; a fonte de verdade do submit continua sendo `paiCodigo`. CA8 garante o fallback.
- **Dependência de testes existentes:** há suíte para `BodyMap.test.ts`, `RegioesGlobaisFormView.test.ts`, `SecaoExameFisico.test.ts`, `RegionSelectorPopup.test.ts`, `regioesCircunferenciais.test.ts`. O re-index por código provavelmente quebra fixtures que montam regiões por nome — **atualizar os testes** faz parte da entrega (não é regressão de produto, é ajuste de fixture ao novo contrato por código).
- **Sem dependência de schema:** confirmar com `imedto-database` que os 14 códigos canônicos batem com os seeds (esperado: sim). Se baterem, nenhuma migration.
- **Gotcha de deploy (referência):** o deploy reaplica os `.sql` de `db/migrations/` sempre — mas este briefing **não toca seed**, então não há risco de re-seed indesejado (ver memória `project_gotcha_deploy_reaplica_seeds_sql`).

---

## 9. Observações para execução

**Não-negociável:**
- **CA4** (todas as variações de chave invalidadas) é a essência do requisito-mãe de paridade. Não fechar P1 com invalidação parcial.
- **R4/CA9** — o vínculo polígono↔região é por **código**. Renomear não pode sumir do boneco.
- **Boneco canônico:** não desenhar regiões novas; geometria dos polígonos intacta (CA10).
- **Reuso:** seguir o padrão do `IConfigGlobalReader` para a invalidação (não inventar mecanismo paralelo se já há um idiomático no projeto). A invalidação automática (R1b) e a manual (R1c) **compartilham o mesmo invalidador** — não duplicar a lógica de limpeza de `catalogo:regioes:*`. No front, reusar `AppSelect`, `AppButton` e `store.arvore`. Único endpoint novo é o de invalidação manual (R1c); fora dele, não criar endpoint nem componente novo.

**Liberdade técnica:**
- Mecanismo exato de invalidação de cache (token de versão vs. rastreamento de chaves vs. `CancellationChangeToken`) — escolha do dev, desde que CA4 passe.
- Forma do seletor de pai (`AppSelect` simples vs. autocomplete) — o catálogo é pequeno; `AppSelect` é suficiente. Não introduzir componente novo no DS sem necessidade.

**Pipeline:** `imedto-developer` (front + backend) → aciona `imedto-database` **apenas para confirmar** que os códigos canônicos batem com os seeds (provável zero migration) → `imedto-qa`.

---

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — na seção dos **Catálogos Globais** (onde já se descreve o live-link de regiões anatômicas), **registrar a estratégia de cache reforçado**: o handler de leitura `ListarRegioesCatalogoQueryHandlers` cacheia o catálogo de regiões em `IMemoryCache` com **TTL longo** (≥6h, era 30 min — chave `catalogo:regioes:vista={...}:ativas={...}`); o cache é invalidado de duas formas: **(a) automaticamente** pelos 5 handlers admin de mutação após persistir, e **(b) manualmente** por um endpoint admin (`POST .../regioes-anatomicas/invalidar-cache`, policy `ImedtoAdmin`) acionado por um botão no configurador. Ambos compartilham o mesmo invalidador e cobrem todas as variações de chave. Alinha ao padrão já documentado do `IConfigGlobalReader` ("invalida cache após mutação"). Mudança **incremental/cirúrgica** (não reescrever a seção inteira). Mantém o cabeçalho "Quando ler/atualizar" coerente.
- **`Docs/DESIGN.md`** — **não precisa**, salvo se o dev acabar extraindo um componente novo de seletor para o DS (improvável; `AppSelect` resolve). Se extrair, documentar na seção de componentes. A seção do BodyMap (§Mapa corporal interativo) **não muda de comportamento visual**; o re-index por código é interno — não exige atualização de doc por si só, mas o dev pode anotar 1 linha de que o casamento polígono↔região é por código (a critério, não bloqueante).
- **`Docs/LGPD.md`** — **não precisa** (catálogo sem PII, audit inalterado).
- **Migrations / `Docs/COMANDOS.md`** — **não precisa** (sem migration esperada).
