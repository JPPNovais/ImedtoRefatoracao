# Addendum — Prefixo automático e travado no Código da nova região

## Refere-se a: 2026-06-25_001_paridade-regioes-anatomicas-exame-fisico.md

**ID**: 2026-06-25_001 (addendum)
**Status**: Aguardando OK explícito do usuário (decisões de produto pré-confirmadas via orquestrador — ver §0)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P (frontend-only)
**Áreas regressivas tocadas**: cadastro de região anatômica (admin global) — campo "Código", modo criação
**Branch de implementação**: `fix/paridade-regioes-anatomicas-exame-fisico` (mesma do briefing original, ainda não mergeada)

---

## 0. Nota de governança (ler antes de executar)

Spec gap descoberto no **teste do usuário** sobre o briefing 2026-06-25_001 (não é bug de código — é regra de produto que faltava no cadastro de região). As decisões abaixo foram destravadas pelo orquestrador com o usuário e relatadas ao BA. O ambiente da pipeline sinaliza que relato de orquestrador **não substitui** confirmação direta do usuário. Portanto:

- O conteúdo técnico (estado atual do form, ausência de componente de prefixo no DS, numeração de CAs/regras) é **factual e verificado no código**.
- As decisões de produto estão registradas na recomendação do BA, que coincide com o relato.
- O **dev só inicia este addendum após OK explícito do usuário**. Decisões batem com a abordagem conservadora (frontend-only, sem contrato de API novo, sem migration).

**O briefing original NÃO foi editado.** Este addendum é incremental: CAs continuam a partir do último do original (CA19 → começa em CA20); regras a partir de R6 → começa em R7.

---

## 1. Por que este addendum existe (o gap)

No briefing original, o campo **"Código"** da nova região é texto livre (`codigo = ref("")`, validado só por obrigatoriedade + ≤60 chars + imutável após criação). No teste, o usuário percebeu que ao criar uma sub-região o código não herda nenhuma estrutura do pai — o que quebra o **padrão de nomenclatura hierárquica** do catálogo (ex.: hoje os seeds usam `cabeca-anterior`, e sub-regiões deveriam ser `cabeca-anterior-<algo>`). Sem ajuda do form, o admin precisa digitar manualmente o prefixo do pai toda vez, sujeito a erro de digitação e a divergência de padrão.

**Regra nova:** ao definir um pai, a nova sub-região herda **automaticamente o código do pai como prefixo travado**, e o admin digita só o sufixo.

Fato relevante para o dev: **não existe** componente de input com prefixo/addon no design system (`AppInput`, `AppField`, `AppInputDecimal`, `AppSearchInput` — nenhum suporta prefixo fixo; `AppInput` não expõe slot). Logo, o padrão "prefixo fixo cinza + input do sufixo" é **UI nova** → ver §6 (Docs/DESIGN.md).

---

## 2. Escopo do addendum

**Inclui:**
- Prefixo automático `<paiCodigo>-` no campo "Código" sempre que um pai estiver definido (modo criação).
- Prefixo **travado** (parte fixa não-editável, visual de addon cinza) + input só do **sufixo** ao lado. O valor submetido é `prefixo + sufixo`.
- Trocar o pai → troca o prefixo e **preserva o sufixo** digitado.
- Região raiz (sem pai) → campo "Código" continua **livre** (comportamento atual intacto).
- Funciona em qualquer nível (2 e 3): o prefixo é sempre o **código completo do pai + "-"** (pai nível 2 `cabeca-anterior-olho` → filho `cabeca-anterior-olho-<sufixo>`).
- Validação de comprimento e formato do **código final** (slug ≤60 chars, sem "-" duplicado no separador).

**Não inclui (anti-scope-creep):**
- Mudança no **backend / contrato de API**: a unicidade e a validação de código final continuam onde já estão (`BusinessException` 422); o front só **monta** o código. Nenhum endpoint novo, nenhuma migration.
- Modo **edição**: o código é imutável após criação (regra do original mantida) — o prefixo travado só existe no modo criação. Na edição o código segue somente-leitura.
- Mudança na derivação de **vista/nível** do pai (segue exatamente como no original, R2/R3 do original).
- Auto-slug do sufixo (transformar "Olho Direito" → "olho-direito") — **não pedido**; o sufixo é digitado já no formato de código (ver R10 sobre a validação que cobre isso). Se o usuário quiser auto-slug depois, é outro addendum.

