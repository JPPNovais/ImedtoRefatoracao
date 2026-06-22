# Addendum 2 — "Usar template" de descrição cirúrgica preenche o campo "Observações" da seção estruturada (merge, não sobrescreve)

**ID**: 2026-06-21_001 (addendum 2)
**Refere-se a**:
- `2026-06-21_001_estruturar-secoes-pos-op-e-descricao-cirurgica.md` (briefing original — imutável)
- `2026-06-21_001_estruturar-secoes-pos-op-e-descricao-cirurgica-addendum.md` (1º addendum — imutável)

**Status**: Aprovado por usuário em 2026-06-21
**Autor**: imedto-business-analyst
**Tipo**: Addendum (Modo B — spec gap Tipo B descoberto pelo `imedto-qa` na validação)
**Estimativa de esforço adicional**: P (acréscimo cirúrgico sobre a entrega original)
**Áreas regressivas tocadas**: prontuário (form de evolução — seção `desc-cirurgica` + integração com a feature pré-existente "modelos de descrição cirúrgica"). Sem novo toque em backend de domínio, schema, multi-tenant, RBAC ou audit.

> O briefing original e o 1º addendum permanecem **imutáveis**. Este addendum 2 fecha uma interação entre duas features que nenhum dos dois documentos previu: a estruturação da seção `desc-cirurgica` (entregue nesta linha de trabalho) quebrou o comportamento da feature **pré-existente** "modelos de descrição cirúrgica". Onde houver conflito, **este addendum prevalece** sobre o comportamento atual em produção (ver §4).

---

## 1. Spec gap descoberto (Tipo B)

### 1.1. As duas features que colidem
- **Feature em entrega (esta linha de trabalho):** a seção `desc-cirurgica` deixou de ser um `texto_longo` (string única em `novaEvolucao["desc-cirurgica"]`) e passou a ser um **objeto estruturado** com vários campos (`cirurgiao`, `data`, `cirurgiasRealizadas`, equipe, `cirurgiaInicio`/`cirurgiaFim`, `profilaxia`, `intercorrencia`, `tecnicaOperatoria`, `observacoes`, etc. — ver §5 do briefing original).
- **Feature pré-existente em produção:** "modelos de descrição cirúrgica" (biblioteca título+corpo, gerida em Config → Modelos e listas, publicada 2026-06-13 commit 4143716). No prontuário, o botão **"Usar template"** — exclusivo da seção `desc-cirurgica` — abre o componente `SeletorTemplateCirurgico.vue`, o profissional escolhe um modelo e o **corpo** (string) do modelo é aplicado ao campo da seção.

### 1.2. O comportamento que quebra
O fluxo de aplicação do template, escrito quando `desc-cirurgica` ainda era um campo de texto único, faz uma **substituição total**:

- `frontend/src/components/prontuario/tabs/ConsultaAtualTab.vue:45-46` emite `("aplicar-template", "desc-cirurgica", corpo)`.
- `frontend/src/views/pacientes/ProntuarioView.vue:528` trata o evento com:
  `@aplicar-template="(chave, corpo) => { novaEvolucao[chave] = corpo }"`

Como `chave` é sempre `"desc-cirurgica"` e `corpo` é uma **string**, essa atribuição troca o **objeto estruturado inteiro** por uma string. Resultado: **todos os campos já preenchidos da seção (cirurgião, equipe, data, profilaxia, técnica operatória, etc.) são destruídos** no instante em que o profissional aplica um template. Não é um bug isolado de uma linha — é uma interação de produto que o briefing não previu (o briefing original tratou a seção como ilha, sem considerar a feature de template que já existia apontando para a mesma chave).

### 1.3. Efeito colateral na confirmação de substituição existente
Há ainda um segundo sintoma derivado do mesmo gap. A confirmação de substituição já existente (memória do projeto: "modelos aplicados inline substituem **com confirmação**") vive em `SeletorTemplateCirurgico.vue:61-70` e decide se pergunta com base em `props.valorAtual.trim()`. Esse `valorAtual` vem de `ConsultaAtualTab.vue:246`:
`:valor-atual="novaEvolucao['desc-cirurgica'] ?? ''"`.

Com a seção agora **objeto**, `novaEvolucao['desc-cirurgica']` não é mais uma string: `props.valorAtual` recebe o objeto coagido (`[object Object]`), e `valorAtual.trim()` fica **sempre truthy** — a confirmação dispara de forma incorreta (sempre pergunta, mesmo quando "Observações" está vazio) e avalia o conteúdo errado. A confirmação precisa passar a olhar para o **campo `observacoes`** (ver §3 e CA28).

