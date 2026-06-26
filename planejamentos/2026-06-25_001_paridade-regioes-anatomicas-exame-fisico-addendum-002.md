# Addendum 002 — Opção "geral" no modo circunferencial do popup de seleção

## Refere-se a: 2026-06-25_001_paridade-regioes-anatomicas-exame-fisico.md
## E ao primeiro addendum: 2026-06-25_001_paridade-regioes-anatomicas-exame-fisico-addendum.md

**ID**: 2026-06-25_001 (addendum-002)
**Status**: Aguardando OK explícito do usuário (decisões de produto pré-confirmadas via orquestrador — ver §0)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P (frontend-only)
**Áreas regressivas tocadas**: exame físico — `RegionSelectorPopup.vue`, passo de sub-regiões, modo circunferencial (incluindo tronco)
**Branch de implementação**: `fix/paridade-regioes-anatomicas-exame-fisico` (mesma do briefing original e do addendum-1, ainda não mergeada)

---

## 0. Nota de governança (ler antes de executar)

Spec gap descoberto no **teste do usuário** sobre o briefing 2026-06-25_001 (não é bug — é uma opção de UX ausente, pré-existente à branch atual). Decisões destravadas pelo orquestrador com o usuário e relatadas ao BA. O ambiente da pipeline sinaliza que relato de orquestrador **não substitui** confirmação direta do usuário. Portanto:

- O conteúdo técnico (estado atual do `RegionSelectorPopup.vue`, computeds existentes, ausência do "geral" no circunferencial) é **factual e verificado no código**.
- As decisões de produto estão registradas na recomendação do BA, que coincide com o relato.
- O **dev só inicia este addendum após OK explícito do usuário**. Decisões batem com a abordagem conservadora (frontend-only, espelha o "geral" do anterior/posterior, sem novo design system, sem backend).

**O briefing original e o addendum-1 NÃO foram editados.** Este addendum é incremental: CAs continuam a partir do último (addendum-1 terminou em CA30 → este começa em **CA31**); regras a partir de R12 → começam em **R13**.

**Fluxo definido pelo orquestrador (relatado):** após OK, `imedto-developer` implementa na mesma branch; **o usuário testa diretamente, SEM `imedto-qa`** (decisão do usuário). Registro aqui que isso é uma exceção ao quality gate padrão — não posso confirmá-la como BA; depende do usuário.

> ### ⚠️ PARTE DO TRONCO — SUPERADA pelo briefing 2026-06-25_002 (fusão estrutural)
> Após a redação deste addendum, o usuário decidiu **fundir o tronco estruturalmente** (Trilha 2): em vez de "um geral por parte" (Tórax/Abdome/Pelve separados), o tronco vira **região real por vista** (`tronco-anterior` ← funde tórax/abdome/pelve anterior; `tronco-posterior` ← funde tórax/lombossacra/pelve posterior; `tronco-circunferencial` ← funde os 3 circunferenciais), e as 9 regiões antigas são removidas do catálogo. Formalizado no **briefing próprio `planejamentos/2026-06-25_002_fusao-estrutural-tronco.md`**.
>
> **Portanto, ficam SUPERADOS por aquele briefing:** **R14**, **CA33**, e a menção a tronco em **R18/CA39**. NÃO implementar a parte do tronco deste addendum — com o tronco virando região real (briefing 002), o "geral" do tronco passa a funcionar como o de qualquer outra região (cabeça/pescoço/membro), sem caso especial.
>
> **Permanece VÁLIDA e implementável neste addendum:** a parte **NÃO-tronco** — opção "(geral)" no circunferencial de **cabeça/pescoço/membros** = registra `<base>-circunferencial` (R13, R15, R16, R17 e os CAs CA31, CA32, CA34–CA38, CA40 na parte não-tronco). Essa parte é independente da fusão e segue valendo.

---

## 1. Por que este addendum existe (o gap)