---

## 3. Regras de negócio (continuam do original — começam em R7)

- **R7 (prefixo automático ao definir pai):** sempre que `paiCodigo` for preenchido por **qualquer** das 3 vias existentes — clique no BodyMap/mapa, dropdown "Região pai" (nível 1+2, do briefing original R2), ou digitação no campo "Código do pai" — o campo "Código" passa a exibir o prefixo fixo `<paiCodigo>-` e o admin edita apenas o sufixo. Mora em: Front (`RegioesGlobaisFormView.vue`, modo criação). O valor enviado ao backend em `store.criar({ codigo })` é `paiCodigo + "-" + sufixo`. Validada em: front (montagem) + back (unicidade/forma já existentes).
- **R8 (prefixo travado — proteção real):** o trecho `<paiCodigo>-` é **não-editável** (renderizado como addon/segmento fixo cinza, fora do input de digitação). Não é possível apagar, selecionar-para-apagar nem editar o prefixo pelo teclado — não basta `readonly` cosmético: o caractere de prefixo nunca está no campo editável do sufixo. Mora em: Front. Validada em: front (tentativa de editar o prefixo não altera o código).
- **R9 (trocar pai preserva sufixo):** ao trocar o pai (`paiCodigo` muda de `A` para `B`), o prefixo passa de `A-` para `B-` e o **sufixo digitado é mantido**. Ex.: `cabeca-anterior-olho` + troca de pai p/ `pescoco-anterior` → `pescoco-anterior-olho`. Mora em: Front (o estado do form guarda o **sufixo** separadamente; o código final é derivado de `paiCodigo + "-" + sufixo`). Validada em: front.
- **R10 (validação do código final):** o **código final** (`prefixo + sufixo`) deve continuar um slug válido e **≤60 caracteres** (limite já existente no original). Como o prefixo do pai consome parte dos 60 chars, o **sufixo tem limite efetivo** = `60 - (len(paiCodigo) + 1)`. Se o sufixo estourar, o form bloqueia o submit com mensagem específica no campo (ex.: "Código completo excede 60 caracteres — encurte o sufixo (máx. N caracteres para este pai)."). Não pode haver "--" no ponto de junção (o separador é exatamente um "-"; sufixo não começa nem termina com "-"). O sufixo segue o formato de código já aceito (slug: minúsculas, dígitos e hífen). Mora em: Front (validação espelha a regra do backend, que continua a fonte da verdade da unicidade/forma). Validada em: front (bloqueio + mensagem) + back (422 em caso de violação que escape ao front).
- **R11 (raiz sem prefixo):** quando **não** há pai (`paiCodigo` vazio → nó raiz, nível 1), o campo "Código" não tem prefixo e permanece **texto livre**, idêntico ao comportamento atual do original. Limpar/remover o pai retorna a esse estado. Mora em: Front. Validada em: front.
- **R12 (não-regressão do resto do form):** a derivação de **vista** e **nível** a partir do pai (watcher de `paiCodigo`, R2/R3 do original), o guard de pai circunferencial (R3 do original / R7 legado) e o fallback de texto livre do "Código do pai" **permanecem inalterados**. O prefixo é uma camada de UI por cima — não muda a lógica de derivação. Mora em: Front. Validada em: front.

---

## 4. Modelo de dados

**Nenhuma mudança.** Frontend-only. O backend recebe o mesmo campo `codigo` (string) que já recebia — agora montado pelo front como `prefixo + sufixo`. A coluna `regioes_anatomicas_catalogo.codigo` e a validação de unicidade/forma do `CriarRegiaoAdminCommandHandler` **não mudam**. Zero migration, zero alteração de contrato de API.

**Multi-tenant / LGPD:** N/A novo — catálogo global, sem PII (igual ao original §5). A montagem do código no front não introduz PII nem audit novo.

---

## 5. UX e fluxo

Modo criação do `RegioesGlobaisFormView.vue`, campo "Código":

**Sem pai (raiz):**
```
Código *   [ digite o código livremente            ]   ← texto livre (comportamento atual)
```

