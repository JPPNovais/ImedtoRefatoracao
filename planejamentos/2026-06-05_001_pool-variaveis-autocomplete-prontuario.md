# Pool de variáveis ↔ campos do prontuário: autocomplete + criação automática ao salvar evolução

**ID**: 2026-06-05_001
**Status**: Aprovado por usuário (decisões fechadas com o dono do produto antes do briefing)
**Documento imutável** — mudanças entram via addendum (`2026-06-05_001_*-addendum.md`), nunca editando este arquivo.
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: prontuário, permissionamento, multi-tenant, admin (catálogos globais), PDFs (indireto — campos `nome` continuam string), enum `TipoVariavelPool`

---

## 1. Contexto e motivação

Hoje o pool de variáveis (`ProntuarioVariavelPool`, tabela `prontuario_variaveis_pool`) existe com CRUD completo — escopo duplo (padrão-sistema com `estabelecimento_id NULL` + por-estabelecimento), tela de configuração (`ListasVariaveisTab.vue`, só Dono) e ferramenta de admin global (`VariaveisGlobaisListView/FormView.vue`). **Mas o pool não está conectado a nada**: os campos de lista do prontuário (alergias, medicações, cirurgias, doenças, parentes, etc.) são `AppInput` de texto puro, sem sugestão. O pool é uma lista que ninguém consome.

No legado, esses campos eram autocomplete alimentados pela lista de variáveis: o profissional digitava, via sugestões já cadastradas e, ao digitar um valor inédito, ele virava item novo no pool **do estabelecimento**. É exatamente esse comportamento que falta.

**Gap central**: (a) os campos do prontuário não puxam o pool por autocomplete; (b) ao salvar a evolução, valores inéditos não viram itens do pool; (c) o enum tem 2 tipos sem campo correspondente no prontuário (`Droga`, `AtividadeFisica`) que poluem a config e o admin.

## 2. Persona-alvo

- **Profissional** que atende e registra evolução (todos os atendentes do estabelecimento) — consome o autocomplete e gera itens novos ao salvar, **sem precisar de permissão de configuração**.
- **Dono / Admin do estabelecimento** — gerencia manualmente a lista via `ListasVariaveisTab` (mantém exigência de `ModelosProntuario`).
- **Admin global Imedto** — popula os padrões-sistema via `VariaveisGlobais*` no módulo admin.

Frequência: o autocomplete é tocado em todo atendimento que preenche HPP / história familiar / procedimentos.

## 3. Escopo

**Inclui (6 tipos vinculados a campo do prontuário):**
- Autocomplete (sugestões padrão-sistema + estabelecimento, por tipo, ativos) nos campos `nome`/equivalente de: Alergia, Medicamento, Cirurgia, Doença, RelacaoFamiliar, Expectativa.
- Criação automática no pool **do estabelecimento** ao salvar a evolução, para valores inéditos dos campos mapeados.
- Dedup robusta (trim + case-insensitive + insensível a acento), reusando padrão-sistema quando já existir.
- Isolamento multi-tenant dos itens não-padrão.
- Permissão diferenciada: criação automática via evolução para qualquer profissional do estabelecimento; CRUD manual segue exigindo `ModelosProntuario`.
- Componente de autocomplete reutilizável para os campos de lista.
- Seção HISTÓRIA FAMILIAR: campo "grau de parentesco" passa a aceitar autocomplete + criação (tipo RelacaoFamiliar). Label pode ser ajustado para "Relação familiar".
- Seção PROCEDIMENTOS: criação do vínculo para Expectativa — ver §6 (decisão de UI) e §8 (risco).

**Remove (2 tipos sem campo no prontuário):**
- `Droga` e `AtividadeFisica`: do enum `TipoVariavelPool`, das validações de mensagem, dos mapas de tipos do front (`TIPOS_VARIAVEL_POOL`, `TipoVariavelPool` em `variavelPoolService.ts`, lista `TIPOS` em `ListasVariaveisTab.vue`), e dos registros existentes no banco via migration de remoção.

