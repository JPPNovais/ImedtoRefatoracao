# Central de Migração — Materialização de registros: a etapa que faltava entre revisão do mapa e preview/carga (addendum 6)

**ID**: 2026-06-15_007
**Refere-se a**: 2026-06-15_001_central-de-migracao-mapeamento-por-ia.md (e aos addendums 2026-06-15_002, 2026-06-15_003, 2026-06-15_004, 2026-06-15_005, 2026-06-15_006)
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (novo passo de escrita `MaterializarRegistros` na transição `mapa_em_revisao → preview_pronto`, reusando parser + mapas aprovados + `MigracaoRegistro.Criar` já existente; toca o handler de preview/materialização e o repositório de registros, dentro do bounded context de migração)
**Áreas regressivas tocadas**: nenhuma de domínio (paciente/agendamento/estoque/orçamento/prontuário intactos — a carga continua por commands existentes) — toca **só** a fase de **materialização/preview** do bounded context de migração. Risco de regressão contido no fluxo dos addendums 001–006 — coberto por CA118. **Provável sem mudança de schema** (`migracao_registros` já existe; índice `(migracao_job_id, status)` já existe — confirmar com `imedto-database`).

> O épico Central de Migração (2026-06-15_001, CA1–CA24) e os addendums 002 (CA25–CA39), 003 (CA40–CA50), 004 (CA51–CA69), 005 (CA70–CA85) e 006 (CA86–CA101) estão entregues em `main`. Este addendum **corrige um BUG MAJOR confirmado em produção**: a Central **nunca salva registros**. Falta a etapa de **materialização** que transforma as linhas do arquivo em `MigracaoRegistro` (pendentes, com payload mapeado). `MigracaoRegistro.Criar` tem **zero call-sites de produção**; a inferência cria só os **mapas** (de-para por bloco) e **descarta as linhas**; o preview e a carga acham a tabela `migracao_registros` **vazia** → o job conclui com **ZERO registros**. Job #12 em produção = **0 registros**. O briefing original e os addendums anteriores permanecem **intocados** e seus CAs continuam válidos. Os CAs deste addendum começam em **CA102** (o addendum 006 terminou em CA101).

---

## 1. Contexto e motivação

