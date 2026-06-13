# Padronização da busca automática de CEP (composable único)

**ID**: 2026-06-13_001
**Status**: Aprovado por usuário em 2026-06-13
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: onboarding (cadastro de clínica) / cadastro de paciente / configuração de unidades / nenhuma no backend

## 1. Contexto e motivação

A busca automática de endereço por CEP (ViaCEP) existe hoje em **três telas**, implementada de **três jeitos diferentes e divergentes**:

| Tela | Disparo | Fonte da busca | Problema |
|---|---|---|---|
| `OnboardingView` (Step 2) | `watch` + `useDebouncedRef` (400ms) + guard `reqId` | `viaCepService.buscarPorCep` | É o melhor padrão hoje, mas mistura logradouro+bairro em string única (`endereco`) e cidade+UF em string única (`cidadeUf`) — campos não estruturados |
| `UnidadesTab` (criar + editar) | `@blur` | `utils/viaCep.buscarCep` | Dispara só ao sair do campo; usa segundo service utilitário duplicado |
| `PacienteFormModal` | `@blur` **e** `watch` (disparo duplo) | **`fetch` cru direto** ao ViaCEP | Duplica a lógica sem usar service algum; pode disparar 2× |
| `PacienteFormSidePanel` | — | — | Tem os campos de endereço mas **não tem busca de CEP nenhuma** |

Consequências da divergência:
- **Dois services fazendo a mesma coisa**: `services/viaCepService.ts` (campos `cidade`/`uf`) e `utils/viaCep.ts` (campo `localidade`/`uf`).
- **Uma terceira cópia via `fetch` cru** dentro do `PacienteFormModal`, fora de qualquer service.
- **UX inconsistente**: em uma tela o endereço aparece ao digitar (debounce), em outra só ao sair do campo (`@blur`), em outra dispara duas vezes.
- **`PacienteFormSidePanel` não autocompleta** — o usuário digita CEP e precisa preencher tudo na mão, atrito desnecessário no fluxo de cadastro rápido.

Esta demanda **unifica tudo num composable único** `useCepAutofill`, com comportamento idêntico em todas as telas, e de quebra **adiciona a busca ao SidePanel** (que estava faltando) e **reestrutura o formulário de endereço do Onboarding** para campos separados — encerrando a string única confusa (`endereco` mistura logradouro+bairro; `cidadeUf` mistura cidade+UF).

Viola o princípio **Reuso > duplicação** do CLAUDE.md ter três implementações de CEP. Esta é uma dívida técnica de UX cross-cutting.

## 2. Persona-alvo

- **Dono de clínica** no primeiro acesso (Onboarding) — preenche o endereço da clínica uma única vez, sob ansiedade do primeiro uso. Atrito aqui é crítico para ativação.
- **Recepção / Profissional / Dono** ao cadastrar paciente (SidePanel de cadastro rápido e Modal completo) — operação de alta frequência no dia a dia. Autofill de CEP economiza segundos por cadastro, dezenas de vezes ao dia.
- **Dono / Admin** ao configurar unidades do estabelecimento (`UnidadesTab`) — operação eventual, mas que hoje tem UX diferente das demais.

## 3. Escopo

**Inclui**:
- Criar composable único `frontend/src/composables/useCepAutofill.ts` com:
  - `watch` reativo sobre o ref do CEP.
  - **Limpeza imediata síncrona** dos campos de endereço quando o CEP cai abaixo de 8 dígitos (usuário apagou/editou).
  - **Busca com debounce ~300-400ms** via `useDebouncedRef` quando o CEP atinge 8 dígitos.
  - **Guard de race condition** (`reqId`, última requisição vence).
  - Estado de `buscando` (loading) exposto.
  - Tratamento silencioso de erro (CEP inexistente / falha de rede não preenche e não quebra).