---

## 2. Decisão do usuário registrada

**Aplicar o corpo do modelo ao campo "Observações" (`observacoes`) da seção estruturada `desc-cirurgica`, via MERGE — preservando todos os demais campos já preenchidos.**

- O corpo (string) do modelo passa a alimentar **somente** o campo `observacoes` do objeto da seção.
- Todos os demais campos (`cirurgiao`, `data`, `cirurgiasRealizadas`, `anestesista`, `auxiliar`, `instrumentador`, `outrosMembros`, `cirurgiaInicio`, `cirurgiaFim`, `profilaxia`, `intercorrencia`, `intercorrenciaDescricao`, `tecnicaOperatoria`) permanecem **intactos**.
- A **confirmação de substituição** que já existe no fluxo continua valendo, agora **avaliada sobre o conteúdo atual de `observacoes`**: se `observacoes` já tem texto, pede confirmação antes de substituir; se está vazio, aplica direto (sem diálogo). Mensagem de confirmação genérica, sem PII (a mensagem atual em `SeletorTemplateCirurgico.vue:158-166` já é genérica e permanece válida).

Justificativa de produto: o template é um corpo de texto livre redigido (relato do ato/observações) — semanticamente equivale ao campo de texto longo "Observações", não aos campos estruturados de identificação/equipe/profilaxia. Aplicá-lo a `observacoes` preserva o trabalho já feito e mantém o valor da biblioteca de modelos.

---

## 3. Escopo e arquitetura da correção

- **Permanece front-only.** Zero backend de domínio, zero command/DTO/query novo, zero migration. O backend continua tratando `ConteudoJson` como string opaca (coerente com R1 do original e §2.2 do 1º addendum). `imedto-database` **não** é acionado.
- **Pontos de mudança (frontend):**
  1. O handler em `ProntuarioView.vue:528` deixa de fazer `novaEvolucao[chave] = corpo` (sobrescrita total) e passa a fazer um **merge** que atribui o corpo apenas a `observacoes`, preservando o restante do objeto da seção (ex.: `novaEvolucao[chave] = { ...(objeto atual da seção, se objeto), observacoes: corpo }`). A forma exata do merge é liberdade técnica do dev (ver §7), desde que respeite os CAs.
  2. O `valor-atual` passado ao `SeletorTemplateCirurgico` (hoje `novaEvolucao['desc-cirurgica'] ?? ''` em `ConsultaAtualTab.vue:246`) passa a refletir o campo `observacoes` da seção (ex.: o texto de `observacoes`, não o objeto coagido), para que a confirmação de substituição volte a decidir corretamente sobre conteúdo real do campo.
- **Retrocompatibilidade (legado string):** se, no momento da aplicação, `novaEvolucao['desc-cirurgica']` ainda for uma **string** (evolução iniciada a partir de conteúdo legado, ou estado intermediário), o merge deve tratar esse caso sem quebrar — promovendo para objeto com `observacoes` recebendo o corpo (não concatenando lixo, não gerando `[object Object]`). Coerente com R11 do original.

---

## 4. Itens afetados do briefing original / 1º addendum / código atual

- **CA2 do briefing original** (caminho feliz — descrição cirúrgica estruturada): **complementado**, não substituído. CA2 garante que a seção grava um objeto com os campos da §5; este addendum garante que aplicar um template **preenche `observacoes`** sem destruir esse objeto. Os CAs novos abaixo são o complemento.
- **Linha do campo `observacoes` na §5 do original** (`observacoes | string | texto longo`): **complementada** — `observacoes` passa a ser também o destino do corpo de um modelo aplicado via "Usar template".
- **CA20–CA22 do 1º addendum** (obrigatoriedade do cirurgião): **não conflitam**. Aplicar um template preenche `observacoes`, nunca `cirurgiao`; a trava do cirurgião continua valendo exatamente como no 1º addendum. CA27 abaixo registra explicitamente que aplicar template **não** preenche nem dispensa o cirurgião.
- **Referências de CA nos comentários do código atual** — **registro de precisão (não é mudança de CA, é alerta de leitura para dev/QA):** os comentários em `SeletorTemplateCirurgico.vue` (linhas 9, 57, 157) e em `ConsultaAtualTab.vue` (linha 34) associam o fluxo de "aplicar template" e a confirmação de substituição a "**CA2/CA3**" (e "CA15/CA16/CA17/R8/R9"). **Esses CA2/CA3/CA15-CA17 NÃO são os deste briefing `2026-06-21_001`** (onde CA2 = descrição cirúrgica estruturada e CA3 = DPO calculado). Eles pertencem ao **briefing da feature "modelos de descrição cirúrgica"** (publicada 2026-06-13, commit 4143716). O dev/QA não deve confundir as numerações: os CAs deste addendum 2 (CA26+) são a fonte de verdade para o comportamento de merge; os comentários antigos no código referem-se ao briefing da feature de modelos e podem permanecer (mudança cirúrgica — não reescrever comentários alheios), desde que o comportamento codificado passe a obedecer CA26–CA29.

