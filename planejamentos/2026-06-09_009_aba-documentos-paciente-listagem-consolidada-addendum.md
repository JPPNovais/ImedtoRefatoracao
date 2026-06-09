# ADDENDUM — Aba "Documentos" do paciente: confirmação de colunas + busca textual

**Refere-se a**: 2026-06-09_009_aba-documentos-paciente-listagem-consolidada.md
**ID**: 2026-06-09_009-addendum
**Status**: Aprovado por usuário em 2026-06-09 (decisões validadas pelo orquestrador via AskUserQuestion)
**Autor**: imedto-business-analyst
**Estimativa de esforço adicional**: P (estende endpoint e UI já especificados; não muda escopo read-only)
**Áreas regressivas tocadas**: prontuário (leitura), relatório (PDF) — inalteradas pelo addendum. Adiciona possível índice textual (sinalizado ao `imedto-database`, não cravado).

---

## A. Motivo do addendum

O orquestrador fechou, via `AskUserQuestion`, dois pontos que estavam como premissa/aberto no original (§11):

1. **Colunas da lista — CONFIRMADAS** exatamente como o default proposto na §11.2 do original: badge de tipo + título descritivo + data + profissional + ações (Visualizar/Baixar), **SEM badge de status**. Nada muda no original; este addendum apenas registra a validação explícita do default — não é mais "premissa do BA", é decisão aprovada do usuário.

2. **Busca por texto — ADICIONADA** (diverge do default §11.1 do original, que previa filtros só por tipo + período). A barra de filtros passa a ter **tipo + período + campo de busca por texto**. O item "Busca textual livre por conteúdo do documento" que o original listava em §3 "Não inclui" deixa de estar fora de escopo **apenas para os campos definidos na §C abaixo** — a busca NÃO é full-text irrestrita sobre todo o conteúdo clínico; é uma busca-resumo dirigida a campos específicos por tipo. Backlog de busca por CID/diagnóstico continua fora.

> **Imutabilidade**: o briefing original 2026-06-09_009 **não é editado**. Onde este addendum conflita com o original (especificamente §3 "Não inclui" → busca textual, e §11.1 → filtros), **prevalece o addendum**.

---

## B. Escopo do addendum

**Adiciona**:
- Parâmetro de query `busca` (string, opcional) ao endpoint agregado existente `GET /api/pacientes/{pacienteId}/documentos`.
- Campo de busca por texto na barra de filtros da aba "Documentos", com debounce.
- Aplicação da busca em cada sub-consulta do UNION, por campos definidos por tipo (§C), antes do UNION — preservando a paginação server-side correta sobre o conjunto unificado.

**Mantém inalterado** (do original):
- Read-only total. A busca não habilita nenhuma ação de escrita.
- Multi-tenant nas três sub-consultas (R2/CA10). A busca **também** roda dentro do filtro `estabelecimento_id` — não é um caminho paralelo.
- Filtro `status = 'Emitida'` para receitas (R1/CA3). A busca não traz rascunho/cancelada.
- Audit de leitura na listagem (R4/CA12). Buscar é listar: a carga com busca registra o mesmo 1 acesso de leitura por carga (não 1 por resultado).
- Minimização do DTO de resumo (R7/CA11): a busca **filtra** por campos clínicos, mas o DTO retornado **continua sem PII clínica** — não retorna o medicamento/exame/conteúdo que casou com o termo. O termo casado não é ecoado no item.
- Ordenação por data desc com paginação sobre o UNION (R3/CA6).

**Continua fora de escopo**:
- Busca por CID/diagnóstico ou full-text irrestrito sobre todo o conteúdo do documento.
- Highlight/realce do trecho casado no resultado (exigiria devolver o trecho → fere minimização).
- Filtro por profissional (permanece backlog, como no original §3).

---

## C. Regras de negócio do addendum

