# Estruturar as seções "Evolução pós-operatória" e "Descrição cirúrgica" da evolução do prontuário

**ID**: 2026-06-21_001
**Status**: Aprovado por usuário em 2026-06-21
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: prontuário (evolução, timeline de leitura, PDF, resumo), design system (componentes de seção), relatório (PDF/leitura). Sem toque em financeiro, estoque, permissionamento.

---

## 1. Contexto e motivação

No legado (Vue 2 + Supabase), as seções **Evolução pós-operatória** e **Descrição cirúrgica** da evolução clínica eram formulários estruturados (selects, toggles, checkboxes, listas repetíveis, durações calculadas) — ver `ReferenciaLegado/Imedto/src/modules/medical-record/components/EvolucaoPosOperatoriaSection.vue` e `DescricaoCirurgicaSection.vue`.

No refactor atual, ambas as seções foram catalogadas como `texto_longo` (textarea de texto livre) em `frontend/src/components/ui/modeloProntuarioBuilder.ts:23-24`. O profissional perdeu a estrutura: hoje digita tudo num campo livre, sem padronização clínica, sem cálculo de DPO, sem cálculo de duração, sem checklist de profilaxia.

A demanda é recuperar a estrutura dessas DUAS seções no fluxo de **evolução do prontuário** (não na aba `CirurgiaView`/`ProcedimentoCirurgico` — que é outro aggregate).

**Por que agora é viável de forma cirúrgica**: o refactor já tem o padrão pronto. O dispatcher `SecaoProntuario.vue` já roteia 7 seções para componentes estruturados dedicados (`hpp`, `h-familiar`, `h-social`, `exame-fisico`, `exames-realizados`, `procedimentos-indicados`, `conduta`) e cai num textarea genérico para o resto. O conteúdo da evolução é JSONB flexível por chave; o backend grava o JSON cru sem validar shape. Logo, **adicionar duas seções estruturadas é trabalho de frontend + documentação, sem migration e sem mudar contrato de backend.**

---

## 2. Persona-alvo

Profissional de saúde (médico cirurgião / equipe), papel `Profissional` ou `Dono`, durante o registro de uma evolução clínica de um paciente operado (pós-operatório imediato/tardio e relato do ato cirúrgico). Frequência: toda consulta de retorno pós-cirúrgico e todo registro de cirurgia realizada. Também impacta quem **lê** a evolução depois (mesma persona + recepção com `prontuario.ver`) e quem **exporta** o PDF.

---

## 3. Escopo

**Inclui:**
- Transformar a seção `evolucao-pos-op` de `texto_longo` para uma seção **estruturada** (componente dedicado, objeto JSONB).
- Transformar a seção `desc-cirurgica` de `texto_longo` para uma seção **estruturada** (componente dedicado, objeto JSONB).
- Formatadores curados em `useEvolucaoResumo.ts` (`FORMATADORES_CURADOS`) para essas duas chaves, para que a leitura readonly (drawer), o resumo da timeline e o PDF exibam texto legível em vez de JSON/genérico.
- Retrocompatibilidade total com evoluções já gravadas (texto livre antigo continua legível).
- Cálculo de DPO (pós-op) e duração da cirurgia (descrição cirúrgica) no front, no momento da edição.
- Atualização de `Docs/DESIGN.md` (componentes novos de seção) — premissa de documentação viva.