No `RegionSelectorPopup.vue`, no passo de sub-regiões:
- **Modo anterior/posterior** (bloco "Modo anterior/posterior: lista normal", template `v-else`): existe a opção **"(geral)"** no topo (`rsp-opt--geral`, ~linha 578-611) que registra a **própria região base** (`regiaoAtual`, ex.: `cabeca-anterior`). É o atalho para "examinei a região como um todo, sem detalhar sub-região".
- **Modo circunferencial** (bloco `v-if="vistaEscolhida === 'circunferencial'"`, ~linhas 463-535): renderiza **apenas os filhos** agrupados em Anterior/Posterior. **Não tem o "(geral)"**. Causa técnica: no circunferencial, `regiaoAtual` é `null` por construção (computed na ~linha 142: `if (vistaEscolhida.value === 'circunferencial') return null`), então o `v-if` do "geral" (que depende de `regiaoAtual`) nunca renderiza.

Comportamento **pré-existente** (não introduzido pela branch atual). O usuário quer que o circunferencial também ofereça o "geral", registrando o nó **`<base>-circunferencial`** correspondente — espelhando a semântica do "geral" do anterior/posterior.

Fatos relevantes já no componente (reuso > duplicação):
- O id circunferencial **simples** (cabeça/pescoço/membro) já é derivado e **guardado contra inexistência**: `idCircunferencial` (membro, ~linha 99) e `idCircunferencialNaoMembro` (não-membro, ~linha 124) — ambos só retornam id se a chave existir em `RAMOS_CIRCUNFERENCIAL` (catálogo); senão `null`.
- O **tronco** circunferencial usa `props.troncoGrupos`; o id circunferencial **por parte** já é derivado dentro do computed `filhosCircunferencial` (~linhas 188-191: `base = grupo.regiaoBaseId.replace(...); idCircParte = '${base}-circunferencial'; ramos = RAMOS_CIRCUNFERENCIAL[idCircParte]`).
- `confirmar()` (~linha 307) já emite `{ regiaoId, lateralidade, vista }` com `vista = vistaEscolhida.value` (= `'circunferencial'`). Selecionar o nó `<base>-circunferencial` via `toggleRegiao` + `idsSelecionados` e confirmar **já produz o emit correto** — não precisa de lógica de emit nova.

---

## 2. Escopo do addendum

**Inclui:**
- Opção **"(geral)"** no modo circunferencial do passo de sub-regiões, em duas configurações:
  - **Regiões simples não-tronco** (cabeça, pescoço, membros): **uma** opção "(geral)" = `<base>-circunferencial` (id já derivado por `idCircunferencial`/`idCircunferencialNaoMembro`). **← parte VÁLIDA e implementável.**
  - **Tronco** (modo circunferencial agrupado via `troncoGrupos`): ~~**um "(geral)" por parte** — Tórax/Abdome/Pelve~~ **⚠️ SUSPENSO (ver §0)** — o usuário reverteu para **fundir** o tronco; regra de tronco será redefinida em addendum-003. NÃO implementar a parte do tronco agora.
- Seleção do "geral" + Confirmar → emite o achado na **região circunferencial** (`<base>-circunferencial`), com `vista = 'circunferencial'`, respeitando lateralidade **exatamente como os filhos já fazem hoje** no modo circunferencial (R16).
- Estado "já selecionado / disabled" do "geral" quando o nó já está em `regioesJaSelecionadas` (espelha os filhos).
- No-op silencioso quando o nó `<base>-circunferencial` não existe/está inativo (espelha o guard atual de `idCircunferencial`).

**Não inclui (anti-scope-creep):**
- Mudança no "geral" do **anterior/posterior** (permanece idêntico — R18).
- Mudança na lista de **filhos** circunferenciais (continua agrupada Anterior/Posterior — R18).
- Novo **design system / classes novas**: reusa `rsp-opt`, `rsp-opt--geral`, `rsp-opt-box`, `rsp-badge-sel`, `rsp-divider`, `rsp-sub-head` já existentes no popup.
- **Backend / contrato / catálogo**: nenhum. O nó `<base>-circunferencial` já vem do catálogo (ativo) e `RAMOS_CIRCUNFERENCIAL` já mapeia os ramos. Zero migration, zero endpoint.
- Lateralidade nova para o "geral" do tronco/cabeça/pescoço além do que os filhos circunferenciais já oferecem hoje (ver R16 — espelho exato, sem inventar).

---

## 3. Regras de negócio (continuam do addendum-1 — começam em R13)