Nenhum outro CA do original (CA1, CA3–CA19) nem do 1º addendum (CA23–CA25) é alterado.

---

## 5. Critérios de aceite incrementais (numerados a partir do último — CA25 do 1º addendum)

- **CA26 (merge — aplicar template com `observacoes` vazio):** Dado um profissional registrando uma evolução com a seção `desc-cirurgica`, com **vários campos já preenchidos** (ex.: `cirurgiao` = "Dr. Fulano", `cirurgiasRealizadas` preenchida, `cirurgiaInicio`/`cirurgiaFim` informados, ao menos uma `profilaxia` marcada, `tecnicaOperatoria` preenchida) **e `observacoes` vazio**, Quando ele clica em "Usar template", escolhe um modelo, Então o **corpo do modelo é aplicado ao campo `observacoes`** e **nenhum diálogo de confirmação é exibido** (campo de destino estava vazio), e o objeto `novaEvolucao["desc-cirurgica"]` permanece um **objeto** (não vira string).

- **CA27 (anti-regressão do bug — demais campos preservados):** Dado o mesmo cenário do CA26, Quando o template é aplicado, Então **todos os demais campos da seção continuam exatamente como estavam** — `cirurgiao`, `data`, `cirurgiasRealizadas`, `anestesista`, `auxiliar`, `instrumentador`, `outrosMembros`, `cirurgiaInicio`, `cirurgiaFim`, `profilaxia` (todas as flags/textos), `intercorrencia`, `intercorrenciaDescricao` e `tecnicaOperatoria` — **nenhum deles é apagado, sobrescrito ou convertido**. (Este é o CA que reproduz e fecha o bug Tipo B: antes, aplicar o template zerava todos esses campos.)

- **CA28 (confirmação — aplicar template com `observacoes` já preenchido):** Dado a seção `desc-cirurgica` com o campo `observacoes` **já contendo texto**, Quando o profissional escolhe um modelo no "Usar template", Então é exibido o diálogo de confirmação genérico ("Substituir o conteúdo atual da descrição cirúrgica pelo template selecionado?"), e: (a) ao **confirmar**, `observacoes` recebe o corpo do modelo (substitui apenas `observacoes`) e os demais campos permanecem intactos (CA27); (b) ao **cancelar**, **nada muda** — `observacoes` mantém o texto anterior e os demais campos permanecem intactos. A decisão de pedir confirmação avalia **`observacoes`**, não o objeto inteiro (sem `[object Object]`).

- **CA29 (retrocompatibilidade do estado — aplicar sobre valor string legado):** Dado uma evolução cujo `novaEvolucao["desc-cirurgica"]` ainda é uma **string** (estado legado/intermediário, não objeto), Quando o profissional aplica um template, Então o resultado é um **objeto** com `observacoes` recebendo o corpo do modelo (promoção segura), sem gerar `[object Object]` nem texto corrompido, e sem lançar erro.

- **CA30 (aplicar template não toca o cirurgião — coerência com o 1º addendum):** Dado a seção `desc-cirurgica` com `cirurgiao` **vazio**, Quando o profissional aplica um template (preenchendo `observacoes`), Então o campo `cirurgiao` **continua vazio** e a trava de obrigatoriedade do cirurgião (CA20–CA22 do 1º addendum) **permanece ativa** ao tentar salvar — ou seja, aplicar um template **não preenche nem dispensa** o cirurgião.

- **CA31 (front-only — sem espelho no backend; regressão arquitetural):** Dado a entrega deste addendum, Quando o QA inspeciona o diff, Então a mudança está **apenas no front** (`ProntuarioView.vue` e a propagação de `valor-atual`/lógica de confirmação em `ConsultaAtualTab.vue`/`SeletorTemplateCirurgico.vue`); **nenhuma** alteração em `RegistrarEvolucaoCommand`, `RegistrarEvolucaoCommandHandler`, `ProntuarioEvolucao` ou em qualquer validação de backend; zero migration; o backend continua tratando `ConteudoJson` como string opaca.

