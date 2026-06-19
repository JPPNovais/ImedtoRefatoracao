# Suporte a CNPJ alfanumérico (IN RFB 2.229/2024)

**ID**: 2026-06-19_002
**Status**: Aprovado por usuário em 2026-06-19
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: cadastro de estabelecimento (onboarding + edição), cadastro de fornecedor de estoque, geração de PDF/recibo/termo, redação de PII (log + IA). **Não toca**: permissionamento, orçamento, prontuário clínico, relatório financeiro, agenda.

---

## 1. Contexto e motivação

A Receita Federal publicou a **IN RFB 2.229/2024**, que altera o formato do CNPJ a partir de **06/07/2026** (homologação SEFAZ em 06/04/2026). O número passa a ser **alfanumérico**:

- 14 posições, máscara visual **inalterada**: `XX.XXX.XXX/XXXX-XX`.
- As **12 primeiras** posições passam a aceitar **letras maiúsculas A–Z além de dígitos 0–9**.
- As **2 últimas** posições (dígitos verificadores) **continuam numéricas**.
- O dígito verificador continua sendo calculado por **módulo-11 com os mesmos pesos**; o valor de cada caractere passa a ser `(ASCII do caractere) − 48` — ou seja `'0'..'9' → 0..9` e `'A'..'Z' → 17..42`.
- **CNPJs numéricos atuais permanecem 100% válidos** e seus DVs não mudam. A mudança é **retrocompatível**.

Hoje o sistema rejeita qualquer CNPJ que contenha letra: a normalização faz `.Where(char.IsDigit)` (apaga as letras), e a validação exige 14 dígitos. Quando o novo formato entrar em vigor, um estabelecimento ou fornecedor com CNPJ alfanumérico **não conseguiria se cadastrar nem ser editado**, e seus documentos (PDF/recibo/termo) sairiam com o número corrompido. Além disso, o redator de PII (log e prompt de IA) não reconheceria o novo formato e **vazaria o CNPJ alfanumérico em claro**.

Esta demanda torna o sistema compatível com o novo formato **antes da vigência**, de forma retrocompatível.

## 2. Persona-alvo

- **Dono / Administrador do estabelecimento** — no onboarding (criação do estabelecimento) e na edição dos dados cadastrais. Frequência: pontual (criação) + esporádica (correção cadastral).
- **Recepção / Financeiro / Estoque** — ao cadastrar ou editar rapidamente um **fornecedor de estoque** com CNPJ. Frequência: recorrente conforme a operação de compras.
- **Paciente / terceiro que recebe documento** — consome o CNPJ exibido em **recibo de pagamento, termo de consentimento e PDFs** (não digita, apenas lê — o número precisa sair correto).

## 3. Escopo

**Inclui** (escopo completo e fechado — 4 frentes):
1. **Estabelecimento** — validação, normalização, máscara e unicidade no onboarding e na edição.
2. **Fornecedor de estoque** — validação, normalização, máscara e unicidade no cadastro e na edição rápida.
3. **Exibição** — formatação correta do CNPJ alfanumérico em PDF de receita, termo de consentimento, recibo de pagamento e no resolvedor de variáveis de termo.
4. **Redação de PII** — regex de CNPJ no sanitizador de log e no sanitizador de prompt de IA passa a reconhecer o formato alfanumérico (não vazar).

**Não inclui** (fora de escopo, registrar como não-objetivo):
- **Migração / backfill de dados** — hoje todo CNPJ persistido é numérico, não há colisão de caixa; nenhuma transformação de dados existentes é necessária.
- **Gate por data de vigência** — decisão do usuário: aceitar ambos os formatos **já no deploy**, sem trava temporal.
- Qualquer outro ponto de entrada de CNPJ além dos 4 acima (não existem outros no código ativo; `ReferenciaLegado/` é ignorado).
- CPF, telefone, CEP e demais campos que continuam **somente dígitos** — não podem ser afetados.

## 4. Regras de negócio

