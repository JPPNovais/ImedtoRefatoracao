# Central de Migração — Classificação semântica de entidades por IA + suporte a dump JSON aninhado (addendum 4)

**ID**: 2026-06-15_005
**Refere-se a**: 2026-06-15_001_central-de-migracao-mapeamento-por-ia.md (e aos addendums 2026-06-15_002, 2026-06-15_003, 2026-06-15_004)
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (parser de dump aninhado + nova etapa de classificação na inferência + evolução da porta `IMapeadorDeMigracao` + normalização de encoding na ingestão + UI admin de revisão da classificação + ajuste do mapa por entidade — toca a fase de **inferência/parsing**, não a de carga)
**Áreas regressivas tocadas**: nenhuma de domínio (paciente/estoque/orçamento/prontuário/agenda intactos) — toca **só** o bounded context de migração (parsing + inferência de mapa + revisão no admin). **Risco de regressão real** sobre o fluxo de arquivos tabulares já suportado (CSV, JSON-array) — coberto por CAs de regressão. Sem mudança de schema de domínio; avaliar 1 coluna leve de staging (ver §6).

> O épico Central de Migração (2026-06-15_001, CA1–CA24) e os addendums 002 (CA25–CA39), 003 (CA40–CA50) e 004 (CA51–CA69) estão entregues em `main`. Este addendum **fecha uma lacuna de capacidade observada em produção**: o pipeline atual assume **1 arquivo = 1 entidade tabular** e **detecta a entidade pelo nome do arquivo** — e quebra com dumps JSON aninhados de sistemas desconhecidos, que são a forma comum de export real. O briefing original e os addendums anteriores permanecem **intocados** e seus CAs continuam válidos. Os CAs deste addendum começam em **CA70** (o addendum 004 terminou em CA69).

---

## 1. Contexto e motivação

**Problema real observado — job #11 falhou.** Um cliente subiu um ZIP contendo **um único arquivo JSON** que é um **dump aninhado** de um sistema desconhecido: um objeto raiz com várias entidades como chaves — `estabelecimento{}` (objeto único de config), `reparticoes[]`, `profissionais[]`, `pacientes[]`, `agendamentos[]`, `atendimentos[]`, `prontuarios[]` — cada valor sendo um array de registros (alguns com sub-objetos aninhados, ex.: `campos_especificos:{}`, `subreparticoes_ids:[]`). O arquivo veio ainda com **encoding quebrado / mojibake** ("Cirurgia PlÃ¡stica" em vez de "Plástica").

**Por que falhou — três limitações confirmadas no código:**

1. **O parser JSON só lê o primeiro array do objeto raiz.** [JsonMigracaoParser.cs](../backend/src/Services/Imedto.Backend.Infrastructure/Migracao/JsonMigracaoParser.cs):70-79 — `EncontrarPrimeiroArray` retorna a **primeira** propriedade-array e ignora todo o resto. Num dump aninhado, leria só `reparticoes[]` (ou o que vier primeiro) e **jogaria fora** pacientes, agendamentos, prontuários etc.

2. **A entidade é detectada pelo NOME do arquivo.** [InferirMapaMigracaoJobHandler.cs](../backend/src/Services/Imedto.Backend.Application/Migracao/Jobs/InferirMapaMigracaoJobHandler.cs):236-244 — `DetectarEntidade("pacientes.json") → "paciente"` (tira a extensão e o `s` final). Com um dump chamado `sistema_hospitalar_backup_2026.json`, mapeia para uma "entidade" inexistente (`"sistema_hospitalar_backup_2026"`), e o `entidadeAlvo` passado para a IA fica inútil.

3. **A arquitetura assume 1 arquivo = 1 entidade tabular.** O loop em [InferirMapaMigracaoJobHandler.cs](../backend/src/Services/Imedto.Backend.Application/Migracao/Jobs/InferirMapaMigracaoJobHandler.cs):138-217 itera `zipArchive.Entries`, cada entry vira **uma** entidade e **um** mapa. Não há conceito de "um arquivo contém N entidades". Dumps aninhados de sistemas desconhecidos não cabem nesse modelo.

**Demanda do usuário (decisão firme):** a IA deve **identificar e separar as entidades** de estruturas não-tabulares (dump JSON aninhado) para conseguir migrar — porque sistemas desconhecidos **raramente** exportam listas tabulares limpas. A detecção de entidade por nome de arquivo é substituída por **classificação semântica da IA**.

**Princípio preservado (não-negociável):** D1/D2 do épico continuam intocados — a IA **mapeia SCHEMA com amostra mascarada**, **nunca** transforma registro a registro. Este addendum só dá à IA uma tarefa a mais **sobre os mesmos metadados**: além de "para onde cada coluna vai", também "que entidade canônica este bloco é". Continua 1 chamada por bloco-candidato (custo controlado — ver D-N2), continua só amostra mascarada, continua humano no loop (gate de aprovação do addendum 003 **antes**, revisão do mapa **depois**).

**Benefício de negócio:** desbloqueia a migração de clientes cujo sistema de origem exporta dump (a maioria dos sistemas reais/legados), atacando diretamente o atrito de portabilidade que é o diferencial da FASE 2B. Sem isso, "traga seus dados de qualquer sistema" é falso na prática.

## 2. Persona-alvo

- **Operador Imedto (admin da plataforma)**, no painel `modules/admin`. Ganha: a IA decompõe o dump em entidades candidatas, propõe a classificação de cada bloco (paciente / agendamento / fornecedor / ... / "sem equivalente — ignorar") **e** o de-para de colunas; o operador **revisa e corrige** a classificação e o mapa antes de aprovar/disparar (a etapa de revisão que já existe, agora com a camada de classificação). Frequência: recorrente, sempre que a origem exporta dump aninhado.
- **Cliente (dono/admin do estabelecimento)**: inalterado — sobe o ZIP como hoje; não precisa saber se é dump ou tabular.