**Não inclui:**
- NÃO alterar campos de texto livre da HISTÓRIA SOCIAL (`atividadeFisicaNivel/Obs`, drogas) — são texto livre da anamnese social, NÃO listas do pool. A remoção de `Droga`/`AtividadeFisica` é só do enum/pool/admin, não desses campos.
- NÃO transformar os campos livres (`observacao`, `dose`, `frequencia`, `motivo`, `ano`, `observacoes`, `comentario`, `doencas` dos parentes) em itens do pool.
- NÃO migrar o histórico de evoluções já gravadas para popular o pool retroativamente (o pool cresce a partir de novas evoluções).
- NÃO alterar o shape de armazenamento do `ConteudoJson` (continua JSONB livre; campos continuam guardando string `nome`).

## 4. Mapeamento tipo → seção → campo

| Tipo do pool | Seção do prontuário | Subseção / item | Campo que casa com o pool | Campos livres (NUNCA viram pool) |
|---|---|---|---|---|
| Alergia | HPP (`hpp`) | `alergias[]` | `nome` | `observacao` |
| Medicamento | HPP (`hpp`) | `medicacoes[]` | `nome` | `dose`, `frequencia`, `motivo`, `observacoes` |
| Cirurgia | HPP (`hpp`) | `cirurgias[]` | `nome` | `ano`, `observacao` |
| Doenca | HPP (`hpp`) | `doencas[]` | `nome` | `observacao` |
| RelacaoFamiliar | História familiar (`h-familiar`) | `parentes[]` | `parentesco` | `doencas`, `comentario` |
| Expectativa | Procedimentos (`procedimentos-indicados`) | item de expectativa (ver §6) | campo `nome` da expectativa | observação livre |

> Observação técnica para o dev: hoje `SecaoProcedimentosIndicados.vue` tem `procedimentos[].descricao` — esse é o procedimento INDICADO (cirurgia/intervenção recomendada), **não** Expectativa. Expectativa é informação distinta ("o que o paciente espera do tratamento"). Ver §6 para a decisão de onde colocar.

## 5. Modelo de dados

- **Tabela `prontuario_variaveis_pool`** (sem mudança estrutural): `id, estabelecimento_id, tipo, nome, ativo, eh_padrao_sistema, criado_em, atualizado_em`. Coluna `tipo` é string (`HasConversion<string>`, max 20) — remover valores do enum **não exige** alteração de coluna/constraint.
- **Migration de remoção** (idempotente, autora: `imedto-database`): `DELETE FROM public.prontuario_variaveis_pool WHERE tipo IN ('Droga','AtividadeFisica');`. Como é string, não há risco de quebra de mapeamento em linhas remanescentes. Após o DELETE, nenhuma linha com esses tipos sobra. (Decisão: hard delete; são dados de catálogo, não PII de paciente, e o dono confirmou remoção completa.)
- **Índices existentes** cobrem o autocomplete: `ix_pool_estabelecimento_tipo` e `ix_pool_padrao_tipo`. A query de listagem (`VariavelPoolQueryRepository.Listar`) filtra `(eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId) AND tipo = @Tipo AND ativo = true ORDER BY eh_padrao_sistema DESC, nome`. Reusar essa query no autocomplete — sem nova query.
- **Audit/LGPD**: o pool guarda só `nome` genérico (ex.: "Penicilina"); não é PII de paciente. A criação automática ocorre dentro do fluxo de `RegistrarEvolucao`, que **já** registra `IProntuarioAcessoLogService` (acesso de escrita). Não criar audit novo para a criação de item de pool — é dado de catálogo. A evolução em si continua auditada como hoje.
- **Multi-tenant**: itens não-padrão são exclusivos por `estabelecimento_id`. Criação automática usa `command.EstabelecimentoId` do `RegistrarEvolucaoCommand` (já é o tenant da sessão).

## 6. UX e fluxo

### Componente de autocomplete (design system — NOVO)
Os campos do prontuário guardam **string `nome`** (não id do pool). `AppSelectComCriacao.vue` existente NÃO serve — é um `<select>` keyed por id, com criação via modal. Precisamos de um typeahead de texto:

- **`AppAutocompleteCriavel`** (nome sugerido; o dev confirma convenção do design system): input de texto + dropdown de sugestões filtrado conforme o usuário digita. `v-model` é **string** (o `nome`). Props: `modelValue: string`, `opcoes: string[]` (lista de nomes do tipo), `placeholder`, `disabled`.
- Comportamento: ao focar/digitar, mostra sugestões (padrão-sistema + estabelecimento, ativos, do tipo) que casam com o texto digitado (filtro normalizado: trim + case + acento). O usuário pode **escolher** uma sugestão OU **digitar um valor inédito e seguir** — valor inédito é aceito livremente no campo (não bloqueia). A criação no pool só acontece no backend ao salvar a evolução.
- **Estados**: carregando sugestões (skeleton/placeholder discreto), lista vazia ("Nenhuma opção cadastrada — digite para criar uma nova"), erro de carregamento (degrada para input de texto puro, sem travar o preenchimento da evolução).
- **Performance**: as listas por tipo são carregadas **uma vez** ao abrir a seção/prontuário (uma chamada por tipo necessário OU uma chamada batch sem filtro de tipo, a critério do dev — preferir reuso de `variavelPoolService.listar`). Filtro de sugestão é client-side sobre a lista já carregada (sem request por tecla). Se o dev optar por busca server-side, aplicar `useDebouncedRef` ~300ms.
- **Mobile-ready**: input full-width, dropdown abaixo, mesma estética dos campos atuais.

### Seções afetadas no front
- **HPP** (`SecaoHistoriaPregressa.vue`): trocar os 4 `AppInput` de `nome` (alergias, medicacoes, cirurgias, doencas) pelo componente de autocomplete, cada um com o tipo correspondente. Campos livres permanecem `AppInput`/`AppSelect` como hoje.
- **História familiar** (`SecaoHistoriaFamiliar.vue`): o campo `parentes[].parentesco` é hoje `AppSelect` com lista fixa `PARENTESCOS`. Passa a usar o autocomplete (tipo RelacaoFamiliar). Os valores fixos atuais ("Irmão(ã)", "Avó materna"...) devem virar **padrões-sistema** semeados pelo admin (item de §9 / handoff ao admin), para não perder as opções existentes. Label pode mudar de "Grau de parentesco" para "Relação familiar". Campos `doencas`/`comentario` permanecem livres.
- **Procedimentos / Expectativa** (`SecaoProcedimentosIndicados.vue`): **DECISÃO DE PRODUTO** — Expectativa é um conceito distinto de "procedimento indicado". Adicionar à seção de Procedimentos uma **subseção "Expectativas do paciente"** com lista de itens `{ nome, observacao }`, onde `nome` usa o autocomplete (tipo Expectativa) e `observacao` é livre. NÃO reaproveitar `procedimentos[].descricao` para Expectativa (são coisas diferentes; `descricao` continua texto livre do procedimento indicado). O builder de modelo (`modeloProntuarioBuilder.ts`) não precisa de nova seção — Expectativa entra como subseção dentro da seção `procedimentos-indicados` já existente, persistida no mesmo `ConteudoJson` sob uma chave nova (ex.: `expectativas[]`). O dev define a chave; o handler de extração (§ R3) precisa conhecê-la.

### Fluxo de salvar
Sem mudança de UX no botão "Salvar consulta": ao salvar a evolução, o backend extrai e cria os itens novos transacionalmente. O profissional não vê passo extra; na próxima abertura do prontuário/seção, os itens recém-criados já aparecem nas sugestões.

## 7. Critérios de aceite (testáveis)

- **CA1 (autocomplete puxa padrão-sistema + estabelecimento por tipo)**: Dado um estabelecimento com itens padrão-sistema "Penicilina" (Alergia) e item de estabelecimento "Látex" (Alergia), Quando o profissional foca o campo `nome` de uma alergia na HPP, Então o dropdown lista "Penicilina" e "Látex" (padrão-sistema primeiro), ambos do tipo Alergia e ativos, e NÃO lista itens de outros tipos.

