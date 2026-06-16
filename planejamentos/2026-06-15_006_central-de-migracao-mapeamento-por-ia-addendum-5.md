# Central de Migração — Resiliência da inferência por IA: retry/backoff, truncamento de amostra e degradação graciosa por bloco (addendum 5)

**ID**: 2026-06-15_006
**Refere-se a**: 2026-06-15_001_central-de-migracao-mapeamento-por-ia.md (e aos addendums 2026-06-15_002, 2026-06-15_003, 2026-06-15_004, 2026-06-15_005)
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (retry/backoff no adapter de IA + espaçamento entre blocos + truncamento de valor na amostra + degradação por bloco no handler de inferência + extensão do Reprocessar para pular blocos OK — toca **só** o adapter `AnthropicMapeadorDeMigracao` e o `InferirMapaMigracaoJobHandler`, dentro do bounded context de migração)
**Áreas regressivas tocadas**: nenhuma de domínio (paciente/estoque/orçamento/prontuário/agenda intactos) — toca **só** a fase de **inferência** do bounded context de migração (adapter de IA + handler de inferência + estado do job). Risco de regressão contido no fluxo de inferência dos addendums 001–005 — coberto por CA101. **Sem mudança de schema.**

> O épico Central de Migração (2026-06-15_001, CA1–CA24) e os addendums 002 (CA25–CA39), 003 (CA40–CA50), 004 (CA51–CA69) e 005 (CA70–CA85) estão entregues em `main`. Este addendum **fecha uma fragilidade de resiliência observada em produção (job #12)**: a inferência de um dump aninhado com ~10+ blocos faz **1 chamada de IA por bloco em sequência**, e a Anthropic retornou **429 (TooManyRequests)** no ~6º bloco — o handler lançava exceção e marcava o **job inteiro** como `falhou`, **perdendo os blocos já mapeados com sucesso** (pacientes/agendamentos/atendimentos vieram 200 OK). O briefing original e os addendums anteriores permanecem **intocados** e seus CAs continuam válidos. Os CAs deste addendum começam em **CA86** (o addendum 005 terminou em CA85).

---

## 1. Contexto e motivação

**Problema real observado — job #12 (confirmado em logs de produção).** A inferência de mapa de um dump JSON aninhado com ~10+ blocos dispara **1 chamada de IA por bloco em sequência** (D-N2 do addendum 004). No job #12, a Anthropic retornou **429 (TooManyRequests)** no ~6º bloco (`entradas`) — saturação de TPM (tokens-por-minuto) de uma conta de tier baixo, agravada por blocos com valores enormes (HTML/base64 em `conteudo_html`). O `AnthropicMapeadorDeMigracao` propagou a falha como exceção, o `InferirMapaMigracaoJobHandler` capturou e chamou `MarcarFalhou(...)` — e o **job inteiro** caiu para `falhou`, **descartando os 5 blocos que já tinham mapeado com 200 OK** (pacientes, agendamentos, atendimentos vieram OK antes do 429).

**Stack confirmado:**
- `AnthropicMapeadorDeMigracao.cs:92` — `client.SendAsync(...)` retorna 429; o adapter checa `!response.IsSuccessStatusCode` e **lança `InvalidOperationException`** sem qualquer retry.
- `InferirMapaMigracaoJobHandler.cs:242` — captura a exceção e marca o job inteiro como falhou; o loop de blocos morre na primeira falha, sem preservar os blocos bons.

**Três fragilidades confirmadas:**

1. **Zero resiliência a 429/529 no adapter.** Qualquer status não-2xx vira exceção imediata — sem respeitar `Retry-After`, sem backoff, sem distinguir transitório (429/overloaded) de permanente (401/403 chave inválida). O `ResendEmailService` **já tem** o padrão de retry correto (backoff exponencial, 4xx≠429 = permanente); o adapter de IA não o seguiu.
2. **Falha de UM bloco derruba o JOB inteiro.** A degradação não é graciosa: um único 429 no 6º de 10 blocos perde os 5 já mapeados. Os blocos bons deveriam ser preservados.
3. **Valores gigantes saturam o TPM.** Campos como `conteudo_html` (HTML de atendimento) ou base64 entram na amostra **inteiros**, inflando os tokens por chamada e empurrando a conta para o limite de TPM. A amostra é mascarada (D2), mas o **tamanho** de cada valor não é limitado.

**Demanda do usuário (decisões firmes, já aprovadas):** tornar a inferência **resiliente** a limites de taxa e sobrecarga do provider — retentar com backoff respeitando `Retry-After`, espaçar as chamadas entre blocos, truncar valores gigantes na amostra, e **nunca** perder o trabalho já feito: falha de um bloco vira **erro daquele bloco** (operador reprocessa só ele), não falha do job — exceto quando **nenhum** bloco mapear.

**Princípios preservados (não-negociáveis):**
- **D1/D2 do épico intactos** — a IA continua mapeando **schema com amostra mascarada**, **nunca** registro a registro. O truncamento (D-R3) é aplicado **sobre o valor já mascarado**, só limita tamanho; não muda o que vai à IA além de cortar comprimento.
- **D11 intacto** — nada de id externo; este addendum não toca vínculo.
- **Ports & adapters intacto** — o retry/backoff vive **no adapter** (`AnthropicMapeadorDeMigracao`, Infrastructure); o domínio/handler não conhece status HTTP, `Retry-After` nem SDK. Espelha `ResendEmailService`.
- **Humano no loop intacto** — gate de aprovação (addendum 003) **antes** da IA; revisão do mapa **depois**. O reprocessamento parcial é **manual** (o operador decide), reusando o `Reprocessar` do addendum 002/003.

**Benefício de negócio:** desbloqueia a migração de clientes em contas de tier baixo da Anthropic (o caso comum no início), e elimina o desperdício de re-inferir blocos que já mapearam corretamente — economia de tokens e de tempo do operador. Sem isso, qualquer dump grande em conta tier baixo falha por inteiro e o operador refaz tudo.

## 2. Persona-alvo

- **Operador Imedto (admin da plataforma)**, painel `modules/admin`. Ganha: a inferência aguenta picos de 429/sobrecarga sem perder trabalho; quando um bloco realmente falha (após esgotar retry), ele vê **qual** bloco e **por quê** (motivo genérico), e **reprocessa só os pendentes** com um clique — sem refazer os blocos bons. Frequência: recorrente, sempre que o dump é grande ou a conta de IA é tier baixo.
- **Cliente (dono/admin do estabelecimento)**: inalterado — sobe o ZIP e acompanha o status; não sabe de retry nem de blocos.

## 3. Escopo

**Inclui:**

**Bloco A — Retry com backoff no adapter de IA (espelha `ResendEmailService`):**
- Ao receber **429 (TooManyRequests)** ou **529 (overloaded)** ou **falha transitória de rede** (timeout/`HttpRequestException`) da Anthropic, o `AnthropicMapeadorDeMigracao` **aguarda e retenta**: respeita o header **`Retry-After`** quando presente; senão **backoff exponencial** (~1s inicial) **com jitter**, **teto de 5 tentativas**. Só falha o bloco **após esgotar** as tentativas. **4xx que não 429** (401/403 — chave inválida/sem permissão) é **permanente** — **não** retenta (não adianta).

**Bloco B — Espaçamento fixo entre blocos:**
- A inferência **já** chama os blocos em sequência (não paralelo). Adicionar uma **pausa fixa ~1s entre blocos** (configurável) para não saturar o TPM de conta tier baixo. (Nota técnica a confirmar pelo dev: o mapeador **não** passa pelo `RateLimitedIaService` — por isso o espaçamento entra explicitamente aqui.)

**Bloco C — Truncamento de valor na amostra:**
- Cada **valor de campo** da amostra **já mascarada** é truncado a **500 caracteres** (com marcador `…[truncado]`) **antes** de ir à IA. Mata o estouro de TPM causado por `conteudo_html`/base64. Mantém D1/D2 (amostra mascarada, sem PII) — o truncamento só limita comprimento de cada valor, depois da máscara.

**Bloco D — Degradação graciosa por bloco:**
- Falha de IA em **UM** bloco (após esgotar retry) **NÃO derruba o job**. O bloco vira **mapa de erro**: marcado no `mapa_json` com `bloco_com_erro: true` + um motivo genérico **sem PII** (categoria fixa — ex.: `limite_taxa_ia`, `provider_indisponivel`, `falha_classificacao`). Os blocos que **mapearam** são preservados. O job vai para **`mapa_em_revisao`** com um **aviso** de quantos/quais blocos falharam. O operador **reprocessa só os pendentes** OU **ignora** esses blocos e segue.
- **Só falha o job inteiro (`falhou`) se NENHUM bloco mapear** (zero sucesso) — ex.: chave inválida (401) derruba todos.

**Bloco E — Reprocessar parcial não re-chama IA nos blocos OK:**
- O upsert por `(jobId, entidade, nomeBlocoOrigem)` (addendum 004) **já** preserva os blocos bons persistidos. Estender a inferência para, ao rodar de novo, **pular a chamada de IA** dos blocos com mapa **bem-sucedido** já persistido — só blocos com **erro/pendentes** voltam à IA. Reusa o `Reprocessar` do addendum 002/003 (volta o job a `aguardando_mapa`, o scheduler re-seleciona).

**Não inclui (backlog / fora deste addendum):**

- **Fila/rate-limiter global de IA (`RateLimitedIaService` aplicado ao mapeador).** O espaçamento fixo (Bloco B) é o mínimo que serve; integrar o mapeador a um rate-limiter central é frente própria. Backlog.
- **Retry automático do job inteiro sem ação do operador.** O reprocessamento parcial é **manual** (decisão do operador, reusando o `Reprocessar`). Auto-retry do job é backlog — manteria o humano fora do loop de custo.
- **Paralelização das chamadas de blocos.** Continua sequencial (com espaçamento). Paralelizar aumentaria a pressão de TPM — contrário ao objetivo. Fora de escopo.
- **Resumo/chunking inteligente de valores gigantes** (ex.: enviar só um trecho representativo do HTML). O truncamento simples a 500 chars é o mínimo que serve; chunking semântico é backlog.
- **Backoff/retry no provider de carga** (commands de domínio). Este addendum é **só** sobre a **inferência** (chamadas à IA). A carga não muda.

## 4. Decisões de produto (fechadas — já aprovadas pelo usuário)

> Padrão conservador: reuso do padrão `ResendEmailService`, LGPD-safe, mínimo viável que serve, **nunca** perder trabalho já feito, **nunca** violar D1/D2/D11.

### D-R1 — Retry com backoff no adapter (espelha `ResendEmailService`)

Ao receber **429** ou **529/overloaded** ou **falha transitória de rede** da Anthropic, o adapter aguarda e retenta: respeita **`Retry-After`** quando presente; senão **backoff exponencial ~1s inicial com jitter**, **teto de 5 tentativas**. Só falha o bloco após esgotar. **4xx≠429 (401/403) = permanente, não retenta.** **Por quê:** é exatamente o padrão já provado em `ResendEmailService` (4xx≠429 permanente; 5xx/429/timeout → retry com backoff exponencial). Resiliência a limite de taxa é responsabilidade do **adapter** (ports & adapters); o handler não conhece status HTTP. `Retry-After` é o sinal autoritativo do provider — respeitá-lo é mais educado e eficaz que adivinhar o backoff. Teto de 5 evita loop infinito num provider persistentemente indisponível.

### D-R2 — Espaçamento fixo entre blocos (~1s, configurável)

Entre uma chamada de bloco e a próxima, **pausa fixa ~1s** (configurável). Os blocos já são sequenciais; o espaçamento reduz a pressão de TPM em conta tier baixo. **Por quê:** o mapeador **não** passa pelo `RateLimitedIaService` (a confirmar pelo dev), então não há throttle central. Uma pausa fixa é o mínimo que serve para não rajadar o provider — barato, determinístico, sem nova infra. Configurável para ajustar conforme o tier da conta.

### D-R3 — Truncar valor na amostra a 500 caracteres

Cada valor de campo da amostra **já mascarada** é truncado a **500 caracteres** (marcador `…[truncado]`) antes de ir à IA. **Por quê:** valores como `conteudo_html` (HTML de atendimento) e base64 inflam os tokens por chamada e estouram o TPM — foi um agravante do 429 no job #12. A IA classifica entidade e mapeia colunas pelo **schema** (nomes de cabeçalho) e por uma **amostra representativa** dos valores; 500 chars por valor são mais que suficientes para isso. Mantém D1/D2: a máscara de PII roda **antes**; o truncamento só corta comprimento, não revela nada novo nem muda a natureza da amostra.

### D-R4 — Degradação graciosa por bloco (falha de bloco ≠ falha de job)

Falha de IA em um bloco (após esgotar retry) **não** derruba o job. O bloco vira **mapa de erro** (`bloco_com_erro: true` + motivo genérico sem PII no `mapa_json`); os blocos que mapearam são **preservados**; o job vai para **`mapa_em_revisao`** com aviso de quantos/quais blocos falharam; o operador **reprocessa só os pendentes** OU **ignora**. **Só falha o job inteiro (`falhou`) se NENHUM bloco mapear (zero sucesso).** **Por quê:** o desperdício do job #12 (perder 5 blocos bons por causa do 6º) é inaceitável — trabalho de IA já pago e correto não pode ser jogado fora. Tratar o bloco como unidade de falha (não o job) espelha a cardinalidade "1 job → N mapas" (addendum 004): cada mapa tem seu destino independente. Falhar o job só faz sentido quando **nada** deu certo (ex.: 401 chave inválida atinge todos). O operador no loop decide o que fazer com os pendentes.

### D-R5 — Reprocessar parcial pula blocos OK (não re-chama IA neles)

Ao reprocessar (manual, reusando addendum 002/003), a inferência **pula a chamada de IA** dos blocos com mapa **bem-sucedido** já persistido (via upsert `(jobId, entidade, nomeBlocoOrigem)` do addendum 004) — só blocos com **erro/pendentes** voltam à IA. **Por quê:** re-inferir blocos que já mapearam com sucesso é gastar tokens à toa e arriscar reintroduzir o 429. O upsert já preserva os blocos bons; basta a inferência **detectar** mapa bem-sucedido persistido e **não rechamar** a IA nele. Economia de custo e de tempo do operador, sem nova infra — só estende o `Reprocessar` existente.

## 5. Regras de negócio

**Bloco A — Retry/backoff:**

- **R-R1 (retry transitório com `Retry-After`/backoff+jitter, teto 5 — D-R1)**: O `AnthropicMapeadorDeMigracao`, ao receber **429**, **529/overloaded** ou **falha transitória de rede** (timeout/`HttpRequestException`), **retenta**: se houver header `Retry-After`, aguarda o tempo indicado; senão aplica **backoff exponencial (~1s inicial) com jitter**. **Teto de 5 tentativas**; após esgotar, propaga falha **do bloco** (não do job). Mora em: adapter de IA (Infrastructure). Validada em: back. Espelha `ResendEmailService`.

- **R-R2 (4xx≠429 é permanente — não retenta — D-R1)**: Status **401/403** (e demais 4xx exceto 429/408) são **permanentes**: o adapter **não** retenta e propaga falha imediata. Mora em: adapter de IA. Validada em: back.

**Bloco B — Espaçamento:**

- **R-R3 (pausa fixa entre blocos — D-R2)**: A inferência aguarda uma **pausa fixa configurável (~1s)** entre o fim de uma chamada de bloco e o início da próxima. Não paraleliza. Mora em: handler de inferência (Application). Validada em: back.

**Bloco C — Truncamento:**

- **R-R4 (valor truncado a 500 chars após máscara — D-R3)**: Cada valor de campo da amostra é truncado a **500 caracteres** (marcador `…[truncado]`) **após** a máscara de PII e **antes** da chamada ao provider. D1/D2 preservados (mascara antes; truncamento só limita comprimento). Mora em: montagem da amostra no handler de inferência (após `PiiSanitizer`/`IAnonimizacaoService`). Validada em: back (verificável no payload).

**Bloco D — Degradação por bloco:**

- **R-R5 (falha de bloco vira mapa de erro, não falha de job — D-R4)**: Quando a IA falha num bloco após esgotar retry, a inferência **persiste um mapa de erro** para esse bloco (`bloco_com_erro: true` + motivo genérico **sem PII** no `mapa_json`) e **continua** com os demais blocos. Os blocos bem-sucedidos são **preservados**. Mora em: handler de inferência. Validada em: back.

- **R-R6 (job só falha se zero sucesso — D-R4)**: Após processar todos os blocos, se **pelo menos um** bloco mapeou com sucesso, o job vai para **`mapa_em_revisao`** com aviso de blocos falhos; se **nenhum** bloco mapeou, o job vai para **`falhou`** (reusa `MarcarFalhou` do addendum 002, com motivo genérico). Mora em: handler de inferência. Validada em: back.

- **R-R7 (aviso de blocos falhos na revisão — D-R4)**: O painel de revisão exibe, por bloco com erro, um aviso ("não foi possível classificar este bloco — limite de taxa da IA / provider indisponível") e a opção de **reprocessar** os pendentes ou **ignorar**. O operador nunca é forçado a aceitar um bloco de erro. Mora em: estado do job + UI admin (reusa a revisão dos addendums 004/003). Validada em: back + front.

**Bloco E — Reprocessar parcial:**

- **R-R8 (reprocessar pula blocos OK — D-R5)**: Ao reprocessar (manual, via `Reprocessar` do addendum 002/003), a inferência **detecta** os blocos com mapa **bem-sucedido** já persistido (upsert `(jobId, entidade, nomeBlocoOrigem)` — addendum 004) e **não rechama a IA** neles; só blocos com erro/pendentes voltam à IA. Mora em: handler de inferência + repositório de mapas. Validada em: back.

## 6. Modelo de dados

**Sem mudança de schema.** Nenhuma tabela `migracao_*` nova; nenhuma coluna nova.

- **`bloco_com_erro` e o motivo do erro vivem no `mapa_json`** de `migracao_mapas` (campos JSON novos — `bloco_com_erro: bool`, `motivo_erro: string` categoria genérica). É a mesma estratégia "campos no `mapa_json`, sem migration" já adotada no addendum 004 para `entidade_classificada`/`encoding_suspeito`/`ignorado`. Sem `ALTER TABLE`.
- **Reprocessar parcial** reusa o upsert `(migracao_job_id, entidade, nome_bloco_origem)` (unique constraint já criada no addendum 004 — `20260615160000_migracao_mapas_nome_bloco_origem`). A detecção de "bloco já mapeado com sucesso" é leitura do `mapa_json` (`bloco_com_erro != true`).
- **Estado do job** reusa os estados existentes: `aguardando_mapa` → `mapa_em_revisao` (sucesso parcial ou total) ou `falhou` (zero sucesso). `MarcarFalhou`/`Reprocessar` do addendum 002 inalterados.

**Audit / LGPD:** o motivo do erro é **categoria genérica sem PII** (ex.: `limite_taxa_ia`, `provider_indisponivel`, `falha_classificacao`) — mesma garantia do `MotivoFalha` (addendum 002) e do `motivo_rejeicao` (addendum 002). O truncamento de valor roda **após** a máscara — nenhum PII a mais vai à IA nem ao log. Nenhum dado de paciente é tocado.

**Confirmar com `imedto-database`:** que `bloco_com_erro`/`motivo_erro` cabem no `mapa_json` sem migration (default do BA: cabem — JSON). Se o painel exigir filtro por coluna (improvável), avaliar coluna leve idempotente. Sem migration por padrão.

## 7. UX e fluxo

**Fluxo-alvo (estende a inferência/revisão dos addendums 003/004/005):**

1. Cliente sobe ZIP → gate de aprovação (addendum 003) inalterado.
2. Operador aprova → job em `aguardando_mapa`.
3. O recorrente `inferir-mapa-migracao` decompõe em blocos (addendum 004) e, **por bloco**, chama a IA com:
   - amostra **mascarada** (addendum 004) **e truncada a 500 chars/valor** (R-R4);
   - **retry com backoff/`Retry-After`** em 429/529/rede (R-R1), permanente em 4xx≠429 (R-R2);
   - **pausa ~1s entre blocos** (R-R3).
4. Se um bloco **falha após retry** → vira **mapa de erro** (R-R5), a inferência **continua** os demais.
5. Ao fim: **≥1 sucesso** → `mapa_em_revisao` com aviso de falhos (R-R6/R-R7); **zero sucesso** → `falhou` (R-R6).
6. Operador na revisão: vê os blocos OK normalmente + os blocos de erro com aviso e botão **reprocessar pendentes** ou **ignorar**. Reprocessar **não rechama IA** nos blocos OK (R-R8).
7. Preview → "Migrar" → carga → relatório → desfazer: **inalterados**.

**Detalhe / revisão (`MigracaoRevisaoView`):**
- Bloco com erro: faixa/aviso ("Não foi possível classificar este bloco — limite de taxa da IA. Reprocesse os pendentes ou ignore.") + ação **Reprocessar pendentes** (reusa o `Reprocessar` do addendum 003) + opção **Ignorar**.
- Banner agregado no topo quando há blocos falhos ("N de M blocos não foram classificados — você pode reprocessar só os pendentes").
- Estados: loading, bloco OK (como hoje), bloco com erro destacado, bloco ignorado (colapsado). Reusa o padrão de confiança/dúvidas/`AppBadge` já existente. Tipografia **por tokens** (CLAUDE.md §5).

**Lado do cliente:** inalterado — só acompanha o status.

**Service/DTO front:** o DTO do mapa por bloco ganha `blocoComErro: bool` e `motivoErro: string` (categoria genérica). A ação de **reprocessar** reusa o endpoint/serviço do `Reprocessar` (addendum 003). Liberdade técnica na forma, desde que os CAs passem.

## 8. Critérios de aceite (testáveis) — começam em CA86

**Bloco A — Retry/backoff (D-R1):**

- **CA86 (retry em 429 com backoff+jitter, sucesso na retentativa — R-R1)**: Dado que a Anthropic retorna **429** na 1ª chamada de um bloco e **200 OK** na 2ª, Quando a inferência roda esse bloco, Então o adapter **aguarda e retenta** (backoff ~1s com jitter, sem `Retry-After`), obtém o mapa na 2ª tentativa, e o bloco é classificado/mapeado normalmente — **sem** marcar o bloco nem o job como falho.

- **CA87 (respeita `Retry-After` quando presente — R-R1)**: Dado que a resposta 429 traz header **`Retry-After: 2`**, Quando o adapter retenta, Então ele aguarda **~2s** (o valor do header) antes da próxima tentativa, em vez do backoff calculado; e procede com a retentativa.

- **CA88 (4xx≠429 é permanente, não retenta — R-R2)**: Dado que a Anthropic retorna **401** (chave inválida), Quando a inferência chama o bloco, Então o adapter **não** retenta (zero tentativas extras) e propaga falha imediata desse bloco (que, se atingir todos os blocos, leva o job a `falhou` por zero sucesso — CA95).

- **CA89 (teto de 5 tentativas — R-R1)**: Dado que a Anthropic retorna **429** em todas as tentativas, Quando a inferência roda o bloco, Então o adapter tenta **no máximo 5 vezes** (não infinito) e, após esgotar, o bloco vira mapa de erro (CA92) — não trava o job.

**Bloco B — Espaçamento (D-R2):**

- **CA90 (pausa fixa entre blocos — R-R3)**: Dado um dump com 4 blocos-candidatos, Quando a inferência os processa em sequência, Então há uma **pausa fixa ~1s** (configurável) entre o fim de uma chamada de bloco e o início da próxima — as chamadas **não** são disparadas em rajada nem em paralelo (verificável no espaçamento temporal/contagem sequencial).

**Bloco C — Truncamento (D-R3):**

- **CA91 (valor truncado a 500 chars após máscara — R-R4)**: Dado um bloco cujo campo `conteudo_html` tem 50.000 caracteres, Quando a amostra é montada e enviada à IA, Então o valor desse campo no payload ao provider tem **no máximo 500 caracteres** (com marcador `…[truncado]`), e a máscara de PII (addendum 004) foi aplicada **antes** do truncamento — o volume e o PII real nunca vão ao provider.

- **CA92-truncamento-LGPD coberto por CA99.**

**Bloco D — Degradação graciosa (D-R4):**

- **CA92 (falha de bloco após retry vira mapa de erro, não derruba job — R-R5)**: Dado um dump com 10 blocos onde o 6º recebe **429 persistente** (esgota as 5 tentativas), Quando a inferência roda, Então o 6º bloco é persistido como **mapa de erro** (`bloco_com_erro: true` + motivo genérico no `mapa_json`), e a inferência **continua** processando os blocos 7–10 — o job **não** vai para `falhou` por causa dele.

- **CA93 (blocos bem-sucedidos preservados — R-R5/D-R4)**: Dado o mesmo dump do CA92 onde os blocos 1–5 e 7–10 mapearam com 200 OK, Quando o 6º falha, Então os **9 blocos OK permanecem persistidos** com seus mapas — nenhum mapa bom é descartado (corrige o bug do job #12).

- **CA94 (≥1 sucesso → `mapa_em_revisao` com aviso — R-R6/R-R7)**: Dado um dump onde ao menos 1 bloco mapeou e ao menos 1 falhou, Quando a inferência termina, Então o job vai para **`mapa_em_revisao`**, e o painel exibe um **aviso** de quantos/quais blocos falharam, com opção de reprocessar os pendentes ou ignorar.

- **CA95 (zero sucesso → `falhou` — R-R6)**: Dado um dump onde **nenhum** bloco mapeou (ex.: 401 chave inválida em todos), Quando a inferência termina, Então o job vai para **`falhou`** (reusa `MarcarFalhou` do addendum 002, motivo genérico sem PII) — pois não há nada para revisar.

- **CA96 (operador não é forçado a aceitar bloco de erro — R-R7)**: Dado um job em `mapa_em_revisao` com um bloco de erro, Quando o operador revisa, Então ele pode **ignorar** o bloco de erro e seguir com os blocos OK, **OU** reprocessar os pendentes — nunca é obrigado a mapear o bloco que falhou.

**Bloco E — Reprocessar parcial (D-R5):**

- **CA97 (reprocessar não re-chama IA nos blocos OK — R-R8)**: Dado um job em `mapa_em_revisao` com 9 blocos OK e 1 bloco de erro, Quando o operador aciona **reprocessar**, Então a inferência re-roda e chama a IA **apenas para o 1 bloco pendente** — os 9 blocos OK **não** geram nova chamada ao provider (verificável: 1 chamada de IA, não 10).

- **CA98 (reprocessar reusa o Reprocessar do addendum 002/003 — R-R8)**: Dado o reprocessamento parcial, Quando o operador o aciona, Então ele usa o `Reprocessar` existente (job volta a `aguardando_mapa`, o scheduler recorrente re-seleciona) — **sem** nova infra de retry de job; o upsert `(jobId, entidade, nomeBlocoOrigem)` (addendum 004) garante que os blocos OK não duplicam.

**LGPD, multi-tenant, RBAC e regressão:**

- **CA99 (LGPD — truncamento e motivo de erro sem PII)**: Dado qualquer bloco truncado ou com erro, Quando a amostra vai à IA e o motivo de erro é persistido/exibido, Então (a) o truncamento ocorre **após** a máscara de PII — nenhum PII a mais vai ao provider; (b) o `motivo_erro` é **categoria genérica** (`limite_taxa_ia`/`provider_indisponivel`/`falha_classificacao`), **nunca** CPF/nome/valor real; (c) **nenhum log** carrega PII (o adapter loga só status + hint do bloco, como hoje).

- **CA100 (multi-tenant — herdado)**: Dado um job de migração, Quando os mapas (OK e de erro) são persistidos e lidos no painel, Então tudo carrega o `estabelecimento_id` do job; um operador/job de outro contexto recebe "não encontrado" genérico; nenhum dado de tenant alheio vaza.

- **CA101 (RBAC + regressão — fluxo dos addendums 001–005 intacto)**: Dado um usuário sem a policy **`ImedtoAdmin`**, Quando tenta reprocessar/revisar, Então recebe **403** e os controles ficam ocultos no front (reuso da policy do `AdminMigracaoController`). E dado o fluxo completo dos briefings 001–005 (upload → aprovação CA44 → decomposição de dump aninhado CA70-72 → classificação por IA CA73-76 → encoding CA80-81 → revisão → preview → carga por commands → upsert por chave de negócio R2 → ordem de FK R5 → relatório com motivos addendum 002 → timeline/progresso/filtros CA51-69), Quando roda com a inferência resiliente, Então **nada regride**: a carga continua por commands, o gate anti-IA (CA44) e o upsert (addendum 004) seguem valendo, e o caminho feliz (sem 429) produz o **mesmo** resultado de antes — o retry/truncamento/degradação só atuam em falha/sobrecarga.

## 9. Riscos e dependências

- **Dependência externa — chave/tier da Anthropic (risco aceito, não-CA).** A conta de IA é dependência externa: o código fica resiliente (retry, espaçamento, truncamento, degradação por bloco), **mas** uma conta de **tier muito baixo** ainda pode degradar — vários blocos caindo em erro mesmo após retry, se o TPM for insuficiente para o dump. Mitigado por: degradação graciosa (o operador reprocessa em lotes), espaçamento configurável (subir o intervalo em tier baixo) e truncamento (menos tokens/chamada). **Registrado como risco de operação, não resolvível só por código.** A médio prazo: subir o tier da conta, ou (backlog) integrar o `RateLimitedIaService` ao mapeador.
- **Regressão do caminho feliz**: o retry/truncamento/degradação **não pode** mudar o resultado quando não há 429. **CA101 é o guardião** — caminho sem falha produz o mesmo mapa de antes. Risco: truncamento agressivo demais cortar informação que a IA usava para classificar — mitigado por 500 chars (folga ampla para schema + amostra representativa) e pelo operador no loop.
- **`Retry-After` malformado/ausente**: o adapter deve cair no backoff calculado se o header faltar ou for inválido (não quebrar). Coberto por R-R1/CA86.
- **Espaçamento × tempo total da inferência**: pausa entre blocos aumenta o tempo total de um dump com muitos blocos. Aceitável (a inferência é assíncrona via scheduler recorrente; o operador não espera síncrono). Configurável para ajustar.
- **Confirmar (nota técnica do dev)**: que o mapeador **não** passa pelo `RateLimitedIaService` hoje — se passar, o espaçamento (Bloco B) pode ser redundante e o dev deve evitar dupla pausa.
- **Área regressiva**: nenhuma de domínio. Risco contido na inferência (adapter de IA + handler). Commands de paciente/estoque/orçamento/prontuário **não** são tocados. **Sem migration.**

## 10. Observações para execução

- **Reuso obrigatório (não construir do zero)**: o retry/backoff **espelha `ResendEmailService`** (`backend/src/Services/Imedto.Backend.Infrastructure/Email/ResendEmailService.cs` — `TentativasMax`, `BaseBackoffMs`, loop `for` com `Task.Delay(backoff exponencial)`, 4xx≠429 permanente). Aplicar o **mesmo padrão** no `AnthropicMapeadorDeMigracao` (apenas: teto 5, base ~1s, **+ jitter**, **+ respeitar `Retry-After`**, **+ 529/overloaded como transitório**). A degradação por bloco e o reprocessar parcial reusam o upsert `(jobId, entidade, nomeBlocoOrigem)` (addendum 004) e o `Reprocessar`/`MarcarFalhou` (addendum 002/003). A revisão reusa `MigracaoRevisaoView` + `AppBadge`. **Nada de nova tabela, nova porta, nem novo scheduler.**
- **Não-negociável (ports & adapters)**: o retry/backoff/`Retry-After` vive **no adapter** (`AnthropicMapeadorDeMigracao`, Infrastructure). O handler de inferência e o domínio **não** conhecem status HTTP nem `Retry-After`. O handler só vê "este bloco mapeou" ou "este bloco falhou".
- **Não-negociável (D1/D2 preservados)**: o truncamento (R-R4) roda **após** a máscara de PII (`PiiSanitizer`/`IAnonimizacaoService`) — nunca antes. A IA continua recebendo só amostra mascarada; o truncamento só corta comprimento.
- **Não-negociável (nunca perder trabalho)**: falha de um bloco **nunca** descarta os blocos bons (R-R5/CA93). Job só falha com zero sucesso (R-R6/CA95).
- **Não-negociável (humano no loop)**: reprocessamento parcial é **manual** (reusa `Reprocessar` — addendum 002/003); sem auto-retry do job. Gate de aprovação (addendum 003) e revisão (addendum 004) intactos.
- **Não-negociável (LGPD)**: motivo de erro é categoria genérica sem PII (CA99); log do adapter loga só status + hint do bloco (como hoje); nenhum PII em log/mensagem.
- **Liberdade técnica**: a fórmula exata do jitter (full jitter vs. equal jitter); o valor base do backoff (~1s) e o intervalo de espaçamento entre blocos (~1s) — **configuráveis** (constantes ou `appsettings`); o limite de truncamento (500 chars — fixo no CA); a categoria exata dos motivos de erro (desde que genérica/sem PII); a forma de detectar "bloco já mapeado com sucesso" no reprocessar (ler `mapa_json.bloco_com_erro != true`). Tudo desde que respeite os CAs.
- **Acionar `imedto-database`**: apenas para **confirmar** que `bloco_com_erro`/`motivo_erro` cabem no `mapa_json` sem migration (default: cabem — JSON, como no addendum 004). **Sem migration esperada.** Se (improvável) o painel exigir filtro por coluna, migration idempotente (gotcha `AddColumn`).
- **Fatiamento sugerido** (PRs pequenos sob este mesmo ID): PR 1 = Bloco A (retry/backoff/`Retry-After`/jitter no adapter) + Bloco C (truncamento) — CA86-89, CA91, CA99. PR 2 = Bloco B (espaçamento) + Bloco D (degradação por bloco no handler) — CA90, CA92-96. PR 3 = Bloco E (reprocessar parcial) — CA97-98. Regressão (CA101) validada em todos.

## 11. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — **atualizar nesta entrega**: na seção do bounded context de Migração (após "Classificação de entidade por IA"), adicionar nota incremental de **resiliência da inferência**: (1) o `AnthropicMapeadorDeMigracao` faz **retry com backoff exponencial + jitter, respeitando `Retry-After`**, em 429/529/falha de rede (teto 5 tentativas); 4xx≠429 é permanente — **espelha `ResendEmailService`**; (2) **pausa fixa ~1s entre blocos** (o mapeador não passa pelo `RateLimitedIaService`); (3) **truncamento de valor a 500 chars após a máscara de PII** (corta `conteudo_html`/base64); (4) **degradação graciosa por bloco** — falha de bloco vira mapa de erro (`bloco_com_erro` no `mapa_json`), job só vai a `falhou` com **zero sucesso**, senão `mapa_em_revisao` com aviso; reprocessar parcial pula blocos OK. **Feito nesta entrega pelo BA.**
- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — **atualizar nesta entrega**: nota incremental nova (não reescrever D1–D14) registrando que a inferência por IA ganhou **resiliência** (retry/backoff/`Retry-After`, espaçamento entre blocos, truncamento de amostra, degradação graciosa por bloco — falha de bloco ≠ falha de job), mantendo D1/D2 (schema + amostra mascarada) e D11 (sem id externo) intactos; risco residual de conta de IA tier baixo registrado como dependência externa. **Feito nesta entrega pelo BA.**
- **`Docs/LGPD.md`** — sem mudança material: a garantia de não-PII na amostra (máscara antes do truncamento) e o motivo de erro como categoria genérica seguem a regra geral já documentada. **Não atualizar** (coberto pela regra geral).
- **`Docs/INFRA.md`** — **dependência externa registrada (não-bloqueante)**: a **chave de IA da Anthropic** já está prevista no SSM (addendum 002). Este addendum não muda infra; o **tier da conta** é variável de operação a monitorar. **Não atualizar** estruturalmente.