## 3. Escopo

**Inclui:**

**Bloco A — Parser de dump JSON aninhado (decomposição em blocos-candidatos):**
- Evoluir o `JsonMigracaoParser` (ou um parser/etapa nova) para, num objeto raiz, **enumerar todos os blocos**: cada propriedade cujo valor é um **array de objetos** vira um **bloco-candidato** (com seu nome de chave, cabeçalhos derivados das chaves dos objetos, e amostra de N linhas). Propriedades cujo valor é **objeto único** (ex.: `estabelecimento{}`) são tratadas como **metadados de config** — sinalizadas, não viram entidade migrável (D-S6).
- **Compatibilidade (não-negociável):** um JSON que já é um **array de objetos na raiz** (`[{...},{...}]`) — o formato JSON-array já suportado — continua sendo **um** bloco-candidato, exatamente como hoje. O parser não pode regredir o caminho tabular existente.

**Bloco B — Classificação semântica da entidade pela IA (substitui detecção por nome):**
- A IA passa a **classificar** cada bloco-candidato numa **entidade canônica do Imedto** (lista fechada — D-S1) **OU** em `"sem_equivalente"` (ignorar/pular) — **e** produzir o de-para de colunas do bloco, na mesma resposta (D-N2). A `IMapeadorDeMigracao` evolui para devolver, por bloco: `{ entidade_classificada, confiança_classificação, de_para_colunas, confiança_mapa, dúvidas }`.
- A **detecção por nome de arquivo deixa de ser a fonte da entidade**. Para arquivos **tabulares** (CSV / JSON-array / 1 arquivo = 1 entidade), o nome do arquivo vira **dica/hint** passada à IA como contexto (e fallback de exibição), **não** mais a decisão final — a IA confirma/corrige a classificação. Isso unifica os dois caminhos sob "a IA classifica".

**Bloco C — Revisão da classificação no painel admin:**
- A tela de revisão do mapa (`MigracaoRevisaoView`) passa a exibir, por bloco-candidato: a **entidade classificada** pela IA (com confiança), um **seletor** para o operador **corrigir** a entidade (incluindo escolher "ignorar este bloco"), e o de-para de colunas daquele bloco (como já hoje). Blocos classificados como `"sem_equivalente"` aparecem com a opção de **pular** explícita; o operador nunca é forçado a mapear (D-S6).
- O preview/contadores e a carga passam a operar sobre os **blocos aceitos** (não-ignorados) com sua entidade final (a do operador, que sobrepõe a da IA).

**Bloco D — Normalização de encoding/mojibake na ingestão:**
- Na leitura do arquivo, **detectar e corrigir UTF-8 mal-decodificado** (mojibake clássico: bytes UTF-8 lidos como Latin-1, ex.: "PlÃ¡stica" → "Plástica") de forma conservadora, **antes** de extrair a amostra e antes da carga. Onde a correção for ambígua/insegura, **não corromper** — manter o original e **sinalizar ao operador** que o arquivo pode ter encoding suspeito (D-E1). A normalização é determinística, **não** é tarefa de IA.

**Bloco E — Sub-objetos dentro de registros:**
- Campos cujo valor é **sub-objeto** (`campos_especificos:{}`) ou **array** (`subreparticoes_ids:[]`) dentro de um registro são tratados como **não-planos**: a IA mapeia apenas os **campos planos** (string/número/data) que casam com campos canônicos; o que é sub-objeto/array **não é mapeado** por padrão e **não inventa** campo (D-S4). A serialização crua desses sub-objetos (hoje `GetRawText()`) permanece disponível no `payload_bruto` para auditoria, mas **não** é carregada como campo de domínio a menos que mapeada explicitamente.

**Não inclui (registrar como backlog / fora deste addendum):**

- **XLSX multi-aba (1 aba = 1 entidade).** **Fica como backlog explícito** (D-S2). Motivo técnico decisivo: o `XlsxMigracaoParser` atual é um **stub que lança `NotSupportedException`** ([XlsxMigracaoParser.cs](../backend/src/Services/Imedto.Backend.Infrastructure/Migracao/XlsxMigracaoParser.cs):20) — XLSX **não é suportado hoje**, apesar de constar no D8 do épico. Implementar parsing XLSX real + multi-aba é uma frente própria; o foco firme do usuário é **dump JSON aninhado**. CSV permanece tabular simples; JSON-array permanece como está.
- **Resolução de FK por id interno do dump** (ex.: `agendamentos[].paciente_id` referenciando ids do sistema de origem). **Fora do MVP desta evolução** (D-S5). Conflita frontalmente com R1/D11 (Imedto gera as próprias PKs; **nunca** usa id externo). Resolver vínculo entre entidades de um mesmo dump por id de origem é uma fase própria. Neste addendum: o vínculo continua sendo resolvido **só por chave de negócio** (R2 do épico — CPF do paciente etc.); blocos como agenda/prontuário que dependem de um paciente **rejeitam com motivo** quando o paciente não é resolvível por chave de negócio (comportamento já existente, agora explicitado para o caso dump — D-S5/CA76).
- **Auto-detecção da origem por fingerprint do dump.** O operador continua confirmando a origem; o template por origem (D13/R10) segue manual. Backlog.
- **Mapeamento de sub-objetos aninhados como sub-registros** (ex.: explodir `campos_especificos` em colunas, ou criar registros-filho). Fora de escopo — sub-objeto não-mapeado é ignorado (D-S4). Backlog se um cliente real exigir.
- **Conversão de mojibake em alfabetos não-latinos** ou heurísticas agressivas de re-encoding. A correção é a do caso UTF-8↔Latin-1 (o mojibake comum em PT-BR); casos exóticos são sinalizados, não corrigidos (D-E1). Backlog.
- **XML** — segue fora (D8 do épico).