- **R8 — Campos de incidência da busca, por tipo**. O termo de `busca` é casado, por sub-consulta, contra os seguintes campos (busca **insensível a maiúsculas e a acentos** via `ILIKE` sobre `unaccent(...)`, padrão `unaccent(coluna) ILIKE unaccent('%' || @busca || '%')`):
  - **Receita** → nome do(s) medicamento(s) dos itens da receita (tabela de itens de receita; `EXISTS`/join contra os itens vinculados à receita). Se o schema guardar o medicamento como texto livre e/ou referência a catálogo, casar contra o **texto exibível do medicamento**. **`imedto-database` confirma o nome real da tabela/coluna de itens de receita contra o schema** (o original já cita `useReceitaPdf`/`receitaService.obter` como fonte do conteúdo completo).
  - **Atestado** → **tipo do atestado** + **conteúdo/texto do atestado** (o campo textual que descreve o atestado). Casar contra ambos com `OR`.
  - **Pedido de exame** → **nome(s) do(s) exame(s) solicitado(s)** (itens do pedido) + **indicação clínica** (se houver coluna de indicação). Casar com `OR`.
  - Mora em: **Query/Dapper** (cláusula condicional dentro de cada sub-select do UNION). Sem trava de front correspondente — o front só envia o termo; a fonte da verdade do match é o backend.

- **R9 — Busca aplicada ANTES do UNION, paginação DEPOIS**. O predicado de busca entra no `WHERE` de **cada sub-consulta** (receitas, atestados, pedidos), combinado por `AND` com os filtros já existentes (`estabelecimento_id`, `paciente_id`, `status='Emitida'` p/ receita, período). O `UNION ALL` é montado sobre as três sub-consultas **já filtradas**; o `ORDER BY data DESC` + `LIMIT/OFFSET` e o `COUNT(*)` total rodam **sobre o conjunto unificado e filtrado**. Não paginar cada tabela isoladamente. Mora em: **Query/Dapper**.

- **R10 — Busca combina com tipo e período (AND)**. `busca`, `tipo` e `dataInicio`/`dataFim` são filtros **cumulativos** (interseção). Ex.: `tipo=Receita&busca=dipirona&dataInicio=...` → só receitas emitidas, no período, cujos itens casam "dipirona". Quando `tipo` restringe a um único tipo, a busca só é avaliada naquela sub-consulta (as demais não entram no UNION). Mora em: **Handler + Query**.

- **R11 — Busca vazia/ausente é no-op**. `busca` ausente, `null`, string vazia ou só espaços → o predicado de busca **não é aplicado** (comporta-se como o original, sem busca). Normalizar `trim` no handler; não enviar `WHERE ... ILIKE '%%'` desnecessário. Mora em: **Handler** (decide aplicar ou não) + **Query** (predicado condicional).

- **R12 — Multi-tenant inalterado sob busca**. A busca nunca amplia o escopo: roda sempre dentro do `estabelecimento_id = @EstabelecimentoId` de cada sub-consulta (R2 do original permanece). Um termo que casaria documento de outro tenant **não** o traz. Mora em: **Query** (predicado de busca é `AND` ao filtro de tenant, nunca `OR`).

- **R13 — LGPD na busca**. (a) O termo `busca` **não vai para log** com PII — segue a regra de não logar conteúdo clínico; se houver log de request, o valor de `busca` é omitido/mascarado como qualquer parâmetro potencialmente sensível. (b) O DTO de resultado **não** ecoa o trecho/medicamento casado (R7 do original mantido). (c) Mensagem de erro genérica também no caminho com busca (R/CA13 do original). Mora em: **Handler/Query (não logar termo) + DTO (sem eco)**.

---

## D. UX e fluxo do addendum

**Barra de filtros** (estende a §6 do original):
- Adicionar um **campo de busca por texto** (input de texto com ícone de lupa, placeholder ex.: "Buscar por medicamento, exame ou atestado…"), reusando o componente de input de busca do design system se existir (`grep` em `components/ui/`); senão, `AppField` + input text com tokens tipográficos (CLAUDE.md §5 — sem `font-size`/`font-weight` literais).
- **Debounce ~300ms** via `useDebouncedRef` (reuso obrigatório — premissa de performance do CLAUDE.md). Digitar **não** dispara request a cada tecla.
- Digitar/limpar a busca **reseta para página 1** e refaz a busca server-side (mesmo comportamento que mudar tipo/período já tem no original §6).
- Botão/ícone de **limpar** o termo (zera e refaz como no-op de busca — R11).
- A busca **convive** com tipo e período na mesma barra; os três são cumulativos (R10).

**Estado vazio com busca** (estende §6 / §11.4 do original):
- Quando há termo de busca e/ou filtros ativos e zero resultados, reusar o empty state de filtro do original: **"Nenhum documento para o filtro selecionado."** (cobre busca + tipo + período). Não criar texto novo — o termo "filtro selecionado" abrange a busca. A contagem total da aba **não** zera por causa da busca (continua refletindo o total real do paciente, como no original §11.4/CA15).

