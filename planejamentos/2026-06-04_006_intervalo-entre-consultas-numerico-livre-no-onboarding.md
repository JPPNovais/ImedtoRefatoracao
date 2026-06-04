# Intervalo entre consultas como campo numérico livre no onboarding

**ID**: 2026-06-04_006
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: nenhuma (apenas o step de configuração de agenda do onboarding)

## 1. Contexto e motivação

No cadastro inicial da plataforma (onboarding do estabelecimento), o campo "Intervalo entre consultas" hoje é um `<select>` com opções fixas (0/5/10/15). O dono que cadastra a clínica fica preso a esses degraus pré-definidos e não consegue informar o intervalo real da sua operação (ex.: 7, 12, 25 minutos).

A própria plataforma já resolve isso em outro ponto: o `FuncionamentoTab` (configuração de funcionamento do estabelecimento, já em produção) usa um input numérico livre de 0 a 240 minutos com validação inline. A demanda é trazer essa mesma liberdade para o momento do cadastro inicial, espelhando o padrão já existente e o domínio do backend — que já valida 0–240.

Evidência: solicitação direta do usuário — "Poderia inserir manualmente o tempo de intervalo entre as consultas no momento do cadastro inicial da plataforma."

## 2. Persona-alvo

Dono / administrador do estabelecimento, no primeiro acesso à plataforma, durante o passo de configuração inicial da agenda (onboarding). Uso pontual (uma vez por estabelecimento), mas de alto impacto: define o intervalo padrão que regerá os encaixes de agenda da clínica.

## 3. Escopo

**Inclui**:
- Substituir o `<select>` de "Intervalo entre consultas" do onboarding (`OnboardingView.vue:913-923`) por um input numérico livre.
- Faixa permitida: 0 a 240 minutos, passo 5, espelhando `FuncionamentoTab.vue`.
- Validação client-side com erro inline idêntico ao padrão existente, bloqueando o avanço/submit quando fora da faixa.
- Manter o markup/visual atual do step (HTML cru com classes `.field`/`.input-wrap`), trocando apenas o controle do campo de intervalo.
- Manter a coerção numérica no envio (`intervaloEntreConsultasMinutos` permanece `int`).

**Não inclui**:
- Qualquer alteração no campo "Duração padrão da consulta" — continua `<select>` fixo (20/30/45/60). Fora deste briefing.
- Migração do step de onboarding para o design system (AppInput, AppFormField etc.) — seria scope creep; mantém-se o HTML cru atual.
- Qualquer mudança de backend: domínio, handler, command, contrato ou endpoint. O backend já valida 0–240 e já recebe `int`.
- Qualquer migration ou mudança de schema.

## 4. Regras de negócio

- **R1**: O intervalo entre consultas deve ser um inteiro entre 0 e 240 minutos (inclusive). Mora em: Domain (`Estabelecimento.cs:175-176`, já existente) e espelhada no Front (novo, no onboarding). Validada em: back (canônico, lança `BusinessException` → 422) + front (UX, erro inline + bloqueio de submit).
- **R2**: Valor 0 representa "sem intervalo entre consultas" e é válido. Mora em: Domain + Front. Validada em: back + front.
- **R3**: O passo de incremento sugerido pelo controle é 5 (espelhando `FuncionamentoTab`), mas valores não-múltiplos de 5 digitados manualmente dentro da faixa (ex.: 7) são aceitos — `step` é apenas auxílio de UI, não regra de validação. Mora em: Front. Validada em: front (e back não rejeita, pois domínio valida apenas faixa 0–240).

## 5. Modelo de dados

Nenhuma alteração. O onboarding continua enviando `intervaloEntreConsultasMinutos` como `int` no `FinalizarOnboardingCommand` (`FinalizarOnboardingCommandHandler.cs:138`), que persiste via domínio `Estabelecimento`. Sem coluna nova, sem índice, sem audit adicional, sem PII. Multi-tenant inalterado (escopo já é o estabelecimento sendo criado no onboarding).

## 6. UX e fluxo

Step de configuração de agenda do onboarding (`OnboardingView.vue`), bloco "Intervalo entre consultas":

- **Antes**: `<select v-model="intervaloConsulta">` com options 0/5/10/15.
- **Depois**: input `type="number"` com `min="0"`, `max="240"`, `step="5"`, dentro do mesmo `.input-wrap`/`.field` atual, mantendo label e layout.
- **Default ao abrir o step**: `0` (equivalente a "Sem intervalo"), mantendo o comportamento atual do `ref` (`OnboardingView.vue:313` → `intervaloConsulta` inicia em `"0"`).
- **Erro inline**: quando o valor estiver fora de 0–240, exibir a mensagem exatamente: `Intervalo entre consultas deve estar entre 0 e 240 minutos.` — mesmo texto e mesma mecânica de `erroIntervalo` em `FuncionamentoTab.vue:77-81`. A mensagem aparece abaixo/junto ao campo sem PII.
- **Estados**:
  - Vazio/inicial: campo com `0`, sem erro.
  - Válido (0–240): sem erro; permite avançar/submeter.
  - Inválido (< 0, > 240): erro inline visível; avanço/submit bloqueado.
