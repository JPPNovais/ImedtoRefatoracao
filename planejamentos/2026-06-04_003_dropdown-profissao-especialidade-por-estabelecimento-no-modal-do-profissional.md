# Dropdown de profissão + especialidade por estabelecimento no modal de detalhes do profissional

**ID**: 2026-06-04_003
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: permissionamento (RBAC do vínculo), prontuário/agenda/seletores (leem profissão+especialidade do vínculo via COALESCE), relatório (PDFs com cabeçalho do profissional)

## 1. Contexto e motivação

Hoje, no modal de detalhes do profissional (`ProfissionalDetalhesModal.vue`), o Dono consegue editar a **especialidade** do vínculo via `AppInput` de **texto livre** (briefing 2026-06-03_004, RBAC fechado em CA7 / commit 837b8be). A **profissão** do vínculo nem é renderizada no modal — só aparece no fluxo de convite.

Dois problemas operacionais:

1. **Texto livre gera lixo de dados.** "Dermato", "dermatologia", "Dermatologista", "DERMA" são todos valores distintos hoje. Isso quebra filtros de agenda, relatórios e a consistência clínica. O convite (`ConvidarProfissionalModal.vue`) já resolveu isso com dropdown estrito de catálogo (profissão → especialidade) — o modal de detalhes ficou para trás e precisa de **paridade** com o convite.
2. **Profissão não é editável no vínculo.** Um profissional pode atuar como uma profissão num estabelecimento e outra em outro (multi-vínculo, regra canônica do produto). Hoje só dá para definir a profissão no momento do convite — depois disso, fica congelada. O Dono precisa poder corrigir/trocar a profissão do vínculo já existente.

Decisão de produto fechada com o dono em 2026-06-04 (5 perguntas): profissão vira **editável por dropdown por estabelecimento**; especialidade vira **dropdown estrito de catálogo** (mesmo padrão do convite); trocar a profissão **limpa** a especialidade; **uma** especialidade por vínculo (mantém o modelo de string única, sem tabela nova); dado legado de texto livre passa por **migração ativa** (casa com catálogo → mantém; não casa → vira nulo e cai no COALESCE para o global).

## 2. Persona-alvo

**Dono da clínica** (papel "Dono", validado por `permissoes.ehDono` do usuário logado — não confundir com `status === "Dono"` do profissional listado, ver R4). Usa o modal de detalhes da equipe ao corrigir/ajustar o vínculo de um profissional já cadastrado no seu estabelecimento. Frequência baixa, alto impacto (afeta como o profissional aparece em agenda, prontuário e PDFs). Edição é **por estabelecimento corrente** — nunca global, nunca afeta outros vínculos do mesmo profissional.

## 3. Escopo

**Inclui**:
- Renderizar e tornar **editável** a **profissão** do vínculo no modal de detalhes, via dropdown (`AppSelect`) populado por `catalogoService.listarProfissoes()`.
- Substituir o `AppInput` de texto livre da **especialidade** por dropdown estrito (`AppSelect`) populado por `catalogoService.listarEspecialidades(profissaoId)`, **dependente da profissão selecionada** — mesma mecânica e mesmo serviço já usados no `ConvidarProfissionalModal.vue` (reuso, não duplicação).
- Comportamento: trocar a profissão **recarrega** as especialidades e **limpa** a especialidade selecionada.
- Persistência **atômica** profissão+especialidade no vínculo via **um único comando** (ver R2 e seção 9), substituindo o atual `AlterarEspecialidadeDoVinculoCommand` que só carrega especialidade.
- Expor `profissaoConvidadaId` (e manter `Profissao` nome) no `ProfissionalVinculadoDto` para o dropdown vir pré-selecionado e filtrar especialidades.
- Atualizar o **conselho exibido** (CRM/CRO/etc.) para acompanhar a profissão selecionada quando o catálogo de profissão define `conselhoSigla` (ver R7).
- **Migração ativa de dado legado** de `especialidade_convidada` (texto livre) → valor de catálogo ou nulo — **executada pelo `imedto-database` via migration idempotente** (ver seção 5 e R6).