- Consolidar a busca num **único service** (escolher `services/viaCepService.ts` como canônico — mais campos, melhor nomeação) e remover/deprecar o `utils/viaCep.ts` e o `fetch` cru do `PacienteFormModal`.
- **Aplicar `useCepAutofill` em todas as 4 telas**, removendo `@blur` de todas:
  - `OnboardingView` (com reestruturação de campos — ver abaixo)
  - `UnidadesTab` (criar + editar)
  - `PacienteFormModal`
  - `PacienteFormSidePanel` (**ganha autofill que não tinha**)
- **Reestruturar o formulário de endereço do `OnboardingView`** para campos separados: `logradouro`, `bairro`, `cidade`, `uf` (separados), além de `cep`, `numero` e `complemento` — encerrando a string única `endereco` (logradouro+bairro) e `cidadeUf` (cidade+UF) na UI.
- No submit do Onboarding, **remontar a string única `endereco`** a partir dos campos separados, **mantendo o contrato HTTP atual intacto** (backend continua recebendo `endereco: string`).

**Não inclui**:
- **Mudança de contrato HTTP do Onboarding** — o backend continua recebendo `endereco` como string única. NÃO mexer no `OnboardingController`, `FinalizarOnboardingCommand`, handler ou entidade `Estabelecimento`.
- **Persistir `cidade`/`estado` estruturados no Onboarding** — as colunas `cidade`/`estado` existem na tabela `estabelecimentos` e o método `Estabelecimento.AtualizarEndereco()` existe, mas preenchê-los no onboarding seria mudança de Application layer NÃO pedida. Fica como backlog (ver §8).
- Validação de CEP contra base oficial / bloqueio de submit por CEP inválido — busca continua best-effort.
- Qualquer mudança no fluxo de funcionamento, profissional, ou outros steps do Onboarding fora do Step 2.
- Migration / mudança de schema — **esta demanda não toca banco**.

## 4. Regras de negócio

> Nota: busca de CEP é UX pura (enriquecimento de formulário client-side via API pública ViaCEP). Não há regra de negócio de domínio no backend — por isso, excepcionalmente, não há espelho back+front. O backend permanece intocado.

- **R1 — Disparo por 8 dígitos**: a busca só dispara quando o CEP tem exatamente 8 dígitos (após `replace(/\D/g, "")`). Mora em: `useCepAutofill` (Front). Validada em: Front (não há back).
- **R2 — Limpeza imediata ao encolher**: quando o CEP digitado cai abaixo de 8 dígitos (usuário apagou), os campos autopreenchidos pela última busca (`logradouro`, `bairro`, `cidade`, `uf`) são limpos **de forma síncrona e imediata** (sem debounce), para não deixar dado de um CEP antigo grudado num CEP novo incompleto. Mora em: `useCepAutofill` (Front).
- **R3 — Debounce na busca**: ao atingir 8 dígitos, a chamada ao ViaCEP é debounced (~300-400ms via `useDebouncedRef`) para não disparar a cada tecla durante a digitação dos últimos dígitos. Mora em: `useCepAutofill` (Front).
- **R4 — Última requisição vence (guard de race)**: cada busca recebe um `reqId` incremental; quando a resposta volta, se já houve um `reqId` mais novo, a resposta é descartada. Garante que digitar dois CEPs em sequência rápida não preencha o formulário com o resultado do CEP errado. Mora em: `useCepAutofill` (Front).
- **R5 — Não sobrescrever o que o usuário já preencheu manualmente**: ao autopreencher, campos que o usuário já editou manualmente NÃO são sobrescritos pela busca (preserva digitação). O `numero` e o `complemento` nunca vêm do ViaCEP de forma confiável — o ViaCEP só preenche `complemento` quando vazio. Mora em: `useCepAutofill` + callback de cada tela (Front). _Comportamento espelha o que `UnidadesTab` e `OnboardingView` já fazem hoje (`r.campo || form.campo`)._
- **R6 — Erro é silencioso**: CEP inexistente (`data.erro`) ou falha de rede não preenche nada, não exibe mensagem de erro, não bloqueia o formulário. O usuário simplesmente preenche na mão. Mora em: `useCepAutofill` (Front).
- **R7 — Onboarding remonta a string única no submit**: o Step 2 do Onboarding coleta campos separados, mas na função `finalizar()` o front **remonta** a string `endereco` no mesmo formato que o backend já recebe hoje, mantendo o contrato. Mora em: `OnboardingView.finalizar()` (Front). Validada em: o payload enviado a `POST /onboarding/finalizar` continua tendo `estabelecimento.endereco: string` e nada mais de endereço — contrato idêntico ao atual.