**Não inclui (decisões de produto já fechadas com o usuário):**
- **Seção pré-operatória**: NÃO ganha campos novos. No legado a "pré-op" não tinha campos próprios (era apenas `exames-realizados` + `procedimentos-indicados` + `conduta`, que já existem estruturados). Fora do escopo.
- **Anestesia dentro da descrição cirúrgica**: a descrição cirúrgica NÃO duplica "tipo de anestesia" nem horários/duração de anestesia (presentes no legado). Esses dados pertencem à **Ficha anestésica** (premissa reuso > duplicação). A descrição cirúrgica mantém **apenas a duração da CIRURGIA** (início/fim da cirurgia), nunca da anestesia. Ver R6 e seção 9.
- Estruturar a seção `ficha-anestesica` em si (hoje é textarea livre no refactor). Ver "Observações para execução" e ambiguidade resolvida AR-1 — fora do escopo desta entrega, mas registrado como dependência conhecida.
- Edição/exclusão de evolução já criada (evolução é imutável/append-only no backend — ver R8). Esta entrega não cria tela de edição.
- Mudança de schema/banco. Conteúdo é JSONB flexível; nada de migration.
- Mudança no aggregate `ProcedimentoCirurgico` / `CirurgiaView`.

---

## 4. Regras de negócio

- **R1 — Estrutura é frontend; persistência é JSONB cru.** Cada seção estruturada gerencia um **objeto** (não string) e é serializado dentro do `conteudoJson` da evolução pela chave da seção (`conteudo["evolucao-pos-op"]`, `conteudo["desc-cirurgica"]`). Mora em: Front (componentes de seção + serialização já existente em `prontuarioService.registrarEvolucao`). Backend grava sem validar shape (`RegistrarEvolucaoCommandHandler`). Validada em: front (UX/montagem do objeto). Não há validação de shape no back — é o padrão atual das outras 7 seções estruturadas e deve ser mantido.

- **R2 — DPO calculado, readonly.** Na pós-op, DPO (dias pós-operatório) = `hoje − data da cirurgia`, em dias inteiros, somente quando `data da cirurgia` está preenchida e não é futura. Campo readonly (o usuário nunca digita). Se a data não está preenchida ou é futura, DPO fica vazio. Mora em: Front (componente da seção pós-op). Validada em: front.

- **R3 — Dia da semana auto-derivado, readonly (descrição cirúrgica).** Derivado da `data` da cirurgia; readonly. Mora em: Front. Validada em: front.

- **R4 — Duração da cirurgia calculada, readonly.** Duração = `fim − início` (HH:MM). Se fim < início, assume virada de dia (+24h), espelhando o legado (`DescricaoCirurgicaSection.vue:447-451`). Exibida como `HH:MM`; vazia (`--:--`) se faltar início ou fim. Mora em: Front. Validada em: front.

- **R5 — "Outro" condicional (profilaxia).** O campo de texto "Outro" (antitrombótica e antibiótica) só é habilitado/persistido quando o respectivo checkbox "Outro" está marcado. Mora em: Front. Validada em: front.

- **R6 — Intercorrências condicional.** A descrição de intercorrência só é exibida/persistida quando o toggle está em "Com intercorrência". Toggle em "Sem intercorrências" → descrição vazia/omitida. Mora em: Front. Validada em: front.

- **R7 — Sem duplicação de anestesia na descrição cirúrgica.** A descrição cirúrgica NÃO contém tipo de anestesia, início/fim de anestesia nem duração de anestesia. O campo "Anestesista" (nome do profissional, dado de equipe) PERMANECE na descrição cirúrgica (é identificação de equipe, não dado clínico de anestesia). Os dados clínicos de anestesia vivem na Ficha anestésica (seção irmã). Mora em: decisão de produto/Front. Validada em: ausência dos campos no objeto + revisão de QA.

- **R8 — Evolução é imutável.** Não há endpoint de editar/excluir evolução no backend (`RegistrarEvolucaoCommandHandler` é append-only; soft-delete existe só como método de domínio não exposto). Logo: ao **criar** uma nova evolução o formulário é editável; ao **visualizar** uma evolução já salva (drawer/timeline/PDF) é sempre readonly e renderizado via `formatarSecaoLegivel`, nunca via formulário editável. Mora em: Front (visualização) + Backend (não expõe edição). Validada em: back (ausência de endpoint) + front (drawer sem edição).