- **CA32 (build + typecheck + suíte):** Dado a entrega, Quando roda lint/typecheck/build do frontend e a suíte Vitest (incluindo os testes existentes `SeletorTemplateCirurgico.test.ts` e `ProntuarioView.test.ts`, que cobrem o fluxo de aplicação de template), Então tudo passa; os testes que assumiam o comportamento antigo de sobrescrita total devem ser atualizados para refletir o merge em `observacoes` (CA26–CA29).

- **CA33 (tipografia — gate, herdado):** Dado quaisquer ajustes de UI desta entrega, Quando roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight` (CLAUDE.md §5). (Provavelmente sem mudança de CSS; CA registrado por completude.)

---

## 6. Riscos e dependências

- **Risco — testes existentes do template assumem sobrescrita total:** `SeletorTemplateCirurgico.test.ts` e `ProntuarioView.test.ts` podem ter asserts que esperam `novaEvolucao["desc-cirurgica"] === corpo`. Devem ser atualizados para o novo contrato (`observacoes` recebe o corpo, demais campos preservados). Coberto por CA32.
- **Risco — `valorAtual` ainda apontando para o objeto:** se o `valor-atual` passado ao seletor não for ajustado para refletir `observacoes`, a confirmação continuará disparando errado (sempre, por causa do `[object Object]`). Mitigação: §3 ponto 2 + CA28.
- **Sem dependência de DB.** `imedto-database` não é acionado. Pipeline: `imedto-developer` → `imedto-qa`.
- **Sem regressão de multi-tenant/RBAC/audit:** é front + integração de duas features de front; o registro/leitura de evolução não muda. CA31 confirma ausência de toque no backend.

---

## 7. Observações para execução (acréscimo ao §9 do original e §6 do 1º addendum)

**Não-negociável:**
- Aplicar o corpo do modelo **somente** ao campo `observacoes` da seção `desc-cirurgica`, via **merge** que preserva todos os demais campos (CA26/CA27). **Nunca** sobrescrever o objeto inteiro com a string (esse é exatamente o bug que este addendum corrige).
- A confirmação de substituição deve avaliar **`observacoes`** (não o objeto coagido). Vazio → aplica direto; com texto → confirma (CA28).
- Permanece **front-only**: não desserializar/validar shape no backend; não criar command/DTO/query/migration (CA31).
- Tratar o caso de `desc-cirurgica` ainda ser **string** (legado/intermediário) promovendo para objeto sem corromper (CA29).
- Aplicar template **não** preenche `cirurgiao`; a trava do 1º addendum (CA20–CA22) continua valendo (CA30).

**Liberdade técnica do dev:**
- Forma exata do merge (spread `{ ...atual, observacoes: corpo }` com guarda de tipo para string legada; helper; ou ajuste no handler do evento) — desde que respeite CA26–CA30.
- Onde derivar o `valor-atual` para a confirmação (no `ConsultaAtualTab` ao montar o `:valor-atual`, ou via prop/computed) — reuso > duplicação; a confirmação e o diálogo já existentes em `SeletorTemplateCirurgico.vue` devem ser reaproveitados (não criar diálogo novo).

**Referências de código (pontos de partida já confirmados):**
- Sobrescrita a corrigir: `frontend/src/views/pacientes/ProntuarioView.vue:528` (`@aplicar-template="(chave, corpo) => { novaEvolucao[chave] = corpo }"`).
- Emissão do evento e botão "Usar template": `frontend/src/components/prontuario/tabs/ConsultaAtualTab.vue:34-46, 191-199, 243-247`.
- Confirmação de substituição + `valorAtual`: `frontend/src/components/prontuario/SeletorTemplateCirurgico.vue:23-89, 157-166` (consome `valor-atual` de `ConsultaAtualTab.vue:246`).
- Shape do objeto da seção (campo `observacoes`): §5 do briefing original.

**Pipeline:** sem `imedto-database` (zero schema). Fluxo: `imedto-developer` → `imedto-qa`. Entra na mesma branch de feature do briefing original + 1º addendum.

---

## 8. Atualização de documentação

- **Sem mudança em `Docs/`.** Esta correção é uma integração entre duas features de front existentes (não introduz padrão de arquitetura, componente novo de design system, novo dado/PII, novo endpoint nem novo audit). O §10 do briefing original e o §5 do 1º addendum continuam sendo a referência de doc da entrega (registro dos componentes de seção em `Docs/DESIGN.md`). `Docs/ARQUITETURA.md`, `Docs/LGPD.md`, `Docs/INFRA.md`, `Docs/COMANDOS.md` — sem mudança.