## 5. Modelo de dados

**Nenhuma mudança de schema.** Esta demanda é 100% frontend.

- Backend do Onboarding intocado: `OnboardingController.FinalizarOnboardingRequest`, `EstabelecimentoOnboardingRequest`, `FinalizarOnboardingCommand`, `EstabelecimentoOnboardingInput`, `FinalizarOnboardingCommandHandler` e a entidade `Estabelecimento` permanecem exatamente como estão.
- Tabela `estabelecimentos`: coluna `endereco varchar(500)` continua recebendo a string única remontada. Colunas `cidade varchar(100)` e `estado char(2)` continuam não sendo preenchidas no onboarding (como hoje).
- Cadastro de paciente e unidades: já têm os campos estruturados persistidos (sem mudança).
- **Sem multi-tenant / audit / LGPD novos** — busca de CEP é dado público de logradouro, não é PII de paciente. Não há gravação nova nem leitura de dado sensível.

## 6. UX e fluxo

### 6.1 Composable `useCepAutofill`

API proposta (liberdade técnica do dev no formato exato, desde que cumpra R1-R6):

```
useCepAutofill(cepRef, onEndereco, { onLimpar?, delay? })
  → retorna { buscando: Ref<boolean> }
```

- `cepRef`: ref reativo do CEP (string com máscara).
- `onEndereco(endereco)`: callback chamado com o resultado da busca; cada tela mapeia os campos retornados (`logradouro`, `bairro`, `cidade`, `uf`, `complemento`) para o seu próprio form, respeitando R5 (não sobrescrever o que o usuário digitou).
- `onLimpar()` (opcional): callback chamado de forma síncrona quando o CEP encolhe < 8 dígitos (R2), para a tela limpar seus campos.
- Internamente: `useDebouncedRef` para a busca, `watch` síncrono sobre `cepRef` para detectar o encolhimento e disparar `onLimpar` imediatamente, `reqId` para o guard de race.
- Reusa o service canônico `viaCepService.buscarPorCep`.

### 6.2 OnboardingView — Step 2 reestruturado

**Antes** (campos atuais): `cep`, `cidadeUf` (string "Cidade / UF"), `endereco` (string "logradouro — bairro"), `numero`.

**Depois** (campos separados):
```
┌─ Sua clínica ──────────────────────────────────────┐
│ Nome da clínica         [______________________]   │
│ CNPJ                    [__________]  Tel [______]  │
│ CEP  [00000-000] (buscando...)                      │
│ Logradouro [________________________]  Nº [_____]   │
│ Complemento [_______________]  Bairro [__________]  │
│ Cidade [________________]  UF [__]                  │
└─────────────────────────────────────────────────────┘
```

- `cep` → dispara `useCepAutofill`.
- `logradouro`, `bairro`, `cidade`, `uf` → autopreenchidos pela busca, editáveis.
- `numero`, `complemento` → sempre manuais.
- Estado `buscando` mostra o hint "buscando..." ao lado do label CEP (já existe esse padrão visual hoje).
- **No submit (`finalizar`)**: remontar `endereco` no mesmo formato atual. Sugestão de formato (manter compatível com o que já é gravado hoje):
  `"{logradouro}, {numero} — {bairro} — {cidade} / {uf} — {cep}"`, omitindo partes vazias com `.filter(Boolean)`. O importante é que o payload tenha `estabelecimento.endereco: string` e nenhum campo de endereço novo.

> Liberdade técnica: o dev pode ajustar o formato exato da string remontada desde que continue legível e caiba em `varchar(500)`. A premissa não-negociável é **não enviar campos de endereço novos no payload** (contrato idêntico).