- **R9 — Multi-tenant herdado.** Registro e leitura de evolução já filtram `EstabelecimentoId` em 3 níveis no backend (paciente, prontuário, modelo — `RegistrarEvolucaoCommandHandler:44,49,55`), com tenant vindo do JWT via `ICurrentTenantAccessor`. Esta entrega NÃO altera isso e NÃO pode introduzir nenhuma query/endpoint novo de domínio. Mora em: Backend (já existente). Validada em: back + regressão.

- **R10 — Audit LGPD herdado.** Registrar evolução grava `ProntuarioAcessoLog` tipo `Escrita`; obter prontuário grava `Leitura`; exportar grava `Exportacao` (já existentes). Esta entrega não altera o fluxo de audit. Mora em: Backend (já existente). Validada em: regressão.

- **R11 — Retrocompatibilidade de leitura.** Evoluções gravadas antes desta entrega têm `conteudo["evolucao-pos-op"]` / `conteudo["desc-cirurgica"]` como **string** (texto livre). Na visualização/resumo/PDF, string continua legível como texto puro (o `formatarSecaoLegivel` cai em `formatarGenerico`, que trata string trimada). Os formatadores curados novos só atuam quando o valor é **objeto** (`typeof === "object" && !Array.isArray`); para string, devem deixar passar para o fallback. Mora em: Front (`useEvolucaoResumo.ts`). Validada em: front + teste unitário com fixture legado (string) e fixture novo (objeto).

- **R12 — Tipografia por token.** Premissa não-negociável (CLAUDE.md §5): nenhum `font-size`/`font-weight` literal em CSS scoped; só tokens. Labels via `AppField`/`AppLabel`/classes do DS. Mora em: Front. Validada em: `npm run check:typography -- --ci`.

---

## 5. Modelo de dados

**Nenhuma migration. Nenhuma alteração de schema.**

O conteúdo da evolução já é a coluna JSONB `prontuario_evolucoes.conteudo` (entidade `ProntuarioEvolucao`, propriedade `ConteudoJson`, imutável). As seções estruturadas são objetos serializados dentro desse JSONB pela chave da seção. O snapshot do modelo (`modelo_snapshot`) também já existe.

**Shape dos objetos (definição canônica — o dev deve seguir exatamente estes nomes de campo para os formatadores curados baterem):**

### `conteudo["evolucao-pos-op"]` (objeto)
| Campo | Tipo | Valores |
|---|---|---|
| `evolucaoPaciente` | string (enum) | `"otima"` \| `"boa"` \| `"regular"` \| `"ruim"` \| `""` |
| `evolucaoComentario` | string | texto curto |
| `seguindoOrientacoes` | string (enum) | `"sim"` \| `"nao"` \| `""` |
| `orientacoesComentario` | string | texto curto |
| `dataCirurgia` | string | `YYYY-MM-DD` |
| `dpo` | string/number | calculado readonly (dias) |
| `destino` | string (enum) | `"Enfermaria"` \| `"UTI"` \| `"RPA"` \| `"Alta"` \| `""` |
| `dieta` | string (enum) | `"Zero"` \| `"Líquida"` \| `"Pastosa"` \| `"Branda"` \| `"Livre"` \| `""` |
| `observacao` | string | texto longo |

### `conteudo["desc-cirurgica"]` (objeto)
| Campo | Tipo | Valores / observação |
|---|---|---|
| `cirurgiao` | string | obrigatório no UX (label com `*`); não bloqueia o salvar da evolução |
| `data` | string | `YYYY-MM-DD` |
| `cirurgiasRealizadas` | string | texto |
| `anestesista` | string | nome (dado de equipe — permanece; ver R7) |
| `auxiliar` | string | nome |
| `instrumentador` | string | nome |
| `outrosMembros` | array de `{ funcao: string, nome: string }` | lista repetível |
| `cirurgiaInicio` | string | `HH:MM` |
| `cirurgiaFim` | string | `HH:MM` |
| `profilaxia` | objeto | ver abaixo |
| `intercorrencia` | string (enum) | `"sem"` \| `"com"` \| `""` |
| `intercorrenciaDescricao` | string | só quando `intercorrencia === "com"` |
| `tecnicaOperatoria` | string | texto longo |
| `observacoes` | string | texto longo |