- **R13 (geral circunferencial — região simples não-tronco):** no modo circunferencial de **cabeça/pescoço/membro**, o passo de sub-regiões exibe, no topo, uma opção "(geral)" cujo `regiaoId` é o **`<base>-circunferencial`** já derivado por `idCircunferencial` (membro) ou `idCircunferencialNaoMembro` (não-membro). O rótulo segue o padrão do geral existente ("<nome da região> (geral)" ou, para membro, `dialogTitle` + "(geral)"). Mora em: Front (`RegionSelectorPopup.vue`). Validada em: front.
- **R14 (geral circunferencial — tronco, um por parte) — ⚠️ SUSPENSA (ver pendência no §0):** ~~no modo circunferencial do **tronco** (`troncoGrupos` presente), cada grupo de parte (Tórax / Abdome / Pelve) exibe, no topo do grupo, sua própria opção "(geral)" cujo `regiaoId` é o `<parte>-circunferencial`~~. **Decisão revertida pelo usuário** (fundir o tronco em vez de separar por parte). NÃO implementar esta regra; aguardar a decisão final de fusão (provável addendum-003).
- **R15 (existência — guard, no-op):** uma opção "(geral)" só é renderizada se o nó `<base>-circunferencial` correspondente **existir** (chave presente em `RAMOS_CIRCUNFERENCIAL`, refletindo o catálogo ativo). Se não existir/estiver inativo, o "geral" daquele caso **não aparece**, sem erro — espelha o guard que `idCircunferencial`/`idCircunferencialNaoMembro` já aplicam (retornam `null`). Mora em: Front. Validada em: front.
- **R16 (emit e lateralidade — espelho do comportamento circunferencial atual):** selecionar o "(geral)" usa o **mesmo** `toggleRegiao`/`idsSelecionados` dos filhos; ao Confirmar, `confirmar()` emite `{ regiaoId: '<base>-circunferencial', lateralidade, vista: 'circunferencial' }`. A lateralidade segue **exatamente** o que o modo circunferencial já faz hoje: para **membro**, vem do `ladoEscolhido` do passo 1 (D/E/bilateral aplicado a todos os selecionados em `confirmar()`); para **não-membro/tronco**, segue o mesmo tratamento dos filhos circunferenciais atuais (o bloco circunferencial atual não renderiza botões D/E por sub-região — o "geral" não introduz botões D/E novos). Mora em: Front. Validada em: front. **Não inventar** fluxo de lateralidade que os filhos circunferenciais não tenham hoje.
- **R17 (já selecionado / disabled):** se o `<base>-circunferencial` do "geral" já está em `props.regioesJaSelecionadas`, a opção é exibida como já selecionada/desabilitada (check preenchido, badge "Selecionado", sem checkbox interativo) — espelha o tratamento `jaFoiSelecionada` dos filhos. Mora em: Front. Validada em: front.
- **R18 (não-regressão):** o "(geral)" do **anterior/posterior**, a lista de **filhos** circunferenciais (grupos Anterior/Posterior), o modo tronco não-circunferencial e os passos de lado/vista permanecem **inalterados**. O "geral" circunferencial (parte **não-tronco**) é uma adição no topo do bloco circunferencial, sem alterar o que já existe. Mora em: Front. Validada em: front. (A adição do "geral" no **tronco circunferencial** está SUSPENSA — ver §0; até a decisão de fusão, o tronco circunferencial permanece como hoje, só com os filhos.)

---

## 4. Modelo de dados

**Nenhuma mudança.** Frontend-only. Os nós `<base>-circunferencial` já existem no catálogo global (ativos, vêm do backend) e `RAMOS_CIRCUNFERENCIAL` já mapeia ramos anterior/posterior de cada base. Zero migration, zero alteração de contrato de API, zero mudança no catálogo.

**Multi-tenant / LGPD / RBAC:** N/A novo — o popup roda no exame físico do prontuário (já gated pelo fluxo existente); nenhuma PII nova, nenhum endpoint, nenhuma permissão nova. O catálogo lido continua global.

---

## 5. UX e fluxo

Passo de sub-regiões, **modo circunferencial**:

**Região simples não-tronco (ex.: Cabeça, plano Circunferencial):**
```
[chips: Circunferencial]
───────────────────────────────
☐ Cabeça (geral)               ← NOVO (R13): registra cabeca-circunferencial
───────────────────────────────
ANTERIOR
  ☐ <filho anterior>
  ...
POSTERIOR
  ☐ <filho posterior>
  ...
```