**Com pai definido (ex.: pai = `cabeca-anterior`):**
```
Código *   ┌─────────────────┬───────────────────────┐
           │ cabeca-anterior-│ olho                  │   ← prefixo cinza FIXO + input do sufixo
           └─────────────────┴───────────────────────┘
           Código final: cabeca-anterior-olho
           (máx. 60 chars — restam N para o sufixo)
```

- **Prefixo:** segmento cinza, não-editável (addon à esquerda). Atualiza sozinho quando o pai muda.
- **Sufixo:** único campo digitável. O usuário nunca apaga o prefixo.
- **Estados:**
  - *Pai trocado:* prefixo muda, sufixo permanece (R9).
  - *Pai removido/limpo:* volta a campo único livre (R11).
  - *Sufixo vazio:* "Código" obrigatório → submit bloqueado (regra existente; código não pode ser só o prefixo + "-").
  - *Sufixo longo demais:* mensagem de R10, submit bloqueado.
  - *Sufixo com caractere inválido:* normaliza/bloqueia conforme a regra de slug já aplicada ao código.
- **Componente:** não há addon de prefixo no DS hoje. O dev decide entre (a) estender `AppInput` com slot/prop de prefixo (preferível — vira padrão reutilizável no DS) ou (b) compor localmente no form com `AppField`. Recomendação do BA: **(a)**, porque "input com prefixo fixo" é um padrão de UX reutilizável (códigos, slugs, domínios) — e isso aciona a atualização de `Docs/DESIGN.md` (§6). Decisão técnica final é do dev.
- **Edição:** sem prefixo travado — código continua somente-leitura (regra do original).

---

## 6. Critérios de aceite (continuam do original — começam em CA20)

- **CA20 (prefixo aparece ao definir pai — 3 vias):** Dado o form de criação, Quando o admin define o pai por **qualquer** das 3 vias (clique no BodyMap, dropdown "Região pai" nível 1+2, ou digitação em "Código do pai") com pai `cabeca-anterior`, Então o campo "Código" passa a mostrar o prefixo fixo `cabeca-anterior-` e um input só para o sufixo, e o "Código final" exibido reflete `cabeca-anterior-<sufixo>`.
- **CA21 (prefixo travado — não editável):** Dado o pai definido e o prefixo `cabeca-anterior-` exibido, Quando o admin tenta apagar, selecionar ou editar o prefixo (backspace no início, seleção do texto do prefixo, etc.), Então o prefixo **não muda** e permanece intacto — apenas o sufixo é editável; o valor submetido sempre começa por `cabeca-anterior-`.
- **CA22 (trocar pai troca prefixo e preserva sufixo):** Dado o código atual `cabeca-anterior-olho` (sufixo `olho`), Quando o admin troca o pai para `pescoco-anterior`, Então o prefixo vira `pescoco-anterior-`, o sufixo continua `olho` e o código final é `pescoco-anterior-olho`.
- **CA23 (remover pai volta a código livre):** Dado um pai definido com prefixo travado, Quando o admin **limpa/remove** o pai (campo "Código do pai" vazio → vira raiz nível 1), Então o campo "Código" volta a ser um input único de **texto livre** (sem prefixo), preservando o que for digitável como código livre — comportamento idêntico ao do briefing original.
- **CA24 (nível 3 — prefixo é o código completo do pai):** Dado que o admin escolhe um pai de **nível 2** com código `cabeca-anterior-olho`, Quando define o sufixo `palpebra`, Então o prefixo travado é `cabeca-anterior-olho-` e o código final é `cabeca-anterior-olho-palpebra` (nível 3, derivado pelo watcher do original).
- **CA25 (limite de 60 chars no código final, com aviso de sufixo):** Dado um pai cujo código tem `len(paiCodigo)` caracteres, Quando o sufixo digitado faz o código final ultrapassar **60 caracteres**, Então o form bloqueia o submit e mostra mensagem específica no campo indicando o limite efetivo do sufixo (`60 - len(paiCodigo) - 1`); Quando o sufixo cabe, Então o submit é permitido.
- **CA26 (sem separador duplicado):** Dado o pai `cabeca-anterior` e um sufixo que o usuário tente iniciar com "-" (ex.: `-olho`), Quando o código é montado, Então não há "--" no ponto de junção — o código final é `cabeca-anterior-olho` (um único separador), e sufixo vazio ou só "-" é tratado como código incompleto (submit bloqueado).
- **CA27 (não-regressão da derivação vista/nível e do guard circunferencial):** Dado o pai definido, Quando o prefixo é aplicado, Então a **vista** e o **nível** continuam derivados do pai exatamente como no briefing original (campos desabilitados, valores corretos) e o guard de pai circunferencial (`MSG_CIRCUNFERENCIAL`) continua bloqueando — o prefixo não altera nenhuma dessas regras.
- **CA28 (unicidade segue no backend):** Dado um código final que já existe no catálogo, Quando o admin tenta salvar, Então o backend retorna 422 (`BusinessException` de unicidade — inalterado) e o front exibe a mensagem genérica; a montagem do prefixo no front **não** assume o papel da validação de unicidade.
- **CA29 (modo edição inalterado):** Dado um registro existente aberto em **edição**, Quando o form carrega, Então o campo "Código" permanece somente-leitura (sem prefixo travado, sem input de sufixo) — o recurso de prefixo só existe na criação.
- **CA30 (documentação — se padrão novo no DS):** Dado que o dev opte por estender `AppInput`/criar um padrão de input com prefixo no design system, Quando o PR é aberto, Então `Docs/DESIGN.md` registra o novo padrão de "input com prefixo fixo"; se o dev compuser localmente no form sem padrão reutilizável, então nenhuma atualização de doc é exigida (e o CA é dado como N/A justificado).