**Não inclui**:
- Mais de uma especialidade por vínculo (mantido modelo de string única; multi-especialidade fica como backlog separado).
- Texto livre / "outra especialidade não listada" — explicitamente fora; é só catálogo.
- Edição da profissão/especialidade do **cadastro global** do profissional (continua fora do escopo do modal — só o vínculo do estabelecimento corrente).
- Edição por papéis que não sejam Dono (RBAC inalterado em relação ao CA7 de 2026-06-03_004).
- Histórico/audit trail por profissional (já adiado no briefing original — backend não expõe endpoint).

## 4. Regras de negócio

- **R1 — Especialidade dependente da profissão (catálogo estrito)**: a especialidade só pode ser um item ativo do catálogo da profissão selecionada. Texto livre é proibido. Mora em: Domain/Handler (validação `ExisteEspecialidadeAtivaPorNome(profissaoId, nome)` — já existe e é usada pelo `ConvidarProfissionalCommandHandler`) + Front (dropdown só lista itens do catálogo). Validada em: **back + front** (422 `BusinessException` "Especialidade não pertence à profissão selecionada ou está inativa." é a fonte da verdade; front é UX).

- **R2 — Persistência atômica profissão+especialidade no vínculo**: a alteração de profissão e especialidade do vínculo é **um único comando** que recebe `profissaoId` (obrigatório quando há especialidade) + `especialidade` (nome do catálogo, nula = limpa). Como trocar a profissão **sempre** limpa a especialidade, separar em dois endpoints abriria janela de estado inconsistente (profissão nova + especialidade da profissão antiga). Mora em: Handler (estende/substitui `AlterarEspecialidadeDoVinculoCommand` → renomear para `AlterarProfissaoEspecialidadeDoVinculoCommand`, carregando `ProfissaoId` + `Especialidade`). Validada em: **back + front**.

- **R3 — Escopo por estabelecimento (multi-tenant + por vínculo)**: a edição grava **apenas** em `especialidade_convidada` / `profissao_convidada_id` do **vínculo do estabelecimento corrente** — nunca no cadastro global do profissional, nunca em outro vínculo do mesmo usuário. O comando filtra por `EstabelecimentoId` (claim) e por `VinculoId`; repositório falha-fechada. Mora em: Handler + Query (`COALESCE(v.especialidade_convidada, p.especialidade)` já é a regra canônica de leitura — ver memória [[especialidade-por-vinculo]] e briefing 2026-06-03_004). Validada em: **back** (filtro de tenant) **+ front** (modal só opera sobre o vínculo aberto).

- **R4 — RBAC: só o Dono edita**: profissão e especialidade do vínculo só podem ser alteradas pelo usuário logado cujo papel é **Dono** (`permissoes.ehDono`). Reaproveita exatamente a condição `podeEditarEspecialidade = vinculoId != null && permissoes.ehDono` já existente (CA7 de 2026-06-03_004). **Atenção (armadilha conhecida)**: `ehDono` local do componente reflete o `status` do **profissional listado**, NÃO o papel do logado — RBAC usa **somente** `permissoes.ehDono`. Mora em: Handler (`UsuarioSolicitanteId` deve ser Dono do estabelecimento; 403 caso contrário) + Front (campos só renderizam editáveis quando `podeEditarEspecialidade`). Validada em: **back + front**.

- **R5 — Vínculo formal obrigatório**: edição só é possível para vínculo com `vinculoId != null` (Dono sintético da própria clínica não tem vínculo formal e não é editável — comportamento já existente, preservado).

- **R6 — Migração de dado legado (responsabilidade do `imedto-database`)**: o texto livre legado em `especialidade_convidada` é normalizado contra o catálogo de especialidades da **profissão do próprio vínculo** (`profissao_convidada_id`): match **case-insensitive e normalizado** (trim, acentuação) → grava o **nome canônico do catálogo**; sem match (ou vínculo sem `profissao_convidada_id`) → grava **NULL** (cai no COALESCE para o cadastro global). Mora em: migration idempotente em `db/migrations/`. Validada em: **migration + relatório de contagem** (ver seção 5).