- **R1 — Normalização alfanumérica dedicada ao CNPJ.** Ao receber um CNPJ (qualquer ponto de entrada), o sistema remove a máscara preservando apenas `[A-Z0-9]` e aplica **uppercase** (`ToUpperInvariant`), produzindo a forma canônica de 14 caracteres. Mora em: **Domain** (`CnpjValidator` no backend; `validateCnpj.ts` no front). **Não-negociável**: criar normalização dedicada ao CNPJ — **nunca** alterar o `SomenteDigitos` genérico do `TextSanitizer` (reusado por CPF/telefone/CEP, que continuam só dígitos). Validada em: **back (fonte da verdade) + front (UX)**.

- **R2 — Validação de formato e DV.** Um CNPJ é válido quando: (a) tem 14 caracteres após normalização; (b) as 12 primeiras estão em `[A-Z0-9]`; (c) as 2 últimas (DV) são **dígitos** `[0-9]`; (d) não são todos os caracteres iguais; (e) os 2 DVs conferem pelo cálculo módulo-11 com valor de caractere `= ASCII − 48`. Mora em: **Domain** (`CnpjValidator.EhValido` + `Estabelecimento`). Validada em: **back + front**.

- **R3 — Uppercase ao vivo + silencioso.** No front, a digitação de minúscula vira maiúscula **automaticamente e em tempo real** (UX fluida, sem mensagem de erro). Mora em: **Front** (máscara inteligente `maska`). Espelho no back via `ToUpperInvariant` na normalização — o back é a fonte da verdade caso a UI seja contornada.

- **R4 — Caractere fora do alfabeto válido é rejeitado.** Caracteres fora de `[A-Z0-9]` (ex.: `@`, acento, `ç`, espaço no meio) são **bloqueados pela máscara** no front; se ainda assim chegarem ao back (chamada direta de API), a validação retorna **422 `BusinessException`** com mensagem clara e **genérica** (sem ecoar o valor inválido completo). Mora em: **Domain/Handler**. Validada em: **back + front**.

- **R5 — DV continua numérico.** As 2 últimas posições devem ser dígitos. A máscara inteligente do front aceita `[A-Za-z0-9]` nas 12 primeiras posições, mas **somente dígito** nas 2 posições do DV. O back rejeita (422) DV com letra. Mora em: **Front (máscara) + Domain (validação)**.

- **R6 — Unicidade e comparação pela forma canônica.** A verificação de CNPJ já cadastrado (estabelecimento e fornecedor) compara pelo **valor normalizado em maiúsculas, sem máscara**. Como não há migração e todos os dados atuais são numéricos, não há risco de colisão de caixa hoje. Mora em: **Query/Handler** (`VerificarCnpjDisponivelQueryHandler` + handlers de fornecedor). Validada em: **back**.

- **R7 — Retrocompatibilidade total.** Todo CNPJ **numérico** válido hoje continua válido, com o **mesmo DV**, sem qualquer alteração de comportamento percebida pelo usuário. Mora em: **Domain**. Validada em: **back + front** (caso de teste obrigatório).

- **R8 — Exibição preserva o número.** Os formatadores de CNPJ usados em PDF/recibo/termo aplicam a máscara `XX.XXX.XXX/XXXX-XX` ao valor canônico **sem descartar letras** (substituir `Where(char.IsDigit)` por preservação de `[A-Z0-9]`). Mora em: **Infra (serviços de PDF) + Front (`usePdfHeader`, `termoResolverVariaveis`)**. Validada em: **back + front**.

- **R9 — Não vazar CNPJ alfanumérico em PII.** Os redatores de PII (log e prompt de IA) reconhecem e mascaram o CNPJ alfanumérico. A regex passa a casar `[A-Z0-9]` nas 12 primeiras posições + `\d{2}` no DV, com a pontuação opcional da máscara. Mora em: **Infra** (`PiiSanitizer`, `RemovePIIEnricher`). Validada em: **back** (LGPD — premissa não-negociável).

## 5. Modelo de dados

**Nenhuma migration de schema esperada** — as colunas já comportam o novo formato:

- `estabelecimentos.cnpj` e `fornecedores_estoque.cnpj` são `varchar(14)`, que acomoda 14 caracteres alfanuméricos.
- Os índices únicos existentes sobre essas colunas continuam válidos (a comparação canônica em maiúsculas é responsabilidade da aplicação — ver R6).