## 4. Decisões de produto (fechadas neste addendum)

> Os 6 pontos que o usuário pediu para o BA fechar. Padrão conservador: reuso, LGPD-safe, mínimo viável que serve, **nunca** quebrar o caminho tabular existente, **nunca** violar D11 (id externo), **nunca** a IA inventar dado.

### D-S1 — Lista fechada de entidades canônicas para a classificação (Ponto 6 — "sem equivalente")

A IA classifica cada bloco em **uma** das entidades canônicas **já suportadas pela carga** (as do épico, Onda 1 + Onda 2) **ou** em `"sem_equivalente"`:

| Valor de classificação | Destino de carga |
|---|---|
| `paciente` | `Paciente` |
| `agendamento` | `Agendamento` |
| `fornecedor_estoque` | `FornecedorEstoque` |
| `categoria_estoque` | `CategoriaEstoque` |
| `fabricante_estoque` | `FabricanteEstoque` |
| `local_estoque` | `LocalEstoque` |
| `item_estoque` | `ItemInventario` |
| `produto_orcamento` | `CatalogoProduto` |
| `procedimento_orcamento` | `ProcedimentoCatalogo` |
| `prontuario` | Onda 2 (evolução/anexo) |
| `sem_equivalente` | **nada** — bloco ignorado/pulado |

**Por quê:** a classificação só pode apontar para uma entidade que a carga sabe carregar (a carga roda pelos commands existentes — R3 do épico). A IA **não** cria entidades novas. Blocos que não casam com nenhuma (`reparticoes`, `atendimentos` com HTML gigante, `profissionais` — que não é entidade de carga deste MVP) caem em `"sem_equivalente"`, o operador vê e **decide pular** — **nunca** se força um mapeamento (Ponto 6). A lista é a fonte da verdade do prompt e da UI; ampliá-la no futuro é só adicionar entidade suportada pela carga.

### D-S2 — Escopo de formatos da evolução: dump JSON aninhado SIM; XLSX multi-aba BACKLOG (Ponto 1)