- **CA2 (criação automática ao salvar evolução)**: Dado que o profissional digita "Sulfa" (valor inédito) no campo `nome` de uma alergia e salva a evolução, Quando o `RegistrarEvolucaoCommandHandler` processa o `ConteudoJson`, Então é criado no pool um `ProntuarioVariavelPool` com `tipo=Alergia`, `nome="Sulfa"`, `estabelecimento_id` = tenant da sessão, `eh_padrao_sistema=false`, `ativo=true`; e numa nova evolução o campo de alergia já sugere "Sulfa".

- **CA3 (dedup robusta reusa, não duplica — case/acento/trim)**: Dado que já existe item "Hipertensão" (Doenca), Quando o profissional salva uma evolução com doença `" hipertensao "` (com espaços, minúsculo, sem acento), Então nenhum item novo é criado (a normalização trim+case+acento reconhece como igual).

- **CA4 (dedup reusa padrão-sistema sem copiar para o estabelecimento)**: Dado um item padrão-sistema "Apendicectomia" (Cirurgia, `estabelecimento_id NULL`), Quando o profissional salva evolução com cirurgia `nome="apendicectomia"`, Então NÃO é criado item no estabelecimento (reusa o padrão-sistema); a contagem de itens do estabelecimento para Cirurgia permanece inalterada.

- **CA5 (isolamento multi-tenant)**: Dado que o estabelecimento A criou (via evolução) o item "Látex" (Alergia, não-padrão), Quando um profissional do estabelecimento B abre o autocomplete de alergia, Então "Látex" NÃO aparece para B; B vê apenas padrões-sistema + os próprios itens. Nenhuma resposta vaza `estabelecimento_id` alheio.

- **CA6 (permissão — criação via evolução sem ModelosProntuario)**: Dado um profissional do estabelecimento SEM a permissão `ModelosProntuario`, Quando ele salva uma evolução com valor inédito num campo mapeado, Então o item é criado no pool do estabelecimento com sucesso (a porta de criação automática NÃO exige `ModelosProntuario`).

- **CA7 (permissão — CRUD manual continua restrito)**: Dado o mesmo profissional sem `ModelosProntuario`, Quando ele tenta criar/editar/excluir item pela tela de configuração (`POST/PUT/DELETE /api/prontuario/pool`), Então recebe 403 e os botões de edição da `ListasVariaveisTab` ficam ocultos/desabilitados (espelho back+front mantido como hoje).

- **CA8 (LGPD — só `nome` vira item, nunca campos livres/PII)**: Dado que o profissional preenche uma medicação com `nome="Losartana"`, `dose="50mg"`, `motivo="Hipertensão do Sr. João"`, `observacoes="paciente relata tontura"`, Quando salva a evolução, Então é criado no pool apenas o item `Medicamento="Losartana"`; `dose`, `motivo` e `observacoes` NÃO geram itens de pool e nenhum valor de campo livre é persistido fora do `ConteudoJson` da evolução.

- **CA9 (campos vazios não geram lixo)**: Dado uma evolução salva com itens de lista contendo `nome` vazio ou só espaços, Quando o handler extrai os valores, Então nenhum item de pool é criado para esses `nome` vazios.

- **CA10 (estado vazio do autocomplete)**: Dado um tipo sem nenhum item (nem padrão-sistema nem do estabelecimento), Quando o profissional foca o campo correspondente, Então o dropdown mostra mensagem "Nenhuma opção cadastrada — digite para criar uma nova" e o profissional consegue digitar e salvar normalmente (criando o primeiro item ao salvar).

- **CA11 (degradação em erro de carregamento)**: Dado que a chamada de listagem do pool falha, Quando o profissional abre a seção, Então o campo degrada para input de texto puro (sem dropdown), o preenchimento e o salvamento da evolução continuam funcionando, e nenhuma mensagem com PII é exibida.