---

## 7. Riscos e dependências

- **Risco — separar "sufixo" de "código final" no estado do form:** a forma mais limpa é guardar o **sufixo** como estado próprio e derivar o `codigo` final (`paiCodigo + "-" + sufixo`) num computed. Tentar manipular a string `codigo` inteira (cortar/recolar prefixo a cada troca de pai) é frágil e tende a quebrar R9 (preservar sufixo). CA22 é o guard.
- **Risco — prefixo "travado" cosmético:** usar só `readonly`/CSS num input único não impede o cursor de editar o prefixo em alguns navegadores. R8/CA21 exigem que o prefixo **não esteja no campo editável** (segmento separado). O dev precisa garantir proteção real, não visual.
- **Risco — limite de 60 chars escondendo erro:** se o front não avisar o limite efetivo do sufixo, o admin digita, salva e leva 422 do backend sem entender. CA25 exige o aviso proativo no front (espelho da regra do back).
- **Dependência — branch:** implementar na **mesma** branch `fix/paridade-regioes-anatomicas-exame-fisico`, por cima do que o original entrega (o dropdown de pai nível 1+2 do original é uma das 3 vias que disparam o prefixo — R7). Se o original ainda não estiver implementado, o dev pode entregar ambos juntos na mesma branch.
- **Sem dependência de backend/DB:** nada para o `imedto-database`. Nenhuma migration, nenhum contrato de API novo.

---

## 8. Observações para execução

**Não-negociável:**
- **R8/CA21** — proteção real do prefixo (não cosmética).
- **R9/CA22** — trocar pai preserva sufixo (estado = sufixo, código final = derivado).
- **R10/CA25** — aviso de limite de 60 chars no front (espelho do back), não deixar o 422 ser a primeira sinalização.
- **R12/CA27** — zero regressão na derivação de vista/nível e no guard circunferencial do original.

**Liberdade técnica:**
- Estender `AppInput` com prefixo (preferível, vira padrão de DS → atualiza `Docs/DESIGN.md`) **ou** compor no form. Decisão do dev. Se for padrão de DS, documentar (CA30).
- Forma de derivar o sufixo limpo do código ao trocar de pai — escolha do dev, desde que CA22 passe.

**Pipeline:** `imedto-developer` (frontend) → `imedto-qa`. Sem `imedto-database` (frontend-only).

---

## 9. Atualização de documentação

- **`Docs/DESIGN.md`** — **condicional**: se o dev introduzir um padrão reutilizável de "input com prefixo fixo" (estendendo `AppInput` ou criando componente novo), registrar na seção de componentes/inputs do design system. Se a solução for composta localmente no form sem padrão reutilizável, **não precisa** (CA30 cobre os dois caminhos).
- **`Docs/ARQUITETURA.md`** — **não precisa** (sem mudança de back/contrato).
- **`Docs/LGPD.md`** — **não precisa** (sem PII, sem audit novo).
- **Migrations / `Docs/COMANDOS.md`** — **não precisa** (frontend-only, zero migration).
