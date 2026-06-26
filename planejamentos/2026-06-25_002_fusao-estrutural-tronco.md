# Fusão estrutural do tronco (Tórax/Abdome/Pelve → Tronco)

**ID**: 2026-06-25_002
**Status**: Aguardando OK explícito do usuário (decisões de produto pré-confirmadas via orquestrador — ver §0)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: catálogo de regiões anatômicas (admin global) / exame físico do prontuário / mapa corporal (BodyMap) / popup de seleção de regiões
**Branch de implementação**: `fix/paridade-regioes-anatomicas-exame-fisico` (mesma da família 2026-06-25_001)

---

## 0. Nota de governança e pontos a confirmar no OK

Mudança **estrutural** de catálogo descoberta no teste do usuário, no contexto da família 2026-06-25_001. Decisão "Trilha 2 — fusão **estrutural**" relatada pelo orquestrador como confirmada com o usuário. O ambiente da pipeline sinaliza que relato de orquestrador **não substitui** confirmação direta do usuário. Portanto:

- O conteúdo técnico (seeds atuais, constantes de tronco no front, ausência de dado clínico) é **factual e verificado no código/banco**.
- As decisões de produto estão registradas na recomendação do BA, coincidindo com o relato.
- O **dev/db só inicia após OK explícito do usuário**.

**Mapeamento da fusão — CONFIRMADO (relatado pelo orquestrador, a ratificar no OK do usuário).** Cada trio por vista vira uma região só:
- **`tronco-anterior`** ("Tronco (anterior)", vista `anterior`) ← funde `torax-anterior` + `abdome-anterior` + `pelve-anterior`.
- **`tronco-posterior`** ("Tronco (posterior)", vista `posterior`) ← funde `torax-posterior` + **`lombossacra-posterior`** + `pelve-posterior`. **A região lombossacra está incluída no Tronco (posterior)** (ponto clínico de maior peso — confirmado).
- **`tronco-circunferencial`** ("Tronco (circunferencial)", vista `circunferencial`) ← funde `torax-circunferencial` + `abdome-circunferencial` + `pelve-circunferencial`.
- **Lateralidade = `false`** nas 3 (tronco não tem E/D).
- "Fundir" = no **catálogo**: remover as 9 antigas e criar as 3 — **sem migração de dado clínico** (inspeção do `imedto-database` deu 0 achados/sub-regiões).

Esses pontos estavam pendentes na primeira redação e foram **confirmados** pelo orquestrador junto ao usuário. Permanece valendo a governança: o **OK direto do usuário** é a porta de entrada do dev/db. Não há ambiguidade de produto aberta.

---

## 1. Contexto e motivação

Hoje o tronco **não existe como região**: é um conjunto de 9 regiões nível-1 separadas — `torax/abdome/pelve` em anterior e circunferencial, e `torax/lombossacra/pelve` em posterior — fundidas **só visualmente** no mapa por uma camada de pseudo-hotspots sintéticos (`tronco-anterior`/`tronco-posterior` em `bodyMapPaths.ts`, sem correspondente no catálogo) e por lógica de "lista agrupada por parte" no popup (`troncoGrupos`/`GRUPOS_TRONCO_*`).

No teste, o usuário decidiu **fundir o tronco de verdade**: parar de separar Tórax/Abdome/Pelve e ter **uma região "Tronco" por vista**. Isso simplifica o cadastro, o mapa e o popup, e alinha o catálogo ao que o usuário quer ver no exame físico (a fusão deixa de ser um truque de UI e passa a ser o modelo de dados real).

**Reversão parcial de briefing anterior:** isto **reverte parte do `planejamentos/2026-06-23_001`** (que padronizou/separou as partes nível-1, incluindo tórax/abdome/pelve). Registro explícito: as partes que aquele briefing manteve separadas no tronco passam a ser fundidas aqui. As demais decisões do 2026-06-23_001 (proteção do nível 1, códigos de membros expandidos) **permanecem**.