- **CA12 (remoção de Droga/AtividadeFisica — banco)**: Dado registros existentes com `tipo='Droga'` e `tipo='AtividadeFisica'`, Quando a migration de remoção roda, Então essas linhas são removidas e a migration é idempotente (rodar 2x não falha); nenhuma outra linha do pool é afetada.

- **CA13 (remoção de Droga/AtividadeFisica — enum/admin/config)**: Dado o enum `TipoVariavelPool` sem `Droga`/`AtividadeFisica`, Quando o usuário abre a `ListasVariaveisTab` e o admin abre `VariaveisGlobais*`, Então os dois tipos não aparecem em nenhum seletor; e Quando alguém tenta criar item via API com `tipo="Droga"`, Então recebe 422 `BusinessException` "Tipo inválido" (mensagem atualizada para listar os 6 tipos válidos: Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa).

- **CA14 (RelacaoFamiliar — autocomplete na história familiar)**: Dado que os parentescos fixos atuais foram semeados como padrão-sistema (tipo RelacaoFamiliar), Quando o profissional foca o campo de relação familiar de um parente, Então vê as opções fixas como sugestões; e Quando digita uma relação inédita e salva, Então ela vira item do pool do estabelecimento (tipo RelacaoFamiliar).

- **CA15 (Expectativa — subseção em Procedimentos)**: Dado a seção Procedimentos com a subseção "Expectativas do paciente", Quando o profissional adiciona uma expectativa com `nome` inédito e salva, Então o item vira pool tipo Expectativa do estabelecimento, e o campo `descricao` de `procedimentos[]` (procedimento indicado) NÃO é tratado como Expectativa (continua texto livre, não gera pool).

- **CA16 (transacionalidade)**: Dado que o salvamento da evolução falha após a extração, Quando ocorre rollback, Então nenhum item de pool é persistido órfão (extração e gravação da evolução são atômicas).

## 8. Riscos e dependências

- **Risco — Expectativa sem campo existente**: confirmado que NÃO há campo/subseção de Expectativa hoje. A decisão (§6) é criar subseção "Expectativas do paciente" dentro de `procedimentos-indicados`. Atenção do dev: não confundir com `cirurgias[]` da HPP (que é tipo Cirurgia) nem com `procedimentos[].descricao` (procedimento indicado, texto livre).
- **Risco — perda das opções fixas de parentesco**: a lista `PARENTESCOS` hardcoded na `SecaoHistoriaFamiliar.vue` precisa ser semeada como padrão-sistema (RelacaoFamiliar) **antes/junto** da troca para autocomplete, senão o profissional perde as sugestões. Dependência de handoff ao admin (§9).
- **Risco — normalização de acento no backend**: o dedup atual (`ExisteOutraComMesmoNome`) faz só `ToLower()` em LINQ→SQL. A normalização insensível a acento precisa ser decidida pelo `imedto-database`/dev: `unaccent` no Postgres (extensão) OU normalização em memória no handler antes de comparar. Como a comparação na criação automática carrega a lista do tipo (padrão+estabelecimento) e compara em memória, normalizar em C# (remover diacríticos + trim + lower) é viável sem extensão nova — preferir isso para não introduzir dependência de extensão. O dedup do CRUD manual (`AdicionarVariavelPoolCommandHandler`) também deve passar a usar a MESMA normalização (e considerar colisão contra padrão-sistema, que hoje ele ignora) para consistência — ver R5.
- **Risco — `ExisteOutraComMesmoNome` não compara contra padrão-sistema**: hoje a dedup do CRUD só olha o próprio estabelecimento. A criação automática (e idealmente o CRUD manual) precisa reusar padrão-sistema. Ajustar a checagem de existência para considerar `(estabelecimento_id = @tenant OR eh_padrao_sistema = true)`.
- **Dependência**: `RegistrarEvolucaoCommandHandler` é o ponto de extração. Hoje ele já tem `EstabelecimentoId` e processa `ConteudoJson` (string). A extração precisa parsear o JSON conhecendo as chaves das seções mapeadas. Como o `ConteudoJson` é livre, o handler deve falhar-suave: se a estrutura não bate (chave ausente), apenas não extrai daquele campo — nunca quebra o salvamento da evolução.
- **Regressão a vigiar**: PDFs de prontuário leem os mesmos campos `nome` (continuam string) — sem impacto esperado, mas validar que a troca de componente no front não muda o shape salvo no `ConteudoJson`.