`profilaxia` (objeto):
| Campo | Tipo |
|---|---|
| `enoxaparina` | boolean |
| `meiaCompressiva` | boolean |
| `botaPneumatica` | boolean |
| `deambulacaoPrecoce` | boolean |
| `antitrombOutroAtivo` | boolean |
| `antitrombOutro` | string |
| `cefazolina` | boolean |
| `gentamicina` | boolean |
| `antibioOutroAtivo` | boolean |
| `antibioOutro` | string |

> **NÃO incluir** (decisão 3 / R7): `tipoAnestesia`, `tipoAnestesiaOutro`, `anestesiaInicio`, `anestesiaFim`. Existem no legado — devem ser deliberadamente OMITIDOS.

**PII/retenção/audit:** os campos são dado clínico sensível (Art. 11 LGPD), mas vivem dentro do JSONB da evolução que já é coberto pelo audit (`ProntuarioAcessoLog`) e pela retenção do prontuário. Minimização: os DTOs de leitura já existentes não mudam; nenhum campo novo é exposto fora do conteúdo da evolução. Nenhum nome de profissional/paciente desses objetos pode vazar em log ou mensagem de erro.

---

## 6. UX e fluxo

Dois componentes novos no diretório `frontend/src/components/prontuario/secoes/`, seguindo o padrão exato dos 7 existentes (props `{ modelValue: objeto, readOnly?: boolean }`, emit `update:modelValue`, helper `atualizar(patch)` com spread merge, `:disabled="readOnly"` em todos os controles, design system primeiro):

- `SecaoEvolucaoPosOperatoria.vue` (chave `evolucao-pos-op`)
- `SecaoDescricaoCirurgica.vue` (chave `desc-cirurgica`)

Ambos registrados no dispatcher `SecaoProntuario.vue` com novo `v-else-if="chave === 'evolucao-pos-op'"` e `chave === 'desc-cirurgica'`, recebendo `v-model="valorEstrutura"` e `:read-only="readOnly"` (mesmo padrão das demais).

**Wireframe textual — pós-op:**
- Linha: "Como está a evolução?" → toggle/segmented (Ótima/Boa/Regular/Ruim) + input "Comentário".
- Linha: "Seguindo orientações?" → toggle (Sim/Não) + input "Comentário".
- Grid 4 colunas: Data da cirurgia (date picker) | DPO (input readonly) | Destino (select) | Dieta (select).
- Observação (textarea).

**Wireframe textual — descrição cirúrgica (cards/painéis):**
- **Identificação**: Cirurgião* (input) | Data (date picker) + Dia da semana (input readonly) | Cirurgia(s) realizada(s) (input).
- **Equipe**: Anestesista | Auxiliar | Instrumentador(a) (3 inputs) + lista repetível "Outros membros" (Função + Nome, com Adicionar/Remover).
- **Duração da cirurgia**: Início (hora) | Fim (hora) | Duração (readonly, calculada). **Apenas cirurgia — sem bloco de anestesia.**
- **Profilaxia**: coluna Antitrombótica (4 checkboxes fixos + checkbox "Outro" com input) | coluna Antibiótica (2 checkboxes fixos + checkbox "Outro" com input).
- **Intercorrências**: toggle Sem/Com + textarea (só quando "Com").
- **Técnica operatória** (textarea).
- **Observações** (textarea).