**Supera o addendum-002 da família 001:** a parte do tronco do `2026-06-25_001_..._addendum-002.md` (R14/CA33 — "um geral por parte" no circunferencial do tronco) fica **SUPERADA** por este briefing: com o tronco virando região real, o "geral" do tronco passa a funcionar como o de qualquer outra região (cabeça/pescoço/membro), sem caso especial.

**Sem dilema LGPD / sem migração de dado clínico:** a inspeção do `imedto-database` confirmou **zero impacto em dado clínico** — 0 sub-regiões filhas do tronco, 0 achados em `exame_fisico_regioes`, 0 exames físicos, 0 anexos com região, 0 prontuários/pacientes afetados. O banco está em fase **pré-uso clínico**. Logo, **não há migração nem descarte de dado de paciente** — é remover do catálogo + ajustar seeds + ajustar frontend. Nenhum dado sensível é tocado; nenhuma decisão de retenção/anonimização se aplica.

---

## 2. Persona-alvo

- **Dono / administrador da plataforma (admin global, policy `ImedtoAdmin`)** — vê o catálogo de regiões simplificado (Tronco no lugar de Tórax/Abdome/Pelve). Baixa frequência.
- **Profissional de saúde** — no exame físico, clica em "Tronco" no mapa (hotspot real) e registra achados na região "Tronco" da vista escolhida. Alta frequência.

---

## 3. Escopo

**Inclui:**

**A — Catálogo / banco (imedto-database):**
- Criar 3 regiões nível 1: `tronco-anterior` ("Tronco (anterior)", vista `anterior`), `tronco-posterior` ("Tronco (posterior)", vista `posterior`), `tronco-circunferencial` ("Tronco (circunferencial)", vista `circunferencial`), todas `pai_codigo=NULL`, `nivel=1`, `lateralidade=false`, `ativo=true`, com `ordem` coerente.
- Remover do catálogo as 9 regiões antigas: `torax-anterior`, `abdome-anterior`, `pelve-anterior` (anterior); `torax-posterior`, `lombossacra-posterior`, `pelve-posterior` (posterior); `torax-circunferencial`, `abdome-circunferencial`, `pelve-circunferencial` (circunferencial).
- Migration + **ajuste dos seeds `.sql`** (`20260526000001_seed_regioes_anatomicas_catalogo.sql` e `20260608120000_seed_regioes_anatomicas_circunferenciais.sql`) para **não recriar** as 9 antigas e **criar** as 3 de tronco. Idempotente e que **não aborte** o migrate.

**B — Frontend (imedto-developer):** remover a camada de "tronco fundido sintético" agora que o tronco é região real:
- `bodyMapPaths.ts` — chaves `tronco-anterior`/`tronco-posterior` deixam de ser sintéticas e passam a casar com regiões reais nível-1 (via `regioesComPath`, que já casa por `r.id` = código — briefing 2026-06-25_001 P3). Não há `tronco-circunferencial` em `bodyMapPaths` (circunferencial nunca é hotspot direto — coerente com o resto do catálogo).
- `BodyMap.vue` — remover/simplificar `CODIGOS_TRONCO`, `TRONCO_HOTSPOTS`/`troncoHotspots`, `classeVistaTronco`, e a camada de pseudo-hotspot (`TroncoClique`, evento `troncoClicado`, `handleTroncoClick`). O tronco vira hotspot normal renderizado por `regioesComPath`.
- `regioesCircunferenciais.ts` — em `RAMOS_CIRCUNFERENCIAL`: remover `torax/abdome/pelve-circunferencial`; adicionar `tronco-circunferencial: { anterior: 'tronco-anterior', posterior: 'tronco-posterior' }`. Remover `PARTE_PARA_TRONCO` (deixa de ter uso).
- `RegionSelectorPopup.vue` — o modo `troncoGrupos` deixa de ser necessário; o tronco vira região normal. O "geral" circunferencial do tronco passa a funcionar como o de cabeça/pescoço/membro (R13 do addendum-002, parte não-tronco). O guard que retornava lista vazia para `troncoGrupos` no circunferencial pode ser revisto/removido.
- `SecaoExameFisico.vue` — remover `GRUPOS_TRONCO_ANTERIOR`/`GRUPOS_TRONCO_POSTERIOR`, `troncoGruposAtivos`, a propagação via `PARTE_PARA_TRONCO` e o `vistaInicial` ligado a tronco-anterior/posterior. O clique no tronco passa a ser um clique de região normal.
- Ajustar/atualizar os **testes** que dependiam de tronco sintético / `troncoGrupos` / `PARTE_PARA_TRONCO` (`BodyMap.test.ts`, `RegionSelectorPopup.test.ts`, `SecaoExameFisico.test.ts`, `regioesCircunferenciais.test.ts`).