**Ação obrigatória do `imedto-database` (verificação, não alteração)** — registrada como **CA10** abaixo:
1. Confirmar via inspeção do RDS que **não existe `CHECK` constraint** em `estabelecimentos.cnpj` ou `fornecedores_estoque.cnpj` que force apenas dígitos (ex.: `cnpj ~ '^[0-9]+$'`). Se existir, **é a única migration permitida** nesta demanda (relaxar o CHECK para `[A-Z0-9]` nas 12 primeiras + `\d` nos 2 DVs, ou removê-lo se a regra vive na aplicação).
2. Confirmar que a **collation** das colunas/índices não atrapalha a comparação de igualdade em maiúsculas (uppercase canônico vindo da aplicação resolve; só sinalizar se houver collation case-insensitive divergente).
3. Não há backfill: dados atuais são todos numéricos e permanecem válidos sem toque.

**Multi-tenant**: a unicidade de CNPJ de estabelecimento é global por natureza do negócio (CNPJ é único na Receita); a de fornecedor de estoque permanece **filtrada por `estabelecimento_id`** como já é hoje — esta demanda **não altera** o escopo de tenant de nenhuma query (apenas a forma de normalizar o valor comparado). **LGPD**: CNPJ de fornecedor/estabelecimento é dado cadastral de pessoa jurídica — não é dado sensível de saúde, mas é PII e **não pode vazar em log/IA** (R9).

## 6. UX e fluxo

**Onboarding (`OnboardingView.vue`) e edição do estabelecimento (`EstabelecimentoView.vue`)**:
- Campo CNPJ troca a máscara fixa `v-maska="'##.###.###/####-##'"` (token `#` = só dígito) por **máscara inteligente** que: aceita `[A-Za-z0-9]` nas 12 primeiras posições, **só dígito** nas 2 do DV, aplica **uppercase ao vivo** e preserva o visual `XX.XXX.XXX/XXXX-XX`.
- A validação inline duplicada `cnpjFormatoValido` no `OnboardingView` deve passar a usar o util compartilhado `validateCnpj` (reuso > duplicação) ou ser ajustada para o novo formato; **preferência por reuso do util**.
- Estados: campo válido (sucesso, sem aviso) / inválido (mensagem clara "CNPJ inválido" sob o campo, genérica) / vazio (sem erro até o submit, conforme regra de obrigatoriedade já existente).
- Sem mudança de layout, sem novo componente de design system — apenas troca de diretiva de máscara e fonte de validação. **Mobile-ready**: comportamento idêntico, herdado dos campos existentes.

**Fornecedor de estoque (`CadastroFornecedoresTab.vue` e `ModalNovoFornecedorRapido.vue`)**:
- Mesma máscara inteligente e mesma fonte de validação (`validateCnpj` / `apenasDigitos` → renomear/ajustar para preservar alfanumérico) que o estabelecimento. Consistência de UX entre os dois cadastros é não-negociável.

**Exibição (PDF/recibo/termo)**: sem interação de UI — apenas o número renderizado sai correto (com letras quando alfanumérico).

## 7. Critérios de aceite (testáveis)

- **CA1 (retrocompatibilidade — caminho feliz numérico):** Dado o CNPJ numérico `11.222.333/0001-81`, Quando é normalizado e validado no back (`CnpjValidator.EhValido`) e no front (`validateCnpj`), Então é considerado **válido**, persiste como `11222333000181` e seu DV (`81`) é confirmado pelo cálculo — comportamento idêntico ao de hoje.

- **CA2 (caminho feliz alfanumérico):** Dado o CNPJ alfanumérico `12.ABC.345/01DE-35` (base `12ABC34501DE`, DV `35`), Quando é normalizado e validado no back e no front, Então é considerado **válido** e persiste como `12ABC34501DE35` (14 caracteres, maiúsculas, sem máscara).

- **CA3 (DV inválido rejeitado):** Dado o CNPJ `12.ABC.345/01DE-34` (mesma base, DV **errado** — o correto é `35`), Quando é validado no back, Então é **rejeitado** com **422 `BusinessException`** e mensagem genérica ("CNPJ inválido"), e no front o campo fica inválido — sem persistir nada.

