# Addendum — Cirurgião obrigatório bloqueia o salvar + confirmação do toggle padrão

**ID**: 2026-06-21_001 (addendum)
**Refere-se a**: 2026-06-21_001_estruturar-secoes-pos-op-e-descricao-cirurgica.md
**Status**: Aprovado por usuário em 2026-06-21
**Autor**: imedto-business-analyst
**Tipo**: Addendum (Modo B — fechamento das ambiguidades remanescentes do briefing original)
**Estimativa de esforço adicional**: P (acréscimo sobre o briefing original)
**Áreas regressivas tocadas**: prontuário (form de evolução). Sem novo toque em backend de domínio, schema, multi-tenant, RBAC ou audit.

> O briefing original permanece **imutável**. Este addendum apenas fecha as duas "Questões em aberto para o usuário" que o original deixou em §"Questões em aberto" (itens 1 e 2), com as decisões que o usuário tomou. Onde houver conflito, **este addendum prevalece sobre o default que o original havia adotado** (ver §"CAs do original alterados/substituídos").

---

## 1. Decisões do usuário registradas

### Decisão 1 — "Cirurgião" obrigatório PASSA A BLOQUEAR o salvar da evolução
O briefing original (Questão em aberto 1) havia adotado o default: *"obrigatório no UX (validação visual com `*`), mas NÃO bloqueia o salvar"*. O usuário decidiu o **oposto**: na seção `desc-cirurgica`, o campo **Cirurgião** é obrigatório e **impede registrar a evolução** se estiver vazio.

Esta decisão **SUBSTITUI** o default do original (R-original do campo `cirurgiao` na seção 5 — "não bloqueia o salvar"; e a Questão em aberto 1). Ver §3 (CAs substituídos).

### Decisão 2 — Toggles de estado usam o componente padrão atual (CONFIRMAÇÃO, sem mudança de escopo)
O briefing original (Questão em aberto 2) havia adotado o default: usar os componentes de toggle/segmented já existentes no design system (`AppPillToggle` quando atender, caindo em `AppSelect`/grupo de `AppButton` caso contrário), **sem** criar variante de cor semântica nova (verde/amarelo/vermelho por estado, como no toggle colorido do legado).

O usuário **CONFIRMA esse default**: usar o componente padrão atual, **sem** variante de cor semântica nova no design system. **Não há mudança de escopo.** Os CAs do original sobre toggles permanecem válidos. Nenhum novo componente nem token de cor é introduzido — `Docs/DESIGN.md` não ganha entrada de variante semântica por causa disso (a entrada dos dois componentes de seção, já prevista no §10 do original, permanece a única atualização de doc).

---

## 2. Resolução da tensão arquitetural (decisão 1) — front-only vs front+back

### 2.1. Estado real apurado no backend (investigação desta entrega)
- `RegistrarEvolucaoCommand` (`Imedto.Backend.Contracts/Prontuarios/Commands/RegistrarEvolucaoCommand.cs`) carrega `ConteudoJson` como **`string` opaca**.
- `RegistrarEvolucaoCommandHandler` (`Imedto.Backend.Application/Prontuarios/Commands/RegistrarEvolucaoCommandHandler.cs`) passa `command.ConteudoJson` direto para `ProntuarioEvolucao.Registrar(...)`. **Nunca desserializa, nunca inspeciona chave de seção, nunca conhece "cirurgião".** As únicas regras de domínio são paciente existe/não-deletado, prontuário existe, modelo existe/ativo e multi-tenant em 3 níveis (linhas 44–59).
- `ProntuarioEvolucao.Registrar(...)` (`Imedto.Backend.Domain/Prontuarios/ProntuarioEvolucao.cs:29-45`) valida apenas que `conteudoJson` **não é vazio/em-branco** (`string.IsNullOrWhiteSpace`). Não há validação de shape de **nenhuma** das 7 seções estruturadas existentes.

Conclusão factual: o backend é **deliberadamente agnóstico ao shape das seções**. O conteúdo é JSONB cru.

### 2.2. A regra é trava de UX, não invariante de domínio — logo **front-only**

**Decisão: a obrigatoriedade do cirurgião fica como validação de formulário no FRONT apenas. A entrega permanece frontend-only.**

Justificativa (coerência com a premissa "regra de negócio sempre no backend, com espelho no back" do CLAUDE.md):
- A premissa exige espelho no backend para **invariantes de negócio** (multi-tenant, RBAC, integridade referencial, regra de cobrança, etc.). "Cirurgião preenchido quando a seção de descrição cirúrgica está presente" é uma **regra de completude de formulário** — pertence à mesma natureza de qualquer campo obrigatório de UX, não a uma invariante que protege a integridade do agregado `ProntuarioEvolucao`.
- Espelhar no back exigiria que o domínio/handler **desserializasse o JSON, conhecesse a chave `desc-cirurgica` e o campo `cirurgiao`**, lançando `BusinessException` (422). Isso quebraria a premissa de seção agnóstica que sustenta TODA a entrega original (R1 do briefing: "estrutura é frontend; persistência é JSONB cru") e abriria precedente para validar o shape de todas as seções no backend — exatamente o oposto do design atual. Seria uma mudança de arquitetura, não um espelho de regra.
- Nenhuma das 7 seções estruturadas existentes valida shape no back; a 8ª/9ª não devem ser exceção. Manter a consistência arquitetural é o caminho correto.