- **R7 — Conselho acompanha a profissão**: o conselho exibido (CRM/CRO/...) deriva de `conselhoSigla` da profissão do catálogo (`ProfissaoCatalogo.conselhoSigla`). Quando a profissão do vínculo muda, a exibição do conselho deve refletir a nova profissão. Não há campo de número de conselho editável neste escopo — apenas a **sigla derivada da profissão**. Se o catálogo da profissão não define sigla, o conselho não é exibido. Mora em: Query/Front (derivação na leitura). Validada em: **front** (exibição) — sem regra de negócio de gravação adicional.

## 5. Modelo de dados

**Sem nova tabela e sem nova coluna.** O vínculo (`VinculoProfissionalEstabelecimento`) já possui:
- `especialidade_convidada` (string, nullable) — string única, mantida.
- `profissao_convidada_id` (long, nullable, FK catálogo de profissões) — adicionada na migration `20260510004306_AdicionarProfissaoConvidadaEmVinculo`. Já existe; só passa a ser **editável** pós-convite.

**Migração de dado (idempotente, autoria `imedto-database`)**:
- Para cada vínculo com `especialidade_convidada IS NOT NULL`:
  - Se existe especialidade ativa no catálogo da `profissao_convidada_id` do vínculo cujo nome bate (normalizado: `lower(unaccent(trim(...)))`) → `UPDATE especialidade_convidada = <nome_canonico_catalogo>`.
  - Caso contrário → `UPDATE especialidade_convidada = NULL`.
- **Multi-tenant**: a migração roda sobre todos os vínculos, mas cada `UPDATE` é restrito ao registro do próprio vínculo — não há cruzamento entre estabelecimentos; o match usa a profissão do próprio vínculo.
- **Não apagar mais do que o necessário**: vínculos com `especialidade_convidada IS NULL` ou já casando exato com o catálogo não são tocados. `profissao_convidada_id` **nunca** é alterada pela migração.
- **Validação obrigatória pelo DB agent** (reportar no PR): quantos registros total, quantos **casaram** (mantidos como catálogo), quantos **viraram NULL**. O DB agent decide se o volume de NULLs exige aval do dono antes de aplicar.
- **PII**: `especialidade` é dado profissional de baixo risco (mesma visibilidade de nome — ver `ProfissionalPublicoDtoTests`). Sem audit trail adicional exigido. Sem PII em log.

**DTO**: `ProfissionalVinculadoDto` ganha `ProfissaoConvidadaId` (`long?`). Mantém `Profissao` (nome) e `Conselho`. Ajustar a Query (`ListarProfissionaisEstabelecimentoQueryHandlers` / `VinculoQueryRepository`) para projetar `profissao_convidada_id`.

## 6. UX e fluxo

Modal `ProfissionalDetalhesModal.vue`, aba **Perfil**, bloco "Especialidade neste estabelecimento" (hoje `AppInput` texto livre). Vira:

```
[ Profissão neste estabelecimento ▾ ]   ← AppSelect, profissões do catálogo, pré-selecionada via profissaoConvidadaId
[ Especialidade ▾ ]                      ← AppSelect, habilita só após profissão; itens do catálogo da profissão
[ Conselho: CRM ]                        ← derivado da profissão (sigla), read-only
i  Trocar a profissão limpa a especialidade. Vazio = usa o cadastro global do profissional.
[ Salvar ]                               ← persiste profissão+especialidade atomicamente
```