### 6.3 Demais telas

- `PacienteFormSidePanel`: liga `useCepAutofill` ao `form.cep`, mapeando `logradouro`, `bairro`, `cidade`, `uf` para os campos já existentes. Ganha hint "buscando..." (reusar o padrão visual existente).
- `PacienteFormModal`: remove o `fetch` cru e o disparo duplo (`@blur` + `watch`), passa a usar `useCepAutofill`. Remove `@blur`.
- `UnidadesTab` (criar + editar): troca `onCepBlurNovo`/`onCepBlurEdit` por `useCepAutofill`. Remove `@blur` de ambos. Mapeia `localidade`→`cidade` corretamente via service canônico.

### 6.4 Estados

- **Loading**: hint "buscando..." (spinner) ao lado do label CEP enquanto a requisição corre. Reusa o padrão visual do OnboardingView atual.
- **Erro** (CEP inexistente / offline): silencioso, sem mensagem. Campos ficam como estão para preenchimento manual (R6).
- **Vazio / encolhimento**: campos autopreenchidos são limpos imediatamente quando CEP < 8 dígitos (R2).
- **Sucesso**: campos preenchidos, hint some.

### 6.5 Componentes do design system

- `AppInput` + diretiva `v-maska="'#####-###'"` para o CEP (padrão já usado em todas as telas).
- `AppField` para labels (onde já é usado — Unidades e Paciente). OnboardingView usa labels próprios (manter consistência com o resto do OnboardingView, que não usa `AppField`).
- Não introduzir componente DS novo — reusar `AppInput`/`AppField` existentes.

## 7. Critérios de aceite (testáveis)

### Composable e comportamento padronizado

- **CA1 (caminho feliz — autofill)**: Dado um campo de CEP ligado ao `useCepAutofill`, Quando o usuário digita um CEP válido completo (8 dígitos, ex: 01311-100), Então após ~300-400ms os campos `logradouro`, `bairro`, `cidade` e `uf` são preenchidos automaticamente com os dados do ViaCEP.
- **CA2 (debounce)**: Dado o usuário digitando os 8 dígitos do CEP, Quando ele digita rápido, Então a chamada ao ViaCEP NÃO dispara a cada tecla — dispara uma vez após o debounce de ~300-400ms a partir da última tecla.
- **CA3 (limpeza imediata ao encolher)**: Dado um formulário com campos de endereço já preenchidos por um CEP, Quando o usuário apaga um dígito do CEP (fica < 8 dígitos), Então os campos `logradouro`, `bairro`, `cidade` e `uf` são limpos imediatamente (síncrono, sem esperar debounce).
- **CA4 (guard de race — última vence)**: Dado que o usuário digita o CEP A e, antes da resposta chegar, apaga e digita o CEP B, Quando ambas as respostas voltam (A depois de B), Então o formulário fica com os dados do CEP B (a resposta obsoleta de A é descartada).
- **CA5 (não sobrescrever digitação manual)**: Dado que o usuário já digitou manualmente um logradouro, Quando a busca de CEP retorna, Então o logradouro digitado pelo usuário NÃO é sobrescrito pelo retorno do ViaCEP (R5).
- **CA6 (erro silencioso — CEP inexistente)**: Dado um CEP com 8 dígitos que não existe (ViaCEP retorna `erro: true`), Quando a busca completa, Então nenhum campo é preenchido, nenhuma mensagem de erro aparece, e o formulário continua editável.
- **CA7 (erro silencioso — offline)**: Dado falha de rede ao consultar o ViaCEP, Quando a busca falha, Então nenhuma exceção vaza para a UI, nenhum campo é preenchido, e o usuário pode preencher manualmente.
- **CA8 (loading)**: Dado uma busca de CEP em andamento, Quando a requisição está pendente, Então o hint "buscando..." (spinner) é exibido ao lado do label CEP; Quando termina, o hint some.

### Aplicação por tela