**Dump JSON aninhado é o foco e entra.** **XLSX multi-aba fica como backlog** — porque o XLSX nem sequer é parseável hoje (stub que lança exceção). **CSV permanece tabular simples** (1 arquivo = 1 entidade, agora com classificação por IA confirmando/corrigindo o hint do nome). **JSON-array** (raiz `[{...}]`) permanece suportado como **um** bloco-candidato. **Por quê:** entregar o que destrava o caso real (job #11) sem abrir uma frente de parsing XLSX que é projeto à parte; o usuário foi firme que o foco é dump aninhado.

### D-N2 — Custo de IA: UMA chamada por bloco-candidato, que classifica E mapeia juntos (Ponto 2)

Para cada bloco-candidato (array de objetos no dump, ou o arquivo tabular inteiro), há **uma** chamada à IA que devolve **classificação + de-para na mesma resposta**. **Não** é uma chamada para classificar tudo de uma vez + N chamadas para mapear; **nem** duas chamadas por bloco (uma p/ classificar, outra p/ mapear). **É 1 chamada por bloco, fazendo as duas tarefas.** A amostra mascarada é **por bloco** (cabeçalhos + N linhas daquele bloco). **Por quê:** equilíbrio de custo e fidelidade. Classificar e mapear no mesmo passo usa o mesmo contexto (os cabeçalhos + amostra do bloco), evita uma rodada extra de tokens, e mantém o teto de custo proporcional ao **número de blocos** (entidades distintas), não ao número de linhas nem ao dobro de chamadas. Um dump com 7 blocos = no máximo 7 chamadas (vs. 1 por arquivo no modelo antigo — aceitável e ainda barato, pois é por entidade, não por linha). Mantém o espírito de CA23 ("1 chamada por arquivo, nunca por linha") generalizado para "1 chamada por **bloco-candidato**".

> **Otimização opcional (liberdade técnica, não-CA):** se o dev preferir, blocos podem ser agrupados numa **única** chamada que recebe os cabeçalhos+amostra de **todos** os blocos e devolve a classificação+mapa de todos — desde que (a) não estoure o limite de contexto do provider e (b) a amostra continue mascarada. O CA exige **no máximo** o teto "1 chamada por bloco"; ir abaixo disso (1 chamada para o dump inteiro) é permitido e bem-vindo se couber no contexto. O que **não** é permitido é passar do teto (ex.: 2 chamadas por bloco, ou 1 por linha).

### D-E1 — Encoding/mojibake: corrigir o caso UTF-8↔Latin-1 na ingestão; sinalizar o resto (Ponto 3)

A ingestão **normaliza o mojibake comum** (texto UTF-8 que foi decodificado como Latin-1/Windows-1252 — o caso "PlÃ¡stica"), de forma **determinística e conservadora**, **antes** de extrair amostra e antes de carregar. Quando a correção é **ambígua ou arriscada** (não dá para afirmar com segurança que é mojibake), **não** se altera o texto — mantém-se o original e **sinaliza-se ao operador** ("encoding suspeito neste arquivo — confira os acentos no preview"). **Por quê:** o mínimo que serve é consertar o caso dominante em PT-BR (quase todo mojibake de export brasileiro é esse), sem risco de corromper dado bom com heurística agressiva. Corrigir sempre que seguro melhora a qualidade da migração e do preview; sinalizar quando inseguro respeita "nunca inventar/corromper dado". **Não** é tarefa de IA — é normalização determinística na camada de parsing/ingestão.

### D-S4 — Sub-objetos dentro de registros: mapeia o plano, ignora o resto, nunca inventa (Ponto 4)

A IA e a carga operam sobre os **campos planos** (string, número, data, booleano) de cada registro. Campos cujo valor é **sub-objeto** (`campos_especificos:{}`) ou **array** (`subreparticoes_ids:[]`) **não** são mapeados para campos canônicos por padrão e **nunca** geram um campo inventado. O sub-objeto cru permanece visível no `payload_bruto` (auditoria), mas só entra no domínio se houver um campo canônico plano correspondente que a IA mapeie explicitamente. **Por quê:** "ignora o que não mapeia; nunca inventa" é a regra segura e espelha R3/D1 do épico (a IA nunca escreve dado, só diz para onde a coluna **plana** vai). Explodir sub-objetos em colunas é complexidade especulativa — backlog.

### D-S5 — Relações por id interno do dump: FORA do MVP; vínculo só por chave de negócio (Ponto 5)

Ids internos do sistema de origem que aparecem em referências entre blocos (`agendamentos[].paciente_id`, `prontuarios[].paciente_id`) **não** são usados para resolver vínculo — **proibido por R1/D11** (Imedto nunca grava/usa id externo). Resolver FK entre entidades de um mesmo dump por id de origem é **fase própria, fora deste addendum**. Consequência explícita e honesta ao operador: blocos dependentes (**agenda**, **prontuário**) que precisam de um paciente continuam resolvendo o vínculo **só por chave de negócio** (CPF/documento — R2/R8 do épico); quando o paciente não é resolvível por chave de negócio, o registro é **rejeitado com motivo** ("paciente não identificado" / "agendamento sem paciente correspondente") — o operador vê no relatório/motivos agregados (addendum 002) quantos caíram por isso. **Por quê:** não violar D11 é inegociável; tentar casar por id de origem abriria a porta para usar identificador externo como chave, exatamente o que o épico proíbe. O MVP entrega a decomposição e a classificação; a religação por id de origem (se um dia valer a pena, traduzindo id-origem→chave-de-negócio internamente sem persistir o id) é discovery futuro.

### D-S6 — "Sem equivalente" e objetos de config: o operador decide pular, nunca força mapeamento (Ponto 6)

Blocos que a IA classifica como `"sem_equivalente"` (ex.: `reparticoes[]`, `atendimentos[]` com HTML corrido) e **objetos únicos de config** (`estabelecimento{}` — não é array de registros) aparecem no painel **sinalizados como não-migráveis/ignoráveis**; o operador **vê e decide pular**, sem nunca ser obrigado a mapear. Objeto único de config nunca é tratado como entidade de carga (não é lista de registros). **Por quê:** honestidade de estrutura (mesmo princípio do R8 do épico — "não inventar evolução estruturada falsa"); forçar `reparticoes` a virar alguma entidade Imedto produziria lixo. O operador no loop é a salvaguarda.

## 5. Regras de negócio

**Bloco A — Parsing de dump aninhado:**

- **R-S1 (decomposição em blocos-candidatos)**: O parser JSON, ao receber um **objeto raiz**, enumera **todas** as propriedades; cada propriedade cujo valor é um **array de objetos** vira um bloco-candidato `{ nome_bloco, cabeçalhos (união das chaves dos objetos), amostra de até N linhas }`. Propriedade com valor **objeto único** → bloco de **config** (sinalizado, não migrável — D-S6). Propriedade escalar/array de escalares no topo → ignorada (metadado). Um JSON cuja **raiz é array de objetos** vira **um único** bloco-candidato (compatibilidade — não regride o JSON-array atual). Mora em: parser de migração (Infrastructure). Validada em: back.

- **R-S2 (sub-objetos não-planos preservados no bruto, não mapeados por padrão — D-S4)**: Ao derivar cabeçalhos/amostra de um bloco, campos com valor **objeto/array** são marcados como **não-planos**; entram no `payload_bruto` (raw), mas não são oferecidos como campo mapeável por padrão e **nunca** geram campo canônico inventado. Mora em: parser + montagem da amostra. Validada em: back.

**Bloco B — Classificação semântica por IA:**

- **R-S3 (a IA classifica a entidade — substitui o nome do arquivo)**: A porta `IMapeadorDeMigracao` evolui para, por bloco-candidato, devolver `{ entidade_classificada ∈ lista D-S1, confianca_classificacao, de_para_colunas, confianca_mapa, duvidas }` numa **única** chamada (D-N2). A classificação é feita pela IA sobre cabeçalhos + amostra mascarada do bloco + (opcional) o nome do bloco/arquivo como **hint**. A detecção por nome de arquivo (`DetectarEntidade`) deixa de decidir a entidade; no máximo vira hint de contexto e fallback de rótulo. Mora em: porta (Domain) + adapter (Infrastructure usando `IaService`). Validada em: back (o domínio não conhece prompt nem provider — ports & adapters preservado).

- **R-S4 (amostra mascarada por bloco — D2/CA5 preservado)**: A amostra enviada à IA por bloco-candidato passa pela mesma máscara de PII (`PiiSanitizer`/`IAnonimizacaoService`) já usada hoje, **antes** de qualquer chamada ao provider. O volume real **nunca** é enviado; só cabeçalhos + N linhas mascaradas por bloco. Mora em: handler de inferência. Validada em: back (verificável no payload da chamada ao provider).

- **R-S5 (teto de custo — no máximo 1 chamada por bloco — D-N2)**: A inferência faz **no máximo** 1 chamada à IA por bloco-candidato (classificação + mapa juntos). Agrupar todos os blocos numa única chamada é permitido (otimização); ultrapassar 1 chamada por bloco (ex.: 2 chamadas por bloco, ou 1 por linha) **não** é. Mora em: handler de inferência. Validada em: back (contagem de chamadas ao provider).

**Bloco C — Revisão da classificação:**

- **R-S6 (humano no loop sobre a classificação — espelha R7)**: Antes da carga, o operador **revisa e pode corrigir** a entidade classificada de cada bloco (incluindo marcar como "ignorar") e o de-para de colunas. A entidade final usada na carga é a do **operador** (que sobrepõe a da IA). Nada é gravado em domínio antes dessa revisão + disparo (gate de aprovação do addendum 003 continua **antes** da IA; revisão do mapa **depois**). Mora em: estado do job + UI admin. Validada em: back + front.

- **R-S7 (bloco "sem_equivalente"/config nunca é carregado sem ação — D-S6)**: Blocos classificados como `"sem_equivalente"` e objetos de config **não** entram na carga a menos que o operador **explicitamente** reclassifique para uma entidade canônica válida. O default é **pular**. O operador nunca é forçado a mapear. Mora em: estado do job + pipeline de carga. Validada em: back + front.

**Bloco D — Encoding:**

- **R-S8 (normalização de mojibake conservadora — D-E1)**: A ingestão detecta e corrige o caso UTF-8 decodificado como Latin-1/Windows-1252 (mojibake PT-BR comum), de forma determinística, antes da amostra e da carga. Quando a correção é insegura/ambígua, **não** altera o texto e **sinaliza** "encoding suspeito" ao operador. Nunca corrompe dado bom; nunca usa IA para isso. Mora em: camada de parsing/ingestão (Infrastructure). Validada em: back + front (sinalização visível).

**Bloco E — Vínculo (preservação do D11):**

- **R-S9 (vínculo só por chave de negócio — D-S5/R1/D11 reafirmado)**: Ids internos do dump **não** são gravados nem usados como chave para religar entidades. Agenda/prontuário dependentes de paciente resolvem o vínculo **só por chave de negócio** (CPF/documento); sem resolução → **rejeita com motivo** (reusa R2/R8 e os motivos agregados do addendum 002). Mora em: pipeline de carga (inalterada na essência; só explicitada para o caso dump). Validada em: back.

## 6. Modelo de dados

**Sem mudança de schema de domínio.** A carga continua usando os commands existentes; nenhuma tabela de paciente/estoque/orçamento/prontuário muda.

**Staging (`migracao_*`) — avaliação leve, com `imedto-database`:**
- O conceito de "bloco-candidato com entidade classificada por IA + entidade final do operador" precisa ser persistido para o painel de revisão. **Reuso preferencial:** `migracao_mapas` já guarda, por job+entidade, o `mapa_json` (de_para + confiança + dúvidas). A classificação da entidade (entidade_classificada pela IA, confiança da classificação, nome do bloco de origem, flag "ignorar") pode caber **dentro do `mapa_json`** (campos novos no JSON, **sem migration**) ou exigir 1 coluna leve em `migracao_mapas` (ex.: `entidade_classificada` / `nome_bloco_origem` / `ignorado`). **Decisão de forma = `imedto-database` + dev** conforme a query de revisão; o default do BA é **estender o `mapa_json`** (zero migration) se a leitura no painel não exigir filtro por coluna. Se exigir coluna, migration **idempotente** (gotcha conhecido `AddColumn` — `Sql` com `IF NOT EXISTS`).
- A relação "1 arquivo → N entidades" passa a ser "1 job → N mapas" (já é a cardinalidade de `migracao_mapas`: PK por job+entidade). Um dump aninhado gera **N registros em `migracao_mapas`** (um por bloco aceito), exatamente como hoje um ZIP com N arquivos gera N mapas — **a cardinalidade do staging já suporta**, só muda a origem dos blocos (de "N arquivos" para "N blocos de 1 arquivo"). Confirmar com `imedto-database` que `(migracao_job_id, entidade)` como chave de `migracao_mapas` comporta dois blocos que a IA classifique na **mesma** entidade (ex.: dois arrays que ambos são "paciente") — se isso for possível no dump, a chave precisa incluir o `nome_bloco_origem` para não colidir. **Ponto de atenção para `imedto-database`** (ver §9).

**Sinalização de encoding suspeito (D-E1):** flag leve por arquivo/bloco — cabe no `mapa_json` ou num campo de staging; sem migration se via JSON. Decisão de forma = dev/db.

**Audit / LGPD:** a classificação e o de-para são **metadados de schema, sem PII** (categorias e nomes de campo, não valores). A amostra mascarada continua a única coisa que vai à IA. Os eventos de transição (addendum 004) seguem cobrindo as mudanças de estado do job. Nenhum dado novo de paciente é tocado.

## 7. UX e fluxo

**Fluxo-alvo (estende §6/§7 do épico no ponto da inferência e da revisão):**

1. Cliente sobe ZIP (pode ser **um** JSON-dump aninhado, ou os N arquivos tabulares de sempre) → job em `aguardando_aprovacao` (gate do addendum 003 inalterado).
2. Operador **aprova a análise** (addendum 003) → job vai para `aguardando_mapa`.
3. O recorrente `inferir-mapa-migracao`:
   - se o arquivo é **dump aninhado**, decompõe em blocos-candidatos (R-S1); se é **tabular**, é um bloco;
   - normaliza encoding (R-S8);
   - para **cada bloco**, mascara a amostra e faz **1 chamada à IA** que **classifica + mapeia** (R-S3/R-S5);
   - persiste um mapa por bloco aceito (com a entidade classificada).
4. Operador na **revisão**: vê os blocos com a entidade que a IA propôs (+ confiança), **corrige** entidade onde a IA errou, **marca "ignorar"** os `"sem_equivalente"`/config, ajusta o de-para. Confirma.
5. Preview → "Migrar" → carga → relatório → desfazer: **inalterados** (operam sobre os blocos aceitos com a entidade final).

**Detalhe / revisão (`MigracaoRevisaoView`):**
- Por bloco-candidato: cabeçalho com **nome do bloco de origem** (ex.: "pacientes" / "reparticoes") + **entidade classificada** pela IA (badge) + **confiança**. Um **seletor de entidade** (lista D-S1 + "Ignorar este bloco") para o operador corrigir. Abaixo, o de-para de colunas daquele bloco (como hoje). Blocos `"sem_equivalente"`/config vêm com "Ignorar" pré-selecionado e um aviso ("sem entidade equivalente — será pulado").
- **Aviso de encoding suspeito** (D-E1) quando aplicável: faixa/nota no bloco ("Acentuação pode estar incorreta neste arquivo — confira o preview").
- Estados: loading, bloco com baixa confiança destacado (reusa o padrão de confiança/dúvidas já existente), bloco ignorado (visualmente apagado/colapsado), erro genérico.
- Componentes do DS: reusar `AppBadge` (entidade/confiança), o seletor padrão do projeto (verificar `AppPillToggle`/select existente antes de criar — backlog de doc conhecido), `AppEmptyState`. Tipografia **sempre por tokens** (CLAUDE.md §5).

**Lado do cliente:** inalterado — o cliente não vê blocos nem classificação; só sobe o ZIP e acompanha o status (addendums 003/004).

**Service/DTO front:** o DTO do mapa por entidade ganha `nomeBlocoOrigem`, `entidadeClassificada`, `confiancaClassificacao`, `ignorado`, `encodingSuspeito`. O service de revisão ganha a ação de **reclassificar/ignorar** bloco (pode ser parte do salvar-mapa existente). Liberdade técnica na forma exata, desde que os CAs passem.

## 8. Critérios de aceite (testáveis) — começam em CA70

**Bloco A — Decomposição de dump aninhado:**

- **CA70 (dump aninhado vira N blocos — R-S1)**: Dado um ZIP com **um** arquivo JSON cujo objeto raiz tem `pacientes[]` (1.000 objetos), `agendamentos[]` (3.000), `reparticoes[]` (10) e `estabelecimento{}` (objeto único), Quando a inferência roda, Então a pipeline produz **um bloco-candidato por array de objetos** (`pacientes`, `agendamentos`, `reparticoes`) com seus cabeçalhos e amostra, e trata `estabelecimento{}` como **config sinalizada** (não vira bloco migrável) — **nenhum** array de objetos é descartado (corrige a limitação do `EncontrarPrimeiroArray`).

- **CA71 (compatibilidade — JSON-array não regride — R-S1)**: Dado um arquivo JSON cuja **raiz é um array de objetos** (`[{...},{...}]`, o formato já suportado), Quando a inferência roda, Então ele é tratado como **um único** bloco-candidato exatamente como antes, e o fluxo tabular existente **não regride** (mesmo resultado de mapa/carga que hoje).

- **CA72 (sub-objeto não-plano preservado, não inventado — R-S2/D-S4)**: Dado um registro de bloco com `nome` (string), `campos_especificos:{...}` (sub-objeto) e `subreparticoes_ids:[...]` (array), Quando a amostra é montada e a IA mapeia, Então `nome` (plano) é candidato a mapeamento, e `campos_especificos`/`subreparticoes_ids` **não** são mapeados para campos canônicos nem geram campo inventado; o conteúdo cru permanece no `payload_bruto` para auditoria.

**Bloco B — Classificação semântica:**

- **CA73 (IA classifica a entidade, não o nome do arquivo — R-S3)**: Dado um dump chamado `sistema_hospitalar_backup_2026.json` (nome inútil) cujo bloco `pacientes[]` tem colunas tipo CPF/nome/nascimento, Quando a inferência roda, Então o bloco é classificado pela IA como **`paciente`** (não como `"sistema_hospitalar_backup_2026"`), e o de-para de colunas é produzido na **mesma** resposta da IA.

- **CA74 (1 chamada por bloco, classifica+mapeia — R-S5/D-N2)**: Dado um dump com 5 blocos-candidatos de objetos, Quando a inferência roda, Então há **no máximo 5** chamadas ao provider de IA (uma por bloco, classificando e mapeando juntas) — **nunca** 1 por linha nem 2 por bloco; (e é aceitável **1 única** chamada para o dump inteiro se o dev optar pela otimização, desde que ≤ teto).

- **CA75 (amostra mascarada por bloco — R-S4/D2/CA5 preservado)**: Dado um bloco `pacientes[]` com CPF e nome reais, Quando a IA é chamada para classificar+mapear esse bloco, Então a porta recebe **apenas** cabeçalhos + amostra de N linhas com PII **mascarada** (via `PiiSanitizer`/`IAnonimizacaoService`); o volume real e o valor real de CPF/nome **nunca** são enviados ao provider (verificável no payload).

**Bloco E — Vínculo sem id externo (preservação D11):**

- **CA76 (não usa id interno do dump; vínculo por chave de negócio — R-S9/R1/D11)**: Dado um dump com `agendamentos[]` que referenciam `paciente_id` (id interno do sistema de origem) e `pacientes[]` que têm CPF, Quando a carga roda, Então o `paciente_id` de origem **não** é gravado em nenhuma coluna de domínio nem usado como chave; a agenda resolve o vínculo ao paciente **por chave de negócio** (paciente migrado por CPF), e a agenda cujo paciente **não** é resolvível por chave de negócio é **rejeitada com motivo** ("agendamento sem paciente correspondente") — contado nos motivos agregados (addendum 002).

**Bloco C — Revisão da classificação:**

- **CA77 (operador corrige a classificação — R-S6)**: Dado um bloco que a IA classificou como `item_estoque` mas que o operador identifica como `produto_orcamento`, Quando o operador altera a entidade no seletor de revisão e confirma, Então a carga usa a entidade **do operador** (`produto_orcamento`), não a da IA.

- **CA78 ("sem equivalente" é pulado por padrão, nunca forçado — R-S7/D-S6/Ponto 6)**: Dado um bloco `reparticoes[]` classificado pela IA como `"sem_equivalente"`, Quando o operador revisa, Então o bloco aparece com "Ignorar" pré-selecionado e um aviso; se o operador **não** agir, o bloco é **pulado** (não carregado); o operador **nunca** é obrigado a mapeá-lo. E dado `estabelecimento{}` (objeto config), Então ele aparece sinalizado como config não-migrável, nunca como entidade de carga.

- **CA79 (humano no loop antes de gravar — R-S6/R7 preservado)**: Dado qualquer dump classificado, Quando a IA termina, Então **nada** é gravado em tabela de domínio antes de o operador revisar a classificação + o mapa e disparar "Migrar"; o gate de aprovação do addendum 003 (CA44) permanece **antes** da chamada de IA.

**Bloco D — Encoding:**

- **CA80 (mojibake UTF-8↔Latin-1 corrigido na ingestão — R-S8/D-E1)**: Dado um arquivo cujo valor é "Cirurgia PlÃ¡stica" (UTF-8 lido como Latin-1), Quando a ingestão normaliza, Então o valor vira "Cirurgia Plástica" **antes** da amostra à IA e antes da carga; o dado carregado fica correto.

- **CA81 (encoding ambíguo é sinalizado, não corrompido — R-S8/D-E1)**: Dado um arquivo cujo encoding é ambíguo (não dá para afirmar com segurança que é mojibake), Quando a ingestão processa, Então o texto **não** é alterado (não corrompe dado bom) e o operador vê um aviso de "encoding suspeito" no bloco/preview; nenhuma heurística agressiva reescreve o conteúdo.

**LGPD, multi-tenant, RBAC e regressão:**

- **CA82 (LGPD — classificação e mapa sem PII)**: Dado qualquer bloco classificado, Quando a classificação + de-para são persistidos e exibidos, Então contêm **apenas** metadados de schema (nome do bloco, entidade canônica, nomes de coluna→campo, confiança, dúvidas) — **nunca** CPF, nome de paciente, valor real de coluna; nenhum log carrega PII.

- **CA83 (multi-tenant — herdado)**: Dado um job de dump, Quando os blocos/mapas são persistidos e lidos no painel, Então tudo carrega o `estabelecimento_id` do job; um operador/job de outro contexto recebe "não encontrado" genérico; nenhum dado de tenant alheio vaza.

- **CA84 (RBAC — ImedtoAdmin)**: Dado um usuário sem a policy `ImedtoAdmin`, Quando tenta revisar/reclassificar blocos ou disparar a carga, Então recebe **403** e os controles de classificação ficam ocultos no front (reuso da policy do `AdminMigracaoController`).

- **CA85 (regressão — fluxo tabular e épico intactos)**: Dado um ZIP com os arquivos tabulares de sempre (`pacientes.csv`, `agendamentos.csv`, JSON-array), Quando todo o fluxo dos briefings 001–004 roda (upload → aprovação → inferência → revisão → preview → carga → relatório com motivos → falha+reprocessar → gate anti-IA CA44 → timeline/progresso/filtros CA51-69), Então **nada** regride: a carga continua por commands, o upsert por chave de negócio (R2) não duplica, a ordem de FK (R5) é respeitada, o gate anti-IA (CA44) e o relatório só-após-concluir continuam valendo. A classificação por IA **substitui** a detecção por nome **sem** quebrar o caminho de 1 arquivo = 1 entidade.

## 9. Riscos e dependências

- **Regressão do caminho tabular (o mais importante)**: a substituição de "entidade por nome de arquivo" por "classificação por IA" não pode quebrar CSV/JSON-array já suportados. **CA71 e CA85 são os guardiões** — devem virar testes automatizados. Risco: a IA classificar errado um arquivo tabular óbvio que antes o nome resolvia — mitigado passando o nome do arquivo como **hint** à IA (R-S3) e pelo operador no loop (R-S6).
- **Colisão de chave em `migracao_mapas` para dois blocos da mesma entidade**: um dump pode ter dois arrays que a IA classifica na **mesma** entidade (raro, mas possível). A chave `(migracao_job_id, entidade)` colidiria. **`imedto-database` deve avaliar** incluir `nome_bloco_origem` na chave/identidade do mapa. Ponto de atenção de schema (§6).
- **Custo de IA por número de blocos**: um dump com muitos blocos = muitas chamadas (1 por bloco). Mitigado pelo teto D-N2 (por bloco, não por linha) e pela otimização opcional (1 chamada para o dump inteiro se couber no contexto). O gate de aprovação do addendum 003 continua sendo o controle de **quando** o gasto ocorre.
- **IA classifica `"sem_equivalente"` em excesso** (conservadora demais) ou força entidade errada — mitigado pelo operador no loop (R-S6/R-S7); confiança da classificação destacada no painel.
- **Encoding**: correção agressiva poderia corromper dado bom — mitigado por D-E1 (conservador: corrige só o caso seguro, sinaliza o resto). CA81 é o guardião.
- **id externo do dump (D11)**: a tentação de religar entidades por id de origem é forte e é exatamente o que o épico proíbe. R-S9/CA76 blindam; resolver FK por id-origem fica para discovery futuro, **fora** deste MVP.
- **Área regressiva**: nenhuma de domínio. Risco contido no bounded context de migração (parsing + inferência + revisão). Commands de paciente/estoque/orçamento/prontuário **não** são tocados.

## 10. Observações para execução

- **Reuso obrigatório (não construir do zero)**: a inferência reusa o `InferirMapaMigracaoJobHandler` (só evolui o passo de detecção→classificação e o loop de "1 arquivo" para "N blocos"); a porta `IMapeadorDeMigracao` **evolui** (não duplica) para devolver entidade classificada + mapa; o `JsonMigracaoParser` evolui para decompor o objeto raiz inteiro (corrige `EncontrarPrimeiroArray`); a máscara de PII reusa `PiiSanitizer`/`IAnonimizacaoService`; a UI reusa a tela de revisão + `AppBadge` + o seletor padrão; a carga, o preview, o relatório, o desfazer e o gate de aprovação (003) **não mudam**.
- **Não-negociável (D1/D2 preservados)**: a IA **só** classifica/mapeia schema sobre **amostra mascarada por bloco** — **nunca** transforma registro a registro, **nunca** recebe o volume real, **nunca** recebe PII não mascarada (R-S3/R-S4/CA75).
- **Não-negociável (D11 preservado)**: id interno do dump **nunca** é gravado nem usado como chave; vínculo só por chave de negócio (R-S9/CA76).
- **Não-negociável (humano no loop)**: gate de aprovação do addendum 003 **antes** da IA (CA44 intacto); revisão da classificação + mapa **antes** da carga (R-S6/CA79); operador nunca é forçado a mapear "sem_equivalente" (R-S7/CA78).
- **Não-negociável (custo)**: ≤ 1 chamada à IA por bloco-candidato (R-S5/CA74); a IA classifica e mapeia **juntas**.
- **Não-negociável (não regredir tabular)**: CSV/JSON-array já suportados continuam funcionando (CA71/CA85) — testes automatizados.
- **Liberdade técnica**: a forma de persistir a classificação (estender `mapa_json` vs. coluna leve em `migracao_mapas` — default: estender o JSON, zero migration); a estratégia de detecção de mojibake (biblioteca vs. heurística round-trip Latin-1→UTF-8); agrupar ou não os blocos numa única chamada de IA (desde que ≤ teto); o componente exato de seletor de entidade na UI; o tamanho da amostra por bloco (reusar `TamanhoAmostra = 10` atual). Tudo desde que respeite os CAs.
- **Acionar `imedto-database`** para: (1) decidir se a classificação cabe no `mapa_json` (sem migration) ou exige coluna(s) leve(s) em `migracao_mapas` (`entidade_classificada`, `nome_bloco_origem`, `ignorado`, `encoding_suspeito`); (2) **avaliar a colisão de chave** `(migracao_job_id, entidade)` quando dois blocos do mesmo dump são a mesma entidade — possivelmente incluir `nome_bloco_origem` na identidade do mapa; (3) confirmar que não há migration de domínio. Migration idempotente se houver coluna (gotcha `AddColumn`).
- **Fatiamento sugerido** (PRs pequenos sob este mesmo ID): PR 1 = Bloco A (parser de dump aninhado + sub-objetos) + Bloco D (encoding) — base de parsing, com CA70-72, CA80-81. PR 2 = Bloco B (evolução da porta + classificação na inferência) — CA73-76, CA82. PR 3 = Bloco C (revisão da classificação no admin) — CA77-79, CA84. Regressão (CA71/CA85) validada em todos.

## 11. Atualização de documentação

- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — **atualizar nesta entrega** com a evolução: registrar que a estratégia evoluiu de **"detecção de entidade por nome de arquivo + 1 arquivo = 1 entidade tabular"** para **"classificação semântica de entidades pela IA + suporte a dump JSON aninhado (decomposição em blocos-candidatos)"**, mantendo D1/D2 (schema + amostra mascarada) e D11 (sem id externo) intactos. Nota incremental nova (não reescrever D1–D14): a IA passa a ter **duas** tarefas sobre os mesmos metadados — classificar a entidade **e** mapear as colunas — numa chamada por bloco; XLSX multi-aba e FK por id-origem ficam como backlog explícito. **Feito nesta entrega pelo BA.**
- **`Docs/ARQUITETURA.md`** — atualizar a descrição do **bounded context de migração**: (1) a inferência decompõe dump aninhado em blocos-candidatos (não mais "1 arquivo = 1 entidade"); (2) a entidade é **classificada pela IA** (porta `IMapeadorDeMigracao` evoluída), não pelo nome do arquivo; (3) normalização de encoding determinística na ingestão. Incremental: nota na seção de migração. **A ser feito no PR pelo dev/BA no marco do Bloco B.**
- **`Docs/LGPD.md`** — nota incremental: a classificação de entidade e o de-para continuam **sem PII** (metadados de schema); a amostra por bloco continua mascarada (mesma garantia do épico/CA5). Só atualizar se a seção de migração já listar as garantias de não-PII; caso contrário, coberto pela regra geral. Avaliar no marco.
- **`Docs/INFRA.md`** — sem mudança de infra. **Não atualizar.** (A dependência da chave de IA no SSM permanece a do addendum 002.)