- Visual e classes permanecem os do onboarding atual (não usar componentes do design system).
- Mobile: input numérico já é responsivo no layout atual; nenhuma regra nova.

## 7. Critérios de aceite (testáveis)

- **CA1** (caminho feliz — valor manual livre): Dado o dono no step de configuração de agenda do onboarding, Quando ele digita `7` no campo "Intervalo entre consultas" e conclui o onboarding, Então o valor é enviado como `intervaloEntreConsultasMinutos: 7` (int) e o estabelecimento é criado com intervalo de 7 minutos, sem erro.

- **CA2** (limites da faixa aceitos): Dado o campo de intervalo, Quando o usuário informa `0`, Então é aceito como válido (sem intervalo) e o submit é permitido; e Quando informa `240`, Então também é aceito como válido e o submit é permitido.

- **CA3** (validação client-side — acima do máximo): Dado o campo de intervalo, Quando o usuário informa `241` (ou qualquer valor > 240), Então o front exibe o erro inline `Intervalo entre consultas deve estar entre 0 e 240 minutos.` e o botão de avançar/concluir o step fica bloqueado (submit não dispara).

- **CA4** (validação client-side — abaixo do mínimo): Dado o campo de intervalo, Quando o usuário informa um valor negativo (ex.: `-5`), Então o front exibe o erro inline `Intervalo entre consultas deve estar entre 0 e 240 minutos.` e o submit fica bloqueado.

- **CA5** (erro se limpa ao corrigir): Dado o campo de intervalo exibindo o erro inline (valor inválido), Quando o usuário corrige para um valor dentro de 0–240 (ex.: `15`), Então o erro inline desaparece e o avanço/submit volta a ser permitido.

- **CA6** (default sensato ao abrir o step): Dado que o usuário abre o step de configuração de agenda do onboarding pela primeira vez, Quando o campo de intervalo é renderizado, Então ele exibe o valor padrão `0` (equivalente a "Sem intervalo") e nenhum erro inline é mostrado.

- **CA7** (coerção numérica preservada — não-regressão de contrato): Dado o submit do onboarding com intervalo válido, Quando o payload é montado, Então `intervaloEntreConsultasMinutos` é enviado como número (`Number(intervaloConsulta.value) || 0`, conforme `OnboardingView.vue:435`), nunca como string, mantendo o contrato `int` do `FinalizarOnboardingCommand` inalterado.

- **CA8** (rede de segurança backend — espelho 422): Dado um envio que, por bypass do front (ex.: chamada direta à API), traga `intervaloEntreConsultasMinutos` fora de 0–240, Quando o `FinalizarOnboardingCommand` é processado, Então o domínio `Estabelecimento` lança `BusinessException` e a API responde 422 com mensagem genérica de regra de negócio, sem persistir o estabelecimento. (Comportamento já existente — validar que segue funcionando, não implementar.)

- **CA9** (não-regressão visual): Dado o step de configuração de agenda do onboarding, Quando renderizado após a mudança, Então o layout/visual do bloco permanece com as classes `.field`/`.input-wrap` atuais (HTML cru), sem componentes do design system, e o campo "Duração padrão da consulta" permanece como `<select>` fixo (20/30/45/60) inalterado.

## 8. Riscos e dependências

- Risco baixo. A mudança é cirúrgica e isolada ao step de agenda do onboarding.
- Atenção a não tocar o campo "Duração padrão da consulta" (mesmo bloco visual) — escopo restrito ao intervalo.
- Garantir que a coerção `Number(...) || 0` continue tratando campo vazio como `0` para não enviar `NaN`/string ao backend.
- Sem dependências de outras features. Backend e contrato já prontos.

## 9. Observações para execução

- **Não-negociável**: reusar o padrão já validado de `FuncionamentoTab.vue:260-272` (input numérico) + `FuncionamentoTab.vue:77-81` (validação `erroIntervalo`). Mesma faixa (0–240), mesmo `step` (5), mesma mensagem de erro literal.
- **Não-negociável**: nenhuma alteração de backend, contrato, migration ou endpoint.
- **Liberdade técnica**: implementar a validação no front via computed/ref espelhando o `erroIntervalo` existente, desde que (a) bloqueie o submit/avanço e (b) limpe ao corrigir.
- Manter `intervaloConsulta` como `ref` e a coerção numérica no envio (`OnboardingView.vue:435`) — apenas o tipo de controle no template muda.
- **Reuso > duplicação**: a regra de faixa e a mensagem já existem; este briefing apenas estende o mesmo padrão ao onboarding, sem criar novo componente nem novo texto.

## 10. Atualização de documentação

Nenhum doc em `Docs/` precisa ser atualizado. Avaliação explícita: a demanda não introduz bounded context, padrão de DI/bus/auth/store, componente novo de design system (mantém HTML cru existente), recurso de infra, comando novo, nem novo tipo de PII/audit/retenção. É uma troca de controle de formulário que reaproveita regra de negócio e validação já documentadas e já em produção. Logo, não há mudança de arquitetura/infra/design/LGPD a refletir.