**Componentes do design system a reutilizar** (de `frontend/src/components/ui/`): `AppInput`, `AppSelect`, `AppTextarea`, `AppCheckbox`, `AppDatePicker`, `AppButton`, `AppField`/`AppLabel`. Toggle segmentado (Ótima/Boa/Regular/Ruim, Sim/Não, Sem/Com): usar `AppPillToggle` se atender; caso contrário, usar `AppSelect`/`AppButton` em grupo — decidir no design system, não inline.

**Campo de hora (início/fim da cirurgia):** o design system **não possui** componente de hora hoje (confirmado: sem `AppTimeInput`/`AppTimePicker`/`type="time"` em `components/ui/`). Comportamento de produto não-negociável: dois campos de hora no formato `HH:MM` (24h) e a duração calculada (R4). A FORMA (input `type="time"` nativo encapsulado num campo do DS, ou novo `AppTimeInput` no design system) é liberdade técnica do dev/design — mas se criar componente novo, **registrar em `Docs/DESIGN.md`** (documentação viva).

**Estados:**
- **Vazio/novo**: formulário em branco (objeto vazio) ao iniciar uma nova evolução.
- **Readonly (evolução salva)**: NÃO renderiza o formulário; renderiza o texto legível via `formatarSecaoLegivel` no `EvolucaoDetalheDrawer.vue` e no PDF (`useProntuarioPdf.ts`). Evolução é imutável (R8).
- **Erro**: erros de back genéricos (sem PII).
- **Sucesso**: evolução registrada entra na timeline.

**Mobile-ready**: grids colapsam para 1 coluna em telas estreitas (espelha `grid-cols-1 md:grid-cols-*` do legado).

---

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — pós-op estruturado):** Dado um profissional registrando uma evolução num modelo que inclui a seção `evolucao-pos-op`, Quando ele seleciona "Boa", marca "Seguindo orientações: Sim", informa data da cirurgia, escolhe Destino "Alta" e Dieta "Livre", preenche observação e salva, Então a evolução é criada e `conteudo["evolucao-pos-op"]` é gravado como **objeto** com os campos da seção 5 (não como string).

- **CA2 (caminho feliz — descrição cirúrgica estruturada):** Dado um profissional registrando evolução com a seção `desc-cirurgica`, Quando preenche cirurgião, cirurgia realizada, equipe (incluindo 1 item em "Outros membros"), início/fim da cirurgia, marca profilaxias e técnica operatória e salva, Então a evolução é criada e `conteudo["desc-cirurgica"]` é gravado como **objeto** com os campos da seção 5, incluindo `outrosMembros` como array.

- **CA3 (DPO calculado):** Dado o campo "Data da cirurgia" preenchido com uma data 5 dias atrás, Quando a seção pós-op renderiza, Então o campo DPO exibe `5`, é readonly e o usuário não consegue editá-lo. E Dado a data em branco ou futura, Então DPO fica vazio.

- **CA4 (dia da semana auto-derivado):** Dado "Data" da descrição cirúrgica preenchida, Quando renderiza, Então "Dia da semana" exibe o dia correto em português (ex.: "Terça-feira"), readonly.

- **CA5 (duração da cirurgia calculada + virada de dia):** Dado início `08:00` e fim `10:30`, Quando renderiza, Então duração exibe `02:30`. E Dado início `23:30` e fim `01:00`, Então duração exibe `01:30` (virada de dia). E Dado faltar início ou fim, Então exibe `--:--`.

- **CA6 (sem duplicação de anestesia — R7):** Dado a seção `desc-cirurgica`, Quando o formulário renderiza, Então NÃO existe nenhum campo "Tipo de anestesia", "Início da anestesia", "Fim da anestesia" nem "Duração da anestesia"; o campo "Anestesista" (nome) está presente; e o objeto salvo NÃO contém as chaves `tipoAnestesia`/`tipoAnestesiaOutro`/`anestesiaInicio`/`anestesiaFim`.