**Loading**: a request de busca usa o mesmo estado de loading da listagem (§6 do original). Sem indicador novo.

---

## E. Modelo de dados / impacto de índice (nota ao `imedto-database`)

**Schema não muda** — a busca lê colunas/itens já existentes.

**Nota ao `imedto-database` (sinalização, NÃO cravar)**:
- A busca usa `unaccent(coluna) ILIKE unaccent('%termo%')` — padrão de **substring com curinga à esquerda**, que **não** aproveita índice B-tree comum. Em volume alto, isso pode degradar.
- **Avaliar** (não criar especulativamente): índice **GIN com `pg_trgm`** (`gin_trgm_ops`) sobre as colunas textuais de busca de cada tabela (texto do atestado, indicação do pedido) e sobre as colunas/itens de medicamento e exame. Requer extensão `pg_trgm` (e `unaccent` já em uso pelo padrão) — confirmar se já habilitadas.
- **Critério de decisão do DB**: medir/estimar volume típico de documentos por paciente. Se o conjunto por `(estabelecimento_id, paciente_id)` já é pequeno (a busca roda sobre o subconjunto do paciente, não a tabela inteira), o índice de tenant+paciente já existente (§5 do original) pode bastar e o `ILIKE` roda sobre poucas linhas — nesse caso **não criar índice trigram**. Criar GIN/trgm só se a inspeção indicar volume que justifique. Decisão e justificativa ficam com o `imedto-database`; o BA apenas sinaliza o risco.
- Migration idempotente apenas se o DB decidir pelo índice; caso contrário, nenhuma mudança de schema.

---

## F. DTO e contrato

- **DTO de resumo (`DocumentoResumoDto`) inalterado** — a busca não acrescenta campo ao item (R13.b). Continua `{ Tipo, Id, Titulo, Data, ProfissionalNome }`.
- **Query string do endpoint** ganha `busca` (opcional): `GET /api/pacientes/{id}/documentos?tipo=&dataInicio=&dataFim=&pagina=&tamanho=&busca=`.
- `PaginaDocumentosDto` inalterado.

---

## G. Critérios de aceite incrementais (testáveis)

Numerados a partir do último CA do original (CA17). Os CAs originais permanecem válidos.