**C — Documentação:** atualizar `Docs/ARQUITETURA.md` (modelo do tronco — ver §10).

**Não inclui:**
- **Migração de dado clínico** — não há dado (confirmado). Nada a migrar/anonimizar.
- **Mudança na coluna `cabeca-anterior-olho` ou em qualquer sub-região de cabeça/pescoço/membros** — intocadas. A fusão é **só do tronco**.
- **Mudança no padrão de prefixo do código (addendum-1 da família 001)** — independente; segue valendo.
- **Mudança na estratégia de cache (briefing 001 Trilha 1)** — reusada: editar/criar/remover regiões já invalida `catalogo:regioes:*` (ver R5/CA performance).
- **Backend de domínio / endpoints** — nenhum endpoint novo; a remoção/criação usa o fluxo de catálogo já existente (admin) ou o seed. (Se o dono quiser remover via UI admin em vez de seed, o `ExcluirRegiaoAdminCommandHandler` hoje **bloqueia exclusão de nível 1** — briefing 2026-06-23_001; por isso a remoção das 9 antigas é feita por **migration/seed**, não pela UI. Ver R7.)

---

## 4. Regras de negócio

- **R1 (3 regiões de tronco — mapeamento exato):** o catálogo passa a ter exatamente 3 regiões de tronco nível-1, `pai_codigo=NULL`, `nivel=1`, `lateralidade=false`, `ativo=true`:
  - `tronco-anterior` = "Tronco (anterior)" (vista `anterior`) — substitui `torax-anterior` + `abdome-anterior` + `pelve-anterior`.
  - `tronco-posterior` = "Tronco (posterior)" (vista `posterior`) — substitui `torax-posterior` + `lombossacra-posterior` + `pelve-posterior` (**a lombossacra está incluída no tronco posterior**).
  - `tronco-circunferencial` = "Tronco (circunferencial)" (vista `circunferencial`) — substitui `torax-circunferencial` + `abdome-circunferencial` + `pelve-circunferencial`.
  Mora em: catálogo (seed/migration). Validada em: banco (linhas presentes) + front (hotspot/lista refletem).