- **CA4 (uppercase silencioso ao vivo):** Dado o usuário digitando `12.abc.345/01de` no campo de CNPJ, Quando os caracteres entram, Então a máscara inteligente exibe `12.ABC.345/01DE` em maiúsculas em tempo real, **sem mensagem de erro**, e o valor submetido ao back já está em maiúsculas (e o back reaplica `ToUpperInvariant` por garantia).

- **CA5 (caractere inválido bloqueado/rejeitado):** Dado o usuário tentando digitar `@`, `ç` ou acento nas 12 primeiras posições, Quando digita, Então a máscara **bloqueia** o caractere no front; e Dado uma chamada direta de API com tais caracteres, Quando chega ao handler, Então retorna **422** com mensagem genérica, sem ecoar o valor completo.

- **CA6 (DV não aceita letra):** Dado o usuário tentando digitar uma **letra nas 2 últimas posições** (DV), Quando digita, Então a máscara aceita **apenas dígito** nessas posições; e o back rejeita (422) qualquer CNPJ cujo DV contenha letra.

- **CA7 (normalização dedicada não contamina CPF/telefone/CEP):** Dado que CPF, telefone e CEP continuam usando `TextSanitizer.SomenteDigitos`, Quando a normalização de CNPJ é introduzida (dedicada, ex.: `SomenteAlfanumericoUpper` no `CnpjValidator`/helper novo), Então os testes existentes de CPF/telefone/CEP continuam verdes e **nenhuma letra é aceita** nesses campos — o genérico `SomenteDigitos` permanece intocado.

- **CA8 (exibição em documento preserva letras):** Dado um estabelecimento/fornecedor com CNPJ alfanumérico `12ABC34501DE35`, Quando é gerado um **PDF de receita, termo de consentimento ou recibo de pagamento** (e o resolvedor de variáveis de termo), Então o número aparece formatado como `12.ABC.345/01DE-35` — com as letras preservadas, não corrompido por `Where(char.IsDigit)`.

- **CA9 (PII não vaza CNPJ alfanumérico):** Dado um log ou prompt de IA contendo `12.ABC.345/01DE-35` (com ou sem máscara), Quando passa pelo `RemovePIIEnricher` (log) e pelo `PiiSanitizer` (IA), Então o CNPJ é **mascarado/redatado** — a nova regex casa `[A-Z0-9]{12}` + `\d{2}` com pontuação opcional; e Dado um CNPJ numérico, Quando passa, Então continua sendo mascarado como hoje (sem regressão).

- **CA10 (verificação de schema pelo `imedto-database`):** Dado o RDS de dev/stage, Quando o `imedto-database` inspeciona `estabelecimentos.cnpj` e `fornecedores_estoque.cnpj`, Então confirma que **não há `CHECK` que force só dígitos** nem collation que quebre a comparação canônica em maiúsculas; se houver `CHECK` restritivo, gera a **única migration permitida** (idempotente, em `db/migrations/`) relaxando-o para `[A-Z0-9]{12}\d{2}`. Resultado da inspeção é registrado no hand-off.

- **CA11 (unicidade pela forma canônica):** Dado um CNPJ alfanumérico já cadastrado em maiúsculas, Quando outro cadastro tenta o mesmo CNPJ (com qualquer máscara/caixa), Então `VerificarCnpjDisponivelQueryHandler` e os handlers de fornecedor detectam como **já existente** comparando o valor canônico (upper, sem máscara) — respeitando o escopo de tenant atual de cada um (estabelecimento global; fornecedor por `estabelecimento_id`).