## 9. Observações para execução

**Não-negociável:**
- Regra de extração/criação e dedup vivem no **backend** (Domain/Handler). Front é UX. Espelho 422 mantido.
- Multi-tenant: criação automática usa `command.EstabelecimentoId`; listagem do autocomplete reusa `VariavelPoolQueryRepository.Listar` (já filtra padrão OR tenant).
- LGPD: só `nome` vira item. Campos livres jamais.
- Os 6 tipos válidos finais: Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa.

**Liberdade técnica do dev:**
- Onde colocar a extração: dentro do `RegistrarEvolucaoCommandHandler` (passo dedicado) ou serviço de domínio chamado por ele — desde que transacional com o salvamento da evolução (CA16).
- Carregar listas do autocomplete por tipo (N chamadas) ou batch — desde que sem request por tecla (filtro client-side) e reusando `variavelPoolService`.
- Nome/forma exata do componente de autocomplete (seguir convenção do design system).
- Chave JSON da subseção Expectativa (ex.: `expectativas[]`) — alinhar com o handler de extração.

**Reuso obrigatório:**
- Query: `VariavelPoolQueryRepository.Listar` (não criar query nova de listagem).
- Service front: `variavelPoolService.listar(tipo)`.
- NÃO reusar `AppSelectComCriacao.vue` (é select por id, não typeahead por string) — criar componente novo.

**Handoff ao admin (fora desta entrega, mas dependência):** semear como padrão-sistema os parentescos fixos atuais (RelacaoFamiliar) e quaisquer alergias/doenças/medicamentos/cirurgias/expectativas globais que o dono queira. O dono fará isso via `VariaveisGlobais*`. O dev deve garantir que, sem seed, o sistema funciona (estado vazio + criação por estabelecimento — CA10).

**Aciona `imedto-database`** para: (1) migration idempotente de remoção de `Droga`/`AtividadeFisica`; (2) avaliar se a normalização de acento será 100% em C# (preferência, sem extensão) ou exigirá `unaccent`. Se em C#, não há migration de schema além da remoção.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — documentar o vínculo pool ↔ prontuário: (a) que a criação de itens do pool acontece no fluxo `RegistrarEvolucao` (criação automática) além do CRUD manual; (b) regra de dedup canônica (trim + case-insensitive + insensível a acento, reusando padrão-sistema sem copiar para o estabelecimento); (c) que itens não-padrão são exclusivos por `estabelecimento_id`. Mudança cirúrgica na seção de Prontuários — não reescrever o doc.
- **`Docs/DESIGN.md`** — adicionar o componente novo de autocomplete (typeahead com criação livre de string) à seção de componentes do design system, deixando claro a diferença para `AppSelectComCriacao` (este é select por id; o novo é typeahead por string).
- **`Docs/LGPD.md`** — registrar que o pool de variáveis guarda apenas `nome` genérico (não-PII) e que a extração ao salvar evolução NUNCA promove campos livres (`observacao`, `dose`, `motivo`, etc.) a item de pool. Acrescentar à seção de minimização do prontuário.
- **`Docs/COMANDOS.md`** — atualizar apenas se a migration de remoção introduzir fluxo novo; caso contrário, "nenhum".
- **`Docs/INFRA.md`** — só se a normalização exigir extensão `unaccent` no Postgres (decisão do `imedto-database`). Se a normalização ficar em C#, "nenhum".

> O `imedto-business-analyst` atualiza ARQUITETURA/DESIGN/LGPD no mesmo ciclo, antes do hand-off ao dev. INFRA/COMANDOS ficam condicionados à decisão técnica do `imedto-database` e são atualizados por ele se necessário. O `imedto-qa` valida nos CAs que os docs aplicáveis foram atualizados.