- **R2 (remoção das 9 antigas):** deixam de existir no catálogo, por trio: **anterior** `torax-anterior`/`abdome-anterior`/`pelve-anterior`; **posterior** `torax-posterior`/`lombossacra-posterior`/`pelve-posterior`; **circunferencial** `torax-circunferencial`/`abdome-circunferencial`/`pelve-circunferencial`. Mora em: catálogo (seed/migration). Validada em: banco (ausentes) + front (não aparecem).
- **R3 (tronco circunferencial simétrico — fim da exceção do abdome):** `RAMOS_CIRCUNFERENCIAL['tronco-circunferencial'] = { anterior: 'tronco-anterior', posterior: 'tronco-posterior' }`. A exceção clínica `abdome-circunferencial → lombossacra-posterior` deixa de existir (não há mais abdome-circunferencial; a lombossacra está absorvida no tronco-posterior). Mora em: Front (`regioesCircunferenciais.ts`) + catálogo. Validada em: front.
- **R4 (tronco é região real no mapa):** o tronco deixa de ser pseudo-hotspot sintético e passa a ser hotspot normal, casado por **código** (`r.id`) via `regioesComPath` (reusa P3 do briefing 001). Clicar no tronco abre o popup como qualquer outra região nível-1. Mora em: Front (`BodyMap.vue`, `bodyMapPaths.ts`). Validada em: front.
- **R5 (paridade/cache — reuso do briefing 001):** criar/remover essas regiões no catálogo invalida o cache `catalogo:regioes:*` (mecanismo do briefing 2026-06-25_001 Trilha 1), de modo que o exame físico reflete a fusão na próxima abertura. Para a entrega via **seed/deploy**, o catálogo é relido após o deploy (cache de processo novo). Mora em: Back (já existente). Validada em: back/efeito.
- **R6 (multi-tenant — catálogo global):** o catálogo de regiões é **global** (sem `estabelecimento_id`); a fusão vale para todos os estabelecimentos por construção. Sem cruzamento de tenant. Mora em: catálogo. Validada em: banco.
- **R7 (remoção por seed/migration, não por UI):** como a UI admin **bloqueia exclusão de nível 1** (briefing 2026-06-23_001, proteção mantida), a remoção das 9 antigas e a criação das 3 de tronco são feitas por **migration/seed idempotente**, não pela tela. A proteção de nível 1 na UI **permanece** (não é alterada por este briefing). Mora em: DB. Validada em: banco + deploy.
- **R8 (idempotência e não-abortar o deploy — GOTCHA crítico):** o deploy **reaplica todos os `db/migrations/*.sql`** a cada deploy (memória `project_gotcha_deploy_reaplica_seeds_sql`). Os seeds devem ser ajustados/superados para **não recriar** as 9 antigas e **criar** as 3 de tronco de forma **idempotente** e que **não aborte** o migrate (sem erro fatal que trave o `apply` e impeça o app de subir). Mora em: DB (`db/migrations/`). Validada em: banco (reaplicar 2× = mesmo estado) + smoke de deploy.

---

## 5. Modelo de dados

**Tabela:** `regioes_anatomicas_catalogo` (`codigo`, `nome`, `pai_codigo`, `nivel`, `vista`, `ordem`, `lateralidade`, `ativo`).

**Mudança:**
- **+3 linhas:** `tronco-anterior`/`tronco-posterior`/`tronco-circunferencial` (nível 1, vistas respectivas, `lateralidade=false`, `ativo=true`).
- **−9 linhas:** as 6 antigas anterior/posterior + 3 circunferenciais.
- **Seeds a ajustar:** `20260526000001_seed_regioes_anatomicas_catalogo.sql` (remover as 6 linhas de torax/abdome/pelve/lombossacra, adicionar tronco-anterior e tronco-posterior) e `20260608120000_seed_regioes_anatomicas_circunferenciais.sql` (remover as 3 linhas torax/abdome/pelve-circunferencial, adicionar tronco-circunferencial). **Estratégia de remoção** (decisão técnica do `imedto-database`, mas obrigatória no efeito): como o deploy reaplica os `.sql`, a forma idempotente é um migration/seed que **delete** as 9 antigas e **upsert** as 3 novas (ou ajustar os INSERTs originais para já não conter as antigas + um migration que limpe resíduos). Não pode abortar se as linhas já não existirem.

**Multi-tenant:** N/A (catálogo global, sem `estabelecimento_id`).

**LGPD:** **sem dado clínico afetado** (0 achados, 0 exames, 0 anexos, 0 sub-regiões — confirmado pelo `imedto-database`). Banco pré-uso clínico. Sem PII, sem audit de paciente, sem retenção/anonimização aplicável. As mutações admin de catálogo (quando houver) já têm audit próprio — inalterado.

---

## 6. UX e fluxo