- **CA9 (SidePanel ganha autofill)**: Dado o `PacienteFormSidePanel` (cadastro rápido de paciente), Quando o usuário digita um CEP válido, Então os campos de endereço são preenchidos automaticamente — comportamento que NÃO existia nesta tela antes.
- **CA10 (PacienteFormModal sem disparo duplo)**: Dado o `PacienteFormModal`, Quando o usuário completa o CEP e sai do campo, Então a busca dispara **uma única vez** (não mais via `@blur` + `watch` simultâneos), e o `fetch` cru foi substituído pelo composable.
- **CA11 (UnidadesTab criar)**: Dado o formulário de criar unidade em `UnidadesTab`, Quando o usuário digita um CEP válido, Então `logradouro`, `bairro`, `cidade` (mapeado de `localidade`) e `estado` (uf) são preenchidos via composable, sem depender de `@blur`.
- **CA12 (UnidadesTab editar)**: Dado o formulário de editar unidade em `UnidadesTab`, Quando o usuário altera o CEP, Então o autofill funciona igual ao de criar, via o mesmo composable.
- **CA13 (remoção de @blur)**: Dado qualquer das telas migradas (Onboarding, UnidadesTab criar/editar, PacienteFormModal), Quando se inspeciona o template, Então não há mais `@blur` disparando busca de CEP em nenhuma delas (busca é 100% reativa via watch/debounce).

### Onboarding reestruturado e contrato

- **CA14 (campos separados no Onboarding)**: Dado o Step 2 do Onboarding, Quando a tela carrega, Então os campos de endereço aparecem separados (`logradouro`, `numero`, `complemento`, `bairro`, `cidade`, `uf`) — não mais a string única `endereco` e o campo combinado `cidadeUf`.
- **CA15 (autofill no Onboarding)**: Dado o Step 2 do Onboarding, Quando o usuário digita um CEP válido, Então `logradouro`, `bairro`, `cidade` e `uf` são autopreenchidos separadamente nos seus respectivos campos.
- **CA16 (contrato HTTP intacto — não-negociável)**: Dado o usuário finalizando o onboarding com endereço preenchido, Quando o front envia `POST /onboarding/finalizar`, Então o payload contém `estabelecimento.endereco` como **uma única string** (remontada dos campos separados) e **nenhum campo de endereço novo** (`logradouro`/`bairro`/`cidade`/`uf`/`cep`/`numero`/`complemento` NÃO aparecem no JSON). Verificável inspecionando a request no DevTools Network.
- **CA17 (onboarding completa sem erro)**: Dado o onboarding finalizado com endereço estruturado preenchido, Quando o backend processa, Então o estabelecimento é criado com sucesso (200/201) e o campo `endereco` no banco contém a string remontada legível — o cadastro da clínica NÃO quebra.
- **CA18 (onboarding sem endereço)**: Dado o usuário que deixa os campos de endereço vazios no onboarding, Quando finaliza, Então o submit funciona normalmente e `estabelecimento.endereco` é `undefined`/omitido (como hoje) — endereço é opcional.

### Reuso e qualidade

- **CA19 (service único)**: Dado o código após a migração, Quando se busca por consumidores de busca de CEP, Então todas as telas usam o `useCepAutofill` (que usa o service canônico `viaCepService`); o `fetch` cru do `PacienteFormModal` foi removido e o `utils/viaCep.ts` foi removido ou não tem mais consumidores.
- **CA20 (tipografia)**: Dado qualquer CSS scoped novo/alterado nas telas migradas, Quando se roda `npm run check:typography -- --ci`, Então não há literal de `font-size`/`font-weight` (tokens DS conforme CLAUDE.md §5).
- **CA21 (sem regressão de cadastro)**: Dado o cadastro de paciente (SidePanel e Modal) e de unidade após a migração, Quando se cria/edita um registro com endereço, Então os campos de endereço são persistidos corretamente como antes (sem regressão no contrato dessas telas, que já usam campos separados).

## 8. Riscos e dependências