- **CA18 (colunas confirmadas — sem badge de status)**: Dado a lista da aba Documentos renderizada, Quando inspecionada, Então cada linha exibe exatamente badge de tipo + título descritivo + data + profissional + ações (Visualizar/Baixar) e **não exibe badge de status** (confirma o default §11.2 do original como decisão aprovada).
- **CA19 (busca retorna o tipo certo — receita)**: Dado um paciente com uma receita emitida cujo item é "Dipirona 500mg" e um atestado sem essa palavra, Quando o usuário digita "dipirona" na busca, Então a requisição inclui `busca=dipirona`, a página volta para 1 e a lista mostra apenas a receita (o atestado não aparece).
- **CA20 (busca por tipo de cada tabela)**: Dado um atestado cujo conteúdo/tipo contém "afastamento" e um pedido de exame cuja indicação contém "hemograma", Quando o usuário busca "afastamento", Então só o atestado aparece; quando busca "hemograma", então só o pedido de exame aparece — cada termo casa contra os campos do tipo correto (R8).
- **CA21 (busca insensível a acento e caixa)**: Dado um documento cujo campo de busca contém "Glicemia em jejum", Quando o usuário busca "GLICEMIA" ou "glicemia", Então o documento aparece em ambos os casos (ILIKE + unaccent — R8).
- **CA22 (busca combina com filtro de tipo)**: Dado receitas e pedidos de exame que ambos casariam o termo "exame", Quando o usuário seleciona tipo "Receitas" E busca "exame", Então apenas receitas que casam aparecem (a busca só é avaliada na sub-consulta de receitas — R10), e os pedidos de exame não entram no resultado.
- **CA23 (busca combina com período)**: Dado documentos que casam o termo em datas dentro e fora do intervalo, Quando o usuário busca o termo E informa `De`/`Até`, Então só os documentos que casam o termo E estão no intervalo aparecem (interseção cumulativa — R10).
- **CA24 (busca vazia é no-op)**: Dado a lista carregada, Quando o usuário digita e depois limpa a busca (ou digita só espaços), Então o predicado de busca não é aplicado e a lista volta a trazer todos os documentos (respeitando tipo/período ativos) — `busca` em branco não filtra nada (R11).
- **CA25 (debounce)**: Dado o campo de busca, Quando o usuário digita "dipirona" caractere a caractere, Então NÃO é disparada uma requisição por tecla; após ~300ms sem digitar (`useDebouncedRef`), exatamente uma requisição é disparada com `busca=dipirona`.
- **CA26 (busca respeita só-emitidos)**: Dado um paciente com uma receita `Rascunho` e uma `Emitida` cujos itens ambos casam "dipirona", Quando o usuário busca "dipirona", Então apenas a receita `Emitida` aparece — a busca não rebaixa o filtro `status='Emitida'` (R1 do original + R9).
- **CA27 (busca respeita multi-tenant)**: Dado um documento do estabelecimento A que casaria o termo e um usuário autenticado no estabelecimento B no mesmo paciente cross-tenant, Quando o usuário B busca o termo, Então recebe "Paciente não encontrado." (ou resultado vazio do seu tenant) e nenhum documento do A aparece — a busca é `AND` ao filtro de tenant, nunca o amplia (R12/CA10 do original).
- **CA28 (paginação server-side com busca)**: Dado um paciente cujo termo de busca casa 45 documentos emitidos, Quando a lista carrega com `busca=` aplicado, Então a 1ª requisição traz `tamanho=20`, `total=45` (total dos que casam, não do paciente inteiro), e a página 2 traz os próximos 20 ordenados por data desc, sem duplicar nem pular registro na borda (R9).
- **CA29 (LGPD — termo não ecoado nem logado)**: Dado uma busca por "dipirona" que retorna uma receita, Quando a resposta é inspecionada, Então o item retornado contém apenas `{ tipo, id, titulo, data, profissionalNome }` — não há trecho casado nem nome do medicamento no payload; e o termo de busca não aparece em log com PII (R13).
- **CA30 (audit não infla com busca)**: Dado um paciente com prontuário, Quando a aba carrega uma busca que retorna 30 resultados, Então é registrada exatamente **uma** linha de acesso de leitura ao prontuário para aquela carga (não uma por resultado) — busca é listar (R4/CA12 do original mantido).

---

## H. Atualização de documentação (addendum)

- **`Docs/ARQUITETURA.md`** — a nota sobre o padrão de query agregada multi-tabela (já prevista na §10 do original) deve mencionar, na mesma nota cirúrgica, que o endpoint suporta **busca textual por sub-consulta antes do UNION** (predicado `ILIKE`/`unaccent` aplicado em cada aggregate, paginação sobre o conjunto unificado). Acréscimo de uma frase à nota já planejada — não cria seção nova.
- **`Docs/LGPD.md`** — sem alteração estrutural: a busca segue minimização (sem eco do trecho) e não-log de termo sensível, regras já documentadas. Não atualizar.
- **`Docs/DESIGN.md`** — atualizar **somente se** nascer componente novo de busca reutilizável. Se reusar input de busca + `useDebouncedRef` existentes, não atualizar. Decisão do dev; o QA valida.
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — atualizar **somente se** o `imedto-database` decidir habilitar extensão `pg_trgm` e/ou criar índice GIN (nova extensão = mudança de infra de banco a registrar). Se a decisão for não criar índice, não atualizar.

---

## I. Riscos e dependências do addendum

- **Itens de medicamento/exame são tabelas-filhas**: a busca de receita/pedido casa contra itens (1:N), não contra colunas da tabela-pai. O `EXISTS (SELECT 1 FROM itens WHERE itens.fk = pai.id AND unaccent(...) ILIKE ...)` precisa ser validado contra o schema real pelo `imedto-database` (nomes de tabela/coluna/FK). Risco de erro de coluna no Dapper se assumido às cegas.
- **`ILIKE '%termo%'` com curinga à esquerda + UNION**: custo controlado pelo escopo `(estabelecimento_id, paciente_id)` — a busca roda sobre os documentos de **um** paciente, não a tabela inteira. Mesmo assim, validar borda de paginação (CA28) e medir antes de assumir custo baixo (§E).
- **Não regredir os CAs do original**: a adição da busca não pode alterar o comportamento quando `busca` está ausente — CA1–CA17 do original devem continuar passando idênticos (R11).