**Mapa corporal (BodyMap):**
```
ANTES: clicar na barriga → pseudo-hotspot "Tronco (anterior)" sintético → popup com lista agrupada Tórax/Abdome/Pelve
DEPOIS: clicar na barriga → hotspot real "Tronco (anterior)" (região do catálogo) → popup da região Tronco (anterior), como qualquer região nível-1
```
- O desenho do tronco no boneco **não muda** (mesmo polígono `tronco-anterior`/`tronco-posterior` de `bodyMapPaths.ts`) — só deixa de ser sintético e passa a casar com a região real por código.
- Coloração por vista (`vistasPorId`) continua funcionando: o tronco acende na cor da vista quando examinado (anterior/posterior/circunferencial).

**Popup de seleção (RegionSelectorPopup):**
```
ANTES (tronco): passo de sub-regiões mostrava grupos Tórax/Abdome/Pelve (troncoGrupos)
DEPOIS (tronco): passo de sub-regiões da região "Tronco (vista)" — lista os filhos do tronco (se houver) + opção "(geral)" = registra a própria região do tronco
```
- No circunferencial do tronco, o "(geral)" passa a funcionar como em cabeça/pescoço/membro (registra `tronco-circunferencial`) — o caso especial de "um geral por parte" (addendum-002 R14/CA33) deixa de existir.

- **Estados:** *loading* do catálogo (já existente); *vazio* (tronco sem filhos → popup mostra só o "geral", comportamento normal de região folha); *já examinado* (tronco acende na cor da vista).
- Sem mudança de design system; reusa hotspot/lista/"geral" existentes.

---

## 7. Critérios de aceite (testáveis)