- **CA12 (espelho back+front — paridade de cálculo):** Dado o mesmo conjunto de CNPJs (numéricos e alfanuméricos, válidos e inválidos), Quando avaliados pelo `CnpjValidator` (C#) e pelo `validateCnpj.ts` (TS), Então **ambos produzem o mesmo veredito** para cada caso — incluindo o cálculo de DV via `(charCodeAt − 48)` no front, substituindo o `Number(digits[i])` que falha para letras. `validateCnpj.test.ts` ganha casos alfanuméricos (válido `12ABC34501DE35`, inválido DV-errado, e o numérico de regressão).

- **CA13 (documentação viva):** Dado que esta demanda altera regra cross-cutting de normalização/validação de CNPJ e a redação de PII, Quando a entrega é concluída, Então `Docs/LGPD.md` (regex de PII de CNPJ) e `Docs/ARQUITETURA.md` (nota sobre normalização dedicada de CNPJ alfanumérico) estão atualizados — ver seção 10.

## 8. Riscos e dependências

- **Risco — regressão silenciosa em CPF/telefone/CEP**: o maior risco é alterar o `SomenteDigitos` genérico por engano. **Mitigação**: normalização de CNPJ deve ser dedicada e isolada (R1, CA7). QA roda a suíte de CPF/telefone/CEP como gate.
- **Risco — paridade back/front divergir**: front e back têm cópias do cálculo de DV; o front usa `Number(digits[i])` que retorna `NaN` para letras. **Mitigação**: CA12 exige paridade testada; trocar para `(charCodeAt − 48)`.
- **Risco — múltiplas cópias de `FormatarCnpj`**: há formatadores duplicados em 4 serviços de PDF + 2 utils de front. **Mitigação**: CA8 lista todos; preferência por extrair um helper único de formatação no back, se o dev julgar que reduz duplicação sem inflar escopo (cirurgia: só se for reuso limpo, não refactor especulativo).
- **Risco — PII vazar durante a janela de implementação**: enquanto a regex não for atualizada, um CNPJ alfanumérico vazaria em log/IA. **Mitigação**: CA9 é bloqueante; tratar PII junto com a feature, não depois.
- **Dependência — inspeção de schema (CA10)**: o dev aciona o `imedto-database` para a verificação do CHECK/collation antes do hand-off ao QA. Sem confirmação, assume-se "sem migration" mas o QA exige a evidência da inspeção.
- **Sem dependência de data de vigência**: decisão do usuário foi liberar no deploy, ambos os formatos aceitos. Nada bloqueia até 06/07/2026.

## 9. Observações para execução

**Não-negociável:**
- Normalização de CNPJ **dedicada** (novo método, ex.: `SomenteAlfanumericoUpper` no `CnpjValidator` ou helper próprio de CNPJ). **Nunca** tocar `TextSanitizer.SomenteDigitos` (genérico, usado por CPF/telefone/CEP).
- `CnpjValidator.CalcularDigito` já usa `digits[i] - '0'` (= ASCII − 48), que **já está correto** para letras — o núcleo do DV não muda. O que muda é a **normalização** (preservar `[A-Z0-9]` + upper) e as **guardas de formato** (12 primeiras alfanuméricas, 2 DVs numéricos).
- Front: `validateCnpj.ts` → `calcularDigito` deve usar `(c.charCodeAt(0) - 48)` em vez de `Number(digits[i])`; `apenasDigitos`/normalização deve preservar `[A-Z0-9]` e aplicar `.toUpperCase()`.
- DV permanece **numérico** em ambos os lados.
- 422 `BusinessException` é a fonte da verdade; máscara/validação de front é UX.
- LGPD: regex de PII em log e IA atualizada **na mesma entrega** (CA9). Mensagens de erro genéricas, sem PII.

**Liberdade técnica do dev:**
- Decidir se extrai um helper único de `FormatarCnpj` no back (reuso) ou ajusta cada serviço in-place — desde que todos os 4 serviços de PDF + 2 utils de front sejam cobertos por CA8 e não haja over-engineering.
- Escolher a configuração exata de tokens da lib `maska` para a máscara inteligente (12 alfanuméricos + 2 dígitos com uppercase), desde que o comportamento dos CA4/CA5/CA6 seja atendido.

**Pontos de mudança (do diagnóstico técnico — todos no código ativo; `ReferenciaLegado/` é ignorado):**

Backend:
- `backend/src/Services/Imedto.Backend.Domain/Inventario/Cadastros/CnpjValidator.cs` — `Normalizar()` preservar `[A-Z0-9]` + upper; manter validação de DV; garantir pos 13-14 dígitos.
- `backend/src/Services/Imedto.Backend.Domain/Estabelecimentos/Estabelecimento.cs` — `SomenteDigitos` privado (~linha 345), usado em `Criar` (~98) e `AtualizarDados` (~145); ajustar normalização e mensagem "CNPJ deve conter 14 dígitos".
- `backend/src/Core/Imedto.Backend.SharedKernel/Text/TextSanitizer.cs:11` `SomenteDigitos()` — **NÃO ALTERAR** (genérico de CPF/telefone/CEP). Criar normalização dedicada a CNPJ.
- Handlers: `CriarEstabelecimentoCommandHandler`, `AtualizarEstabelecimentoCommandHandler`, `FornecedorEstoqueHandlers` (criar+atualizar), `VerificarCnpjDisponivelQueryHandler` (tem cópia própria de `DigitosVerificadoresValidos`) — substituir `.Where(char.IsDigit)` pela normalização dedicada.
- Exibição: `QuestPdfReceitaService.cs`, `QuestPdfTermoService.cs`, `QuestPdfReciboPagamentoService.cs`, `TermoResolverDeVariaveis.cs` — cada um tem `FormatarCnpj` com `Where(char.IsDigit)`.
- PII: `backend/src/Services/Imedto.Backend.Infrastructure/Ia/PiiSanitizer.cs` e `backend/src/Services/Imedto.Backend.API/Logging/RemovePIIEnricher.cs` — `CnpjRegex` passa a aceitar `[A-Z0-9]` nas 12 primeiras + `\d{2}` no DV.

Frontend:
- `frontend/src/utils/validateCnpj.ts` — `apenasDigitos`, `validateCnpj`, `validateCnpjObrigatorio`, `formatarCnpj`, `calcularDigito` (trocar `Number(digits[i])` por `charCodeAt − 48`); preservar `[A-Z0-9]` + upper.
- `frontend/src/utils/validateCnpj.test.ts` — adicionar casos alfanuméricos (válido, inválido por DV, numérico de regressão).
- `frontend/src/views/auth/OnboardingView.vue` e `frontend/src/views/estabelecimento/EstabelecimentoView.vue` — máscara inteligente; remover/ajustar `cnpjFormatoValido` inline duplicado do Onboarding (preferir reuso do util).
- `frontend/src/components/estoque/cadastros/CadastroFornecedoresTab.vue` e `frontend/src/components/estoque/cadastros/modais/ModalNovoFornecedorRapido.vue` — usar a normalização/validação atualizada.
- `frontend/src/composables/usePdfHeader.ts:160` `formatarCnpj` (`replace(/\D/g)`) — preservar `[A-Z0-9]`.
- `frontend/src/utils/termoResolverVariaveis.ts` — espelho do resolver.

**Exemplos canônicos para os testes (validados pelo algoritmo oficial):**
- Alfanumérico **válido**: `12.ABC.345/01DE-35` → canônico `12ABC34501DE35` (base `12ABC34501DE`, DV `35`).
- Alfanumérico **inválido** (DV errado): `12.ABC.345/01DE-34` (DV correto é `35`).
- Numérico **válido** (retrocompatibilidade): `11.222.333/0001-81` → canônico `11222333000181`.

## 10. Atualização de documentação

Docs a atualizar **na mesma entrega** (cirúrgico, só as seções afetadas):

- **`Docs/LGPD.md`** — na seção de redação de PII / regex, atualizar a descrição do padrão de CNPJ para o formato alfanumérico (`[A-Z0-9]` nas 12 primeiras + `\d{2}` no DV), registrando que log e prompt de IA mascaram o novo formato. **Obrigatório** (regra cross-cutting de PII).
- **`Docs/ARQUITETURA.md`** — adicionar nota curta sobre **normalização dedicada de CNPJ** (preserva `[A-Z0-9]` + uppercase, distinta do `TextSanitizer.SomenteDigitos` genérico usado por CPF/telefone/CEP) e que o DV permanece numérico (módulo-11, valor = ASCII − 48). Contextualiza a IN RFB 2.229/2024.
- **`Docs/DESIGN.md`** — **sem alteração**: não há componente novo de design system; apenas troca de diretiva de máscara em campos existentes.
- **`Docs/INFRA.md`** / **`Docs/COMANDOS.md`** — **sem alteração** (sem recurso AWS novo, sem comando novo). Migration só se a inspeção do CA10 revelar `CHECK` restritivo — nesse caso o `imedto-database` segue o fluxo padrão de migration já documentado, sem novo padrão a registrar.