**Bug MAJOR confirmado (job #12 prod = 0 registros).** O discovery (`Docs/Discoverys/migracao-dados-ia/01_discovery.md §4`) prevê o fluxo:

```
extrai cabeçalhos + amostra → 1 chamada à IA (mapa) → operador revisa o mapa
  → código aplica o mapa às N linhas (determinístico)   ← ESTA PEÇA NUNCA FOI IMPLEMENTADA
  → valida obrigatórios por registro → carga por command → relatório
```

A peça **"código aplica o mapa às N linhas"** — a **materialização** das linhas em `MigracaoRegistro` (status `pendente`, com o payload já mapeado para campos canônicos) — **nunca foi codificada**. Evidência confirmada no código atual:

- **`MigracaoRegistro.Criar` (`Domain/Migracao/MigracaoRegistro.cs:42`) tem ZERO call-sites de produção** — só a definição existe; nada o chama. Os métodos `MarcarImportadoCriado`/`MarcarRejeitado`/`MarcarPulado` (que a carga e o relatório consomem) nunca recebem um registro materializado para marcar.
- **A inferência (`InferirMapaMigracaoJobHandler`) cria só os `migracao_mapas`** (de-para por bloco) e **descarta as linhas do arquivo** — nada é persistido em `migracao_registros`.
- **O preview (`PreviewOnda1QueryHandler:34`) lê `_registroRepo.ListarPorJob(jobId)`** e conta `r.Status == "pendente"`. Como a tabela está **vazia**, `TotalRegistros = 0` e `PorEntidade = {}` — o operador vê "0 registros" e segue mesmo assim.
- **A carga (`CarregarOnda1JobHandler`/`CarregarOnda2JobHandler`) itera os `migracao_registros` pendentes.** Sem registros, **nada é criado** — o job conclui `concluido` com **0 criados**.

**Resultado:** todo o fluxo (upload → aprovação → inferência → revisão → preview → "Migrar" → carga → relatório) **roda inteiro sem erro** e **conclui com zero registros**. É um bug silencioso — não falha, só não importa nada. Job #12 em produção confirmou: 0 registros criados, relatório vazio.

**Por que o bug passou:** os addendums 002–006 focaram em resiliência da **inferência** (decomposição de dump, classificação por IA, retry/backoff, degradação por bloco). A etapa de **materialização** — que fica **depois** da revisão do mapa e **antes** do preview contar — caiu no vão entre "o mapa está pronto" e "a carga lê registros". O preview é hoje uma **Query** que muta estado (`MarcarPreviewPronto`) mas **não materializa** nada; a carga assume que os registros já existem. Ninguém os cria.

**Demanda do usuário (decisões firmes, já aprovadas):** implementar a **materialização** como **passo de escrita** na transição `mapa_em_revisao → preview_pronto`: aplicar o parser + os mapas aprovados às linhas reais de cada bloco aceito, gerando um `MigracaoRegistro` **pendente** por linha (com o payload já mapeado para campos canônicos). O preview passa a contar registros **reais**; a carga passa a ter o que importar. Idempotente (re-materializar limpa os `pendente` e regera; `importado_*` nunca são tocados). LGPD-safe (payload é staging, sem PII em log). A carga continua **só por commands** (ordem de FK + upsert por chave de negócio intactos — D11).

**Princípios preservados (não-negociáveis):**
- **D1/D2 do épico intactos** — a IA continua mapeando **schema com amostra mascarada**; a materialização aplica o **mapa já aprovado** às linhas, **sem nova chamada de IA**. A materialização usa a **linha real inteira** (não a amostra truncada a 500 chars — o truncamento do addendum 006 é **só** para a IA).
- **D11 intacto** — nada de id externo; a materialização grava o payload mapeado, a carga resolve PK própria + upsert por chave de negócio (CPF/nome+data/etc.) como já faz.
- **Carga só por commands** — a materialização **não** escreve em tabelas de domínio; ela só popula o **staging** `migracao_registros`. Quem cria Paciente/Agendamento/etc. continua sendo o command de domínio na carga (ordem de FK, upsert).
- **Humano no loop intacto** — gate de aprovação (addendum 003) **antes** da IA; revisão do mapa (addendum 004) **depois**; a materialização roda na transição para preview, **após** o operador estar satisfeito com o mapa.

**Benefício de negócio:** **destrava o produto inteiro** — sem materialização, a Central de Migração **não migra nada**. Esta é a peça que faz a feature funcionar de ponta a ponta. Job #12 (0 registros) vira job que **realmente cria** pacientes e agendamentos no banco.

## 2. Persona-alvo

- **Operador Imedto (admin da plataforma)**, painel `modules/admin`. Hoje vê "0 registros" no preview e um relatório vazio após "Migrar" — e não entende por quê (nada falha). Depois: o preview mostra a **contagem real** por entidade; "Migrar" **cria de fato** os registros; o relatório mostra **N criados ≠ 0**. Frequência: toda migração — é o caminho principal.
- **Cliente (dono/admin do estabelecimento)**: o ganho final — seus dados **realmente** entram no Imedto. Hoje sobe o ZIP, acompanha "concluído" e não acha nenhum paciente importado. Depois: encontra os pacientes/agendamentos migrados.

## 3. Escopo

**Inclui:**

**Bloco A — Passo de materialização (escrita) na transição `mapa_em_revisao → preview_pronto`:**
- Um passo/handler dedicado de **escrita** (`MaterializarRegistros`) que roda **antes** do preview contar. O preview hoje é uma Query que muta estado (`MarcarPreviewPronto`); a materialização é **escrita** (cria N `MigracaoRegistro`), então vira um **command/passo de escrita** disparado na mesma transição (`mapa_em_revisao → preview_pronto`).
- Reusa o **parser** (`JsonMigracaoParser`/CSV — addendum 004) para reler as linhas reais de cada bloco aceito, e os **mapas aprovados** (`migracao_mapas` por `nome_bloco_origem`) para o de-para.
- Por linha de bloco aceito → 1 `MigracaoRegistro.Criar(jobId, estabelecimentoId, entidade, payloadBruto)` com status `pendente` e o **payload já mapeado** para campos canônicos.

**Bloco B — Iteração por bloco classificado (dump aninhado):**
- Itera os **blocos classificados** de `migracao_mapas` por `nome_bloco_origem` (addendum 004) — **não** por nome de arquivo. Para cada bloco aceito, aplica o de-para do **seu** mapa às linhas **daquele** bloco. Um dump aninhado com pacientes + agendamentos gera `MigracaoRegistro` de **N entidades** distintas (cada bloco com sua entidade).

**Bloco C — Aplicação do de-para (linha → payload canônico):**
- Para cada linha, aplica o mapa do bloco: colunas marcadas **`ignorar`** são **descartadas**; as demais viram **campos canônicos** no `payload_bruto` (JSON). O **valor real inteiro** da linha é usado — **nunca** a amostra truncada a 500 chars (o truncamento do addendum 006 é exclusivo da chamada à IA).
- A **validação de obrigatório** **não** ocorre na materialização — ela fica na **CARGA** (o command de domínio lança `BusinessException` que vira `MarcarRejeitado(motivoSemPii)`). A materialização **sempre cria** o registro pendente, mesmo que falte obrigatório; a carga decide aceitar/rejeitar.

**Bloco D — Blocos não-materializáveis:**
- Blocos `sem_equivalente` (sem entidade canônica), `ignorado` (operador descartou) e `eh_config` (configuração não-migrável — addendum 004) **NÃO geram registro**. Só blocos **aceitos com entidade classificada** são materializados.
- Blocos com erro/ignorados pela degradação do addendum 006 (`bloco_com_erro: true`) **NÃO materializam**; os blocos OK do mesmo job materializam normalmente.

**Bloco E — Idempotência (re-materializar):**
- Re-materializar (operador re-gera o preview, ou edita um mapa e volta à revisão) **LIMPA os registros `pendente` do job** e os **regera** a partir dos mapas aprovados atuais. Registros já carregados (`importado_criado`/`importado_atualizado`/`rejeitado`/`pulado`) **nunca são tocados** — re-materializar não desfaz nem duplica importação.

**Não inclui (backlog / fora deste addendum):**

- **Validação de obrigatório na materialização.** Fica na CARGA (como hoje os commands fazem). Antecipar a validação para a materialização é frente própria — backlog. Aqui a materialização só **monta** o payload; a carga valida.
- **Transformação de valor por IA na materialização.** A materialização é **determinística** — aplica o de-para de schema (coluna→campo), não transforma valor por linha (D1). Sem nova chamada de IA.
- **Preview rico com diff/dedupe antecipado.** O preview continua sendo **contagem por entidade** (como hoje, mas agora real). Prever duplicatas/conflitos no preview (antes da carga) é backlog — o upsert por chave de negócio (addendum 004) já resolve na carga.
- **Re-parse incremental / streaming de arquivos gigantes.** A materialização relê o arquivo do bloco (≤50MB, R12 — já limitado no upload). Streaming/chunking de arquivos enormes é backlog.

## 4. Decisões de produto (fechadas — já aprovadas pelo usuário)

> Padrão conservador: reuso de parser + mapas + `MigracaoRegistro.Criar` já existentes, LGPD-safe, carga só por commands, idempotência sem duplicar importação, D11 intacto.

### D-M1 — Materialização é um passo de escrita na transição `mapa_em_revisao → preview_pronto`

A materialização é um **command/passo de escrita** (`MaterializarRegistros`) disparado na transição `mapa_em_revisao → preview_pronto`, **antes** de o preview contar. **Por quê:** o preview hoje é uma **Query** (`PreviewOnda1QueryHandler`) que muta estado via `MarcarPreviewPronto` — mas **não materializa** nada, por isso conta zero. Criar N `MigracaoRegistro` é **escrita** — não cabe numa Query (CQRS: leitura não escreve). A transição `mapa_em_revisao → preview_pronto` é o ponto natural: o operador já revisou o mapa, está satisfeito, e pede o preview; é aí que o de-para aprovado deve ser aplicado às linhas. Reusa o parser e os mapas — sem reabrir a IA.

### D-M2 — Idempotência: re-materializar limpa `pendente` e regera; `importado_*` intactos

Re-materializar (re-preview, ou editar mapa e voltar) **deleta os `MigracaoRegistro` com status `pendente`** do job e os **regera** dos mapas atuais. Registros `importado_criado`/`importado_atualizado`/`rejeitado`/`pulado` **nunca** são tocados. **Por quê:** o operador pode iterar o mapa várias vezes antes de migrar — cada iteração deve refletir o mapa **atual**, sem acumular registros duplicados de iterações anteriores. Mas, uma vez **carregado** (`importado_*`), o registro é histórico de importação real — apagá-lo perderia a rastreabilidade do undo (`EntidadeAlvoId` alimenta o desfazer do addendum 002) e poderia recriar duplicatas na próxima carga. Limpar **só** os `pendente` é o mínimo seguro. O índice `(migracao_job_id, status)` já existe (`ix_migracao_registros_job_status`) e cobre o `DELETE WHERE migracao_job_id=@job AND status='pendente'`.

### D-M3 — Itera blocos classificados (`migracao_mapas` por `nome_bloco_origem`), não nome de arquivo

A materialização itera os **blocos classificados** de `migracao_mapas` (chave `nome_bloco_origem` do addendum 004), aplicando o de-para de **cada bloco** às linhas **daquele** bloco. **Por quê:** um dump aninhado tem N blocos (pacientes, agendamentos, ...) num só arquivo (addendum 004 decompõe `DecomporObjetoRaiz`). Iterar por **bloco** (não por arquivo) garante que cada bloco materialize sua entidade correta com seu mapa correto. É a mesma cardinalidade "1 job → N mapas → N grupos de registros" já estabelecida.

### D-M4 — De-para: `ignorar` descartado; demais viram campos canônicos; valor real inteiro; obrigatório valida na carga

Colunas marcadas **`ignorar`** no mapa são **descartadas**; as demais viram **campos canônicos** no `payload_bruto`. Usa o **valor real inteiro** da linha (não a amostra truncada). A **validação de obrigatório** fica na **CARGA** (`BusinessException` do command → `MarcarRejeitado`). **Por quê:** o de-para aprovado já diz coluna→campo; aplicá-lo é determinístico. O truncamento a 500 chars do addendum 006 é **só** para economizar tokens na IA — a carga precisa do valor **inteiro** (um CPF, uma data, um nome completos), então a materialização usa a linha real. Validar obrigatório na carga (não na materialização) mantém **uma** fonte de verdade de validação (o command de domínio, espelhando a premissa "regra de negócio sempre no backend/command") — a materialização só monta o staging.

### D-M5 — Carga continua só por commands; D11 intacto

A materialização **não** escreve em tabelas de domínio — só popula `migracao_registros` (staging). A carga (`CarregarOnda1/2JobHandler`) continua criando Paciente/Agendamento/etc. **por commands**, com ordem de FK (R5) e upsert por chave de negócio (R2/D11). **Por quê:** separação limpa — materialização = "monta o que vai ser importado" (staging); carga = "importa de fato" (domínio, regras, FK, dedupe). Nunca materializar direto no domínio preserva o humano no loop (preview entre materializar e carregar) e a idempotência (re-materializar mexe só no staging, nunca em dado de domínio).

## 5. Regras de negócio

**Bloco A — Materialização (escrita):**

- **R-M1 (materialização cria registro pendente por linha de bloco aceito — D-M1)**: Na transição `mapa_em_revisao → preview_pronto`, o passo `MaterializarRegistros` relê as linhas reais de cada bloco aceito (via parser) e cria **1 `MigracaoRegistro` `pendente` por linha**, com o `payload_bruto` já mapeado para campos canônicos (via `MigracaoRegistro.Criar`). Mora em: `MaterializarRegistrosCommandHandler` (Application) + `IMigracaoRegistroRepository`. Validada em: back.

- **R-M2 (preview conta o real — D-M1)**: Após a materialização, o preview conta os `MigracaoRegistro` `pendente` reais por entidade — `TotalRegistros` e `PorEntidade` refletem as linhas materializadas, nunca zero quando há blocos aceitos com linhas. Mora em: preview (após materialização). Validada em: back + front.

**Bloco B — Blocos:**

- **R-M3 (itera blocos classificados — D-M3)**: A materialização itera os blocos de `migracao_mapas` por `nome_bloco_origem` (addendum 004), aplicando o de-para de cada bloco às linhas daquele bloco; um dump aninhado materializa registros de N entidades. Mora em: handler de materialização. Validada em: back.

- **R-M4 (blocos não-materializáveis não geram registro — D-M3/D-M4)**: Blocos `sem_equivalente`, `ignorado`, `eh_config` e `bloco_com_erro: true` (addendum 006) **NÃO geram** `MigracaoRegistro`. Só blocos aceitos com entidade classificada são materializados. Mora em: handler de materialização. Validada em: back.

**Bloco C — De-para:**

- **R-M5 (de-para: ignorar descartado, demais canônicos, valor real inteiro — D-M4)**: Ao montar o `payload_bruto`, colunas `ignorar` são descartadas; as demais viram campos canônicos com o **valor real inteiro** da linha (não a amostra truncada a 500 chars). Mora em: handler de materialização. Validada em: back.

- **R-M6 (obrigatório valida na carga, não na materialização — D-M4)**: A materialização **sempre cria** o registro pendente, mesmo faltando obrigatório. A validação de obrigatório ocorre na **carga** (`BusinessException` do command → `MarcarRejeitado(motivoSemPii)`). Mora em: command de domínio na carga (validação) + handler de materialização (não valida). Validada em: back.

**Bloco D — Idempotência:**

- **R-M7 (re-materializar limpa pendente e regera; importado_* intacto — D-M2)**: Re-materializar **deleta os `MigracaoRegistro` `pendente`** do job (`DELETE WHERE migracao_job_id=@job AND status='pendente'`) e os **regera** dos mapas atuais. Registros `importado_criado`/`importado_atualizado`/`rejeitado`/`pulado` **nunca** são tocados — sem duplicar nem desfazer importação. Mora em: handler de materialização + repositório. Validada em: back. (Índice `(migracao_job_id, status)` já cobre o DELETE.)

**Bloco E — Carga:**

- **R-M8 (carga só por commands, ordem FK + upsert intactos — D-M5)**: A carga (`CarregarOnda1/2JobHandler`) consome os `MigracaoRegistro` `pendente` materializados e cria as entidades **por commands**, com ordem de FK (R5) e upsert por chave de negócio (R2/D11) — **inalterado**. A materialização nunca escreve em tabela de domínio. Mora em: handlers de carga (inalterados) + materialização (só staging). Validada em: back.

## 6. Modelo de dados

**Provável sem mudança de schema.** A tabela `migracao_registros` **já existe** (`CriarTabelasMigracao` / `MigracaoRegistroConfiguration`) com todas as colunas necessárias: `migracao_job_id`, `estabelecimento_id`, `entidade`, `payload_bruto` (jsonb), `status` (default `pendente`), `motivo_rejeicao`, `entidade_alvo_id`, `criado_em`. O construtor `MigracaoRegistro.Criar` e os métodos `MarcarImportado*`/`MarcarRejeitado`/`MarcarPulado` **já existem** — só faltava chamá-los.

- **Índice da idempotência já existe**: `ix_migracao_registros_job_status` em `(migracao_job_id, status)` cobre o `DELETE WHERE migracao_job_id=@job AND status='pendente'` da re-materialização (R-M7). Nenhum índice novo previsto.
- **Multi-tenant**: `estabelecimento_id` herdado do job (redundante na tabela para queries sem JOIN — já modelado). A materialização herda o `estabelecimento_id` do `MigracaoJob` (R-M1).
- **FK CASCADE**: `migracao_registros.migracao_job_id` já tem `ON DELETE CASCADE` — registros somem com o job.

**Audit / LGPD**: `payload_bruto` é **staging** com PII do tenant (jsonb) — **nunca logar** (já documentado na config). É apagado quando o arquivo expira (30 dias, `ExpirarArquivosMigracaoJob` — CASCADE do job, ou política de retenção a confirmar). Nenhum PII em log/mensagem na materialização. O `motivo_rejeicao` (preenchido na carga) continua categoria genérica sem PII (addendum 002).

**Acionar `imedto-database` (confirmação, não modelagem):** confirmar que **(a)** `migracao_registros` tem todas as colunas que a materialização precisa (default do BA: sim — já modelado); **(b)** o índice `ix_migracao_registros_job_status` `(migracao_job_id, status)` suporta o `DELETE WHERE status='pendente'` da re-materialização sem full scan (default do BA: sim — índice composto já existe). **Provável ZERO migration.** Se (improvável) o DELETE em lote exigir tuning, o DB agent decide.

## 7. UX e fluxo

**Fluxo-alvo (insere a materialização no fluxo dos addendums 003–006):**

1. Cliente sobe ZIP → gate de aprovação (addendum 003) → inferência resiliente por bloco (addendums 004/006) → `mapa_em_revisao`. **Inalterado.**
2. Operador revisa o mapa (addendum 004): aceita/reclassifica blocos, marca colunas `ignorar`, ignora blocos não-migráveis. **Inalterado.**
3. Operador pede **preview** → **NOVO: a materialização roda** (`mapa_em_revisao → preview_pronto`): relê as linhas dos blocos aceitos, aplica o de-para aprovado, cria N `MigracaoRegistro` `pendente`. O preview então conta os registros **reais** por entidade.
4. Operador vê o preview com **contagem real** (não mais 0) → clica **"Migrar"**.
5. Carga (`CarregarOnda1/2JobHandler`) consome os `pendente`, cria por commands (ordem FK + upsert + obrigatório → rejeita com motivo). **Inalterado.**
6. Relatório: **N criados ≠ 0** + rejeitados/pulados com motivo (addendum 002). **Inalterado** — agora com dados reais.
7. Desfazer (addendum 002): só os `importado_criado` deste job. **Inalterado.**

**Re-materializar (idempotência):** se o operador volta da revisão (edita um mapa) e pede o preview de novo, a materialização **limpa os `pendente`** e regera — o preview reflete o mapa novo, sem duplicar.

**Painel admin (`MigracaoRevisaoView`):** o botão de preview/"gerar preview" passa a disparar a materialização (o operador percebe a contagem real aparecer). Estados: loading durante a materialização (pode levar alguns segundos em arquivo grande), preview com contagem por entidade (real), erro genérico se a materialização falhar. Reusa os componentes/padrões existentes. Tipografia por tokens (CLAUDE.md §5).

**Lado do cliente:** inalterado — só acompanha o status.

## 8. Critérios de aceite (testáveis) — começam em CA102

**Bloco A — Materialização cria registros (D-M1):**

- **CA102 (materialização cria 1 registro pendente por linha de bloco aceito — R-M1)**: Dado um job em `mapa_em_revisao` com um bloco `pacientes` aceito contendo 30 linhas, Quando a materialização roda (transição para `preview_pronto`), Então **30 `MigracaoRegistro` `pendente`** são criados em `migracao_registros`, cada um com `entidade='paciente'`, `estabelecimento_id` do job e `payload_bruto` mapeado para campos canônicos.

- **CA103 (payload mapeado para campos canônicos — R-M1/R-M5)**: Dado um bloco cujo mapa mapeia `nome_completo → nome`, `documento → cpf`, Quando uma linha é materializada, Então o `payload_bruto` do registro contém as **chaves canônicas** (`nome`, `cpf`) com os valores reais da linha — não as chaves de origem.

- **CA104 (preview conta o real, não zero — R-M2)**: Dado um job materializado com 30 pacientes e 12 agendamentos pendentes, Quando o operador abre o preview, Então `TotalRegistros = 42` e `PorEntidade = { paciente: 30, agendamento: 12 }` — **nunca 0** quando há blocos aceitos com linhas (corrige o bug do job #12).

**E2E obrigatório (caminho completo):**

- **CA105 (E2E — registros REALMENTE criados no banco — R-M1/R-M8)**: Dado um upload com um bloco de **pacientes** e um de **agendamentos** (IA **mockada** com de-para conhecido), Quando o fluxo completo roda — inferência (mock) → materialização → carga — Então: (a) a materialização cria os `MigracaoRegistro` pendentes; (b) a carga cria as entidades de domínio **via commands** (`Paciente` e `Agendamento` realmente persistidos no banco); (c) o relatório reporta **N criados ≠ 0**. Este é o teste que prova que a Central **migra de ponta a ponta** — o bug do job #12 não se reproduz.

**Bloco B — Dump aninhado (D-M3):**

- **CA106 (dump aninhado materializa N entidades — R-M3)**: Dado um dump JSON aninhado decomposto em blocos `pacientes` (entidade `paciente`) e `consultas` (entidade `agendamento`), ambos aceitos, Quando a materialização roda, Então gera `MigracaoRegistro` de **paciente** (das linhas de `pacientes`) **e** de **agendamento** (das linhas de `consultas`) — iterando por bloco classificado, não por nome de arquivo.

- **CA107 (blocos não-materializáveis não geram registro — R-M4)**: Dado um dump com um bloco `sem_equivalente`, um bloco `ignorado` pelo operador e um bloco `eh_config` (configuração), Quando a materialização roda, Então **nenhum** desses três gera `MigracaoRegistro` — só os blocos aceitos com entidade classificada materializam.

**Bloco C — De-para (D-M4):**

- **CA108 (coluna ignorar descartada; valor real inteiro — R-M5)**: Dado um bloco onde a coluna `observacoes` está marcada `ignorar` e a coluna `nome` tem um valor de 800 caracteres, Quando uma linha é materializada, Então (a) `observacoes` **não** aparece no `payload_bruto`; (b) o `nome` no payload tem os **800 caracteres reais inteiros** — **não** truncado a 500 (o truncamento do addendum 006 é só para a IA, não para a materialização/carga).

- **CA109 (obrigatório ausente → registro criado, carga rejeita — R-M6)**: Dado uma linha de paciente **sem CPF** (obrigatório), Quando a materialização roda, Então o `MigracaoRegistro` **é criado** (`pendente`) mesmo sem CPF; e Quando a **carga** processa esse registro, Então o command lança `BusinessException` e o registro vira `rejeitado` com `motivo_rejeicao` genérico ("CPF ausente") — a validação de obrigatório é da carga, não da materialização.

**Bloco D — Idempotência (D-M2):**

- **CA110 (re-materializar limpa pendente e regera, sem duplicar; importado_* intacto — R-M7)**: Dado um job já materializado com 30 pacientes `pendente` e 10 já `importado_criado` (de uma carga parcial anterior), Quando o operador edita um mapa e re-materializa, Então (a) os **30 `pendente` são deletados e regerados** dos mapas atuais (sem virar 60); (b) os **10 `importado_criado` permanecem intactos** — re-materializar não os apaga nem os duplica.

**Multi-tenant, LGPD, RBAC, regressão:**

- **CA111 (multi-tenant — herdado e falha-fechada)**: Dado um job de migração, Quando a materialização cria os registros, Então cada `MigracaoRegistro` carrega o `estabelecimento_id` do job; um operador/job de outro contexto recebe "não encontrado" genérico; sem tenant claim, o repositório falha-fechada (vazio/throws). Nenhum dado de tenant alheio vaza.

- **CA112 (LGPD — payload staging, sem PII em log)**: Dado a materialização de qualquer linha, Quando o `payload_bruto` (com PII do tenant) é persistido, Então (a) **nenhum log** carrega o payload nem PII (só contagens/status); (b) o payload é staging em `migracao_registros`, apagado quando o arquivo/job expira (CASCADE / retenção — addendum 001); (c) mensagens de erro da materialização são genéricas, sem PII.

- **CA113 (carga só por commands — ordem FK + upsert intactos — R-M8)**: Dado registros materializados de pacientes e agendamentos, Quando a carga roda, Então cria **via commands** (não SQL direto), respeitando a ordem de FK (paciente antes de agendamento — R5) e o upsert por chave de negócio (R2/D11 — não duplica paciente já existente). A materialização nunca escreveu em tabela de domínio.

- **CA114 (Onda 2 — materializa só blocos prontuário; vínculo por CPF na carga)**: Dado um job Onda 2 (`onda='prontuario'`) com blocos de prontuário/evolução, Quando a materialização roda, Então materializa os `MigracaoRegistro` dos blocos de prontuário; e a carga (`CarregarOnda2JobHandler`) vincula ao paciente **por CPF** (`IMigracaoPacienteLookup`) como já faz (Marco 5 — inalterado). Onda 2 segue dependendo da Onda 1 (CA13).

- **CA115 (degradação do addendum 006 — bloco com erro não materializa, OK materializa)**: Dado um job onde um bloco ficou `bloco_com_erro: true` (limite de taxa da IA, addendum 006) e outros 3 blocos mapearam OK, Quando a materialização roda, Então o bloco com erro **não** gera registro e os 3 blocos OK **materializam normalmente** — a degradação graciosa por bloco e a materialização compõem sem conflito.

- **CA116 (progresso ao vivo honesto — total = materializados — reusa CA57)**: Dado um job materializado e em carga, Quando o painel consulta o progresso ao vivo (CA57, `GET /{jobId}/progresso` — `GROUP BY entidade, status` em `migracao_registros`), Então o **total** reflete os registros **materializados** (não zero) e o percentual avança conforme `pendente → importado_*` — o progresso passa a ser honesto (antes era sempre 0/0).

- **CA117 (RBAC — ImedtoAdmin no disparo; gate anti-IA intacto)**: Dado um usuário sem a policy **`ImedtoAdmin`**, Quando tenta disparar a materialização/preview, Então recebe **403** e os controles ficam ocultos no front (reuso da policy do `AdminMigracaoController`). E o gate de aprovação anti-IA (CA44, addendum 003) permanece **antes** da inferência — a materialização não cria atalho que pule a aprovação.

- **CA118 (regressão — fluxo 001–006 intacto)**: Dado o fluxo completo dos briefings 001–006 (upload → aprovação CA44 → decomposição de dump CA70-72 → classificação por IA CA73-76 → inferência resiliente CA86-101 → revisão → **materialização (novo)** → preview → carga por commands → upsert R2 → ordem FK R5 → relatório com motivos addendum 002 → desfazer só-criados addendum 002 → timeline/progresso CA51-69), Quando roda com a materialização inserida, Então **nada regride**: o gate de aprovação, a inferência resiliente, o upsert por chave de negócio, a ordem de FK e o desfazer (só `importado_criado`) seguem valendo; a única diferença é que agora **registros são de fato criados** — o caminho que antes concluía com zero agora conclui com N ≠ 0.

## 9. Riscos e dependências

- **Risco — re-parse do arquivo na materialização.** A materialização relê o arquivo do bloco (do S3) para obter as linhas reais. O arquivo está limitado a ≤50MB (R12) e a materialização é assíncrona (passo de escrita, não bloqueia request síncrono longo se for via job/command com retorno rápido). **Mitigado**: limite de 50MB já no upload; se o re-parse for caro, rodar dentro do mesmo padrão de job recorrente (a confirmar pelo dev — pode ser command síncrono se rápido, ou passo no scheduler se lento). Liberdade técnica.
- **Risco — idempotência do DELETE em lote.** O `DELETE WHERE migracao_job_id=@job AND status='pendente'` precisa do índice `(migracao_job_id, status)` (já existe) para não fazer full scan. **Coberto** por R-M7/CA110 + confirmação do DB agent.
- **Risco — divergência de chaves canônicas entre materialização e carga.** A materialização monta o `payload_bruto` com chaves canônicas; a carga lê essas chaves. Se as chaves divergirem, a carga não acha o campo. **Mitigado**: usar o **mesmo** vocabulário canônico que os commands de carga já esperam (o dev deve alinhar as chaves do payload com o que `CarregarOnda1/2JobHandler` lê hoje) — CA105 (E2E) é o guardião.
- **Regressão do fluxo 001–006**: a materialização **insere** um passo; não pode quebrar nada antes (inferência/revisão) nem depois (carga/relatório/desfazer). **CA118 é o guardião.**
- **Área regressiva**: nenhuma de domínio. Risco contido na fase de materialização/preview do bounded context de migração. Commands de paciente/agendamento/estoque/orçamento/prontuário **não** mudam — a carga continua por eles. **Provável sem migration.**

## 10. Observações para execução

- **Reuso obrigatório (não construir do zero)**: `MigracaoRegistro.Criar` + `MarcarImportado*`/`MarcarRejeitado`/`MarcarPulado` **já existem** (`Domain/Migracao/MigracaoRegistro.cs`) — só faltava chamá-los. O parser (`JsonMigracaoParser`/CSV — addendum 004) relê as linhas; os mapas vêm de `migracao_mapas` por `nome_bloco_origem` (addendum 004). O repositório `IMigracaoRegistroRepository` já existe (`ListarPorJob`); precisa de um método para **deletar pendentes do job** (re-materialização — R-M7) e **salvar em lote** os novos. A carga (`CarregarOnda1/2JobHandler`) e o relatório (addendum 002) **não mudam** — passam a ter registros para consumir.
- **Não-negociável (CQRS — materialização é escrita)**: a materialização é **command/passo de escrita** (`MaterializarRegistros`), **não** uma Query. O `PreviewOnda1QueryHandler` atual muta estado (`MarcarPreviewPronto`) numa Query — a materialização (escrita de N registros) deve sair da Query e virar passo de escrita na transição. O dev decide a forma exata (command dedicado disparado antes do preview, ou passo no handler de transição) desde que a escrita não viva numa Query.
- **Não-negociável (valor real, não truncado)**: a materialização usa a **linha real inteira** — o truncamento a 500 chars do addendum 006 é **exclusivo** da chamada à IA. CA108 é o guardião. Nunca aplicar o truncamento da amostra ao payload de carga.
- **Não-negociável (carga só por commands — D11)**: a materialização **só** popula `migracao_registros` (staging). Nunca escreve Paciente/Agendamento/etc. — quem cria é o command na carga, com ordem de FK + upsert por chave de negócio. CA113 é o guardião.
- **Não-negociável (idempotência)**: re-materializar limpa **só** os `pendente` e regera; `importado_*`/`rejeitado`/`pulado` **nunca** são tocados (R-M7/CA110). O DELETE usa o índice `(migracao_job_id, status)` existente.
- **Não-negociável (LGPD)**: `payload_bruto` é PII do tenant — **nunca** logar; mensagens genéricas; o registro é staging apagável (CA112).
- **Liberdade técnica**: a forma de disparar a materialização (command explícito vs. passo no handler de transição vs. passo no scheduler se o re-parse for lento); o vocabulário exato das chaves canônicas no payload (desde que **alinhado** com o que os commands de carga leem — CA105); a forma de deletar pendentes em lote (Dapper `DELETE` ou EF `ExecuteDelete`); o batch size do insert. Tudo desde que respeite os CAs.
- **Acionar `imedto-database`**: apenas para **confirmar** que **(a)** `migracao_registros` tem as colunas que a materialização precisa (default: sim — já modelado) e **(b)** o índice `ix_migracao_registros_job_status` `(migracao_job_id, status)` suporta o `DELETE WHERE status='pendente'` da re-materialização sem full scan (default: sim). **Provável ZERO migration.** Se (improvável) precisar de tuning do DELETE em lote, o DB agent decide.
- **Fatiamento sugerido** (PRs pequenos sob este mesmo ID): PR 1 = passo de materialização + de-para + iteração por bloco (CA102-108) + repositório (delete pendentes / save em lote). PR 2 = idempotência da re-materialização (CA110) + integração preview (CA104) + Onda 2 (CA114) + degradação addendum 006 (CA115). PR 3 = E2E (CA105) + progresso honesto (CA116). Regressão (CA118), multi-tenant (CA111), LGPD (CA112), RBAC (CA117) validados em todos.

## 11. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — **atualizado nesta entrega pelo BA**: (1) no diagrama de estados do `MigracaoJob`, anotar a **etapa de materialização** na transição `mapa_em_revisao → preview_pronto` (relê linhas dos blocos aceitos, aplica de-para aprovado, cria N `MigracaoRegistro` pendentes); (2) nova subseção **"Materialização de registros (addendum 6 — CA102-118)"** após "Resiliência da inferência por IA", documentando: a materialização é o passo de **escrita** (não Query) que faltava (`MigracaoRegistro.Criar` tinha zero call-sites → bug do job #12, 0 registros); itera blocos classificados; de-para descarta `ignorar`, demais viram campos canônicos com **valor real inteiro** (não a amostra truncada do addendum 006); obrigatório valida na **carga**; idempotência (re-materializar limpa `pendente`, regera; `importado_*` intactos) usando o índice `(migracao_job_id, status)` existente; carga continua só por commands (D11).
- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — **atualizado nesta entrega pelo BA**: nota incremental no §4 (no passo "código aplica o mapa às N linhas") registrando que **essa peça só foi de fato implementada no addendum 6** — antes faltava a materialização, e o produto concluía com zero registros (job #12). Não reescrever D1–D14 nem o restante.
- **`Docs/LGPD.md`** — sem mudança material: o `payload_bruto` como staging com PII (não-logável, apagável na expiração) já segue a regra geral de minimização/audit/sem-PII-em-log. **Não atualizar** (coberto pela regra geral).
- **`Docs/INFRA.md`** — sem mudança: nenhum recurso AWS/SSM novo; provável sem migration. **Não atualizar.**