**Impacto no escopo:** permanece **frontend-only**. Zero backend de domínio, zero command/DTO novo, zero migration. Executor: `imedto-developer` (frontend). `imedto-database` continua **não** sendo acionado.

> Observação para o QA: por ser trava de UX, o backend **continua aceitando** uma evolução sem `cirurgiao` se a request for forjada (ex.: chamada direta à API sem passar pelo front). Isso é **aceito por design** — é o mesmo nível de garantia das demais regras de completude de seção, e não há risco de integridade de dados (o JSONB cru permanece válido). A trava existe na UX do formulário de registro.

### 2.3. Semântica do gatilho (não-negociável)

A trava só pode disparar quando a seção `desc-cirurgica` está **presente e em edição** na evolução sendo registrada. Definições:

- **Seção presente:** o modelo ativo da evolução inclui a chave `desc-cirurgica` e, portanto, o componente `SecaoDescricaoCirurgica` está renderizado no formulário de registro.
- **Gatilho da trava:** a evolução só é bloqueada ao salvar **se** a seção `desc-cirurgica` está presente **E** foi tocada/preenchida em qualquer campo (ou seja, o usuário começou a registrar a descrição cirúrgica) **E** o campo `cirurgiao` está vazio (string vazia ou só espaços).
- **Não bloqueia:**
  - Evolução cujo modelo **não inclui** a seção `desc-cirurgica` (uma evolução comum nunca é travada por causa de uma seção que ela não tem).
  - Evolução com a seção `desc-cirurgica` presente mas **inteiramente vazia/intocada** (o profissional não está registrando descrição cirúrgica nesta evolução; não faz sentido travar por um campo de uma seção que ele deliberadamente deixou em branco). Consistente com a regra do original de que seção vazia é omitida (CA11 do original).

> Resumo da condição: **bloqueia ⇔ (seção `desc-cirurgica` presente) ∧ (seção tem ao menos um campo preenchido) ∧ (`cirurgiao` vazio)**. Caso contrário, salva normalmente.

**Feedback de UX ao bloquear:** o campo Cirurgião recebe estado de erro inline (mensagem genérica, ex.: "Informe o cirurgião para registrar a descrição cirúrgica."), o foco vai para o campo, e o salvar não é submetido. Sem PII na mensagem. Tipografia por token (CLAUDE.md §5).

---

## 3. CAs do briefing original alterados/substituídos

- **Questão em aberto 1 do original** (§"Questões em aberto para o usuário", item 1) — **RESOLVIDA e INVERTIDA**: o default "obrigatório no UX mas NÃO bloqueia o salvar" é **substituído** por "obrigatório E bloqueia o salvar" (CA20–CA22 deste addendum).
- **Linha do campo `cirurgiao` na §5 do original** ("obrigatório no UX (label com `*`); **não bloqueia o salvar da evolução**") — a cláusula "não bloqueia o salvar" fica **substituída** por "bloqueia o salvar quando a seção está preenchida e o campo está vazio" (ver §2.3). O restante da definição do campo (string, label com `*`) permanece.
- **CA2 do original** (caminho feliz — descrição cirúrgica) — permanece válido, mas passa a **pressupor `cirurgiao` preenchido** (o caminho feliz já preenche o cirurgião; CA2 não muda de redação, mas o salvar agora depende do cirurgião — formalizado pelo CA20). Não é substituído; é complementado pelos CAs abaixo.
- **Questão em aberto 2 do original** (§"Questões em aberto", item 2) — **CONFIRMADA** (sem inversão). CA6/CA8 do original sobre toggles permanecem inalterados; nenhuma variante de cor semântica é adicionada (CA23 apenas registra a confirmação como CA de regressão de não-introdução).

Nenhum outro CA do original é alterado. CA1, CA3–CA19 permanecem como estão.

---

## 4. Critérios de aceite incrementais (numerados a partir do último do original — CA19)

- **CA20 (bloqueio do salvar — caminho de falha):** Dado um profissional registrando uma evolução cujo modelo inclui a seção `desc-cirurgica`, e ele preencheu ao menos um campo dessa seção (ex.: cirurgia realizada, início/fim, ou técnica), **mas deixou o campo Cirurgião vazio**, Quando ele aciona "Salvar consulta", Então o salvar **não é submetido**, o campo Cirurgião exibe estado de erro inline com mensagem genérica (sem PII), o foco vai para o campo Cirurgião, e **nenhuma** request `POST` de registro de evolução é disparada.