- **CA7 (profilaxia "Outro" condicional):** Dado o checkbox "Outro" (antitrombótica) desmarcado, Quando o usuário tenta digitar no campo "Outro", Então o campo está desabilitado. Quando marca o checkbox, Então o campo habilita e o texto digitado é persistido em `profilaxia.antitrombOutro`. (Idem antibiótica.)

- **CA8 (intercorrência condicional):** Dado o toggle em "Sem intercorrências", Quando renderiza, Então não há campo de descrição. Quando muda para "Com", Então aparece a textarea e seu conteúdo é persistido em `intercorrenciaDescricao`.

- **CA9 (retrocompatibilidade — leitura de legado string):** Dado uma evolução antiga cujo `conteudo["evolucao-pos-op"]` é uma **string** de texto livre, Quando o usuário abre o `EvolucaoDetalheDrawer`, Então o texto da string é exibido legível (não some, não vira `[object Object]`, não vira JSON cru). Idem `desc-cirurgica`. (Coberto por teste unitário em `useEvolucaoResumo.test.ts` com fixture legado.)

- **CA10 (leitura estruturada legível — drawer + PDF):** Dado uma evolução nova cujo `conteudo["evolucao-pos-op"]` é **objeto**, Quando aberta no drawer e quando exportada em PDF, Então cada campo preenchido aparece como texto legível e rotulado (ex.: "Evolução: Boa", "Destino: Alta", "DPO: 5", "Dieta: Livre"), via formatador curado em `FORMATADORES_CURADOS`; campos vazios são omitidos; nenhum `true`/`false` cru, nenhuma chave técnica (`evolucaoPaciente`), nenhum JSON aparece. Idem `desc-cirurgica` (ex.: "Cirurgião: ...", "Duração: 02:30", "Profilaxia antitrombótica: Enoxaparina 40mg SC; Meia compressiva", "Intercorrências: ...").

- **CA11 (seção vazia omitida):** Dado uma evolução cujo objeto pós-op/desc-cirúrgica está totalmente vazio, Quando aberta no drawer/resumo/PDF, Então a seção é omitida (string vazia do formatador), consistente com `contarSecoesPreenchidas`.

- **CA12 (imutabilidade — sem edição):** Dado uma evolução já salva, Quando visualizada no drawer/timeline, Então não há formulário editável nem botão "Editar" para essas seções; a visualização é sempre readonly (R8). E nenhum endpoint de update/delete de evolução é introduzido.

- **CA13 (multi-tenant — herdado, regressão):** Dado um usuário do estabelecimento B, Quando tenta registrar/obter evolução de paciente do estabelecimento A, Então recebe resposta genérica de não-encontrado e nada com PII é logado — comportamento atual do `RegistrarEvolucaoCommandHandler`/query handler, que esta entrega NÃO altera. (CA de regressão: confirmar que nenhuma query/endpoint novo foi criado.)

- **CA14 (RBAC — herdado):** Dado um usuário sem permissão na área `prontuario` (ou sem papel Profissional/Dono), Quando chama o endpoint de registrar evolução, Então recebe 403 e o front não oferece o registro — comportamento atual (`[RequiresPapel(Profissional, Dono)]` + `[RequiresAcao("prontuario")]` em `ProntuarioController`), inalterado.

- **CA15 (LGPD — audit herdado):** Dado o registro de uma evolução com essas seções, Quando salva, Então é gravada 1 linha em `ProntuarioAcessoLog` com tipo `Escrita` (`{prontuarioId, usuarioId, estabelecimentoId}`); ao abrir o prontuário, tipo `Leitura`; ao exportar, `Exportacao` — comportamento atual, inalterado. Nenhuma PII desses objetos aparece em log/erro.

- **CA16 (LGPD — minimização):** Dado os DTOs de listagem/timeline da evolução, Quando carregados, Então nenhum campo novo dessas seções é exposto fora do conteúdo já existente (`conteudo`); a entrega não adiciona campo a DTO de listagem.