- **CA1 (catálogo — 3 regiões de tronco existem):** Dado o catálogo após a migration/seed, Quando se consulta `regioes_anatomicas_catalogo`, Então existem exatamente `tronco-anterior` (vista anterior), `tronco-posterior` (vista posterior) e `tronco-circunferencial` (vista circunferencial), todas nível 1, `pai_codigo=NULL`, `lateralidade=false`, `ativo=true`.
- **CA2 (catálogo — 9 antigas removidas):** Dado o catálogo após a migration/seed, Quando se consulta `regioes_anatomicas_catalogo`, Então **não** existem `torax-anterior`, `abdome-anterior`, `pelve-anterior`, `torax-posterior`, `lombossacra-posterior`, `pelve-posterior`, `torax-circunferencial`, `abdome-circunferencial`, `pelve-circunferencial`.
- **CA3 (idempotência do seed — GOTCHA deploy):** Dado o conjunto de migrations/seeds aplicado, Quando o deploy reaplica **todos** os `db/migrations/*.sql` uma segunda vez (simulando o comportamento real), Então o estado final é idêntico (3 troncos presentes, 9 antigas ausentes), **nenhum erro aborta** o `apply`, e o app sobe normalmente.
- **CA4 (mapa — tronco é hotspot real):** Dado o exame físico aberto, Quando o profissional clica na região do tronco no boneco (anterior ou posterior), Então abre o popup da região "Tronco (vista)" — **sem** passar pela lista agrupada Tórax/Abdome/Pelve — e o desenho do tronco é o mesmo de antes (sem regressão visual).
- **CA5 (mapa — coloração por vista do tronco):** Dado que o tronco foi examinado numa vista, Quando o mapa renderiza, Então o hotspot do tronco acende na cor da vista correspondente (anterior/posterior/circunferencial), via `vistasPorId`, igual a qualquer região nível-1.
- **CA6 (circunferencial do tronco simétrico):** Dado o plano **Circunferencial** do tronco, Quando o popup resolve os ramos, Então usa `tronco-anterior` (anterior) + `tronco-posterior` (posterior) — **sem** a exceção do abdome/lombossacra — e a opção "(geral)" registra `tronco-circunferencial`.
- **CA7 (popup — tronco vira região normal):** Dado o popup aberto na região do tronco, Quando o passo de sub-regiões renderiza, Então exibe os filhos do tronco (se houver) e a opção "(geral)" que registra a própria região do tronco da vista — **sem** o modo `troncoGrupos` (que separava Tórax/Abdome/Pelve).
- **CA8 (registrar achado no tronco):** Dado o "(geral)" ou um filho do tronco selecionado, Quando o profissional confirma, Então o achado é registrado na região de tronco correta com a `vista` escolhida (anterior/posterior/circunferencial).
- **CA9 (multi-tenant — global):** Dado que o catálogo é global, Quando a fusão é aplicada, Então vale para todos os estabelecimentos igualmente; nenhum tenant vê as 9 antigas nem deixa de ver o tronco.
- **CA10 (cache/paridade — reuso Trilha 1):** Dado que as regiões mudaram no catálogo, Quando o exame físico é aberto após o deploy (ou após mutação admin que invalide `catalogo:regioes:*`), Então reflete a fusão (tronco presente, 9 antigas ausentes) sem esperar o TTL do cache.
- **CA11 (não-regressão — cabeça/pescoço/membros):** Dado o exame físico, Quando se examina cabeça, pescoço ou membros (anterior/posterior/circunferencial), Então o comportamento é **idêntico** ao anterior — a fusão tocou **apenas** o tronco; `cabeca-anterior-olho` e demais sub-regiões seguem intactas.
- **CA12 (não-regressão — testes do front atualizados e verdes):** Dado o ajuste das constantes de tronco, Quando a suíte de front roda, Então os testes que dependiam de tronco sintético/`troncoGrupos`/`PARTE_PARA_TRONCO` foram atualizados para o novo modelo e passam (sem deixar referência morta a código removido).
- **CA13 (LGPD — sem dado clínico afetado):** Dado que não há achados/exames/anexos/sub-regiões ligados às 9 antigas (confirmado), Quando a remoção é aplicada, Então nenhum dado de paciente é perdido ou órfão — a operação é só de catálogo.
- **CA14 (RBAC — admin global):** Dado que o catálogo é editado por admin global (policy `ImedtoAdmin`) e que a remoção de nível 1 é feita por seed/migration (UI bloqueia exclusão de nível 1, proteção mantida), Quando um usuário não-admin acessa, Então nada muda no acesso — a fusão não abre nenhuma permissão nova.
- **CA15 (documentação viva):** Dado que o modelo do tronco mudou (de fundido-sintético para região real), Quando o PR é aberto, Então `Docs/ARQUITETURA.md` é atualizado na seção de hierarquia de regiões (§10) — o QA/usuário valida.

---

## 8. Riscos e dependências

- **Risco — seed que aborta o deploy (GOTCHA crítico):** se a remoção das 9 antigas usar DDL/DML que falha quando as linhas já não existem, o `apply` aborta e o app não sobe (incidente análogo já ocorreu — memória `project_gotcha_deploy_reaplica_seeds_sql`). CA3 é o guard: idempotente, reaplicável, sem abortar.
- **Risco — referência morta no front:** remover `PARTE_PARA_TRONCO`/`troncoGrupos`/`TRONCO_HOTSPOTS` exige varrer todos os importadores (`SecaoExameFisico.vue`, `BodyMap.vue`, `RegionSelectorPopup.vue`, `RegioesGlobaisFormView.vue` do addendum-1 da família 001 que importa `PARTE_PARA_TRONCO`). **Atenção:** `RegioesGlobaisFormView.vue` usa `PARTE_PARA_TRONCO` no seletor de tronco do cadastro (addendum 2026-06-22_004) — se `PARTE_PARA_TRONCO` for removido, esse seletor precisa ser ajustado para o tronco-região-real (ou o seletor de tronco do cadastro deixa de ser necessário, já que o tronco vira hotspot direto). CA12 cobre os testes; o dev deve garantir zero import órfão.
- **Risco — colisão com a família 001 na mesma branch:** este briefing e os 3 arquivos da família 001 vivem na **mesma branch** e tocam arquivos comuns (`BodyMap.vue`, `regioesCircunferenciais.ts`, `RegionSelectorPopup.vue`, `RegioesGlobaisFormView.vue`). Ordem recomendada: implementar a família 001 (cache, prefixo, geral não-tronco) e **depois** esta fusão, que simplifica/remove a camada de tronco — assim a fusão remove código que o 001 não precisa manter. O dev deve reconciliar (a fusão **supera** a parte de tronco do addendum-002).
- **Dependência — inspeção de dado já feita:** o `imedto-database` confirmou zero dado clínico. Antes de remover, **reconfirmar** rapidamente (defensivo) que segue 0 achados em `exame_fisico_regioes` para os 9 códigos — se algo tiver sido criado no intervalo, escalar (mas é pré-uso clínico, improvável).
- **Reversão de briefing anterior:** documentar que reverte parte do `2026-06-23_001` (separação das partes) e supera a parte de tronco do `2026-06-25_001_addendum-002`.