**Tronco (plano Circunferencial) — ⚠️ SUSPENSO (ver §0):** o desenho abaixo descrevia a regra "um geral por parte", que foi **revertida** (usuário quer fundir o tronco). NÃO implementar; aguardar addendum-003. O diagrama fica como registro histórico da decisão anterior:
```
(SUSPENSO — desenho da regra revertida "um geral por parte")
TÓRAX  → Tórax (geral) = torax-circunferencial
ABDOME → Abdome (geral) = abdome-circunferencial
PELVE  → Pelve (geral) = pelve-circunferencial
```
Até a decisão de fusão fechar, o **tronco circunferencial permanece exatamente como hoje** (só os filhos Anterior/Posterior, sem "geral").

- **Componente/estilo:** reusa as classes existentes do popup (`rsp-opt`, `rsp-opt--geral`, `rsp-opt-box`, `rsp-badge-sel`, `rsp-divider`). Sem design system novo.
- **Estados:** *já selecionado* (badge "Selecionado", disabled — R17); *inexistente* (não renderiza — R15); *seleção ativa* (check preenchido).
- **Confirmar:** habilitado quando há ≥1 selecionado (regra existente do rodapé). O "geral" conta como seleção.

---

## 6. Critérios de aceite (continuam do addendum-1 — começam em CA31)

- **CA31 (geral circunferencial — cabeça/pescoço):** Dado o popup no plano **Circunferencial** de uma região simples não-tronco (ex.: Cabeça), Quando o passo de sub-regiões renderiza, Então aparece no topo a opção "Cabeça (geral)" cujo `regiaoId` é `cabeca-circunferencial`, acima dos grupos Anterior/Posterior.
- **CA32 (geral circunferencial — membro):** Dado o popup no plano **Circunferencial** de um membro (ex.: Membro superior, lado Direito no passo 1), Quando o passo de sub-regiões renderiza, Então aparece o "(geral)" cujo `regiaoId` é `membro-superior-direito-circunferencial` (derivado de `idCircunferencial`), e ao Confirmar o emit traz `vista: 'circunferencial'` e a lateralidade `D` vinda do passo de lado (R16).
- **CA33 (geral circunferencial — tronco, um por parte) — ⚠️ SUSPENSO (ver pendência no §0):** ~~Dado o popup no plano Circunferencial do tronco, Então cada parte (Tórax, Abdome, Pelve) tem sua própria opção "(geral)"~~. **Decisão revertida** (usuário quer fundir o tronco). Critério suspenso até a decisão final de fusão; será redefinido em addendum-003.
- **CA34 (selecionar geral + confirmar emite na região circunferencial):** Dado o "(geral)" de Cabeça selecionado no plano Circunferencial, Quando o usuário clica em Confirmar, Então o componente emite `confirmar` com um item `{ regiaoId: 'cabeca-circunferencial', lateralidade: <conforme R16>, vista: 'circunferencial' }` e o achado é registrado na vista circunferencial.
- **CA35 (já selecionado → disabled):** Dado que `cabeca-circunferencial` já está em `regioesJaSelecionadas`, Quando o "(geral)" de Cabeça é renderizado no circunferencial, Então ele aparece como já selecionado/desabilitado (check preenchido, badge "Selecionado", sem checkbox interativo) — igual ao tratamento dos filhos.
- **CA36 (nó inexistente/inativo → no-op):** Dado um caso em que o nó `<base>-circunferencial` não existe em `RAMOS_CIRCUNFERENCIAL`/catálogo (ou está inativo), Quando o passo circunferencial renderiza, Então o "(geral)" daquele caso **não é exibido** e **nenhum erro** é lançado (a lista de filhos segue normal).
- **CA37 (lateralidade espelha o comportamento atual):** Dado o "(geral)" circunferencial de uma região **não-membro** (cabeça/pescoço/tronco), Quando é selecionado, Então **não** surgem botões D/E novos que os filhos circunferenciais não tenham hoje — a lateralidade segue exatamente o tratamento já existente no modo circunferencial (R16); para **membro**, a lateralidade vem do lado escolhido no passo 1.
- **CA38 (não-regressão anterior/posterior):** Dado o popup nos planos **Anterior** ou **Posterior**, Quando o passo de sub-regiões renderiza, Então o "(geral)" e a lista de filhos permanecem **idênticos** ao comportamento atual (zero regressão).
- **CA39 (não-regressão dos filhos circunferenciais):** Dado o plano **Circunferencial**, Quando o passo renderiza, Então os grupos de **filhos** Anterior/Posterior continuam exibidos como hoje, com o "(geral)" apenas **adicionado** no topo do bloco para **região simples (não-tronco)** — sem remover nem reordenar os filhos. (Para o **tronco**, enquanto a decisão de fusão não fecha, nada é adicionado — o tronco circunferencial permanece exatamente como hoje; ver §0.)
- **CA40 (documentação):** Dado que a mudança reusa classes existentes e não introduz padrão novo de design system, Quando o PR é aberto, Então **nenhuma** atualização de `Docs/` é exigida (e o CA é dado como N/A justificado). Se o dev, por opção, criar um padrão reutilizável, então documenta em `Docs/DESIGN.md`.