- **CA17 (performance / foco):** Dado a aba de prontuário, Quando o usuário não está editando essas seções, Então nenhuma request extra é disparada por elas (são puramente client-side dentro do form de evolução já carregado); o cálculo de DPO/duração/dia-da-semana é local e síncrono.

- **CA18 (tipografia — gate):** Dado os componentes novos de seção, Quando roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight` (CLAUDE.md §5).

- **CA19 (build + typecheck + suíte):** Dado a entrega, Quando roda lint/typecheck/build do frontend e a suíte Vitest, Então tudo passa, incluindo os testes novos de retrocompatibilidade e formatação curada.

---

## 8. Riscos e dependências

- **Risco — formatadores curados x shape:** se o nome de campo do objeto divergir do que o formatador curado espera, a leitura/PDF mostra campo errado/omisso. Mitigação: a seção 5 é a fonte canônica de nomes; o formatador e o componente devem usar exatamente esses nomes. CA10 cobre.
- **Risco — quebrar leitura de legado:** se o formatador curado tentar tratar string como objeto, evoluções antigas quebram. Mitigação: R11 + CA9 (guarda `typeof === "object" && !Array.isArray`, já é o padrão de `formatarSecaoLegivel`).
- **Risco — regressão multi-tenant/RBAC/audit:** improvável, pois é frontend + formatadores; mas QA deve confirmar que nenhum endpoint/query novo foi adicionado (CA13). Dependência: NÃO acionar `imedto-database` (sem schema).
- **Dependência conhecida (fora de escopo) — Ficha anestésica:** a decisão 3 manda que a anestesia "converse" com a Ficha anestésica. Hoje a SEÇÃO `ficha-anestesica` do prontuário é textarea livre (não estruturada) no refactor; o contrato `FichaAnestesica.cs` do backend pertence ao aggregate `ProcedimentoCirurgico`, NÃO à evolução. Resolução desta entrega (AR-1): a descrição cirúrgica apenas **deixa de duplicar** a anestesia (R7); estruturar a própria seção `ficha-anestesica` fica como demanda futura separada (backlog). Não há acoplamento técnico a criar agora — são seções irmãs no mesmo modelo de evolução; cada uma guarda seu objeto sob sua chave.
- **Dependência — toggle segmentado:** se o DS não tiver um toggle adequado (`AppPillToggle` existe mas ainda não consta em `Docs/DESIGN.md`), o dev decide reusar ou cair em `AppSelect`. Não bloqueante.

---

## 9. Observações para execução

**Não-negociável:**
- Manter o padrão dos 7 componentes de seção existentes (props/emit/`atualizar(patch)`/`:disabled="readOnly"`). NÃO inventar contrato novo de seção.
- Serialização via o fluxo já existente (`prontuarioService.registrarEvolucao` → `conteudoJson`). NÃO criar endpoint, command, DTO de leitura ou query novos. NÃO tocar backend de domínio (multi-tenant/RBAC/audit já cobrem).
- Zero migration / zero alteração de schema.
- Omitir deliberadamente os campos de anestesia clínica na descrição cirúrgica (R7/CA6).
- Tipografia só por token (CLAUDE.md §5); design system primeiro.
- Formatadores curados em `useEvolucaoResumo.ts` (`FORMATADORES_CURADOS["evolucao-pos-op"]` e `["desc-cirurgica"]`), seguindo o estilo dos curados existentes (linhas, rótulos legíveis, omissão de vazios, sem `true`/`false` cru). Garantir que string legada continue passando para `formatarGenerico`.

**Liberdade técnica do dev:**
- Forma do campo de hora (input nativo encapsulado vs. novo `AppTimeInput` no DS) — desde que respeite `HH:MM`/24h e R4, e documente se criar componente novo.
- Reuso de `AppPillToggle` vs. `AppSelect`/grupo de `AppButton` para os toggles segmentados.
- Estrutura interna dos componentes (cards/painéis) — espelhar o legado é desejável, mas a fidelidade pixel-perfect não é requisito; o requisito é o objeto de dados e os CAs.

**Referências de código (ponto de partida já validado):**
- Catálogo das seções: `frontend/src/components/ui/modeloProntuarioBuilder.ts:23-24` (chaves já existem; não precisa mudar `tipo` — o dispatcher decide pelo `chave`, como faz com `conduta`).
- Dispatcher: `frontend/src/components/prontuario/SecaoProntuario.vue` (adicionar 2 `v-else-if`, espelhando os existentes).
- Padrão de componente estruturado: `frontend/src/components/prontuario/secoes/SecaoExamesRealizados.vue` e `SecaoProcedimentosIndicados.vue`.
- Formatadores/leitura: `frontend/src/composables/useEvolucaoResumo.ts` (`FORMATADORES_CURADOS`, `formatarSecaoLegivel`), consumidos por `EvolucaoDetalheDrawer.vue` e `useProntuarioPdf.ts`.
- Referência legado (campos/labels/cálculos): `ReferenciaLegado/Imedto/src/modules/medical-record/components/EvolucaoPosOperatoriaSection.vue` e `DescricaoCirurgicaSection.vue`.

**Pipeline:** sem `imedto-database` (zero schema). Fluxo: `imedto-developer` → `imedto-qa`.

---

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar à seção de componentes de seção de prontuário os dois novos componentes estruturados (`SecaoEvolucaoPosOperatoria`, `SecaoDescricaoCirurgica`), seguindo o registro dos 7 existentes. SE o dev criar um componente de hora novo no design system (`AppTimeInput`), registrá-lo também na seção de componentes de formulário do DS, com variantes/estados. Responsável: `imedto-developer` na mesma entrega; validado pelo `imedto-qa`.
- **`Docs/ARQUITETURA.md`** — sem mudança (não há padrão novo de service/store/bus; segue o padrão de seção estruturada já documentado).
- **`Docs/LGPD.md`** — sem mudança (audit/multi-tenant/minimização herdados, sem novo tipo de PII exposta nem novo endpoint).
- **`Docs/COMANDOS.md` / `Docs/INFRA.md`** — sem mudança.

---

## Ambiguidades resolvidas (registro de decisão)

- **AR-1 — Como a descrição cirúrgica "conversa" com a Ficha anestésica?** A seção `ficha-anestesica` é textarea livre no refactor (não estruturada); o contrato `FichaAnestesica.cs` do backend pertence ao aggregate `ProcedimentoCirurgico`, não à evolução. **Decisão**: nesta entrega a descrição cirúrgica apenas DEIXA DE DUPLICAR a anestesia (remove tipo/horários/duração de anestesia — R7). Não há acoplamento técnico a criar: são seções irmãs do mesmo modelo de evolução, cada uma com seu objeto sob sua chave. Estruturar a própria `ficha-anestesica` fica como demanda futura (backlog), fora deste escopo.
- **AR-2 — Campo de hora no design system.** Não existe componente de hora no DS. Comportamento de produto está cravado (HH:MM/24h, duração calculada); a forma é liberdade técnica do dev, com obrigação de documentar se criar componente novo.

## Questões em aberto para o usuário (não bloqueantes — padrão sensato adotado; confirme se discorda)

1. **"Cirurgião" obrigatório:** o legado marca com `*`. Adotado: obrigatório no UX (validação visual), mas NÃO bloqueia o salvar da evolução (o backend grava JSONB cru e a evolução pode ter outras seções). Se você quiser bloqueio duro de salvar sem cirurgião, isso vira regra de front adicional — avise.
2. **Toggle segmentado:** adotado reuso de `AppPillToggle`/`AppSelect` conforme o dev avaliar. Se você quiser fidelidade visual ao toggle colorido do legado (verde/amarelo/vermelho por estado), confirme — pode exigir variante nova no DS.