- **Reuso obrigatório**: copiar a mecânica do `ConvidarProfissionalModal.vue` — `catalogoService.listarProfissoes()`, `watch(profissaoId)` que limpa especialidade + recarrega `listarEspecialidades(id)`, flag `carregandoEspecialidades`, computed `profissaoTemEspecialidades`. O valor da especialidade é o **nome** (string), igual ao convite (não id). NÃO duplicar a lógica — extrair, se necessário, para reuso entre os dois modais (avaliar componente compartilhado; decisão técnica do dev, ver seção 9).
- **Componentes do design system**: `AppSelect`, `AppField`, `AppButton`. Sem HTML/CSS novo fora do padrão.
- **Estados**:
  - **Loading profissões**: dropdown de profissão desabilitado enquanto `listarProfissoes()` não resolve.
  - **Loading especialidades**: dropdown de especialidade desabilitado, placeholder "Carregando...".
  - **Profissão sem especialidades no catálogo**: dropdown de especialidade não renderiza (como no convite — `profissaoTemEspecialidades`), especialidade fica nula.
  - **Vazio**: profissão sem seleção → especialidade desabilitada com placeholder "Selecione a profissão primeiro".
  - **Erro back (422/403)**: mensagem genérica em `erro.value`, sem PII.
  - **Sucesso**: emite `atualizado` com profissão+especialidade novas; header do modal reflete (`ph-spec`, conselho).
- **Mobile-ready**: grid já colapsa para 1 coluna em `max-width: 720px` (preservar).

## 7. Critérios de aceite (testáveis)

- **CA1 (dropdown de profissão — caminho feliz)**: Dado um Dono no modal de detalhes de um profissional com `profissaoConvidadaId` definido, Quando a aba Perfil carrega, Então o dropdown de profissão renderiza populado por `/catalogo/profissoes` e vem **pré-selecionado** com a profissão do vínculo.

- **CA2 (especialidade dependente)**: Dado o dropdown de profissão com uma profissão selecionada, Quando o usuário abre o dropdown de especialidade, Então ele lista **apenas** as especialidades ativas daquela profissão (`/catalogo/especialidades?profissaoId=`), e vem pré-selecionada a especialidade atual do vínculo se ela existir no catálogo.

- **CA3 (troca de profissão limpa especialidade)**: Dado uma profissão e especialidade já selecionadas, Quando o usuário troca a profissão, Então a especialidade selecionada é **limpa** e o dropdown de especialidade recarrega com as especialidades da nova profissão.

- **CA4 (persistência atômica no vínculo, não no global)**: Dado um Dono que selecionou profissão X + especialidade Y e clicou em Salvar, Quando o back grava, Então `profissao_convidada_id` e `especialidade_convidada` do **vínculo** são atualizados num único comando, e o cadastro global do profissional (`p.profissao`/`p.especialidade`) **permanece inalterado**.

- **CA5 (multi-tenant — só o estabelecimento corrente)**: Dado um profissional com vínculos em dois estabelecimentos A e B, Quando o Dono de A altera a profissão/especialidade pelo modal, Então **apenas** o vínculo de A é alterado; o vínculo de B permanece intocado. E dado um usuário do estabelecimento B tentando alterar um vínculo do A, Quando chama o endpoint, Então recebe erro genérico ("não encontrado") e nada é gravado nem logado com PII.

- **CA6 (RBAC — só o Dono)**: Dado um usuário logado com papel diferente de Dono (`permissoes.ehDono === false`), Quando abre o modal, Então os dropdowns de profissão/especialidade renderizam **somente leitura** (não editáveis) e o endpoint, se chamado diretamente, retorna **403**. Dado o Dono, Quando edita, Então a operação é permitida.

- **CA7 (catálogo estrito — sem texto livre)**: Dado uma requisição ao endpoint com uma especialidade que **não** pertence à profissão informada (ou inativa), Quando o handler valida, Então retorna **422** com mensagem "Especialidade não pertence à profissão selecionada ou está inativa." e nada é gravado. (Front nunca permite digitar texto livre — só seleção.)

- **CA8 (migração legada — casa → mantém)**: Dado um vínculo legado com `especialidade_convidada = "dermatologia"` (texto livre minúsculo) e profissão Médico cujo catálogo tem "Dermatologia", Quando a migration roda, Então `especialidade_convidada` passa a valer **"Dermatologia"** (nome canônico do catálogo).