---

## 7. Riscos e dependências

- **Risco — `regiaoAtual` é `null` no circunferencial:** o "geral" do anterior/posterior depende de `regiaoAtual`, que é `null` no circunferencial. O dev **não** deve tentar reaproveitar o mesmo `v-if` do "geral" existente — precisa de um caminho próprio no bloco circunferencial usando `idCircunferencial`/`idCircunferencialNaoMembro` (simples) e a derivação por parte (tronco). CA31/CA33 são os guards.
- **Risco — tronco agrupa por Ant/Post, não por parte:** o bloco circunferencial atual junta filhos por Anterior/Posterior, não por parte. "Um geral por parte" (R14) exige expor as partes no circunferencial do tronco. O dev decide o arranjo (reusando `troncoGrupos`), mas o efeito de CA33 é obrigatório. Atenção para **não** quebrar a lista de filhos atual (CA39).
- **Risco — duplo registro:** garantir que selecionar o "(geral)" (`<base>-circunferencial`) e também filhos não cause confusão — são ids distintos; o backend aceita ambos (são regiões diferentes do catálogo). Não há regra de exclusão mútua pedida; o "geral" e os filhos coexistem como seleções independentes (espelha o anterior/posterior, onde geral + sub-região podem ambos ser marcados).
- **Risco — lateralidade inventada:** R16/CA37 são explícitos: **não** adicionar botões D/E ao "geral" circunferencial de não-membro se os filhos circunferenciais não os têm hoje. Espelho exato.
- **Sem dependência de backend/DB/QA na pipeline:** frontend-only. O fluxo relatado é dev → teste do usuário (sem `imedto-qa`) — exceção ao gate padrão que só o usuário pode autorizar.

---

## 8. Observações para execução

**Não-negociável:**
- **R13/R14/CA31-CA33** — "geral" simples = `<base>-circunferencial`; tronco = um por parte (`torax/abdome/pelve-circunferencial`).
- **R15/CA36** — guard de existência (no-op silencioso), espelhando `idCircunferencial`.
- **R16/CA37** — lateralidade e emit espelham **exatamente** o circunferencial atual; não inventar D/E para não-membro.
- **R18/CA38-CA39** — zero regressão no anterior/posterior e na lista de filhos.

**Liberdade técnica:**
- Computed novo para listar os "gerais" por parte do tronco (`{ label, idCircunferencial }`) reusando `troncoGrupos` + `RAMOS_CIRCUNFERENCIAL` — forma a critério do dev.
- Arranjo visual do tronco circunferencial (manter Ant/Post + inserir geral, ou reagrupar por parte) — desde que CA33/CA39 passem. Sem classe de design system nova.

**Pipeline (relatada pelo orquestrador):** `imedto-developer` (frontend) → **teste direto do usuário, sem `imedto-qa`**. Registro que pular o QA é exceção ao gate padrão; só o usuário pode autorizar. Sem `imedto-database`.

---

## 9. Atualização de documentação

- **`Docs/DESIGN.md`** — **não precisa** (reusa classes/padrões já existentes do popup; sem componente ou token novo). Só atualizar se o dev, por opção própria, extrair um padrão reutilizável (CA40 cobre os dois caminhos).
- **`Docs/ARQUITETURA.md`** — **não precisa** (sem mudança de back/contrato).
- **`Docs/LGPD.md`** — **não precisa** (sem PII/audit novo).
- **Migrations / `Docs/COMANDOS.md`** — **não precisa** (frontend-only, zero migration).