- **Risco — formato da string remontada no Onboarding**: a string gravada em `endereco` muda de formato em relação à atual (hoje: `"endereco, numero — cidadeUf — cep"`; depois: campos remontados). Registros antigos no banco continuam com o formato antigo — não há migração de dados, e isso é aceitável (é texto livre de exibição). O QA deve só garantir que o novo formato é legível e cabe em `varchar(500)`.
- **Risco — divergência de campo entre os dois services antigos**: `viaCepService` usa `cidade`, `utils/viaCep` usa `localidade`. Ao consolidar no `viaCepService`, garantir que o `UnidadesTab` (que esperava `localidade`) mapeie corretamente para `cidade`. Coberto por CA11/CA12.
- **Risco — sobrescrever digitação**: R5 é sutil. Em telas de **edição** (UnidadesTab editar, PacienteFormModal editar), os campos já vêm preenchidos do registro; uma busca disparada por edição do CEP não deve apagar dados válidos do paciente/unidade. O `onLimpar` (R2) só dispara ao encolher o CEP — atenção do dev para não limpar agressivamente em telas de edição. QA deve validar edição além de criação.
- **Risco — disparo no carregamento de tela de edição**: ao abrir uma tela de edição com CEP já preenchido (8 dígitos), o `watch` pode disparar uma busca no mount e sobrescrever dados. O dev deve garantir que o autofill **não dispare no valor inicial** (só em mudança feita pelo usuário) ou que R5 proteja os campos. Validar em CA12 e no Modal de edição.
- **Dependência**: `useDebouncedRef` (já existe em `frontend/src/composables/`) e `viaCepService` (já existe). Nenhuma lib nova.
- **Áreas regressivas a vigiar**: fluxo de ativação (Onboarding é o primeiro acesso — quebrar aqui impede uso do produto), cadastro de paciente (alta frequência).

**Backlog (fora deste escopo, registrar para o produto)**:
- Persistir `cidade`/`estado` estruturados no Onboarding via `Estabelecimento.AtualizarEndereco()` — habilitaria variáveis estruturadas (ex: `{{cidade}}` em termos/documentos). Mudança de Application layer, não pedida aqui.

## 9. Observações para execução

- **Não-negociável**: o contrato HTTP do Onboarding NÃO muda (CA16). Não tocar backend (`OnboardingController`, Command, Handler, entidade `Estabelecimento`, migrations). Esta é uma demanda **frontend-only**.
- **Não-negociável**: remover `@blur` de todas as telas (CA13) e o `fetch` cru do `PacienteFormModal` (CA10/CA19). O objetivo é UX idêntica em todas as telas via o composable único.
- **Não-negociável**: comportamento de limpeza imediata síncrona ao encolher (R2/CA3) e guard de race (R4/CA4) — são as duas características que distinguem o composable de uma busca ingênua.
- **Liberdade técnica**: assinatura exata do `useCepAutofill`, formato exato da string remontada do Onboarding (desde que legível e ≤ 500 chars), e se remove fisicamente `utils/viaCep.ts` ou apenas o deixa sem consumidores (preferência: remover se ninguém mais usa — Surgical Changes permite remover orphan que a própria mudança criou).
- **Reuso**: service canônico é `services/viaCepService.ts` (`buscarPorCep`, campos `cidade`/`uf`). Não criar service novo.
- **Atenção especial às telas de edição**: garantir que abrir uma edição com CEP preenchido não dispare busca que sobrescreva dados (ver §8). O QA deve validar criação E edição.
- **Sem migration**: o `imedto-database` NÃO precisa ser acionado nesta demanda.

## 10. Atualização de documentação

- `Docs/DESIGN.md` — adicionar `useCepAutofill` à seção de composables/padrões de formulário do design system: nome, propósito (busca automática de endereço por CEP padronizada), API resumida e a regra de que **toda nova tela com campo de CEP deve usar este composable** (encerra a divergência de três implementações). Mudança incremental, só a seção de composables.

> Nenhum outro doc muda: arquitetura backend, infra, LGPD e comandos permanecem como estão (demanda é frontend-only, sem schema, sem dado sensível novo, sem comando novo).