- **CA9 (migração legada — não casa → nulo)**: Dado um vínculo legado com `especialidade_convidada = "Acupunturólogo zen"` que não existe no catálogo da profissão do vínculo (ou vínculo sem `profissao_convidada_id`), Quando a migration roda, Então `especialidade_convidada` vira **NULL** e a exibição passa a usar o cadastro global via COALESCE. E `profissao_convidada_id` **não** é alterada em nenhum caso.

- **CA10 (migração idempotente + relatório)**: Dado a migration já aplicada uma vez, Quando é reaplicada, Então nenhum registro é alterado de novo (idempotência), e o PR do DB agent reporta a contagem {total com especialidade, casaram, viraram NULL}.

- **CA11 (estados vazio/loading dos dropdowns)**: Dado a profissão ainda não selecionada, Quando o modal abre, Então o dropdown de especialidade aparece desabilitado com placeholder de orientação. Dado especialidades carregando, Quando a request está em voo, Então o dropdown mostra "Carregando..." e fica desabilitado. Dado uma profissão sem especialidades no catálogo, Quando selecionada, Então o dropdown de especialidade não é exibido e a especialidade fica nula.

- **CA12 (conselho acompanha a profissão)**: Dado o Dono trocou a profissão de uma com `conselhoSigla = "CRM"` para uma com `conselhoSigla = "CRO"`, Quando salva e o modal recarrega, Então o conselho exibido passa de "CRM" para "CRO". E dado uma profissão sem `conselhoSigla`, Quando selecionada, Então nenhum conselho é exibido.

- **CA13 (não-regressão de leitura)**: Dado agenda, prontuário, seletores de profissional e PDFs que leem profissão/especialidade do vínculo via `COALESCE(v.especialidade_convidada, p.especialidade)`, Quando profissão/especialidade do vínculo são alteradas pelo modal, Então essas telas refletem o novo valor sem quebrar (a leitura já passa pelo COALESCE — confirmar que nenhuma delas lê texto livre fora do catálogo).

## 8. Riscos e dependências

- **Migração destrutiva (NULL)**: o passo R6 converte texto-livre-sem-match em NULL. Risco de perda percebida de dado se o catálogo estiver incompleto. **Mitigação**: DB agent reporta a contagem de NULLs **antes** de o QA commitar; se o volume for alto, dono decide se amplia o catálogo antes. Não há rollback de conteúdo de texto livre depois do NULL — o valor original se perde. (Aceito pelo dono na decisão 5.)
- **Quebra de contrato do DTO**: adicionar `ProfissaoConvidadaId` muda `ProfissionalVinculadoDto` — checar consumidores no front (`vinculoService`) e testes de contrato (`ProfissionalPublicoDtoTests` — atenção: o DTO **público** é mais restrito; `profissaoConvidadaId` vai no DTO **vinculado/interno**, não no público de seletores).
- **Substituição do command**: trocar `AlterarEspecialidadeDoVinculoCommand` por um command profissão+especialidade afeta `ModeloPermissaoController.AlterarEspecialidadeDoVinculo`, o handler, e os testes `AlterarEspecialidadeDoVinculoCommandHandlerTests`. Atualizar a rota/DTO do controller e o `vinculoService.alterarEspecialidade` no front.
- **Áreas regressivas**: permissionamento (RBAC do vínculo), leitura COALESCE em agenda/prontuário/seletores/PDFs (memória [[especialidade-por-vinculo]] lista os 3 pontos de leitura + PDFs).
- **Dependência**: catálogos `/catalogo/profissoes` e `/catalogo/especialidades` já existem e são usados pelo convite — sem novo endpoint de catálogo.

## 9. Observações para execução