---

## 9. Observações para execução

**Não-negociável:**
- **CA3/R8** — seed idempotente que não aborta o deploy (o erro mais caro possível: travar o `apply` e derrubar o app).
- **CA2/CA1** — exatamente as 9 removidas e as 3 criadas; nem mais, nem menos.
- **CA6/R3** — circunferencial do tronco simétrico (fim da exceção do abdome).
- **CA11** — fusão toca **só** o tronco; cabeça/pescoço/membros e `cabeca-anterior-olho` intocados.
- **CA12** — zero import órfão / referência morta após remover as constantes de tronco.

**Liberdade técnica:**
- Estratégia exata da migration/seed (DELETE + upsert vs. reescrever INSERTs + migration de limpeza) — do `imedto-database`, desde que CA3 passe.
- Forma de simplificar o front (remover vs. neutralizar as constantes) — do `imedto-developer`, desde que CA4/CA7/CA12 passem.

**Pipeline:** `imedto-database` (migration/seed — primeiro, pois define os códigos reais) → `imedto-developer` (front: remove camada sintética, ajusta circunferencial, atualiza testes). 

**Validação local mínima (recomendada, mesmo se o usuário dispensar o `imedto-qa`):** por ser mudança **estrutural com migration**, recomendo: (1) `dotnet build` + `npm run build`/typecheck verdes; (2) **smoke do exame físico** com o app rodando local — abrir o prontuário, clicar no tronco no mapa, confirmar que abre o popup da região Tronco e registra achado nas 3 vistas; (3) reaplicar a migration 2× no banco local e confirmar idempotência (CA3) + app sobe. O usuário decide o `imedto-qa` com o orquestrador; esta validação local mínima é o piso de segurança para uma migration.

---

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — seção **"Hierarquia de regiões anatômicas"** / **"Vista circunferencial"**: atualizar o modelo do tronco. Substituir a descrição das partes separadas (Tórax/Abdome/Pelve) e da **exceção clínica do abdome** (`abdome-circunferencial → lombossacra-posterior`) pelo novo modelo: **o tronco é uma região real por vista** (`tronco-anterior`/`tronco-posterior`/`tronco-circunferencial`), o circunferencial do tronco é **simétrico** (`tronco-anterior` + `tronco-posterior`), e o mapa casa o tronco por **código** (não mais por pseudo-hotspot sintético). Mudança **incremental/cirúrgica** na seção afetada. Registrar que isto **reverte parte do briefing 2026-06-23_001** e **supera a parte de tronco do 2026-06-25_001_addendum-002**.
- **`Docs/DESIGN.md`** — **não precisa** (sem componente/token novo; reusa hotspot e popup existentes). Opcional: 1 linha na seção do BodyMap registrando que o tronco deixou de ser pseudo-hotspot sintético.
- **`Docs/LGPD.md`** — **não precisa** (sem dado clínico afetado, sem PII, sem audit novo).
- **`Docs/COMANDOS.md`** — **não precisa** (fluxo de migration/seed já documentado; nada novo de comando).