- **CA21 (caminho feliz com cirurgião — desbloqueio):** Dado o mesmo cenário do CA20, Quando o profissional preenche o campo Cirurgião e aciona "Salvar consulta", Então a evolução é registrada normalmente (objeto `desc-cirurgica` gravado conforme §5 do original, incluindo `cirurgiao`), e o estado de erro inline desaparece.

- **CA22 (gatilho — não bloqueia fora da condição):**
  - **CA22.a — seção ausente:** Dado uma evolução cujo modelo **NÃO inclui** a seção `desc-cirurgica`, Quando o profissional preenche e salva, Então o salvar ocorre normalmente e **nenhuma** validação de cirurgião é disparada (a trava não existe para esse modelo).
  - **CA22.b — seção presente mas intocada/vazia:** Dado uma evolução cujo modelo inclui `desc-cirurgica`, mas o profissional **não preencheu nenhum campo** dessa seção (deixou toda a descrição cirúrgica em branco), Quando ele salva preenchendo outras seções, Então o salvar ocorre normalmente, a seção `desc-cirurgica` é omitida (consistente com CA11 do original) e **o cirurgião vazio NÃO bloqueia**.

- **CA23 (toggle padrão — sem variante de cor nova; regressão de não-introdução):** Dado os toggles de estado das duas seções (Ótima/Boa/Regular/Ruim na pós-op; Sim/Não; Sem/Com), Quando renderizam, Então usam o componente padrão atual do design system (`AppPillToggle` ou `AppSelect`/grupo de `AppButton`, conforme o original), **sem** nenhuma variante de cor semântica nova (verde/amarelo/vermelho por estado) e **sem** novo token de cor; `Docs/DESIGN.md` não ganha entrada de variante semântica por causa dos toggles.

- **CA24 (front-only — sem espelho no backend; regressão arquitetural):** Dado a entrega, Quando o QA inspeciona o diff, Então **nenhuma** alteração foi feita em `RegistrarEvolucaoCommand`, `RegistrarEvolucaoCommandHandler`, `ProntuarioEvolucao` (domínio) nem em qualquer validação de backend; a obrigatoriedade do cirurgião vive **somente** no front; o backend continua tratando `ConteudoJson` como string opaca (a trava é de UX, conforme §2.2). Zero migration.

- **CA25 (tipografia — gate, herdado):** Dado o estado de erro inline novo do campo Cirurgião, Quando roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight` (CLAUDE.md §5). (Reforço do CA18 do original aplicado ao novo elemento de UI.)

---

## 5. Atualização de documentação

- Sem mudança adicional em relação ao §10 do briefing original. A obrigatoriedade do cirurgião é regra de UX de formulário (não padrão de arquitetura nem componente novo do design system), portanto **não** gera entrada em `Docs/ARQUITETURA.md` nem em `Docs/DESIGN.md`.
- A única atualização de doc da entrega continua sendo a já prevista no §10 do original: registrar `SecaoEvolucaoPosOperatoria` e `SecaoDescricaoCirurgica` em `Docs/DESIGN.md` (e `AppTimeInput` **se** o dev criar componente de hora novo).
- `Docs/LGPD.md` — sem mudança (a mensagem de erro do bloqueio é genérica, sem PII; nenhum novo dado/endpoint/audit).

---

## 6. Observações para execução (acréscimo ao §9 do original)

**Não-negociável (acréscimo):**
- A trava de cirurgião é **front-only**. **NÃO** desserializar `ConteudoJson` no backend, **NÃO** validar shape de seção no domínio/handler, **NÃO** introduzir `BusinessException` por cirurgião ausente. Isso preservaria a premissa de seção agnóstica (R1 do original) — ver §2.2 e CA24.
- O gatilho da trava segue exatamente a condição de §2.3: bloqueia ⇔ seção presente ∧ ao menos um campo da seção preenchido ∧ `cirurgiao` vazio. Não bloquear evolução sem a seção, nem evolução com a seção totalmente intocada (CA22).
- Toggles permanecem com o componente padrão atual; **não** criar variante de cor semântica nem token novo (CA23).

**Liberdade técnica do dev:**
- A forma de detectar "seção tocada/preenchida" (flag de toque, comparação com objeto vazio canônico, ou presença de qualquer valor não-vazio no objeto da seção) é decisão do dev — desde que respeite CA20–CA22.
- A apresentação do erro inline (componente de erro do `AppField`/DS já existente) — reuso > componente novo.

**Pipeline:** sem `imedto-database` (zero schema). Fluxo: `imedto-developer` → `imedto-qa`. O addendum entra junto com o briefing original na mesma branch de feature.