- **Não-negociável**:
  - Catálogo estrito back+front (R1). Nenhum caminho que aceite texto livre.
  - Persistência **atômica** profissão+especialidade num único comando (R2) — não fazer dois PUTs sequenciais.
  - RBAC via `permissoes.ehDono`, **nunca** via `ehDono` local do componente (R4 — armadilha já documentada no CA7 de 2026-06-03_004).
  - Filtro multi-tenant por `EstabelecimentoId` no handler; grava só no vínculo corrente (R3/CA5).
  - Migração de dado é **exclusivamente** do `imedto-database`, idempotente, em `db/migrations/`, com relatório de contagem (R6/CA10). O dev **não** mexe em dado legado por código de aplicação.
- **Reuso > duplicação**: a mecânica profissão→especialidade (serviço `catalogoService`, watch que limpa especialidade, `carregandoEspecialidades`, `profissaoTemEspecialidades`) **já existe** em `ConvidarProfissionalModal.vue`. O dev deve reaproveitar — preferencialmente extraindo um composable/componente compartilhado (ex.: `useProfissaoEspecialidade` ou `ProfissaoEspecialidadeFields.vue`) consumido pelos **dois** modais. Decisão de **como** extrair é técnica (dev), mas duplicar o bloco inteiro é recusado.
- **Liberdade técnica**: nome do command/composable, estrutura do controller DTO, forma da query SQL de projeção do `profissaoConvidadaId`.
- **Arquivos âncora confirmados na investigação**:
  - Front modal: `frontend/src/components/equipe/ProfissionalDetalhesModal.vue` (bloco `especialidade-edit`, hoje `AppInput`).
  - Front convite (fonte de reuso): `frontend/src/components/equipe/ConvidarProfissionalModal.vue` (watch `form.profissaoId`, `profissaoTemEspecialidades`).
  - Front serviço catálogo: `frontend/src/services/catalogoService.ts` (`listarProfissoes`, `listarEspecialidades`).
  - Front serviço vínculo: `frontend/src/services/vinculoService.ts` (`alterarEspecialidade` → ajustar).
  - Back command: `backend/.../Vinculos/Commands/AlterarEspecialidadeDoVinculoCommand.cs` + handler.
  - Back controller: `ModeloPermissaoController.AlterarEspecialidadeDoVinculo`.
  - Back validação catálogo (reuso): `ConvidarProfissionalCommandHandler` usa `ExisteEspecialidadeAtivaPorNome(profId, nome)`.
  - Back DTO: `backend/.../Vinculos/Queries/Results/ProfissionalVinculadoDto.cs` (+ Query `ListarProfissionaisEstabelecimentoQueryHandlers` / `VinculoQueryRepository`).
  - Entidade vínculo: campos `EspecialidadeConvidada` (string) + `ProfissaoConvidadaId` (long?) — coluna adicionada em `20260510004306_AdicionarProfissaoConvidadaEmVinculo`.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — atualizar a descrição do contrato de alteração do vínculo: o comando de especialidade passa a ser **profissão+especialidade atômico**; documentar que `ProfissionalVinculadoDto` expõe `profissaoConvidadaId`. Ajuste cirúrgico na seção de Vínculos/CQRS — não reescrever o doc.
- **`Docs/DESIGN.md`** — se o dev extrair o composable/componente compartilhado de profissão→especialidade (recomendado), registrá-lo na seção de componentes/padrões reutilizáveis do design system (ex.: `ProfissaoEspecialidadeFields` ou `useProfissaoEspecialidade`), citando que é a fonte única usada por convite **e** detalhes do profissional.
- **`Docs/LGPD.md`** — **não** precisa: especialidade é dado profissional de baixo risco, sem novo PII, sem novo audit. (Citado explicitamente para o QA não cobrar.)
- **Migração de dado**: o `imedto-database` documenta a migração e o relatório de contagem no PR/migration; sem mudança estrutural de schema que exija nova entrada em `INFRA.md`/`COMANDOS.md` (sem nova extensão/índice estratégico — confirmar com o DB agent se o `unaccent` já está habilitado; se precisar habilitar a extensão `unaccent`, aí sim `INFRA.md`/`COMANDOS.md` são atualizados pelo DB agent).
